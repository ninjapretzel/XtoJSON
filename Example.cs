using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

#if DEBUG

#if UNITY_EDITOR 
//Example if you are running this inside of UNITY.
using UnityEngine;

public class Example : MonoBehaviour {
	void Start() {
		JsonExample.ObjectExample();
		JsonExample.StructExample();
		JsonExample.ReflectionExample_Foobar();
		JsonExample.ReflectionExample_NestedList();
		
		JsonExample.FromReadme();

	}


}

#endif

public class JsonExample {
	#if !UNITY_2019 && !UNITY_2018
	public struct Vector3 {
		public float x,y,z;
		public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
		public override string ToString() {
			return $"({x}, {y}, {z})";
		}
	}
	#endif

	public static void FromReadme() {
		{
			Vector3 v = Json.To<Vector3>("{x:5,y:3,z:1}");
			Print(v);// (5.0, 3.0, 1.0)
		}
		{
			Vector3 v = new Vector3(1, 2, 3);
			string json = Json.ToJson(v); 
			Print(json); // {"x":1,"y":2,"z":3}

		}
		{
			// Multiple ways,
			// Direct parse and interpret with as or cast...
			JsonObject obj1 = Json.Parse("{x:5,y:3,z:1}") as JsonObject;
			JsonObject obj2 = (JsonObject)Json.Parse("{x:5,y:3,z:1}");
			// Generic parse
			JsonObject obj3 = Json.Parse<JsonObject>("{x:5,y:3,z:1}");
		}
		{
			Vector3 someObject = new Vector3(123,456,789);
			JsonObject obj = Json.Reflect(someObject) as JsonObject;
		}

		{
			JsonObject obj = new JsonObject(); // Empty Object
			obj["key"] = "value"; // Can use indexing syntax as dictionary
			obj["ayy"] = "lmao";
			foreach (var pair in obj) { /* print($"{pair.Key}: {pair.Value}"); */} // Can iterate as dictionary 
			JsonObject obj2 = new JsonObject("key", "value", "ayy", "lmao"); // can construct via params[]}

		}
	}

	//Conditional stuff to support for examples in unity.
#if UNITY_EDITOR
	public static void Print(object o) { UnityEngine.Debug.Log(o.ToString()); }
	public static void Print(string s) { UnityEngine.Debug.Log(s); }
#elif UNITY_STANDALONE
	public class Debug {
		public static void Log(string s) { }
		public static void Log(object o) { }
	}
	public static void Print(object o) {}
	public static void Print(string s) {}
#else
	public static void Print(object o) { Console.WriteLine(o.ToString()); }
	public static void Print(string s) { Console.WriteLine(s); }
#endif

#region Example Stuffs

	public class Skree {
		public Skruck bob;
		public Skruck joe;
		
		public Skree() {
			bob = new Skruck(1, 1, 1, 1);
			joe = new Skruck(2, 3, 4, 5);
		}
		public override string ToString() { return "bob: " + bob + " | joe: " + joe; }
	}
	
	public struct Skruck {
		public float r;
		public float g;
		public float b;
		public float a;
		public Skruck(float x, float y, float z, float w) { r = x; g = y; b = z; a = w; }
		public override string ToString() { return "[" + r + ", " + g + ", " + b + ", " + a + "]"; }
	}
	
	//internal test class to serialize
	public class Foobar {
		//Blacklist of stuff not to reflect/restore
		public static string[] _blacklist = new string[] { "black", "listed", "blacklisted" };

		//Fields of primitive types are serialized using the proper primitive type.
		public string name = "bob";
		public float x = 0;
		public float y = 0;
		public float z = 0;
		public double dollars = 0;
		public bool dead = false;

		//blacklisted fields. contained in the _blacklist.
		public string black = "blue";
		public string listed = "nope";

		//null references are serialized with a 'JsonNull' value type.
		//Objects are serialized recursively, so any object 'nested'
			//in a type is serialized as well.
		public System.Object obj = null;
		public Foobarbix nested = null;
		//Anything that can be indexed by strings or ints is also serialized.
		//Things indexed with strings are stored as objects.
		//Otherwise, they're stored as arrays.
		public Dictionary<string, float> numbers;
		public string[] names = new string[] { "bobby", "hank", "peggy" };
		
		//Properties with only a 'get' are ignored.
		//Properties with only a 'set' are ignored as well.
		//Properties require both a 'get' and 'set' to be serialized
		public float fucksGiven { get { return 0; } }
		public float incomeDollahs { set { dollars += value; } }
		private bool _mlgprostatus;
		public bool mlgprostatus { 
			get { return _mlgprostatus; }
			set { _mlgprostatus = value; }
		}

		//blacklisted property. satisfies get/set requirements, but is in the _blacklist.
		public string blacklisted {
			get { return black + listed; }
			set { 
				int len = value.Length;
				black = value.Substring(0, len/2);
				listed = value.Substring(len/2);
			}
		}
		
