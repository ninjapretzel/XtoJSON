/*	XtoJSON
	Lightweight JSON Library for C#
	2015-2020  Jonathan Cohen
	Contact: ninjapretzel@yahoo.com

	Copyright (c) 2020 Jonathan Cohen aka ninjapretzel

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all
	copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
	SOFTWARE.
*/

//Definition Flags:

// #define XtoJSON_StrictCommaRules 
//		Enabled - When parsing JSON, makes commas before end-group characters cause an exception.
//		Disabled - Commas before end-group characters are allowed
// Example: { "blah":"bluh", } 
//		the above JSON will cause an exception to be thrown with the flag enabled, 
//		and will parse successfully with the flag disabled.

// #define XtoJSON_StringNumbers
//		Enabled - numbers are stored internally as a string value, and are converted to and from number types
//		Disabled - numbers are stored internally as a double value, and are parsed from a string when converted from anything other than a double
// May have minor performance implications when enabled.
//		Alternatively, if you want to use floats instead of doubles:
// #define XtoJSON_FloatNumbers
//		Enabled - numbers are stored internally as a float value.
// 

// #define XtoJSON_ConcurrentObjects - Deprecated.
//		Has no effect as of 2.0.0
//		ConcurrentDictionary can be used as an internal data storage with by providing an IDictionary<JsonString, JsonValue> generating function to JsonObject.DictionaryGenerator
// Previously:
//		Enabled - JsonObjects internally use ConcurrentDictionary<,> to hold Key/Value pairs.
//		Disabled - JsonObjects internally use Dictionary<,> to hold Key/Value pairs.
//	

// Hook into some other useful diagnostic stuff
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Linq;
using System.Text; // Needed when paired alongside ZSharp, since StringBuilder is wrapped (outside of namespace)
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

#region Abstract/Primary stuff

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonType

/// <summary>Enum of all types supported by XtoJSON</summary>
public enum JsonType {
	/// <summary> Represents a string value </summary>
	String,
	/// <summary> Represents a true/false value </summary>
	Boolean,
	/// <summary> Represents a numeric value </summary>
	Number,
	/// <summary> Represents an arbitrary object type </summary>
	Object,
	/// <summary> Represents an array of arbitrary values </summary>
	Array,
	/// <summary> Represents a missing value </summary>
	Null,
	/// <summary> Represents a function value </summary>
	Function,
}

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//Json

/// <summary> Quick access to Json parsing and reflection, and other information  </summary>
public static class Json {

	/// <summary> Major version number </summary>
	public const int MAJOR = 2;
	/// <summary> Minor version number </summary>
	public const int MINOR = 6;
	/// <summary> Sub-minor version Revision number </summary>
	public const int REV = 2;

	/// <summary> String representation of current version of library </summary>
	public static string VERSION { get { return MAJOR + "." + MINOR + "." + REV; } }

	/// <summary> Parse a json string into its JsonValue representation. </summary>
	/// <param name="json"> JSON formatted string to parse </param>
	/// <returns> <see cref="JsonValue"/> result, otherwise null. </returns>
	public static JsonValue Parse(string json) {
		JsonDeserializer jds = new JsonDeserializer(json);
		return jds.Deserialize();
	}

	/// <summary> Parses a JSON formatted string directly into a <see cref="JsonValue"/> derivitive type. </summary>
	/// <typeparam name="T"> Expected result type. </typeparam>
	/// <param name="json"> JSON formatted string to parse </param>
	/// <returns> Object of type T, or null. </returns>
	public static T Parse<T>(string json) where T : JsonValue {
		JsonDeserializer jds = new JsonDeserializer(json);
		JsonValue val = jds.Deserialize();
		return (val is T) ? ((T)val) : null;
	}

	/// <summary> Parses a JSON formatted string using strict rules directly into a <see cref="JsonValue"/> derivitive type.  </summary>
	/// <param name="json"> JSON formatted string to parse </param>
	/// <returns> <see cref="JsonValue"/> result, otherwise throws a <see cref="JsonDeserializeFailedException"/>. </returns>
	public static JsonValue ParseStrict(string json) {
		JsonDeserializer jds = new JsonDeserializer(json);
		return jds.DeserializeStrict();
	}
	/// <summary> Parses a JSON formatted string directly into a <see cref="JsonValue"/> derivitive type. </summary>
	/// <typeparam name="T"> Expected result type. </typeparam>
	/// <param name="json"> JSON formatted string to parse </param>
	/// <returns> Object of type T, or null. </returns>
	public static T ParseStrict<T>(string json) where T : JsonValue {
		JsonDeserializer jds = new JsonDeserializer(json);
		JsonValue val = jds.DeserializeStrict();

		return (val is T) ? ((T)val) : throw new Exception($"Type {typeof(T)} expected, parsed {val.GetType()}");
	}

	/// <summary> Parses a string, directly into an arbitrary type. Hurr, get it? Json.To&lt;X&gt;</summary>
	/// <typeparam name="X"> Expected result type. </typeparam>
	/// <param name="json"> JSON formatted string to parse </param>
	/// <returns> Object of type T, from data in <paramref name="json"/> </returns>
	public static X To<X>(string json) {
		JsonDeserializer jds = new JsonDeserializer(json);
		JsonValue val = jds.Deserialize();
		return GetValue<X>(val);
	}

	/// <summary> Converts pretty much anything to a JSON string representation of itself </summary>
	/// <param name="obj"> Value to convert to Json. </param>
	/// <returns> Json string representation of <paramref name="obj"/>. </returns>
	public static string ToJson(object obj) {
		JsonValue val = Reflect(obj);
		return val.ToString();
	}

	/// <summary> Trys to parse a json string into a JsonValue representation. 
	/// if it fails, catches the exception that was thrown, and returns a null</summary>
	public static JsonValue TryParse(string json) {
		try { return Parse(json); } catch {
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

	/// <summary> Convert some object to a JSON string </summary>
	/// <param name="o">object to convert</param>
	/// <returns>Json-formed string or element representing the object parameter</returns>
	public static string Serialize(object o) {
		JsonValue val = Reflect(o);
		return val.PrettyPrint();
	}

	/// <summary> Deserializes a JSON string into an object of a given type. </summary>
	/// <typeparam name="T">Generic type to deserialize as</typeparam>
	/// <param name="json">Json-Format data string</param>
	/// <returns>A 'T' object deserialized from the given string.</returns>
	public static T Deserialize<T>(string json) {
		JsonValue val = Parse(json);
		return GetValue<T>(val);
	}

	/// <summary>
	/// Creates a clone of the object by reflecting its internal values, and constructing a new object
	/// using the same values.
	/// </summary>
	/// <typeparam name="T">Type of object to get back</typeparam>
	/// <param name="o">Source data to use</param>
	/// <returns>A T object with the data provided by o</returns>
	public static T Clone<T>(object o) {
		JsonValue val = Reflect(o);
		return GetValue<T>(val);
	}
	
	public static readonly IDictionary<Type, JsonType> DEFAULT_EXPECTED_REFLECTIONS = new ReadOnlyDictionary<Type, JsonType>(
		new Dictionary<Type, JsonType>() {
			{ typeof(string), JsonType.String },
			{ typeof(Guid), JsonType.String },
			{ typeof(DateTime), JsonType.String },
			{ typeof(bool), JsonType.Boolean },
			{ typeof(byte), JsonType.Number },
			{ typeof(sbyte), JsonType.Number },
			{ typeof(short), JsonType.Number },
			{ typeof(ushort), JsonType.Number },
			{ typeof(int), JsonType.Number },
			{ typeof(uint), JsonType.Number },
			{ typeof(long), JsonType.Number },
			{ typeof(ulong), JsonType.Number },
			{ typeof(float), JsonType.Number },
			{ typeof(double), JsonType.Number },
			{ typeof(decimal), JsonType.Number },
		}
	);
	/// <summary> Get the expected reflected <see cref="JsonType"/> of a given <see cref="Type"/></summary>
	public static JsonType ExpectedReflectedType(Type t) {
		if (DEFAULT_EXPECTED_REFLECTIONS.ContainsKey(t)) { return DEFAULT_EXPECTED_REFLECTIONS[t]; }
		if (t.IsArray) { return JsonType.Array; }
		if (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(List<>))) { return JsonType.Array; }
		return JsonType.Object;
	}

	/// <summary> Get the expected type of the reflection of a code object. </summary>
	public static JsonType ReflectedType(object o) {
		if (o == null) { return JsonType.Null; }
		Type t = o.GetType();
		return ExpectedReflectedType(t);
	}


}

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonValue

/// <summary> Base class for all representations of Json values </summary>
public abstract class JsonValue {

	/// <summary> Base JsonNull null reference </summary>
	public static JsonValue NULL { get { return JsonNull.instance; } }

	/// <summary> Hidden constructor. </summary>
	internal JsonValue() { }

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
	/// <summary> Is this a function? </summary>
	public bool isFunction { get { return JsonType == JsonType.Function; } }

	/// <summary> How many items are in this JsonValue, given it is a collection? </summary>
	public virtual int Count { get { throw new InvalidOperationException("This JsonValue is not a collection"); } }

	/// <summary>Indexes the JsonValue with another JsonValue as the index</summary>
	/// <param name="index">JsonValue to use as an index. Converted to a string for JsonObject, and converted to an int for JsonArray </param>
	/// <returns>Item at the given index, if the type can be indexed. </returns>
	public virtual JsonValue this[JsonValue index] {
		get { throw new Exception(this.JsonType.ToString() + " Cannot be indexed!"); }
		set { throw new Exception(this.JsonType.ToString() + " Cannot be indexed!"); }
	}

	/// <summary> Does this JsonValue have a given key, when treated as a JsonObject </summary>
	public virtual bool ContainsKey(JsonString key) { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }
	/// <summary> Does this JsonValue contain all of the keys in a given array, when treated as a JsonObject </summary>
	public virtual bool ContainsAllKeys(params JsonString[] keys) { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }
	/// <summary> Does this JsonValue contain any of the keys in a given array, when treated as a JsonObject </summary>
	public virtual bool ContainsAnyKeys(params JsonString[] keys) { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }

	/// <summary> Treat this JsonValue as a JsonObject, and retrieve a string at a given key </summary>
	public string GetString(JsonString key) {
		if (ContainsKey(key)) {
			JsonValue thing = this[key];
			if (thing.isString) { return thing.stringVal; }
		}
		return "";
	}

	/// <summary> Treat this JsonValue as a JsonObject, and retrieve a bool at a given key </summary>
	public bool GetBoolean(JsonString key) {
		if (ContainsKey(key)) {
			JsonValue thing = this[key];
			if (thing.isBool) { return thing.boolVal; }
		}
		return false;
	}

	/// <summary> Treat this JsonValue as a JsonObject, and retrieve a float at a given key. </summary>
	public float GetFloat(JsonString key) { return (float)GetNumber(key); }
	/// <summary> Treat this JsonValue as a JsonObject, and retrieve an int at a given key. </summary>
	public int GetInt(JsonString key) { return (int)GetNumber(key); }
	/// <summary> Treat this JsonValue as a JsonObject, and retrieve a double at a given key. </summary>
	public double GetNumber(JsonString key) {
		if (ContainsKey(key)) {
			JsonValue thing = this[key];
			if (thing.isNumber) { return thing.numVal; }
		}
		return 0;
	}
	/// <summary> Get the boolean value of this JsonValue </summary>
	public virtual bool boolVal {
		get {
			switch (JsonType) {
				case JsonType.Array:
				case JsonType.Object:
				case JsonType.Function:
					return true;
				case JsonType.String:
					return stringVal.Length > 0;
				case JsonType.Number:
					double val = numVal;
					return val != 0 && !double.IsNaN(val);
				case JsonType.Null:
				default:
					return false;

			}
		}
	}
	/// <summary> Get the double value of this JsonValue </summary>
	public virtual double numVal { get { throw new InvalidOperationException("This JsonValue is not a number, it is a " + JsonType); } }
	/// <summary> Get the float value of this JsonValue </summary>
	public virtual float floatVal { get { throw new InvalidOperationException("This JsonValue is not a number, it is a " + JsonType); } }
	/// <summary> Get the double value of this JsonValue </summary>
	public virtual double doubleVal { get { throw new InvalidOperationException("This JsonValue is not a number, it is a " + JsonType); } }
	/// <summary> Get the integer value of this JsonValue </summary>
	public virtual int intVal { get { throw new InvalidOperationException("This JsonValue is not a number, it is a " + JsonType); } }
	/// <summary> Get the string value of this JsonValue </summary>
	public virtual string stringVal { get { throw new InvalidOperationException("This JsonValue is not a string, it is a " + JsonType); } }

	/// <summary> Get the JsonType of this JsonValue. Fixed, based on the subclass. </summary>
	public abstract JsonType JsonType { get; }
	/// <summary> Get the string representation of this JsonValue. </summary>
	public abstract override string ToString();
	/// <summary> Get a pretty string representation of this JsonValue. Defaults to indentLevel = 0 </summary>
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

	/// <summary> implicit conversion from JsonValue to string </summary>
	public static implicit operator string(JsonValue val) { if (val == null) { val = NULL; } return val.stringVal; }
	/// <summary> implicit conversion from JsonValue to bool </summary>
	public static implicit operator bool(JsonValue val) { if (val == null) { val = NULL; } return val.boolVal; }
	/// <summary> implicit conversion from JsonValue to double </summary>
	public static implicit operator double(JsonValue val) { if (val == null) { val = NULL; } return val.numVal; }
	/// <summary> implicit conversion from JsonValue to decimal </summary>
	public static implicit operator decimal(JsonValue val) { if (val == null) { val = NULL; } return (decimal)val.numVal; }
	/// <summary> implicit conversion from JsonValue to float </summary>
	public static implicit operator float(JsonValue val) { if (val == null) { val = NULL; } return (float)val.numVal; }
	/// <summary> implicit conversion from JsonValue to int </summary>
	public static implicit operator int(JsonValue val) { if (val == null) { val = NULL; } return (int)val.numVal; }

	/// <summary> Plus operator on JsonValues </summary>
	/// <param name="lhs">Left Hand Side</param>
	/// <param name="rhs">Right Hand Side</param>
	/// <returns> Javascript equivlent of LHS + RHS </returns>
	public static JsonValue operator +(JsonValue lhs, JsonValue rhs) {
		if (lhs == null) { lhs = NULL; }
		if (rhs == null) { rhs = NULL; }
		if (lhs.isString || rhs.isString) { return lhs.stringVal + rhs.stringVal; }
		return lhs.numVal + rhs.numVal;
	}

