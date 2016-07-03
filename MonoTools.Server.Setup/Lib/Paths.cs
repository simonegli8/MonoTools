using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Silversite.Services {

	public class Paths {
	
		public static void Split(string path, out string directory, out string name) {
			path = Normalize(path);
			int i = path.LastIndexOf('/');
			if (i >= 0 && i < path.Length) {
				directory = path.Substring(0, i);
				name = path.SafeSubstring(i+1);
			} else {
				directory = string.Empty;
				name = path;
			}
		}

		public static string Directory(string path) {
			if (string.IsNullOrEmpty(path) || path == "~") return "~";
			string dir, file;
			Split(path, out dir, out file);
			return dir;
		}

		public static string File(string path) {
			if (string.IsNullOrEmpty(path) || path == "~") return "";
			string dir, file;
			Split(path, out dir, out file);
			return file;
		}

		public static string Move(string file, string to) { return Combine(to, File(file)); }

		/// <summary>
		/// Returns the filename without path & extension 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string FileWithoutExtension(string path) {
			int i = path.LastIndexOf('.');
			int j = path.LastIndexOf('/')+1;
			if (i <= j) i = path.Length;
			return path.Substring(j, i - j);
		}

		/// <summary>
		/// Combines two paths, and resolves '..' segments.
		/// </summary>
		/// <param name="path1">The root path.</param>
		/// <param name="path2">The relative path to the root path.</param>
		/// <returns>The combined path.</returns>
		public static string Combine(string path1, string path2) {
			string path = "";
			int slash;

			if (path2.StartsWith("/..")) path2 = path2.Substring(1);

			while (path2.StartsWith("../")) { // resolve relative paths.
				if (path1.EndsWith("/")) {
					slash = path1.LastIndexOf('/', path1.Length-1);
					if (slash <= 0) slash = 0;
					path1 = path1.Substring(0, slash);
				} else {
					slash = path1.LastIndexOf('/', path1.Length-1);
					if (slash <= 0) slash = 0;
					slash = path1.LastIndexOf('/', slash-1);
					if (slash <= 0) slash = 0;
					path1 = path1.Substring(0, slash);
				}
				path2 = path2.Substring(3);
			}

			if (path2.StartsWith("~")) path2 = path2.Substring(1);
			if (path1.EndsWith("/")) {
				if (path2.StartsWith("/")) path2 = path2.Substring(1);
				path = path1 + path2;
			} else if (path2.StartsWith("/")) {
				path = path1 + path2;
			} else {
				path = path1 + "/" + path2;
			}
			return path;
		}

		public static string Combine(params string[] paths) {
			if (paths == null || paths.Length == 0) return "";
			else if (paths.Length == 1) return paths[0];
			else if (paths.Length == 2) return Combine(paths[0], paths[1]);
			else return Combine(paths[0], Combine(paths.Skip(1).ToArray()));
		}

		public static string Relative(string file, string relativePath) {
			if (relativePath.Contains(':')) return relativePath;
			return Paths.Combine(Paths.Directory(file) + "/", relativePath);
		}

		public static string Normalize(string path) => path;
		public static string Map(string path) => path;
		public static string Unmap(string physicalPath) => physicalPath;
		public static string Absolute(string path) => path;

		public static string AddSlash(string path) { return path.EndsWith("/") ? path : path + "/"; }
		public static string RemoveSlash(string path) { return path.EndsWith("/") ? path.Remove(path.Length - 1) : path; }

		public static string Extension(string path) {
			var name = File(path);
			int i = name.LastIndexOf('.');
			if (i > 0) return name.Substring(i+1).ToLower();
			return string.Empty;
		}

		public static string WithoutExtension(string path) {
			int i = path.LastIndexOf('.');
			int j = path.LastIndexOf('/');
			if (i > 0 && i > j) return path.Substring(0, i);
			return path;
		}

		public static string ChangeExtension(string path, string ext) {
			if (ext.StartsWith(".")) return WithoutExtension(path) + ext;
			else return WithoutExtension(path) + "." + ext;
		}

		private static bool MatchSingle(string pattern, string path) {
			pattern = pattern.StartsWith("!") ? pattern.Substring(1) : Regex.Escape(pattern).Replace("\\*\\*", ".*").Replace("\\*", "[^/\\\\]*").Replace("\\?", ".");
			return Regex.Match(path, pattern).Success;
		}
		/// <summary>
		/// Checks wether the path matches one of a comma or semicolon separated list of file patterns or a single file pattern.
		/// </summary>
		/// <param name="patterns">A comma or semicolon separared list of patterns or a single pattern</param>
		/// <param name="path">The path to check.</param>
		/// <returns>True if one of the patterns matches the path.</returns>
		public static bool Match(string patterns, string path) {
			foreach (var p in patterns.Tokens()) {
				if (MatchSingle(p, path)) return true;
			}
			return false;
		}

		public static bool ExcludeMatch(string patterns, string path) {
			var file = Paths.File(path);
			foreach (var p in patterns.Tokens()) {
				if (p == file || MatchSingle(p, path)) return true;
			}
			return false;
		}

		public static string Encode(string path) {
			var ichars = System.IO.Path.GetInvalidFileNameChars()
				.Concat(System.IO.Path.GetInvalidPathChars())
				.Where(ch => ch != '+')
				.Prepend('+')
				.Distinct()
				.ToList();
			var s = new StringBuilder(path);
			foreach (var ch in ichars) {
				s = s.Replace(ch.ToString(), "+" + ((IEnumerable<byte>)Encoding.UTF8.GetBytes(new char[] { ch })).StringList("{0:X}", ""));
			}
			return s.ToString();
		}

		public static string Decode(string path) {
			var regex = new Regex("~([0-9A-F]+)");
			return regex.Replace(path,
				new MatchEvaluator(m => {
					var match = m.Groups[1].Value;
					var x = uint.Parse(match.UpTo(8), System.Globalization.NumberStyles.AllowHexSpecifier);
					var ch = Encoding.UTF8.GetString(new byte[] { (byte)(x % 0xFF), (byte)(x >> 8 % 0xFF), (byte)(x >> 16 % 0xFF), (byte)(x >> 24 % 0xFF) })[0];
					return ch + match.Substring(Encoding.UTF8.GetBytes(new char[] { ch }).Length*2);
				}));
		}

	}
}