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

		public static void TestLambdas() {
			TestRun(@"x = func()=>{ ""yay"" }; x()", "yay");
			TestRun(@"x = func()->{ ""yay"" }; x()", "yay");
			TestRun(@"x = func(x)->{ x+5 }; x(10)", 15);
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

		public static void TestLiterals() {
			{
				JsonArray expected = new JsonArray();
				TestRun(@"[]", expected);
				TestRun(@"return []", expected);
				TestRun(@"x = func () => { return [] }; x()", expected);
				TestRun(@"x = func () => { [] }; x()", expected);
			}
			{
				JsonArray expected = new JsonArray(1, 2, 3);
				TestRun(@"[1,2,3]", expected);
				TestRun(@"x = []; x.Add(1); x.Add(2); x.Add(3); return x;", expected);
				TestRun(@"x = [] x.Add(1) x.Add(2) x.Add(3) return x", expected);
				TestRun(@"x = [] x.Add(1) x.Add(2) x.Add(3) x", expected);
				TestRun(@"return [1,2,3]", expected);
				TestRun(@"x = func()=>{ return [1,2,3]; }; return x()", expected);
			}
			{
				JsonObject expected = new JsonObject("x", 4, "y", 5, "z", 6);
				// An object literal and code block are ambiguous, 
				// so to return an object literal, only the 'return' statement is valid. 
				TestRun(@"return { x: 4, y: 5, z: 6 } ", expected);
				TestRun(@"obj = {}; obj[""x""] = 4; obj[""y""] = 5; obj[""z""] = 6; return obj", expected);
				TestRun(@"obj = {}; obj.x = 4 obj.y = 5 obj.z = 6 return obj", expected);

			}
			
		}

		public static void TestPaths() {
			{
				JsonObject expected = new JsonObject("jumpedOver", "true");
				TestRun(@"
the = { 
	quick: { 
		brown: { 
			fox: { 
				jumpsOver: func(it)->{
					it[""jumpedOver""] = true
					return it;
				}
			} 
		} 
	},
	lazy: {
		dog: {

		}
	}
}
adj = [ ""quick"", ""brown"", ""lazy"" ]
the[ adj[0] ][ adj[1] ].fox.jumpsOver(the[ adj[2] ].dog);
", expected);
			}
		}

		public static void TestLogic() {
			{
				JsonValue expected = "yes";
				TestRun(@"x = 5; if (x == 5) { ""yes"" } else { ""no"" }", expected);
				TestRun(@"x = 5; if (x != 5) { ""no"" } else { ""yes"" }", expected);
			}
			{
				JsonValue expected = "five";
				string counter = @"
if (x == 1) { ""one"" }
else if (x == 2) { ""two"" }
else if (x == 3) { ""three"" }
else if (x == 4) { ""four"" }
else if (x == 5) { ""five"" }
else if (x == 6) { ""six"" }
else if (x == 7) { ""seven"" }
else if (x == 8) { ""eight"" }
else if (x == 9) { ""nine"" }
else { ""I can't count that high."" }
";
				TestRun("x=5;\n"+counter, "five");
				TestRun("x=3;\n"+counter, "three");
				TestRun("x=7;\n"+counter, "seven");
				TestRun("x=999;\n"+counter, "I can't count that high.");
				TestRun("x=9913219;\n"+counter, "I can't count that high.");
			}
		}

		public static void TestForLoop() {
			{
				TestRun("x = 0; for (var i = 0; i <= 10; i++) { x++ }", 10);
				TestRun("x = 0; for (var i = 0; i < 10; i++) { x++ }", 9);
				TestRun("x = 0; for (var i = 0; i < 10; i++) { ++x }", 10);
			}
		}

		public static void TestWhileLoop() {
			{
				TestRun(@"x = 0; while(x <= 10) { x++ }", 10);
				TestRun(@"x = 0; while(x < 10) { x++ }", 9);
				TestRun(@"x = 0; while(x < 10) { ++x }", 10);
			}
		}
		public static void TestDoWhileLoop() {
			{
				TestRun(@"x = 0; do { x++ } while (x <= 10)", 10);
				TestRun(@"x = 0; do { x++ } while (x < 10)", 9);
				TestRun(@"x = 0; do { ++x } while (x < 10)", 10);
			}
		}

		public static void TestEachLoop() {
			{
				TestRun(@"x = 0; arr = [ 1, 2, 3, 4, 5]; each (v in arr) { x += v }", 15);
				TestRun(@"x = """"; arr = [ ""1"", ""2"", ""3"", ""4"", ""5""]; each (v in arr) { x += v }", "12345");
			}
		}

		public static void TestLoopLabels() {
			{
				string expected = "Gottem";
				TestRun(@"
var it = ""dont gottem""
var SomeData = { Get: func(a,b) => { return 1 + a * b } };
:outer: 
for (var y = 0; y < 10; y++) {
	for (var x = 0; x < 10; x++) {
		it = ""dont gottem"";
		if (SomeData.Get(x,y) == 50) {
			it = ""Gottem"";
			break :outer:
		}
		it = ""dont gottem"";
	}
}
return it;
", expected);
				TestRun(@"
var it = ""dont gottem""
var SomeData = { Get: func(a,b) => { return 1 + a * b } }; // Very compact, but should still work.
x=0;y=0;
:outer:
while (y < 10) {
	x = 0;
	while (x < 10) {
		it=""dont gottem"";
		if (SomeData.Get(x,y) == 50) { 
			it = ""Gottem""; 
			break :outer: 
		} 
		it = ""dont gottem"";
		x++
	}
	y++
}
return it
", expected);
				// Compact version of previous:
				TestRun(@"
var it = ""dont gottem""
var SomeData = { Get: func(a,b) => { return 1 + a * b } }; // Very compact, but should still work.
x=0;y=0;:outer:while(y<10){x=0;while(x<10){it=""dont gottem"";if(SomeData.Get(x,y)==50){it=""Gottem"";break:outer:}it=""dont gottem"";x++}y++}
return it
", expected);
			}

		}

		public static void TestArgs() {
			{
				TestRun(@"
var fn = func() => { 
	var x = """"; 
	each (arg in args) { 
		x += arg + "" ""; 
	}
}
fn(1, 2, 3, 4, 5);
", "1 2 3 4 5 ");

			}
		}


	}


#endif
}
