using Lib;
using static XJS.Nodes;

public partial class XJS {

	
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	/// <summary> 
	///		<para> Program Nodes hold: </para>
	///		<para> import-export? - List of imports and exports </para>
	///		<para> stmts? - List of statements to execute for the program.</para>
	/// </summary>
	public static Node ParseProgram(this Tokenizer tok) {
		Node prog = new Node(PROGRAM);

		prog.Map("imports-exports", tok.ParseImportsExports());
		prog.Map("stmts", tok.ParseStatementList());

		return prog;
	}

	/// <summary> Constant Array for import/export keywords </summary>
	public static readonly string[] IMP_EXP = { "import", "export" };

	/// <summary> Parses import/export lists at the begining of the program. </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseImportsExports(this Tokenizer tok) {
		Node impexp = new Node(IMPORT_EXPORT);
		Node imports = impexp.Map("imports", new Node(IMPORTS));
		Node exports = impexp.Map("exports", new Node(EXPORTS)); 

		while (tok.At(IMP_EXP)) {
			if (tok.At("import")) {
				imports.Add(tok.peekToken);
				tok.Next();
				
				Token importPath = tok.ParseFixedPath();
				Node import = new Node(IMPORT_RENAME);
				
				//tok.Next();

				if (tok.At("as")) {
					tok.Next();

					import.Map("thing", importPath);

					import.Map("as", tok.peekToken);

					tok.Next();

					imports.List(import);
				} else {
					imports.List(importPath);
				}

			} else if (tok.At("export")) { // Redundant, but more clear.
				tok.Next();
				exports.List(tok.ParseFixedPath());
			}

			if (tok.At(";")) { tok.Next(); } // Consume, but don't require ';'
		}

