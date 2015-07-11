//Definition Flags:

//XtoJSON_StrictCommaRules 
//	Enabled - When parsing JSON, makes commas before end-group characters cause an exception.
//	Disabled - Commas before end-group characters are allowed
//Example: { "blah":"bluh", } 
//	the above JSON will cause an exception to be thrown with the flag enabled, 
//	and will parse successfully with the flag disabled.

//XtoJSON_StringNumbers
//	Enabled - numbers are stored internally as a string value, and are converted to and from number types
//	Disabled - numbers are stored internally as a double value.
//May have minor performance implications.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;



#region Abstract/Primary stuff

public enum JsonType { String, Boolean, Number, Object, Array, Null }

public static class Json {
	public const string VERSION = "0.3.0";

	public static JsonValue Parse(string json) {
		JsonDeserializer jds = new JsonDeserializer(json);
		return jds.Deserialize();
	}

	public static JsonValue TryParse(string json) {
		try { return Parse(json); }
		catch {
			Console.WriteLine("Error! Couldn't Parse Json!");
			return null;
		}
	}
	
	public static JsonValue Reflect(object obj) { return JsonReflector.Reflect(obj); }
	
	public static object GetValue(JsonValue val, Type destType) { return JsonReflector.GetReflectedValue(val, destType); }
	public static T GetValue<T>(JsonValue val) {
		if (typeof(JsonObject).IsAssignableFrom(typeof(T))) { return (T)(object)(val as JsonObject); }
		else if (typeof(JsonArray).IsAssignableFrom(typeof(T))) { return (T)(object)(val as JsonArray); }

		object o = GetValue(val, typeof(T)); 
		if (o == null) { return default(T); }
		return (T)o;
	}
	
	public static void ReflectInto(JsonObject source, object destination) {
		if (source != null) {
			JsonReflector.ReflectInto(source, destination);
		}
	}

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
	
	public static JsonValue ParseJson(this string json) { return Parse(json); }
	public static JsonValue DeserializeJson(this string json) { return Parse(json); }

	public static JsonValue ReflectJson(this object obj) { return Reflect(obj); }
	public static JsonValue SerializeJson(this object obj) { return Reflect(obj); }

}

public abstract class JsonValue {

	protected readonly string HORIZONTAL_TAB = "\t";
	public static int CURRENT_INDENT = 0;
	public static readonly JsonValue NULL = JsonNull.instance;

	public bool isNumber { get { return JsonType == JsonType.Number; } }
	public bool isString { get { return JsonType == JsonType.String; } }
	public bool isBool { get { return JsonType == JsonType.Boolean; } }
	public bool isObject { get { return JsonType == JsonType.Object; } }
	public bool isArray { get { return JsonType == JsonType.Array; } }
	public bool isNull { get { return JsonType == JsonType.Null; } }

	public virtual int Count { get { throw new InvalidOperationException("This JsonValue is not a collection"); } }
	public virtual JsonValue this[int index] { 
		get { throw new InvalidOperationException("This JsonValue cannot be indexed with an integer"); }
		set { throw new InvalidOperationException("This JsonValue cannot be indexed with an integer"); }
	}
	public virtual JsonValue this[string key] { 
		get { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }
		set { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }
	}

	public virtual bool ContainsKey(string key) { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }
	public virtual bool ContainsAllKeys(params string[] keys) { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }
	public virtual bool ContainsAnyKeys(params string[] keys) { throw new InvalidOperationException("This JsonValue cannot be indexed with a string"); }

	public string GetString(string key) {
		if (ContainsKey(key)) {
			JsonValue thing = this[key];
			if (thing.isString) { return thing.stringVal; }
		}
		return "";
	}
	
	public bool GetBoolean(string key) {
		if (ContainsKey(key)) {
			JsonValue thing = this[key];
			if (thing.isBool) { return thing.boolVal; }
		}
		return false;
	}

	public float GetFloat(string key) { return (float) GetNumber(key); }
	public int GetInt(string key) { return (int) GetNumber(key); }
	public double GetNumber(string key) { 
		if (ContainsKey(key)) {
			JsonValue thing = this[key];
			if (thing.isNumber) { return thing.numVal; }
		}
		return 0;
	}

