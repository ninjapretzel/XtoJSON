using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Lib;

public partial class XJS {
	
	// @TBD TODO
	// Refactor this class to use separate lists and dictionaries for speedup. 
	// There should be no collections in empty nodes.

	/// <summary> Class used to build program trees from </summary>
	public class Node {

		/// <summary> Unordered Map of data within the node </summary>
		public Dictionary<string, string> dataMap;
		/// <summary> Unordered Map of children of the node </summary>
		public Dictionary<string, Node> nodeMap;

		/// <summary> Ordered List of children </summary>
		public List<Node> nodeList;
		/// <summary> Ordered list of data </summary>
		public List<string> dataList;

		/// <summary> Tokens that compose this node for sourcemapping information. </summary>
		public List<Token> tokens;

		/// <summary> Number of entries in the ordered data 'list' </summary>
		public int DataListed { get { return (dataList == null) ? 0 : dataList.Count; } }
		
		/// <summary> Number of entries in the ordered children 'list' </summary>
		public int NodesListed { get { return (nodeList == null) ? 0 : nodeList.Count; } }
		
		/// <summary> Number of data values mapped </summary>
		public int DataMapped { get { return (dataMap == null) ? 0 : dataMap.Count; } }

		/// <summary> Number of child nodes mapped </summary>
		public int NodesMapped { get { return (nodeMap == null) ? 0 : nodeMap.Count; } }

		/// <summary> Gets/sets the <see cref="Nodes"/> id for this node. </summary>
		public Nodes type { get; set; }

		/// <summary> Get first line this node is on, or -1 if no tokens are recorded. </summary>
		public int line { get { return tokens != null ? tokens[0].line : -1; } }
		/// <summary> Get column of first line this node is on, or -1 if no tokens are recorded. </summary>
		public int col { get { return tokens != null ? tokens[0].col : -1; } }

		/// <summary> Gets the last line this node is on, or -1 if no tokens are recorded. </summary>
		public int lastLine {
			get {
				int max = -1; 
				if (tokens != null) {
					foreach (var token in tokens) { if (token.line > max) { max = token.line; } }
				}
				return max;
			}
		}
		/// <summary> Gets the last column on the last line this node is on in the source code. </summary>
		public int lastCol {
			get {
				int maxLine = -1;
				int maxCol = -1;

				if (tokens != null) {
					for (int i = 0; i < tokens.Count; i++) { 
						var token = tokens[i];
						if (token.line > maxLine) {
							maxLine = token.line;
							maxCol = token.col;
						} else if (token.line == maxLine) {
							if (token.col > maxCol) { maxCol = token.col; }
						}

					}
				}
				return col;
			}
		}




		/// <summary> Constructor </summary>
		public Node() {
			dataMap = null;
			nodeMap = null;
			nodeList = null;
			dataList = null;

			type = Nodes.UNTYPED;
		}

		/// <summary> Constructor which takes a type parameter. </summary>
		/// <param name="type"> Type value for the node. </param>
		public Node(Nodes type) {
			nodeMap = null;
			dataMap = null;
			nodeList = null;
			dataList = null;

			this.type = type;
		}

		///<summary> Adds the given <paramref name="token"/> to the node's tokens list. </summary>
		public void Add(Token token) {
			if (tokens == null) { tokens = new List<Token>(); }
			tokens.Add(token);
		}

		/// <summary> Maps the given <paramref name="node"/> by <paramref name="name"/> and returns the mapped node </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Node Map(string name, Node node) {
			if (nodeMap == null) { nodeMap = new Dictionary<string, Node>(); }
			if (node != null) { nodeMap[name] = node; } 
			return node; 
		}