		public Foobar() {
			numbers = new Dictionary<string, float>();
			nested = new Foobarbix();
			nested.bollucks = 5;
		}
		public Foobar(string n, float xx, float yy, float zz, double bucks, bool ded) {
			numbers = new Dictionary<string, float>();
			numbers["gp"] = 30;
			name = n;
			x = xx;
			y = yy;
			z = zz;
			dollars = bucks;
			dead = ded;
			nested = new Foobarbix();
			nested.bollucks = 3;
		}
		
		//Massive messy function for testing purposes.
		public override string ToString() {
			string str = "name:  ";
			str += mlgprostatus ? "xXx_313337_" : "";
			str += name;
			str += mlgprostatus ? "_MLG_PRO_xXx" : "";

			str += "\n(" + x + ", " + y + ", " + z + ")";
			str += "\n$" + dollars;
			str += "\n" + (dead ? "DED" : "NOT DED");
			if (obj == null) { 
				str += "\nobj: NULL";		
			} else {
				str += "\nobj: " + obj.ToString();
			}
			
			if (nested == null) {
				str += "\nnested: NULL";
			} else { 
				str += "\nnested: {\n" + nested.ToString() + " }";
			}

			str += "\nBlacklisted:" + blacklisted;
			str += "\nblack:" + black;
			str += "\nlisted:" + listed;
			
			if (numbers == null) {
				str += "\nNumbers: NULL";
			} else {
				str += "\nNumbers: {";
				foreach (var pair in numbers) {
					str += "\n\t" + pair.Key + ":" + pair.Value;
				}
				str += "\n}";
			}
			
			if (names == null) {
				str += "\nNames: NULL";
			} else {
				str += "\nNames: {";
				foreach (string s in names) { str += "\n\t" + s; }
				str += "\n}";
			}
			
			return str;
		}
		
	}
	//Another class that has references inside 'Foobar'
	public class Foobarbix {
		public float bollucks;
		public string blah { get { return "HULLO"; } }
		public override string ToString() {
			return "blah:" + blah + "\nbollucks: " + bollucks + "\n";
		}
	}
	
	//A 'linked list' class.
	public class NestedList {
		public string val;
		public NestedList next;
		public NestedList() { val = ""; next = null; }
		public NestedList(string str) { val = str; next = null; }
		
		public override string ToString() {
			if (next == null) { return val; }
			return val + "->" + next.ToString();
		}
	}
	
	public static void ObjectExample() {
		Print("Arbitrary Object Example");
		//This test constructs an arbitrary JsonObject
		//Then generates Json from the object, and reconstructs it.
		//This is the basic way of using these object to construct Json text 
			//if no model class exists to reflect from
		
		//JsonObjects are containers that map strings to JsonValues
		//These mapped JsonValues can be primitives (strings, doubles, ints, bools)
		//as well as other JsonObjects, or JsonArrays.
		JsonObject obj = new JsonObject();
		obj["name"] = "Ken Sama";
		obj["class"] = "Samurai";
		obj["str"] = 10;
		obj["vit"] = 10;
		obj["dex"] = 5;
		obj["agi"] = 5;
		
		//There are a few ways to get number values out of a JsonObject.
		//One is to use the numVal property, which will break if that value is not of that type (number)
		//Another is to use GetFloat() (or GetNumber(), GetInt(), etc methods)
		//which will return 0 if the key does not exist, or the value is not of that type.
		obj["hp"] = 50 + obj["str"].numVal * 2 + obj["vit"].numVal * 10;
		obj["atk"] = 20 + obj.GetFloat("str") * 2 + obj.GetFloat("dex");
		obj["aspd"] = 100 + obj.GetNumber("agi") * 5 + obj.GetNumber("dex"); 
		
		//Arrays also can be created and filled via code.
		JsonArray inv = new JsonArray();
		obj["inventory"] = inv;
		
		//Arrays can store JsonValue types.
		//Arrays are indexed like normal arrays, with integers starting from 0
		//These can be any valid JsonValue, be it primitives, JsonObjects, or JsonArrays.
		//JsonArrays function like a List<JsonValue> object might
		//(Support for full IList<JsonValue> functionality pending.
		JsonObject potion = new JsonObject();
		inv.Add(potion);
		potion["name"] = "Vial of Viscous White Liquid";
		potion["type"] = "consumable";
		potion["stackable"] = true;
		potion["qty"] = 5;
		potion["hprec"] = 50;
		potion["effect"] = "spunky";
		potion["effectDuration"] = 3;
		
		JsonObject sword = new JsonObject();
		inv.Add(sword);
		sword["name"] = "Katana of 1000 truths";
		sword["type"] = "gear";
		sword["atk"] = 105;
		sword["equipSlot"] = "weapon";
		
		JsonArray gems = new JsonArray();
		sword["gems"] = gems;
		
		//JsonValues can be parsed from well-formed json.
		//This Json can come from just about any source.
		gems.Add(Json.Parse("{\"name\":\"redGem\",\"type\":\"gem\",\"atk\":\"15\",\"str\":\"2\"}"));
		gems.Add(Json.Parse("{\"name\":\"yellowGem\",\"type\":\"gem\",\"str\":\"3\",\"vit\":\"5\"}"));
		
		JsonObject armor = new JsonObject();
		inv.Add(armor);
		armor["name"] = "Yukata of Nihon";
		armor["type"] = "gear";
		armor["def"] = 15;
		armor["equipSlot"] = "body";
		armor["gems"] = new JsonArray();
		//Empty arrays are also possible.
		
		inv.Add(Json.Parse("{\"name\":\"greenGem\",\"type\":\"gem\",\"dex\":\"5\",\"agi\":\"2\"}"));		
		
		string json = obj.PrettyPrint();
		Print("Constructed an object: " + json);
		
		JsonValue parsed = Json.Parse(json);
		JsonObject refObj = parsed as JsonObject;
		
		
		string json2 = refObj.PrettyPrint();
		Print("Parsed the json back into an object: " + json2);
		
		if (json == json2) {
			Print("Objects generated same json!");
		}
		
	}
	