	public virtual bool boolVal { get { throw new InvalidOperationException("This JsonValue is not a boolean"); } }
	public virtual double numVal { get { throw new InvalidOperationException("This JsonValue is not a number"); } }
	public virtual float floatVal { get { throw new InvalidOperationException("This JsonValue is not a number"); } }
	public virtual double doubleVal { get { throw new InvalidOperationException("This JsonValue is not a number"); } }
	public virtual int intVal { get { throw new InvalidOperationException("This JsonValue is not a number"); } }
	public virtual string stringVal { get { throw new InvalidOperationException("This JsonValue is not a string"); } }
	

	internal JsonValue() { }

	public abstract JsonType JsonType { get; }
	public abstract override string ToString();
	public abstract string PrettyPrint();
	

	public static implicit operator JsonValue(string val) { return new JsonString(val); }
	public static implicit operator JsonValue(bool val) { return JsonBool.Get(val); }
	public static implicit operator JsonValue(double val) { return new JsonNumber(val); }
	public static implicit operator JsonValue(float val) { return new JsonNumber(val); }
	public static implicit operator JsonValue(int val) { return new JsonNumber(val); }

	
	public static explicit operator string(JsonValue val) { return val.stringVal; }
	public static explicit operator bool(JsonValue val) { return val.boolVal; }
	public static explicit operator double(JsonValue val) { return val.numVal; }
	public static explicit operator decimal(JsonValue val) { return (decimal) val.numVal; }
	public static explicit operator float(JsonValue val) { return (float) val.numVal; }
	public static explicit operator int(JsonValue val) { return (int) val.numVal; }
	
}

public abstract class JsonValueCollection : JsonValue {

	protected readonly string JsonVALUE_SEPARATOR = ",";

	internal JsonValueCollection() { }

	protected abstract string CollectionToPrettyPrint();
	protected abstract string CollectionToString();
	public override string ToString() { return BeginMarker + CollectionToString() + EndMarker; }

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

	protected abstract string BeginMarker { get; }
	protected abstract string EndMarker { get; }


}

#endregion


#region Primitives 

//JsonNull with a private constructor and an instance field
public class JsonNull : JsonValue {
	public string _value { get { return "null"; } }
	public static JsonNull instance = new JsonNull();


	private JsonNull() : base() { }

	public override JsonType JsonType { get { return JsonType.Null; } }
	public override string ToString() { return _value; }
	public override string PrettyPrint() { return _value; }

}

//JsonBool with a private constructor and two instance fields
public class JsonBool : JsonValue {
	private string _value;
	public static JsonBool TRUE = new JsonBool(true);
	public static JsonBool FALSE = new JsonBool(false);


	public override bool boolVal { get { return _value == "true"; } }

	public override JsonType JsonType { get { return JsonType.Boolean; } }

	public static implicit operator JsonBool(bool val) { return val ? TRUE : FALSE; }
	public static JsonBool Get(bool val) { return val ? TRUE : FALSE; }

	private JsonBool(bool value) : base() { _value = (""+value).ToLower(); }

	public override string ToString() { return _value; }
	public override string PrettyPrint() { return ToString(); }

}

public class JsonNumber : JsonValue {
	public override JsonType JsonType { get { return JsonType.Number; } }

#if XtoJSON_StringNumbers
	private string _value;

	public override double numVal { get { return Double.Parse(_value); } }
	public override double doubleVal { get { return Double.Parse(_value); } }
	public override float floatVal { get { return Single.Parse(_value); } }
	public override int intVal	{ get { return Int32.Parse(_value); } }


	protected static NumberFormatInfo formatter = defaultNumberFormat;
	static NumberFormatInfo defaultNumberFormat {
		get {
			NumberFormatInfo info = new NumberFormatInfo();
			info.NumberDecimalSeparator = ".";
			return info;
		}
	}

	public static implicit operator JsonNumber(double val) { return new JsonNumber(val); }
	public static implicit operator JsonNumber(decimal val) { return new JsonNumber(val); }
	public static implicit operator JsonNumber(float val) { return new JsonNumber(val); }
	public static implicit operator JsonNumber(int val) { return new JsonNumber(val); }
	internal JsonNumber(string value) : base() { _value = value; }