		/// <summary> Inserts the <paramref name="node"/> into the 'list' at index <see cref="NodesListed"/> and returns the listed node </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Node List(Node node) {
			if (nodeList == null) { nodeList = new List<Node>(); }
			if (node != null) { nodeList.Add(node); }  
			return node; 
		}

		/// <summary> Maps the given <paramref name="val"/> into data by <paramref name="name"/> </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Map(string name, string val) { 
			if (dataMap == null) { dataMap = new Dictionary<string, string>(); }
			if (val != null) { dataMap[name] = val; } 
		}

		/// <summary> Maps the given <paramref name="val"/>'s content into data by <paramref name="name"/> </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Map(string name, Token val) { 
			if (tokens == null) { tokens = new List<Token>(); }
			tokens.Add(val);
			Map(name, val.content); 
		}

		/// <summary> Adds the given <paramref name="val"/> into the 'list' of data at index <see cref="DataListed"/> </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void List(string val) {
			if (dataList == null) { dataList = new List<string>(); }
			if (val != null) { dataList.Add(val); }
			//if (val != null) { dataMap[""+(dataListSize++)] = val; }
		}

		/// <summary> Adds the given <paramref name="val"/>'s content into the 'list' of data at index <see cref="DataListed"/> </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void List(Token val) { 
			if (tokens == null) { tokens = new List<Token>(); }
			tokens.Add(val);
			List(val.content); 
		}
		
		/// <summary> Returns a child by index. </summary>
		/// <param name="index"> Index of child node to grab </param>
		/// <returns> Child node at <paramref name="index"/>, or null if there is none. </returns>
		public Node Child(int index) {
			if (nodeList == null) { return null; }
			if (index < nodeList.Count) { return nodeList[index]; }
			return null;
		}

		/// <summary> Returns a child by name. </summary>
		/// <param name="name"> Name of child to grab </param>
		/// <returns> Child node mapped to <paramref name="name"/>, or null if there is none. </returns>
		public Node Child(string name) {
			if (nodeMap == null) { return null; }
			if (nodeMap.ContainsKey(name)) { return nodeMap[name]; }
			return null;
		}

		/// <summary> Returns a data value by index. </summary>
		/// <param name="index"> Index of data value to grab </param>
		/// <returns> Data value at <paramref name="index"/>, or null if there is none. </returns>
		public string Data(int index) {
			if (dataList == null) { return null; }
			if (index < dataList.Count) { return dataList[index]; }
			return null;
		}

		/// <summary> Returns a data value by name. </summary>
		/// <param name="name"> Name of data value to grab </param>
		/// <returns> Data value mapped to <paramref name="name"/>, or null if there is none. </returns>
		public string Data(string name) {
			if (dataMap == null) { return null; }
			if (dataMap.ContainsKey(name)) { return dataMap[name]; }
			return null;
		}

		/// <inheritdoc />
		public override string ToString() { return ToString(0); }

		/// <summary> Build s a string representation of this node, with a given <paramref name="indent"/> level. </summary>
		/// <param name="indent"> Number of levels to indent </param>
		/// <param name="indentString"> Characters to indent each level with, default is "  "</param>
		/// <returns> String of the current node and its children, indented at the given <paramref name="indent"/> level. </returns>
		public string ToString(int indent, string indentString = "  ") {
			StringBuilder str = new StringBuilder();
			string ident = "";
			for (int i = 0; i < indent; i++) { ident += indentString; }
			string ident2 = ident + indentString;
			string ident3 = ident2 + indentString;

			str.Append("\n");
			str.Append(ident);
			str.Append("Node ");
			str.Append(type);
			str.Append(" From [Line ");
			str.Append(line);
			str.Append(", Col ");
			str.Append(col);
			str.Append("] - [Line ");
			str.Append(lastLine);
			str.Append(", Col ");
			str.Append(lastCol);
			str.Append("]");

			if (dataMap != null) {
				str.Append("\n");
				str.Append(ident2);
				str.Append("DataMap:");
			
				foreach (var pair in dataMap) {
					str.Append("\n");
					str.Append(ident3);
					str.Append(pair.Key);
					str.Append(": ");
					str.Append(pair.Value);
				}

			}

			if (dataList != null) {
				str.Append("\n");
				str.Append(ident2);
				str.Append("DataList: [");
				
				for (int i = 0; i < dataList.Count; i++) {
					str.Append(i > 0 ? ", " : "");
					str.Append(dataList[i]);
				}

				str.Append("]");
			}

			if (nodeMap != null) {
				str.Append("\n");
				str.Append(ident2);
				str.Append("NodeMap:");

				foreach (var pair in nodeMap) {
					str.Append("\n");
					str.Append(ident3);
					str.Append(pair.Key);
					str.Append(": ");
					str.Append(pair.Value.ToString(indent+1, indentString));
				}

			}
			
			if (nodeList != null) {
				str.Append("\n");
				str.Append(ident2);
				str.Append("NodeList:");

				for (int i = 0; i < nodeList.Count; i++) {
					str.Append("\n");
					str.Append(ident3);
					str.Append(i);
					str.Append(": ");
					str.Append(nodeList[i].ToString(indent+1, indentString));
				}
			}

			return str.ToString();
		}
	}
	
}
