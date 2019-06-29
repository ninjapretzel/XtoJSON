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
	public static class XJS_Interp_Tests {

		private static void TestRun(string input, object expected, XJS.Interpreter interp = null) {
			if (interp == null) { interp = new XJS.Interpreter(); }
			var program = XJS.Parse(input);
			var result = interp.Execute(program);

			if (expected != null) {
				result.ShouldEqual(expected);
			}

		}

		private static void DebugRun(string input, object expected, XJS.Interpreter interp = null) {
			if (interp == null) { interp = new XJS.Interpreter(); }
			var program = XJS.Parse(input);
			Debug.Log(program);
			var result = interp.Execute(program);

			if (expected != null) {
				result.ShouldEqual(expected);
			}

		}
		

		public static void TestFrameStackLoops() {
			{
				XJS.Interpreter i = new XJS.Interpreter();
				i.frames.Count.ShouldBe(1);
				i.frame.Count.ShouldBe(0);

				TestRun("", null, i);
			
				i.frames.Count.ShouldBe(1);
				i.frame.Count.ShouldBe(0);

				TestRun("fn = func() => { 5 } x = fn()", 5, i);
				i.frames.Count.ShouldBe(1);
				i.frame.Count.ShouldBe(0);

				TestRun(@"for (var i = 0; i < 5; i++) { x += fn() }", 30, i);
				i.frames.Count.ShouldBe(1);
				i.frame.Count.ShouldBe(0);

				TestRun(@"while (x < 50) { x += fn() } ", 50, i);
				i.frames.Count.ShouldBe(1);
				i.frame.Count.ShouldBe(0);

				TestRun(@"do { x += fn() } while (x < 75)", 75, i);
				i.frames.Count.ShouldBe(1);
				i.frame.Count.ShouldBe(0);
				
				TestRun(@"
arr = [ fn(), fn(), fn(), fn(), fn() ]
each (it in arr) { x += it }
", 100, i);
			}
		}
		public static void TestFrameStackDefs() {
			{
				XJS.Interpreter i = new XJS.Interpreter();
				i.frames.Count.ShouldBe(1);
				i.frame.Count.ShouldBe(0);

				i.frame.Push();
				i.frame.Count.ShouldBe(1);
				i.frame.Declare("x", 1);
				i.frame.Count.ShouldBe(1);
				
				i.frame.Push();
				i.frame.Count.ShouldBe(2);
				i.frame.Declare("x", 2);
				i.frame.Count.ShouldBe(2);
				
				TestRun("x", 2, i);
				i.frame.Pop();
				i.frame.Count.ShouldBe(1);
				TestRun("x", 1, i);
				i.frame.Pop();
				i.frame.Count.ShouldBe(0);


			}
		}
		public static void TestFrameStackPersistence() {
			{
				XJS.Interpreter i = new XJS.Interpreter();
				var x1 = i.frame["x"];
				(x1 == null || x1 == JsonValue.NULL).ShouldBe(true);

				TestRun("var x = 5", 5, i);
				i.frames.Count.ShouldBe(1);
				i.frame.Count.ShouldBe(0);
				var x2 = i.frame["x"];
				(x2 == null || x2 == JsonValue.NULL).ShouldBe(true);
			
				TestRun("x = 5", 5, i);
				var x3 = i.frame["x"];
				x3.ShouldEqual(5);

				TestRun("x", 5, i);
				i.frames.Count.ShouldBe(1);
				i.frame.Count.ShouldBe(0);
			}
		}
		public static void TestValueInjection() {
			{
				XJS.Interpreter i = new XJS.Interpreter();
				TestRun("it", null, i);

				i.frame["it"] = "has a value";
				TestRun("it", "has a value", i);
			}
			{ // Effectively the same as the previous 
				XJS.Interpreter i = new XJS.Interpreter();
				TestRun("it", null, i);

				i.global["it"] = "has a value";
				TestRun("it", "has a value", i);
			}
			{
				XJS.Interpreter i = new XJS.Interpreter();
				TestRun("it", null, i);

				i.global["it"] = "has a value";
				TestRun("it", "has a value", i);
			}
		}


		public static void TestFunctionInsertion() {
			{
				JsonFunction fn = new JsonFunction(() => 5);
				XJS.Interpreter i = new XJS.Interpreter();
				i.global["fn"] = fn;

				TestRun("fn()", 5, i);
			}
			


		}




	}


#endif
}