	public JsonNumber(int value) : this(value.ToString()) { }
	public JsonNumber(double value) : this(value.ToString(formatter)) { }
	public JsonNumber(decimal value) : this(value.ToString(formatter)) { }
	public JsonNumber(float value) : this(value.ToString(formatter)) { }
	public JsonNumber(byte value) : this(value.ToString()) { }

	public override string ToString() { return _value; }
	public override string PrettyPrint() { return ToString(); }
#else
	private double _value;

	public override double numVal { get { return _value; } }
	public override double doubleVal { get { return _value; } }
	public override float floatVal { get { return (float)_value; } }
	public override int intVal { get { return (int)_value; } }

	public static implicit operator JsonNumber(double val) { return new JsonNumber(val); }
	public static implicit operator JsonNumber(decimal val) { return new JsonNumber((double)val); }
	public static implicit operator JsonNumber(float val) { return new JsonNumber(val); }
	public static implicit operator JsonNumber(int val) { return new JsonNumber(val); }
	internal JsonNumber(double value) : base() { _value = value; }

	public JsonNumber(int value) : this((double)value) { }
	public JsonNumber(float value) : this((double)value) { }
	public JsonNumber(decimal value) : this((double)value) { }
	public JsonNumber(byte value) : this((double)value) { }

	public override string ToString() { return ""+_value; }
	public override string PrettyPrint() { return ""+_value; }

#endif
}

public class JsonString : JsonValue {
	private string _value;

	public override string stringVal { get { return _value; } }
	public override JsonType JsonType { get { return JsonType.String; } }

	public static implicit operator JsonString(string val) { return new JsonString(val); }
	public static implicit operator string(JsonString val) { return val._value; }
	public JsonString(string value) : base() { _value = value; }

	public override string ToString() { return ToJsonString(_value); }

	public override int GetHashCode() { return _value.GetHashCode(); }
	public override bool Equals(object other) { 
		JsonString otherAsString = other as JsonString;
		if (otherAsString != null) {
			return _value == otherAsString._value;
		}
		return _value.Equals(other); 
	}

	public override string PrettyPrint() { return ToString(); }


	public static string ToJsonString(string text) {
		if (text == null) { return "\"\""; }
		return "\"" + text.JsonEscapeString() + "\"";
	}
}


#endregion


#region Composites

public class JsonObject : JsonValueCollection, IEnumerable<KeyValuePair<JsonString, JsonValue>> {

	protected override string BeginMarker { get { return "{"; } }
	protected override string EndMarker { get { return "}"; } }

	private Dictionary<JsonString, JsonValue> data;
	private readonly string NAMEVALUEPAIR_SEPARATOR = ":";

