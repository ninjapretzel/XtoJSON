using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static JsonTests.TestFramework;

namespace JsonTests {

#if DEBUG
	public static class XJS_Interp_Tests {

		private static void TestRun(string input, object expected, XJS.Interpreter.ExecutionContext context = null) {
			if (context == null) { context = new XJS.Interpreter().NewContext(); }
			var program = XJS.Parse(input);
			var result = context.Execute(program);

			if (expected != null) {
				result.ShouldEqual(expected);
			}

		}

		private static void DebugRun(string input, object expected, XJS.Interpreter.ExecutionContext context = null) {
			if (context == null) { context = new XJS.Interpreter().NewContext(); }
			var program = XJS.Parse(input);
			XJS.Debug.Log(program);
			var result = context.Execute(program);

			if (expected != null) {
				result.ShouldEqual(expected);
			}

		}
		

		public static void TestFrameStackLoops() {
			{
				XJS.Interpreter.ExecutionContext i = new XJS.Interpreter().NewContext();
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
				XJS.Interpreter.ExecutionContext i = new XJS.Interpreter().NewContext();
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
				XJS.Interpreter.ExecutionContext i = new XJS.Interpreter().NewContext();
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
				XJS.Interpreter.ExecutionContext i = new XJS.Interpreter().NewContext();
				TestRun("it", null, i);

				i.frame["it"] = "has a value";
				TestRun("it", "has a value", i);
			}
			{ // Effectively the same as the previous 
				XJS.Interpreter.ExecutionContext i = new XJS.Interpreter().NewContext();
				TestRun("it", null, i);

				i.global["it"] = "has a value";
				TestRun("it", "has a value", i);
			}
			{
				XJS.Interpreter.ExecutionContext i = new XJS.Interpreter().NewContext();
				TestRun("it", null, i);

				i.global["it"] = "has a value";
				TestRun("it", "has a value", i);
			}
		}


		public static void TestFunctionInsertion() {
			{
				JsonFunction fn = new JsonFunction(() => 5);
				XJS.Interpreter.ExecutionContext i = new XJS.Interpreter().NewContext();
				i.global["fn"] = fn;

				TestRun("fn()", 5, i);
			}
			


		}


		internal class __TestMethods_AsyncRunning {
			public static JsonValue Delay(int ms) {
				JsonPromise p = new JsonPromise();
				Task.Run(async () => {
					await Task.Delay(ms);
					p.SetValue(true);
				});
				return p;
			}
			public static int field = 0;
			public static void Reset() { field = 0; }
			public static void Increment() { Interlocked.Increment(ref field); }
		}
		public static void TestAsyncRunning() {
			{
				XJS.Interpreter interp = new XJS.Interpreter();
				interp.LoadMethods(null, typeof(__TestMethods_AsyncRunning));
				string type = nameof(__TestMethods_AsyncRunning);
				string delay = nameof(__TestMethods_AsyncRunning.Delay);
				string increment = nameof(__TestMethods_AsyncRunning.Increment);
				interp.global[delay] = interp.global[type][delay];
				interp.global[increment] = interp.global[type][increment];
				int repsEach = 10;
				int initialIncrement = 2;

				var program = XJS.Parse($@"
Increment();
Increment();
for (var i = 0; i < {repsEach}; i++) {{ Delay(1); Increment(); }} 
");
				void reset() { __TestMethods_AsyncRunning.Reset(); }
				reset();
				int fieldVal() { return __TestMethods_AsyncRunning.field; }

				{
					List<IEnumerator<JsonValue>> steppers = new List<IEnumerator<JsonValue>>();
					int n = 10;
					int k = 0;
					
					// Initialize program
					for (int i = 0; i < n; i++) { steppers.Add(interp.Async(program)); }
					// Program should not run until `MoveNext` is called.
					fieldVal().ShouldBe(k*n);
					

					for (int i = 0; i < n; i++) { Thread.Sleep(2); steppers[i].MoveNext(); }
					// Increment should be called twice per program at the begninning, before the first `Delay`. 
					k+=initialIncrement;
					fieldVal().ShouldBe(k*n);
					

					for (int j = 0; j < 4; j++) {
						for (int i = 0; i < n; i++) { Thread.Sleep(2); steppers[i].MoveNext(); }
						// Increment should be called once per program between each `Delay`. 
						k+=1;
						fieldVal().ShouldBe(k*n);
					}
					
					// Run the rest...
					for (int i = 0; i < n; i++) { while (steppers[i].MoveNext()) { Thread.Sleep(2); } }
					fieldVal().ShouldBe((initialIncrement + repsEach) * n); 
					
				}

				
			}
		}

	}


#endif
}
