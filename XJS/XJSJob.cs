using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public partial class XJS {


	/// <summary> Type representing a single loaded file </summary>
	public class Script {
		/// <summary> Original relative filepath </summary>
		public string filename;
		/// <summary> Original resource path </summary>
		public string resource;
		/// <summary> Text loaded from file </summary>
		public string text;
		/// <summary> Parse tree built from script </summary>
		public Node program;

		private Script() { }
		
		public static Script FromFile(string filename) { 
			return new Script() { filename = filename }.Load(); 
		}
		public static Script FromResource(string path) {
			return new Script() { resource = path }.Load();
		}
		public static Script FromText(string text) {
			return new Script() { text = text }.Load();
		}

		public Script Load() {
			if (filename != null) {
				try {
					text = File.ReadAllText(filename);
				} catch (Exception e) { 
					Debug.LogError($"Failed to load script from file: {e}");
				}

			} else if (resource != null) {
				try {
					text = Resources.Load<TextAsset>(resource).text;
				} catch (Exception e) {
					Debug.LogError($"Failed to load script from resource: {e}");
				}

			}

			if (text != null) {
				text = XJSHelpers.StripCStyleComments(text);
				Tokenizer tk = new Tokenizer(text);
				program = ParseProgram(tk);
			}

			return this;
		}


		public JsonValue Run() {
			if (program != null) {
				XJSInterpreter interp = new XJSInterpreter();
				try {
					return interp.Execute(program);

				} catch (Exception e) {
					Debug.LogWarning("Running program failed!\n" + e);
				}
			}

			return null;
		}
	}
	
}
