using UnityEngine;
using System;
using System.Collections.Generic;

using static XJS.Nodes;
using System.Reflection;
using Lib;

public partial class XJS {

	/// <summary> Delegate time to represent a very verbose Func type </summary>
	/// <param name="lhs"> Left Hand Side </param>
	/// <param name="rhs"> Right Hand Side </param>
	/// <returns> Result of some operation between lhs and rhs </returns>
	public delegate JsonValue BinaryOp(JsonValue lhs, JsonValue rhs);

	/// <summary> Class holding the logic for interpreting a program tree </summary>
	public class XJSInterpreter {

		/// <summary> Class to keep track of contexts and scopes </summary>
		public class Frame : System.Collections.Generic.List<JsonObject> {

			/// <summary> The 'this' object reference </summary>
			public JsonValue theThis = null;
			
			/// <summary> Base context object </summary>
			public JsonObject baseContext; 

			/// <summary> Gets the 'top' context object </summary>
			public JsonObject topContext { get { return (Count > 0) ? this[Count - 1] : baseContext; } }

			/// <summary> Basic constructor. Starts with an empty context and no 'this' </summary>
			public Frame() : base() { baseContext = new JsonObject(); }

			/// <summary> Constructor with a This object. Starts with the given context. </summary>
			/// <param name="baseContext"> Context at base level. </param>
			public Frame(JsonObject baseContext) { this.baseContext = baseContext; }

			/// <summary> Constructor with a This object. Starts with the given context. </summary>
			/// <param name="baseContext"> Context at base level. </param>
			public Frame(JsonValue theThis, JsonObject baseContext) { 
				this.theThis = theThis;
				this.baseContext = baseContext;
			}
			/// <summary> Indexer. Gets the value from the 'top' object, working its way back to the base, then the . </summary>
			/// <param name="key"> Key of value to get </param>
			/// <returns> Value of the key in the first object it sees, or null. </returns>
			public JsonValue this[string key] {
				get {
					for (int i = Count-1; i >= 0; i--) {
						if (this[i].ContainsKey(key)) { return this[i][key]; }
					}
					if (baseContext.ContainsKey(key)) {
						return baseContext[key];
					}
					if (theThis != null) {
						return DoGet(theThis, key);
					}
					return null;
				}
				set {
					for (int i = Count-1; i >= 0; i--) {
						if (this[i].ContainsKey(key)) { 
							this[i][key] = value; 
							return; 
						}
					}
					if (baseContext.ContainsKey(key)) {
						baseContext[key] = value;
					} else if (theThis != null) {
						DoSet(theThis, key, value);
					}
					
				}
			}

			/// <summary> Declares a new field, in the top context. </summary>
			/// <param name="name"> Name of new field </param>
			/// <param name="val"> Value to initialize new field to </param>
			public void Declare(string name, JsonValue val) { topContext[name] = val; }

			/// <summary> Adds another scope block on top of all existing scopes. </summary>
			public void Push() { Add(new JsonObject()); }

			/// <summary> Removes the 'top' scope block. </summary>
			public void Pop() { RemoveAt(Count-1); }


			public override string ToString() {
				StringBuilder str = "";
				str = str + $"Base: {baseContext}";
				for (int i = 0; i < Count; i++) {
					str = str + $"\nFrame[{i}]: {this[i]}";
				}
				return str.ToString();
			}
		}

		/// <summary> The global object reference. Always available, via 'global.xyz' </summary>
		public JsonObject global;
		
		/// <summary> Stack Frame History (of function calls) </summary>
		public Stack<Frame> frames;
		/// <summary> Current Frame </summary>
		public Frame frame { get { return frames.Peek(); } }

		/// <summary> Current Break target label. Null signifies normal execution. </summary>
		public string breakTarget = null;

		/// <summary> Current Continue target label. Null signifies normal execution. </summary>
		public string continueTarget = null;

		/// <summary> Current Return value. Actual null signifies normal execution. JsonNull.instance is a valid return value. </summary>
		public JsonValue returnValue = null;