		return impexp;
	}

	/// <summary> Parses a 'path' which is a series of names separated by '.' </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Token ParseFixedPath(this Tokenizer tok) {

		tok.Require(NAME);
		StringBuilder path = new StringBuilder(tok.content);
		Token start = tok.Next();

		while (tok.At(".")) {
			path.Append('.');
			tok.Next();

			tok.Require(NAME);
			path.Append(tok.content);
			tok.Next();
		}


		return new Token(path.ToString(), "!PATH", start.line, start.col);
	}

	public static Node ParseExprPath(this Tokenizer tok) {
		// All paths start with a name. 
		tok.Require(NAME);

		Node path = new Node(PATHEXPR);
		Node first = new Node(ATOM);
		first.Map("const", tok.peekToken);

		path.List(first);
		tok.Next();
		
		while (tok.At(".") || tok.At("[")) {
			if (tok.At(".")) {
				tok.Next();
				Node next = new Node(ATOM);
				tok.Require(NAME);
				next.Map("const", tok.peekToken);
				path.List(next);
				tok.Next();
			} else if (tok.At("[")) {
				tok.Next();
				Node next = tok.ParseExpression();
				tok.RequireNext("]");
				path.List(next);
			}
		}

		return path;
	}
	
	/// <summary> Parses a label string. This is prefixed/suffixed with ':' </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Label string that is at the front of the tokenizer. </returns>
	public static string ParseLabel(this Tokenizer tok) {
		tok.RequireNext(":");

		tok.Require(NAME);
		string label = tok.peekToken.content;
		tok.Next();

		tok.RequireNext(":");
		return label;
	}

	public const int SAFETYWALL = 1000000;
	/// <summary> Parses a statement list. </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseStatementList(this Tokenizer tok) {
		Node stmts = new Node(STMTLIST);
		
		int safetyWall = 0;
		while (!(tok.Done || tok.At("}"))) {
			stmts.List(tok.ParseStatement());
			if (++safetyWall > SAFETYWALL) { Debug.LogWarning("Broke out of STMTLIST debug safetywall!\n" + tok); break; }
		}

		return stmts;
	}
	
	/// <summary> Labelable statements (loops) </summary>
	public static readonly string[] LABEL_ABLE_LOOP_TOKEN = { "for", "each", "do", "while", };
	/// <summary> Parses a single Statement node. </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseStatement(this Tokenizer tok) {
		if (tok.At(";")) { tok.Next(); return null; }
		else if (tok.At("if")) { return tok.ParseIfStmt(); }
		else if (tok.At("var")) { return tok.ParseDecStmt(); }
		else if (tok.At("{")) { return tok.ParseCodeBlock(); }
		else if (tok.At("return")) { return tok.ParseReturnStmt(); }
		else if (tok.At("for")) { return tok.ParseForLoop(); }
		else if (tok.At("each")) { return tok.ParseEachLoop(); }
		else if (tok.At("do")) { return tok.ParseDoWhileLoop(); }
		else if (tok.At("while")) { return tok.ParseWhileLoop(); }
		else if (tok.At("break")) { return tok.ParseBreakStmt(); }
		else if (tok.At("continue")) { return tok.ParseContinueStmt(); }
		else if (tok.At(":")) {
			
			string label = tok.ParseLabel();

			tok.Require(LABEL_ABLE_LOOP_TOKEN);

			Node loop = null;

			if (tok.At("for")) { loop = tok.ParseForLoop(); }
			else if (tok.At("each")) { loop = tok.ParseEachLoop(); }
			else if (tok.At("do")) { loop = tok.ParseDoWhileLoop(); }
			else if (tok.At("while")) { loop = tok.ParseWhileLoop(); }

			loop?.Map("label", label);

			return loop;
		}
		else if (tok.At(STRING) || tok.At(NUMBER) || tok.At(NAME) 
			|| tok.At("(") || tok.At("-") || tok.At("[")
			|| tok.At("++") | tok.At("--")) {
			return tok.ParseExpression();
		}

		return null;
	}

	/// <summary>
	///		<para> Parses a CodeBlock node </para>
	///		<para> CodeBlock node holds: </para>
	///		<para> [nodelist]? - List of statements to execute, in order. </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseCodeBlock(this Tokenizer tok) {
		Node codeblock = new Node(CODEBLOCK);

		tok.RequireNext("{");

		int safetyWall = 0;
		while (!tok.At("}")) {
			codeblock.List(tok.ParseStatement());

			if (safetyWall++ > SAFETYWALL) { Debug.LogWarning("Broke out of CODEBLOCK safetywall!\n" + tok); break;  }
		}

		tok.RequireNext("}");

		return codeblock;
	}

	/// <summary>
	///		<para> Parses a ForLoop node. </para>
	///		<para> The ForLoop node contains: </para>
	///		<para> init? - Initializer. If not present, nothing happens. </para>
	///		<para> cond? - Loop condition. If not present, is always true. </para>
	///		<para> incr? - Loop increment. If not present, nothing happens. </para>
	///		<para> body - Statements to run. </para>
	///		<para> label? - Label of the loop. Actually attached by <see cref="ParseStatement(Tokenizer)"/> </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseForLoop(this Tokenizer tok) {
		Node forloop = new Node(FORLOOP);
		tok.RequireNext("for");
		tok.RequireNext("(");
		
		if (!tok.At(";")) {
			Node assign;
			if (tok.At("var")) { assign = tok.ParseDecStmt(); }
			else { 
				assign = tok.ParseFromName(); 
				if (assign.type != ASSIGN) { tok.Error("For loop initializer can only have a declaration or assignment statement!"); }
			}
			forloop.Map("init", assign);
		} 

		tok.RequireNext(";");

		if (!tok.At(";")) { forloop.Map("cond", tok.ParseExpression()); }

		tok.RequireNext(";");

		if (!tok.At(")")) {
			Node assign = tok.ParseFromName();
			if (assign.type != ASSIGN) { tok.Error("For loop increment can only have an assignment statement!"); }
			forloop.Map("incr", assign);
		}

		tok.RequireNext(")");

		forloop.Map("body", tok.ParseStatement());

		return forloop;
	}

	/// <summary>
	///		<para> Parses an EachLoop node. </para>
	///		<para> The EachLoop node contains: </para>
	///		<para> name - name of variable inside of loop body. </para>
	///		<para> path - path of collection to loop over. </para>
	///		<para> body - Statements to run </para>
	///		<para> label? - Label of the loop. Actually attached by <see cref="ParseStatement(Tokenizer)"/> </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseEachLoop(this Tokenizer tok) {
		Node eachloop = new Node(EACHLOOP);
		tok.RequireNext("each");
		tok.RequireNext("(");

		if (tok.At(NAME)) {
			eachloop.Map("name", tok.peekToken);
			tok.Next();
		} else {
			tok.Require("(");
			eachloop.Map("names", tok.ParseVarList());
		}

		tok.RequireNext("in");
		
		// Todo: Change to ParseExprPath
		eachloop.Map("path", tok.ParseExprPath());
		tok.RequireNext(")");

		eachloop.Map("body", tok.ParseStatement());

		return eachloop;
	}

	/// <summary>
	///		<para> Parses a DoWhileLoop node. </para>
	///		<para> Always runs its body at least once, and then repeats it so long as its condition is true. </para>
	///		<para> The DoWhileLoop node contains: </para>
	///		<para> cond - loop condition </para>
	///		<para> body - Statements to run </para>
	///		<para> label? - Label of the loop. Actually attached by <see cref="ParseStatement(Tokenizer)"/> </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseDoWhileLoop(this Tokenizer tok) {
		Node dowhileloop = new Node(DOWHILELOOP);
		tok.RequireNext("do");
		
		dowhileloop.Map("body", tok.ParseStatement());

		tok.RequireNext("while");
		tok.RequireNext("(");

		dowhileloop.Map("cond", tok.ParseExpression());

		tok.RequireNext(")");

		return dowhileloop;
	}

	/// <summary> 
	///		<para> Parses a WhileLoop node. </para>
	///		<para> Only enters its body if its condition is true when reached. </para>
	///		<para> The DoWhileLoop node contains: </para>
	///		<para> cond - loop condition </para>
	///		<para> body - Statements to run </para>
	///		<para> label? - Label of the loop. Actually attached by <see cref="ParseStatement(Tokenizer)"/> </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseWhileLoop(this Tokenizer tok) {
		Node whileloop = new Node(WHILELOOP);
		tok.RequireNext("while");
		tok.RequireNext("(");

		whileloop.Map("cond", tok.ParseExpression());

		tok.RequireNext(")");
		
		whileloop.Map("body", tok.ParseStatement());

		return whileloop;
	}

	/// <summary> 
	///		<para> Parses a BreakStatement node. </para>
	///		<para> The BreakStatement node may contain: </para>
	///		<para> target? - the target label for the loop that the break statement will break from </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseBreakStmt(this Tokenizer tok) {
		Node breakstmt = new Node(BREAKSTMT);
		tok.RequireNext("break");

		if (tok.At(":")) { breakstmt.Map("target", tok.ParseLabel()); }

		return breakstmt;
	}

	/// <summary> 
	///		<para> Parses a ContinueStatement node. </para>
	///		<para> The ContinueStatement node may contain: </para>
	///		<para> target? - the target label for the loop that the continue statement will repeat </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseContinueStmt(this Tokenizer tok) {
		Node continuestmt = new Node(CONTINUESTMT);
		tok.RequireNext("continue");

		if (tok.At(":")) { continuestmt.Map("target", tok.ParseLabel()); }

		return continuestmt;
	}
	
	/// <summary>
	///		<para> Parses a ReturnStatement node. </para>
	///		<para> The ReturnStatement node may contain: </para>
	///		<para> expr? - the expression of the value to return </para>
	/// </summary>
	/// <param name="tok"></param>
	/// <returns></returns>
	public static Node ParseReturnStmt(this Tokenizer tok) {
		Node returnstmt = new Node(RETURNSTMT);
		tok.RequireNext("return");
		
		if (!tok.At(";")) {
			returnstmt.Map("expr", tok.ParseExpression());
		} else {
			tok.Next(); // Consume ;. 
		}
		
		return returnstmt;
	}

	/// <summary>
	///		<para> Parses an IfStatment node. </para>
	///		<para>If Statement Node holds:</para>
	///		<para>cond - condition expression to evaluate</para>
	///		<para>stmt - statement to execute if cond evaluates true</para>
	///		<para>[nodelist]? - list of condition Expression (even) and Statement (odd) 
	///							pairs to check in turn if cond evaluates false</para>
	///		<para>else? - statement to execute if all condition expressions evaluate false. </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseIfStmt(this Tokenizer tok) {
		Node ifStmt = new Node(IFSTMT);

		tok.RequireNext("if");
		tok.RequireNext("(");

		ifStmt.Map("cond", tok.ParseExpression() );
		tok.RequireNext(")");

		ifStmt.Map("stmt", tok.ParseStatement() );

		while (tok.At("else")) {
			tok.Next();
			if (tok.At("if")) {
				tok.Next();
				// Contains if...else if... else if... chain
				// Even elements contain condition expressions in order
				ifStmt.List(tok.ParseExpression() );
				// Odd elements contain statements to run if the paired expression is true
				ifStmt.List(tok.ParseStatement() );

			} else {

				// Else block if no others run
				ifStmt.Map("else", tok.ParseStatement() );
			}
		}
		
		return ifStmt;
	}

	/// <summary>
	///		<para> Parses a variable declaration. </para>
	///		<para>Dec statements are distinct in that they do not escape their block, and override anything with the same name. </para>
	///		<para>They may contain: 
	///			<para> target - the name of the variable that is being stored. </para>
	///			<para> assign? - the kind of assignment to do. (default values are treated as null or 0) </para>
	///			<para> expr? - The expression to assign, to the target </para>
	///		</para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseDecStmt(this Tokenizer tok) {
		Node decStmt = new Node(DECSTMT);

		tok.RequireNext("var");
		decStmt.Map("target", tok.content);
		tok.Next();

		if (tok.At(ASSIGN_TOKEN)) {
			decStmt.Map("assign", tok.content);
			tok.Next();
			decStmt.Map("expr", tok.ParseExpression());
		}

		return decStmt;
	}

	/// <summary>
	///		<para> Parses a full expression </para>
	///		<para> Expressions start with a 'boolean expression' even if they aren't just a true or false value. </para>
	///		<para> Every value has a 'truthyness' to it. </para>
	///		<para> BooleanExpressions are one or more BooleanTerms linked by '||' </para>
	///		<para> They Contain: 
	///			<para> value - the starting value on the left hand side </para>
	///			<para> [nodelist]? - all linked expressions to apply via '||' </para>
	///		</para>
	///		<para> Will instead just return the value if there are no linked BooleanTerms </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseExpression(this Tokenizer tok) {
		Node boolexpr = new Node(EXPR);
		Node val = boolexpr.Map("value", tok.ParseBoolTerm());

		// List out each bool term. All are || 
		while (tok.At("||")) {
			tok.Next();
			boolexpr.List(tok.ParseBoolTerm());
			//boolexpr.Map("rhs", tok.ParseBoolTerm());
		}

		return (boolexpr.NodesListed == 0) ? val : boolexpr;
	}

	/// <summary>
	///		<para> Parses a BooleanTerm </para>
	///		<para> BooleanTerms are one or more BooleanFactors linked by '&&' </para>
	///		<para> They Contain: 
	///			<para> value - the starting value on the left hand side </para>
	///			<para> [nodelist]? - all linked expressions to apply via '&&' </para>
	///		</para>
	///		<para> Will instead just return the value if there are no linked BooleanFactors </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseBoolTerm(this Tokenizer tok) {
		Node boolterm = new Node(BOOLTERM);
		Node val = boolterm.Map("value", tok.ParseBoolFactor());

		// List out each bool factor. All are && 
		while (tok.At("&&")) {
			tok.Next();
			boolterm.List(tok.ParseBoolFactor());
			//boolterm.Map("rhs", tok.ParseBoolFactor());
		}

		return (boolterm.NodesListed == 0) ? val : boolterm;
	}

	/// <summary> Constant array of comparison tokens to detect boolean comparisons </summary>
	public static readonly string[] COMPARISON_TOKEN = { "<", ">", "!=", "==", "<=", ">=" };

	/// <summary>
	///		<para> Parses a BooleanFactor </para>
	///		<para> They contain: 
	///			<para> value - leftmost ArithExpr value </para>
	///			<para> negate? - present/true if there was a '!' in front of the factor. </para>
	///			<para> comparison? - present, and one of the tokens in </para>
	///		</para>
	///		<para> Will instead just return the value if there is no comparison or negation </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseBoolFactor(this Tokenizer tok) {
		Node boolfact = new Node(BOOLFACTOR);

		if (tok.At("!")) {

			boolfact.Map("negate", "true");
			tok.Next();
			boolfact.Map("value", tok.ParseExpression());

		} else {

			Node value = boolfact.Map("value", tok.ParseArithExpr());
			
			if (tok.At(COMPARISON_TOKEN)) {
				boolfact.Map("comparison", tok.peekToken);
				tok.Next();
				boolfact.Map("rhs", tok.ParseArithExpr());
			} else {
				return value;
			}

		}
		
		return boolfact;
	}

	/// <summary> Constant array of add/subtract operators </summary>
	public static readonly string[] ADD_SUB_TOKEN = { "+", "-" };

	/// <summary>
	///		<para> Parses an ArithmaticExpression </para>
	///		<para> ArithmaticExpressions are one or more ArithmaticTerms linked by '+' or '-' operators. </para>
	///		<para> They contain: 
	///			<para> value - leftmost value to start applying operators to </para>
	///			<para> [datalist]? - operators separating value and other ArithmaticTerms </para>
	///			<para> [nodelist]? - other linked ArithmaticTerms </para>
	///		</para>
	///		<para> Will instead just return the value if there are no operators. </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseArithExpr(this Tokenizer tok) {
		Node arithexpr = new Node(ARITHEXPR);

		Node val = arithexpr.Map("value", tok.ParseArithTerm());

		while (tok.At(ADD_SUB_TOKEN)) {
			arithexpr.List(tok.peekToken);
			tok.Next();
			arithexpr.List(tok.ParseArithTerm());
		}

		// If there are no chained operators, we just care about the value
		return (arithexpr.NodesListed == 0) ? val : arithexpr;
	}

	/// <summary> COnstant array of multiplication/division/modulo division operators </summary>
	public static readonly string[] MUL_DIV_MOD_TOKEN = { "*", "/", "%" };

	/// <summary>
	///		<para> Parses an ArithmaticTerm </para>
	///		<para> ArithmaticTerms are one or more ArithmaticFactors linked by '*', '/' or '%' operators. </para>
	///		<para> They contain:
	///			<para> value - leftmost value to start applying operators to </para>
	///			<para> [datalist]? - operators separating value and other ArithmaticFactors </para>
	///			<para> [nodelist]? - other linked ArithmaticFactors </para>
	///		</para>
	///		<para> Will instead just return the value if there are no operators. </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseArithTerm(this Tokenizer tok) {
		Node arithterm = new Node(ARITHTERM);

		Node val = arithterm.Map("value", tok.ParseArithFactor());

		while (tok.At(MUL_DIV_MOD_TOKEN)) {
			arithterm.List(tok.peekToken);
			tok.Next();
			arithterm.List(tok.ParseArithFactor());
		}
		
		// If there are no chained operators, we just care about the value 
		return (arithterm.NodesListed == 0) ? val : arithterm;
	}

	/// <summary>
	///		<para> Parses an ArithmaticFactor </para>
	///		<para> ArithmaticFactors are a negated ArithmaticFactor, an embedded Expression, or an Atom. </para>
	///		<para> They may contain: 
	///			<para> negate - ArithmaticFactor to negate </para>
	///		</para>
	///		<para> If there is no negation applied, returns the embedded node returned by 
	///			<see cref="ParseAtom(Tokenizer)"/> or
	///			<see cref="ParseExpression(Tokenizer)"/>
	///		</para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseArithFactor(this Tokenizer tok) {
		Node arithfact = new Node(ARITHFACTOR);
		Node node = null;
		
		if (tok.At("-")) {
			tok.Next();
			arithfact.Map("negate", tok.ParseArithFactor());
		} else if (tok.At("(")) {
			tok.Next();

			node = tok.ParseExpression();

			tok.RequireNext(")");
			
		} else {
			node = tok.ParseAtom();
		}
		
		// Only time we care about the arithfactor itself,
		// is if it is present to negate the value of its child.
		return (node == null) ? arithfact : node;
	}

	/// <summary> Constant array of tokens that represent a constant value </summary>
	public static readonly string[] CONST_TOKEN = { "null", NUMBER, STRING, "true", "false", };
	/// <summary> Constant array of tokens that represent different kinds of assignments </summary>
	public static readonly string[] ASSIGN_TOKEN = { "=", "+=", "-=", "*=", "/=", "%=", "^=", "|=", "&=", "<<=", ">>=", };
	/// <summary> Constant array of tokens that represent an INCrement or DECrement operation on a single variable </summary>
	public static readonly string[] INC_DEC_TOKEN = { "++", "--" };
	
	/// <summary>
	///		<para> Parses an Atom node </para>
	///		<para> An Atom node may be a: 
	///			<para> object - In-line Object </para>
	///			<para> array - In-line Array</para>
	///			<para> func - Function definition </para>
	///			<para> value - Variable value from <see cref="ParseFromName(Tokenizer)"/></para>
	///		</para>
	///		<para> Or be a node containing: 
	///			<para> const? - Constant values (number/string values, true/false/null) </para>
	///			<para> inner? - Assignment with value generation, or function call from <see cref="ParseFromName(Tokenizer)"/></para>
	///		</para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseAtom(this Tokenizer tok) {
		Node atom = new Node(ATOM);

		if (tok.At(CONST_TOKEN)) {
			string content = tok.peekToken.content;
			// Strip out ""s or ''s around strings
			if (tok.peekToken.Is(STRING)) { content = content.Substring(1, content.Length-2); }
			atom.Map("const", content);
			tok.Next();
		} else if (tok.At("{")) {

			return tok.ParseObject();
		} else if (tok.At("[")) {

			return tok.ParseArray();
		} else if (tok.At("func")) {

			return atom.Map("func", tok.ParseFunc());
		} else {
			Node check = atom.Map("inner", tok.ParseFromName());
			if (check.type == VALUE) { return check; }
		}

		return atom;
	}

	/// <summary>
	///		<para> Parses a value, assignment statement, or a function call </para>
	///		<para> 
	///			Due to the way the grammar rules work, it is difficult to determine if 
	///			a PATH is the start of a value, assignment or a function call.
	///			We don't know what it is until later on, after we get the PATH.
	///		</para>
	///		<para>
	///			If the next token is an ASSIGN_TYPE token, it is an assignment 
	///			Otherwise, if it is an '(', it is a function call.
	///		</para>
	///		<para>
	///			This also parses out an indexer.
	///			Functions can be stored inside of indexable collections, and called, ex funcs["doThing"]()
	///		</para>
	///		<para>
	///			The output of this function can be one of many things:
	///			<para> funcCall - contains target, indexExpr?, plist </para>
	///			<para> assign - contains target, indexExpr?, assignType, expr, pre?, post? </para>
	///			<para> value - contains target, indexExpr? </para>
	///		</para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseFromName(this Tokenizer tok) {
		Node node = new Node(VALUE);

		if (tok.At(INC_DEC_TOKEN)) {
			node.Map("pre", tok.content);
			tok.Next();
			node.type = ASSIGN;
		}
		// Token path = tok.ParseFixedPath();
		Node path = tok.ParseExprPath();
		Node funcCallParams = null;

		// Included in ParseExprPath
		// if (tok.At("[")) { indexExpr = tok.ParseIndexer(); }

		if (tok.At("(")) {
			// Detect function calls
			funcCallParams = tok.ParseParamsList();
			node.type = FUNCCALL;
		} else if (tok.At(ASSIGN_TOKEN)) {
			node.Map("assignType", tok.peekToken);
			tok.Next();
			node.Map("expr", tok.ParseExpression());
			node.type = ASSIGN;
		} else if (tok.At(INC_DEC_TOKEN)) {
			node.Map("post", tok.content);
			tok.Next();
			node.type = ASSIGN;
		}
		
		// Anything that is null does not get mapped.
		node.Map("target", path);
		node.Map("params", funcCallParams);

		return node;
	}

	/// <summary> Parses an Indexer. They have the form <code>[ EXPR ]</code> </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseIndexer(this Tokenizer tok) {
		tok.RequireNext("[");
		Node indexExpr = tok.ParseExpression();
		
		tok.RequireNext("]");
		return indexExpr;
	}

	/// <summary> 
	///		<para> Parses a live parameters list </para> 
	///		<para> Parameters lists contain:
	///			<para> [nodelist]? - List of Expression nodes for the parameters list </para>
	///		</para>
	///		<para> The list will just be empty if there are no parameters. </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseParamsList(this Tokenizer tok) {
		Node funcCallParams = new Node(PARAMSLIST);

		// Start with '('
		tok.RequireNext("(");
		
		while (!tok.At(")")) {
			funcCallParams.List(tok.ParseExpression());

			// Consume separator ','
			if (tok.At(",")) { tok.Next(); }
		}

		// End with ')'
		tok.RequireNext(")");

		return funcCallParams;
	}

	/// <summary> 
	///		<para> Parses an object literal. </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseObject(this Tokenizer tok) {
		Node obj = new Node(OBJECTLITERAL);
		tok.RequireNext("{");

		while (!tok.At("}")) {

			if (tok.At("...")) {

				tok.Next();
				Node spread = new Node(SPREAD);
				spread.Map("target", tok.ParseFromName());
				obj.List(spread);
				
			} else {

				tok.Require(NAME);
				var name = tok.peekToken;
				tok.Next();
			
				if (tok.At(":")) {
					tok.RequireNext(":");

					var expr = tok.ParseExpression();

					obj.Map(name.content, expr);
				} else {
					obj.List(name);
				}
			
			}
			if (tok.At(",")) {
				tok.Next();
			}

		}

		tok.RequireNext("}");

		return obj;
	}

	/// <summary>
	///		<para> Parses an array literal. </para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseArray(this Tokenizer tok) {
		Node arr = new Node(ARRAYLITERAL);
		tok.RequireNext("[");

		while (!tok.At("]")) {
			if (tok.At("...")) {
				tok.Next();
				Node spread = new Node(SPREAD);
				spread.Map("target", tok.ParseFromName());
				arr.List(spread);

			} else {
				arr.List(tok.ParseExpression());

			}

			// Consume but do not require commas. 
			if (tok.At(",")) { tok.Next(); }
		}

		tok.RequireNext("]");

		return arr;
	}

	/// <summary> Const array of tokens for 'rockets' for syntactic purposes. </summary>
	public static readonly string[] ROCKET_TOKENS = { "=>", "->" };

	/// <summary>
	///		<para> Parses a function declaration </para>
	///		<para> Function Declarations contain: 
	///			<para> varlist - list of parameter variables that get passed into this function </para>
	///			<para> codeblock - Body of code that belongs to the function </para>
	///		</para>
	/// </summary>
	/// <param name="tok"> Token Stream to read from </param>
	/// <returns> Program node built from tokenizer stream </returns>
	public static Node ParseFunc(this Tokenizer tok) {
		Node func = new Node(FUNCDEC);
		tok.RequireNext("func");

		func.Map("varlist", tok.ParseVarList());

		// tok.RequireNext(ROCKET_TOKENS);
		if (tok.At(ROCKET_TOKENS)) { tok.Next(); }
		
		func.Map("codeblock", tok.ParseCodeBlock());

		return func;
	}

	/// <summary> 
	///		<para> Parses a VariableList </para>
	///		<para> VariableLists contain: 
	///			<para> [datalist]? - The names of variables for use inside of the function body </para>
	///		</para>
	///		<para> Will be empty if there were no variables declared. </para>
	/// </summary>
	/// <param name="tok"></param>
	/// <returns></returns>
	public static Node ParseVarList(this Tokenizer tok) {
		Node vars = new Node(VARLIST);

		// var list must be a comma-separated list of valid names inside ()
		tok.RequireNext("(");

		while (!tok.At(")")) {
			// Must be a name
			tok.Require(NAME);
			// List it
			vars.List(tok.peekToken);
			// Then consume it
			tok.Next();

			// Consume separator ','
			if (tok.At(",")) { tok.Next(); }

		}

		// End with ')'
		tok.RequireNext(")");

		return vars;
	}


}
