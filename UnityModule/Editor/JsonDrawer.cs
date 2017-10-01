#if UNITY_3 || UNITY_4 || UNITY_5 || UNITY_2017
#define UNITY
#endif
#if UNITY

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[CustomEditor(typeof(JsonData))]
public class JsonDataEditor : Editor {

	
	public override void OnInspectorGUI() {

		base.OnInspectorGUI();
		JsonData tg = target as JsonData;
		
		
		tg.json = JsonDrawer.DrawLive(tg.json, ref tg.cached, tg.name);
		if (GUI.changed) {
			EditorUtility.SetDirty(tg);
		}
		
	}

}


[CustomEditor(typeof(BehaviourWithJsonData), true)]
public class EmbeddedJsonEditor : Editor {
	

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();
		serializedObject.Update();
		
		var fields = target.GetType().GetFields();
		Editor _editor = null;
		int i = 0;
		foreach (var field in fields) {
			if (field.FieldType == typeof(JsonData)) {
				string fieldName = field.Name;
				
				GUI.color = JsonDrawer.ModColor(i++);
				GUILayout.BeginVertical("box"); {
					GUI.color = Color.white;

					var attachedAsset = serializedObject.FindProperty(fieldName);
					if (attachedAsset.objectReferenceValue != null) {
						string assetName = attachedAsset.objectReferenceValue.name;
						
						GUILayout.Label("JsonData [" + assetName + "] assigned to [" + fieldName + "]");

						CreateCachedEditor(attachedAsset.objectReferenceValue, null, ref _editor);
						//var editor = _editor as JsonDataEditor;
						//editor.assetName = assetName;

						_editor.OnInspectorGUI();
						serializedObject.ApplyModifiedProperties();
					} else {

						GUILayout.Label("Nothing Assigned to [" + fieldName + "]");
					}

				} GUILayout.EndVertical();
			}
		}



	}

}


public static class JsonDrawer {
	public static readonly string[] EMPTY = new string[] { };

	static string addKey = "";
	static string addValue = "";
	static HashSet<string> toggledPaths = new HashSet<string>();
	static bool expandedByDefault = false;

	const string FOCUS_BUSTER = "AYYLMAOOOO";
	private static void Unfocus() {
		GUI.SetNextControlName(FOCUS_BUSTER);
		GUI.TextField(new Rect(-1000, -1000, 1, 1), "");
		GUI.FocusControl(FOCUS_BUSTER);
	}

	public static Color ModColor(int i) {
		i = (i < 0) ? -i : i;
		return nestColors[i % nestColors.Length];
	}

	public static string DrawLive(string serialized, ref JsonValue live, string label = "Json") {

		GUI.color = Color.Lerp(live == null ? Color.green : Color.red, Color.white, .75f);
		
		if (GUILayout.Button(live == null ? "↕↕ Deserializing  ↕↕" : "↑↑ Serializing ↑↑", GUILayout.ExpandWidth(true))) {
			if (live == null) { 
				try {
					live = Json.Parse(serialized);
				} catch (Exception e) {
					Debug.LogWarning("Error during parse: " + e.StackTrace);
				}
			} else { live = null; }
			
		}
		GUI.color = Color.white;

		if (live != null) { 
		
			if (Draw(live, label)) {
				return live.ToString();
			}
			return serialized; 
		}
		return DrawFromString(serialized, label);
	}

	public static string DrawFromString(string fieldVal, string label = "Json") {
		try {
			JsonValue val = Json.Parse(fieldVal);
			if (val.isObject || val.isArray) {
				if (Draw(val, label)) {
					return val.ToString();
				}
			} else {
				GUILayout.BeginVertical("box"); {
					GUILayout.Label(label);
					GUILayout.Label("Not an object or array.");
				} GUILayout.EndVertical();
			}


		} catch (Exception e) {
			GUILayout.BeginVertical("box"); {
				GUILayout.Label(label);
				GUILayout.Label("Error during draw.");
				
				Color oldContent = GUI.contentColor;
				Color oldSkin = GUI.skin.label.normal.textColor;
				
				GUI.skin.label.normal.textColor = Color.red;
				GUI.color = Color.red;
				GUI.contentColor = Color.red;
				
				GUILayout.Box(e.StackTrace);
				
				GUI.skin.label.normal.textColor = oldSkin;
				GUI.color = Color.white;
				GUI.contentColor = oldContent;
			} GUILayout.EndVertical();

		}

		return fieldVal;
	}

	public static Color[] nestColors = new Color[] {
		Color.white,
		new Color(.8f, .8f, .8f),
	};

