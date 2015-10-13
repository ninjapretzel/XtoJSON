/*	XtoJSON
	Lightweight JSON Library for C#
	Copyright (C) 2015  Jonathan Cohen
	Contact: ninjapretzel@yahoo.com

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

//Definition Flags:

//XtoJSON_StrictCommaRules 
//	Enabled - When parsing JSON, makes commas before end-group characters cause an exception.
//	Disabled - Commas before end-group characters are allowed
//Example: { "blah":"bluh", } 
//	the above JSON will cause an exception to be thrown with the flag enabled, 
//	and will parse successfully with the flag disabled.

//XtoJSON_StringNumbers
//	Enabled - numbers are stored internally as a string value, and are converted to and from number types
//	Disabled - numbers are stored internally as a double value, and are parsed from a string when converted from anything other than a double
//May have minor performance implications when enabled.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5
using UnityEngine;


#endif



#region Abstract/Primary stuff

/// <summary>Enum of all types supported by XtoJSON</summary>
public enum JsonType { String, Boolean, Number, Object, Array, Null }

/// <summary> Quick access to Json parsing and reflection </summary>
public static class Json {
	/// <summary> Current version of library </summary>
	public const string VERSION = "0.5.2";


	/// <summary> Parse a json string into its JsonValue representation. </summary>
	public static JsonValue Parse(string json) {
		JsonDeserializer jds = new JsonDeserializer(json);
		return jds.Deserialize();
	}

	/// <summary> Trys to parse a json string into a JsonValue representation. 
	/// if it fails, catches the exception that was thrown, and returns a null</summary>
	public static JsonValue TryParse(string json) {
		try { return Parse(json); }
		catch {
			Console.WriteLine("Error! Couldn't Parse Json!");
			return null;
		}
	}


	/// <summary> Reflect a code object into a JsonValue representation. </summary>
	public static JsonValue Reflect(object obj) { return JsonReflector.Reflect(obj); }

	/// <summary> Reflect a JsonValue into a specified type.  </summary>
	public static object GetValue(JsonValue val, Type destType) { return JsonReflector.GetReflectedValue(val, destType); }
	/// <summary> Reflect a JsonValue into a specified type. </summary>
	public static T GetValue<T>(JsonValue val) {
		if (typeof(JsonObject).IsAssignableFrom(typeof(T))) { return (T)(object)(val as JsonObject); }
		else if (typeof(JsonArray).IsAssignableFrom(typeof(T))) { return (T)(object)(val as JsonArray); }

		object o = GetValue(val, typeof(T)); 
		if (o == null) { return default(T); }
		return (T)o;
	}

	/// <summary> Reflect information in a JsonObject into a desitnation code object. </summary>
	public static void ReflectInto(JsonObject source, object destination) {
		if (source != null) {
			JsonReflector.ReflectInto(source, destination);
		}
	}

	/// <summary> Get the expected type of the reflection of a code object. </summary>
	public static JsonType ReflectedType(object o) {
		if (o == null) { return JsonType.Null; }
		Type t = o.GetType();
		if (t.IsArray) { return JsonType.Array; }
		if (t == typeof(string)) { return JsonType.String; }
		if (t == typeof(bool)) { return JsonType.Boolean; }

		if (t == typeof(int)
			|| t == typeof(byte)
			|| t == typeof(float)
			|| t == typeof(double)
			|| t == typeof(long)) { return JsonType.Number; }

		return JsonType.Object;
	}

	/// <summary> More formal name for Parse() </summary>
	public static JsonValue ParseJson(this string json) { return Parse(json); }
	/// <summary> More formal name for Parse() </summary>
	public static JsonValue DeserializeJson(this string json) { return Parse(json); }

	/// <summary> More formal name for Reflect() </summary>
	public static JsonValue ReflectJson(this object obj) { return Reflect(obj); }
	/// <summary> More formal name for Reflect() </summary>
	public static JsonValue SerializeJson(this object obj) { return Reflect(obj); }

}

/// <summary> Base class for all representations of Json values </summary>
public abstract class JsonValue {

	protected readonly string HORIZONTAL_TAB = "\t";
	public static int CURRENT_INDENT = 0;

	/// <summary> Base JsonNull null reference </summary>
	public static readonly JsonValue NULL = JsonNull.instance;

	/// <summary> Is this JsonValue a JsonNumber? </summary>
	public bool isNumber { 
		get {
			if (JsonType == JsonType.String) {
				double d = 0;
				return Double.TryParse(stringVal, out d);
			}
			return JsonType == JsonType.Number; 
		} 
	}
	/// <summary> Is this JsonValue a JsonString? </summary>
	public bool isString { get { return JsonType == JsonType.String; } }
	/// <summary> Is this JsonValue a JsonBoolean? </summary>
	public bool isBool { get { return JsonType == JsonType.Boolean; } }
	/// <summary> Is this JsonValue a JsonObject? </summary>
	public bool isObject { get { return JsonType == JsonType.Object; } }
	/// <summary> Is this JsonValue a JsonArray? </summary>
	public bool isArray { get { return JsonType == JsonType.Array; } }
	/// <summary> Is this a null? </summary>
	public bool isNull { get { return JsonType == JsonType.Null; } }

	/// <summary> How many items are in this JsonValue, given it is a collection? </summary>
	public virtual int Count { get { throw new InvalidOperationException("This JsonValue is not a collection"); } }
	/// <summary> Indexes the JsonValue as if it was a JsonArray </summary>
	public virtual JsonValue this[int index] { 
		get { throw new InvalidOperationException("This JsonValue cannot be indexed with an integer"); }
		set { throw new InvalidOperationException("This JsonValue cannot be indexed with an integer"); }
	}
	/// <summary> Indexes the JsonValue as if it was a JsonObject </summary>
	public virtual JsonValue this[string key] { 
		get { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }
		set { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }
	}

	/// <summary> Does this JsonValue have a given key, when treated as a JsonObject </summary>
	public virtual bool ContainsKey(string key) { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }
	/// <summary> Does this JsonValue contain all of the keys in a given array, when treated as a JsonObject </summary>
	public virtual bool ContainsAllKeys(params string[] keys) { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }
	/// <summary> Does this JsonValue contain any of the keys in a given array, when treated as a JsonObject </summary>
	public virtual bool ContainsAnyKeys(params string[] keys) { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }

	/// <summary> Treat this JsonValue as a JsonObject, and retrieve a string at a given key </summary>
	public string GetString(string key) {
		if (ContainsKey(key)) {
			JsonValue thing = this[key];
			if (thing.isString) { return thing.stringVal; }
		}
		return "";
	}

	/// <summary> Treat this JsonValue as a JsonObject, and retrieve a bool at a given key </summary>
	public bool GetBoolean(string key) {
		if (ContainsKey(key)) {
			JsonValue thing = this[key];
			if (thing.isBool) { return thing.boolVal; }
		}
		return false;
	}

	/// <summary> Treat this JsonValue as a JsonObject, and retrieve a float at a given key. </summary>
	public float GetFloat(string key) { return (float) GetNumber(key); }
	/// <summary> Treat this JsonValue as a JsonObject, and retrieve an int at a given key. </summary>
	public int GetInt(string key) { return (int) GetNumber(key); }
	/// <summary> Treat this JsonValue as a JsonObject, and retrieve a double at a given key. </summary>
	public double GetNumber(string key) { 
		if (ContainsKey(key)) {
			JsonValue thing = this[key];
			if (thing.isNumber) { return thing.numVal; }
		}
		return 0;
	}
	/// <summary> Get the boolean value of this JsonValue </summary>
	public virtual bool boolVal { get { throw new InvalidOperationException("This JsonValue is not a boolean"); } }
	/// <summary> Get the double value of this JsonValue </summary>
	public virtual double numVal { get { throw new InvalidOperationException("This JsonValue is not a number"); } }
	/// <summary> Get the float value of this JsonValue </summary>
	public virtual float floatVal { get { throw new InvalidOperationException("This JsonValue is not a number"); } }
	/// <summary> Get the double value of this JsonValue </summary>
	public virtual double doubleVal { get { throw new InvalidOperationException("This JsonValue is not a number"); } }
	/// <summary> Get the integer value of this JsonValue </summary>
	public virtual int intVal { get { throw new InvalidOperationException("This JsonValue is not a number"); } }
	/// <summary> Get the string value of this JsonValue </summary>
	public virtual string stringVal { get { throw new InvalidOperationException("This JsonValue is not a string"); } }

	/// <summary> Hidden constructor. </summary>
	internal JsonValue() { }

	/// <summary> Get the JsonType of this JsonValue. Fixed, based on the subclass. </summary>
	public abstract JsonType JsonType { get; }
	/// <summary> Get the string representation of this JsonValue. </summary>
	public abstract override string ToString();
	/// <summary> Get a pretty string representation of this JsonValue. </summary>
	public abstract string PrettyPrint();

	/// <summary> Implicit conversion from string to JsonValue </summary>
	public static implicit operator JsonValue(string val) { return new JsonString(val); }
	/// <summary> Implicit conversion from bool to JsonValue </summary>
	public static implicit operator JsonValue(bool val) { return JsonBool.Get(val); }
	/// <summary> Implicit conversion from double to JsonValue </summary>
	public static implicit operator JsonValue(double val) { return new JsonNumber(val); }
	/// <summary> Implicit conversion from float to JsonValue </summary>
	public static implicit operator JsonValue(float val) { return new JsonNumber(val); }
	/// <summary> Implicit conversion from int to JsonValue </summary>
	public static implicit operator JsonValue(int val) { return new JsonNumber(val); }

	/// <summary> Explicit conversion from JsonValue to string </summary>
	public static explicit operator string(JsonValue val) { return val.stringVal; }
	/// <summary> Explicit conversion from JsonValue to bool </summary>
	public static explicit operator bool(JsonValue val) { return val.boolVal; }
	/// <summary> Explicit conversion from JsonValue to double </summary>
	public static explicit operator double(JsonValue val) { return val.numVal; }
	/// <summary> Explicit conversion from JsonValue to decimal </summary>
	public static explicit operator decimal(JsonValue val) { return (decimal) val.numVal; }
	/// <summary> Explicit conversion from JsonValue to float </summary>
	public static explicit operator float(JsonValue val) { return (float) val.numVal; }
	/// <summary> Explicit conversion from JsonValue to int </summary>
	public static explicit operator int(JsonValue val) { return (int) val.numVal; }

	public static bool operator !=(JsonValue a, object b) { return !(a == b); }


	static double NUMBER_TOLERANCE = 1E-16;
	/// <summary> Override for == operator.
	/// If a and b are of compatible types, attempts to compare their equality.
	/// It first checks their references, and returns true if both are the same address.
	/// 
	/// Then:
	/// If they are numbers, it takes their difference over their sum, and returns true if it differes by a very small percentage (1E-16).
	/// If they are bools or strings, in compares their internal representations.
	/// If one is null, it checks against the JsonNull special value.
	/// </summary>
	public static bool operator ==(JsonValue a, object b) {
		//Handle 'same object' comparisons early.
		if (ReferenceEquals(a, b)) { return true; }
		//Handle nulls before attempting to do anything else...
		if (ReferenceEquals(a, null) || ReferenceEquals(a, JsonNull.instance)) {
			//if a is null, they can only be equal if both are null...
			return ReferenceEquals(b, null) || ReferenceEquals(b, JsonNull.instance);
		} else if (ReferenceEquals(b, null) || ReferenceEquals(b, JsonNull.instance)) {
			//if a is not null, and b is null, then they cannot be equal...
			return false;
		}

		//Both things are not null at this point...
		switch (a.JsonType) {
			case JsonType.Number:
				//Numbers compared within tolerance
				//Default compared to double.NaN
				double val = double.NaN;
				if (b.GetType().IsNumeric()) { val = JsonHelpers.GetNumericValue(b); }
				if (b is JsonNumber) { val = (b as JsonNumber).doubleVal; }
				if (val == double.NaN) { return false; }

				double s = a.doubleVal + val;
				double d = a.doubleVal - val;
				if (s == 0) { return false; }

				d /= s; 
				if (d < 0) { d *= -1; }

				return d < NUMBER_TOLERANCE;
			case JsonType.Boolean: // bools compared by true/false equivelence
				if (b is bool) { return a.boolVal == ((bool)b); }
				if (b is JsonBool) { return a.boolVal == ((JsonBool)b).boolVal; }

				return false;
			case JsonType.String: // strings compared by internal representations via 'string == string'
				if (b is string) { return a.stringVal == ((string)b); }
				if (b is JsonString) { return a.stringVal == ((JsonString)b).stringVal; }

				return false;

			case JsonType.Object:
			case JsonType.Array:
			default:
				// Objects and Arrays with different addresses are not considered equal by '=='
				return false;
		}

	}

	/// <summary>
	/// Equality Comparison from any JsonValue type to any object
	/// This is more in depth than a plain '==' comparison, which defaults to references
	/// This checks all keys within the method
	/// </summary>
	public override bool Equals(object b) {
		if (ReferenceEquals(this, b)) { return true; }
		if (b == null) { return false; }

		switch (JsonType) {
			case JsonType.Number:
			case JsonType.Boolean:
			case JsonType.String:
				//Re-use code for '==' for 'primitive' types.
				return (this == b);
			case JsonType.Array:
				if (b is JsonArray) {
					JsonArray arr = b as JsonArray;
					if (Count != arr.Count) { return false; }
					for (int i = 0; i < Count; i++) {
						if (this[i] != arr[i]) { return false; }
					}
					return true;
				}
				return false;
			case JsonType.Object:
				if (b is JsonObject) {
					JsonObject obj = b as JsonObject;
					if (Count != obj.Count) { return false; }
					int i = 0;
					foreach (var pair in obj) {
						string key = pair.Key.stringVal;
						JsonValue val = pair.Value;
						if (!this.ContainsKey(key)) { return false; }
						if (val != this[key]) { return false; }
						i++;
					}
					return true;
				}
				return false;
			default: // Default - a is JsonNull.instance
				return (b == null || b == JsonNull.instance);
		}
	}

	public override int GetHashCode() {
		return base.GetHashCode();
	}

}

/// <summary> Base class for JsonValues that hold a group of objects </summary>
public abstract class JsonValueCollection : JsonValue {

	protected readonly string JsonVALUE_SEPARATOR = ",";

	/// <summary> Hidden internal constructor </summary>
	internal JsonValueCollection() { }

	/// <summary> Create a pretty string representation of this collection </summary>
	protected abstract string CollectionToPrettyPrint();
	/// <summary> Create a compact string representation of this collection </summary>
	protected abstract string CollectionToString();
	/// <summary> Create a compact string representation of this collection </summary>
	public override string ToString() { return BeginMarker + CollectionToString() + EndMarker; }
	/// <summary> Create a pretty string representation of this collection </summary>
	public override string PrettyPrint() {
		return Environment.NewLine +
			"".PadLeft(JsonValue.CURRENT_INDENT, Convert.ToChar(base.HORIZONTAL_TAB)) +
			BeginMarker +
			Environment.NewLine +
			CollectionToPrettyPrint() +
			Environment.NewLine +
			"".PadLeft(JsonValue.CURRENT_INDENT, Convert.ToChar(base.HORIZONTAL_TAB)) +
			EndMarker;
	}

	/// <summary> Definition for begining marker character </summary>
	protected abstract string BeginMarker { get; }
	/// <summary> Definition for ending marker character </summary>
	protected abstract string EndMarker { get; }


}

#endregion


#region Primitives 

/// <summary> Represents a null as a JsonObject </summary>
public class JsonNull : JsonValue {
	/// <summary> internal representatino </summary>
	public string _value { get { return "null"; } }
	/// <summary> single instance of null </summary>
	public static JsonNull instance = new JsonNull();

	/// <summary> private constructor </summary>
	private JsonNull() : base() { }

	public override JsonType JsonType { get { return JsonType.Null; } }
	public override string ToString() { return _value; }
	public override string PrettyPrint() { return _value; }

}

/// <summary> bool type represented as a JsonValue. </summary>
public class JsonBool : JsonValue {
	/// <summary> internal representation </summary>
	private string _value;

	/// <summary> 'true' instance </summary>
	public static JsonBool TRUE = new JsonBool(true);
	/// <summary> 'false' instance </summary>
	public static JsonBool FALSE = new JsonBool(false);

	public override bool boolVal { get { return _value == "true"; } }
	public override JsonType JsonType { get { return JsonType.Boolean; } }

	/// <summary> Implicit conversion from bool to JsonBool </summary>
	public static implicit operator JsonBool(bool val) { return val ? TRUE : FALSE; }
	/// <summary> Use a boolean value to access one of the JsonBool instances </summary>
	public static JsonBool Get(bool val) { return val ? TRUE : FALSE; }

	/// <summary> Private constructor </summary>
	private JsonBool(bool value) : base() { _value = (""+value).ToLower(); }

	public override string ToString() { return _value; }
	public override string PrettyPrint() { return ToString(); }

}

/// <summary> Representation of a number as a JsonValue </summary>
public class JsonNumber : JsonValue {
	public override JsonType JsonType { get { return JsonType.Number; } }
	/// <summary> Conversion between strings and numbers </summary>
	protected static NumberFormatInfo formatter = defaultNumberFormat;
	static NumberFormatInfo defaultNumberFormat {
		get {
			NumberFormatInfo info = new NumberFormatInfo();
			info.NumberDecimalSeparator = ".";
			return info;
		}
	}

#if XtoJSON_StringNumbers
	/// <summary> internal representation </summary>
	private string _value;


	public override double numVal { get { return Double.Parse(_value); } }
	public override double doubleVal { get { return Double.Parse(_value); } }
	public override float floatVal { get { return Single.Parse(_value); } }
	public override int intVal	{ get { return Int32.Parse(_value); } }


	/// <summary> Internal hidden constructor </summary>
	internal JsonNumber(string value) : base() { _value = value; }

	/// <summary> int constructor </summary>
	public JsonNumber(int value) : this(value.ToString()) { }
	/// <summary> double constructor </summary>
	public JsonNumber(double value) : this(value.ToString(formatter)) { }
	/// <summary> decimal constructor </summary>
	public JsonNumber(decimal value) : this(value.ToString(formatter)) { }
	/// <summary> float constructor </summary>
	public JsonNumber(float value) : this(value.ToString(formatter)) { }
	/// <summary> byte constructor </summary>
	public JsonNumber(byte value) : this(value.ToString()) { }

	public override string ToString() { return ""+_value; }
	public override string PrettyPrint() { return ""+_value; }

#else
	/// <summary> Internal representation </summary>
	private double _value;

	public override double numVal { get { return _value; } }
	public override double doubleVal { get { return _value; } }
	public override float floatVal { get { return (float)_value; } }
	public override int intVal { get { return (int)_value; } }

	/// <summary> Internal hidden constructor </summary>
	internal JsonNumber(double value) : base() { _value = value; }

	/// <summary> int constructor </summary>
	public JsonNumber(int value) : this(Double.Parse(""+value)) { }
	/// <summary> float constructor </summary>
	public JsonNumber(float value) : this(Double.Parse("" + value)) { }
	/// <summary> decimal constructor </summary>
	public JsonNumber(decimal value) : this(Double.Parse("" + value)) { }
	/// <summary> byte constructor </summary>
	public JsonNumber(byte value) : this(Double.Parse(""+value)) { }

	public override string ToString() { return _value.ToString(formatter); }
	public override string PrettyPrint() { return _value.ToString(formatter); }

#endif

	/// <summary> Implicit conversion from double to JsonNumber </summary>
	public static implicit operator JsonNumber(double val) { return new JsonNumber(val); }
	/// <summary> Implicit conversion from double to JsonNumber </summary>
	public static implicit operator JsonNumber(decimal val) { return new JsonNumber((double)val); }
	/// <summary> Implicit conversion from double to JsonNumber </summary>
	public static implicit operator JsonNumber(float val) { return new JsonNumber(val); }
	/// <summary> Implicit conversion from double to JsonNumber </summary>
	public static implicit operator JsonNumber(int val) { return new JsonNumber(val); }

}

/// <summary> Representation of a string as a JsonValue </summary>
public class JsonString : JsonValue {
	/// <summary> Internal representation </summary>
	private string _value;

	public override double numVal {
		get {
			double d = 0;
			if (Double.TryParse(_value, out d)) {
				return d;
			}
			return base.numVal;
		}
	}

	public override double doubleVal { get { return numVal; } }
	public override int intVal { get { return (int) numVal; } }
	public override float floatVal { get { return (float) numVal; } }

	public override string stringVal { get { return _value; } }
	public override JsonType JsonType { get { return JsonType.String; } }

	/// <summary> Implicit conversion from string to JsonString </summary>
	public static implicit operator JsonString(string val) { return new JsonString(val); }
	/// <summary> Implicit conversion from JsonString to string </summary>
	public static implicit operator string(JsonString val) { return val._value; }

	/// <summary> Constructor </summary>
	public JsonString(string value) : base() { _value = value; }

	public override string ToString() { return ToJsonString(_value); }

	/// <summary> Get the hash code of this object. Wraps through to the string inside of it. </summary>
	public override int GetHashCode() { return _value.GetHashCode(); }

	public override string PrettyPrint() { return ToString(); }

	/// <summary> Conversion for representation inside of Json </summary>
	public static string ToJsonString(string text) {
		if (text == null) { return "\"\""; }
		return "\"" + text.JsonEscapeString() + "\"";
	}

}


#endregion


#region Composites
/// <summary> Representation of arbitrary object types as JsonObjects </summary>
public class JsonObject : JsonValueCollection, IEnumerable<KeyValuePair<JsonString, JsonValue>> {


	protected override string BeginMarker { get { return "{"; } }
	protected override string EndMarker { get { return "}"; } }

	/// <summary> Internal representation of information. </summary>
	private Dictionary<JsonString, JsonValue> data;

	/// <summary> Separator character </summary>
	private readonly string NAMEVALUEPAIR_SEPARATOR = ":";

	public override JsonType JsonType { get { return JsonType.Object; } }
	public override int Count { get { return data.Count; } }
	public override JsonValue this[string key] {
		get { 
			if (key == null) { return NULL; }
			if (data.ContainsKey(key)) { return data[key]; } 
			return NULL; 
		}
		set { 
			if (value == null && data.ContainsKey(key)) { data.Remove(key); }
			if (value != null) { data[key] = value; }
		}
	}

	public override bool ContainsKey(string key) { return data.ContainsKey(key); }
	public override bool ContainsAnyKeys(params string[] keys) {
		foreach (string key in keys) {
			if (ContainsKey(key)) { return true; }
		}
		return false;
	}
	public override bool ContainsAllKeys(params string[] keys) {
		foreach (string key in keys) {
			if (!ContainsKey(key)) { return false; }
		}
		return true;
	}

	/// <summary> Default Constructor, creates an empty collection. </summary>
	public JsonObject() : base() { data = new Dictionary<JsonString, JsonValue>(); }
	/// <summary> Copy infomration from another JsonObject </summary>
	public JsonObject(JsonObject src) : this() { Add(src); }
	/// <summary> Create an JsonObject, setting its data to the parameter. </summary>
	public JsonObject(Dictionary<JsonString, JsonValue> src) : base() { data = src; }

	/// <summary> Create a duplicate of this object </summary>
	public JsonObject Clone() { return new JsonObject(this); }

	/// <summary> Add a key:value pair </summary>
	public JsonObject Add(JsonString name, JsonValue value) {
		if (value == null) { return this; }
		if (value == JsonNull.instance) { return this; }

		if (!data.ContainsKey(name)) {
			data.Add(name, value);
		}

		return this;
	}

	/// <summary> Adds all of the entries in a Dictionary<string, JsonValue> 
	/// or other type of Enumerable group of pairs of <string, JsonValue> </summary>
	public JsonObject AddAll<T>(IEnumerable<KeyValuePair<string, T>> info) where T : JsonValue {
		foreach (var pair in info) {
			this[pair.Key] = pair.Value;
		}
		return this;
	}
	/// <summary> Add all of the entries in a grouping of <string, object> pairs, reflecting each value. </summary>
	public JsonObject AddAllReflect<T>(IEnumerable<KeyValuePair<string, T>> info) {
		foreach (var pair in info) {
			this[pair.Key] = Json.Reflect(pair.Value);
		}
		return this;
	}

	/// <summary> Attempt to get a T from a given key. Reflects the JsonValue into a T </summary>
	public T Get<T>(string key) { return Json.GetValue<T>(this[key]); }

	/// <summary> Attempt to get a primitive type from a given key. </summary>
	public object GetPrimitive<T>(string name) { return GetPrimitive(name, typeof(T)); }
	/// <summary> Attempt to get a primitive type from a given key and given type.</summary>
	public object GetPrimitive(string name, Type type) {
		JsonValue val = this[name];
		if (type == typeof(string) 	&& val.isString) { return val.stringVal; }
		if (type == typeof(float) 	&& val.isNumber) { return val.floatVal; } 
		if (type == typeof(double) 	&& val.isNumber) { return val.numVal; } 
		if (type == typeof(int) 	&& val.isNumber) { return val.intVal; } 
		if (type == typeof(bool) 	&& val.isBool) { return val.boolVal; }
		if (type.IsNumeric() && val.isString) {
			double numVal = 0;
			double.TryParse(val.stringVal, out numVal);
			return Convert.ChangeType(numVal, type);
		}

		if (type.IsValueType && val.isObject) {
			return Json.GetValue(val, type);
		}

		return null;
	}

	/// <summary> Add all information from another JsonObject. </summary>
	public JsonObject Add(JsonObject other) { foreach (var pair in other) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable<KeyValuePair<string, string>> </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, string>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable<KeyValuePair<string, double>> </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, double>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable<KeyValuePair<string, short>> </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, short>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable<KeyValuePair<string, float>> </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, float>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable<KeyValuePair<string, long>> </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, long>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable<KeyValuePair<string, byte>> </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, byte>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable<KeyValuePair<string, int>> </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, int>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }

	/// <summary>Try to get a T from this object. 
	/// Returns the T if it can be found.
	/// If a T cannot be found, returns a default value. </summary>
	public T Extract<T>( string key, T defaultValue = default(T) ) {
		if (ContainsKey(key)) {
			JsonValue val = this[key];

			if (val.JsonType == Json.ReflectedType(defaultValue) ) { return Json.GetValue<T>(val); }
		}
		return defaultValue;
	}

	public JsonArray ToJsonArray() {
		JsonArray arr = new JsonArray();
		foreach (var pair in this) { 
			arr.Add(pair.Value);
		}
		return arr;
	}

	/// <summary> Returns the internal Dictionary's Enumerator </summary>
	IEnumerator IEnumerable.GetEnumerator() { return data.GetEnumerator(); } 
	/// <summary> Returns the internal Dictionary's Enumerator</summary>
	public IEnumerator<KeyValuePair<JsonString, JsonValue>> GetEnumerator() { return data.GetEnumerator(); }
	/// <summary> Returns the internal Dictionary's Enumerator </summary>
	public IEnumerator<KeyValuePair<JsonString, JsonValue>> Pairs { get { return data.GetEnumerator(); } }

	/// <summary> Returns the internal dictionary. </summary>
	public Dictionary<JsonString, JsonValue> GetData() { return data; }

	/// <summary> Gets a collection of all <string, bool> pairs </summary>
	public Dictionary<string, bool> ToDictOfBool() {
		Dictionary<string, bool> d = new Dictionary<string, bool>();
		foreach (var pair in data) {
			if (pair.Value.isBool) { d[pair.Key] = pair.Value.boolVal; }
		}
		return d;
	}
	/// <summary> Gets a collection of all <string, string> pairs </summary>
	public Dictionary<string, string> ToDictOfString() {
		Dictionary<string, string> d = new Dictionary<string, string>();
		foreach (var pair in data) {
			if (pair.Value.isString) { d[pair.Key] = pair.Value.stringVal; }
		}
		return d;
	}
	/// <summary> Gets a collection of all <string, double> pairs </summary>
	public Dictionary<string, double> ToDictOfDouble() {
		Dictionary<string, double> d = new Dictionary<string, double>();
		foreach (var pair in data) {
			if (pair.Value.isNumber) { d[pair.Key] = pair.Value.numVal; }
		}
		return d;
	}
	/// <summary> Gets a collection of all <string, float> pairs </summary>
	public Dictionary<string, float> ToDictOfFloat() {
		Dictionary<string, float> d = new Dictionary<string, float>();
		foreach (var pair in data) {
			if (pair.Value.isNumber) { d[pair.Key] = (float)pair.Value.numVal; }
		}
		return d;
	}
	/// <summary> Gets a collection of all <string, int> pairs </summary>
	public Dictionary<string, int> ToDictOfInt() {
		Dictionary<string, int> d = new Dictionary<string, int>();
		foreach (var pair in data) {
			if (pair.Value.isNumber) { d[pair.Key] = (int)pair.Value.numVal; }
		}
		return d;
	}

	/// <summary> Removes the KeyValue pair associated with the given key </summary>
	public JsonObject Remove(string key) { 
		if (ContainsKey(key)) { data.Remove(key); }
		return this;
	}

	/// <summary> Creates a new JsonObject that has a subset of the original's KeyValue pairs </summary>
	public JsonObject Mask(IEnumerable<JsonString> mask) {
		JsonObject result = new JsonObject();
		foreach (JsonString str in mask) { result.Add(str, this[str]); }
		return result;
	}
	/// <summary> Creates a new JsonObject that has a subset of the original's KeyValue pairs </summary>
	public JsonObject Mask(IEnumerable<string> mask) {
		JsonObject result = new JsonObject();
		foreach (string str in mask) { result.Add(str, this[str]); }
		return result;
	}

	/// <summary> Removes all KeyValue pairs from the JsonObject. </summary>
	public JsonObject Clear() { data.Clear(); return this; }



	/// <summary> Takes all of the KeyValue pairs from the other object, 
	/// and sets this object to have the same values for those keys. </summary>
	public JsonObject Set(JsonObject other) {
		foreach (var pair in other) {
			this[pair.Key] = pair.Value;
		}
		return this;
	}

	/// <summary> Takes all of the pairs from a dictionary, 
	/// and sets this object to have the JsonValue version of the Values associated with the key </summary>
	public JsonObject Set<T>(Dictionary<string, T> info) {
		foreach (var pair in info) {
			this[pair.Key] = Json.Reflect(pair.Value);
		}
		return this;
	}


	/// <summary>
	/// Compares keys between two JsonObjects
	/// If the values of the keys are the same (including 'not being there'), returns true.
	/// If any of the keys exists in one but not the other, or values at the keys are different, returns false.
	/// Optionally, an array of keys can be provided to tell it what set to check.
	/// </summary>
	/// <param name="other">Other object to chec </param>
	/// <param name="stuff">Subset of keys to check. Optional</param>
	/// <returns></returns>
	public bool Same(JsonObject other, string[] stuff = null) {
		if (stuff == null) { return this.Equals(other); }
		foreach (string s in stuff) {
			if (ContainsKey(s) && other.ContainsKey(s)) {
				if (this[s] != other[s]) { return false; }
			}
		}
		return true;
	}


	protected override string CollectionToPrettyPrint() {
		JsonValue.CURRENT_INDENT++;
		List<string> output = new List<string>();
		List<string> nvps = new List<string>();
		foreach (KeyValuePair<JsonString, JsonValue> kvp in data) {
			string s = "".PadLeft(JsonValue.CURRENT_INDENT, Convert.ToChar(base.HORIZONTAL_TAB)) + kvp.Key.PrettyPrint() + NAMEVALUEPAIR_SEPARATOR;
			if (kvp.Value != null) {
				s += kvp.Value.PrettyPrint();
			} else {//possible for collection to have an actual 'null' instead of JsonValue.NULL
				s += "null";
			}
			nvps.Add(s);
		}
		output.Add(string.Join(base.JsonVALUE_SEPARATOR + Environment.NewLine, nvps.ToArray()));
		JsonValue.CURRENT_INDENT--;
		return string.Join("", output.ToArray());
	}

	protected override string CollectionToString() {
		List<string> output = new List<string>();
		List<string> nvps = new List<string>();
		foreach (KeyValuePair<JsonString, JsonValue> kvp in data) {
			string s = kvp.Key.ToString() + NAMEVALUEPAIR_SEPARATOR;
			if (kvp.Value != null) {
				s += kvp.Value.ToString();
			} else {//possible for collection to have an actual 'null' instead of JsonValue.NULL
				s += "null";
			}
			nvps.Add(s);
		}
		output.Add(string.Join(base.JsonVALUE_SEPARATOR, nvps.ToArray()));
		return string.Join("", output.ToArray());
	}



}
/// <summary> Representation of an array of objects </summary>
public class JsonArray : JsonValueCollection, IEnumerable<JsonValue> {

	protected override string BeginMarker { get { return "["; } }
	protected override string EndMarker { get { return "]"; } }
	/// <summary> Internal representation of data </summary>
	protected List<JsonValue> list;
	/// <summary> Get the internal representation of data </summary>
	public List<JsonValue> GetList() { return list; }

	public override JsonType JsonType { get { return JsonType.Array; } }
	public override int Count { get { return list.Count; } }
	public override JsonValue this[int index] { 
		get { return list[index]; }
		set { list[index] = value; }
	}

	/// <summary> Default Constructor, creates an empty list </summary>
	public JsonArray() : base() { list = new List<JsonValue>(); }
	/// <summary> Creates a new JsonArray using a given list as its internal data. </summary>
	public JsonArray(List<JsonValue> values) : base() { list = values; }
	/// <summary> Creates a new JsonArray and copies all elements from another list. </summary>
	public JsonArray(JsonArray src) : this() { AddAll(src); }

	/// <summary> Creates a copy of this JsonArray </summary>
	public JsonArray Clone() { return new JsonArray(this); }

	/// <summary> Adds a single JsonValue into this list </summary>
	public JsonArray Add(JsonValue val) { list.Add(val); return this; }
	/// <summary> Add all JsonValues from a collection </summary>
	public JsonArray AddAll(IEnumerable<JsonValue> info) {
		foreach (JsonValue val in info) { Add(val); }
		return this;
	}


	/// <summary> Add all JsonValues that are contained in a collection </summary>
	public JsonArray AddAll<T>(IEnumerable<T> info) where T : JsonValue {
		foreach (T val in info) { Add( (JsonValue) val ); }
		return this;
	}
	/// <summary> Add reflections of all objects that are contained in a collection </summary>
	public JsonArray AddAllReflect<T>(IEnumerable<T> info) { 
		foreach (T val in info) { Add( Json.Reflect(val) ); }
		return this;
	}

	/// <summary> Remove all objects from this JsonArray </summary>
	public JsonArray Clear() { list.Clear(); return this; }
	/// <summary> Does this array contain a specific JsonValue? </summary>
	public bool Contains(JsonValue val) { return list.Contains(val); }
	/// <summary> What is the index of a specific JsonValue </summary>
	public int IndexOf(JsonValue val) { return list.IndexOf(val); }
	/// <summary> Remove a given JsonValue from the JsonArray </summary>
	public JsonArray Remove(JsonValue val) { list.Remove(val); return this; }
	/// <summary> Insert a JsonValue at a specific position </summary>
	public JsonArray Insert(int index, JsonValue val) { list.Insert(index, val); return this; }
	/// <summary> Remove a JsonValue from a specific index </summary>
	public JsonArray RemoveAt(int index) { list.RemoveAt(index); return this; }

	/// <summary> Returns the internal list's Enumerator </summary>
	IEnumerator IEnumerable.GetEnumerator() { return list.GetEnumerator(); }

	/// <summary> Returns the internal list's Enumerator </summary>
	public IEnumerator<JsonValue> GetEnumerator() { return list.GetEnumerator(); }

	public static implicit operator string[](JsonArray a) { return a.ToStringArray(); }
	public static implicit operator double[](JsonArray a) { return a.ToDoubleArray(); }
	public static implicit operator float[](JsonArray a) { return a.ToFloatArray(); }
	public static implicit operator int[](JsonArray a) { return a.ToIntArray(); }

	/// <summary> Get an array of all JsonNumbers as double values  </summary>
	public double[] ToDoubleArray() { return ToDoubleList().ToArray(); }
	/// <summary> Get a list of all JsonNumbers as double values </summary>
	public List<double> ToDoubleList() {
		List<double> arr = new List<double>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isNumber) { arr.Add(val.numVal); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonNumbers as int values  </summary>
	public int[] ToIntArray() { return ToIntList().ToArray(); }
	/// <summary> Get a list of all JsonNumbers as int values  </summary>
	public List<int> ToIntList() {
		List<int> arr = new List<int>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isNumber) { arr.Add((int)val.numVal); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonNumbers as float values  </summary>
	public float[] ToFloatArray() { return ToFloatList().ToArray(); }
	/// <summary> Get an list of all JsonNumbers as float values  </summary>
	public List<float> ToFloatList() {
		List<float> arr = new List<float>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isNumber) { arr.Add((float)val.numVal); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonBooleans as bool values  </summary>
	public bool[] ToBoolArray() { return ToBoolList().ToArray(); }
	/// <summary> Get a list of all JsonBooleans as bool values  </summary>
	public List<bool> ToBoolList() {
		List<bool> arr = new List<bool>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isBool) { arr.Add(val.boolVal); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonStrings as string values  </summary>
	public string[] ToStringArray() { return ToStringList().ToArray(); }
	/// <summary> Get a list of all JsonStrings as string values  </summary>
	public List<string> ToStringList() {
		List<string> arr = new List<string>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isString) { arr.Add(val.stringVal); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonObjects values  </summary>
	public JsonObject[] ToJsonObjectArray() { return ToJsonObjectList().ToArray(); }
	/// <summary> Get a list of all JsonObjects values  </summary>
	public List<JsonObject> ToJsonObjectList() {
		List<JsonObject> arr = new List<JsonObject>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isObject) { arr.Add(val as JsonObject); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonObjects as T values  </summary>
	public T[] ToArrayOf<T>() { return ToListOf<T>().ToArray(); }
	/// <summary> Get a list of all JsonNumbers as T values  </summary>
	public List<T> ToListOf<T>() {
		Type type = typeof(T);
		ConstructorInfo constructor = type.GetConstructor(new Type[]{}); 

		List<T> arr = new List<T>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			T sval = default(T);
			bool setVal = false;
			if (val.isString && type == typeof(string)) { sval = (T)(object)val.stringVal; setVal = true; }
			else if (val.isNumber && type == typeof(double)) { sval = (T)(object)val.numVal; setVal = true; }
			else if (val.isNumber && type == typeof(int)) { sval = (T)(object)(int)val.numVal; setVal = true; }
			else if (val.isNumber && type == typeof(float)) { sval = (T)(object)(float)val.numVal; setVal = true; }
			else if (val.isNumber && type == typeof(byte)) { sval = (T)(object)(byte)val.numVal; setVal = true; }
			else if (val.isNumber && type == typeof(long)) { sval = (T)(object)(long)val.numVal; setVal = true; }
			else if (val.isBool && type == typeof(bool)) { sval = (T)(object)val.boolVal; setVal = true; }
			else if (val.isNull) { sval = (T)(object)null; }
			else if (val.isObject) {
				JsonObject jobj = val as JsonObject;
				object obj = (object) constructor.Invoke(new object[]{});
				JsonReflector.ReflectInto(jobj, obj);
				sval = (T)obj;
				setVal = true;
			}

			if (setVal) { arr.Add(sval); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonObjects as object values  </summary>
	public object[] ToObjectArray(Type type) { return ToObjectList(type).ToArray(); }
	/// <summary> Get a list of all JsonObjects as object values  </summary>
	public List<object> ToObjectList(Type type) {
		ConstructorInfo constructor = type.GetConstructor(new Type[]{});

		List<object> arr = new List<object>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isString && type == typeof(string)) { arr.Add(val.stringVal); }
			else if (val.isNumber && type == typeof(double)) { arr.Add(val.numVal); }
			else if (val.isNumber && type == typeof(int)) { arr.Add((int)val.numVal); }
			else if (val.isNumber && type == typeof(float)) { arr.Add((float)val.numVal); }
			else if (val.isNumber && type == typeof(byte)) { arr.Add((byte)val.numVal); }
			else if (val.isNumber && type == typeof(long)) { arr.Add((long)val.numVal); }
			else if (val.isBool && type == typeof(bool)) { arr.Add(val.boolVal); }
			else if (val.isNull) { arr.Add(null); }
			else if (val.isObject && constructor != null) {
				object obj = constructor.Invoke(new object[]{});
				JsonObject jobj = val as JsonObject;
				JsonReflector.ReflectInto(jobj, obj);
				arr.Add(obj);
			}
		}


		return arr;
	}

	protected override string CollectionToPrettyPrint() {
		JsonValue.CURRENT_INDENT++;
		List<string> output = new List<string>();
		List<string> nvps = new List<string>();
		foreach (JsonValue jv in list) {
			string s = "null";
			//possible for collection to have an actual 'null' instead of JsonValue.NULL
			if (jv != null) { s = jv.PrettyPrint(); }
			nvps.Add("".PadLeft(JsonValue.CURRENT_INDENT, Convert.ToChar(base.HORIZONTAL_TAB)) + s);
		}
		output.Add(string.Join(base.JsonVALUE_SEPARATOR + Environment.NewLine, nvps.ToArray()));
		JsonValue.CURRENT_INDENT--;
		return string.Join("", output.ToArray());
	}

	protected override string CollectionToString() {
		List<string> output = new List<string>();
		List<string> nvps = new List<string>();
		foreach (JsonValue jv in list) {
			string s = "null";
			//possible for collection to have an actual 'null' instead of JsonValue.NULL
			if (jv != null) { s = jv.ToString(); }
			nvps.Add(s);
		}
		output.Add(string.Join(base.JsonVALUE_SEPARATOR, nvps.ToArray()));
		return string.Join("", output.ToArray());
	}



}

#endregion


#region Reflector 

/// <summary> Class containing Reflection Code </summary>
public class JsonReflector {

	/// <summary> Grab method info for JsonArray.ToArrayOf() </summary>
	static MethodInfo toArrayOf = typeof(JsonArray).GetMethod("ToArrayOf");
	/// <summary> Binding flags for easy usage </summary>
	static BindingFlags publicMembers = BindingFlags.Instance | BindingFlags.Public;
	/// <summary> Binding flags for easy usage </summary>
	static BindingFlags publicMember = BindingFlags.Instance | BindingFlags.Public;

	/// <summary> Reflect a JsonValue based on a given type. Attempts to return an object, 
	/// so return value may be null even if a value type is requested. </summary>
	public static object GetReflectedValue(JsonValue val, Type destType) {
		if (val == null) { return null; }
		object sval = null;
		if (val.isString && destType == typeof(string)) { sval = val.stringVal; }
		else if (val.isString && destType.IsNumeric()) { 
			double numVal = 0;
			double.TryParse(val.stringVal, out numVal);
			sval = Convert.ChangeType(numVal, destType);
		}
		else if (val.isNumber && destType == typeof(double)) { sval = val.numVal; }
		else if (val.isNumber && destType == typeof(float)) { sval = (float) val.numVal; }
		else if (val.isNumber && destType == typeof(int)) { sval = (int) val.numVal; }
		else if (val.isNumber && destType == typeof(byte)) { sval = (byte) val.numVal; }
		else if (val.isNumber && destType == typeof(long)) { sval = (long) val.numVal; }
		else if (val.isBool && destType == typeof(bool)) { sval = val.boolVal; }
		else if (val.isArray && destType.IsArray) {
			//TBD: Reflect the JsonArray into a new array
			JsonArray arr = val as JsonArray;
			Type eleType = destType.GetElementType();
			MethodInfo genericGrabber = toArrayOf.MakeGenericMethod(eleType);
			sval = genericGrabber.Invoke(arr, new object[]{});

		} else if (val.isObject) {
			//TBD: Reflect the JsonObject into a new object of that type
			JsonObject jobj = val as JsonObject;

			if (destType.IsValueType) {
				object boxedValue = Activator.CreateInstance(destType);
				FieldInfo[] fields = destType.GetFields();
				foreach (FieldInfo field in fields) {
					object innerVal = GetReflectedValue(jobj[field.Name], field.FieldType);
					if (innerVal != null) {
						field.SetValue(boxedValue, innerVal);
					}
				}
				return boxedValue;
			}
			sval = destType.GetNewInstance();
			if (sval != null) { ReflectInto(jobj, sval); }


		}

		return sval;
	}
	/// <summary> Reflect value stored in source JsonObject into a destination object. 
	/// Will recusively reflect parallel objects into their fields when applicable. </summary>
	public static void ReflectInto(JsonObject source, object destination) {
		Type type = destination.GetType();
		if (type.IsValueType) { throw new Exception("Can't reflect Json into a value type. Use Json.GetValue(JsonValue, Type) instead."); }

		var data = source.GetData();

		PropertyInfo mapper = type.GetProperty("Item", new Type[]{typeof(string)});
		Type mapperValueType = null;
		MethodInfo mapperSetMethod = null;

		if (mapper != null) {
			mapperValueType = mapper.PropertyType;
			mapperSetMethod = mapper.GetSetMethod();
		}

		PropertyInfo indexer = type.GetProperty("Item", new Type[]{typeof(int)});
		Type indexerValueType = null;
		MethodInfo adder = null;

		if (indexer != null) {
			indexerValueType = indexer.PropertyType;
			adder = type.GetMethod("Add", new Type[]{indexerValueType});
		}

		JsonArray _ITEMS = null;
		foreach (var pair in data) {
			string key = pair.Key;
			JsonValue val = pair.Value;

			if (key == "_ITEMS") {
				_ITEMS = val as JsonArray;
				continue;
			}

			PropertyInfo property = type.GetProperty(key, publicMember);
			if (property != null && property.IsWritable() && property.IsReadable()) {

				Type destType = property.PropertyType;
				MethodInfo setMethod = property.GetSetMethod();

				object sval = GetReflectedValue(val, destType);

				if (sval != null) {
					setMethod.Invoke(destination, new object[]{sval});
				}

				//If there exists a property by a name, there is likely no field by the same name
				//unless you're a hacker.
				continue;
			}

			FieldInfo field = type.GetField(key, publicMember);
			if (field != null) {

				Type destType = field.FieldType;

				object sval = GetReflectedValue(val, destType);

				if (sval != null) {
					field.SetValue(destination, sval);
				}
				//If we found a field at all, we don't need to try the indexer.
				continue;
			}

			//Don't bother with indexer if the set method wasn't extracted.
			//It will only be set if the index type is string
			//and the result type is assignable from a json primitive.
			if (mapperSetMethod != null) {
				object sval = GetReflectedValue(val, mapperValueType);

				if (sval != null) {
					mapperSetMethod.Invoke(destination, new object[] {key, sval});
				}

				continue;
			}


		}

		if (_ITEMS != null && indexer != null && adder != null) {
			List<JsonValue> list = _ITEMS.GetList();
			for (int i = 0; i < list.Count; i++) {
				JsonValue val = list[i];
				object sval = GetReflectedValue(val, indexerValueType);
				adder.Invoke(destination, new object[]{sval} );
				//indexerSetMethod.Invoke(destination, new object[] {i,sval}); 
			}

		}


	}

	/// <summary> Get a JsonRepresentation of a given code object. 
	/// Creates a new JsonValue based on what is needed. </summary>
	public static JsonValue Reflect(object source) {
		if (source == null) { return null; }
		Type type = source.GetType();

		//Return object directly if it is already a JsonValue in some way.
		if (typeof(JsonValue).IsAssignableFrom(type)) { return ((JsonValue)source); }

		JsonValue jval = null;

		//Handle primitive types
		if (type == typeof(string)) { return ((string)source); }
		else if (type == typeof(double)) { return ((double)source); }
		else if (type == typeof(int)) { return ((int)source); }
		else if (type == typeof(float)) { return ((float)source); }
		else if (type == typeof(byte)) { return ((byte)source); }
		else if (type == typeof(long)) { return ((long)source); }
		else if (type == typeof(short)) { return ((short)source); }
		else if (type == typeof(bool)) { return ((bool)source); }
		else if (type.IsArray) {
			JsonArray arr = new JsonArray();
			jval = arr;
			Array obj = source as Array;
			for (int i = 0; i < obj.Length; i++) {
				//Reflect that element and add it into the json array
				arr.Add(Reflect(obj.GetValue(i)));
			}
		} else {
			PropertyInfo keys = type.GetProperty("Keys");

			PropertyInfo mapper = type.GetProperty("Item", new Type[]{typeof(string)});

			PropertyInfo count = type.GetProperty("Count", typeof(int));
			PropertyInfo indexer = type.GetProperty("Item", new Type[]{typeof(int)});

			PropertyInfo[] properties = type.GetProperties(publicMembers);
			FieldInfo[] fields = type.GetFields(publicMembers);

			JsonObject obj = new JsonObject();
			jval = obj;

			string[] blacklist = null;
			FieldInfo blacklistField = type.GetField("_blacklist", BindingFlags.Public | BindingFlags.Static);
			if (blacklistField != null && blacklistField.FieldType == typeof(string[])) {
				blacklist = (string[])blacklistField.GetValue(null);
			}
			if (blacklist == null) { blacklist = new string[0]; }


			if (keys != null 
				&& mapper != null
				&& !mapper.IsObsolete()
				&& typeof(IEnumerable<string>).IsAssignableFrom(keys.PropertyType)) {

				MethodInfo keysGet = keys.GetGetMethod();
				MethodInfo mapperGet = mapper.GetGetMethod();
				IEnumerable<string> sKeys = (IEnumerable<string>)keysGet.Invoke(source, null);

				foreach (string key in sKeys) {
					if (blacklist.Contains<string>(key)) { continue; }

					object mappedObj = mapperGet.Invoke(source, new object[]{key});
					obj.Add(key, Reflect(mappedObj));
				}

			}

			if (count != null && indexer != null) {
				JsonArray arr = new JsonArray();
				MethodInfo countGet = count.GetGetMethod();
				MethodInfo indexerGet = indexer.GetGetMethod();
				int cnt = (int)countGet.Invoke(source, null);
				for (int i = 0; i < cnt; i++) {
					object indexedObj = indexerGet.Invoke(source, new object[]{i});
					arr.Add(Reflect(indexedObj));
				}
				obj.Add("_ITEMS", arr);
			}

			foreach (PropertyInfo property in properties) {

				if (property.Name == "Item"
					|| blacklist.Contains<string>(property.Name) 
					|| !property.IsWritable() 
					|| !property.IsReadable()
					|| property.IsObsolete()) { continue; }

#if UNITY_5
				//Why did they not mark this obsolete? Can't automatically detect it...
				if (property.Name == "useConeFriction") { continue; }
#endif

				MethodInfo propGet = property.GetGetMethod();

				object grabbed = propGet.Invoke(source, null);
				obj.Add(property.Name, Reflect(grabbed));
			}

			foreach (FieldInfo field in fields) {
				if (blacklist.Contains<string>(field.Name)
					|| field.IsObsolete()) { continue; }

				object grabbed = field.GetValue(source);
				obj.Add(field.Name, Reflect(grabbed));
			}

		}

		return jval;
	}

}

#endregion


#region Deserializer

/// <summary> Class holding logic for parsing Json text into JsonValues 
/// A new instance of this class is created automatically by Json.Parse() </summary>
public class JsonDeserializer {

	/// <summary> Json text that is being parsed </summary>
	private string json;
	/// <summary> Current position </summary>
	private int index;

	/// <summary> quick access to the current character </summary>
	private char next { get { return json[index]; } }

	/// <summary> Constructor. Starts parsing from the begining of a given string </summary>
	public JsonDeserializer(string str) {
		index = 0;
		json = str;
	}

	/// <summary> Deserialize the Json text, and get back the resulting JsonValue </summary>
	public JsonValue Deserialize() {
		index = 0;
		SkipWhitespace();
		return ProcessValue();
	}

	/// <summary> Process the next JsonValue, and recursivly process any other necessary 
	/// JsonValues stored within. </summary>
	JsonValue ProcessValue() {
		if (next == '[') { return ProcessArray(); }
		if (next == '{') { return ProcessObject(); }
		if (next == '"') { 
			string val = ProcessString().JsonUnescapeString();
			//TBD: Additional processing

			return val;
		}

		int startIndex = index;
		while (index < json.Length && next != ',' && next != '}' && next != ']' && !char.IsWhiteSpace(next)) {
			index++;
		}
		string jval = json.Substring(startIndex, index-startIndex);

		if (jval == "true") { return true; }
		if (jval == "false") { return false; }
		if (jval == "null") { return JsonValue.NULL; }

		double dval;
		if (double.TryParse(jval, out dval)) { return dval; }

		return JsonValue.NULL;

	}

	string ProcessString() {
		int startIndex = index + 1;

		while (true) {
			index++;

			while (next != '\"') { index++; }

			int j = index - 1;
			int count = 0;

			while (json[j] == '\\') {
				j--;
				count++;
			}

			//if there are an even number of backslashes, 
			//then they are all just backslashes and they aren't escaping the quote
			//otherwise, the quote is being escaped and we need to keep searching for the close quote
			if (count % 2 == 0) {
				break;
			}

		}

		return json.Substring(startIndex, index - startIndex);
	}

	/// <summary> Logic for parsing contents of a JsonArray </summary>
	JsonArray ProcessArray() {
		index++;
		JsonArray array = new JsonArray();

		SkipWhitespace();
		if (next == ']') {
			index++;
			return array;
		}

		while (true) {
			array.Add(ProcessValue());
			if (!MoveNext()) { break; }
		}

		return array;
	}

	/// <summary> Logic for parsing the contents of a JsonObject</summary>
	JsonObject ProcessObject() {
		index++;
		JsonObject obj = new JsonObject();
		SkipWhitespace();
		if (next == '}') {
			index++;
			return obj;
		}

		while (true) {
			string key = ProcessKey();
			SkipWhitespace();
			JsonValue val = ProcessValue();
			obj.Add(key, val);
			if (!MoveNext()) { break; }
		}

		return obj;
	}

	/// <summary> Logic for moving over characters until the next control character </summary>
	bool MoveNext() {
		while (index < json.Length && next != ',' && next != ']' && next != '}') { index++; }

		if (next == ',') {
			index++;
			SkipWhitespaceEnd();
			if (next == ']' || next == '}') {
#if XtoJSON_StrictCommaRules
				throw new Exception("Commas before end characters not allowed.");
#else
				index++; 
				return false;
#endif
			}
			if (index >= json.Length) { return false; }

		} else {
			if (json[index] == ']' || json[index] == '}') {
				index++;
				return false;
			}
			if (index >= json.Length) { return false; }
		}

		return true;
	}

	/// <summary> Logic for extracting a string value from the text </summary>
	string ProcessKey() {
		int startIndex = index + 1;
		int matchQuote = -1;
		while (json[index++] != ':' || matchQuote == -1) { 
			if (json[index] == '\"' && json[index-1] != '\\') {
				matchQuote = index;
			}
		}


		string result = json.Substring(startIndex, matchQuote - startIndex).TrimEnd();
		//Debug.Log("ProcessKey: " + startIndex + "-" + index + " [" + result + "]");
		return result;
	}

	/// <summary> Logic to skip over whitespace until a non whitespace character or the end of the file. </summary>
	void SkipWhitespaceEnd() {
		while (index < json.Length && char.IsWhiteSpace(next)) { index++; }
	}
	/// <summary> Logic to skip to the next non-whitepace character </summary>
	void SkipWhitespace() {
		while (char.IsWhiteSpace(next)) { index++; }
	}

}

#endregion

#region Helpers

/// <summary> Class containing some helper functions. </summary>
public static class JsonHelpers {

	/// <summary> Escape characters to escape inside of Json text </summary>
	static string[] TOESCAPE = new string[] { "\\", "\"", "\b", "\f", "\n", "\r", "\t" };

	/// <summary> Is the type of an object is a given type? </summary>
	public static bool IsOf(this object o, Type t) { return o.GetType() == t; }
	/// <summary> Is an object is of an enum type? </summary>
	public static bool IsTypeOfEnum(this object o) { return o.GetType().BaseType == typeof(System.Enum); }
	/// <summary> Is an object of an array type? </summary>
	public static bool IsTypeOfArray(this object o) { return o.GetType().IsArray; }

	/// <summary> Replace all escapeable characters with their escaped versions. </summary>
	public static string JsonEscapeString(this string str) {
		string s = str;
		for (int i = 0; i < TOESCAPE.Length; i++) {
			string escaped = TOESCAPE[i];
			s = s.Replace(escaped, "\\" + escaped);
		}
		return s;
	}

	/// <summary> Replace all escaped characters with their unescaped versions. </summary>
	public static string JsonUnescapeString(this string str) {
		string s = str;
		for (int i = 0; i < TOESCAPE.Length; i++) {
			string escaped = TOESCAPE[TOESCAPE.Length - i - 1];
			s = s.Replace("\\" + escaped, escaped);
		}
		return s;
	}

	/// <summary> Array of numeric types </summary>
	static Type[] numericTypes = new Type[] { 
		typeof(double), 
		typeof(int), 
		typeof(float), 
		typeof(long),
		typeof(decimal), 
		typeof(byte),
		typeof(short), 
	};

	/// <summary> is a type a numeric type? </summary>
	public static bool IsNumeric(this Type type) {
		return numericTypes.Contains(type);
	}

	public static double GetNumericValue(object obj) {
		if (obj.GetType() == typeof(double)) { return (double)obj; }
		if (obj.GetType() == typeof(int)) { return (double)(int)obj; }
		if (obj.GetType() == typeof(float)) { return (double)(float)obj; }
		if (obj.GetType() == typeof(long)) { return (double)(long)obj; }
		if (obj.GetType() == typeof(decimal)) { return (double)(decimal)obj; }
		if (obj.GetType() == typeof(byte)) { return (double)(byte)obj; }
		if (obj.GetType() == typeof(short)) { return (double)(short)obj; }

		return 0;
	}

	public static bool IsObsolete(this MemberInfo info) {
		return System.Attribute.GetCustomAttribute(info, typeof(System.ObsoleteAttribute)) != null;
	}

	/// <summary> Call the constructor of a Type, calling an empty constructor if it exists. </summary>
	public static object GetNewInstance(this Type type) {
		ConstructorInfo constructor = type.GetConstructor(new Type[] {});
		if (constructor != null) {
			return constructor.Invoke(new object[] {});
		}
		return null;
	}

	/// <summary> See if a property has a SET function </summary>
	public static bool IsWritable(this PropertyInfo p) {
		MethodInfo setter = p.GetSetMethod();
		return setter != null;
	}

	/// <summary> See if a property has a GET function </summary>
	public static bool IsReadable(this PropertyInfo p) {
		MethodInfo getter = p.GetGetMethod();
		return getter != null;
	}

}
#endregion 

#region Functional Operations

/// <summary> Class containing 'functional' operations 
/// these operations process numeric information between multiple JsonObjects </summary>
public static class JsonOperations {

	/// <summary> Get a version of the given object with all of its numbers' sign changed. 
	/// lim is an optional parameter that limits what fields are used
	/// if present, all of the strings in it will be processed into the result.
	/// if absent, all of the strings that are mapped to numbers will be processed into the result. </summary>
	public static JsonObject Negate(this JsonObject obj, JsonArray lim = null) {
		JsonObject result = new JsonObject();

		if (lim == null) {
			foreach (var pair in obj) {
				if (pair.Value.isNumber) { result[pair.Key] = -pair.Value.numVal; }
			}
		} else {
			foreach (var val in lim) {
				if (val.isString) {
					result[val.stringVal] = -obj.GetNumber(val.stringVal);
				}
			}
		}

		return result;
	}

	/// <summary> Sums numbers that are inside of a JsonObject.
	/// Optionally, another parameter can be provided, lim.
	/// lim defines what keys are used in the sum. </summary>
	public static double SumOfNumbers(this JsonObject thing, JsonArray lim = null) {
		double sum = 0;

		if (lim == null) {
			foreach (var pair in thing) {
				if (pair.Value.isNumber) { sum += pair.Value.numVal; }
			}
		} else {
			foreach (var key in lim) {
				if (key.isString) {
					var val = thing[key.stringVal];
					if (val.isNumber) { sum += val.numVal; }
				}
			}
		}

		return sum;
	}

	/// <summary>
	/// Multiply two 'vectors' componentwise, and return the result.
	/// Optionally, another parameter can be provided, lim
	/// lim defines which dimensions are present in the result.
	/// without lim, the result contains the INTERSECTION between lhs and rhs as vectors.
	/// </summary>
	public static JsonObject Scale(this JsonObject lhs, JsonObject rhs, JsonArray lim = null) {
		JsonObject result = new JsonObject();

		if (lim == null) {
			foreach (var lpair in lhs) {
				string key = lpair.Key.stringVal;
				var lval = lpair.Value;

				if (lval != null && lval.isNumber) {
					var rval = rhs[key];
					if (rval.isNumber) {
						result[key] = rval.numVal * lval.numVal;
					}
				}
			}
		} else {
			foreach (var val in lim) {
				if (val.isString) {
					string key = val.stringVal;
					result[key] = lhs.GetFloat(key) * rhs.GetFloat(key);
				}
			}
		}

		return result;
	}

	/// <summary> Multiply the left side object (as a 'vector') by the right hand side object (as a 'matrix').
	/// a 'vector' is a JsonObject with only string:float value pairs considered.
	/// a 'matrix' is a JsonObject with only string:'vector' pairs considered.
	/// Optionally, another parameter can be provided, lim.
	/// lim defines what the 'dimensions' of the multiplication are.
	/// if not present, all 'dimensions' are used. </summary>
	public static JsonObject Multiply(this JsonObject lhs, JsonObject rhs, JsonArray lim = null) {
		JsonObject result = new JsonObject();

		if (lim == null) {
			foreach (var pair in rhs) {
				JsonValue val = pair.Value;
				string key = pair.Key.stringVal;
				if (val.isObject) { result[key] = lhs.MultiplyRow(val as JsonObject); }
				if (val.isNumber) { result[key] = lhs.GetNumber(key) * val.numVal; }
			}

		} else {
			foreach (var val in lim) {
				if (val.isString) {
					string key = val.stringVal;
					if (val.isObject) { result[key] = lhs.MultiplyRow(val as JsonObject); }
					if (val.isNumber) { result[key] = lhs.GetNumber(key) * rhs.GetNumber(key); }
				}
			}

		}

		return result;
	}



	/// <summary> Calculates one result of a multiplication of one 'vector' times one 'row' of a matrix </summary>
	public static double MultiplyRow(this JsonObject lhs, JsonObject rhs) {
		double d = 0;

		foreach (var pair in rhs) { 
			if (pair.Value.isNumber) { d += lhs.GetNumber(pair.Key.stringVal) * pair.Value.numVal; }
		}

		return d;
	}

	/// <summary> Creates the result of the left 'vector' plus the right 'vector' 
	/// lim is an optional parameter that can be provided to define the 'dimensions' that are used.
	/// if present, each string contained will be a 'dimension' in the result.
	/// if absent, the full range of each 'vector' will be considered.
	/// </summary>
	public static JsonObject AddNumbers(this JsonObject lhs, JsonObject rhs, JsonArray lim = null) {
		JsonObject result = new JsonObject();

		if (lim == null) {
			foreach (var pair in lhs) {
				if (pair.Value.isNumber) { result[pair.Key.stringVal] = pair.Value; }
			}

			foreach (var pair in rhs) {
				if (pair.Value.isNumber) { result[pair.Key.stringVal] = result.GetNumber(pair.Key.stringVal) + pair.Value.numVal; }
			}

		} else {
			foreach (var val in lim) {
				if (val.isString) {
					string key = val.stringVal;
					result[key] = lhs.GetNumber(key) + rhs.GetNumber(key);
				}
			}

		}

		return result;
	}

	/// <summary> Clamp a value. by default range is [0, 1]</summary>
	static double Clamp(double val, double min = 0, double max = 1) {
		if (val < min) { return min; } 
		else if ( val > max) { return max; } 
		return val;
	}

	/// <summary> Combine two 'vector' JsonObjects as if each number is a ratio between [0, 1]
	/// combines each 'dimension' as (1 - (1 - a) * (1 - b))
	/// so if one vector has .5 and the other one has .2, the result will be .6 </summary>
	public static JsonObject CombineRatios(this JsonObject lhs, JsonObject rhs, JsonArray lim = null) {
		JsonObject result = new JsonObject();

		if (lim == null) {
			foreach (var pair in lhs) {
				if (pair.Value.isNumber) { result[pair.Key.stringVal] = Clamp(pair.Value.numVal); }
			}

			foreach (var pair in rhs) {
				if (pair.Value.isNumber) { 
					double a = result.GetNumber(pair.Key.stringVal);
					double b = Clamp(pair.Value.numVal);

					result[pair.Key.stringVal] = 1 - (1 - a) * (1 - b);
				}
			}
		} else {
			foreach (var val in lim) {
				if (val.isString) {
					string key = val.stringVal;
					double a = Clamp(lhs.GetNumber(key));
					double b = Clamp(rhs.GetNumber(key));
					result[key] = 1 - (1 - a) * (1 - b);
				}
			}

		}

		return result;
	}

	/// <summary> Gets a list of keys from a JsonObject that match a given rule </summary>
	public static JsonArray GetMatchingKeys(this JsonObject obj, JsonObject rule = null) {
		if (rule == null) { rule = new JsonObject(); }
		JsonArray result = new JsonArray();

		string prefix = rule.Extract<string>("prefix", "");
		string suffix = rule.Extract<string>("suffix", "");
		string contains = rule.Extract<string>("contains", "");

		foreach (var pair in obj) {
			string key = pair.Key.stringVal;

			if ( ("" == prefix || key.StartsWith(prefix)) 
				&& ("" == suffix || key.EndsWith(suffix))
				&& ("" == contains || key.Contains(contains))) {

				result.Add(key);
			}

		}


		return result;
	}



}

#endregion


