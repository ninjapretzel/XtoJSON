using System.Runtime.CompilerServices;
using Lib;

public static class XJSHelpers {
	/// <summary> Checks the string starting at position = startIndex to see if check directly matches the string from that position. </summary>
	/// <param name="str"> String to check </param>
	/// <param name="startIndex"> first index in str to match against first char in check </param>
	/// <param name="check"> string to check against str </param>
	/// <returns> true if, from startIndex for check.Length characters, str contains the same characters as check does. 
	/// False if str runs out of characters, or has any characters that don't match the parallel ones in check.</returns>
	internal static bool MatchAt(this string str, int startIndex, string check) {
		for (int i = 0; i < check.Length; i++) {
			int idx = i + startIndex;
			if (idx > str.Length) { return false; }
			if (str[idx] != check[i]) { return false; }
		}
		return true;
	}

	/// <summary> Checks the string for whitespace at a given index. Outside of the string is considered whitespace. </summary>
	/// <param name="str"> String to check </param>
	/// <param name="i"> index to check at </param>
	/// <returns> True if whitespace is at the index or index is invalid, false otherwise. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool WhitespaceAt(this string str, int i) {
		if (i < 0 || i >= str.Length) { return true; }
		char c = str[i];
		return c == ' ' || c == '\t' || c == '\n' || c == '\r';
	}

	/// <summary> Checks the string for an alphanumeric character at a given index. </summary>
	/// <param name="str"> String to check </param>
	/// <param name="i"> index to check at </param>
	/// <returns> True if there is an alphanumeric characater at the index, false otherwise. </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool AlphaNumAt(this string str, int i) {
		if (i < 0 || i >= str.Length) { return false; }
		char c = str[i];
		return ( (c >= 'a' && c <= 'z') 
			|| (c >= 'A' && c <= 'Z') 
			|| (c >= '0' && c <= '9'));
	}

	public static void QuickTestMatchAt() {
		string src = "ayy bee cee dee eie eff gee. This bee movie is the bees knees";
		string bee = "bee";
		for (int i = 0; i < src.Length; i++) {
			XJS.Debug.Log("Checking [" + src + "] at " + i + " : " + src.MatchAt(i, bee));
		}
	}

	/// <summary> Strips standard C-Style comments from text </summary>
	/// <param name="input"> Input text file to strip comments from </param>
	/// <returns> Modified string without comment blocks </returns>
	public static string StripCStyleComments(string input) {
		StringBuilder output = "";
		
		const int NORMAL = 0;
		const int LINE_COMMENT = 1;
		const int BLOCK_COMMENT = 2;
		const int STRING = 3;

		int mode = NORMAL;
		for (int i = 0; i < input.Length; i++) {
			char c = input[i];
			if (mode == NORMAL) {
				if (c == '/') {
					char c2 = input[i + 1];
					if (c2 == '/' || c2 == '*') {
						mode = c2 == '/' ? LINE_COMMENT : BLOCK_COMMENT;
						i++;
						continue;
					}
				} else if (c == '"') {
					mode = STRING;
				}
				output += c;

			} else if (mode == LINE_COMMENT) {
				
				if (c == '\n') { 
					mode = NORMAL;
					output += '\n';	
				}

			} else if (mode == BLOCK_COMMENT) {
				
				if (c == '*') {
					char c2 = input[i+1];
					if (c2 == '/') {
						i++;
						mode = NORMAL;
					}
				} else if (c == '\n') { output += '\n'; } 
				// Preserve line numbers

			} else if (mode == STRING) {

				if (c == '"') {
					char cp = input[i-1];
					if (cp != '\\') {
						mode = NORMAL;
					}
				}
				
				output += c;
			}


		}


		return output.ToString();
	}


}