	public static bool Draw(JsonValue root, string rootLabel = "Json") {
		if (root == null) {
			GUILayout.BeginVertical("box"); {
				GUILayout.Label(rootLabel);
				GUILayout.Label("Root JsonValue is NULL");
			} GUILayout.EndVertical();
			return false;
		}
		if (!root.isObject && !root.isArray) {
			GUILayout.BeginVertical("box"); {
				GUILayout.Label(rootLabel);
				GUILayout.Label("Root JsonValue needs to be a JsonObject or JsonArray");
			} GUILayout.EndVertical();
			return false;
		}

		// Uncomment in case we need to do any custom GUI stuff...
		// GUILayout.Label("");
		// Rect next = GUILayoutUtility.GetLastRect();
		JsonValue changeKey = null;
		JsonValue removeKey = null;
		JsonValue changeValue = null;
		JsonValue changeTarget = null;
		string currentPath = rootLabel;
		Stack<string> history = new Stack<string>();
		Stack<JsonValue> visited = new Stack<JsonValue>();

		var smallButton = GUILayout.Width(24);
		history.Push(currentPath);


		Func<string, bool> isExpanded = (path) => {
			
			// 'root' paths could be considered expanded by default by uncommenting this...

			//if (history.Count == 1) { return !toggledPaths.Contains(currentPath); }
			if (toggledPaths.Contains(path)) { return !expandedByDefault; }
			return expandedByDefault;
		};

		Action<JsonValue, JsonValue> removeButton = (obj, key) => {
			if (obj.isObject || obj.isArray) {
				if (GUILayout.Button("x", smallButton)) { removeKey = key; changeTarget = obj; }
			}
		};

		Action<string> toggle = (path) => {
			if (toggledPaths.Contains(path)) { toggledPaths.Remove(path); }
			else { toggledPaths.Add(path); }
		};
		
		// These may use eachother, so we have to declare them before we bind to them.
		Action<JsonValue, JsonValue, JsonValue> drawField = null;
		Action<JsonValue, JsonObject> drawJsonObject = null;
		Action<JsonValue, JsonArray> drawJsonArray = null;
		Action<JsonValue> addFieldControl = null;

		Action<JsonValue> pushPath = (next) => {
			history.Push(currentPath);
			currentPath = currentPath + "." + next.stringVal;
		};

		Action popPath = () => { 
			currentPath = history.Pop();	
		};

		addFieldControl = (target) => {
			GUILayout.BeginHorizontal("button"); {
				
				//string valueFieldName = currentPath + "_value";
				//string keyFieldName = currentPath + "_key";
				var e = Event.current;
				bool acceptedAdd = false;
				bool returnDetected = false;

				returnDetected = (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return);

				if (GUILayout.Button("+", smallButton)) {
					acceptedAdd = true;
				}

				if (target.isArray) {
					GUILayout.Label(target.Count + ": ", GUILayout.ExpandWidth(false));
					addValue = EditorGUILayout.TextField(addValue);
					
				} else if (target.isObject) {
					addKey = EditorGUILayout.TextField(addKey);
					GUILayout.Label(":", GUILayout.ExpandWidth(false));
					addValue = EditorGUILayout.TextField(addValue);
				}

				if (!acceptedAdd) {
					acceptedAdd = returnDetected && (e.type == EventType.Used);
				}

				if (acceptedAdd) {

					if (addKey != "" || target.isArray) {
						changeTarget = target;
						changeKey = target.isArray ? (JsonValue)target.Count : (JsonValue)addKey;

						float val;
						if (float.TryParse(addValue, out val)) { changeValue = val; } 
						else if (addValue == "true") { changeValue = true; } 
						else if (addValue == "false") { changeValue = false; } 
						else if (addValue == "{}") { changeValue = new JsonObject(); } 
						else if (addValue == "[]") { changeValue = new JsonArray(); } 
						else { changeValue = addValue; }

						addKey = "";
						addValue = "";
					}

				}

			} GUILayout.EndHorizontal();

		};

		drawJsonArray = (keyLabel, arr) => {
			bool expanded = isExpanded(currentPath);
			GUILayout.BeginHorizontal(); {
				if (currentPath.Contains('.')) {
					var parent = visited.Peek();
					removeButton(parent, keyLabel);
				}
				GUILayout.Label(keyLabel, GUILayout.ExpandWidth(false));
				GUILayout.Label(":", GUILayout.ExpandWidth(false));
				GUILayout.Label("[", GUILayout.ExpandWidth(false));

				if (!expanded) {
					if (GUILayout.Button("...")) { toggle(currentPath); }
					GUILayout.Label("]");
					GUILayout.FlexibleSpace();
				} else if (GUILayout.Button("-", smallButton)) {
					toggle(currentPath);
				}
			} GUILayout.EndHorizontal();

			
			if (expanded) {
				GUI.color = nestColors[visited.Count % nestColors.Length];
				GUILayout.BeginVertical("box"); {
					GUI.color = Color.white;

					for (int i = 0; i < arr.Count; i++) {
						
						JsonValue key = i;
						JsonValue val = arr[key];

						drawField(arr, key, val);
					}

					addFieldControl(arr);

				} GUILayout.EndVertical();
				GUILayout.Label("]");
			}
		};

		drawJsonObject = (keyLabel, obj) => {
			bool expanded = isExpanded(currentPath);
			GUILayout.BeginHorizontal(); {
				if (currentPath.Contains('.')) { 
					var parent = visited.Peek();
					removeButton(parent, keyLabel); 
				}
				GUILayout.Label(keyLabel, GUILayout.ExpandWidth(false));
				GUILayout.Label(":", GUILayout.ExpandWidth(false));
				GUILayout.Label("{", GUILayout.ExpandWidth(false));
				
				if (!expanded) {
					if (GUILayout.Button("...")) { toggle(currentPath); }
					GUILayout.Label("}");
					GUILayout.FlexibleSpace();
				} else if (GUILayout.Button("-", smallButton)) {
					toggle(currentPath);
				}
			} GUILayout.EndHorizontal();

			if (expanded) {
				GUI.color = nestColors[visited.Count % nestColors.Length];
				GUILayout.BeginVertical("box"); {
					GUI.color = Color.white;

					foreach (var pair in obj) {
						JsonValue key = pair.Key;
						JsonValue val = pair.Value;

						drawField(obj, key, val);
					}

					addFieldControl(obj);

				} GUILayout.EndVertical();
				GUILayout.Label("}");
			}

		};


		drawField = (obj, key, val) => {

			if (val.isString || val.isBool || val.isNumber) {

				GUILayout.BeginHorizontal(); {
					removeButton(obj, key);
					string keyLabel = key.stringVal + " : ";

					float fval;
					string cval;
					if (float.TryParse(val.stringVal, out fval)) {
						cval = EditorGUILayout.FloatField(keyLabel, fval).ToString();
					} else {
						cval = EditorGUILayout.TextField(key.stringVal + " : ", val.stringVal);
					}
					if (cval != val.stringVal) {
						changeKey = key;
						changeTarget = obj;
						
						if (float.TryParse(cval, out fval)) { changeValue = fval; }
						else if (cval == "true") { changeValue = true; }
						else if (cval == "false") { changeValue = false; }
						else if (cval == "[]") { changeValue = new JsonArray(); Unfocus(); }
						else if (cval == "{}") { changeValue = new JsonObject(); Unfocus(); }
						else { changeValue = cval; }

						
						
					}

				} GUILayout.EndHorizontal();

			} else if (val.isObject) {

				pushPath(key.stringVal);
				visited.Push(obj);

				drawJsonObject(key, val as JsonObject);

				visited.Pop();
				popPath();

			} else if (val.isArray) {

				pushPath(key.stringVal);
				visited.Push(obj);

				drawJsonArray(key, val as JsonArray);

				visited.Pop();
				popPath();
			}

			

		};

		if (root.isObject) { drawJsonObject(rootLabel, root as JsonObject); }
		else if (root.isArray) { drawJsonArray(rootLabel, root as JsonArray); }

		if (changeTarget == null) {
			if (removeKey != null) {
				Debug.LogWarning("JsonObjectDrawer: Tried to remove key " + removeKey.stringVal + " with null target.");
			}
			if (changeKey != null) {
				Debug.LogWarning("JsonObjectDrawer: Tried to change key " + changeKey.stringVal + " with null target.");
			}
		} else if (changeTarget.isObject) {

			if (removeKey != null) {
				changeTarget[removeKey] = null;
			}
			if (changeKey != null) {
				changeTarget[changeKey] = changeValue;
			}

		} else if (changeTarget.isArray) {
			JsonArray asArr = changeTarget as JsonArray;
			if (removeKey != null) {
				asArr.RemoveAt(removeKey.intVal);
			}

			if (changeKey != null) {
				int index = changeKey.intVal;
				if (index == asArr.Count) { asArr.Add(changeValue); }
				else { asArr[changeKey] = changeValue; }
			}

		}


		return (removeKey != null || changeKey != null);
	}
	
	
}


#endif