		/// <summary> True if returning, false otherwise. </summary>
		public bool Returning { get { return !ReferenceEquals(returnValue, null); } }

		/// <summary> Exception generated, if any </summary>
		public Exception exception = null;


		/// <summary> Gets a value along a path </summary>
		/// <param name="path"> Path-formed string </param>
		/// <returns> Object in the current frame at the given path. </returns>
		public JsonValue GetAtPath(string path) {
			string[] stops = path.Split('.');
			if (stops.Length == 1) { return frame[path]; }
			return GetAtPath(stops);
		}

		/// <summary> Gets a value along a path </summary>
		/// <param name="stops"> Sequence of keys to navigate along path </param>
		/// <returns> Object in the frame at the given path. </returns>
		public JsonValue GetAtPath(string[] stops) {
			
			JsonValue val = frame[stops[0]];
			for (int i = 1; i < stops.Length; i++) {
				if (val == null) { return val; }

				val = val[stops[i]];
			}

			return val;
		}

		private static void Dbg(string str) {
			Debug.Log($"<color=#444400>{str}</color>");
		}
		/// <summary> Gets the container of some value. This is the last object down some chain of access. </summary>
		/// <param name="path"> Path-formed string </param>
		/// <returns> Object containing the item the path describes. May be a Frame or JsonObject, or null. </returns>
		public object TracePath(string path) {
			string[] stops = path.Split('.');
			//Dbg($"tracing path {{{path}}}");
			var result = TracePath(stops);
			//Dbg($"Result is {{{result}}}");
			return result;
		}

		/// <summary> Gets the container of the object decribed by the path stops. Does not follow the last stop. </summary>
		/// <param name="stops"> Sequence of keys to navigate along path </param>
		/// <returns> Second to last object along the provided path stops. May be a Frame or JsonValue, or null. </returns>
		public object TracePath(string[] stops) {
			if (stops.Length == 1) { 
				//Dbg($"No stops, so uses frame's this.");
				return frame.theThis; 
			}
			if (stops.Length == 2) { 
				//Dbg($"One stop, so direct access of frame");
				return frame[stops[0]]; 
			}

			JsonValue val = frame[stops[0]];
			for (int i = 1; i < stops.Length - 1; i++) {
				//Dbg($"Tracing at stop {i} {{{val}}}");
				string key = stops[i];
				if (val is JsonObject && val.ContainsKey(key)) {
					val = val[key];
				} else {
					return val;
				}
			}

			return val;
		}

		/// <summary> Try to get a thing from an object </summary>
		/// <param name="obj">object to get from</param>
		/// <param name="key">name of thing to get (field/property/method)</param>
		/// <returns>Thing to get, as a JsonValue</returns>
		public static JsonValue DoGet(object obj, string key) {
			//Dbg($"DoGet on {{{obj}}} ({obj.GetType()} for {key}");
			if (obj is JsonObject && (obj as JsonObject).ContainsKey(key)) {
				var result = (obj as JsonObject)[key];
				//Dbg($"Direct access on JsonObject[{key}] for {result} ({result.GetType()})");
				return result;
			}

			int ind;
			if (obj is JsonArray
				&& int.TryParse(key, out ind)
				&& (ind >= 0 && ind < (obj as JsonArray).Count)) {

				var result = (obj as JsonArray)[ind];
				//Dbg($"Direct access on JsonArray[{ind}] for {result} ({result.GetType()})");

			}

			//Dbg($"DoGet: Attempting to reflect {key} on {obj.GetType()}.");
			// Attempt to reflect value from object
			Type type = obj.GetType();
			MethodInfo methodCheck = type.GetMethod(key, PUBLIC_INSTANCE);
			if (methodCheck != null) {
				//Dbg($"DoGet: Creating Wrapper method {type}.{key}");
				return MakeFunction(methodCheck, obj);
			}

			PropertyInfo propertyCheck = type.GetProperty(key, PUBLIC_INSTANCE);
			if (propertyCheck != null) {
				//Dbg($"DoGet: Wrapping property {type}.{key}");
				MethodInfo getter = propertyCheck.GetGetMethod();
				if (getter != null) {
					try {
						object result = getter.Invoke(obj, EMPTY_ARGS);
						if (result is JsonValue) {
							return (JsonValue)result;
						} else {
							try {
								return Json.Reflect(result);
							} catch (Exception e) {
								Debug.LogError($"Scripting Error: Failed to invoke method {type}.{key}, result of type {result.GetType()} cannot convert into JsonValue: {e}");
							}
						}

					} catch (Exception e) {
						Debug.LogError($"Scripting Error: Failed to invoke method {type}.{key}: {e}");
					}
				} else {
					Debug.LogError($"Scripting Error: No getter method on property {type}.{key}.");
				}
			}

			FieldInfo fieldCheck = type.GetField(key, PUBLIC_INSTANCE);
			if (fieldCheck != null) {
				//Dbg($"DoGet: Access of field {type}.{key}");
				var result = fieldCheck.GetValue(obj);
				if (result is JsonValue) {
					return (JsonValue)result;
				} else {
					try {
						return Json.Reflect(result);
					} catch (Exception e) {
						Debug.LogError($"Scripting Error: Failed to get field {type}.{key}, result of type {result.GetType()} cannot convert into JsonValue: {e}");
					}
				}
			}


			return null;
		}