	/// <summary> Minus operator on JsonValues </summary>
	/// <param name="lhs">Left Hand Side</param>
	/// <param name="rhs">Right Hand Side</param>
	/// <returns> Javascript equivlent of LHS - RHS </returns>
	public static JsonValue operator -(JsonValue lhs, JsonValue rhs) {
		if (lhs == null) { lhs = NULL; }
		if (rhs == null) { rhs = NULL; }
		return lhs.numVal - rhs.numVal;
	}

	/// <summary> Minus operator on JsonValues </summary>
	/// <param name="lhs">Left Hand Side</param>
	/// <param name="rhs">Right Hand Side</param>
	/// <returns> Javascript equivlent of LHS * RHS </returns>
	public static JsonValue operator *(JsonValue lhs, JsonValue rhs) {
		if (lhs == null) { lhs = NULL; }
		if (rhs == null) { rhs = NULL; }
		return lhs.numVal * rhs.numVal;
	}

	/// <summary> Minus operator on JsonValues </summary>
	/// <param name="lhs">Left Hand Side</param>
	/// <param name="rhs">Right Hand Side</param>
	/// <returns> Javascript equivlent of LHS / RHS </returns>
	public static JsonValue operator /(JsonValue lhs, JsonValue rhs) {
		if (lhs == null) { lhs = NULL; }
		if (rhs == null) { rhs = NULL; }

		if (rhs.numVal == 0) {
			if (lhs > 0) { return double.PositiveInfinity; }
			if (lhs < 0) { return double.NegativeInfinity; }
			return double.NaN;
		}

		return lhs.numVal / rhs.numVal;
	}

	/// <summary> not-equal operator for JsonValues </summary>
	/// <param name="a">Left Hand Side</param>
	/// <param name="b">Right Hand Side</param>
	/// <returns>the inversion of (a == b) </returns>
	public static bool operator !=(JsonValue a, object b) { return !(a == b); }

	/// <summary> Maximum PERCENTAGE difference for two JsonNumbers to be considered equal. Default is 1E-16, or .0000000000001% </summary>
	static double NUMBER_TOLERANCE = 1E-20;

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
		// Handle nulls before attempting to do anything else...
		if (ReferenceEquals(a, null) || ReferenceEquals(a, JsonNull.instance)) {
			//if a is null, they can only be equal if both are null...
			return ReferenceEquals(b, null) || ReferenceEquals(b, JsonNull.instance);
		} else if (ReferenceEquals(b, null) || ReferenceEquals(b, JsonNull.instance)) {
			//if a is not null, and b is null, then they cannot be equal...
			return false;
		}
		
		// Handle 'same object' comparisons early.
		if (ReferenceEquals(a, b)) { return true; }

		// Both things are not null at this point...
		switch (a.JsonType) {
			case JsonType.Number:
				// Numbers compared within tolerance
				// Default compared to double.NaN
				double aVal = a.doubleVal;
				double bVal = double.NaN;
				if (b.GetType().IsNumeric()) { bVal = JsonHelpers.GetNumericValue(b); }
				else if (b is JsonNumber) { bVal = (b as JsonNumber).doubleVal; }
				else { return false; }

				// If both are the same NaN/+Inf/-Inf, they are equal.
				if (double.IsNaN(aVal) && double.IsNaN(bVal)) { return true; }
				if (double.IsPositiveInfinity(aVal) && double.IsPositiveInfinity(bVal)) { return true; }
				if (double.IsNegativeInfinity(aVal) && double.IsNegativeInfinity(bVal)) { return true; }
				// If One is infinity/nan, the other isn't, or isn't the same, not equal. 
				if (double.IsInfinity(aVal) || double.IsInfinity(bVal) || double.IsNaN(aVal) || double.IsNaN(bVal)) {
					return false;
				}

				if (aVal == bVal) { return true; }

				double s = aVal + bVal;
				double d = aVal - bVal;
				if (s == 0) { return false; }

				// Check Percentage between sum/difference
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

			case JsonType.Function:
			case JsonType.Object:
			case JsonType.Array:
			default:
				// functions Objects and Arrays with different addresses are not considered equal by '=='
				return false;
		}

	}