	public override JsonType JsonType { get { return JsonType.Object; } }
	public override int Count { get { return data.Count; } }
	public override JsonValue this[string key] {
		get { 
			if (data.ContainsKey(key)) { return data[key]; } 
			return NULL; 
		}
		set { data[key] = value; }
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

	public JsonObject() : base() { data = new Dictionary<JsonString, JsonValue>(); }
	public JsonObject(JsonObject src) : this() { Add(src); }
	public JsonObject(Dictionary<JsonString, JsonValue> src) : base() { data = src; }

	public JsonObject Clone() { return new JsonObject(this); }
	
	public JsonObject Add(JsonString name, JsonValue value) {
		if (!data.ContainsKey(name)) {
			data.Add(name, value);
		}
		return this;
	}

	public JsonObject AddAll<T>(IEnumerable<KeyValuePair<string, T>> info) where T : JsonValue {
		foreach (var pair in info) {
			this[pair.Key] = pair.Value;
		}
		return this;
	}
	public JsonObject AddAllReflect<T>(IEnumerable<KeyValuePair<string, T>> info) {
		foreach (var pair in info) {
			this[pair.Key] = Json.Reflect(pair.Value);
		}
		return this;
	}

	public T Get<T>(string key) { return Json.GetValue<T>(this[key]); }
	
	public object GetPrimitive<T>(string name) { return GetPrimitive(name, typeof(T)); }
	public object GetPrimitive(string name, Type type) {
		JsonValue val = this[name];
		if (type == typeof(string) 	&& val.isString) { return val.stringVal; }
		if (type == typeof(float) 	&& val.isNumber) { return val.floatVal; } 
		if (type == typeof(double) 	&& val.isNumber) { return val.numVal; } 
		if (type == typeof(int) 	&& val.isNumber) { return val.intVal; } 
		if (type == typeof(bool) 	&& val.isBool) { return val.boolVal; }
		
		if (type.IsValueType && val.isObject) {
			return Json.GetValue(val, type);
		}
		
		return null;
	}
	
	
	public JsonObject Add(JsonObject other) { foreach (var pair in other) { this[pair.Key] = pair.Value; } return this; }
	public JsonObject Add(Dictionary<string, string> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	public JsonObject Add(Dictionary<string, double> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	public JsonObject Add(Dictionary<string, short> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	public JsonObject Add(Dictionary<string, float> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	public JsonObject Add(Dictionary<string, long> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	public JsonObject Add(Dictionary<string, byte> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }
	public JsonObject Add(Dictionary<string, int> info) { foreach (var pair in info) { this[pair.Key] = pair.Value; } return this; }

	public T Extract<T>(string key, T defaultValue = default(T)) {
		if (ContainsKey(key)) {
			JsonValue val = this[key];
			
			if (val.JsonType == Json.ReflectedType(defaultValue) ) { return Json.GetValue<T>(val); }
		}
		return defaultValue;
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return data.GetEnumerator();
	}
	
	public IEnumerator<KeyValuePair<JsonString, JsonValue>> GetEnumerator() {
		return data.GetEnumerator();
	}
	
	public IEnumerator<KeyValuePair<JsonString, JsonValue>> Pairs { 
		get {
			return data.GetEnumerator();
		} 
	}

	public Dictionary<JsonString, JsonValue> GetData() { return data; }
	
	public Dictionary<string, bool> ToDictOfBool() {
		Dictionary<string, bool> d = new Dictionary<string, bool>();
		foreach (var pair in data) {
			if (pair.Value.isBool) { d[pair.Key] = pair.Value.boolVal; }
		}
		return d;
	}
	public Dictionary<string, string> ToDictOfString() {
		Dictionary<string, string> d = new Dictionary<string, string>();
		foreach (var pair in data) {
			if (pair.Value.isString) { d[pair.Key] = pair.Value.stringVal; }
		}
		return d;
	}
	public Dictionary<string, double> ToDictOfDouble() {
		Dictionary<string, double> d = new Dictionary<string, double>();
		foreach (var pair in data) {
			if (pair.Value.isNumber) { d[pair.Key] = pair.Value.numVal; }
		}
		return d;
	}
	public Dictionary<string, float> ToDictOfFloat() {
		Dictionary<string, float> d = new Dictionary<string, float>();
		foreach (var pair in data) {
			if (pair.Value.isNumber) { d[pair.Key] = (float)pair.Value.numVal; }
		}
		return d;
	}
	public Dictionary<string, int> ToDictOfInt() {
		Dictionary<string, int> d = new Dictionary<string, int>();
		foreach (var pair in data) {
			if (pair.Value.isNumber) { d[pair.Key] = (int)pair.Value.numVal; }
		}
		return d;
	}
	
	//public void Add(string name, JsonValue value) { data.Add(name, value); }
	public JsonObject Remove(string key) { 
		if (ContainsKey(key)) { data.Remove(key); }
		return this;
	}

	public JsonObject Mask(IEnumerable<JsonString> mask) {
		JsonObject result = new JsonObject();
		foreach (JsonString str in mask) { result.Add(str, this[str]); }
		return result;
	}
	public JsonObject Mask(IEnumerable<string> mask) {
		JsonObject result = new JsonObject();
		foreach (string str in mask) { result.Add(str, this[str]); }
		return result;
	}


	public JsonObject Clear() { data.Clear(); return this; }

	public JsonObject Set(JsonObject other) {
		foreach (var pair in other) {
			this[pair.Key] = pair.Value;
		}
		return this;
	}

	public JsonObject Set<T>(Dictionary<string, T> info) {
		foreach (var pair in info) {
			this[pair.Key] = Json.Reflect(pair.Value);
		}
		return this;
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

public class JsonArray : JsonValueCollection, IEnumerable<JsonValue> {

	protected override string BeginMarker { get { return "["; } }
	protected override string EndMarker { get { return "]"; } }

	protected List<JsonValue> list;
	public List<JsonValue> GetList() { return list; }
	
	public override JsonType JsonType { get { return JsonType.Array; } }
	public override int Count { get { return list.Count; } }
	
	
	public override JsonValue this[int index] { 
		get { return list[index]; }
		set { list[index] = value; }
	}

	public JsonArray() : base() { list = new List<JsonValue>(); }
	public JsonArray(List<JsonValue> values) : base() { list = values; }
	public JsonArray(JsonArray src) : this() { AddAll(src); }
	
	public JsonArray Clone() { return new JsonArray(this); }
	
	//For some reason, this function had a 'if !list.Contains(value)' predicate before the add
	//Figured this was a mistake, but just in case it fucks up.
	public JsonArray Add(JsonValue val) { list.Add(val); return this; }
	public JsonArray AddAll(IEnumerable<JsonValue> info) {
		foreach (JsonValue val in info) { Add(val); }
		return this;
	}

	

	public JsonArray AddAll<T>(IEnumerable<T> info) where T : JsonValue {
		foreach (T val in info) { Add( (JsonValue) val ); }
		return this;
	}
	public JsonArray AddAllReflect<T>(IEnumerable<T> info) { 
		foreach (T val in info) { Add( Json.Reflect(val) ); }
		return this;
	}
	
	
	public JsonArray Clear() { list.Clear(); return this; }
	public bool Contains(JsonValue val) { return list.Contains(val); }
	public int IndexOf(JsonValue val) { return list.IndexOf(val); }
	public JsonArray Remove(JsonValue val) { list.Remove(val); return this; }
	public JsonArray Insert(int index, JsonValue val) { list.Insert(index, val); return this; }
	public JsonArray RemoveAt(int index) { list.RemoveAt(index); return this; }
	
	
	IEnumerator IEnumerable.GetEnumerator() {
		return list.GetEnumerator();
	}
	
	public IEnumerator<JsonValue> GetEnumerator() {
		return list.GetEnumerator();
	}
	
	
	public double[] ToDoubleArray() { return ToDoubleList().ToArray(); }
	public List<double> ToDoubleList() {
		List<double> arr = new List<double>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isNumber) { arr.Add(val.numVal); }
		}
		return arr;
	}
	
	public int[] ToIntArray() { return ToIntList().ToArray(); }
	public List<int> ToIntList() {
		List<int> arr = new List<int>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isNumber) { arr.Add((int)val.numVal); }
		}
		return arr;
	}
	
	public float[] ToFloatArray() { return ToFloatList().ToArray(); }
	public List<float> ToFloatList() {
		List<float> arr = new List<float>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isNumber) { arr.Add((float)val.numVal); }
		}
		return arr;
	}
	
	public bool[] ToBoolArray() { return ToBoolList().ToArray(); }
	public List<bool> ToBoolList() {
		List<bool> arr = new List<bool>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isBool) { arr.Add(val.boolVal); }
		}
		return arr;
	}
	