		/// <summary> </summary>
		/// <param name="obj"></param>
		/// <param name="key"> Key to set, may be index or key  </param>
		/// <param name="value"> Value to set </param>
		public static void DoSet(object obj, string key, JsonValue value) {
			if (obj is JsonBool || obj is JsonNull || obj is JsonNumber || obj is JsonString) {
				return;
			}
			if (obj is JsonObject && (obj as JsonObject).ContainsKey(key)) {
				(obj as JsonObject)[key] = value;
				return;
			}

			int ind;
			if (obj is JsonArray
				&& int.TryParse(key, out ind)
				&& (ind >= 0 && (ind < (obj as JsonArray).Count))) {
				(obj as JsonArray)[ind] = value;
				return;
			}

			Type type = obj.GetType();
			MethodInfo methodCheck = type.GetMethod(key, PUBLIC_INSTANCE);
			if (methodCheck != null) {
				// Note: Not sure what to do in this case. Probably want to log a message and not change anything.
			}

			PropertyInfo propertyCheck = type.GetProperty(key, PUBLIC_INSTANCE);
			if (propertyCheck != null) {
				MethodInfo setter = propertyCheck.GetSetMethod();
				if (setter != null) {
					object[] argArr = new object[1];
					try {
						argArr[0] = Json.GetValue(value, setter.GetParameters()[0].ParameterType);

					} catch (Exception e) {
						Debug.LogError($"Scripting Error: Failed to call setter {type}.{key}: {e}");
					}

				} else {
					Debug.LogError($"Scripting Error: No setter method on property {type}.{key}.");
				}
			}

		}


		/// <summary> Sets up a Frame to a default state. </summary>
		public XJSInterpreter() {
			global = new JsonObject();

			// Set up a debug object... (Debug.Log, wew)
			var proxy = global["Debug"] = new JsonObject();
			proxy["Log"] = new JsonFunction((context, prams)=>{Debug.Log(prams[0]); return JsonNull.instance; });

			frames = new Stack<Frame>();
		}