	/// <summary>
	/// Equality Comparison from any JsonValue type to any object
	/// This is more in depth than a plain '==' comparison, which defaults to references
	/// Recursively checks for equality on arrays/objects.
	/// </summary>
	public override bool Equals(object b) {
		if (ReferenceEquals(this, b)) { return true; }
		if (ReferenceEquals(b, null) && !ReferenceEquals(this, JsonNull.instance)) { return false; }

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
						if (!this[i].Equals(arr[i])) { return false; }
					}
					return true;
				}
				return false;
			case JsonType.Object:
				if (b is JsonObject) {
					JsonObject obj = b as JsonObject;
					
					// Apparantly the ConcurrentDictionary.Count property can be obnoxiously slow
					// so we should avoid comparing counts on Concurrent Objects.
					if (Count != obj.Count) { return false; }
					
					int i = 0;
					foreach (var pair in obj) {
						JsonString key = pair.Key;
						JsonValue val = pair.Value;
						if (!ContainsKey(key)) { return false; }
						if (!val.Equals(this[key])) { return false; }
						i++;
					}
					return true;
				}
				return false;
			case JsonType.Function:
				return (ReferenceEquals(this, b)); // Functions are equal by reference only 
			default: // Default - a is JsonNull.instance
				return (b == null || ReferenceEquals(b, JsonNull.instance));
		}
	}

	/// <summary> Uses base implementation of GetHashCode. This method is overidden here since Equals is overidden, but only objects/arrays will inherit this version. </summary>
	/// <returns> Base 'object' hash code. </returns>
	public override int GetHashCode() {
		return base.GetHashCode();
	}

	/// <summary> Creates a 'deep copy' of this object, in terms of JsonObject and JsonArray references. </summary>
	/// <returns> Deep copy of this object. </returns>
	public virtual JsonValue DeepCopy() {
		// Create a deep copy of every 
		if (isObject) {
			JsonObject copy = new JsonObject();
			foreach (var pair in this as JsonObject) {
				var key = pair.Key;
				var val = pair.Value;
				if (val.isObject || val.isArray) { val = val.DeepCopy(); }
				copy[key] = val;
			}
			return copy;
		}
		if (isArray) {
			JsonArray copy = new JsonArray();
			foreach (var val in this as JsonArray) {
				var v = val;
				if (v.isObject || v.isArray) { v = v.DeepCopy(); }
				copy.Add(v);
			}
			return copy;
		}

		// Non-objects and non-arrays are immutable, and trivially deepcopied.
		return this;
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonFunction
/// <summary> Represents a function in the context of a JsonObject, with parameters contained in a JsonArray. </summary>
public class JsonFunction : JsonValue {

	/// <summary> Empty Function that does nothing and returns null. </summary>
	public static readonly Func<JsonValue, JsonObject, JsonArray, JsonValue> EMPTY_FUNCTION = (theThis, context, prams) => JsonNull.instance;

	/// <summary> Function reference. Parameters are (context, params), returns a JsonValue </summary>
	private Func<JsonValue, JsonObject, JsonArray, JsonValue> func;

	/// <summary> A context object that has been bound to this function, if any. </summary>
	private JsonObject boundContext = null;

	/// <summary> The 'this' that has been bound to this function, if any. </summary>
	private JsonValue boundThis = null;

	/// <summary> Default constructor that wraps <see cref="EMPTY_FUNCTION"/> </summary>
	public JsonFunction() { func = EMPTY_FUNCTION; }
	
	/// <summary> Constructor that wraps a function just returning a JsonValue. </summary>
	/// <param name="func"> Function to wrap that takes no context/parameters and returns a JsonValue. </param>
	public JsonFunction(Func<JsonValue> func) { this.func = (theThis, context, prams) => { return func(); }; }

	/// <summary> Constructor that wraps a function taking a 'context' JsonObject and returning a JsonValue. </summary>
	/// <param name="wrappedFunc"> Function that takes a context, but no parameters or 'this', and returns a JsonValue. </param>
	public JsonFunction(Func<JsonObject, JsonValue> wrappedFunc) { func = (theThis, context, prams) => { return wrappedFunc(context); }; }
	
	/// <summary> Constructor that wraps a function taking a parameters JsonArray and returning a JsonValue. </summary>
	/// <param name="wrappedFunc"> Function that takes parameters, but no 'context' or 'this', and returns a JsonValue. </param>
	public JsonFunction(Func<JsonArray, JsonValue> wrappedFunc) { func = (theThis, context, prams) => { return wrappedFunc(prams); }; }

	/// <summary> Constructor that wraps a function taking a 'context' JsonObject, parameters JsonArray, and returns a JsonValue. </summary>
	/// <param name="wrappedFunc"> Function that takes a context and parameters, but no this </param>
	public JsonFunction(Func<JsonObject, JsonArray, JsonValue> wrappedFunc) { func = (theThis, context, prams) => { return wrappedFunc(context, prams); }; }

	/// <summary> Constructor that wraps a function taking a 'this' JsonValue, 'context', JsonObject and 'params' JsonArray, and returning a JsonValue. </summary>
	/// <param name="wrappedFunc"> Function that takes a JsonObject context, and JsonArray parameters, and returns a JsonValue. </param>
	public JsonFunction(Func<JsonValue, JsonObject, JsonArray, JsonValue> wrappedFunc) { func = (theThis, context, prams) => { return wrappedFunc(theThis, context, prams); }; } //(theThis, context, prams) => { return func(theThis, context, prams); }; }

	/// <summary> Invokes the wrapped function with the given <paramref name="context"/> and <paramref name="prams"/>. </summary>
	/// <param name="context"> JsonObject holding the context of the invocation, if one has not been bound. </param>
	/// <param name="prams"> Parameters passed into the function </param>
	/// <returns> JsonValue generated by the function call </returns>
	public JsonValue Invoke(JsonObject context, JsonArray prams) {
		return func(boundThis, context ?? boundContext, prams);
	}

	/// <summary> Create a copy of the function, bound to a given 'this' object. </summary>
	/// <param name="theThis"> 'this' object for the function to be bound to </param>
	/// <returns> Copy of this JsonFunction, bound to a given context object. </returns>
	public JsonFunction BindThis(JsonValue theThis) {
		JsonFunction copy = new JsonFunction();
		copy.func = func;
		copy.boundContext = boundContext;
		copy.boundThis = theThis;
		return copy;
	}

	public JsonFunction UnbindThis() {
		JsonFunction copy = new JsonFunction();
		copy.func = func;
		copy.boundContext = boundContext;
		copy.boundThis = null;
		return copy;
	}

	/// <summary> Creates a copy of this function, bound to a given context </summary>
	/// <param name="context"> Context object to bind the function to </param>
	/// <returns> Copy of this JsonFunction, bound to a given context object. </returns>
	public JsonFunction BindContext(JsonObject context) {
		JsonFunction copy = new JsonFunction();
		copy.func = func;
		copy.boundContext = context;
		copy.boundThis = boundThis;
		return copy;
	}

	/// <summary> Pretty sure this is yagni, but creates a copy of this function, bound to no context. </summary>
	/// <returns> Copy of this JsonFunction, with no bound context object. </returns>
	public JsonFunction UnbindContext() {
		JsonFunction copy = new JsonFunction();
		copy.func = func;
		copy.boundContext = null;
		copy.boundThis = boundThis;
		return copy;
	}
	

	/// <inheritdoc />
	public override JsonType JsonType { get { return JsonType.Function; } }

	/// <inheritdoc />
	public override string PrettyPrint() { return "func()=>{}"; }

	/// <inheritdoc />
	public override string ToString() { return "func()=>{}"; }
}

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonValueCollection

/// <summary> Base class for JsonValues that hold a group of objects </summary>
[System.Obsolete("No reason to have this abstract base class. No Real functionality needs to be defined here. Please use JsonObject or JsonArray as types instead.")]
public abstract class JsonValueCollection : JsonValue {
	/// <summary> Internal value separator </summary>
	[System.Obsolete("Class is marked obsolete")]
	protected readonly string JsonVALUE_SEPARATOR = ",";
	/// <summary> Hidden internal constructor </summary>
	[System.Obsolete("Class is marked obsolete")]
	internal JsonValueCollection() { }
	/// <summary> Create a pretty string representation of this collection </summary>
	[System.Obsolete("Class is marked obsolete")]
	protected abstract string CollectionToPrettyPrint();
	/// <summary> Create a compact string representation of this collection </summary>
	[System.Obsolete("Class is marked obsolete")]
	protected abstract string CollectionToString();
	/// <summary> Create a compact string representation of this collection </summary>
	public override string ToString() { return BeginMarker + CollectionToString() + EndMarker; }
	/// <summary> Create a pretty string representation of this collection </summary>
	public override string PrettyPrint() {
		throw new NotSupportedException("Obsolete method, use JsonValue.PrettyPrint() instead.");
	}
	/// <summary> Definition for begining marker character </summary>
	[System.Obsolete("Class is marked obsolete")]
	protected abstract string BeginMarker { get; }
	/// <summary> Definition for ending marker character </summary>
	[System.Obsolete("Class is marked obsolete")]
	protected abstract string EndMarker { get; }
}

#endregion

#region Primitives

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonNull

/// <summary> Represents a null as a JsonObject </summary>
public class JsonNull : JsonValue {
	/// <summary> internal representatino </summary>
	public string _value { get { return "null"; } }

	/// <summary> Reference to only instance of JsonNull </summary>
	public static readonly JsonNull instance = new JsonNull();
	
	/// <summary> private constructor </summary>
	private JsonNull() : base() { }

	/// <inheritdoc />
	public override double numVal { get { return 0; } }
	/// <inheritdoc />
	public override double doubleVal { get { return 0; } }
	/// <inheritdoc />
	public override float floatVal { get { return 0; } }
	/// <inheritdoc />
	public override int intVal { get { return 0; } }

	/// <inheritdoc />
	public override string stringVal { get { return "null"; } }
	/// <inheritdoc />
	public override JsonType JsonType { get { return JsonType.Null; } }
	/// <inheritdoc />
	public override string ToString() { return _value; }
	/// <inheritdoc />
	public override string PrettyPrint() { return _value; }

}

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonBool

/// <summary> bool type represented as a JsonValue. </summary>
public class JsonBool : JsonValue {
	/// <summary> internal representation </summary>
	private bool _value;

	/// <summary> 'true' instance </summary>
	public static JsonBool TRUE = new JsonBool(true);
	/// <summary> 'false' instance </summary>
	public static JsonBool FALSE = new JsonBool(false);

	/// <inheritdoc />
	public override string stringVal { get { return _value ? "true" : "false"; } }
	/// <inheritdoc />
	public override int intVal { get { return _value ? 1 : 0; } }
	/// <inheritdoc />
	public override double numVal { get { return _value ? 1 : 0; } }
	/// <inheritdoc />
	public override float floatVal { get { return _value ? 1 : 0; } }
	/// <inheritdoc />
	public override double doubleVal { get { return _value ? 1 : 0; } }
	/// <inheritdoc />
	public override bool boolVal { get { return _value; } }
	/// <inheritdoc />
	public override JsonType JsonType { get { return JsonType.Boolean; } }

	/// <summary> Implicit conversion from bool to JsonBool </summary>
	public static implicit operator JsonBool(bool val) { return val ? TRUE : FALSE; }
	/// <summary> Use a boolean value to access one of the JsonBool instances </summary>
	public static JsonBool Get(bool val) { return val ? TRUE : FALSE; }

	/// <summary> Private constructor </summary>
	private JsonBool(bool value) : base() { _value = value; }
	
	/// <inheritdoc />
	public override string ToString() { return stringVal; }
	/// <inheritdoc />
	public override string PrettyPrint() { return stringVal; }

}

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonNumber

/// <summary> Representation of a number as a JsonValue </summary>
public class JsonNumber : JsonValue {
	/// <inheritdoc />
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

	/// <inheritdoc />
	public override double numVal { get { return Double.Parse(_value); } }
	/// <inheritdoc />
	public override double doubleVal { get { return Double.Parse(_value); } }
	/// <inheritdoc />
	public override float floatVal { get { return Single.Parse(_value); } }
	/// <inheritdoc />
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

	/// <inheritdoc />
	public override string ToString() { return ""+_value; }
	/// <inheritdoc />
	public override string PrettyPrint() { return ""+_value; }

#elif X_TO_JSON_FloatNumbers
	
	/// <summary> Internal representation </summary>
	private float _value;

	/// <inheritdoc />
	public override string stringVal { get { return _value.ToString("###0.#"); } }
	/// <inheritdoc />
	public override double numVal { get { return _value; } }
	/// <inheritdoc />
	public override double doubleVal { get { return _value; } }
	/// <inheritdoc />
	public override float floatVal { get { return _value; } }
	/// <inheritdoc />
	public override int intVal { get { return (int)_value; } }

	/// <summary> float constructor </summary>
	public JsonNumber(float value) { _value = value; }

	/// <summary> Internal hidden constructor </summary>
	internal JsonNumber(double value) : base() { _value = (float)value; }
	/// <summary> int constructor </summary>
	public JsonNumber(int value) : this(float.Parse("" + value)) { }
	/// <summary> decimal constructor </summary>
	public JsonNumber(decimal value) : this(float.Parse("" + value)) { }
	/// <summary> byte constructor </summary>
	public JsonNumber(byte value) : this(float.Parse("" + value)) { }

	/// <inheritdoc />
	public override string ToString() { return _value.ToString(formatter); }
	/// <inheritdoc />
	public override string PrettyPrint() { return _value.ToString(formatter); }

#else
	/// <summary> Internal representation </summary>
	private double _value;

	/// <inheritdoc />
	public override string stringVal { get { return _value.ToString("###0.#"); } }
	/// <inheritdoc />
	public override double numVal { get { return _value; } }
	/// <inheritdoc />
	public override double doubleVal { get { return _value; } }
	/// <inheritdoc />
	public override float floatVal { get { return (float)_value; } }
	/// <inheritdoc />
	public override int intVal { get { return (int)_value; } }

	/// <summary> Internal hidden constructor </summary>
	internal JsonNumber(double value) : base() { _value = value; }

	/// <summary> int constructor </summary>
	public JsonNumber(int value) : this(Double.Parse("" + value)) { }
	/// <summary> float constructor </summary>
	public JsonNumber(float value) : this(Double.Parse("" + value)) { }
	/// <summary> decimal constructor </summary>
	public JsonNumber(decimal value) : this(Double.Parse("" + value)) { }
	/// <summary> byte constructor </summary>
	public JsonNumber(byte value) : this(Double.Parse("" + value)) { }

	/// <inheritdoc />
	public override string ToString() { return _value.ToString(formatter); }
	/// <inheritdoc />
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

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonString

/// <summary> Representation of a string as a JsonValue </summary>
public class JsonString : JsonValue {
	/// <summary> Constant for String interning </summary>
	private const int MAX_INTERN_SIZE = 32;
	/// <summary> Internal representation </summary>
	private string _value;

	
	/// <inheritdoc />
	public override double numVal {
		get {
			double d = 0;
			if (Double.TryParse(_value, out d)) {
				return d;
			}
			if (_value.ToLower() == "infinity") { return double.PositiveInfinity; }
			if (_value.ToLower() == "-infinity") { return double.NegativeInfinity; }

			return double.NaN;
		}
	}
	
	/// <inheritdoc />
	public override double doubleVal { get { return numVal; } }
	/// <inheritdoc />
	public override int intVal { get { return (int)numVal; } }
	/// <inheritdoc />
	public override float floatVal { get { return (float)numVal; } }

	/// <inheritdoc />
	public override string stringVal { get { return _value; } }
	/// <inheritdoc />
	public override JsonType JsonType { get { return JsonType.String; } }

#if !NO_INTERNS
	/// <summary> Constructor </summary>
	public JsonString(string value) : base() { _value = value.Length <= MAX_INTERN_SIZE ? string.Intern(value) : value; }
	/// <summary> Interns JsonString instances to reduce RAM usage and garbage collection. </summary>
	private static readonly ConcurrentDictionary<string, JsonString> INTERNS = new ConcurrentDictionary<string, JsonString>();
	/// <summary> Implicit conversion from string to JsonString </summary>
	public static implicit operator JsonString(string val) { 
		if (val.Length > MAX_INTERN_SIZE) { return new JsonString(val); }
		if (!INTERNS.ContainsKey(val)) { INTERNS[val] = new JsonString(val); }
		return INTERNS[val]; 
	}
#else
	/// <summary> Constructor </summary>
	public JsonString(string value) : base() { _value = value; }
	/// <summary> Implicit conversion from string to JsonString </summary>
	public static implicit operator JsonString(string val) { return new JsonString(val); }
#endif

	/// <summary> Implicit conversion from JsonString to string </summary>
	public static implicit operator string(JsonString val) { return val._value; }


	/// <summary> Get the hash code of this object. Wraps through to the string inside of it. </summary>
	public override int GetHashCode() { return _value.GetHashCode(); }

	/// <inheritdoc />
	public override string ToString() { return ToJsonString(_value); }

	/// <inheritdoc />
	public override string PrettyPrint() { return ToString(); }

	/// <summary> Conversion for representation inside of Json </summary>
	public static string ToJsonString(string text) {
		if (text == null) { return "\"\""; }
		return "\"" + text.JsonEscapeString() + "\"";
	}

}

#endregion

#region Composites

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonObject

/// <summary> Representation of arbitrary object types as JsonObjects </summary>
public class JsonObject : JsonValue, IEnumerable<KeyValuePair<JsonString, JsonValue>> {

	/// <summary>
	/// Parses a standard CSV-format spreadsheet into a JsonObject.
	/// Requires that the top row is a header row with names for the columns.
	/// Each row in the CSV becomes another JsonObject, inserted into the first object
	/// based on the value in a specified column.
	/// For any row, if a column is not present, it does not add a key/value pair for that column.
	/// The separator can also be specified.
	/// Defaults to using the left-most column as the index, and a comma (',') as the separator.
	/// </summary>
	/// <param name="csv">string containing CSV formatted spreadsheet to parse</param>
	/// <param name="sep">separator character. defaults to ','</param>
	/// <param name="keyIndex">index of column to use as a 'key'</param>
	/// <returns>CSV formatted spreadsheet converted into a JsonObject</returns>
	public static JsonObject ParseCSV(string csv, char sep = ',', int keyIndex = 0) {
		JsonObject ret = new JsonObject();

		string[] lines = csv.Split('\n');
		string[] keys = lines[0].Split(sep);
		for (int i = 0; i < keys.Length; i++) { keys[i] = keys[i].Trim(); }

		for (int i = 1; i < lines.Length; i++) {
			JsonObject obj = new JsonObject();
			string line = lines[i];
			if (line.Length <= 2) { continue; }
			if (line.StartsWith("#")) { continue; }

			string[] content = line.Split(sep);
			string objkey = content[keyIndex];

			for (int k = 0; k < keys.Length && k < content.Length; k++) {
				string str = content[k];

				if (str != null && str != "") {
					string key = keys[k];
					double val = 0;
					if (double.TryParse(str, out val)) {
						obj[key] = val;
					} else if (str.ToLower() == "true") {
						obj[key] = true;
					} else if (str.ToLower() == "false") {
						obj[key] = false;
					} else {
						obj[key] = str.Replace("\\n", "\n").Replace("\\t", "\t");
					}
				}
			}
			ret.Add(objkey, obj);
		}
		return ret;
	}
	
	/// <summary> Default method for generating a Dictionary used to store data. </summary>
	private static Dictionary<JsonString, JsonValue> DefaultGenerator() { return new Dictionary<JsonString, JsonValue>(); }

	/// <summary> Private reference to the </summary>
	private static Func<IDictionary<JsonString, JsonValue>> _DictionaryGenerator = null;
	/// <summary> The current storage Dictionary generator that is being used. </summary>
	public static Func<IDictionary<JsonString, JsonValue>> DictionaryGenerator {
		get {
			if (_DictionaryGenerator == null) { return DefaultGenerator; }
			return _DictionaryGenerator;
		}
		set { _DictionaryGenerator = value; }
	}


	/// <summary> Internal representation of information. </summary>
	private IDictionary<JsonString, JsonValue> data;
	
	/// <inheritdoc />
	public override JsonType JsonType { get { return JsonType.Object; } }

	/// <summary> Number of Key/Value pairs in the JsonObject </summary>
	public override int Count { get { return data.Count; } }

	/// <summary> Returns true if the collection is empty. </summary>
	public bool IsEmpty { get { return !data.Any(); } }

	/// <summary> Indexes this JsonObject with a given key. Strings are perferred, but any JsonValue will be converted to a String and used. </summary>
	/// <param name="key"> JsonValue to use to index this JsonObject. </param>
	/// <returns> JsonValue existing at the given <paramref name="key"/>, or JsonNull.instance if the key is not in the object. </returns>
	public override JsonValue this[JsonValue key] {
		get {
			if (key.isString) {
				if (data.ContainsKey((JsonString)key)) { return data[(JsonString)key]; }
				return NULL;
			} else if (key.isBool || key.isNumber) {
				string k = key.stringVal;
				if (data.ContainsKey(k)) { return data[k]; }
				return NULL;
			} else {
				throw new Exception(key.JsonType.ToString() + " is not a valid key for " + JsonType.ToString());
			}
		}

		set {
			if (key.isString) {
				if (value == null && data.ContainsKey((JsonString)key)) { data.Remove((JsonString)key); }
				if (value != null) { data[(JsonString)key] = value; }
			} else if (key.isBool || key.isNumber) {
				string k = key.stringVal;
				if (value == null && data.ContainsKey(k)) { data.Remove(k); }
				if (value != null) { data[k] = value; }
			} else {
				throw new Exception(key.JsonType.ToString() + " is not a valid key for " + JsonType.ToString());
			}

		}
	}
	/// <inheritdoc />
	public override bool ContainsKey(JsonString key) { return data.ContainsKey(key); }
	/// <inheritdoc />
	public override bool ContainsAnyKeys(params JsonString[] keys) {
		foreach (string key in keys) {
			if (ContainsKey(key)) { return true; }
		}
		return false;
	}
	/// <inheritdoc />
	public override bool ContainsAllKeys(params JsonString[] keys) {
		foreach (string key in keys) {
			if (!ContainsKey(key)) { return false; }
		}
		return true;
	}

	/// <summary> Does this object have a property defined as <paramref name="key"/>?</summary>
	/// <param name="key"> Name of property to check. </param>
	/// <returns> True if property exists, false otherwise. </returns>
	public bool Has(JsonString key) { return data.ContainsKey(key); }
	
	/// <summary> Does this object have a property defined as <paramref name="key"/> that matches <typeparamref name="T"/>? </summary>
	/// <typeparam name="T"> JsonValue based type to match </typeparam>
	/// <param name="key"> Key to check </param>
	/// <returns> true if <paramref name="key"/> exists, and matches the given type, otherwise false </returns>
	public bool Has<T>(JsonString key) where T : JsonValue {
		return Has(key) && this[key].GetType() == typeof(T);
	}

	/// <summary> Does this object contain all of the given keys? </summary>
	/// <param name="keys"> Array of keys to check </param>
	/// <returns> True if this object has all of the keys, false otherwise </returns>
	public bool HasAll(params JsonString[] keys) { return this.ContainsAllKeys(keys); }
	/// <summary> Does this object contain all of the given keys? </summary>
	/// <param name="keys"> Collection of keys to check </param>
	/// <returns> True if this object has all of the keys, false otherwise </returns>
	public bool HasAll(IEnumerable<JsonString> keys) { return this.ContainsAllKeys(keys.ToArray()); }
	/// <summary> Does this object contain any of the given keys? </summary>
	/// <param name="keys"> Array of keys to check </param>
	/// <returns> True if this object has any single key, false if the object contains NONE of the keys. </returns>
	public bool HasAny(params JsonString[] keys) { return this.ContainsAnyKeys(keys); }
	/// <summary> Does this object contain any of the given keys? </summary>
	/// <param name="keys"> Array of keys to check </param>
	/// <returns> True if this object has any single key, false if the object contains NONE of the keys. </returns>
	public bool HasAny(IEnumerable<JsonString> keys) { return this.ContainsAnyKeys(keys.ToArray()); }
	
	/// <summary> Default Constructor, creates an empty JsonObject with the standard Dictionary. </summary>
	public JsonObject() : base() { data = DictionaryGenerator(); }
	
	/// <summary> Copy infomration from another JsonObject. This is a shallow copy. </summary>
	public JsonObject(JsonObject src) : this() { Add(src); }
	
	/// <summary> Create an JsonObject, setting its data to the parameter. </summary>
	public JsonObject(IDictionary<JsonString, JsonValue> src) : base() { data = src; }
	
	/// <summary> Initialize with an array of key,value pairs</summary>
	/// <param name="pairs">Pairs of values to use. Must be even in length, and contain 'JsonString,JsonValue' pairs</param>
	public JsonObject(params JsonValue[] pairs) : this() {

		if (pairs.Length % 2 == 0) {
			for (int i = 0; i < pairs.Length; i += 2) {
				JsonString key = (JsonString)pairs[i];
				JsonValue val = pairs[i + 1];
				if (val == null || val == JsonNull.instance) { continue; }

				data[key] = val;
			}

		} else {
			throw new TargetParameterCountException("Parameters into JsonObject(params JsonValue[]) must be even");
		}
	}

	/// <summary> Create a shallow copy of this object </summary>
	/// <returns> Shallow copy of this object </returns>
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


	/// <summary> Adds all of the entries in a Dictionary &lt;string, JsonValue&gt;
	/// or other type of Enumerable group of pairs of &lt;string, JsonValue&gt; </summary>
	public JsonObject AddAll<T>(IEnumerable<KeyValuePair<string, T>> info) where T : JsonValue {
		foreach (var pair in info) {
			this[pair.Key] = pair.Value;
		}
		return this;
	}
	/// <summary> Add all of the entries in a grouping of &lt;string, object&gt; pairs, reflecting each value. </summary>
	public JsonObject AddAllReflect<T>(IEnumerable<KeyValuePair<string, T>> info) {
		foreach (var pair in info) {
			this[pair.Key] = Json.Reflect(pair.Value);
		}
		return this;
	}

	/// <summary> Attempt to get a T from a given key. Reflects the JsonValue into a T </summary>
	public T Get<T>(JsonString key) { return Json.GetValue<T>(this[key]); }

	/// <summary> Attempt to get a primitive type from a given key. </summary>
	public object GetPrimitive<T>(JsonString name) { return GetPrimitive(name, typeof(T)); }
	/// <summary> Attempt to get a primitive type from a given key and given type.</summary>
	public object GetPrimitive(JsonString name, Type type) {
		JsonValue val = this[name];
		if (type == typeof(string) && val.isString) { return val.stringVal; }
		if (type == typeof(float) && val.isNumber) { return val.floatVal; }
		if (type == typeof(double) && val.isNumber) { return val.numVal; }
		if (type == typeof(int) && val.isNumber) { return val.intVal; }
		if (type == typeof(bool) && val.isBool) { return val.boolVal; }
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
	/// <summary> Add all information from an IEnumerable&lt;KeyValuePair&lt;string, string&gt;&gt; </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, string>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable&lt;KeyValuePair&lt;string, double&gt;&gt; </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, double>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable&lt;KeyValuePair&lt;string, short&gt;&gt; </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, short>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable&lt;KeyValuePair&lt;string, float&gt;&gt; </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, float>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable&lt;KeyValuePair&lt;string, long&gt;&gt; </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, long>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable&lt;KeyValuePair&lt;string, byte&gt;&gt; </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, byte>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	/// <summary> Add all information from an IEnumerable&lt;KeyValuePair&lt;string, int&gt;&gt; </summary>
	public JsonObject Add(IEnumerable<KeyValuePair<string, int>> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }


	/// <summary>Try to get a T from this object. 
	/// Returns the T if it can be found.
	/// If a T cannot be found, returns a default value. </summary>
	[System.Obsolete("Renamed to 'Pull'")]
	public T Extract<T>(JsonString key, T defaultValue = default(T)) {
		return Pull<T>(key, defaultValue);
	}

	/// <summary>Try to get a T from this object. 
	/// Returns the T if it can be found.
	/// If a T cannot be found, returns a default value. </summary>
	public T Pull<T>(JsonString key, T defaultValue = default(T)) {
		if (ContainsKey(key)) {
			JsonValue val = this[key];

			if (typeof(T) == val.GetType()) { return (T)(object)val; }
			if (val.JsonType == Json.ReflectedType(defaultValue)) { return Json.GetValue<T>(val); }
			if (val.JsonType == Json.ExpectedReflectedType(typeof(T))) {return Json.GetValue<T>(val); }
		}
		return defaultValue;
	}




	/// <summary> Converts this JsonObject into a JsonArray, containing only the values in the object, in an arbitrary order. </summary>
	/// <returns>JsonArray containing all JsonValues in this JsonObject</returns>
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
	public IDictionary<JsonString, JsonValue> GetData() { return data; }
	#region Dictionary Conversions
	/// <summary> Gets a collection of all &lt;string, bool&gt; pairs </summary>
	public Dictionary<string, bool> ToDictOfBool() {
		Dictionary<string, bool> d = new Dictionary<string, bool>();
		foreach (var pair in data) {
			if (pair.Value.isBool) { d[pair.Key.stringVal] = pair.Value.boolVal; }
		}
		return d;
	}
	/// <summary> Gets a collection of all &lt;string, string&gt; pairs </summary>
	public Dictionary<string, string> ToDictOfString() {
		Dictionary<string, string> d = new Dictionary<string, string>();
		foreach (var pair in data) {
			if (pair.Value.isString) { d[pair.Key.stringVal] = pair.Value.stringVal; }
		}
		return d;
	}
	/// <summary> Gets a collection of all &lt;string, double&gt; pairs </summary>
	public Dictionary<string, double> ToDictOfDouble() {
		Dictionary<string, double> d = new Dictionary<string, double>();
		foreach (var pair in data) {
			if (pair.Value.isNumber) { d[pair.Key.stringVal] = pair.Value.numVal; }
		}
		return d;
	}
	/// <summary> Gets a collection of all &lt;string, float&gt; pairs </summary>
	public Dictionary<string, float> ToDictOfFloat() {
		Dictionary<string, float> d = new Dictionary<string, float>();
		foreach (var pair in data) {
			if (pair.Value.isNumber) { d[pair.Key.stringVal] = (float)pair.Value.numVal; }
		}
		return d;
	}
	/// <summary> Gets a collection of all &lt;string, int&gt; pairs </summary>
	public Dictionary<string, int> ToDictOfInt() {
		Dictionary<string, int> d = new Dictionary<string, int>();
		foreach (var pair in data) {
			if (pair.Value.isNumber) { d[pair.Key.stringVal] = (int)pair.Value.numVal; }
		}
		return d;
	}
	#endregion
	
	/// <summary> Removes the KeyValue pair associated with the given key </summary>
	public JsonObject Remove(JsonString key) {
		if (ContainsKey(key)) { data.Remove(key); }
		return this;
	}

	/// <summary> Creates a new JsonObject that has a subset of the original's KeyValue pairs </summary>
	public JsonObject Mask(IEnumerable<JsonString> mask) {
		JsonObject result = new JsonObject();
		foreach (JsonString str in mask) { result.Add(str, this[str]); }
		return result;
	}

	/// <summary> Creates a new JsonObject that has a subset of the original's KeyValue pairs,
	/// using a list of strings as the mask. </summary>
	/// <param name="mask">collection of strings to use as the mask</param>
	/// <returns>A copy of the original JsonObject, only containing keys that are in the mask. </returns>
	public JsonObject Mask(IEnumerable<string> mask) {
		JsonObject result = new JsonObject();
		foreach (string str in mask) { result.Add(str, this[str]); }
		return result;
	}

	/// <summary> Creates a new JsonObject that has a subset of the original's Key value pairs,
	/// using the boolean value of pairs within another JsonObject </summary>
	/// <param name="mask">JsonObject containing mask pairs. Only considers pairs of (string, bool) </param>
	/// <returns>A copy of the original JsonObject with the mask applied to it. </returns>
	public JsonObject Mask(JsonObject mask) {
		List<JsonString> msk = new List<JsonString>();
		foreach (var pair in mask) {
			if (pair.Value.isBool && pair.Value.boolVal) {
				msk.Add(pair.Key);
			}
		}
		return Mask(msk);
	}

	/// <summary> Removes all KeyValue pairs from the JsonObject. </summary>
	public JsonObject Clear() { data.Clear(); return this; }

	/// <summary> Sets keys of this JsonObject, and any children in both it and other </summary>
	/// <param name="other">Other data to override this object's data with</param>
	/// <returns>A reference to the original JsonObject, after it has been modified with the values from 'other'</returns>
	public JsonObject SetRecursively(JsonObject other) {
		foreach (var pair in other) {
			var thisOfKey = this[pair.Key];
			if (pair.Value.isObject && thisOfKey.isObject) {
				(thisOfKey as JsonObject).SetRecursively(pair.Value as JsonObject);
			} else {
				this[pair.Key] = pair.Value;
			}
		}
		return this;
	}

	/// <summary> Combines this and <paramref name="other"/> into a new JsonObject, recursively </summary>
	/// <param name="other">other JsonObject to combine with </param>
	/// <returns>A new JsonObject with the combined values of this and other (in order) </returns>
	public JsonObject CombineRecursively(JsonObject other) {
		JsonObject obj = new JsonObject().Set(this);
		foreach (var pair in other) {
			var objOfKey = obj[pair.Key];
			if (pair.Value.isObject && objOfKey.isObject) {
				obj[pair.Key] = (objOfKey as JsonObject).Clone().SetRecursively(pair.Value as JsonObject);
			} else {
				obj[pair.Key] = pair.Value;
			}
		}
		return obj;
	}

	/// <summary> Combines <paramref name="first"/> and <paramref name="second"/> into a new JsonObject, recursively </summary>
	/// <param name="first">First JsonObject</param>
	/// <param name="second">Second JsonObject</param>
	/// <returns>A new JsonObject with the combined values of first and second (in order)</returns>
	public static JsonObject CombineRecursively(JsonObject first, JsonObject second) {
		return first.CombineRecursively(second);
	}


	/// <summary> Combines this JsonObject with <paramref name="other"/>, into a new JsonObject. </summary>
	/// <param name="other">Other JsonObject to combine with </param>
	/// <returns>A new JsonObject containing values from this and <paramref name="other"/> </returns>
	public JsonObject Combine(JsonObject other) { return Combine(this, other); }

	//public JsonObject Combine(JsonObject other, params string[] lim) { return null; }

	/// <summary> Combines <paramref name="first"/> and <paramref name="second"/> into a new JsonObject </summary>
	/// <param name="first">First JsonObject</param>
	/// <param name="second">Second JsonObject</param>
	/// <returns>A new JsonObject with the combined values of first and second (in order)</returns>
	public static JsonObject Combine(JsonObject first, JsonObject second) {
		return new JsonObject().Set(first).Set(second);
	}


	/// <summary> Takes all of the KeyValue pairs from the other object, and sets this object to have the same values for those keys. </summary>
	/// <param name="other">Other object holding values to set</param>
	/// <returns>The same object that this method was called on, after it has been modified</returns>
	public JsonObject Set(JsonObject other) {
		foreach (var pair in other) { this[pair.Key] = pair.Value; }
		return this;
	}

	/// <summary> Takes a given subset of key/value pairs from another object and applies them to his object. </summary>
	/// <param name="other">Other object holding values to set </param>
	/// <param name="lim">Subset of keys to set on the current object, given <paramref name="other"/> also contains them.</param>
	/// <returns>The same object that this method was called on, after it has been modified</returns>
	public JsonObject Set(JsonObject other, params string[] lim) {
		foreach (var param in lim) {
			if (other.ContainsKey(param)) { this[param] = other[param]; }
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

	/// <summary> Sets a key to a value. Supports assignment when indexers are not available. </summary>
	/// <returns> The same object that it was called on.</returns>
	public JsonObject Set(string key, JsonValue value) {
		this[key] = value;
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



	/// <summary> Turns this JsonObject into a compact string. </summary>
	/// <returns> String containing begin/end braces, All Key/Value pairs inside of the current JsonObject, without any excess whitespace. </returns>
	public override string ToString() {
		StringBuilder str = new StringBuilder();
		str.Append("{");

		int i = 0;
		foreach (var pair in data) {
			//str.Append('\"');
			//str.Append(pair.Key.stringVal);
			//str.Append('\"');
			str.Append(pair.Key.ToString());
			str.Append(':');
			if (pair.Value == null) {//possible for collection to have an actual 'null' instead of JsonValue.NULL
				str.Append("null");
			} else {
				str.Append(pair.Value.ToString());
			}
			i++;
			if (i < Count) { str.Append(','); }
		}

		str.Append("}");
		return str.ToString();
	}



	/// <summary> Pretty prints the given JsonObject into a easily human-readable string. </summary>
	/// <returns> PrettyPrinted string version of this object </returns>
	public override string PrettyPrint() {
		return new JsonPrettyPrinter().PrettyPrint(this).ToString();
	}

}

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonArray

/// <summary> Representation of an array of objects </summary>
public class JsonArray : JsonValue, IEnumerable<JsonValue>, IList<JsonValue> {

	/// <summary>
	/// Parses a standard CSV-format spreadsheet into a JsonArray.
	/// Requires that the top row is a header row with names for the columns.
	/// Each row in the CSV becomes another JsonObject, inserted into the array.
	/// For any row, if a column is not present, it does not add a key/value pair for that column.
	/// The separator can also be specified.
	/// Defaults to using a comma (',') as the separator.
	/// </summary>
	/// <param name="csv">string containing CSV formatted spreadsheet to parse</param>
	/// <param name="sep">separator character. defaults to ','</param>
	/// <returns>CSV formatted spreadsheet converted into a JsonArray</returns>
	public static JsonArray ParseCSV(string csv, char sep = ',') {
		JsonArray arr = new JsonArray();

		string[] lines = csv.Split('\n');
		string[] keys = lines[0].Split(sep);
		for (int i = 0; i < keys.Length; i++) { keys[i] = keys[i].Trim(); }

		for (int i = 1; i < lines.Length; i++) {
			JsonObject obj = new JsonObject();
			string line = lines[i];
			if (line.Length <= 2) { continue; }
			if (line.StartsWith("#")) { continue; }

			string[] content = line.Split(sep);
			for (int k = 0; k < keys.Length && k < content.Length; k++) {
				string str = content[k];

				if (str != null && str != "") {
					string key = keys[k];
					double val = 0;
					if (double.TryParse(str, out val)) {
						obj[key] = val;
					} else if (str.ToLower() == "true") {
						obj[key] = true;
					} else if (str.ToLower() == "false") {
						obj[key] = false;
					} else {
						obj[key] = str.Replace("\\n", "\n").Replace("\\t", "\t");
					}
				}
			}
			arr.Add(obj);
		}
		return arr;
	}

	/// <summary> Internal representation of data </summary>
	protected List<JsonValue> list;
	/// <summary> Get the internal representation of data </summary>
	public List<JsonValue> GetList() { return list; }

	/// <inheritdoc />
	public override JsonType JsonType { get { return JsonType.Array; } }
	/// <inheritdoc />
	public override int Count { get { return list.Count; } }

	int ICollection<JsonValue>.Count { get { return Count; } }

	bool ICollection<JsonValue>.IsReadOnly { get { return false; } }

	JsonValue IList<JsonValue>.this[int index] {
		get { return this[(JsonValue)index]; }
		set { this[(JsonValue)index] = value; }
	}

	/// <summary> Index this JsonArray with a given JsonValue. Integers are preferred, but any value will be converted to an integer and then used. </summary>
	/// <param name="index"> Index to check at </param>
	/// <returns> The JsonValue that exists at the given <paramref name="index"/>, or JsonNull.instance if the index is out of range. </returns>
	public override JsonValue this[JsonValue index] {
		get {
			if (index.isNumber || index.isString) {
				double indD = index.numVal;
				if (double.IsNaN(indD) || double.IsInfinity(indD)) { return NULL; }
				int ind = (int)indD;
				
				if (ind > 0 || ind < list.Count) { return list[ind]; }
				return NULL;
			} else {
				throw new Exception(index.JsonType.ToString() + " is not a valid index for JsonArray!");
			}
		}
		set {
			if (index.isNumber || index.isString) {
				int ind = index.intVal;
				if (ind >= 0 && ind < Count) { list[ind] = value; } else if (ind == Count) { list.Add(value); } else { throw new Exception("Index " + ind + " is out of bounds!"); }
			} else {
				throw new Exception(index.JsonType.ToString() + " is not a valid index for JsonArray!");
			}
		}
	}

	/// <summary> Default Constructor, creates an empty list </summary>
	public JsonArray() : base() { list = new List<JsonValue>(); }
	/// <summary> Creates a new JsonArray using a given list as its internal data. </summary>
	public JsonArray(List<JsonValue> values) : base() { list = values; }
	/// <summary> Creates a new JsonArray and copies all elements from another list. </summary>
	public JsonArray(JsonArray src) : this() { AddAll(src); }
	/// <summary> Creates a new JsonArray with an array of values</summary>
	public JsonArray(params JsonValue[] values) : this() { AddAll(values); }

	/// <summary> Convert arbitrary Array to JsonArray </summary>
	/// <param name="a">Array to convert </param>
	/// <returns>JsonArray with reflections of all objects in <paramref name="a"/></returns>
	public static implicit operator JsonArray(Array a) {
		JsonArray arr = new JsonArray();
		foreach (var obj in a) { arr.Add(Json.Reflect(obj)); }
		return arr;
	}

	/// <summary> Creates a copy of this JsonArray </summary>
	public JsonArray Clone() { return new JsonArray(this); }

	/// <summary> Adds a single JsonValue into this list </summary>
	public JsonArray Add(JsonValue val) { list.Add(val); return this; }

	/// <summary> Add all items in an arbitrary array, and return the modified object. </summary>
	/// <param name="arr">Array to add</param>
	/// <returns>The object on which this method was called</returns>
	public JsonArray AddAll<T>(IEnumerable<T> arr) {
		foreach (var val in arr) { Add(Json.Reflect(val)); }
		return this;
	}

	/// <summary> Add reflections of all objects that are contained in a collection </summary>
	public JsonArray AddAllReflect<T>(IEnumerable<T> info) {
		foreach (T val in info) { Add(Json.Reflect(val)); }
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

	/// <summary> Get an object at a given <paramref name="index"/>, as a the given type <typeparamref name="T"/>. </summary>
	/// <typeparam name="T"> Generic type parameter of object to get </typeparam>
	/// <param name="index"> Index in this array to use </param>
	/// <returns> Object at the given <paramref name="index"/>, interpreted as type <typeparamref name="T"/> </returns>
	public T Get<T>(int index) { return Json.GetValue<T>(this[index]); }

	public T Pull<T>(int index, T defaultValue = default(T)) {
		if (index >= 0 && index < Count) {
			JsonValue val = this[index];
			if (val.JsonType == Json.ReflectedType(defaultValue)) { return Json.GetValue<T>(val); }
		}
		return defaultValue;
	}

	/// <summary>
	/// Searches through the array for an object with a key matching a string value
	/// </summary>
	/// <param name="key">Key to search for </param>
	/// <param name="value">String to search for </param>
	/// <returns>first object containing the (key:value) pair</returns>
	public JsonObject FindObjectBy(string key, string value) {
		foreach (var val in this) {
			JsonObject obj = val as JsonObject;
			if (obj != null) {
				if (obj.ContainsKey(key) && obj.Get<string>(key) == value) { return obj; }
			}
		}

		return null;
	}

	/// <summary>
	/// Searches through the array for an object with a key matching a string value
	/// </summary>
	/// <param name="key">Key to search for </param>
	/// <param name="value">Value to search for </param>
	/// <param name="tolerance">tolerance of comparison (default = .001)</param>
	/// <returns>first object containing the (key:value) pair inside of the tolerance range</returns>
	public JsonObject FindObjectBy(string key, double value, double tolerance = .001) {
		foreach (var val in this) {
			JsonObject obj = val as JsonObject;
			if (obj != null) {
				if (obj.ContainsKey(key) && Math.Abs(obj.Get<double>(key) - value) < tolerance) {
					return obj;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Finds ALL objects that have a pair with (key:value)
	/// </summary>
	/// <param name="key">Key to look for </param>
	/// <param name="value">Value to look for </param>
	/// <returns>JsonArray of all matching objects, or an empty JsonArray if none match</returns>
	public JsonArray FindObjectsBy(string key, string value) {
		JsonArray ray = new JsonArray();

		foreach (var val in this) {
			JsonObject obj = val as JsonObject;
			if (obj != null) {
				if (obj.ContainsKey(key) && obj.Get<string>(key) == value) {
					ray.Add(obj);
				}
			}
		}

		return ray;
	}

	/// <summary>
	/// Finds ALL objects that have a pair with (key:value)
	/// </summary>
	/// <param name="key">Key to look for </param>
	/// <param name="value">Value to look for </param>
	/// <param name="tolerance">tolerance of comparison (default = .001)</param>
	/// <returns>JsonArray of all matching objects, or an empty JsonArray if none match</returns>
	public JsonArray FindObjectsBy(string key, double value, double tolerance = .001) {
		JsonArray ray = new JsonArray();

		foreach (var val in this) {
			JsonObject obj = val as JsonObject;
			if (obj != null) {
				if (obj.ContainsKey(key) && Math.Abs(obj.Get<double>(key) - value) < tolerance) {
					ray.Add(obj);
				}
			}
		}

		return ray;
	}

	/// <summary> Get an array of all JsonNumbers as double values  </summary>
	public double[] OnlyNumbersToArray() { return OnlyNumbersToList().ToArray(); }
	/// <summary> Get a list of all JsonNumbers as double values </summary>
	public List<double> OnlyNumbersToList() {
		List<double> arr = new List<double>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isNumber) { arr.Add(val.numVal); }
		}
		return arr;
	}



	/// <summary> Get an array of all JsonNumbers as int values  </summary>
	public int[] OnlyIntToArray() { return OnlyIntToList().ToArray(); }
	/// <summary> Get a list of all JsonNumbers as int values  </summary>
	public List<int> OnlyIntToList() {
		List<int> arr = new List<int>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isNumber) { arr.Add((int)val.numVal); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonNumbers as float values  </summary>
	public float[] OnlyFloatsToArray() { return OnlyFloatToList().ToArray(); }
	/// <summary> Get an list of all JsonNumbers as float values  </summary>
	public List<float> OnlyFloatToList() {
		List<float> arr = new List<float>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isNumber) { arr.Add((float)val.numVal); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonBooleans as bool values  </summary>
	public bool[] OnlyBoolToArray() { return OnlyBoolToList().ToArray(); }
	/// <summary> Get a list of all JsonBooleans as bool values  </summary>
	public List<bool> OnlyBoolToList() {
		List<bool> arr = new List<bool>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isBool) { arr.Add(val.boolVal); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonStrings as string values  </summary>
	public string[] OnlyStringsToArray() { return OnlyStringToList().ToArray(); }
	/// <summary> Get a list of all JsonStrings as string values  </summary>
	public List<string> OnlyStringToList() {
		List<string> arr = new List<string>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isString) { arr.Add(val.stringVal); }
		}
		return arr;
	}

	/// <summary> Get an array of all primitive elements in the JsonArray as strings </summary>
	/// <returns> A string[] of all primitive elements in the JsonArray as strings </returns>
	public string[] ToStringArray() { return ToStringList().ToArray(); }
	/// <summary> Get a List&lt;string&gt; of all primitive elements in the JsonArray as strings. </summary>
	/// <returns> List&lt;string&gt; of all primitive values in the JsonArray as strings </returns>
	public List<string> ToStringList() {
		List<string> arr = new List<string>();
		foreach (var item in this) {
			if (!item.isObject && !item.isArray) {
				arr.Add(item.stringVal);
			}
		}
		return arr;
	}

	/// <summary> Get an array of only JsonObjects values  </summary>
	public JsonObject[] OnlyObjectToArray() { return OnlyObjectToList().ToArray(); }
	/// <summary> Get a list of only JsonObjects values  </summary>
	public List<JsonObject> OnlyObjectToList() {
		List<JsonObject> arr = new List<JsonObject>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isObject) { arr.Add(val as JsonObject); }
		}
		return arr;
	}

	/// <summary> Get an array of all JsonObjects as T values  </summary>
	/// <returns> T[] of all of the JsonObjects in this JsonArray reflected into objects of type 'T' </returns>
	public T[] ToArrayOf<T>() { return ToListOf<T>().ToArray(); }
	/// <summary> Get a list of all JsonObjects as T values  </summary>
	/// <returns> List&lt;T&gt; of all of the JsonObjects in this JsonArray reflected into objects of type 'T' </returns>
	public List<T> ToListOf<T>() {
		List<T> arr = new List<T>(Count);
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (ReferenceEquals(val, null)) { val = JsonNull.instance; }
			T sval = Json.GetValue<T>(val);
			arr.Add(sval);
		}
		return arr;
	}

	/// <summary> Get an array of all JsonObjects as object values  </summary>
	public object[] ToObjectArray(Type type) { return ToObjectList(type).ToArray(); }
	/// <summary> Get a list of all JsonObjects as object values  </summary>
	public List<object> ToObjectList(Type type) {
		
		List<object> arr = new List<object>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (ReferenceEquals(val, null)) { val = JsonNull.instance; }
			object sval = Json.GetValue(val, type);
			arr.Add(sval);
		}


		return arr;
	}

	/// <summary> Prints out this JsonArray into a string with only one line. </summary>
	/// <returns> String containing all of the ToString'd elements of the contents of this JsonArray. </returns>
	public override string ToString() {
		StringBuilder str = new StringBuilder();
		str.Append('[');
		int i = 0;
		foreach (var item in this) {
			if (item == null) {
				str.Append("null");
			} else {
				str.Append(item.ToString());
			}
			i++;
			if (i < Count) { str.Append(','); }
		}

		str.Append(']');
		return str.ToString();
	}

	/// <summary> PrettyPrints the content of this JsonArray into an easily human readable string. </summary>
	/// <returns> PrettyPrinted string containing the content of this JsonArray</returns>
	public override string PrettyPrint() {
		return new JsonPrettyPrinter().PrettyPrint(this).ToString();
	}

	int IList<JsonValue>.IndexOf(JsonValue item) {
		return list.IndexOf(item);
	}

	void IList<JsonValue>.Insert(int index, JsonValue item) {
		list.Insert(index, item);
	}

	void IList<JsonValue>.RemoveAt(int index) {
		list.RemoveAt(index);
	}

	void ICollection<JsonValue>.Add(JsonValue item) {
		list.Add(item);
	}

	void ICollection<JsonValue>.Clear() {
		list.Clear();
	}

	bool ICollection<JsonValue>.Contains(JsonValue item) {
		return list.Contains(item);
	}

	void ICollection<JsonValue>.CopyTo(JsonValue[] array, int arrayIndex) {
		list.CopyTo(array, arrayIndex);
	}

	bool ICollection<JsonValue>.Remove(JsonValue item) {
		return list.Remove(item);
	}

	IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator() {
		return list.GetEnumerator();
	}
}

#endregion

#region PrettyPrinter
////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonPrettyPrinter

/// <summary> Provides thread-safe pretty printing </summary>
public class JsonPrettyPrinter {

	/// <summary> Current indent level </summary>
	private int indentLevel = 0;

	/// <summary> PrettyPrints a given JsonObject. </summary>
	/// <param name="obj">JsonObject to PrettyPrint</param>
	/// <param name="str">StringBuilder to append to. If null, a new StringBuilder is created. This is also returned when the function ends.</param>
	/// <returns>String builder holding pretty printed information</returns>
	public StringBuilder PrettyPrint(JsonObject obj, StringBuilder str = null) {
		if (str == null) { str = new StringBuilder(); }
		string tabs = "".PadLeft(indentLevel, '\t');

		str.Append(tabs);
		str.Append('{');
		indentLevel++;

		int i = 0;
		foreach (var pair in obj) {
			str.Append('\n');
			str.Append(tabs);
			str.Append('\t');

			//str.Append('\"');
			//str.Append(pair.Key.stringVal);
			//str.Append('\"');
			str.Append(pair.Key.PrettyPrint());
			str.Append(':');
			if (pair.Value != null) {
				if (pair.Value.isObject) {
					str.Append('\n');
					this.PrettyPrint(pair.Value as JsonObject, str);
				} else if (pair.Value.isArray) {
					str.Append('\n');
					this.PrettyPrint(pair.Value as JsonArray, str);
				} else {
					str.Append(pair.Value.PrettyPrint());
				}
			} else {//possible for collection to have an actual 'null' instead of JsonValue.NULL
				str.Append("null");
			}
			i++;
			if (i < obj.Count) { str.Append(','); }
		}
		indentLevel--;

		str.Append('\n');
		str.Append(tabs);
		str.Append('}');

		return str;
	}

	/// <summary> PrettyPrints a given JsonArray.</summary>
	/// <param name="arr">JsonArray to PrettyPrint</param>
	/// <param name="str">StringBuilder to append to. If null, a new StringBuilder is created. This is also returned when the function ends.</param>
	/// <returns>String builder holding pretty printed information</returns>
	public StringBuilder PrettyPrint(JsonArray arr, StringBuilder str = null) {
		if (str == null) { str = new StringBuilder(); }
		string tabs = "".PadLeft(indentLevel, '\t');

		str.Append(tabs);
		str.Append('[');
		indentLevel++;

		int i = 0;
		foreach (var item in arr) {
			str.Append('\n');

			//Append item
			if (item == null) {
				str.Append(tabs);
				str.Append('\t');
				str.Append("null");
			} else if (item.isObject) {
				this.PrettyPrint(item as JsonObject, str);
			} else if (item.isArray) {
				this.PrettyPrint(item as JsonArray, str);
			} else {
				str.Append(tabs);
				str.Append('\t');

				str.Append(item.PrettyPrint());
			}

			i++;
			if (i < arr.Count) { str.Append(','); }
		}
		indentLevel--;

		str.Append('\n');
		str.Append(tabs);
		str.Append(']');

		return str;
	}
}
#endregion


#region Reflector

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonReflector

/// <summary> Class containing Reflection Code </summary>
public static class JsonReflector {

	/// <summary> Grab method info for JsonArray.ToArrayOf&lt;T&gt;() for generic insertion  </summary>
	public static readonly MethodInfo toArrayOf = typeof(JsonArray).GetMethod("ToArrayOf");
	/// <summary> Grab method info for JsonArray.ToListOf&lt;T&gt;() for generic insertion  </summary>
	public static readonly MethodInfo toListOf = typeof(JsonArray).GetMethod("ToListOf");
	/// <summary> Grab method info for JsonReflector.ToNullable&lt;T&gt;() for generic insertion </summary>
	public static readonly MethodInfo toNullable = typeof(JsonReflector).GetMethod("ToNullable");

	/// <summary> Binding flags for easy usage </summary>
	static BindingFlags PUBLIC_MEMBERS = BindingFlags.Instance | BindingFlags.Public;

	/// <summary> Contains all blacklisted types (reflect to null by default) </summary>
	public static HashSet<Type> blacklist = new HashSet<Type>();
	
	/// <summary> Custom, explicitly registered generators </summary>
	// private static Dictionary<Type, Func<JsonValue, object>> generators = new Dictionary<Type, Func<JsonValue, object>>();
	private static IDictionary<Type, Delegate> generators = new ConcurrentDictionary<Type, Delegate>();

	/// <summary> Custom, explicitly registered reflectors </summary>
	//private static Dictionary<Type, Func<object, JsonValue>> reflectors = new Dictionary<Type, Func<object, JsonValue>>();
	private static IDictionary<Type, Delegate> reflectors = new ConcurrentDictionary<Type, Delegate>();

	/// <summary> Register both reflection and generation conversions for a type. </summary>
	/// <typeparam name="T"> Generic type. </typeparam>
	/// <param name="generator"> Function taking <see cref="JsonValue"/> and returning <typeparamref name="T"/> </param>
	/// <param name="reflector"> Function taking <typeparamref name="T"/> and returning <see cref="JsonValue"/> </param>
	public static void RegisterConversions<T>(Func<JsonValue, T> generator, Func<T, JsonValue> reflector) {
		RegisterGenerator(generator);
		RegisterReflector(reflector);
	}

	/// <summary> Registers a custom generator for the given type </summary>
	/// <typeparam name="T"> Generic type to know what type to bind to </typeparam>
	/// <param name="generator"> Function taking a JsonValue and yielding a T </param>
	public static void RegisterGenerator<T>(Func<JsonValue, T> generator) { generators[typeof(T)] = generator; }
	/// <summary> Removes the generator for the given type, if you need to clean up your mess. </summary>
	/// <typeparam name="T"> Generic type to know what type to unbind </typeparam>
	public static void UnregisterGenerator<T>() { generators.Remove(typeof(T)); CACHED_GENERATORS.Remove(typeof(T)); }

	/// <summary> Registers a custom reflector for the given type </summary>
	/// <typeparam name="T"> Generic type to know what type to bind to </typeparam>
	/// <param name="generator"> Function taking a T and yielding a JsonValue </param>
	public static void RegisterReflector<T>(Func<T, JsonValue> reflector) { reflectors[typeof(T)] = reflector; }
	/// <summary> Removes the reflector for the given type, if you need to clean up your mess. </summary>
	/// <typeparam name="T"> Generic type to know what type to unbind </typeparam>
	public static void UnregisterReflector<T>() { reflectors.Remove(typeof(T)); CACHED_REFLECTORS.Remove(typeof(T)); }

	/// <summary> Blacklist a given type from being reflected </summary>
	/// <param name="t">Type to blacklist</param>
	public static void Blacklist(Type t) {
		if (!blacklist.Contains(t)) { blacklist.Add(t); }
	}

	/// <summary> Remove a given type from being blacklisted</summary>
	/// <param name="t">Type to unblacklist</param>
	public static void UnBlacklist(Type t) {
		if (blacklist.Contains(t)) { blacklist.Remove(t); }
	}

	public static Nullable<T> NullableOfNull<T>() where T : struct { return new Nullable<T>(); }
	public static Nullable<T> ToNullable<T>(JsonValue val) where T : struct {
		if (ReferenceEquals(val, null)) { return NullableOfNull<T>(); }
		object obj = GetReflectedValue(val, typeof(T));
		if (obj == null) { return NullableOfNull<T>(); }
		return new Nullable<T>( (T) obj );
	}

	public static bool IsNullableType(this Type type) {
		return type.IsGenericType && typeofNullable == type.GetGenericTypeDefinition();
	}

	static readonly Type typeofNullable = typeof(Nullable<int>).GetGenericTypeDefinition();
	static readonly Type typeofPhysicMaterial = Type.GetType("UnityEngine.PhysicMaterial, UnityEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);
	static readonly Type typeofMaterial = Type.GetType("UnityEngine.Material, UnityEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);
	static readonly Type typeofQuaternion = Type.GetType("UnityEngine.Quaternion, UnityEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);
	static readonly Type typeofRigidBody = Type.GetType("UnityEngine.Rigidbody, UnityEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", false);
	/// <summary> Did the Load() function run? (Should always return true) </summary>
	public static bool loaded = Load();
	static bool Load() {
		// PhysicMaterials and Materials are resource objects, and many objects have automatic duplication in setters for these types.
		// They are blacklisted to prevent materials from being silently duplicated and leaked, and also so that resource references are preserved in prefabs
		// It is likely other similar types in UnityEngine should be blacklisted as well, but no problems have been noticed so far.
		if (typeofPhysicMaterial != null) { Blacklist(typeofPhysicMaterial); }
		if (typeofMaterial != null) { Blacklist(typeofMaterial); }

		return true;
	}

	/// <summary> Dictionary holding cached delegates </summary>
	public static readonly IDictionary<Type, Func<object, JsonValue>> CACHED_REFLECTORS = new ConcurrentDictionary<Type, Func<object, JsonValue>>();
	/// <summary> Dictionary holding cached delegates </summary>
	public static readonly IDictionary<Type, Func<JsonValue, object>> CACHED_GENERATORS = new ConcurrentDictionary<Type, Func<JsonValue, object>>();
	public static readonly object[] NO_PARAMETERS = new object[0];
	public static Func<object, JsonValue> REFLECTOR(Type t) {
		bool hadCache = CACHED_REFLECTORS.ContainsKey(t);
		if (!hadCache && reflectors.ContainsKey(t)) {
			return CACHED_REFLECTORS[t] = (obj) => { return (JsonValue)reflectors[t].Method.Invoke(null, new object[] { obj } ); };
		}
		if (hadCache) { return CACHED_REFLECTORS[t]; }
		MethodInfo info = t.GetMethod("ToJson", BindingFlags.Instance | BindingFlags.Public);
		if (info != null && info.ReturnType == typeof(JsonValue)) {
			CACHED_REFLECTORS[t] = (obj) => { return (JsonValue)info.Invoke(obj, NO_PARAMETERS); };
		} else {
			CACHED_REFLECTORS[t] = null;
		}
		return CACHED_REFLECTORS[t];
	}
	public static Func<JsonValue, object> GENERATOR(Type t) {
		bool hadCache = CACHED_GENERATORS.ContainsKey(t);
		if (!hadCache && generators.ContainsKey(t)) {
			return CACHED_GENERATORS[t] = (jval) => { return generators[t].Method.Invoke(null, new object[] { jval } ); };
		}
		if (hadCache) { return CACHED_GENERATORS[t]; }
		MethodInfo info = t.GetMethod("FromJson", BindingFlags.Static | BindingFlags.Public);
		Func<JsonValue, object> generator = null;
		if (info != null && info.ReturnType == t) {
			var paramInfos = info.GetParameters();
			if (paramInfos.Length == 1 && paramInfos[0].ParameterType == typeof(JsonValue)) {
				object[] prams = new object[] { };
				generator = (jval) => { return info.Invoke(null, new object[] { jval }); };
			}
		}
		return CACHED_GENERATORS[t] = generator;
	}

	/// <summary> Reflect a JsonValue based on a given type. Attempts to return an object, 
	/// so return value may be null even if a value type is requested. </summary>
	public static object GetReflectedValue(JsonValue val, Type destType) {
		if (destType.IsJsonType()) { return val.DeepCopy(); }
		if (val == null || blacklist.Contains(destType)) { return null; }
		object sval = null;
		Func<JsonValue, object> generator;
		
		// Automatic primitive dereflection.
		if (destType.IsNullableType()) {
			Type genericType = destType.GetGenericArguments()[0];
			MethodInfo genericGrabber = toNullable.MakeGenericMethod(genericType);
			sval = genericGrabber.Invoke(null, new object[] { val });
		} 
		else if (val.isString && destType == typeof(string)) { sval = val.stringVal; }
		else if (val.isString && destType == typeof(Guid)) { sval = Guid.Parse(val.stringVal); }
		else if (val.isString && destType == typeof(DateTime)) { sval = DateTime.Parse(val.stringVal); }
		else if (val.isBool && destType == typeof(bool)) { sval = val.boolVal; } 
		else if (val.isString && destType.IsNumeric()) {
			double numVal = 0;
			double.TryParse(val.stringVal, out numVal);
			sval = Convert.ChangeType(numVal, destType);
		} else if (val.isString && destType.IsEnum) {
			try {
				sval = Enum.Parse(destType, val.stringVal);
			} catch {
				sval = Enum.ToObject(destType, 0);
			}
		} 
		else if (val.isNumber && destType == typeof(double)) { sval = val.numVal; } 
		else if (val.isNumber && destType == typeof(float)) { sval = (float)val.numVal; } 
		else if (val.isNumber && destType == typeof(int)) { sval = (int)val.numVal; } 
		else if (val.isNumber && destType == typeof(long)) { sval = (long)val.numVal; } 
		else if (val.isNumber && destType == typeof(byte)) { sval = (byte)val.numVal; } 
		else if (val.isNumber && destType == typeof(short)) { sval = (short)val.numVal; } 
		else if (val.isNumber && destType == typeof(uint)) { sval = (uint)val.numVal; } 
		else if (val.isNumber && destType == typeof(ulong)) { sval = (ulong)val.numVal; } 
		else if (val.isNumber && destType == typeof(ushort)) { sval = (ushort)val.numVal; } 
		else if (val.isNumber && destType == typeof(decimal)) { sval = (decimal)val.numVal; } 
		else if (val.isNumber && destType == typeof(sbyte)) { sval = (sbyte)val.numVal; } 
		// Internally, explicitly registered generators take top priority, then implicitly registered generators. 
		else if ((generator = GENERATOR(destType)) != null) { sval = generator(val); }
		// Otherwise automatic reflection is used
		else if (val.isArray && destType.IsArray) {
			//TBD: Reflect the JsonArray into a new array
			JsonArray arr = val as JsonArray;
			Type eleType = destType.GetElementType();
			MethodInfo genericGrabber = toArrayOf.MakeGenericMethod(eleType);
			sval = genericGrabber.Invoke(arr, new object[] { });

		} else if (val.isArray && typeof(IList).IsAssignableFrom(destType)) {
			JsonArray arr = val as JsonArray;
			Type eleType;
			if (JsonHelpers.TryListOfWhat(destType, out eleType)) {
				MethodInfo genericGrabber = toListOf.MakeGenericMethod(eleType);
				sval = genericGrabber.Invoke(arr, new object[] { });

			} else {
				sval = null;
			}

		} else if (val.isObject) {
			//TBD: Reflect the JsonObject into a new object of that type???
			JsonObject jobj = val as JsonObject;

			if (destType.IsValueType) {
				object boxedValue = Activator.CreateInstance(destType);
				FieldInfo[] fields = destType.GetFields();

				if (typeofQuaternion != null && destType == typeofQuaternion) {
					// Remove 'eulerAngles' from Quaternions, as it is redundant information
					// which is calculated from other information in the struct.
					fields = fields.Where((field) => (field.Name != "eulerAngles")).ToArray();
				}

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
	/// <param name="source">JsonObject with data</param>
	/// <param name="destination">Object to apply data from source to</param>
	public static void ReflectInto(JsonObject source, object destination) {
		Type type = destination.GetType();
		if (type.IsValueType) { throw new Exception("Can't reflect Json into a value type. Use Json.GetValue(JsonValue, Type) instead."); }

		var data = source.GetData();

		PropertyInfo mapper = type.GetProperty("Item", new Type[] { typeof(string) });
		Type mapperValueType = null;
		MethodInfo mapperSetMethod = null;

		if (mapper != null) {
			mapperValueType = mapper.PropertyType;
			mapperSetMethod = mapper.GetSetMethod();
		}

		PropertyInfo indexer = type.GetProperty("Item", new Type[] { typeof(int) });
		Type indexerValueType = null;
		MethodInfo adder = null;

		if (indexer != null) {
			indexerValueType = indexer.PropertyType;
			adder = type.GetMethod("Add", new Type[] { indexerValueType });
		}

		JsonArray _ITEMS = null;
		foreach (var pair in data) {
			string key = pair.Key;
			JsonValue val = pair.Value;

			if (key == "_ITEMS") {
				_ITEMS = val as JsonArray;
				continue;
			}

			PropertyInfo property = type.GetProperty(key, PUBLIC_MEMBERS);
			if (property != null && property.IsWritable() && property.IsReadable()) {

				Type destType = property.PropertyType;
				MethodInfo setMethod = property.GetSetMethod();

				object sval = GetReflectedValue(val, destType);

				if (sval != null) {
					setMethod.Invoke(destination, new object[] { sval });
				}

				//If there exists a property by a name, there is likely no field by the same name
				//unless you're a hacker.
				continue;
			}

			FieldInfo field = type.GetField(key, PUBLIC_MEMBERS);
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
					mapperSetMethod.Invoke(destination, new object[] { key, sval });
				}

				continue;
			}


		}

		if (_ITEMS != null && indexer != null && adder != null) {
			List<JsonValue> list = _ITEMS.GetList();
			for (int i = 0; i < list.Count; i++) {
				JsonValue val = list[i];
				object sval = GetReflectedValue(val, indexerValueType);
				adder.Invoke(destination, new object[] { sval });
				//indexerSetMethod.Invoke(destination, new object[] {i,sval}); 
			}

		}


	}
	
	/// <summary> Get a JsonRepresentation of a given code object. 
	/// Creates a new JsonValue based on what is needed. </summary>
	/// <param name="source">object to reflect</param>
	/// <returns>JsonValue representing the same data as the source object</returns>
	public static JsonValue Reflect(object source) {
		if (source == null) { return null; }
		Type type = source.GetType();

		//Return object directly if it is already a JsonValue in some way.
		if (typeof(JsonValue).IsAssignableFrom(type)) { return ((JsonValue)source); }

		Func<object, JsonValue> reflector;
		JsonValue jval = null;
		
		// use automatic reflection for primitives
		if (type == typeof(string)) { return ((string)source); } 
		else if (type == typeof(double)) { return ((double)source); } 
		else if (type == typeof(int)) { return ((int)source); } 
		else if (type == typeof(float)) { return ((float)source); } 
		else if (type == typeof(byte)) { return ((byte)source); } 
		else if (type == typeof(long)) { return ((long)source); } 
		else if (type == typeof(short)) { return ((short)source); } 
		else if (type == typeof(bool)) { return ((bool)source); } 
		else if (type == typeof(uint)) { return ((uint)source); } 
		else if (type == typeof(ulong)) { return ((ulong)source); } 
		else if (type == typeof(ushort)) { return ((ushort)source); } 
		else if (type == typeof(decimal)) { return ((double)source); } 
		else if (type == typeof(sbyte)) { return ((sbyte)source); } 
		else if (type == typeof(Guid)) { return (source.ToString()); }
		else if (type == typeof(DateTime)) { return ((DateTime)source).ToString("o"); }
		// Internally explicitly registered reflectors take top priority, then implicitly registered reflectors
		else if ((reflector = REFLECTOR(type)) != null) { return reflector(source); }
		// Otherwise, resume automatic reflection
		else if (type.IsArray) {
			JsonArray arr = new JsonArray();
			jval = arr;
			Array obj = source as Array;
			for (int i = 0; i < obj.Length; i++) {
				//Reflect that element and add it into the json array
				arr.Add(Reflect(obj.GetValue(i)));
			}
		} else if (typeof(IList).IsAssignableFrom(type)) {
			JsonArray arr = new JsonArray();
			jval = arr;
			IList obj = (IList)source;
			for (int i = 0; i < obj.Count; i++) {
				//Reflect that element and add it into the json array
				arr.Add(Reflect(obj[i]));
			}
		} else {
			PropertyInfo keys = type.GetProperty("Keys");

			PropertyInfo mapper = type.GetProperty("Item", new Type[] { typeof(string) });

			PropertyInfo count = type.GetProperty("Count", typeof(int));
			PropertyInfo indexer = type.GetProperty("Item", new Type[] { typeof(int) });

			PropertyInfo[] properties = type.GetProperties(PUBLIC_MEMBERS);
			FieldInfo[] fields = type.GetFields(PUBLIC_MEMBERS);

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

					object mappedObj = mapperGet.Invoke(source, new object[] { key });
					obj.Add(key, Reflect(mappedObj));
				}

			}

			if (count != null && indexer != null) {
				JsonArray arr = new JsonArray();
				MethodInfo countGet = count.GetGetMethod();
				MethodInfo indexerGet = indexer.GetGetMethod();
				int cnt = (int)countGet.Invoke(source, null);
				for (int i = 0; i < cnt; i++) {
					object indexedObj = indexerGet.Invoke(source, new object[] { i });
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

				// Skip Quaternion.eulerAngles, as it's redundant information
				// which is calculated from other information in the struct. 
				if (type == typeofQuaternion && property.Name == "eulerAngles") { continue; }
				// This property is deprecated as of UNITY_5. This prevents warnings when serializing rigidbodies.
				if (type == typeofRigidBody && property.Name == "useConeFriction") { continue; }

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

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonDeserializer

/// <summary> Exception thrown by <see cref="JsonDeserializer"/> when it is given bad input </summary>
public class JsonDeserializeFailedException : Exception {
	/// <summary> Current parser state </summary>
	public JsonDeserializer state { get; private set; }
	/// <summary> Internal message </summary>
	public string msg { get; private set; }
	/// <summary> Current line number </summary>
	public int line { get { return state.line; } }
	/// <summary> Current number of characters read on line </summary>
	public int col { get { return state.col; } }
	public JsonDeserializeFailedException(string msg, JsonDeserializer state) 
			: base() 
	{
		this.msg = msg;
		this.state = state;
	}
	/// <summary> Overrided to prevent doing a <see cref="JsonDeserializer.ToString"/> in the constructor. </summary>
	public override string Message { get { return $"{msg}{state}"; } }
}

/// <summary> Class holding logic for parsing Json text into JsonValues 
/// A new instance of this class is created automatically by Json.Parse() </summary>
public class JsonDeserializer {

	/// <summary> Json text that is being parsed </summary>
	public string json { get; private set; }
	/// <summary> Internal index. Do not modify outside of <see cref="index"/>.set </summary>
	private int __index;
	/// <summary> Current position. </summary>
	private int index {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get { return __index; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set {
			if (value < __index) {
				throw new JsonDeserializeFailedException("JsonDeserializer.index.set: Cannot set index below current value", this);
			}
			if (value > json.Length) {
				throw new JsonDeserializeFailedException("JsonDeserializer.index.set: Cannot set index outside input range", this);
			}
			for (;__index < value; __index++) {
				if (cur == '\n') { line += 1; col = 0; }
				else { col++; }
			}
		}
	}

	public override string ToString() {
		char c = cur;
		string s = ""+c;
		if (c == ' ') { s = "SPACE"; }
		if (c == '\n') { s = "\\n"; }
		if (c == '\r') { s = "\\r"; }
		if (c == '\t') { s = "\\t"; }
		StringBuilder area = new StringBuilder();
		string[] split = json.Split('\n');
		//Console.WriteLine($"ToString at {line},{col} in\n{json}");
		if (split.Length > 1) {
			int from = line - 5; if (from < 0) { from = 0; }
			int to = line + 5; if (to >= split.Length) { to = split.Length-1; }

			for (int i = from; i <= to; i++) {
				string si = "" + i;
				int spaces = 5 - si.Length;
				for (int k = 0; k < spaces; k++) { area.Append(' '); }
				area.Append(si);
				area.Append(':');
				if (line == i) {
					area.Append("->");
				} else {
					area.Append("  ");
				}
				//Console.WriteLine($"From {from} to {to} at {i} / {split.Length}");
				area.Append(split[i]);
				area.Append("\n");
			}
		} else {
			area.Append(json);
		}
		return $"@{line}:{col} cur='{s}', in\n{area}";
	}

	/// <summary> Current line number </summary>
	public int line { get; private set; }
	/// <summary> Current number of characters read on line </summary>
	public int col { get; private set; }

	/// <summary> quick access to the current character </summary>
	public char cur { get { return json[index]; } }

	/// <summary> quick access to the previous character, or a nullchar </summary>
	private char prev { get { return index > 0 ? json[index-1] : '\0'; } }

	/// <summary> quick access to the next character, or a nullchar </summary>
	private char next { get { return index < json.Length-1 ? json[index + 1] : '\0'; } }

	/// <summary> Constructor. Starts parsing from the begining of a given string </summary>
	public JsonDeserializer(string str) {
		json = str;
		line = 0;
		col = 0;
		index = 0;
	}

	/// <summary> Deserialize the Json text, and get back the resulting JsonValue </summary>
	public JsonValue Deserialize() {
		index = 0;
		if (json.Length == 0) { return null; }
		SkipWhitespace();
		return ProcessValue();
	}

	/// <summary> Process the next JsonValue, and recursively process any other necessary 
	/// JsonValues stored within. </summary>
	JsonValue ProcessValue() {
		if (cur == '[') { return ProcessArray(); }
		if (cur == '{') { return ProcessObject(); }
		if (cur == '"') { return ProcessString().JsonUnescapeString(); }
		int startIndex = index;
		while (index < json.Length && cur != ',' && cur != '}' && cur != ']' && !char.IsWhiteSpace(cur)) {
			index++;
			if (AtComment()) {
				break;
			}
		}
		string jval = json.Substring(startIndex, index - startIndex);

		if (jval == "true") { return JsonBool.TRUE; }
		if (jval == "false") { return JsonBool.FALSE; }
		if (jval == "null") { return JsonValue.NULL; }

		double dval;
		if (double.TryParse(jval, out dval)) { return dval; }

		return jval;
	}
	
	/// <summary> Logic for parsing content of a string </summary>
	string ProcessString() {
		// Don't include starting " in output string
		int startIndex = index + 1;

		while (true) {
			index++;
			while (cur != '\"') { index++; }
			// Found potential matching \"
			int j = index - 1;
			int count = 0;

			while (json[j] == '\\') { j--; count++; }

			//if there are an even number of backslashes, 
			//then they are all just backslashes and they aren't escaping the quote
			if (count % 2 == 0) { break; }
			//otherwise. if odd, the quote is being escaped and we need to keep searching for the close quote
		}
		
		// Consume final '"'
		index++;
		return json.Substring(startIndex, index - 1 - startIndex);
	}

	/// <summary> Logic for parsing contents of a JsonArray </summary>
	JsonArray ProcessArray() {
		index++;
		JsonArray array = new JsonArray();

		SkipWhitespace();
		if (cur == ']') {
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
		if (cur == '}') {
			index++;
			return obj;
		}
		while (true) {
			string key = ProcessKey();
			key = key.JsonUnescapeString();
			SkipWhitespace();
			
			if (cur == ',' || cur == '}') {
				obj.Add(key, true);
			}
			if (cur == ':') {
				index++;
				SkipWhitespace();
				JsonValue val = ProcessValue();
				obj.Add(key, val);
			}
			if (!MoveNext()) { break; }
		}

		return obj;
	}

	/// <summary> Logic for moving over characters until the next control character </summary>
	bool MoveNext() {
		while (index < json.Length && cur != ',' && cur != ']' && cur != '}') { index++; }

		if (cur == ',') {
			index++;
			SkipWhitespaceEnd();
			if (cur == ']' || cur == '}') {
#if XtoJSON_StrictCommaRules
				throw new Exception("Commas before end characters not allowed.");
#else
				index++;
				return false;
#endif
			}
			if (index >= json.Length) { return false; }

		} else {
			if (cur == ']' || cur == '}') {
				index++;
				return false;
			}
			if (index >= json.Length) { return false; }
		}

		return true;
	}

	/// <summary> Helper for testing 'alpha' characters (which can start keys directly) </summary>
	internal static bool IsAlpha(char c) {
		return c == '$' || c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
	}
	/// <summary> Helper for testing 'alpha' characters (which are valid direct key characters) </summary>
	internal static bool IsAlphaNum(char c) {
		return c == '$' || c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
	}
	/// <summary> Helper for determining being at a comment marker </summary>
	private bool AtComment() {
		return cur == '/' && next == '/';
	}

	/// <summary> Logic for extracting a string value from the text </summary>
	string ProcessKey() {
		int startIndex = -1;
		int endIndex = -1;
		if (cur == '"') {
			string key = ProcessString();
			SkipWhitespace();
			return key;
		} else {
			startIndex = index;
			if (IsAlpha(cur)) {
				while (IsAlphaNum(cur)) {
					index++;
				}
			}
			endIndex = index;
		}

		return json.Substring(startIndex, endIndex - startIndex).TrimEnd();
	}

	/// <summary> Logic to skip over whitespace until a non whitespace character or the end of the file. </summary>
	void SkipWhitespaceEnd() {
		bool comment = AtComment();

		while (comment || index < json.Length && char.IsWhiteSpace(cur)) {
			index++;
			if (comment) {
				if (cur == '\n') { comment = false; }
			}
			if (!comment) {

				if (cur == '/' && next == '/') { comment = true; }
			}
		}
	}
	/// <summary> Logic to skip to the next non-whitepace character, and there should be one. </summary>
	void SkipWhitespace() {
		bool comment = AtComment();
		while (comment || char.IsWhiteSpace(cur)) {
			index++;
			if (comment) {
				if (cur == '\n') { comment = false; }
			}
			if (!comment) {
				if (cur == '/' && next == '/') { comment = true; }
			}
		}
	}

	/// <summary> Deserialize in strict mode, rejecting all syntactic sugar. </summary>
	/// <returns> Deserialized JsonValue </returns>
	public JsonValue DeserializeStrict() {
		index = 0;
		if (json.Length == 0) { return false; }
		void Skip() { while (char.IsWhiteSpace(cur)) { index++; } }  
		void SkipEnd() { while (index < json.Length && char.IsWhiteSpace(cur)) { index++; } }  
		SkipEnd();
		JsonValue LintValue() {
			if (cur == '{') { return LintObject(); }
			if (cur == '[') { return LintArray(); }
			if (cur == '"') { return ProcessString().JsonUnescapeString(); }

			int startIndex = index;
			while (index < json.Length && cur != ',' && cur != '}' && cur != ']' && !char.IsWhiteSpace(cur)) { index++; }
			string jval = json.Substring(startIndex, index - startIndex);
			if (jval == "true") { return JsonBool.TRUE; }
			if (jval == "false") { return JsonBool.FALSE; }
			if (jval == "null") { return JsonValue.NULL; }
			double dval;
			if (double.TryParse(jval, out dval)) { return dval; }

			throw new JsonDeserializeFailedException($"\"{jval}\" does not match any JsonValue.", this);
		}

		JsonArray LintArray() {
			index++;
			JsonArray arr = new JsonArray();
			Skip();
			if (cur == ']') { index++; return arr; }
			while (true) {
				JsonValue val = LintValue();
				arr.Add(val);
				Skip();
				if (cur == ']') { index++; break; }
				if (cur != ',') { throw new JsonDeserializeFailedException("Strict Json requires ',' separating array values", this); }
				index++;
				Skip();
			}
			return arr;
		}
		JsonObject LintObject() {
			index++;
			JsonObject obj = new JsonObject();
			Skip();
			if (cur == '}') { index++; return obj; }
			while (true) {
				if (cur != '"') { throw new JsonDeserializeFailedException("Strict Json requires keys be in quotes.", this); }
				string key = ProcessString();
				key = key.JsonUnescapeString();
				Skip();
				if (cur != ':') { throw new JsonDeserializeFailedException("Strict Json requires ':' after object key", this); }
				index++;
				Skip();
				JsonValue value = LintValue();
				obj.Add(key, value);
				Skip();
				if (cur == '}') { index++; break; }
				if (cur != ',') { throw new JsonDeserializeFailedException("Strict Json requires ',' separating object pairs", this); }
				index++;
				Skip();
			}
			return obj;
		}

		JsonValue parsed = LintValue(); 
		SkipEnd();
		if (index != json.Length) { throw new JsonDeserializeFailedException("Strict Json requires no additional non-whitespace characters at the end", this); }
		return parsed;
	}




}

#endregion

#region Helpers

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonHelpers (helper class)

/// <summary> Class containing some helper functions. </summary>
public static class JsonHelpers {

	/// <summary> Escape characters to escape inside of Json text </summary>
	static string[] TOESCAPE = new string[] { "\\", "\"", "\b", "\f", "\n", "\r", "\t" };

	/// <summary> Escape character codes to use for escapes</summary>
	static string[] ESCAPE_CODES = new string[] { "\\\\", "\\\"", "\\b", "\\f", "\\n", "\\r", "\\t" };

	/// <summary> Is the type of an object is a given type? </summary>
	public static bool IsOf(this object o, Type t) { return o.GetType() == t; }
	/// <summary> Is an object is of an enum type? </summary>
	public static bool IsTypeOfEnum(this object o) { return o.GetType().BaseType == typeof(System.Enum); }
	/// <summary> Is an object of an array type? </summary>
	public static bool IsTypeOfArray(this object o) { return o.GetType().IsArray; }

	/// <summary> Replace escapeable characters with their escaped versions, except '/' and '\u'. </summary>
	public static string JsonEscapeString(this string str) {
		string s = str;
		for (int i = 0; i < TOESCAPE.Length; i++) {
			string escaped = TOESCAPE[i];
			string escapeCode = ESCAPE_CODES[i];
			s = s.Replace(escaped, escapeCode);
		}
		return s;
	}

	/// <summary> Replace Strict escapable characters with their escaped versions </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static string JsonFullEscapeString(this string str) {
		return str.JsonEscapeString().Replace("/", "\\/");
	}

	/// <summary> Replace escaped characters with their unescaped versions, except '\u'. </summary>
	public static string JsonUnescapeString(this string str) {
		string s = str;
		for (int i = 0; i < TOESCAPE.Length; i++) {
			string escaped = TOESCAPE[TOESCAPE.Length - i - 1];
			string escapeCode = ESCAPE_CODES[ESCAPE_CODES.Length - i - 1];

			s = s.Replace(escapeCode, escaped);
		}
		return s.Replace("\\/", "/");
	}

	/// <summary> Array of numeric types </summary>
	internal static Type[] numericTypes = new Type[] {
		typeof(double),
		typeof(int),
		typeof(float),
		typeof(long),
		typeof(decimal),
		typeof(byte),
		typeof(short),
		typeof(uint),
		typeof(ulong),
		typeof(ushort),
		typeof(sbyte),
	};

	/// <summary> is a type a numeric type? </summary>
	public static bool IsNumeric(this Type type) {
		return numericTypes.Contains(type);
	}

	public static bool IsJsonType(this Type type) {
		return type == typeof(JsonBool) ||
			type == typeof(JsonNumber) ||
			type == typeof(JsonString) ||
			type == typeof(JsonObject) ||
			type == typeof(JsonArray);
	}

	/// <summary> Get the numeric value of an object, as a double, regardless of the type that underpins it. </summary>
	/// <param name="num"> Object containing some numeric data. </param>
	/// <returns> double value of num </returns>
	public static double GetNumericValue(object num) {
		if (num.GetType() == typeof(double)) { return (double)num; }
		if (num.GetType() == typeof(int)) { return (double)(int)num; }
		if (num.GetType() == typeof(float)) { return (double)(float)num; }
		if (num.GetType() == typeof(long)) { return (double)(long)num; }
		if (num.GetType() == typeof(decimal)) { return (double)(decimal)num; }
		if (num.GetType() == typeof(byte)) { return (double)(byte)num; }
		if (num.GetType() == typeof(short)) { return (double)(short)num; }

		return 0;
	}

	/// <summary> Checks a MemberInfo for the System.ObsoleteAttribute </summary>
	/// <param name="info"> MemberInfo object to inspect </param>
	/// <returns> True, if the MemberInfo has the System.ObsoleteAttribute decorator, false otherwise. </returns>
	public static bool IsObsolete(this MemberInfo info) {
		return System.Attribute.GetCustomAttribute(info, typeof(System.ObsoleteAttribute)) != null;
	}

	/// <summary> Call the constructor of a Type, calling an empty constructor if it exists. </summary>
	public static object GetNewInstance(this Type type) {
		ConstructorInfo constructor = type.GetConstructor(new Type[] { });
		if (constructor != null) {
			return constructor.Invoke(new object[] { });
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


	/// <summary> 
	/// Test if a type implements IListT and determine the type of T. Taken from stack overflow 
	/// https://stackoverflow.com/questions/1043755/c-sharp-generic-list-t-how-to-get-the-type-of-t/13608408#13608408
	/// </summary>
	/// <param name="type">Unknown Type to check</param>
	/// <param name="innerType">Place to write back to once InnerType of the IList&lt;T&gt; is discovered</param>
	/// <returns>True If the Unknown Type is an IList&lt;T&gt;, false otherwise</returns>
	public static bool TryListOfWhat(Type type, out Type innerType) {
		if (type == null) { innerType = null; return false; }
		if (!typeof(IList).IsAssignableFrom(type)) { innerType = null; return false; }

		var interfaceTest = new Func<Type, Type>(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>) ? i.GetGenericArguments().Single() : null);
		innerType = interfaceTest(type);
		if (innerType != null) {
			return true;
		}

		foreach (var i in type.GetInterfaces()) {
			innerType = interfaceTest(i);
			if (innerType != null) {
				return true;
			}
		}

		return false;
	}

}
#endregion

#region Functional Operations

////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////
//JsonOperations (helper class)

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
					result[val] = -obj.GetNumber(val.stringVal);
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
					var val = thing[key];
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
				var key = lpair.Key;
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
					var key = val as JsonString;
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
				JsonString key = pair.Key;
				if (val.isObject) { result[key] = lhs.MultiplyRow(val as JsonObject); }
				if (val.isNumber) { result[key] = lhs.GetNumber(key) * val.numVal; }
			}

		} else {
			foreach (var field in lim) {
				if (field.isString) {
					JsonString key = field as JsonString;
					JsonValue val = rhs[key];

					if (val.isObject) { result[key] = lhs.MultiplyRow(val as JsonObject); }
					if (val.isNumber) { result[key] = lhs.GetNumber(key) * val.numVal; }
				}
			}

		}

		return result;
	}



	/// <summary> Calculates one result of a multiplication of one 'vector' times one 'row' of a matrix </summary>
	public static double MultiplyRow(this JsonObject lhs, JsonObject rhs) {
		double d = 0;

		foreach (var pair in rhs) {
			if (pair.Value.isNumber) { d += lhs.GetNumber(pair.Key) * pair.Value.numVal; }
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
				if (pair.Value.isNumber) { result[pair.Key] = pair.Value; }
			}

			foreach (var pair in rhs) {
				if (pair.Value.isNumber) { result[pair.Key] = result.GetNumber(pair.Key) + pair.Value.numVal; }
			}

		} else {
			foreach (var val in lim) {
				if (val.isString) {
					JsonString key = val as JsonString;
					result[key] = lhs.GetNumber(key) + rhs.GetNumber(key);
				}
			}

		}

		return result;
	}

	/// <summary> Clamp a value. by default range is [0, 1]</summary>
	static double Clamp(double val, double min = 0, double max = 1) {
		if (val < min) { return min; } else if (val > max) { return max; }
		return val;
	}

	/// <summary> Combine two 'vector' JsonObjects as if each number is a ratio between [0, 1]
	/// combines each 'dimension' as (1 - (1 - a) * (1 - b))
	/// so if one vector has .5 and the other one has .2, the result will be .6 </summary>
	public static JsonObject CombineRatios(this JsonObject lhs, JsonObject rhs, JsonArray lim = null) {
		JsonObject result = new JsonObject();

		if (lim == null) {
			foreach (var pair in lhs) {
				if (pair.Value.isNumber) { result[pair.Key] = Clamp(pair.Value.numVal); }
			}

			foreach (var pair in rhs) {
				if (pair.Value.isNumber) {
					double a = result.GetNumber(pair.Key);
					double b = Clamp(pair.Value.numVal);

					result[pair.Key] = 1 - (1 - a) * (1 - b);
				}
			}
		} else {
			foreach (var val in lim) {
				if (val.isString) {
					JsonString key = val as JsonString;
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

		string prefix = rule.Pull<string>("prefix", "");
		string suffix = rule.Pull<string>("suffix", "");
		string contains = rule.Pull<string>("contains", "");

		foreach (var pair in obj) {
			string key = pair.Key;

			if (("" == prefix || key.StartsWith(prefix))
				&& ("" == suffix || key.EndsWith(suffix))
				&& ("" == contains || key.Contains(contains))) {

				result.Add(pair.Key);
			}

		}


		return result;
	}


	/// <summary> Reduces arrays to single values for the given set of fields </summary>
	/// <param name="source"> Source Object to reduce arrays within </param>
	/// <param name="fields"> Fields to check for arrays to reduce </param>
	/// <param name="reductor"> Function to use to pick a value from the arrays </param>
	/// <returns></returns>
	public static JsonObject ReduceArrays(this JsonObject source, string[] fields, Func<JsonArray, JsonValue> reductor) {
		foreach (var field in fields) {
			var val = source[field];
			if (val.isArray) { source[field] = reductor(val as JsonArray); }
		}
		return source;
	}

	/// <summary> Gets the <see cref="JsonValue"/> in <paramref name="src"/> at the given <paramref name="path"/>, or null. </summary>
	/// <param name="src"> Source <see cref="JsonObject"/> or <see cref="JsonArray"/> to trace</param>
	/// <param name="path"> Path to follow, names and/or indexes separated by '.' and/or '/'. </param>
	/// <returns> Either the <see cref="JsonValue"/> at the given path, or null if the path cannot be traced. </returns>
	public static JsonValue XPath(this JsonValue src, string path) {
		if (src != null) {
			JsonValue trace = src;
			string[] jumps = path.Split('.', '/');
			//try {
			foreach (string jump in jumps) {
				if (trace.isObject || trace.isArray) {
					trace = trace[jump];
				}
			}
			return trace;
			//}
		}
		return JsonNull.instance;
	}

	/// <summary> Gets the <typeparamref name="T"/> in <paramref name="src"/> at the given <paramref name="path"/>, or null. </summary>
	/// <typeparam name="T"> Generic type to get </typeparam>
	/// <param name="src"> Source <see cref="JsonObject"/> or <see cref="JsonArray"/> to trace</param>
	/// <param name="path"> Path to follow, names and/or indexes separated by '.' and/or '/'. </param>
	/// <returns> Either the <typeparamref name="T"/> at the given path, or null if the path cannot be traced. </returns>
	public static T XPath<T>(this JsonValue src, string path) where T : JsonValue {
		if (src != null) {
			JsonValue trace = src;
			string[] jumps = path.Split('.', '/');
			//try {
			foreach (string jump in jumps) {
				if (trace.isObject || trace.isArray) {
					trace = trace[jump];
				}
			}
			return trace as T;
			//}
		}
		return null;
	}
}
#endregion


#region Extensions
namespace XtoJSON {

	/// <summary> Holds extensions for types outside of the JsonValue derived types </summary>
	public static class Extensions {

		/// <summary> Extension on object to copy arbitrary fields into it from another object. </summary>
		/// <typeparam name="T"> Generic Type </typeparam>
		/// <param name="target"> Target to recieve data </param>
		/// <param name="data"> Object providing the data </param>
		public static void ApplyValues<T>(this object target, T data) {
			JsonObject dataObj = Json.Reflect(data) as JsonObject;
			if (dataObj != null) {
				Json.ReflectInto(dataObj, target);
			}
		}

		/// <summary> More formal name for Parse() </summary>
		public static JsonValue ParseJson(this string json) { return Json.Parse(json); }
		/// <summary> More formal name for Parse() </summary>
		public static JsonValue DeserializeJson(this string json) { return Json.Parse(json); }

		/// <summary> More formal name for Reflect() </summary>
		public static JsonValue ReflectJson(this object obj) { return Json.Reflect(obj); }
		/// <summary> More formal name for Reflect() </summary>
		public static JsonValue SerializeJson(this object obj) { return Json.Reflect(obj); }

	}

}


#endregion