	public string[] ToStringArray() { return ToStringList().ToArray(); }
	public List<string> ToStringList() {
		List<string> arr = new List<string>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isString) { arr.Add(val.stringVal); }
		}
		return arr;
	}
	
	public JsonObject[] ToJsonObjectArray() { return ToJsonObjectList().ToArray(); }
	public List<JsonObject> ToJsonObjectList() {
		List<JsonObject> arr = new List<JsonObject>();
		for (int i = 0; i < Count; i++) {
			JsonValue val = this[i];
			if (val.isObject) { arr.Add(val as JsonObject); }
		}
		return arr;
	}
	
	public T[] ToArrayOf<T>() { return ToListOf<T>().ToArray(); }
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
	
	public object[] ToObjectArray(Type type) { return ToObjectList(type).ToArray(); }
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


public class JsonReflector {

	static MethodInfo toArrayOf = typeof(JsonArray).GetMethod("ToArrayOf");
	static BindingFlags publicMembers = BindingFlags.Instance | BindingFlags.Public;
	static BindingFlags publicMember = BindingFlags.Instance | BindingFlags.Public;
	
	public static object GetReflectedValue(JsonValue val, Type destType) {
		if (val == null) { return null; }
		object sval = null;
		if (val.isString && destType == typeof(string)) { sval = val.stringVal; }
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
	
