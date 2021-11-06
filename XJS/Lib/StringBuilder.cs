using System;
using Builder = System.Text.StringBuilder;

namespace Lib {
	/// <summary> Wraps System.Text.StringBuilder to add operator and implicit conversion functionality. </summary>
	public class StringBuilder {

		/// <summary> Instance of wrapped object of original type </summary>
		private Builder str;

		/// <summary> Wrap Blank Constructor </summary>
		public StringBuilder() { str = new Builder(); }
		/// <summary> Wrap string (Initial content) Constructor </summary>
		public StringBuilder(string s) { str = new Builder(s); }
		/// <summary> Wrap int (Capacity) Constructor </summary>
		public StringBuilder(int cap) { str = new Builder(cap); }

		/// <summary> Implicit conversion to string type </summary>
		public static implicit operator string(StringBuilder m) { return m.ToString(); }

		/// <summary> Implicit conversion from string type </summary>
		public static implicit operator StringBuilder(string s) { return new StringBuilder(s); }

		/// <summary> Operator for + bool. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, bool b) { return a.Append(b); }
		/// <summary> Operator for + byte. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, byte b) { return a.Append(b); }
		/// <summary> Operator for + char. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, char b) { return a.Append(b); }
		/// <summary> Operator for + char[]. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, char[] b) { return a.Append(b); }
		/// <summary> Operator for + decimal. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, decimal b) { return a.Append(b); }
		/// <summary> Operator for + double. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, double b) { return a.Append(b); }
		/// <summary> Operator for + short. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, short b) { return a.Append(b); }
		/// <summary> Operator for + int. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, int b) { return a.Append(b); }
		/// <summary> Operator for + long. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, long b) { return a.Append(b); }
		/// <summary> Operator for + object. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, object b) { return a.Append(b); }
		/// <summary> Operator for + sbyte. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, sbyte b) { return a.Append(b); }
		/// <summary> Operator for + float. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, float b) { return a.Append(b); }
		/// <summary> Operator for + string. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, string b) { return a.Append(b); }
		/// <summary> Operator for + ushort. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, ushort b) { return a.Append(b); }
		/// <summary> Operator for + uint. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, uint b) { return a.Append(b); }
		/// <summary> Operator for + ulong. The original type had a number of overloads for Append, so operators reflect that. Modifies the StringBuilder and returns itself, just like the Append call. </summary>
		public static StringBuilder operator +(StringBuilder a, ulong b) { return a.Append(b); }

		/// <summary> Wrapper for Capacity property. Current maximum character length without resizing.  </summary>
		public int Capacity { get { return str.Capacity; } set { str.Capacity = value; } }
		/// <summary> Wrapper for Length property. Current number of characters in the string. </summary>
		public int Length { get { return str.Length; } set { str.Length = value; } }
		/// <summary> Wrapper for MaxCapacity property. The absolute maximum number of space that this StringBuilder can be resized to</summary>
		public int MaxCapacity { get { return str.MaxCapacity; } }

		/// <summary> Wrapper for indexer </summary>
		/// <param name="index">Index to index at </param>
		/// <returns>character at position index</returns>
		public char this[int index] { get { return str[index]; } set { str[index] = value; } }

		/// <summary> 
		/// Gets the index of a character from this StringBuilder, as if it was converted to a string object now. 
		/// Obviously, not thread safe if other threads may modify this StringBuilder
		/// </summary>
		/// <param name="c"> Character to check </param>
		/// <returns> Index of the character c, if it exists, otherwise -1 if it does not. </returns>
		public int IndexOf(char c) {
			for (int i = 0; i < Length; i++) {
				if (c == this[i]) { return i; }
			}
			return -1;
		}

		/// <summary> Grabs substring starting at <paramref name="startIndex"/>, and ending at Length-1 </summary>
		/// <param name="startIndex"> Starting position </param>
		/// <returns> Substring from <paramref name="startIndex"/> to end of string </returns>
		public string Substring(int startIndex) {
			return this.Substring(startIndex, Length - startIndex);
		}
		/// <summary> Grabs substring starting at <paramref name="startIndex"/>, and <paramref name="length"/> chars long </summary>
		/// <param name="startIndex"> Starting position </param>
		/// <param name="length"> Length of substring </param>
		/// <returns> Substring from <paramref name="startIndex"/>, and <paramref name="length"/> chars long </returns>
		public string Substring(int startIndex, int length) {
			return ToString(startIndex, length);
		}

		#region Pass-Throughs

		/// <summary> Wrapper for Append(bool). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(bool value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(byte). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(byte value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(char). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(char value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(char[]). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(char[] value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(decimal). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(decimal value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(double). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(double value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(short). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(short value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(int). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(int value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(long). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(long value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(object). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(object value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(sbyte). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(sbyte value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(float). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(float value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(string). Appends the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(string value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(ushort). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(ushort value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(uint). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(uint value) { str.Append(value); return this; }
		/// <summary> Wrapper for Append(ulong). Appends the string representation of the parameter to the end of the builder's array, resizing if necessary. </summary>
		public StringBuilder Append(ulong value) { str.Append(value); return this; }

