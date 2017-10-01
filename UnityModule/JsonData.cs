#if UNITY_3 || UNITY_4 || UNITY_5 || UNITY_2017
#define UNITY
#endif
#if UNITY

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewJsonDataAsset", menuName = "Json Data Asset File", order = 55)]
public class JsonData : ScriptableObject {
	
	[TextArea(3, 6)] public string json = "{}";
	[NonSerialized] public JsonValue cached = null;

	public JsonValue value {
		get {
			if (cached == null) {
				Load();
			}
			return cached;
		}
	}

	private void Load() {
		try {
			if (cached == null) {
				cached = Json.Parse(json);
			}
		} catch (Exception e) {
			Debug.LogWarning("JsonData.Load: Failed to load Json\n" + json);
			Debug.LogWarning(e.StackTrace);
		}
	}

	public void Awake() {
		Load();
	}

	public void OnEnable() {
		Load();
	}

	public void OnDisable() {
		if (cached != null) { cached = null; }
	}

	private static int id = 10000;
	public static JsonData Factory() {
		var asset = CreateInstance<JsonData>();
		asset.name = "RuntimeJson" + (id++);
		return asset;
	}
	
}



public abstract class BehaviourWithJsonData : MonoBehaviour { }

#endif