		/// <summary> Turns a method info and an object to bind it to into a JsonFunction that can be called from inside of a script. </summary>
		/// <param name="info"> MethodInfo to bind.</param>
		/// <param name="obj"> Object to bind the function call to. pass null for static objects. </param>
		/// <returns> JsonFunction that invokes <paramref name="info"/> on <paramref name="obj"/> and attempts to return the result as a JsonValue. Returns <see cref="JsonNull.instance"/> instead of plain nulls. </returns>
		public static JsonFunction MakeFunction(MethodInfo info, object obj = null) {

			return new JsonFunction((context, prams) => {
				//Dbg($"Invoking Wrapper Function for {info.DeclaringType}.{info.Name}");
				
				try {

					var pramInfos = info.GetParameters();
					object[] reflParams = new object[pramInfos.Length];

					for (int i = 0; i < pramInfos.Length; i++) {
						var pramInfo = pramInfos[i];
							
						try {

							var targetType = pramInfo.ParameterType;
							//Dbg($"Attempting to fill parameter {i} of type {pramInfo.ParameterType} with ({prams[i].GetType()}) {prams[i]}");
							if (targetType.IsAssignableFrom(prams[i].GetType())) {
								reflParams[i] = prams[i];
							} else {
								reflParams[i] = Json.GetValue(prams[i], targetType);
							}
						} catch (Exception) {
							//Dbg($"oops, using default. {e}");
							if (pramInfo.HasDefaultValue) {
								reflParams[i] = pramInfo.DefaultValue;
							} else {
								reflParams[i] = null;
							}
						}

					}
					

					object result = info.Invoke(obj, reflParams);
					
					if (ReferenceEquals(result, null)) { return JsonNull.instance; }

					// regular cast, generates error if it fails.
					return (JsonValue) result;
					
				} catch (Exception e) {
					// Expected to maybe be normal case ?
					// Maybe Return error object and let the scripting side figure it out?
					if (obj != null) {
						Type type = obj.GetType();
						Debug.LogError($"Scripting Error: Failed to invoke method {info.Name} on a {type} via JsonFunction. {e}");
					} else {
						Debug.LogError($"Scripting Error: Failed to invoke static method {info.DeclaringType}.{info.Name}. {e}");
					}

				}

				return JsonNull.instance;
			});

		}

		/// <summary> Turns a node into a JsonFunction that can be executed. </summary>
		/// <param name="funcNode"> Node holding program tree to execute </param>
		/// <returns> JsonFunction bound to nothing, which executes the given Program tree as its body. </returns>
		public JsonFunction MakeFunction(Node funcNode) {
			Node varlist = funcNode.Child("varlist");
			Node body = funcNode.Child("codeblock");

			return new JsonFunction((theThis, context, prams) => {
				// Create a new frame with the applied context and this 
				Frame next = new Frame(theThis, context);
				// Push initial scope block into stackframe 
				next.Push();
				for (int i = 0; i < varlist.DataListed; i++) {
					var paramName = varlist.Data(i);
					//Dbg($"Setting param {paramName} to {prams[i]}");
					// Copy vars into initial scope (they are assumed to line up)
					next.Declare(paramName, prams[i]);
					//Dbg(""+next);
				}
				next.Push();
				// Push stackframe onto stack
				frames.Push(next);

				// Execute function body 
				//Dbg($"Before\n{frame}");
				JsonValue result = Execute(body);
				//Dbg($"After\n{frame}");
				// Pop stackframe from stack 
				frames.Pop();

				// Return result 
				return result;
			});

		}

		private static readonly object[] EMPTY_ARGS = new object[0];

		const BindingFlags ANY_INSTANCE = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		const BindingFlags ANY_STATIC = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

		const BindingFlags PUBLIC_INSTANCE = BindingFlags.Public | BindingFlags.Instance;
		const BindingFlags PUBLIC_STATIC = BindingFlags.Public | BindingFlags.Static;

		const BindingFlags ANY_PUBLIC = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
		/// <summary> Loads methods from a given type to be called via reflection inside of scripts. Adds an object to the global namespace. </summary>
		/// <param name="type"> Type to reflect into a set of JsonFunctions </param>
		/// <param name="lim"> A set of strings to use to limit the methods that are reflected. </param>
		public void LoadMethods(Type type, string[] lim = null) {

			List<MethodInfo> methods = new List<MethodInfo>();

			if (lim == null) {
				MethodInfo[] ms = type.GetMethods(ANY_PUBLIC);
				methods.AddRange(ms);
			} else {
				foreach (var name in lim) {
					MethodInfo m = type.GetMethod(name, ANY_PUBLIC);
					if (m != null) {
						methods.Add(m);
					}
				}
			}

			JsonObject typeRep = new JsonObject();
			
			foreach (var method in methods) {
				typeRep[method.Name] = MakeFunction(method);	
			}
			
			global[type.Name] = typeRep;
		}

