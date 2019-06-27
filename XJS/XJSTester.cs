#if UNITY_2018 || UNITY_2019
#define UNITY
#endif
// Unity detection
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define COMP_SERVICES
using System.Runtime.CompilerServices;

#endif
// Use UnityEngine's provided utilities.
using UnityEngine;


#else
// Hook into some other useful diagnostic stuff
#define COMP_SERVICES
using System.Runtime.CompilerServices;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Concurrent;
using static JsonTests.TestFramework;

namespace JsonTests {

#if DEBUG
	public static class XJS_Tests {

		private static void TestRun(string input, object expected, XJS.Interpreter interp = null) {
			if (interp == null) { interp = new XJS.Interpreter(); }
			var program = XJS.Parse(input);
			var result = interp.Execute(program);

			result.ShouldEqual(expected);
		}

		private static void DebugRun(string input, object expected, XJS.Interpreter interp = null) {
			if (interp == null) { interp = new XJS.Interpreter(); }
			var program = XJS.Parse(input);
			Debug.Log(program);
			var result = interp.Execute(program);

			result.ShouldEqual(expected);
		}

		public static void TestBasics() {
				TestRun("5", 5);
				TestRun("5;", 5);
				TestRun("return 5;", 5);
				TestRun("return \"5\";", 5);
				TestRun("return \"five\";", "five");
				TestRun("x = 5; return x;", 5);
				TestRun("x = 5; x;", 5);
				TestRun("x = 5; x", 5);
				TestRun("x = 5 x", 5);
				TestRun("var x = 5; return x;", 5);
				TestRun("var x = 5; x;", 5);
		}
		public static void TestIncDec() {
			TestRun("x = 5; x++; x;", 6);
			TestRun("x = 5; x++; -x;", -6);
			TestRun("x = 5; x--; x;", 4);
			TestRun("x = 5; x--; -x;", -4);
				
			TestRun("x = -5; x++; x;", -4);
			TestRun("x = -5; x++; -x;", 4);
			TestRun("x = -5; x--; x;", -6);
			TestRun("x = -5; x--; -x;", 6);
		}
		
		public static void TestOperators() {
			TestRun("5 + 4 + 3 + 2 + 1", 15);
			TestRun(
@"""the"" + "" "" + ""quick"" + "" "" + ""brown"" + "" "" + ""fox"" + "" "" + ""jumps"" + "" "" + ""over"" + "" "" + ""the"" + "" "" + ""lazy"" + "" "" + ""dog""", 
"the quick brown fox jumps over the lazy dog");

			

		}


	}

#endif
}