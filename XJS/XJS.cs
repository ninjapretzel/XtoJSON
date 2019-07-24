using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary> Container class for XJS language </summary>
public static partial class XJS {

	/// <summary> Parses program text into a tree representing the program </summary>
	/// <param name="script"> Text of program to parse </param>
	/// <returns> Node holding entire parsed program tree </returns>
	public static Node Parse(string script) {
		script = XJSHelpers.StripCStyleComments(script);
		Tokenizer tokenizer = new Tokenizer(script);
		return ParseProgram(tokenizer);
	}

	#region Constant type stuff 
	/// <summary> Keywords of the language </summary>
	public static readonly string[] keywords = {
		"var", "as",
		"import", "export",
		"func", "function",

		"return",
		"if", "else",
		"for", "each", "while", "do",
		"break", "continue",
		"in",
		"null", "true", "false",
	};

	/// <summary> Punctuation of the language, with larger constructs checked first. </summary>
	public static readonly string[] punct = {
		// Size 3
		">>>", "===", ">>=", "<<=", "...",

		// Size 2
		"+=", "-=", "/=", "*=", "%=", "^=", "|=", "&=",
		"?.", "??",
		"++", "--",
		"&&", "||",
		"<<", ">>", "<=", ">=", "==", "!=",

		"=>", "->",

		// Size 1
		"+", "-", "/", "*", "%",
		"|", "&", "^", "~",
		"(", ")", "{", "}", "[", "]",
		"<", ">", "=", "!",
		",", ".", "\"", "\'",

		"?", ":", ";",
	};

	/// <summary> Impossible token representing type for names </summary>
	public const string NAME = "!NAME";
	/// <summary> Impossible token representing type for numbers </summary>
	public const string NUMBER = "!NUM";
	/// <summary> Impossible token representing type for strings</summary>
	public const string STRING = "!STR";
	/// <summary> type for names </summary>
	public const string SPACE = " ";
	/// <summary> type for names </summary>
	public const string NEWLINE = "\n";
	/// <summary> type for names </summary>
	public const string TAB = "\t";
	// <summary> Impossible token representing type for names </summary>
	//public const string NAME = "!NAME";

	/// <summary> Constant name of argNames array </summary>
	public const string ARGNAMES = "argNames";
	/// <summary> Constant name of args array </summary>
	public const string ARGS = "args";

	/// <summary> Regex pattern for matching names </summary>
	public const string nameRegex = @"[a-zA-Z_\$][a-zA-Z0-9_\$]*";
	/// <summary> Regex checker for names </summary>
	public static readonly Regex name = new Regex(nameRegex);

	/// <summary> Regex pattern for matching numbers </summary>
	public const string numRegex = @"(0x[0-9A-Fa-f]+[lL]?)|(\d+\.\d*[fF]?)|(\d*\.\d+[fF]?)(0x[0-9A-Fa-f]+[lL]?)|(\d+[lL]?)|(\d+\.\d*[fF]?)|(\d*\.\d+[fF]?)";
	/// <summary> Regex checker for numbers </summary>
	public static readonly Regex num = new Regex(numRegex);

	#endregion
	

	#region Node Types
	/// <summary> Enum containing types for nodes. </summary>
	public enum Nodes {
		/// <summary> No type has been assigned. </summary>
		UNTYPED = -1,
		/// <summary> Root Program Node </summary>
		PROGRAM = 0,
		/// <summary> Container for Imports and Exports lists </summary>
		IMPORT_EXPORT, 
		/// <summary> A list of Imports </summary>
		IMPORTS,
		/// <summary> A single import, applying a rename to the imported item. </summary>
		IMPORT_RENAME,
		/// <summary> A List of Exports </summary>
		EXPORTS,
		/// <summary> Statements under the Root Program Node </summary>
		STMTLIST, 
		/// <summary> Statements inside arbitrary blocks of code </summary>
		CODEBLOCK,
		/// <summary> A for Loop </summary>
		FORLOOP,
		/// <summary> An each Loop </summary>
		EACHLOOP,
		/// <summary> A do...while Loop</summary>
		DOWHILELOOP,
		/// <summary> A while Loop </summary>
		WHILELOOP,
		/// <summary> A break Statement </summary>
		BREAKSTMT,
		/// <summary> A continue Statment </summary>
		CONTINUESTMT,
		/// <summary> A return Statement </summary>
		RETURNSTMT,
		/// <summary> An if...else Statement </summary>
		IFSTMT,
		/// <summary> A Declaration Statement </summary>
		DECSTMT,
		/// <summary> An Expression </summary>
		EXPR,
		/// <summary> A Boolean Term</summary>
		BOOLTERM,
		/// <summary> A Boolean Factor </summary>
		BOOLFACTOR,
		/// <summary> An Arithmatic Expression </summary>
		ARITHEXPR,
		/// <summary> An Arithmatic Term </summary>
		ARITHTERM,
		/// <summary> An Arithmatic Factor </summary>
		ARITHFACTOR,
		/// <summary> A variable, constant, function call, or literal value </summary>
		ATOM,
		/// <summary> An assignment to a value </summary>
		ASSIGN,
		/// <summary> A variable's value </summary>
		VALUE,
		/// <summary> A call to a function </summary>
		FUNCCALL,
		/// <summary> A list of parameters passed to a function </summary>
		PARAMSLIST,
		/// <summary> An Object Literal </summary>
		OBJECTLITERAL,
		/// <summary> An Array Literal </summary>
		ARRAYLITERAL,
		/// <summary> A function Declaration </summary>
		FUNCDEC,
		/// <summary> A list of names, to add to the context of a function </summary>
		VARLIST,
		/// <summary> Expression that resolves to a path </summary>
		PATHEXPR,
		/// <summary> Placeholder to spread an iterable </summary>
		SPREAD,

	}



	#endregion



}
