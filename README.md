# XtoJSON
### Json and a bit more
Readme last updated 2.2.1

XtoJSON is, a lightweight JSON parser written in C#.
I wrote it because:
- I was dissapointed that System.Json isn't easily available outside of silverlight
- I disliked things about existing JSON libraries like JSONSharp and Json.Net.
- I wanted more flexible parsing support
- I wanted intermediate objects robust enough for scripting

## Features
- Serialize C# objects and arrays to JSON
- Parse JSON to C# objects and arrays
- Provides access to intermediate representations of `JsonValue`s
	- `JsonArray` acts like `IList<JsonValue>` (Does not implement IList for fluent calls)
	- `JsonObject` acts like `IDictionary<JsonString, JsonValue>` (same as above)
	- Implicit conversions to and from `JsonValue` primitives make them seamless to work with
	- Reflect data from intermediate objects into C# objects
	- Generate intermediate objects from C# objects
	- Construct intermediate objects procedurally (via code)
- Parses both 'correct' and more leniant JSON styles.
	- double quotes around 'proper' keys optional
	- commas following last elements of collections allowed
	- c-style line comments are allowed 
- Can attempt to reflect values and fallback during query
	- `JsonObject.Pull<T>(string key, T defaultValue)`
- Can plug in arbitrary IDictionary types to use internally
	- Need multithreading support? Assign the following on startup:
	- `JsonObject.DictionaryGenerator = ()=>new ConcurrentDictionary<JsonString, JsonValue>();`
	
## Does not:
- Does not detect circular references when serializing objects
- Does not support `\u####` notation 
- Does not like single-quotes beginning strings

#### One-Line Serialize and Deserialize
- Turn Json into an object:
```csharp
Vector3 v = Json.To<Vector3>("{x:5,y:3,z:1}"); // (5.0, 3.0, 1.0)
```
- Turn an object into Json:
```csharp
Vector3 v = new Vector3(1,2,3);
string json = Json.ToJson(v); // {"x":1,"y":2,"z":3}
```

#### Intermediate Representations
- Turn Json into intermediate JsonObject
```csharp
// Multiple ways,
// Direct parse and interpret with as or cast...
JsonObject obj1 = Json.Parse("{x:5,y:3,z:1}") as JsonObject;
JsonObject obj2 = (JsonObject) Json.Parse("{x:5,y:3,z:1}");
// Generic parse
JsonObject obj3 = Json.Parse<JsonObject>("{x:5,y:3,z:1}");
```
- Turn any object into intermediate JsonObject
```csharp
JsonObject obj = Json.Reflect(someObject) as JsonObject;
```
- Construct and manipulate intermediate JsonObject
```csharp
JsonObject obj1 = new JsonObject(); // Empty Object
obj["key"] = "value"; // Can use indexing syntax as dictionary
obj["ayy"] = "lmao"; 
foreach (var pair in obj) { /* print($"{pair.Key}: {pair.value}"); */} // Can iterate as dictionary 
JsonObject obj2 = new JsonObject("key", "value", "ayy", "lmao"); // can construct via params[]
```