		/// <summary> Wrapper for Append(char, int). Appends 'value' to the end of the StringBuilder 'repeatCount' times, resizing if necessary. </summary>
		public StringBuilder Append(char value, int repeatCount) { str.Append(value, repeatCount); return this; }

		/// <summary> Wrapper for Append(char[], int, int). Appends a 'substring' from a char[] to the end of the StringBuilder, resizing if necessary. </summary>
		public StringBuilder Append(char[] value, int startIndex, int charCount) { str.Append(value, startIndex, charCount); return this; }

		/// <summary> Wrapper for Append(string, int, int). Appends a 'substring' from a string to the end of the StringBuilder, resizing if necessary. </summary>
		public StringBuilder Append(string value, int startIndex, int count) { str.Append(value, startIndex, count); return this; }

		//public StringBuilder AppendFormat(string a, object b) { str.AppendFormat(a, b); return this; }
		/// <summary> Wrapper for AppendFormat(string, object[]). Appends a formatted string to the end of the StringBuilder, resizing if necessary. </summary>
		public StringBuilder AppendFormat(string a, params object[] b) { str.AppendFormat(a, b); return this; }

		/// <summary> Wrapper for AppendLine(). Appends a newline to the end of the StringBuilder, resizing if necessary. </summary>
		public StringBuilder AppendLine() { str.AppendLine(); return this; }
		/// <summary> Wrapper for AppendLine(string). Appends the given string and a newline to the end of the StringBuilder, resizing if necessary. </summary>
		public StringBuilder AppendLine(string value) { str.AppendLine(value); return this; }

		/// <summary> Clear the StringBuilder. Does the same thing (Sets Length property to zero). According to MS's docs, this is what their method does anyway, so this cuts out the middleman. </summary>
		public StringBuilder Clear() { str.Length = 0; return this; }

		/// <summary> Wrapper for CopyTo(). Copies the contents of this StringBuilder to some char[]</summary>
		public void CopyTo(int sourceIndex, char[] dest, int destIndex, int count) {
			str.CopyTo(sourceIndex, dest, destIndex, count);
		}

		/// <summary> Makes sure that there is at least capacity space for characters. </summary>
		public int EnsureCapacity(int capacity) { return str.EnsureCapacity(capacity); }

		/// <summary> Is this equal to some other object? </summary>
		public override bool Equals(object other) { return str.Equals(other); }
		/// <summary> Is this equal to some System.StringBuilder? </summary>
		public bool Equals(Builder other) { return str.Equals(other); }
		/// <summary> Is this eqal to some other StringBuilder? </summary>
		public bool Equals(StringBuilder other) { return str.Equals(other.str); }
		/// <summary> Get the hash code of the wrapped StringBuilder </summary>
		public override int GetHashCode() { return str.GetHashCode(); }

		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, bool value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, byte value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, char value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, char[] value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, decimal value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, double value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, short value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, int value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, long value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, object value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, sbyte value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, float value) { str.Insert(index, value); return this; }
		/// <summary> Insert 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, string value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, ushort value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, uint value) { str.Insert(index, value); return this; }
		/// <summary> Insert the string representation of 'value' at 'index'. </summary>
		public StringBuilder Insert(int index, ulong value) { str.Insert(index, value); return this; }

		/// <summary> Insert the string 'value' at 'index', and repeat 'count' times. </summary>
		public StringBuilder Insert(int index, string value, int count) { str.Insert(index, value, count); return this; }
		/// <summary> Insert a 'substring' from 'startIndex', 'charCount' long in 'value' at 'index' in the StringBuilder. </summary>
		public StringBuilder Insert(int index, char[] value, int startIndex, int charCount) { str.Insert(index, value, startIndex, charCount); return this; }

		/// <summary> Remove 'length' characters begining at 'start'</summary>
		public StringBuilder Remove(int start, int length) { str.Remove(start, length); return this; }

		/// <summary> Replace all instances of 'oldChar' with 'newChar' </summary>
		public StringBuilder Replace(char oldChar, char newChar) { str.Replace(oldChar, newChar); return this; }
		/// <summary> Replace all instances of 'oldChar' with 'newChar' inside of a given substring </summary>
		public StringBuilder Replace(char oldChar, char newChar, int startIndex, int count) { str.Replace(oldChar, newChar, startIndex, count); return this; }
		/// <summary> Replace all instances of 'oldValue' with 'newValue' </summary>
		public StringBuilder Replace(string oldValue, string newValue) { str.Replace(oldValue, newValue); return this; }
		/// <summary> Replace all instances of 'oldValue' with 'newValue' inside of a given substring</summary>
		public StringBuilder Replace(string oldValue, string newValue, int startIndex, int count) { str.Replace(oldValue, newValue, startIndex, count); return this; }

		/// <summary> Convert the StringBuilder to an immutable string </summary>
		public override string ToString() { return str.ToString(); }
		/// <summary> Convert a substring in the StringBuilder to an immutable string </summary>
		public string ToString(int startIndex, int length) { return str.ToString(startIndex, length); }

		#endregion


	}


}
