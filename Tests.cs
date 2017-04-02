// Unity detection
#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_6
#define UNITY
// Use UnityEngine's provided utilities.
using UnityEngine;

#else
// Hook into some other useful diagnostic stuff
using System.Runtime.CompilerServices;
using System.Diagnostics;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
/// <summary> Class containing a small suite of tests to ensure all functionality is good. </summary>
public static class JsonTests {
	// ~180 lines to get most of Shouldly's functionality.
	#region shouldly-like-extensions
/// <summary> Generates a short informative string about the type and content of an object </summary>
/// <param name="obj"> Object to make info about </param>
/// <returns> Short string with info about the object </returns>
#if !UNITY
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	private static string Info(this object obj) {
		if (obj == null) { obj = ActualNull.instance; }
		return string.Format("({0})({1})", obj.GetType().Name, obj);
	}
	private static string[] sep = new string[] { "\n\r", "\r\n", "\n", "\r" };
	private static string CallInfo(this string stackTrace) {
		string[] lines = stackTrace.Split(sep, StringSplitOptions.None);
		string line = lines[lines.Length - 1];
		string method = line.Substring(6, line.IndexOf('(', 6) - 6);
		string fileAndLineStr = line.Substring(line.LastIndexOf('\\') + 1);
		string[] fileAndLine = fileAndLineStr.Split(':');
		string file = fileAndLine[0];
		string lineNumber = fileAndLine[1];

		return string.Format("{0}, in {1}, {2}", method, file, lineNumber);
	}

	public class ActualNull {
		public static readonly ActualNull instance = new ActualNull();
		private ActualNull() { }
		public override string ToString() { return "null"; }
	}

	private static string Fmt(this string s, params object[] args) { return string.Format(s, args); }
	private static string SHOULD_BE_FAILED = "Values\n\t{0}\nand\n\t{1}\nShould have been ==, but were not.";

	// I really wish this shit could be solved properly with a generic method...
	/// <summary> Asserts two string values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this string v1, string v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two bool values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this bool v1, bool v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two decimal values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this decimal v1, decimal v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two double values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this double v1, double v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two float values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this float v1, float v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two char  values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this char v1, char v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two byte values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this byte v1, byte v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two short values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this short v1, short v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two int values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this int v1, int v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two long values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this long v1, long v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }

	/// <summary> Asserts two sbyte values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this sbyte v1, sbyte v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two ushort values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this ushort v1, ushort v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two uint values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this uint v1, uint v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
	/// <summary> Asserts two ulong values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
	private static void ShouldBe(this ulong v1, ulong v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }

