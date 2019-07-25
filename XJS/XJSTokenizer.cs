using System;
using System.Text.RegularExpressions;

public partial class XJS {

	/// <summary> Represents a stream of tokens coming from a loaded script </summary>
	public class Tokenizer {

		/// <summary> Original source string </summary>
		public string src { get; private set; }

		/// <summary> Current column </summary>
		public int col { get; private set; }
		/// <summary> Current line </summary>
		public int line { get; private set; }
		/// <summary> Current raw char position </summary>
		public int i { get; private set; }


		/// <summary> Token ahead of cursor, which hasn't been consumed. </summary>
		public Token peekToken { get; private set; }
		/// <summary> Last consumed token </summary>
		public Token lastToken { get; private set; }
		/// <summary> Last consumed non-whitespace token </summary>
		public Token lastRealToken { get; private set; }

		/// <summary> Gets the content from the current peek token </summary>
		public string content { get { return peekToken.content; } }

		/// <summary> Gets the type from the current peek token </summary>
		public string atType { get { return peekToken.type; } }


		/// <summary> Basic Constructor.</summary>
		/// <param name="source"> Source file to read. </param>
		public Tokenizer(String source) {
			src = source.Replace("\r\n", "\n").Replace("\r", "\n");
			Reset();
		}

		/// <summary> Throw an exception with a given message </summary>
		/// <param name="message"></param>
		public void Error(string message) { throw new Exception(message + "\n" + this); }
		
		/// <summary> Throws an exception if the peekToken is NOT the given type. </summary>
		public void Require(string type) { if (!peekToken.Is(type)) { Error("Expected: " + type); } }

		/// <summary> Throws an exception if the peekToken is NOT one of the given types. </summary>
		public void Require(string[] types) {
			if (!peekToken.Is(types)) {
				string str = "";
				foreach (string s in types) { str += s + ", "; }
				Error("Expected " + str);
			}
		}

		/// <summary> Throws an exception if the peekToken is NOT the given type, 
		/// but if it is, consumes it. </summary>
		public void RequireNext(string type) { Require(type); Next(); }

		/// <summary> Throws an exception if the peekToken is NOT one of the given types, 
		/// but if it is, consumes it. </summary>
		public void RequireNext(string[] types) { Require(types); Next(); }


		/// <summary> Returns if the peekToken is a given type </summary>
		public bool At(string type) { return peekToken.Is(type); }

		/// <summary> Returns if the peekToken is one of a given set of types </summary>
		public bool At(string[] types) { return peekToken.Is(types); }

		/// <summary> Returns true if the Tokenizer is out of tokens. </summary>
		public bool Done { get { return !peekToken.IsValid; } }

		/// <summary> Resets the tokenizer to its initial state </summary>
		public void Reset() {
			i = 0;
			line = 1;
			col = 0;
			lastRealToken = lastToken = Token.INVALID_TOKEN;
			peekToken = Peek();
			while (peekToken.IsWhitespace) {
				Move();
			}
		}


		/// <summary> Get the next token </summary>
		/// <returns> Token that has been removed from context, or the current token if invalid. </returns>
		public Token Next() {
			if (peekToken.IsValid) {
				Token save = lastRealToken;

				if (Move()) {
					while (peekToken.IsValid && peekToken.IsWhitespace) { Move(); }
				}

				return save;
			}

			return peekToken;
		}

		

		/// <summary> Moves this tokenizer forward by one token. </summary>
		/// <returns> True if moved at all, false if nothing happened. </returns>
		public bool Move() {
			if (peekToken.IsValid) {
				if (peekToken.Is(NEWLINE)) { col = 0; line++; } 
				else { col += peekToken.content.Length; }

				i += peekToken.content.Length;
				lastToken = peekToken;
				if (!peekToken.IsWhitespace) { lastRealToken = peekToken; }
				peekToken = Peek();
			}
			return peekToken.IsValid;
		}

		/// <summary> Peeks ahead at the next token that has yet to be consumed. </summary>
		/// <returns> Next token sitting in front of the head, or an invalid token if out of characters or WTF. </returns>
		public Token Peek() {
			if (i >= src.Length) { return Token.Done(line, col); }
			// Whitespace
			char c = src[i];
			if (c == ' ') { return new Token(SPACE, line, col); }
			if (c == '\t') { return new Token(TAB, line, col); }
			if (c == '\n') { return new Token(NEWLINE, line, col); }
			if (c == '\"') { return ExtractString('\"'); }
			if (c == '\'') { return ExtractString('\''); }
			foreach (string p in punct) { if (src.MatchAt(i, p)) { return new Token(p, line, col); } }
			foreach (string k in keywords) { if (src.MatchAt(i, k) && !src.AlphaNumAt(i + k.Length)) { return new Token(k, line, col); } }

			Match nameCheck = name.Match(src, i);
			if (nameCheck.Success && nameCheck.Index == i) { return new Token(nameCheck.Value, NAME, line, col); }

			Match numCheck = num.Match(src, i);
			if (numCheck.Success && numCheck.Index == i) { return new Token(numCheck.Value, NUMBER, line, col); }

			return Token.Invalid(line, col);
		}

		/// <summary> Error token message if a newline is sitting inside of a string literal.</summary>
		public static readonly string BAD_STRING_NEWLINE_INSIDE = "Newline in string literal";
		/// <summary> Error token message if no matching character for a string delimeter. </summary>
		public static readonly string BAD_STRING_NO_MATCHING_QUOTE = "No matching quote for string literal";

		/// <summary> Extracts a string from the current position in the source </summary>
		/// <param name="match"> Character to match on the other end of the string region </param>
		/// <returns> Token created from matched string, or an error token if it FAILED. </returns>
		private Token ExtractString(char match) {
			// Find next actual newline
			int nextNL = src.IndexOf('\n', i + 1);
			// Make index always greater than any characters in the string.
			if (nextNL == -1) { nextNL = src.Length + 1; }

			int nextMatch = src.IndexOf(match, i + 1);
			while (true) {
				if (nextMatch == -1) { return new Token(BAD_STRING_NO_MATCHING_QUOTE, Token.INVALID, line, col); }
				if (nextMatch > nextNL) { return new Token(BAD_STRING_NEWLINE_INSIDE, Token.INVALID, line, col); }
				if (src[nextMatch - 1] != '\\') { break; }
				nextMatch = src.IndexOf(match, nextMatch + 1);
			}

			int len = nextMatch - i + 1;
			return new Token(src.Substring(i, len), STRING, line, col);
		}

		public override string ToString() {
			return "Token: " + peekToken + " on Line: " + line + " Col " + col;
		}
	}
}