	//Reflects info stored in 
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
						|| !property.IsReadable()) { continue; }

				MethodInfo propGet = property.GetGetMethod();
				
				object grabbed = propGet.Invoke(source, null);
				obj.Add(property.Name, Reflect(grabbed));
			}
			
			foreach (FieldInfo field in fields) {
				if (blacklist.Contains<string>(field.Name)) { continue; }
				
				object grabbed = field.GetValue(source);
				obj.Add(field.Name, Reflect(grabbed));
			}
			
		}
			
		return jval;
	}

}

#endregion


#region Deserializer

public class JsonDeserializer {
	private string json;
	private int index;

	private char next { get { return json[index]; } }

	public JsonDeserializer(string str) {
		index = 0;
		json = str;
	}

	public JsonValue Deserialize() {
		index = 0;
		SkipWhitespace();
		return ProcessValue();
	}

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

	string ProcessKey() {
		int startIndex = index + 1;
		int matchQuote = -1;
		while (json[index++] != ':') { 
			if (json[index] == '\"' && json[index-1] != '\\') {
				matchQuote = index;
			}
		}


		string result = json.Substring(startIndex, matchQuote - startIndex).TrimEnd();
		//Debug.Log("ProcessKey: " + startIndex + "-" + index + " [" + result + "]");
		return result;

	}

	void SkipWhitespaceEnd() {
		while (index < json.Length && char.IsWhiteSpace(next)) { index++; }
	}

	void SkipWhitespace() {
		while (char.IsWhiteSpace(next)) { index++; }
	}

}

#endregion

#region Helpers

public static class JsonHelperExtensions {

	static string[] TOESCAPE = new string[] { "\\", "\"", "\b", "\f", "\n", "\r", "\t" };

	public static bool IsOf(this object o, Type t) { return o.GetType() == t; }
	public static bool IsTypeOfEnum(this object o) { return o.GetType().BaseType == typeof(System.Enum); }
	public static bool IsTypeOfArray(this object o) { return o.GetType().IsArray; }

	public static string JsonEscapeString(this string str) {
		string s = str;
		for (int i = 0; i < TOESCAPE.Length; i++) {
			string escaped = TOESCAPE[i];
			s = s.Replace(escaped, "\\" + escaped);
		}
		return s;
	}

	public static string JsonUnescapeString(this string str) {
		string s = str;
		for (int i = 0; i < TOESCAPE.Length; i++) {
			string escaped = TOESCAPE[TOESCAPE.Length - i - 1];
			s = s.Replace("\\" + escaped, escaped);
		}
		return s;
	}


	static Type[] numericTypes = new Type[] { 
		typeof(int), 
		typeof(short), 
		typeof(long),
		typeof(double), 
		typeof(decimal), 
		typeof(byte)
	};
	
	public static bool IsNumeric(this Type type) {
		return numericTypes.Contains(type);
	}
	
	///Returns a new instance of an object, from the empty constructor if it exists.
	public static object GetNewInstance(this Type type) {
		ConstructorInfo constructor = type.GetConstructor(new Type[] {});
		if (constructor != null) {
			return constructor.Invoke(new object[] {});
		}
		return null;
	}
	
	
	public static bool IsWritable(this PropertyInfo p) {
		MethodInfo setter = p.GetSetMethod();
		return setter != null;
	}
	
	public static bool IsReadable(this PropertyInfo p) {
		MethodInfo getter = p.GetGetMethod();
		return getter != null;
	}
	
}
#endregion 

#region Functional Operations

public static class JsonOperations {

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

	public static double MultiplyRow(this JsonObject lhs, JsonObject rhs) {
		double d = 0;

		foreach (var pair in rhs) { 
			if (pair.Value.isNumber) { d += lhs.GetNumber(pair.Key.stringVal) * pair.Value.numVal; }
		}

		return d;
	}

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

	static double Clamp(double val, double min = 0, double max = 1) {
		if (val < min) { return min; } 
		else if ( val > max) { return max; } 
		return val;
	}

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












































