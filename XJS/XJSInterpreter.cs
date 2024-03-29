﻿#if UNITY_2017_1_OR_NEWER
#define UNITY
using UnityEngine;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using Lib;
using static XJS.Nodes;
using System.Collections.Concurrent;

public partial class XJS {

	/// <summary> Delegate time to represent a very verbose Func type </summary>
	/// <param name="lhs"> Left Hand Side </param>
	/// <param name="rhs"> Right Hand Side </param>
	/// <returns> Result of some operation between lhs and rhs </returns>
	public delegate JsonValue BinaryOp(JsonValue lhs, JsonValue rhs);

	/// <summary> Class holding the logic for interpreting a program tree </summary>
	public class Interpreter {


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
					} else {
						baseContext[key] = value;
					}
					
				}
			}

			/// <summary> Declares a new field, in the top context. </summary>
			/// <param name="name"> Name of new field </param>
			/// <param name="val"> Value to initialize new field to </param>
			public void Declare(string name, JsonValue val) { topContext[name] = val; }

			/// <summary> Adds a scope block on top of all existing scopes. </summary>
			/// <param name="obj"> scope block to add </param>
			public void Push(JsonObject obj) { Add(obj); }

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

		/// <summary> Sets up a Frame to a default state. </summary>
		public Interpreter() {
			global = new JsonObject(new ConcurrentDictionary<JsonString, JsonValue>());
			
			// Set up a debug object... (Debug.Log, wew)
			var proxy = global["Debug"] = new JsonObject();
			proxy["Log"] = new JsonFunction((context, prams) => { 
				Debug.Log("XJS Debug Log: \n" + prams[0].ToString()); 
				return JsonNull.instance; 
			});

		}

		/// <summary> Loads methods from a given type to be called via reflection inside of scripts. Adds an object to the global namespace. </summary>
		/// <param name="binding"> Object to bind methods to </param>
		/// <param name="type"> Type to reflect into a set of JsonFunctions </param>
		/// <param name="lim"> A set of strings to use to limit the methods that are reflected. </param>
		public void LoadMethods(object binding, Type type, string[] lim = null) {

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
			// Debug.Log($"Loading type {type.Name} with {methods.Count} methods");

			foreach (var method in methods) {
				// Debug.Log(method);
				typeRep[method.Name] = Interpreter.MakeFunction(method, binding);
			}

			global[type.Name] = typeRep;
		}

		/// <summary> Used to expose <see cref="ExecutionContext"/> for testing. </summary>
		/// <returns> Object of type <see cref="ExecutionContext"/> to test </returns>
		internal ExecutionContext NewContext() { return new ExecutionContext(global); }

		/// <summary> Asynchronously step through a given program. </summary>
		/// <param name="program"> Program tree to step through </param>
		/// <returns> Enumerator that controls the async execution of the given program </returns>
		public IEnumerator<JsonValue> Async(Node program) { return new ExecutionContext(global).Async(program); }

		/// <summary> Immediately run a given program. </summary>
		/// <param name="program"> Program tree to run </param>
		/// <returns> Result value of running the given program </returns>
		public JsonValue Execute(Node program) { return new ExecutionContext(global).Execute(program); }

		/// <summary> Immediately run a single <see cref="JsonFunction"/>. </summary>
		/// <param name="program"> Program function to run </param>
		/// <param name="args"> Args to execute program with </param>
		/// <returns> Result of running the given program </returns>
		public JsonValue Execute(JsonFunction program, JsonArray args) { return new ExecutionContext(global).Execute(program, args); }

		/// <summary> Get a global value from the interpreter </summary>
		/// <param name="key"> Key to look up </param>
		/// <returns> global value associated with key </returns>
		public JsonValue GetGlobal(string key) {
			return global[key];
		}

		/// <summary> Set a global value inside the interpreter </summary>
		/// <param name="key"> Key to set </param>
		/// <param name="value"> Value to associate with key </param>
		public void SetGlobal(string key, JsonValue value) {
			global[key] = value;
		}

		/// <summary> Class holding logic for actually executing a program </summary>
		public class ExecutionContext {

			/// <summary> Creates a new <see cref="ExecutionContext"/> with the given <see cref="JsonObject"/> as the global context </summary>
			/// <param name="global"> global context object. </param>
			public ExecutionContext(JsonObject global) {
				this.global = global;
				frames = new Stack<Frame>();
				frames.Push(new Frame(global));
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

			/// <summary> Current promise being awaited. Null signifies normal execution. </summary>
			public JsonPromise promise = null;

			/// <summary> True if returning, false otherwise. </summary>
			public bool Returning { get { return !ReferenceEquals(returnValue, null); } }

			/// <summary> Exception generated, if any </summary>
			public Exception exception = null;

			/// <summary> Get a global value from the interpreter </summary>
			/// <param name="key"> Key to look up </param>
			/// <returns> global value associated with key </returns>
			public JsonValue GetGlobal(string key) {
				return global[key];
			}

			/// <summary> Set a global value inside the interpreter </summary>
			/// <param name="key"> Key to set </param>
			/// <param name="value"> Value to associate with key </param>
			public void SetGlobal(string key, JsonValue value) {
				global[key] = value;
			}

			/// <summary> Gets a value along a path in the current stack.  </summary>
			/// <param name="path"> Path-formed string </param>
			/// <returns> Object in the current frame at the given path. </returns>
			public JsonValue GetAtPath(string path) {
				JsonValue result;
				//Debug.Log($"Getting {{{path}}} with frame\n{{{frame.ToString()}}}\nand global\n{{{global.PrettyPrint()}}}");
				string[] stops = path.Split('.');
				if (stops.Length == 1) { result = frame[path]; } else { result = GetAtPath(stops); }
				//Dbg($"GetAtPath {{{path}}} is {{{result}}}");
				return result;
			}

			/// <summary> Gets a value along a path in the current stack </summary>
			/// <param name="stops"> Sequence of keys to navigate along path </param>
			/// <returns> Object in the frame at the given path. </returns>
			public JsonValue GetAtPath(string[] stops) {
				var target = TracePath(stops);
				return DoGet(target, stops[stops.Length - 1]);
			}

			/// <summary> Sets a value along the given path in the current stack, to the given value. </summary>
			/// <param name="path"> Path-formed string </param>
			/// <param name="value"> Value to set </param>
			public void SetAtPath(string path, JsonValue value) {
				//Dbg($"SetAtPath {{{path}}} to {{{value}}}");
				string[] stops = path.Split('.');
				if (stops.Length == 1) { frame[path] = value; } else { SetAtPath(stops, value); }
			}
			/// <summary> Sets a value along the given path in the current stack, to the given value. </summary>
			/// <param name="stops"> sequence of keys to navigate along path </param>
			/// <param name="value"> Value to set </param>
			public void SetAtPath(string[] stops, JsonValue value) {
				var target = TracePath(stops);

				DoSet(target, stops[stops.Length - 1], value);
			}

			/// <summary> Debug helper method, colorizes text for easier recognition </summary>
			/// <param name="str"> text to print </param>
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
					//Dbg($"One stop, so frame");
					return frame;
				}
				if (stops.Length == 2) {
					//Dbg($"One stop, so directly access frame");
					return frame[stops[0]];
				}

				JsonValue val = frame[stops[0]];
				for (int i = 1; i < stops.Length - 1; i++) {
					//Dbg($"Tracing at stop {i} {{{val}}}");
					string key = stops[i];
					int k;
					if (val is JsonArray && int.TryParse(key, out k) && k >= 0 && k < (val as JsonArray).Count) {
						val = val[k];
					} else if (val is JsonObject && val.ContainsKey(key)) {
						val = val[key];
					} else {
						return null;
					}
				}

				return val;
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
					var args = new JsonArray();
					var argNames = new JsonArray();

					for (int i = 0; i < varlist.DataListed; i++) {
						var paramName = varlist.Data(i);
						//Dbg($"Setting param {paramName} to {prams[i]}");
						if (i < prams.Count) {
							// Copy vars into initial scope (they are assumed to line up)
							next.Declare(paramName, prams[i]);
						}

						//Dbg(""+next);
						argNames.Add(paramName);
					}
					args.AddAll(prams);

					next.Declare(ARGNAMES, argNames);
					next.Declare(ARGS, args);
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
			public JsonFunction MakeAsyncFunction(Node funcNode) {

				Node varlist = funcNode.Child("varlist");
				Node body = funcNode.Child("codeblock");

				return new JsonFunction((theThis, context, prams) => {
					// Create a new frame with the applied context and this 
					Frame next = new Frame(theThis, context);
					// Push initial scope block into stackframe 
					next.Push();
					var args = new JsonArray();
					var argNames = new JsonArray();

					for (int i = 0; i < varlist.DataListed; i++) {
						var paramName = varlist.Data(i);
						//Dbg($"Setting param {paramName} to {prams[i]}");
						if (i < prams.Count) {
							// Copy vars into initial scope (they are assumed to line up)
							next.Declare(paramName, prams[i]);
						}

						//Dbg(""+next);
						argNames.Add(paramName);
					}
					args.AddAll(prams);

					next.Declare(ARGNAMES, argNames);
					next.Declare(ARGS, args);
					next.Push();

					// Push stackframe onto stack
					frames.Push(next);

					// Execute function body 
					//Dbg($"Before\n{frame}");
					// JsonValue result = Execute(body);
					Stepper stepper = new Stepper(this, body);


					//Dbg($"After\n{frame}");
					// Pop stackframe from stack 
					//frames.Pop();

					// Return result 
					return stepper;
				});

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

			/// <summary> Returns an <see cref="IEnumerable{T}"/> of <see cref="JsonValue"/> which allows top-level statements in a program to be executed one at a time </summary>
			/// <param name="node"> Node to execute. </param>
			/// <returns> <see cref="IEnumerator{T}"/> of <see cref="JsonValue"/> for every result of each statement. </returns>
			/// <remarks>
			/// <paramref name="node"/> must be of <see cref="Node.type"/> == <see cref="XJS.Nodes.PROGRAM"/>. 
			/// </remarks>
			public IEnumerator<JsonValue> Async(Node node, JsonObject context = null) {
				// immediately stop when an exception is raised. 
				if (exception != null) { yield break; }
				if (node == null) {
					Debug.LogWarning("Atchung! Stepping on null node!");
					yield break;
				}

				switch (node.type) {

					case PROGRAM: {
							if (context == null) { context = new JsonObject(); }
							var toStep = node.Child("stmts");

							var stepper = Async(toStep, context);
							JsonValue last = null;
							while (stepper.MoveNext()) {
								last = stepper.Current;
								// Pause when we have an unfulfiled promise 
								while (last is JsonPromise p && !p.hasValue) { yield return p; }
								// Respect any ongoing flow-control, which will escape the scope blocks
								if (breakTarget != null || continueTarget != null) { yield break; }
								if (returnValue != null) { yield return returnValue; yield break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
							}
							yield return last;
							yield break;
						}

					case STMTLIST:
					case CODEBLOCK: {
							frame.Push();
							JsonValue last = null;
							for (int i = 0; i < node.NodesListed; i++) {

								frames.Push(frame);
								// last = Execute(stmts.Child(i));
								var stepper = Async(node.Child(i));
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									if (breakTarget != null || continueTarget != null) { yield break; }
									if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}

								frames.Pop();
							}

							yield return last;
							yield break;
						}

					case DECSTMT: {
							string target = node.Data("target");
							Node expr = node.Child("expr");
							var stepper = Async(expr);

							JsonValue last = null;
							while (stepper.MoveNext()) {
								last = stepper.Current;
								// Pause when we have an unfulfiled promise 
								while (last is JsonPromise p && !p.hasValue) { yield return p; }
								// Respect any ongoing flow-control, which will escape the scope blocks
								//if (breakTarget != null || continueTarget != null) { yield break; }
								//if (returnValue != null) { yield return returnValue; yield break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
							}
							frame.Declare(target, last);
							yield return last;
							yield break;
						}

					case ARRAYLITERAL: {
							JsonArray arr = new JsonArray();

							if (node.NodesListed > 0) {
								// TBD: stitched/splatted arrays
								foreach (var child in node.nodeList) {
									if (child.type == SPREAD) {
										var stepper = Async(child.Child("target"));
										JsonValue last = null;
										while (stepper.MoveNext()) {
											last = stepper.Current;
											// Pause when we have an unfulfiled promise 
											while (last is JsonPromise p && !p.hasValue) { yield return p; }
											// Respect any ongoing flow-control, which will escape the scope blocks
											//if (breakTarget != null || continueTarget != null) { yield break; }
											//if (returnValue != null) { yield return returnValue; yield break; }
											// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
										}

										if (last.isArray) {
											arr.AddAll(last as JsonArray);
										}

									} else {
										var stepper = Async(child);
										JsonValue last = null;
										while (stepper.MoveNext()) {
											last = stepper.Current;
											// Pause when we have an unfulfiled promise 
											while (last is JsonPromise p && !p.hasValue) { yield return p; }
											// Respect any ongoing flow-control, which will escape the scope blocks
											//if (breakTarget != null || continueTarget != null) { yield break; }
											//if (returnValue != null) { yield return returnValue; yield break; }
											// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
										}
										arr.Add(last);
									}
								}
							}

							yield return arr;
							yield break;
						}

					case OBJECTLITERAL: {
							JsonObject obj = new JsonObject();

							if (node.DataListed > 0) {
								foreach (var name in node.dataList) {
									var value = GetAtPath(name);
									obj[name] = value;
								}
							}

							if (node.NodesListed > 0) {
								foreach (var spread in node.nodeList) {
									var stepper = Async(spread.Child("target"));
									JsonValue last = null;
									while (stepper.MoveNext()) {
										last = stepper.Current;
										// Pause when we have an unfulfiled promise 
										while (last is JsonPromise p && !p.hasValue) { yield return p; }
										// Respect any ongoing flow-control, which will escape the scope blocks
										//if (breakTarget != null || continueTarget != null) { yield break; }
										//if (returnValue != null) { yield return returnValue; yield break; }
										// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
									}
									if (last.isObject) {
										foreach (var pair in last as JsonObject) {
											obj[pair.Key] = pair.Value;
										}
									}
								}
							}

							if (node.NodesMapped > 0) {
								foreach (var pair in node.nodeMap) {
									var name = pair.Key;
									var expr = pair.Value;
									var stepper = Async(expr);
									JsonValue last = null;
									while (stepper.MoveNext()) {
										last = stepper.Current;
										// Pause when we have an unfulfiled promise 
										while (last is JsonPromise p && !p.hasValue) { yield return p; }
										// Respect any ongoing flow-control, which will escape the scope blocks
										//if (breakTarget != null || continueTarget != null) { yield break; }
										//if (returnValue != null) { yield return returnValue; yield break; }
										// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
									}
									obj[name] = last;
								}
							}


							yield return obj;
							yield break;
						}

					case PATHEXPR: {
							StringBuilder s = "";

							for (int i = 0; i < node.NodesListed; i++) {
								if (i >= 1) {
									s.Append(".");
								}

								var stepper = Async(node.Child(i));
								JsonValue last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
								s.Append(last.stringVal);

							}

							yield return s.ToString();
							yield break;
						}

					case ASSIGN: {
							Node expr = node.Child("expr");
							string assignType = node.Data("assignType");
							// TODO: Rework this so it traces a path
							// string target = node.Data("target");
							Node targetPathExpr = node.Child("target");
							var stepper = Async(targetPathExpr);
							JsonValue last = null;
							while (stepper.MoveNext()) {
								last = stepper.Current;
								// Pause when we have an unfulfiled promise 
								while (last is JsonPromise p && !p.hasValue) { yield return p; }
								// Respect any ongoing flow-control, which will escape the scope blocks
								//if (breakTarget != null || continueTarget != null) { yield break; }
								//if (returnValue != null) { yield return returnValue; yield break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
							}
							string targetPath = last;

							string pre = node.Data("pre");
							string post = node.Data("post");

							if (pre != null) {
								BinaryOp op = pre == "++" ? ADD_OP : SUB_OP;

								JsonValue val = GetAtPath(targetPath); // frame[target];
								JsonValue val2 = op(val, 1);
								SetAtPath(targetPath, val2);
								//frame[target] = val2;

								yield return val2;
								yield break;
							} else if (post != null) {
								BinaryOp op = post == "++" ? ADD_OP : SUB_OP;
								JsonValue val = GetAtPath(targetPath); //frame[targetPath];
								JsonValue val2 = op(val, 1);
								SetAtPath(targetPath, val2);
								//frame[targetPath] = val2;

								yield return val;
								yield break;

							} else {
								stepper = Async(expr);
								last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}

								JsonValue result = last;
								if (assignType == "=") {
									SetAtPath(targetPath, result);
									yield return result;
									yield break;
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
									JsonValue val = GetAtPath(targetPath); //frame[targetPath];
									JsonValue val2 = op(val, result);
									SetAtPath(targetPath, val2);
									//frame[targetPath] = val2;

									yield return val2;
									yield break;
								}

							}
						}

					case ATOM: {
							string cdata = node.Data("const");
							Node inner = node.Child("inner");
							if (cdata != null) {
								double d;
								if (double.TryParse(cdata, out d)) { yield return d; } else { yield return cdata; }
								yield break;
							} else if (inner != null) {
								var stepper = Async(inner);
								JsonValue last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
								yield return last;
								yield break;
							}
							throw new Exception(@"Scripting error: Unhandled atom type. Should have a constant or inner expression.");
						}

					case VALUE: {
							Node targetPathNode = node.Child("target");
							var stepper = Async(targetPathNode);
							JsonValue last = null;
							while (stepper.MoveNext()) {
								last = stepper.Current;
								// Pause when we have an unfulfiled promise 
								while (last is JsonPromise p && !p.hasValue) { yield return p; }
								// Respect any ongoing flow-control, which will escape the scope blocks
								//if (breakTarget != null || continueTarget != null) { yield break; }
								//if (returnValue != null) { yield return returnValue; yield break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
							}
							JsonValue val = GetAtPath(last);
							yield return val;
							yield break;
						}

					case RETURNSTMT: {
							JsonValue retVal = null;
							Node expr = node.Child("expr");
							if (expr != null) {
								var stepper = Async(expr);
								JsonValue last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
								retVal = last;
							}
							yield return (returnValue = (ReferenceEquals(retVal, null) ? JsonNull.instance : retVal));
							yield break;
						}

					case EXPR:
					case BOOLTERM:
					case ARITHEXPR:
					case ARITHTERM: {
							BinaryOp defaultOP = null;
							if (node.type == EXPR) { defaultOP = OR_OP; } else if (node.type == BOOLTERM) { defaultOP = AND_OP; } else if (node.type == ARITHEXPR) { defaultOP = ADD_OP; } else if (node.type == ARITHTERM) { defaultOP = MUL_OP; }

							var stepper = Async(node.Child("value"));
							JsonValue last = null;
							while (stepper.MoveNext()) {
								last = stepper.Current;
								// Pause when we have an unfulfiled promise 
								while (last is JsonPromise p && !p.hasValue) { yield return p; }
								// Respect any ongoing flow-control, which will escape the scope blocks
								//if (breakTarget != null || continueTarget != null) { yield break; }
								//if (returnValue != null) { yield return returnValue; yield break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
							}
							JsonValue val = last;
							for (int i = 0; i < node.NodesListed; i++) {
								stepper = Async(node.Child(i));
								last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}

								BinaryOp op = defaultOP;
								if (i < node.DataListed) {
									if (node.Data(i) == "-") { op = SUB_OP; }
									if (node.Data(i) == "/") { op = DIV_OP; }
									if (node.Data(i) == "%") { op = MOD_OP; }
								}
								val = op(val, last);

							}

							yield return val;
							yield break;
						}

					case ARITHFACTOR: {
							var stepper = Async(node.Child("negate"));
							JsonValue last = null;
							while (stepper.MoveNext()) {
								last = stepper.Current;
								// Pause when we have an unfulfiled promise 
								while (last is JsonPromise p && !p.hasValue) { yield return p; }
								// Respect any ongoing flow-control, which will escape the scope blocks
								//if (breakTarget != null || continueTarget != null) { yield break; }
								//if (returnValue != null) { yield return returnValue; yield break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
							}
							yield return -last.doubleVal;
							yield break;
						}

					case BOOLFACTOR: {
							var stepper = Async(node.Child("value"));
							JsonValue last = null;
							while (stepper.MoveNext()) {
								last = stepper.Current;
								// Pause when we have an unfulfiled promise 
								while (last is JsonPromise p && !p.hasValue) { yield return p; }
								// Respect any ongoing flow-control, which will escape the scope blocks
								//if (breakTarget != null || continueTarget != null) { yield break; }
								//if (returnValue != null) { yield return returnValue; yield break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
							}
							JsonValue val = last;
							Node rhs = node.Child("rhs");
							if (rhs != null) {
								stepper = Async(rhs);
								last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
								JsonValue rhsVal = last;
								BinaryOp compOp;
								string comparison = node.Data("comparison");
								switch (comparison) {
									case "==": compOp = EQ_OP; break;
									case "!=": compOp = NE_OP; break;
									case ">": compOp = GT_OP; break;
									case ">=": compOp = GE_OP; break;
									case "<": compOp = LT_OP; break;
									case "<=": compOp = LE_OP; break;
									default: compOp = EQ_OP; break;
								}
								val = compOp(val, rhsVal);
							}

							bool negate = node.Data("negate") == "true";
							if (negate) { val = !val; }

							yield return val;
							yield break;
						}

					case FUNCDEC: {
							//yield return MakeAsyncFunction(node);
							yield return MakeAsyncFunction(node);
							yield break;
						}

					case FUNCCALL: {
							//Debug.Log("Calling function (" + node.Data("target") + ")" + node);
							Node targetPathNode = node.Child("target");
							var stepper = Async(targetPathNode);
							JsonValue last = null;
							while (stepper.MoveNext()) {
								last = stepper.Current;
								// Pause when we have an unfulfiled promise 
								while (last is JsonPromise p && !p.hasValue) { yield return p; }
								// Respect any ongoing flow-control, which will escape the scope blocks
								//if (breakTarget != null || continueTarget != null) { yield break; }
								//if (returnValue != null) { yield return returnValue; yield break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
							}
							string target = last;
							Node funcCallParams = node.Child("params");
							//Dbg($"Calling Function {target} with {funcCallParams.NodesListed} parameter(s)");

							// Optional.
							//Node indexExpr = node.Child("indexExpr");
							JsonArray prams = new JsonArray();

							for (int i = 0; i < funcCallParams.NodesListed; i++) {
								stepper = Async(funcCallParams.Child(i));
								last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									// if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
								JsonValue pram = last;

								// JsonValue pram = Execute(funcCallParams.Child(i));
								//Dbg($"Executing FUNCCALL {target} parameter {{{i}}} = {{{pram}}}");
								prams.Add(pram);
							}

							JsonFunction func = GetAtPath(target) as JsonFunction;

							if (func == null) {
								object targetObject = TracePath(target);

								string funcName = (target.Contains("."))
									? target.Substring(target.LastIndexOf('.') + 1)
									: target;

								func = DoGet(targetObject, funcName) as JsonFunction;
							}

							if (func == null) {
								throw new Exception($"Unknown Function {target}");
							}


							// The function will take care of adding a frame if it needs it.
							JsonValue result = func.Invoke(global, prams);
							//Dbg($"Invoking function {target} got {{{result}}}");

							// If we got a return value, reset it to null to stop popping frames.
							returnValue = null;

							yield return result;
							yield break;
						}

					case IFSTMT: {
							var stepper = Async(node.Child("cond"));
							JsonValue last = null;
							while (stepper.MoveNext()) {
								last = stepper.Current;
								// Pause when we have an unfulfiled promise 
								while (last is JsonPromise p && !p.hasValue) { yield return p; }
								// Respect any ongoing flow-control, which will escape the scope blocks
								//if (breakTarget != null || continueTarget != null) { yield break; }
								//if (returnValue != null) { yield return returnValue; yield break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
							}
							if (last.boolVal) {
								stepper = Async(node.Child("stmt"));
								last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
								yield return last;
								yield break;
							}

							for (int i = 0; i < node.NodesListed; i += 2) {
								Node cond = node.Child(i);
								Node stmt = node.Child(i + 1);
								stepper = Async(cond);
								last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
								if (last.boolVal) {
									stepper = Async(stmt);
									last = null;
									while (stepper.MoveNext()) {
										last = stepper.Current;
										// Pause when we have an unfulfiled promise 
										while (last is JsonPromise p && !p.hasValue) { yield return p; }
										// Respect any ongoing flow-control, which will escape the scope blocks
										//if (breakTarget != null || continueTarget != null) { yield break; }
										//if (returnValue != null) { yield return returnValue; yield break; }
										// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
									}
									yield return last;
									yield break;
								}
							}

							Node elseStmt = node.Child("else");
							if (elseStmt != null) {
								stepper = Async(elseStmt);
								last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
								yield return last;
							}
							yield break;
						}

					case CONTINUESTMT: {
							continueTarget = node.Data("target") ?? "!FixedContinue!";
							yield break;
						}

					case BREAKSTMT: {
							breakTarget = node.Data("target") ?? "!FixedBreak!";
							yield break;
						}

					case EACHLOOP: {
							//Dbg($"In Eachloop");
							// Each loops are weird compared to standard loops.
							string label = node.Data("label");

							// They read a collection value 
							Node targetPathNode = node.Child("path");
							// Map each item or pair in the collection to one or more names
							string name = node.Data("name");
							Node nameList = node.Child("names");
							// And then repeatedly call a body with each var being set into the names.
							Node body = node.Child("body");

							// Look up the name if it was not set solo
							if (name == null) { name = nameList.Data(0); }

							string name2 = (nameList != null && nameList.DataListed > 1) ? nameList.Data(1) : null;
							//Look up collection to iterate 
							var stepper = Async(targetPathNode);
							JsonValue last = null;
							while (stepper.MoveNext()) {
								last = stepper.Current;
								// Pause when we have an unfulfiled promise 
								while (last is JsonPromise p && !p.hasValue) { yield return p; }
								// Respect any ongoing flow-control, which will escape the scope blocks
								//if (breakTarget != null || continueTarget != null) { yield break; }
								//if (returnValue != null) { yield return returnValue; yield break; }
								// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
							}
							string targetPath = last;

							var target = GetAtPath(targetPath);

							frame.Push();
							frame.Declare(name, null);
							//Dbg($"EACHLOOP: Declared {name} ");
							if (name2 != null) {
								frame.Declare(name2, null);
							}

							JsonValue last2 = null;
							if (target is JsonArray) {
								foreach (var item in (target as JsonArray)) {
									//Dbg($"EACHLOOP on Array: set {name} to {item} ");
									frame[name] = item;
									stepper = Async(body);
									last = null;
									while (stepper.MoveNext()) {
										last = stepper.Current;
										// Pause when we have an unfulfiled promise 
										while (last is JsonPromise p && !p.hasValue) { yield return p; }
										// Respect any ongoing flow-control, which will escape the scope blocks
										//if (breakTarget != null || continueTarget != null) { yield break; }
										//if (returnValue != null) { yield return returnValue; yield break; }
										// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
									}
									last2 = last;

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

									if (Returning) {
										frame.Pop();
										yield return returnValue;
										yield break;
									}

								}
							} else if (target is JsonObject) {
								foreach (var pair in (target as JsonObject)) {
									var key = pair.Key;
									var val = pair.Value;
									//Dbg($"EACHLOOP on Object: set {name},{name2} to {key},{val} ");
									frame[name] = key;
									frame[name2] = val;

									stepper = Async(body);
									last = null;
									while (stepper.MoveNext()) {
										last = stepper.Current;
										// Pause when we have an unfulfiled promise 
										while (last is JsonPromise p && !p.hasValue) { yield return p; }
										// Respect any ongoing flow-control, which will escape the scope blocks
										//if (breakTarget != null || continueTarget != null) { yield break; }
										//if (returnValue != null) { yield return returnValue; yield break; }
										// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
									}
									last2 = last;

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

									if (Returning) {
										frame.Pop();

										yield return returnValue;
										yield break;
									}

								}
							}

							frame.Pop();

							yield return last2;
							yield break;
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
							JsonValue last2 = null;

							// Only for loops have their own context...
							if (node.type == FORLOOP) { frame.Push(); }

							// If we have an init, execute it.
							if (init != null) {
								var stepper = Async(init);
								// Execute(init); 
								last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
							}

							// If we are a dowhile, execute the body once before checking the condition.
							if (node.type == DOWHILELOOP) {
								var stepper = Async(body);
								last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
								last2 = last;
							}


							// Repeatidly check the condition, and do the body if true...
							while (Execute(cond).boolVal) {
								var stepper = Async(body);
								last = null;
								while (stepper.MoveNext()) {
									last = stepper.Current;
									// Pause when we have an unfulfiled promise 
									while (last is JsonPromise p && !p.hasValue) { yield return p; }
									// Respect any ongoing flow-control, which will escape the scope blocks
									//if (breakTarget != null || continueTarget != null) { yield break; }
									//if (returnValue != null) { yield return returnValue; yield break; }
									// Any returning implies there will be a frame removed anyway, so this doesn't need to pop the frame from the stack...
								}
								last2 = last;

								// Check for breaking
								if (breakTarget != null) {
									//Dbg($"Loop Breaking to {breakTarget}");
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
									//Dbg($"Loop continuing to {continueTarget}");
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

								if (Returning) {
									// Pop the context before we are done...
									if (node.type == FORLOOP) { frame.Pop(); }

									yield return returnValue;
									yield break;
								}

								// Execute increment statement if it exists...
								if (incr != null) { Execute(incr); }

							}

							// Pop the context before we are done...
							if (node.type == FORLOOP) { frame.Pop(); }

							yield return last2;
							yield break;
						}

					default: {
							break;
						}
				}
				yield break;
			}

			/// <summary> Executes a JsonFunction in the context of this interpreter. </summary>
			/// <param name="fn"> Function to execute. </param>
			/// <param name="args"> Arguments to pass to function </param>
			/// <returns> Result of function's execution. </returns>
			public JsonValue Execute(JsonFunction fn, JsonArray args = null) {
				if (args == null) { args = new JsonArray(); }
				return fn.Invoke(global, args);
			}

			/// <summary> Fully executes a program tree. </summary>
			/// <param name="node"> Root node of program tree to execute. </param>
			/// <returns> Result of execution </returns>
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
										// TBD: Request imports from context, declare export references
								var result = Execute(node.Child("stmts"));

								// TBD: Register exports
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
								return result;
							}

						case ARRAYLITERAL: { // Array Literal, obviously. 
								JsonArray arr = new JsonArray();

								if (node.NodesListed > 0) {
									// TBD: stitched/splatted arrays
									foreach (var child in node.nodeList) {
										if (child.type == SPREAD) {
											var it = Execute(child.Child("target"));
											if (it.isArray) {
												arr.AddAll(it as JsonArray);
											}

										} else {

											arr.Add(Execute(child));
										}
									}
								}

								return arr;
							}
						case OBJECTLITERAL: { // Object Literal, obviously. 
								JsonObject obj = new JsonObject();

								if (node.DataListed > 0) {
									foreach (var name in node.dataList) {
										var value = GetAtPath(name);
										obj[name] = value;
									}
								}

								if (node.NodesListed > 0) {
									foreach (var spread in node.nodeList) {
										var it = Execute(spread.Child("target"));
										if (it.isObject) {
											foreach (var pair in it as JsonObject) {
												obj[pair.Key] = pair.Value;
											}
										}
									}
								}

								if (node.NodesMapped > 0) {
									foreach (var pair in node.nodeMap) {
										var name = pair.Key;
										var expr = pair.Value;
										obj[name] = Execute(expr);
									}
								}


								return obj;
							}
						case PATHEXPR: {
								StringBuilder s = "";
								s.Append(Execute(node.Child(0)).stringVal);

								for (int i = 1; i < node.NodesListed; i++) {
									s.Append(".");
									s.Append(Execute(node.Child(i)).stringVal);
								}
								//Dbg($"Created {s} from PATHEXPR: </color>{node}");

								return s.ToString();
							}
						case ASSIGN: { // Assignment statement 
								Node expr = node.Child("expr");
								string assignType = node.Data("assignType");
								// TODO: Rework this so it traces a path
								// string target = node.Data("target");
								Node targetPathExpr = node.Child("target");
								string targetPath = Execute(targetPathExpr).stringVal;

								string pre = node.Data("pre");
								string post = node.Data("post");

								if (pre != null) {
									BinaryOp op = pre == "++" ? ADD_OP : SUB_OP;

									JsonValue val = GetAtPath(targetPath); // frame[target];
									JsonValue val2 = op(val, 1);
									SetAtPath(targetPath, val2);
									//frame[target] = val2;

									return val2;

								} else if (post != null) {
									BinaryOp op = post == "++" ? ADD_OP : SUB_OP;
									JsonValue val = GetAtPath(targetPath); //frame[targetPath];
									JsonValue val2 = op(val, 1);
									SetAtPath(targetPath, val2);
									//frame[targetPath] = val2;

									return val;

								} else {
									JsonValue result = Execute(expr);
									if (assignType == "=") {
										SetAtPath(targetPath, result);
										return result;

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
										JsonValue val = GetAtPath(targetPath); //frame[targetPath];
										JsonValue val2 = op(val, result);
										SetAtPath(targetPath, val2);
										//frame[targetPath] = val2;

										return val2;
									}

								}
							}
						//return frame[node.data["target"]] = Execute(node.nodes["expr"]);

						case ATOM: {
								string cdata = node.Data("const");
								Node inner = node.Child("inner");

								if (cdata != null) {
									double d;
									if (double.TryParse(cdata, out d)) { return d; }
									return cdata;
								} else if (inner != null) {
									return Execute(inner);
								}

								throw new Exception(@"Scripting error: Unhandled atom type. Should have a constant or inner expression. ");
							}

						case VALUE: {
								Node targetPathNode = node.Child("target");
								string targetPath = Execute(targetPathNode).stringVal;
								JsonValue val = GetAtPath(targetPath);

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
								if (node.type == EXPR) { defaultOP = OR_OP; } else if (node.type == BOOLTERM) { defaultOP = AND_OP; } else if (node.type == ARITHEXPR) { defaultOP = ADD_OP; } else if (node.type == ARITHTERM) { defaultOP = MUL_OP; }

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
						case ARITHFACTOR: {
								return -Execute(node.Child("negate"));
							}
						case BOOLFACTOR: {
								JsonValue val = Execute(node.Child("value"));

								string comparison = node.Data("comparison");
								Node rhs = node.Child("rhs");

								if (rhs != null) {
									JsonValue rhsVal = Execute(rhs);
									BinaryOp compOp;
									switch (comparison) {
										case "==": compOp = EQ_OP; break;
										case "!=": compOp = NE_OP; break;
										case ">": compOp = GT_OP; break;
										case ">=": compOp = GE_OP; break;
										case "<": compOp = LT_OP; break;
										case "<=": compOp = LE_OP; break;
										default: compOp = EQ_OP; break;
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
								Node targetPathNode = node.Child("target");
								string target = Execute(targetPathNode).stringVal;
								Node funcCallParams = node.Child("params");
								//Dbg($"Calling Function {target} with {funcCallParams.NodesListed} parameter(s)");

								// Optional.
								//Node indexExpr = node.Child("indexExpr");
								JsonArray prams = new JsonArray();

								for (int i = 0; i < funcCallParams.NodesListed; i++) {
									JsonValue pram = Execute(funcCallParams.Child(i));
									//Dbg($"Executing FUNCCALL {target} parameter {{{i}}} = {{{pram}}}");
									prams.Add(pram);
								}

								JsonFunction func = GetAtPath(target) as JsonFunction;
								if (func == null) {
									object targetObject = TracePath(target);

									string funcName = (target.Contains("."))
										? target.Substring(target.LastIndexOf('.') + 1)
										: target;

									func = DoGet(targetObject, funcName) as JsonFunction;
								}

								// The function will take care of adding a frame if it needs it.
								JsonValue result = func.Invoke(global, prams);
								//Dbg($"Invoking function {target} got {{{result}}}");

								// If we got a return value, reset it to null to stop popping frames.
								returnValue = null;

								return result;
							}

						case IFSTMT: {
								if (Execute(node.Child("cond")).boolVal) {
									return Execute(node.Child("stmt"));
								}

								for (int i = 0; i < node.NodesListed; i += 2) {
									Node cond = node.Child(i);
									Node stmt = node.Child(i + 1);
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
								continueTarget = node.Data("target") ?? "!FixedContinue!";
								break;
							}

						case BREAKSTMT: {
								breakTarget = node.Data("target") ?? "!FixedBreak!";
								break;
							}
						case EACHLOOP: {
								//Dbg($"In Eachloop");
								// Each loops are weird compared to standard loops.
								string label = node.Data("label");

								// They read a collection value 
								Node targetPathNode = node.Child("path");
								// Map each item or pair in the collection to one or more names
								string name = node.Data("name");
								Node nameList = node.Child("names");
								// And then repeatedly call a body with each var being set into the names.
								Node body = node.Child("body");

								// Look up the name if it was not set solo
								if (name == null) { name = nameList.Data(0); }

								string name2 = (nameList != null && nameList.DataListed > 1) ? nameList.Data(1) : null;
								//Look up collection to iterate 
								string targetPath = Execute(targetPathNode).stringVal;
								var target = GetAtPath(targetPath);

								frame.Push();
								frame.Declare(name, null);
								//Dbg($"EACHLOOP: Declared {name} ");
								if (name2 != null) {
									frame.Declare(name2, null);
								}

								JsonValue last = null;
								if (target is JsonArray) {
									foreach (var item in (target as JsonArray)) {
										//Dbg($"EACHLOOP on Array: set {name} to {item} ");
										frame[name] = item;
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

										if (Returning) {
											frame.Pop();
											return returnValue;
										}

									}
								} else if (target is JsonObject) {
									foreach (var pair in (target as JsonObject)) {
										var key = pair.Key;
										var val = pair.Value;
										//Dbg($"EACHLOOP on Object: set {name},{name2} to {key},{val} ");
										frame[name] = key;
										frame[name2] = val;

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

										if (Returning) {
											frame.Pop();
											return returnValue;
										}

									}
								}

								frame.Pop();


								return last;
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
										//Dbg($"Loop Breaking to {breakTarget}");
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
										//Dbg($"Loop continuing to {continueTarget}");
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

									if (Returning) {
										// Pop the context before we are done...
										if (node.type == FORLOOP) { frame.Pop(); }

										return returnValue;
									}

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

		/// <summary> The global object reference. Always available, via 'global.xyz', and shared between all <see cref="ExecutionContext"/>. </summary>
		public JsonObject global;
		
		/// <summary> Class that holds logic to step through an async execution </summary>
		public class Stepper : JsonPromise, IEnumerator<JsonValue> {
			/// <summary> Wrapped enumerator, which is the actual execution. </summary>
			IEnumerator<JsonValue> wrapped;
			/// <summary> ExecutionContext that is being stepped over </summary>
			ExecutionContext context;


			public Stepper(ExecutionContext context, Node body) {
				//Log.Info("New stepper created");
				this.context = context;
				wrapped = context.Async(body);
			}
			private JsonValue last = null;
			public JsonValue Current { get; private set; }

			protected override void TryComplete() {
				//Log.Info($"Stepper TryComplete called with value {Current}");
				if (Current is JsonPromise p && !p.hasValue) { return; }
				while (MoveNext()) {
					//Log.Info($"Stepper TryComplete looping with value {Current}");
					if (Current is JsonPromise p2 && !p2.hasValue) { return; }
				}
			}

			object System.Collections.IEnumerator.Current { get { return Current; } }

			public void Dispose() { }

			public bool MoveNext() {
				if (wrapped.MoveNext()) {
					last = Current;
					Current = wrapped.Current;
					return true;
				}
				//Log.Info($"Stepper Promise Completed with value {Current}");
				try {
					SetValue(last);
				} catch (Exception e) {
					#if UNITY
					Debug.LogError($"XJSInterpreter.Stepper.MoveNext(): Failed to set Promise value to {last} / {Current}" + e.Message.ToString());
					#endif
				}
				context.frames.Pop();
				Current = null;
				return false;
			}

			public void Reset() { throw new InvalidOperationException("Promises are not able to be Reset()!"); }
		}


		/// <summary> Empty object array for preventing memory use from empty function calls </summary>
		private static readonly object[] EMPTY_ARGS = new object[0];

		/// <summary> Binding flags that represent any (public/private) instance method </summary>
		public const BindingFlags ANY_INSTANCE = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		/// <summary> Binding flags that represent any (public/private) static method </summary>
		public const BindingFlags ANY_STATIC = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

		/// <summary> Binding flags that represent only public instance methods </summary>
		public const BindingFlags PUBLIC_INSTANCE = BindingFlags.Public | BindingFlags.Instance;
		/// <summary> Binding flags that represent only public static methods </summary>
		public const BindingFlags PUBLIC_STATIC = BindingFlags.Public | BindingFlags.Static;

		/// <summary> Binding flags that represent any public methods</summary>
		public const BindingFlags ANY_PUBLIC = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

		/// <summary> Try to get a thing from an object </summary>
		/// <param name="obj">object to get from</param>
		/// <param name="key">name of thing to get (field/property/method)</param>
		/// <returns>Thing to get, as a JsonValue</returns>
		public static JsonValue DoGet(object obj, string key) {
			if (obj is Frame) {
				return (obj as Frame)[key];
			}
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
				return result;
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
			//Dbg($"DoSet {{{key}}} to {{{value}}} on {obj.GetType()}");

			if (obj is Frame) {
				//Dbg($"DoSet on Frame {{{key}}} to {{{value}}}");
				(obj as Frame)[key] = value;
				return;
			}
			if (obj is JsonBool || obj is JsonNull || obj is JsonNumber || obj is JsonString) {
				return;
			}
			if (obj is JsonObject) {
				//Dbg($"DoSet on JsonObject {{{key}}} to {{{value}}}");
				(obj as JsonObject)[key] = value;
				return;
			}

			int ind;
			if (obj is JsonArray
				&& int.TryParse(key, out ind)
				&& (ind >= 0 && (ind < (obj as JsonArray).Count))) {
				//Dbg($"DoSet on JsonArray {{{ind}}} to {{{value}}}");
				(obj as JsonArray)[ind] = value;
				return;
			}

			Type type = obj.GetType();
			MethodInfo methodCheck = type.GetMethod(key, PUBLIC_INSTANCE);
			if (methodCheck != null) {
				//Dbg($"Reflective DoSet on {type} via Method");
				// Note: Not sure what to do in this case. Probably want to log a message and not change anything.
			}

			PropertyInfo propertyCheck = type.GetProperty(key, PUBLIC_INSTANCE);
			if (propertyCheck != null) {
				//Dbg($"Reflective DoSet on {type} via Property: {{{key}}} to {{{value}}}");
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
					return (JsonValue)result;

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



	}

	
}
