# XtoJSON
###Press X to Jason. Oh wait, that's something else. This is a lightweight C# Json library.

Anyway, XtoJSON is, as it says, a lightweight JSON parser written in C#.  
I wrote it after being dissapointed that System.Json isn't easily available outside of silverlight.  
It's not perfect- I know that there are still some problems with it.  
It is loosely based on the System.Json classes, with some tweaks and changes thrown in.  

It's also all in one file- This is because I wanted to be able to easily drop it into a project I am working on.
I decided to do it this way because many other JSON libraries for C# are made of many many classes, 
when JSON is a pretty easy thing to handle in a small amount of code.

Other things were based on JSONSharp and Json.Net consider XtoJSON the bastard child of these three libraries.

Here is a quick example. (inside of some function in C#)
```
JsonObject obj = new JsonObject();
obj["name"] = "Bobby";
obj["class"] = "warrior";
obj["hp"] = 50;
obj["dead"] = false;

Console.WriteLine(obj.PrettyPrint());
//Gives us the json:
/*
{
    "name":"Bobby",
	"class":"warrior",
	"hp":50,
	"dead":false
}
//*/
```


###Features :
 * Serialize C# objects to JSON
 * Serializes objects as arrays that provide an indexer with an int index (this[int blah]) and int 'Count' properties
 * Serializes objects that provide an indexer with a string index (this[string blah]) and IEnumerable<string> 'Keys' properties
 * Serializes objects recusively, and as extensively as possible.
 * **BE WARNED: ANY OBJECT WITH LOOPING REFERENCES (CIRCULAR LISTS) WILL NOT TERMINATE SERIALIZATION (INFINITE LOOP)**
 * Deserialize JSON into internal JsonValue classes
 * Reflect information stored in a JsonObject into an arbitrary object.
 * Construct JSON objects/arrays from scratch (via code)
 * Query and Modify JavaScript-like objects and arrays
 * Implicit conversions from C# primitives to JsonValue types
 * Explicit conversions from JsonValue types back to primitives
 * Easy, quick to write syntax

###Classes:
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

