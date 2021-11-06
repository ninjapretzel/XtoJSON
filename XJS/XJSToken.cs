using System.Linq;

public partial class XJS {

	/// <summary> Represents a single token read from a source script </summary>
	public struct Token {
		
		/// <summary> Fixed, impossible string to represent all invalid tokens </summary>
		public const string INVALID = "!INVALID";
		/// <summary> Generic invalid token for WTF moments. </summary>
		public static Token INVALID_TOKEN = new Token(INVALID);

		/// <summary> Create an invalid token at a certain spot. </summary>
		/// <param name="line"> line number, if applicable </param>
		/// <param name="col"> column in line, if applicable </param>
		/// <returns> Invalid token at location </returns>
		public static Token Invalid(int line = -1, int col = -1) {
			return new Token(INVALID, line, col);
		}

		/// <summary> Generic done token for being FINISHED! </summary>
		public static readonly Token DONE_TOKEN = new Token("DONE!", INVALID);

		/// <summary> Create a done token at a certain spot. </summary>
		/// <param name="line"> line number, if applicable </param>
		/// <param name="col"> column in line, if applicable </param>
		/// <returns> Done token at location </returns>
		public static Token Done(int line = -1, int col = -1) {
			return new Token("DONE!", INVALID, line, col);
		}

		/// <summary> Content of the token </summary>
		public string content { get; private set; }
		/// <summary> Type of the token </summary>
		public string type { get; private set; }
		/// <summary> Line the token was created on, if applicable. </summary>
		public int line { get; private set; }
		/// <summary> Column of line the token was created on, if applicable. </summary>
		public int col { get; private set; }
		

		/// <summary> Assigns both content and type to the same string. </summary>
		/// <param name="content"> Content/type for this token</param>
		public Token(string content, int line = -1, int col = -1) { 
			this.content = type = content; 
			this.line = line; 
			this.col = col; 
		}

		/// <summary> Construct a token with a given content/type </summary>
		/// <param name="content"> Content for token </param>
		/// <param name="type"> Type for token </param>
		public Token(string content, string type, int line = -1, int col = -1) {
			this.content = content;
			this.type = type;
			this.line = line;
			this.col = col;
		}

		/// <summary> Returns true if this token is a 'kind' </summary>
		public bool Is(string kind) { return type == kind; }

		/// <summary> Returns true if this token's type is contained in 'types' </summary>
		public bool Is(string[] types) { return types.Contains(type); }

		/// <summary> Returns true if this token represents a valid token from a source file.
		/// False if it represents an error or the DONE condition. </summary>
		public bool IsValid { get { return type != INVALID; } }

		/// <summary> True if this token is a space tab or newline, false otherwise. </summary>
		public bool IsWhitespace { get { return content == " " || content == "\t" || content == "\n"; } }

		/// <summary> Human readable representation </summary>
		public override string ToString() {
			string c = StringContent();
			return $"{{{c}}} @ {line}:{col}";
		}
		private string StringContent() {
			if (content == " ") { return "SPACE"; }
			if (content == "\t") { return "TAB"; }
			if (content == "\n") { return "NEWLINE"; }
			if (!ReferenceEquals(type, content)) { return type + ": [" + content + "]"; }
			return type;
			
		}


	}

}
