using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Silversite {

	public static class StringExtensions {

		public static string String<T>(this IEnumerable<T> list, string format) {
			var s = new StringBuilder();
			foreach (var e in list) s.Append(string.Format(format, e));
			return s.ToString();
		}

		public static string String<T>(this IEnumerable<T> list) {
			var s = new StringBuilder();
			if (typeof(T) == typeof(string)) foreach (string e in list.OfType<string>()) s.Append(e);
			else foreach (var e in list) s.Append(e.ToString());
			return s.ToString();
		}

		public static string StringList<T>(this IEnumerable<T> list, string separator) {
			var s = new StringBuilder();
			var first = true;
			if (typeof(T) == typeof(string)) {
				foreach (string e in list.OfType<string>()) {
					if (!first) s.Append(separator);
					else first = false;
					s.Append(e);
				}
			} else {
				foreach (var e in list) {
					if (!first) s.Append(separator);
					else first = false;
					s.Append(e.ToString());
				}
			}
			return s.ToString();
		}

		public static string StringList<T>(this IEnumerable<T> list, string format, string separator) {
			var s = new StringBuilder();
			var first = true;
			foreach (var e in list) {
				if (!first) s.Append(separator);
				else first = false;
				s.Append(string.Format(format, e));
			}
			return s.ToString();
		}

		public static string StringList<T>(this IEnumerable<T> list, char separator) {
			var s = new StringBuilder();
			var first = true;
			if (typeof(T) == typeof(string)) {
				foreach (string e in list.OfType<string>()) {
					if (!first) s.Append(separator);
					else first = false;
					s.Append(e);
				}
			} else {
				foreach (var e in list) {
					if (!first) s.Append(separator);
					else first = false;
					s.Append(e.ToString());
				}
			}
			return s.ToString();
		}

		public static string SafeSubstring(this string str, int start, int len) {
			int end = start + len;
			if (end < 0) end = 0;
			if (start < 0) start = 0;
			if (end > str.Length) end = str.Length;
			if (start > str.Length) start = str.Length;
			if (start >= end) return string.Empty;
			else return str.Substring(start, end - start);
		}

		public static string SafeSubstring(this string str, int start) {
			if (start < 0) start = 0;
			if (start < str.Length) return str.Substring(start);
			return string.Empty;
		}

		public static string UpTo(this string str, int end) {
			if (end <= 0) return string.Empty;
			if (end >= str.Length) return str;
			return str.Substring(0, end);
		}

		public static string UpTo(this string str, char ch) {
			int index = str.IndexOf(ch);
			return index >= 0 ? str.UpTo(index) : str;
		}

		public static string UpTo(this string str, string pattern) {
			int index = str.IndexOf(pattern);
			return index >= 0 ? str.UpTo(index) : str;
		}

		public static string FromOn(this string str, int start) {
			if (start <= 0) return str;
			if (start >= str.Length) return string.Empty;
			return str.Substring(start);
		}

		public static string FromOn(this string str, char ch) {
			int index = str.IndexOf(ch);
			return index >= 0 ? str.FromOn(index) : str;
		}

		public static string FromOn(this string str, string pattern) {
			int index = str.IndexOf(pattern);
			return index >= 0 ? str.FromOn(index) : str;
		}

		public static string After(this string str, char ch) {
			int index = str.IndexOf(ch);
			return index >= 0 ? str.FromOn(index+1) : str;
		}
		public static string After(this string str, string pattern) {
			int index = str.IndexOf(pattern);
			return index >= 0 ? str.FromOn(index + pattern.Length) : str;
		}
		public static string UpToIncluding(this string str, char ch) {
					int index = str.IndexOf(ch);
			return index >= 0 ? str.UpTo(index+1) : str;
		}
		
		public static string UpToIncluding(this string str, string pattern) {
			int index = str.IndexOf(pattern);
			return index >= 0 ? str.UpTo(index + pattern.Length) : str;
		}

		public static string Repeat(this string str, int n) {
			var s = new StringBuilder();
			while (n > 0) s.Append(str);
			return s.ToString();
		}

		public static List<T> SplitList<T>(this string s, Func<string, T> select, params char[] separator)
		{
			if (s == null) return new List<T>();
			if (separator.Length == 0) separator = new char[2] {',', ';'};
			return s.Split(separator).Select(str => select(str.Trim())).ToList();
		}
		public static List<T> SplitList<T>(this string s, params char[] separator) {
			var tokens = s.SplitList(separator);
			var list = new List<T>();
			foreach (var t in tokens) {
				T x;
				if (t.TryParse<T>(out x)) list.Add(x);
			}
			return list;
		}
		public static List<string> SplitList(this string s, params char[] separator) { return s.SplitList(s2 => s2, separator); }
		public static List<string> SplitList(this string s) { return s.SplitList(',', ';'); }
		public static List<T> Tokens<T>(this string s, Func<string, T> select, params char[] separator) {
			if (s == null) return new List<T>();
			if (separator.Length == 0) separator = new char[2] { ',', ';' };
			return s.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(str => select(str.Trim())).ToList();
		}
		public static List<T> Tokens<T>(this string s, params char[] separator) {
			var tokens = s.Tokens(separator);
			var list = new List<T>();
			foreach (var t in tokens) {
				T x;
				if (t.TryParse<T>(out x)) list.Add(x);
			}
			return list;
		}
		public static List<string> Tokens(this string s, params char[] separator) { return s.Tokens(s2 => s2, separator); }
		public static List<string> Tokens(this string s) { return s.Tokens(',', ';'); }

		public static T Parse<T>(this string s) {
			var type  = typeof(T);
			if (type == typeof(string)) return (T)Convert.ChangeType(s, typeof(T));
			if (type.IsEnum) return (T)Enum.Parse(type, s);
			var m = type.GetMethod("Parse", System.Reflection.BindingFlags.Static);
			return (T)m.Invoke(null, s);
		}

		public static bool TryParse<T>(this string s, out T x) {
			var type = typeof(T);
			if (type == typeof(string)) {
				x = (T)Convert.ChangeType(s, typeof(T));
				return true;
			} else {
				//if (type.IsEnum) return Enum.TryParse<T>(s, out x);
				var m = type.GetMethod("TryParse", System.Reflection.BindingFlags.Static);
				x = default(T);
				return (bool)m.Invoke(null, s, x);
			}
		}

		public static bool ContainsAny(this string s, params char[] chars) { return chars.Any(ch => s.Contains(ch)); }
		public static bool ContainsAny(this string s, params string[] strs) { return strs.Any(str => s.Contains(str)); }

#if !NET40
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif

		public static bool IsNullOrEmpty(this string str) { return str == null || str == string.Empty; }
		public static bool IsNullOrWhiteSpace(this string str) {
			if (str == null || str == string.Empty) return true;
			int i = 0;
			while (i < str.Length && char.IsWhiteSpace(str[i])) i++;
			return i >= str.Length;
		}

		public static StringBuilder Append(this StringBuilder sb, string format, params object[] args) { return sb.Append(string.Format(format, args)); }
	}
}