	/// <summary> Tests two objects, and throws an exception if they are not equal by == in one direction (obj == other) </summary>
	/// <param name="obj"> Object to test </param>
	/// <param name="other"> Object to test against </param>
	private static void ShouldBe(this object obj, object other) {
		if (!(obj == other)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(obj.Info(), other.Info())); }
	}

	/// <summary> 
	/// Checks two objects for equality, using a specific == operator, 
	/// defined in the class of <paramref name="T"/>, between Two <paramref name="T"/>s
	/// </summary>
	/// <typeparam name="T"> First type (expected of <paramref name="obj"/>) </typeparam>
	/// <param name="obj"> First object for comparison. Should be of type <paramref name="T"/> </param>
	/// <param name="other"> Second object for comparison. </param>
	private static void ShouldBe<T>(this object obj, object other) {
		Type type = typeof(T);
		if (!(obj is T)) { throw new AssertFailed("ShouldBe<>", "Value\n\t" + obj.Info() + "\nShould be castable to type\n\t" + type.Name + "\nBut is not."); }
		if (!(other is T)) { throw new AssertFailed("ShouldBe<>", "Value\n\t" + other.Info() + "\nShould be castable to type\n\t" + type.Name + "\nBut is not."); }
		Type[] tarr = new Type[] { type, type };
		MethodInfo op_Equality = type.GetMethod("op_Equality", tarr);
		if (op_Equality == null) { throw new AssertFailed("ShouldBe<>", "Type\n\t" + type.Name + "\nDoes not have expected custom operator"); }
		if (op_Equality.ReturnType != typeof(bool)) { throw new AssertFailed("ShouldBe<>", "Type\n\t" + type.Name + "\nHas == operator returning non-bool value."); }
		Type[] argTypes = op_Equality.GetParameters().Select(a => a.ParameterType).ToArray();
		if (argTypes.Length != 2) { throw new AssertFailed("ShouldBe<>", "Type\n\t" + type.Name + "\nHas == operator having more or less tahn 2 parameters!"); }
		T tobj = (T)obj;
		T tother = (T)other;
		object[] args = new object[] { tobj, tother };
		bool result = (bool)op_Equality.Invoke(null, args);
		if (!result) { throw new AssertFailed("ShouldBe<>", SHOULD_BE_FAILED.Fmt(obj.Info(), other.Info())); }
	}

	/// <summary> 
	/// Checks two objects for equality, using a specific == operator, 
	/// defined in the class of <paramref name="T"/>, 
	/// between a <paramref name="T"/> and a <paramref name="T2"/>
	/// </summary>
	/// <typeparam name="T"> First type (expected of <paramref name="obj"/>) </typeparam>
	/// <typeparam name="T2"> Second type (expected of <paramref name="other"/>) </typeparam>
	/// <param name="obj"> First object for comparison. Should be of type <paramref name="T"/> </param>
	/// <param name="other"> Second object for comparison. Should be of type <paramref name="T2"/> </param>
	private static void ShouldBe<T, T2>(this object obj, object other) {
		Type type = typeof(T);
		Type type2 = typeof(T2);
		if (!(obj is T)) { throw new AssertFailed("ShouldBe<,>", "Value\n\t" + obj.Info() + "\nShould have been type\n\t" + type.Name + "\nBut was not."); }
		if (!(other is T2)) { throw new AssertFailed("ShouldBe<,>", "Value\n\t" + other.Info() + "\nShould have been type\n\t" + type2.Name + " \nBut was not."); }
		Type[] tarr = new Type[] { type, type2 };
		bool tfirst = true;
		MethodInfo op_Equality = type.GetMethod("op_Equality", tarr);
		if (op_Equality == null) { tfirst = false; tarr[0] = type2; tarr[1] = type; op_Equality = type.GetMethod("op_Equality", tarr); }

		if (op_Equality == null) { throw new AssertFailed("ShouldBe<,>", "Type\n\t" + type.Name + "\nDoes not have custom == operator defined."); }
		if (op_Equality.ReturnType != typeof(bool)) { throw new AssertFailed("ShouldBe<>", "Type\n\t" + type.Name + "\nHas == operator returning non-bool value."); }

		Type[] argTypes = op_Equality.GetParameters().Select(a => a.ParameterType).ToArray();
		if (argTypes.Length != 2) { throw new AssertFailed("ShouldBe<,>", "Type\n\t" + type.Name + "\nHas == operator having more or less tahn 2 parameters!"); }
		if (argTypes[0] != type && argTypes[1] != type) {
			throw new AssertFailed("ShouldBe<,>", "Type\n\t" + type.Name + "\nHas == operator without its own type as a parameter");
		}
		if (argTypes[0] != type2 && argTypes[1] != type2) {
			throw new AssertFailed("ShouldBe<,>", "Type\n\t" + type.Name + "\nHas == operator without type\n\t" + type2.Name + "\nas a parameter");
		}


		T tobj = (T)obj;
		T2 t2other = (T2)other;
		object[] args = new object[] { (tfirst ? (object)tobj : (object)t2other), (tfirst ? (object)t2other : (object)tobj) };
		bool result = (bool)op_Equality.Invoke(null, args);
		if (!result) { throw new AssertFailed("ShouldBe<,>", SHOULD_BE_FAILED.Fmt(obj.Info(), other.Info())); }
	}


	/// <summary> Tests two things, and throws an exception if they are equal by != in one direction (obj != other) </summary>
	/// <param name="obj"> Object to test </param>
	/// <param name="other"> Object to test against </param>
	private static void ShouldNotBe(this object obj, object other) {
		if (!(obj != other)) { throw new AssertFailed("ShouldNotBe", "Values\n\t" + obj.Info() + "\nand\n\t" + other.Info() + "\nShould have been !=, but were not."); }
	}

	/// <summary> Tests two things, and throws an exception if they are not equal by Equals in one direction (obj.Equals(other)) </summary>
	/// <param name="obj"> Object to test </param>
	/// <param name="other"> Object to test against </param>
	private static void ShouldEqual(this object obj, object other) {
		if (!obj.Equals(other)) { throw new AssertFailed("ShouldEqual", "Values\n\t" + obj.Info() + "\nand\n\t" + other.Info() + "\nShould have been .Equal(), but were not."); }
	}

	/// <summary> Tests two things, and throws an exception if they are not equal by Equals in one direction (!obj.Equals(other)) </summary>
	/// <param name="obj"> Object to test </param>
	/// <param name="other"> Object to test against </param>
	private static void ShouldNotEqual(this object obj, object other) {
		if (obj.Equals(other)) { throw new AssertFailed("ShouldNotEqual", "Values\n\t" + obj.Info() + "\nand\n\t" + other.Info() + "\nShould not have been .Equal(), but were."); }
	}

	/// <summary> Tests a boolean expression for truthiness </summary>
	/// <param name="b"> Expression expected to be true </param>
	private static void ShouldBeTrue(this bool b) {
		if (!b) { throw new AssertFailed("ShouldBeTrue", "Expression should have been true, but was false"); }
	}

	/// <summary> Tests a boolean expression for falsity </summary>
	/// <param name="b"> Expression expected to be false </param>
	private static void ShouldBeFalse(this bool b) {
		if (b) { throw new AssertFailed("ShouldBeFalse", "Expression should have been false, but was true"); }
	}

	/// <summary> Throws an AssertFailed. Marks a line of code as something that should not be reached. </summary>
	private static void ShouldNotRun() {
		throw new AssertFailed("ShouldNotRun", "Line of code invokign this method should not have been reached.");
	}

