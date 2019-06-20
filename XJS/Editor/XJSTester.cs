
#if UNITY_EDITOR && !UNITY_WEBPLAYER

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.IO;
using System.Collections.Generic;

using static XJS;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace LevelUpper.Editor {

	public class XJSTester : EditorWindow {
		
		public TextAsset testScript;

		[MenuItem("Window/Custom/XJSTester")]
		public static void ShowWindow() { 
			EditorWindow.GetWindow(typeof(XJSTester)); 
		}
		
		public XJSTester() {
			
		}
		
		void OnGUI() { 
			
			testScript = EditorGUILayout.ObjectField("Test Script", testScript, typeof(TextAsset), false) as TextAsset;
			

			if (GUILayout.Button("Job It")) {
				string path = AssetDatabase.GetAssetPath(testScript);
				
				Script s = Script.FromFile(path);
				Debug.Log($"Program Loaded: {s.program}");
				Debug.Log($"Running program: ");
				var result = s.Run();

				Debug.Log($"Result: {result}");


			}
			
		}
		
		void Update() { }
		void OnInspectorUpdate() { }
		
		void OnFocus() { }
		void OnLostFocus() { }

		void OnSelectionChange() { }
		void OnHierarchyChange() { }
		void OnProjectChange() { }
		
		void OnDestroy() { }
		
	}
	
}
#endif
