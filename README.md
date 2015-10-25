# XtoJSON
### Press X to Jason. Oh wait, that's Heavy Rain. Oops. This is a lightweight C# Json library.

Anyway, XtoJSON is, as it says, a lightweight JSON parser written in C#.  
I wrote it after being dissapointed that System.Json isn't easily available outside of silverlight, and not liking specific things about existing JSON libraries like JSONSharp and Json.Net.

It is loosely based on the System.Json classes, as well as the JSONSharp and Json.Net libraries. Consider XtoJSON the bastard child of these three libraries.

#### One-Line Serialize and Deserialize
```
//Unity Vector3
Vector3 pos = new Vector3(1, 2, 3);
string posJson = Json.Serialize(pos);
//Gives us: {
//	"x":1,
//	"y":2,
//	"z":3
//}
Vector3 dePos = Json.Deserialize<Vector3>(posJson);
//gives us: (1.0, 2.0, 3.0)
```


#### Sample Code: 
```
JsonObject obj = new JsonObject()
    .Add("name", "Bobby")
    .Add("class", "warrior")
    .Add("hp", 50)
    .Add("dead", false);
Console.WriteLine(obj.PrettyPrint());
Gives us the json:
{
    "name":"Bobby",
	"class":"warrior",
	"hp":50,
	"dead":false
}
```

### Features :
 * Serialize C# objects to JSON
 * Serializes objects as arrays that provide an indexer with an int index (this[int blah]) and int 'Count' properties
 * Serializes objects that provide an indexer with a string index (this[string blah]) and IEnumerable<string> 'Keys' properties
 * Serializes objects recusively, and as extensively as possible.
 * **BE WARNED: ANY OBJECT WITH LOOPING REFERENCES (ex CIRCULAR LISTS with last pointint to first ) WILL NOT TERMINATE SERIALIZATION (INFINITE LOOP)**
 * Deserialize JSON into internal JsonValue classes
 * Reflect information stored in a JsonObject into an arbitrary object.
 * Construct JSON objects/arrays from scratch (via code)
 * Query and Modify JavaScript-like objects and arrays
 * Implicit conversions from C# primitives to JsonValue types
 * Explicit conversions from JsonValue types back to primitives
 * Easy, quick to write syntax

### Classes:
 * Json - Quick access to JsonReflector and JsonDeserializer.
 * JsonValue - Base class for everything
 * JsonValueCollection - Base class for objects/arrays
 * JsonBool - Boolean primitive
 * JsonNumber - Double primitive
 * JsonString - String primitive
 * JsonObject - Functions like a Dictionary< string, JsonValue >
 * JsonArray - Functions like a List< JsonValue >
 * JsonReflector - Reflects an object's primitive fields. Used by Json.Reflect()
 * JsonDeserializer - Class that deserializes json strings. Used by Json.Parse()