#endregion
	
	// Too many conditional compiles following...
	// TBD: Find a better way to structure support for multiple platforms
#if DEBUG
	/// <summary> Output stream to write to, if assigned. </summary>
	internal static TextWriter Out = null;
#endif
/// <summary> Debug helper method </summary>
/// <param name="message"> Message object to output to the assigned outstream </param>
#if !UNITY // Note: Unity has control over this symbol, so this function shouldn't be marked Conditional in some cases when unity uses DEBUG
	[Conditional("DEBUG")]
#endif
	public static void Log(object message) {
#if DEBUG
		if (Out != null) {
#if UNITY
			Debug.Log(message);
#else
			Out.WriteLine(message); 
#endif
		}
#endif
	}
	private static List<MethodInfo> GetTestMethods(this Type type) {
		var allMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		var nestedTypes = type.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		List<MethodInfo> tests = new List<MethodInfo>();

		foreach (var method in allMethods) {
			if (method.Name.StartsWith("Test") && method.GetParameters().Count() == 0) { tests.Add(method); }
		}

		foreach (var subtype in nestedTypes) {
			tests.AddRange(subtype.GetTestMethods());
		}

		return tests;
	}

	private static MethodInfo[] GetTestMethods() {
		return (typeof(JsonTests)).GetTestMethods().ToArray();
	}
	

	private class AssertFailed : Exception {
		internal string type = null;
		internal string description = "No Description Given";
		public AssertFailed() { }
		public AssertFailed(string desc) { description = desc; }
		public AssertFailed(string type, string desc) {
			this.type = type;
			description = desc;
		}
	}


	/// <summary> Runs all of the tests, and returns a string containing information about tests passing/failing. </summary>
	/// <returns> A log of information about the results of the tests. </returns>
	public static string RunTests() {
		var tests = GetTestMethods();
		var empty = new object[0];
		MemoryStream logStream = new MemoryStream();
		Encoding encoding = Encoding.ASCII;
		TextWriter logWriter = new StreamWriter(logStream, encoding);
		
		Out = logWriter;
		Log("Testing Log Follows:");

		foreach (var test in tests) {
			Log("Running (" + test.Name + ")");
			
			try {
				test.Invoke(null, empty);
				Log("\tSuccess!");
				
			} catch (TargetInvocationException e) {
				if (e.InnerException != null) {
					Exception ex = e.InnerException;
					if (ex is AssertFailed) {
						AssertFailed fail = ex as AssertFailed;
						string type = fail.type;
						if (type == null) { type = "Assertion"; }
						Log("\tFailure, " + type + " Failed:\n" + fail.description);
					} else {
						Log("\tFailure, Exception Generated: " + ex.GetType().Name);

					}
					Log("\tLocation: " + ex.StackTrace.CallInfo());
					
				}
			} catch (Exception e) {
				Log("Unexpected Exception:\n\t" + e.GetType().Name);
				Log("\tFull Trace: " + e.StackTrace);
			}
			Log("\n");
		}

		logWriter.Flush();
		Out = null;
		return encoding.GetString(logStream.ToArray());
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////
	// Test Code
	/// <summary> Test holding JsonObject test functions </summary>
	
	public class TestJsonObject {
		public static void TestObjectAdd() {
			{
				JsonObject obj = new JsonObject();

				obj.Count.ShouldBe(0);

				obj.Add("what", "huh")
					.Add("okay", "alright");

				obj.Count.ShouldBe(2);
			}
		}
		public static void TestObjectVectOps() {
			{
				JsonObject v1 = new JsonObject("x", 5, "y", 3, "z", 2);
				JsonObject v2 = new JsonObject("x", 3, "y", 1, "z", 4);

				var v3 = v1.Multiply(v2);
				v3["x"].numVal.ShouldBe(15);
				v3["y"].numVal.ShouldBe(3);
				v3["z"].numVal.ShouldBe(8);

				var v4 = v1.AddNumbers(v2);
				v4["x"].numVal.ShouldBe(8);
				v4["y"].numVal.ShouldBe(4);
				v4["z"].numVal.ShouldBe(6);
			}

			{
				JsonObject matrix = new JsonObject()
					.Add("maxHP", new JsonObject("str", 2, "vit", 5))
					.Add("maxMP", new JsonObject("int", 2, "wis", 2));

				JsonObject stats = new JsonObject("str", 10, "dex", 10, "vit", 10, "int", 10, "wis", 10);

				var result = stats.Multiply(matrix);

				result["maxHP"].numVal.ShouldBe(70);
				result["maxMP"].numVal.ShouldBe(40);
			}
		}
		public static void TestObjectEqual() {

			{
				JsonObject a = new JsonObject("x", 3);
				JsonObject b = new JsonObject("x", 3);
				JsonObject c = new JsonObject("x", 2);

				a.Equals(b).ShouldBeTrue();
				b.Equals(a).ShouldBeTrue();

				a.Equals(c).ShouldBeFalse();
			}

			{
				JsonObject a = new JsonObject("x", null, "y", false, "z", true);
				JsonObject b = new JsonObject()
					.Add("x", null)
					.Add("y", false)
					.Add("z", true);
				JsonObject c = new JsonObject("x", "something", "y", true, "z", false);


				a.Equals(b).ShouldBeTrue();
				b.Equals(a).ShouldBeTrue();

				a.Equals(c).ShouldBeFalse();
			}

			{
				JsonObject a = new JsonObject("x", 5, "y", 12, "z", 15, "tag", "blah")
					.Add("nested", new JsonObject("x", 3, "nestedNested", new JsonObject()))
					.Add("array", new JsonArray("a", "b", "c", 1, 2, 3))
					.Add("emptyObject", new JsonObject())
					.Add("emptyArray", new JsonArray());

				JsonObject b = new JsonObject("x", 5, "y", 12, "z", 15, "tag", "blah")
					.Add("emptyObject", new JsonObject())
					.Add("array", new JsonArray("a", "b", "c", 1, 2, 3))
					.Add("emptyArray", new JsonArray())
					.Add("nested", new JsonObject("x", 3, "nestedNested", new JsonObject()));

				JsonObject c = new JsonObject("x", 5, "y", 12, "z", 15, "tag", "blah")
					.Add("emptyObject", new JsonObject())
					.Add("array", new JsonArray("a", "b", "c", 1, 2, 3))
					.Add("emptyArray", new JsonArray())
					.Add("nested", new JsonObject("x", 3, "nestedNested", new JsonObject("x", 5)));

				a.Equals(b).ShouldBeTrue();
				b.Equals(a).ShouldBeTrue();

				a.Equals(c).ShouldBeFalse();
			}

			{
				JsonObject a = new JsonObject()
					.Add("name", "bob saget")
					.Add("paperTowels", 50)
					.Add("hasBalls", true);

				JsonObject b = new JsonObject()
					.Add("name", "bob saget")
					.Add("paperTowels", 50)
					.Add("hasBalls", true);

				JsonObject c = new JsonObject()
					.Add("name", "bobby bob bobberton")
					.Add("paperTowels", "three hundred")
					.Add("hasBalls", "yes");

				a.ShouldEqual(b);
				a.ShouldNotBe(b);
				a.ShouldNotBe(c);
				a.ShouldNotEqual(c);

				a.Add("son", c);
				b.Add("son", c.Clone());

				a.ShouldEqual(b);
				a.ShouldNotBe(b);

				a["son"].ShouldBe(c);
				a["son"].ShouldEqual(c);

				b["son"].ShouldNotBe(c);
				b["son"].ShouldEqual(c);
			}
			5.ShouldBe(12);
		}
		public static void TestObjectIndex() {
			{
				Dictionary<string, float> data = new Dictionary<string, float>() {
				{"str", 5},
				{"dex", 12},
				{"vit", 8},
			};
				JsonObject obj = new JsonObject();
				foreach (var pair in data) { obj[pair.Key] = pair.Value; }

				obj.Count.ShouldBe(3);
				obj["str"].numVal.ShouldBe(5);
				obj["vit"].numVal.ShouldBe(8);
			}
		}
		public static void TestObjectSet() {
			{
				JsonObject a = new JsonObject("x", 1, "y", 2, "z", 3);
				JsonObject b = new JsonObject("x", 4, "y", 5, "z", 6);

				a.Set(b);

				a["x"].numVal.ShouldBe(4);
				a["y"].numVal.ShouldBe(5);
				a["z"].numVal.ShouldBe(6);
			}

			{
				JsonObject a = new JsonObject()
					.Add("nested", new JsonObject("a", 1, "b", 2, "c", 3));

				JsonObject b = new JsonObject()
					.Add("nested", new JsonObject("x", 1, "y", 2, "z", 3, "c", 621));

				a.Set(b);

				a["nested"].Count.ShouldBe(4);
				a["nested"].ContainsKey("a").ShouldBeFalse();
				a["nested"].ContainsKey("x").ShouldBeTrue();
			}

			{
				JsonObject a = new JsonObject()
					.Add("nested", new JsonObject("a", 1, "b", 2, "c", 3));

				JsonObject b = new JsonObject()
					.Add("nested", new JsonObject("x", 1, "y", 2, "z", 3, "c", 621));

				a.SetRecursively(b);

				a["nested"].Count.ShouldBe(6);
				a["nested"].ContainsKey("a").ShouldBeTrue();
				a["nested"].ContainsKey("x").ShouldBeTrue();

			}

		}
		public static void TestObjectPrintParse() {
			{
				JsonObject obj = new JsonObject();

				string str = obj.ToString();
				str.ShouldBe("{}");

				string pp = obj.PrettyPrint();
				pp.ShouldBe<string>("{\n}");

				JsonObject strParse = Json.Parse(str) as JsonObject;
				JsonObject ppParse = Json.Parse(pp) as JsonObject;

				true.ShouldBe(obj.Equals(strParse));
				true.ShouldBe(obj.Equals(ppParse));

			}

			{
				JsonObject obj = new JsonObject("x", 5, "y", 20, "str", "someString", "z", false);

				string str = obj.ToString();
				string expectedToString = "{'x':5,'y':20,'str':'someString','z':false}".Replace('\'', '\"');
				str.ShouldBe(expectedToString);

				string pp = obj.PrettyPrint();
				string expectedPrettyPrint = @"{
	'x':5,
	'y':20,
	'str':'someString',
	'z':false
}".Replace('\'', '\"');

				pp.ShouldBe<string>(expectedPrettyPrint);

				JsonObject strParse = Json.Parse(str) as JsonObject;
				JsonObject ppParse = Json.Parse(pp) as JsonObject;

				true.ShouldBe(obj.Equals(strParse));
				true.ShouldBe(obj.Equals(ppParse));
			}

			{
				JsonObject obj = new JsonObject();
				obj["x"] = new JsonObject();
				obj["x"]["y"] = new JsonObject();
				obj["x"]["y"]["z"] = new JsonObject();

				string str = obj.ToString();
				string expectedToString = "{'x':{'y':{'z':{}}}}".Replace('\'', '\"');
				str.ShouldBe<string>(expectedToString);

				string pp = obj.PrettyPrint();
				string expectedPrettyPrint = @"{
	'x':
	{
		'y':
		{
			'z':
			{
			}
		}
	}
}".Replace('\'', '\"');
				pp.ShouldBe<string>(expectedPrettyPrint);

				JsonObject strParse = Json.Parse(str) as JsonObject;
				JsonObject ppParse = Json.Parse(pp) as JsonObject;

				true.ShouldBe(obj.Equals(strParse));
				true.ShouldBe(obj.Equals(ppParse));

			}

		}
	}
	/// <summary> Test holding JsonArray test functions </summary>
	public static class TestJsonArray {
		public static void TestArrayGeneral() {
			{
				JsonArray x = new JsonArray();
				x.Count.ShouldBe(0);

				x[0] = "Test1";
				x["1"] = "test2";
				x[2.0] = "test3";

				x.Count.ShouldBe(3);
				x[1].stringVal.ShouldBe<string>("test2");
				x["2"].stringVal.ShouldBe<string>("test3");

			}

			{
				JsonArray x = new object[] { 1, 2, 3 };
				JsonArray y = new JsonArray() { 1, 2, 3 };
				JsonArray z = new int[] { 1, 2, 3 };

				x.Count.ShouldBe(y.Count);
				x.Equals(y).ShouldBeTrue();
				z.Equals(z).ShouldBeTrue();
				y.Equals(z).ShouldBeTrue();
			}

		}
		public static void TestArrayAdd() {
			{
				JsonArray x = new JsonArray().Add(1).Add(2).Add(3);
				x.Count.ShouldBe(3);
			}

			{
				JsonArray x = new JsonArray().Add(1).Add(2).Add(3);
				JsonArray y = new JsonArray().Add(x);

				y.Count.ShouldBe(1);
				y[0].Count.ShouldBe(3);
			}

			{
				int[] nums = { 1, 2, 3 };
				JsonArray x = new JsonArray().Add((JsonArray)nums);
				x.Count.ShouldBe(1);
				x[0].Count.ShouldBe(3);
			}

		}
		public static void TestArrayAddAll() {
			{
				JsonArray x = new JsonArray();
				JsonArray y = new JsonArray();
				x.AddAll(y);
				x.Count.ShouldBe(0);
			}

			{
				JsonArray x = new JsonArray();
				JsonArray y = new JsonArray() { 1, 2, 3 };

				x.AddAll(y);
				x.Count.ShouldBe(3);
			}

			{
				JsonArray x = new JsonArray();
				int[] y = { 1, 2, 3 };

				x.AddAll(y);

			}
		}
		public static void TestArrayPrintParse() {
			{
				JsonArray arr = new JsonArray();
				string str = arr.ToString();
				string pp = arr.PrettyPrint();

				str.ShouldBe<string>("[]");
				pp.ShouldBe<string>("[\n]");

				JsonArray strParse = Json.Parse(str) as JsonArray;
				JsonArray ppParse = Json.Parse(pp) as JsonArray;

				true.ShouldBe(arr.Equals(strParse));
				true.ShouldBe(arr.Equals(ppParse));

			}

			{
				JsonArray arr = new JsonArray(1, 2, 3, 4, 5, 6);

				string str = arr.ToString();
				string pp = arr.PrettyPrint();

				string strExpected = "[1,2,3,4,5,6]";
				string ppExpected = @"[
	1,
	2,
	3,
	4,
	5,
	6
]".Replace('\'', '\"');

				str.ShouldBe<string>(strExpected);
				pp.ShouldBe<string>(ppExpected);

				JsonArray strParse = Json.Parse(str) as JsonArray;
				JsonArray ppParse = Json.Parse(pp) as JsonArray;

				true.ShouldBe(arr.Equals(strParse));
				true.ShouldBe(arr.Equals(ppParse));

			}

			{
				JsonArray arr = new JsonArray();
				arr.Add(new JsonArray());
				arr.Add(new JsonArray().Add(new JsonArray()));
				arr.Add(new JsonArray().Add(new JsonArray().Add(new JsonArray())));

				string str = arr.ToString();
				string pp = arr.PrettyPrint();

				string strExpected = "[[],[[]],[[[]]]]";
				string ppExpected = @"[
	[
	],
	[
		[
		]
	],
	[
		[
			[
			]
		]
	]
]";
				str.ShouldBe<string>(strExpected);
				pp.ShouldBe<string>(ppExpected);

				JsonArray strParse = Json.Parse(str) as JsonArray;
				JsonArray ppParse = Json.Parse(pp) as JsonArray;

				true.ShouldBe(arr.Equals(strParse));
				true.ShouldBe(arr.Equals(ppParse));

			}
		}
		public static void TestNestedPrintParse() {
			JsonObject obj = new JsonObject(
				"value", 20,
				"level", 5,
				"name", "Sword of Boom",
				"desc", "It goes boom.",
				"damage", new JsonArray(new JsonObject("power", 25, "type", "fire"), new JsonObject("power", 10, "type", "elec")),
				"proc", new JsonObject(
					"chance", .1,
					"scripts", new JsonArray("explode", new JsonObject("name", "stun", "chance", .35, "duration", 3))
				)

			);

			string str = obj.ToString();
			string pp = obj.PrettyPrint();

			string strExpected = "{'value':20,'level':5,'name':'Sword of Boom','desc':'It goes boom.','damage':[{'power':25,'type':'fire'},{'power':10,'type':'elec'}],'proc':{'chance':0.1,'scripts':['explode',{'name':'stun','chance':0.35,'duration':3}]}}".Replace('\'', '\"');
			string ppExpected = @"{
	'value':20,
	'level':5,
	'name':'Sword of Boom',
	'desc':'It goes boom.',
	'damage':
	[
		{
			'power':25,
			'type':'fire'
		},
		{
			'power':10,
			'type':'elec'
		}
	],
	'proc':
	{
		'chance':0.1,
		'scripts':
		[
			'explode',
			{
				'name':'stun',
				'chance':0.35,
				'duration':3
			}
		]
	}
}".Replace('\'', '\"');

			str.ShouldBe<string>(strExpected);
			//Debug.Log(pp);
			//Debug.Log(ppExpected);
			pp.ShouldBe<string>(ppExpected);

			JsonObject strParse = Json.Parse(str) as JsonObject;
			JsonObject ppParse = Json.Parse(pp) as JsonObject;

			true.ShouldBe(obj.Equals(strParse));
			true.ShouldBe(obj.Equals(ppParse));
		}
		public static void TestListReflecting() {
			{
				List<string> list = new List<string>();
				for (int i = 0; i < 10; i++) { list.Add("" + (char)('a' + i)); }

				var reflect = Json.Reflect(list);
				//Debug.Log(reflect);

				true.ShouldBe(reflect.isArray);

				List<string> reflectBack = Json.GetValue<List<string>>(reflect);

				10.ShouldBe(reflectBack.Count);
				for (int i = 0; i < 10; i++) {
					string expect = "" + (char)('a' + i);
					reflectBack[i].ShouldBe<string>(expect);
				}


			}

		}
		public static void TestEquality() {
			{
				JsonArray a = new JsonArray().Add("Heeeello").Add("nurse").Add(42);
				JsonArray b = new JsonArray().Add("Heeeello").Add("nurse").Add(42);
				JsonArray c = new JsonArray().Add("yes").Add("no").Add("maybe").Add("could you repeat the question?");

				a.ShouldEqual(b);
				a.ShouldNotBe(b);
				a.ShouldNotBe(c);
				a.ShouldNotEqual(c);

			}
		}
	}
	/// <summary> Test holding General JsonValue test functions </summary>
	public static class TestGeneral {
		public static void TestEscapes() {
			{
				JsonObject obj = new JsonObject();

				string key = "scv:\"wark\"";
				string val = "balls:\"borf\"";
				obj[key] = val;
				1.ShouldBe(obj.Count);
				true.ShouldBe(obj.ContainsKey(key));
				true.ShouldBe(obj[key] == val);

				string str = obj.ToString();
				string pp = obj.PrettyPrint();

				JsonObject strParse = Json.Parse(str) as JsonObject;
				JsonObject ppParse = Json.Parse(pp) as JsonObject;
				true.ShouldBe(obj.Equals(strParse));
				true.ShouldBe(obj.Equals(ppParse));

			}
		}
		public static void TestBoolConversion() {
			{ // JsonNull should always be a false
				if (JsonNull.instance) { ShouldNotRun(); }
				JsonValue empty = null;
				if (empty) { ShouldNotRun(); }
			}

			{ // JsonBools should behave directly
				JsonBool yes = true;
				JsonBool no = false;
				if (yes) { /* good */ } else { ShouldNotRun(); }
				if (!yes) { ShouldNotRun(); }

				if (no) { ShouldNotRun(); }
				if (!no) { /* good */ } else { ShouldNotRun(); }
			}

			{ // JsonNumber are only false when 0
				JsonNumber zero = 0;
				JsonNumber five = 5;
				var zeroCon = five - five;

				if (zero) { ShouldNotRun(); }
				if (five) { /* good */ } else { ShouldNotRun(); }
				if (zeroCon) { ShouldNotRun(); }
			}

			{ // JsonString are only false when empty
				JsonString empty = "";
				JsonString other = "other";
				if (empty) { ShouldNotRun(); }
				if (other) { /* good */ } else { ShouldNotRun(); }
			}
			{ // JsonObject are always true if not null
				JsonObject empty = new JsonObject();
				JsonObject obj = new JsonObject().Add("ayy","lmao");
				if (empty) { /* good */ } else { ShouldNotRun(); }
				if (obj) { /* good */ } else { ShouldNotRun(); }
			} 
			{ // JsonArray are always true if not null
				JsonArray empty = new JsonArray();
				JsonArray arr = new JsonArray().Add("ayy").Add("lmao");
				if (empty) { /* good */ } else { ShouldNotRun(); }
				if (arr) { /* good */ } else { ShouldNotRun(); }
			}
		}
		public static void TestNumberConversion() {
			{
				JsonString five = "5";
				float fiveF = five;		fiveF.ShouldBe(5);
				double fiveD = five;	fiveD.ShouldBe(5);
				int fiveI = five;		fiveI.ShouldBe(5);
				long fiveL = five;		fiveL.ShouldBe(5);
				decimal fiveDC = five;	fiveDC.ShouldBe(5);
			}
			{
				JsonBool yes = true;
				float yesF = yes;		yesF.ShouldBe(1);
				double yesD = yes;		yesD.ShouldBe(1);
				int yesI = yes;			yesI.ShouldBe(1);
				long yesL = yes;		yesL.ShouldBe(1);
				decimal yesDC = yes;	yesDC.ShouldBe(1);

				JsonBool no = false;
				float noF = no;			noF.ShouldBe(0);
				double noD = no;		noD.ShouldBe(0);
				int noI = no;			noI.ShouldBe(0);
				long noL = no;			noL.ShouldBe(0);
				decimal noDC = no;		noDC.ShouldBe(0);
			}
		}
		public static void TestEqualities() {
		{ // JsonNull
			(JsonNull.instance == null).ShouldBeTrue();
			JsonNull.instance.ShouldEqual(null);

		}

		{ // JsonNumber
			JsonNumber a = 5;
			JsonNumber b = 5;
			JsonNumber c = 10;

			(a == b).ShouldBeTrue();
			(a == 5).ShouldBeTrue();
			(a != c).ShouldBeTrue();
			(c == 10).ShouldBeTrue();

			a.ShouldEqual(b);
			a.ShouldEqual(5);
			a.ShouldNotEqual(c);
			c.ShouldEqual(10);

		}

		{ // Infinity and NaN
			JsonValue jminf = double.NegativeInfinity;
			JsonValue jpinf = double.PositiveInfinity;
			JsonValue jnan = double.NaN;

			double minf = double.NegativeInfinity;
			double pinf = double.PositiveInfinity;
			double nan = double.NaN;

			(jpinf == pinf).ShouldBeTrue();
			(jminf == minf).ShouldBeTrue();
			(jnan == nan).ShouldBeTrue();

			jpinf.ShouldEqual(pinf);
			jminf.ShouldEqual(minf);
			jnan.ShouldEqual(nan);

			jpinf.ShouldNotBe(jminf);
			jpinf.ShouldNotBe(minf);
			jpinf.ShouldNotBe(jnan);
			jpinf.ShouldNotBe(nan);

			jminf.ShouldNotBe(jpinf);
			jminf.ShouldNotBe(pinf);
			jminf.ShouldNotBe(jnan);
			jminf.ShouldNotBe(nan);

			jnan.ShouldNotBe(jpinf);
			jnan.ShouldNotBe(pinf);
			jnan.ShouldNotBe(jminf);
			jnan.ShouldNotBe(minf);
		}

		{ // JsonStrings
			JsonString a = "hullo";
			JsonString b = "hullo";
			JsonString c = "bob saget";

			(a == b).ShouldBeTrue();
			(a == "hullo").ShouldBeTrue();
			(a != c).ShouldBeTrue();
			(c == "bob saget").ShouldBeTrue();

			a.ShouldEqual(b);
			a.ShouldEqual("hullo");
			a.ShouldNotEqual(c);
			c.ShouldEqual("bob saget");
		}

		{ // JsonBool
			JsonBool a = true;
			JsonBool b = true;
			JsonBool c = false;

			ShouldBeTrue(a);
			(a != c).ShouldBeTrue();
			ShouldBeFalse(c);
			
		}

		{ // JsonObject
			JsonObject a = new JsonObject()
				.Add("name", "bob saget")
				.Add("paperTowels", 50)
				.Add("hasBalls", true);

			JsonObject b = new JsonObject()
				.Add("name", "bob saget")
				.Add("paperTowels", 50)
				.Add("hasBalls", true);

			JsonObject c = new JsonObject()
				.Add("name", "bobby bob bobberton")
				.Add("paperTowels", "three hundred")
				.Add("hasBalls", "yes");

			a.ShouldEqual(b);
			a.ShouldNotBe(b);
			a.ShouldNotBe(c);
			a.ShouldNotEqual(c);

			a.Add("son", c);
			b.Add("son", c.Clone());

			a.ShouldEqual(b);
			a.ShouldNotBe(b);

			a["son"].ShouldBe(c);
			a["son"].ShouldEqual(c);

			b["son"].ShouldNotBe(c);
			b["son"].ShouldEqual(c);
		}

		

	}
	}
	////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////
	
}