	public static void StructExample() {
		Skruck cuck = new Skruck(4, 5, 6, 7);
		Print("Struct by itself:" + cuck);
		
		JsonObject reflected = Json.Reflect(cuck) as JsonObject;
		Print("Reflected a struct: \n" + reflected.PrettyPrint());
		
		cuck = new Skruck(1,2,3,4);
		Print("Changed Struct: " + cuck);
		
		try {
			Json.ReflectInto(reflected, cuck);
		} catch (Exception e) {
			Print("Produced Exception: " + e);
		}
		cuck = (Skruck) Json.GetValue(reflected, typeof(Skruck));
		Print("Reflected back into struct with Json.GetValue(). Result: " + cuck);
		
		///
		Print("Struct nested inside object:");
		Skree skree = new Skree();
		Print("Started with: " + skree);
		
		reflected = Json.Reflect(skree) as JsonObject;
		Print("Reflected into: " + reflected.PrettyPrint()); 
		
		skree.bob = new Skruck(99, 98, 97, 96);
		skree.joe = new Skruck(95, 94, 93, 92);
		Print("Changed to: " + skree);
		
		Json.ReflectInto(reflected, skree);
		
		Print("Reflected back into: " + skree);
		
		
	}
	
	public static void ReflectionExample_Foobar() {
		Print("Reflection Example - What things are reflected?");
		//Create and set up the test object (see below for class)
		Foobar foobar = new Foobar("poop", 5, 6, 7, 0, false);
		foobar.mlgprostatus = true;
		foobar.incomeDollahs = 53;
		foobar.incomeDollahs = 120;
		foobar.incomeDollahs = 77;
		foobar.incomeDollahs = 170;
		
		foobar.names = new string[] { "Bob", "George", "Light", "Rock", "Roll", "Bass", "Treble", "Tango"};
			foobar.numbers["strength"] = 10;
		foobar.numbers["vitality"] = 10;

		foobar.blacklisted = "lolbutts";
		foobar.black = "grey";
		
		Print("Started with: {\n" + foobar + "\n}\n\n");
		
		JsonValue reflected = Json.Reflect(foobar);
		//Reflect an object
		//Reflecting an object returns a JsonValue 
		//(base class of all others)
		//Primitive types:
		//bool -> JsonBoolean
		//string -> JsonString
		//double, int, long, short, byte, float -> JsonNumber
		//Array types will return a JsonArray
		//Object types return a JsonObject
		
		//store the 'pretty' json in a string.
		string json = reflected.PrettyPrint();
		Print("Reflected the object into json: " + json + "\n\n");
		
		//Reconstruct a JsonObject by parsing the json.
		JsonValue parsed = Json.Parse(json);
		//Parse returns a JsonValue, not always an object
		//Might be an array, number, string, boolean or null
		//Cast the parsed object into a JsonObject
		JsonObject refObject = parsed as JsonObject;
		if (refObject != null) {
			Foobar other = new Foobar();
			Print("Made new object: {\n" + other + "\n}\n\n");

			Json.ReflectInto(refObject, other);
			Print("Reflected json into new object: {\n" + other + "\n}\n\n");
		}
		
		
	}
	
	public static void ReflectionExample_NestedList() {
		Print("Reflection Example - Nested List (recursive nesting)");
		NestedList list = new NestedList("HULLO");
		//Set up the list with a few levels of bullshit.
		NestedList a = list;
		
		//This can go any number of levels deep.
		//Well, at least up until a stack overflow occurs...
		for (int i = 0; i < 7; i++) {
			string str = (i%2 == 0) ? "DELETE THIS" : "HULLO";
			a.next = new NestedList(""+ i + str);
			a = a.next;
		}
		Print("Started with the list: " + list);
		
		JsonValue reflected = Json.Reflect(list);
		string json = reflected.PrettyPrint();
		Print("Reflected list into Json: " + json);
		
		JsonValue parsed = Json.Parse(json);
		JsonObject refObj = parsed as JsonObject;
		NestedList reconstructed = new NestedList();
		Json.ReflectInto(refObj, reconstructed);
		
		Print("Reconstructed list from Json: " + reconstructed);

	}

	#endregion

}

#endif