		public static BinaryOp ADD_OP = (lhs, rhs) => { return lhs + rhs; };
		public static BinaryOp SUB_OP = (lhs, rhs) => { return lhs - rhs; };

		public static BinaryOp MUL_OP = (lhs, rhs) => { return lhs * rhs; };
		public static BinaryOp DIV_OP = (lhs, rhs) => { return lhs / rhs; };
		public static BinaryOp MOD_OP = (lhs, rhs) => { return lhs / rhs; };

		public static BinaryOp AND_OP = (lhs, rhs) => { return lhs && rhs; };
		public static BinaryOp OR_OP = (lhs, rhs) => { return lhs || rhs; };

		public static BinaryOp EQ_OP = (lhs, rhs) => { return (lhs == rhs); };
		public static BinaryOp NE_OP = (lhs, rhs) => { return (lhs != rhs); };
		public static BinaryOp GT_OP = (lhs, rhs) => { return (lhs > rhs); };
		public static BinaryOp GE_OP = (lhs, rhs) => { return (lhs >= rhs); };
		public static BinaryOp LT_OP = (lhs, rhs) => { return (lhs < rhs); };
		public static BinaryOp LE_OP = (lhs, rhs) => { return (lhs <= rhs); };


		public JsonValue Execute(Node node) {
			// Quickly pop back up the stack if there is an exception
			if (exception != null) { return null; }
			if (node == null) {
				Debug.LogWarning("Atchung! Executing null node!");
				return null;
			}

			try {
				
				switch (node.type) {

					case PROGRAM: { // Entire program 
							// TBD: Request imports from context, declare export references. 
							frames.Push(new Frame(global));
							var result = Execute(node.Child("stmts"));

							// TBD: Register exports
							frames.Pop();

							return result;
						}

					case STMTLIST: // Top level statement list 
					case CODEBLOCK: { // arbitrary Codeblock between {}s
							// Codeblocks have scoping semantics. 
							// They push a new scope where new vars can be created...
							frame.Push();
							JsonValue last = null;
							for (int i = 0; i < node.NodesListed; i++) {
								last = Execute(node.Child(i));
								// Respect any ongoing flow-control, which will escape the scope blocks
								if (breakTarget != null || continueTarget != null) { break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								if (Returning) { return returnValue; }
							}
							// It finally undoes its scope block before returning. 
							frame.Pop();
							return last;
						}

						
					case DECSTMT: { // Declaration statement

							string target = node.Data("target");
							Node expr = node.Child("expr");
							JsonValue result = Execute(expr);
							frame.Declare(target, result);
							break;
						}

					case ARRAYLITERAL: { // Array Literal, obviously. 
							JsonArray arr = new JsonArray();

							if (node.NodesListed > 0) {
								// TBD: stitched/splatted arrays
								foreach (var child in node.nodeList) {
									arr.Add(Execute(child));
								}
							}
							
							return arr;
						}
					case OBJECTLITERAL: { // Object Literal, obviously. 
							JsonObject obj = new JsonObject();
							
							if (node.NodesMapped > 0) {
								foreach (var pair in node.nodeMap) {
									var name = pair.Key;
									var expr = pair.Value;
									obj[name] = Execute(expr);
								}
							}

							// TBD: stitched/splatted literals 

							return obj; 
						}
					case ASSIGN: { // Assignment statement 
							Node expr = node.Child("expr");
							string assignType = node.Data("assignType");
							// TODO: Rework this so it traces a path
							string target = node.Data("target");

							// TODO: Eventually figure out indexing
							//Node indexExpr = node.Child("indexExpr");
							string pre = node.Data("pre");
							string post = node.Data("post");
						
							if (pre != null) {
								BinaryOp op = pre == "++" ? ADD_OP : SUB_OP;
								JsonValue val = frame[target];
								JsonValue val2 = op(val, 1);
								frame[target] = val2;
						
								return val2;

							} else if (post != null) {
								BinaryOp op = post == "++" ? ADD_OP : SUB_OP;
								JsonValue val = frame[target];
								JsonValue val2 = op(val, 1);
								frame[target] = val2;
						
								return val;

							} else {
								JsonValue result = Execute(expr);
								if (assignType == "=") {
									frame[target] = result;
								} else {
									BinaryOp op;
									switch (assignType[0]) {
										case '+': op = ADD_OP; break;
										case '-': op = SUB_OP; break;
										case '*': op = MUL_OP; break;
										case '/': op = DIV_OP; break;
										case '%': op = MOD_OP; break;
										default: op = ADD_OP; break;
									}
									JsonValue val = frame[target];
									JsonValue val2 = op(val, result);
									frame[target] = val2;

									return val2;
								}
						
							}

							break;
						}
						//return frame[node.data["target"]] = Execute(node.nodes["expr"]);

					case ATOM: {
							string cdata = node.Data("const");
							Node inner = node.Child("inner");
							Node func = node.Child("func");

							if (cdata != null) {
								double d;
								if (double.TryParse(cdata, out d)) { return d; }
								return cdata;
							} else if (inner != null) {
						
								return Execute(inner);
						
							} else if (func != null) {
							
								// TBD: make Function declarations work
								// return MakeFunc(func);
							}
							
							break;
						}

					case VALUE: {
							string target = node.Data("target");
							JsonValue val = GetAtPath(target);
							Node indexExpr = node.Child("indexExpr");

							if (indexExpr != null) {
								JsonValue index = Execute(indexExpr);
								return val[index];
							}

							return val;
						}
					case RETURNSTMT: {
							JsonValue retVal = null;
							
							Node expr = node.Child("expr");
							if (expr != null) { retVal = Execute(expr); }

							// JsonNull is a valid return value, but null isn't.
							// 'return;' by itself always generates a JsonNull return value.
							return (returnValue = (ReferenceEquals(retVal, null) ? JsonNull.instance : retVal));
						}

						// All of these cases are repeatedly doing some operations over a list of values
					case EXPR:
					case BOOLTERM:
					case ARITHEXPR:
					case ARITHTERM: {
							BinaryOp defaultOP = null;
							if (node.type == EXPR) { defaultOP = OR_OP; } 
							else if (node.type == BOOLTERM) { defaultOP = AND_OP; } 
							else if (node.type == ARITHEXPR) { defaultOP = ADD_OP; } 
							else if (node.type == ARITHTERM) { defaultOP = MUL_OP; }

							// Starting value
							JsonValue value = Execute(node.Child("value"));

							for (int i = 0; i < node.NodesListed; i++) {
								JsonValue next = Execute(node.Child(i));
								BinaryOp op = defaultOP;
								if (i < node.DataListed) {
									if (node.Data(i) == "-") { op = SUB_OP; }
									if (node.Data(i) == "/") { op = DIV_OP; }
									if (node.Data(i) == "%") { op = MOD_OP; }
								}
								value = op(value, next);
							}

							return value;
						}

					case BOOLFACTOR: {
							JsonValue val = Execute(node.Child("value"));

							string comparison = node.Data("comparison");
							Node rhs = node.Child("rhs");
							
							if (rhs != null) {
								JsonValue rhsVal = Execute(rhs);
								BinaryOp compOp;
								switch (comparison) {
									case "==":	compOp = EQ_OP; break;
									case "!=":	compOp = NE_OP; break;
									case ">":	compOp = GT_OP; break;
									case ">=":	compOp = GE_OP; break;
									case "<":	compOp = LT_OP; break;
									case "<=":	compOp = LE_OP; break;
									default:	compOp = EQ_OP; break;
								}
								val = compOp(val, rhsVal);
							}

							bool negate = node.Data("negate") == "true";
							if (negate) { val = !val; }

							return val;
						}

					case FUNCDEC: {
							return MakeFunction(node);
						}

					case FUNCCALL: {
							//Debug.Log("Calling function (" + node.Data("target") + ")" + node);
							string target = node.Data("target");
							Node funcCallParams = node.Child("params");

							// Optional.
							//Node indexExpr = node.Child("indexExpr");
							JsonArray prams = new JsonArray();

							for (int i = 0; i < funcCallParams.NodesListed; i++) {
								//Dbg($"Executing FUNCCALL {target} parameter {i}");
								JsonValue pram = Execute(funcCallParams.Child(i));
								prams.Add(pram);
							}

							JsonFunction func = GetAtPath(target) as JsonFunction;
							if (func == null) {
								object targetObject = TracePath(target);
							
								string funcName = (target.Contains(".")) 
									? target.Substring(target.LastIndexOf('.')+1)
									: target; 
								func = DoGet(targetObject, funcName) as JsonFunction;
							}
							
							// The function will take care of adding a frame if it needs it.
							JsonValue result = func?.Invoke(global, prams);
							//Dbg($"Invoking function {target} got {{{result}}}");

							// If we got a return value, reset it to null to stop popping frames.
							returnValue = null;

							return result;
						}

					case IFSTMT: {
							if (Execute(node.Child("cond")).boolVal) {
								return Execute(node.Child("stmt"));
							}

							for (int i = 0; i < node.NodesListed; i+=2) {
								Node cond = node.Child(i);
								Node stmt = node.Child(i+1);
								if (Execute(cond).boolVal) {
									return Execute(stmt);
								}
							}

							Node elseStmt = node.Child("else");
							if (elseStmt != null) {
								return Execute(elseStmt);
							}
							
							break;
						}

					case CONTINUESTMT: {
							continueTarget = node.Data("target") ?? "!FixedBreak!";
							break;
						}

					case BREAKSTMT: {
							breakTarget = node.Data("target") ?? "!FixedBreak!";
							break;
						}
						
					case FORLOOP: // These 3 are actually super similar.
					case WHILELOOP:
					case DOWHILELOOP: {
							// Optional label
							string label = node.Data("label");

							// All standard loops have a body and condition...
							Node cond = node.Child("cond");
							Node body = node.Child("body");

							// For loops might have init and increment
							Node init = node.Child("init");
							Node incr = node.Child("incr");

							JsonValue last = null;

							// Only for loops have their own context...
							if (node.type == FORLOOP) { frame.Push(); }

							// If we have an init, execute it.
							if (init != null) { Execute(init); }
							
							// If we are a dowhile, execute the body once before checking the condition.
							if (node.type == DOWHILELOOP) { last = Execute(body); }
							
							
							// Repeatidly check the condition, and do the body if true...
							while (Execute(cond).boolVal) {

								last = Execute(body);

								// Check for breaking
								if (breakTarget != null) {
									if (breakTarget == label || breakTarget == "!FixedBreak!") { 
										// If it's a fixed label or ours, this is the place to break
										breakTarget = null; 
										// And we break.
									}
									// And if it's not, we also break.
									break; 
								}

								// Check for continuing
								if (continueTarget != null) {
									if (continueTarget == label || continueTarget == "!FixedContinue!") {
										// If it's a fixed label or ours, this is the place to restart.
										continueTarget = null; 
										// and we don't break... (and we still want to hit the increment)
										// So we don't continue anyway.
									} else {
										// If target continue on some other loop, we get outta here.
										break; 
									}
								}

								if (Returning) { return returnValue; }

								// Execute increment statement if it exists...
								if (incr != null) { Execute(incr); }

							}
							
							// Pop the context before we are done...
							if (node.type == FORLOOP) { frame.Pop(); }

							return last;
						}

					default: {
						break;
						}
				}
			} catch (Exception e) {
				Debug.LogWarning($"Exception Occurred when executing {node}\nException: {e}");
				exception = e;
			}



			return null;
		}


	}

	
}
