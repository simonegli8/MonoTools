using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Linq;

namespace Silversite.Services {

	public static class Files {

		public static void Delete(IEnumerable<string> paths) {
			paths.Each(path => {
				if (path.Contains("*")) All(path).Each(p => Delete(p));
				path = Paths.Normalize(path);
				var ftppath = path.Substring(1);
				var diskpath = Paths.Map(path);
				if (Directory.Exists(diskpath)) {
						try {
							Directory.Delete(diskpath, true);
						} catch {
							var info = new DirectoryInfo(diskpath);
							var children = info.EnumerateFileSystemInfos("*", SearchOption.AllDirectories).ToList();
							foreach (var file in children.OfType<FileInfo>()) File.Delete(file.FullName);
							children.Reverse();
							foreach (var dir in children.OfType<DirectoryInfo>()) Directory.Delete(dir.FullName);
						}
				} else if (File.Exists(diskpath)) {
					File.Delete(diskpath);
				}
			});
		}
		public static void Delete(params string[] paths) { Delete((IEnumerable<string>)paths); }
	
		static void SaveRaw(Stream src, string path) {
			const int size = 8 * 1024;
			var buf = new byte[size];
			int n;
			if (src.CanSeek) src.Seek(0, SeekOrigin.Begin);
			using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
				while (src.Position < src.Length) {
					n = src.Read(buf, 0, size);
					file.Write(buf, 0, n);
				}
			}
		}

		public static void Save(Stream src, string path) {
			SaveRaw(src, Paths.Map(path));
		}

		public static void Save(string text, string path) {
			using (var w = new StreamWriter(new MemoryStream(2*text.Length))) {
				w.Write(text);
				w.Flush();
				Save(w.BaseStream, path);
			}
		}

		public static void Save(byte[] buffer, string path) {
			using (var w = new BinaryWriter(new MemoryStream(buffer.Length))) {
				w.Write(buffer);
				w.Flush();
				Save(w.BaseStream, path);
			}
		}

		public static void Save(object x, string path) {
			using (var m = new MemoryStream()) {
				var bf = new BinaryFormatter();
				bf.Serialize(m, x);
				Save(m, path);
			}
		}

		public static void Save(XContainer x, string path) { Save(x.ToString(SaveOptions.OmitDuplicateNamespaces), path); }


		public static void SaveWithPath(Stream src, string path) {
			if (!Directory.Exists(Paths.Directory(path))) CreateDirectory(Paths.Directory(path));
			Save(src, path);
		}

		public static void SaveWithPath(string text, string path) {
			if (!Directory.Exists(Paths.Directory(path))) CreateDirectory(Paths.Directory(path));
			Save(text, path);
		}

		public static void SaveWithPath(byte[] buffer, string path) {
			if (!Directory.Exists(Paths.Directory(path))) CreateDirectory(Paths.Directory(path));
			Save(buffer, path);
		}

		public static void SaveWithPath(object x, string path) {
			if (!Directory.Exists(Paths.Directory(path))) CreateDirectory(Paths.Directory(path));
			Save(x, path);
		}
		public static void SaveWithPath(XContainer x, string path) { SaveWithPath(x.ToString(SaveOptions.OmitDuplicateNamespaces), path); }

		public static void SaveLines(IEnumerable<string> lines, string path) {
			using (var w = new StreamWriter(new MemoryStream())) {
				foreach (var line in lines) w.WriteLine(line);
				w.Flush();
				Save(w.BaseStream, path);
			}
		}

		public static void SaveLinesWithPath(IEnumerable<string> lines, string path) {
			if (!Directory.Exists(Paths.Directory(path))) CreateDirectory(Paths.Directory(path));
			SaveLines(lines, path);
		}

		public static string Load(string path) {
			using (var r = new StreamReader(Read(path), Encoding.UTF8, true)) {
				return r.ReadToEnd();
			}
		}

		public static List<string> LoadLines(string path) {
			var res = new List<string>();
			using (var r = new StreamReader(Read(path), Encoding.UTF8, true)) {
				while (!r.EndOfStream) res.Add(r.ReadLine());
			}
			return res;
		}

		public static byte[] LoadBuffer(string path) {
			using (var r = new BinaryReader(Read(path))) {
				return r.ReadBytes((int)r.BaseStream.Length);
			}
		}

		public static object LoadSerializable(string path) {
			using (var f = new FileStream(path, FileMode.Open, FileAccess.Read)) {
				var bf = new BinaryFormatter();
				return bf.Deserialize(f);
			}
		}

		public static XElement LoadXElement(string path) {
			using (var r = new StreamReader(Read(path), Encoding.UTF8, true)) {
				return XElement.Load(r, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
			}
		}

		public static XDocument LoadXDocument(string path) {
			using (var r = new StreamReader(Read(path), Encoding.UTF8, true)) {
				return XDocument.Load(r, LoadOptions.PreserveWhitespace | LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
			}
		}

		public static void Append(string text, string path) {
			if (!Directory.Exists(Paths.Directory(path))) CreateDirectory(Paths.Directory(path));
			using (var w = new StreamWriter(Paths.Map(path), true)) w.Write(text); 
		}

		static IEnumerable<string> AllRecursive(DirectoryInfo dir, string patterns) {
			return
				dir.Exists ?
					dir.GetDirectories().SelectMany(d => AllRecursive(d, patterns))
						.Union(AllLocal(dir, patterns), StringComparer.OrdinalIgnoreCase)
					: new string[0];
		}
		static IEnumerable<string> AllLocal(DirectoryInfo dir, string patterns) {
			return
				dir.Exists ? 
					dir.GetFiles()
						.Select(f => Paths.Unmap(f.FullName))
						.Where(path => Paths.Match(patterns, path))
					: new string[0];
		}

		public static IEnumerable<string> All(string patterns) {
			return patterns
				.Tokens(s => Paths.Directory(s))
				.Select(s => s.Contains('*') ? Paths.Directory(s.UpTo('*')) : s)
				.Select(s => s.IsNullOrEmpty() ? "~/" : s)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				//.Where(d => !d.Contains("*"))
				.Select(d => new DirectoryInfo(d))
				.SelectMany(dir => AllRecursive(dir, patterns))
				.Distinct(StringComparer.OrdinalIgnoreCase);
		}
		public static IEnumerable<string> All(params string[] patterns) { return patterns.SelectMany(p => All(p)); }

		public static System.IO.FileSystemInfo Info(string path) {
			var file = new FileInfo(path);
			var dir = new DirectoryInfo(path);
			if (file.Exists) return file;
			if (dir.Exists) return dir;
			return null;
		}

		public static Stream Read(string path) {
			if (File.Exists(path)) return new FileStream(path, FileMode.Open, FileAccess.Read);
			throw new FileNotFoundException(string.Format("File {0} not found.", path));
		}

		public static Stream Write(string path) {
			 return new FileStream(path, FileMode.Create, FileAccess.Write);
		}

		public static void Move(string src, string dest) {
			if (src.Contains(";") || src.Contains('*') || src.Contains('?')) {
				var srcdir = src;
				if (src.Contains('*')) srcdir = srcdir.UpTo('*');
				All(src).Each(f => {
					Move(f, Paths.Combine(dest, Paths.Directory(f.Substring(srcdir.Length)), Paths.File(f)));
				});
			} else {
				if (src == dest) return;
				if (Directory.Exists(src)) Directory.Move(Paths.Map(src), Paths.Map(dest));
				else {
					try {
						File.Move(Paths.Map(src), Paths.Map(dest));
					} catch (Exception ex) {
						var dir = Paths.Directory(dest);
						if (!Directory.Exists(dir)) {
							CreateDirectory(dir);
							File.Move(Paths.Map(src), Paths.Map(dest));
						} else {
							throw ex;
						}
					}
				}
			}
		}

		public static void Copy(string src, string dest) {
			if (src.Contains(";") || src.Contains('*') || src.Contains('?')) {
				var srcdir = src;
				if (src.Contains('*')) srcdir = srcdir.UpTo('*');
				All(src).Each(f => {
					Copy(f, Paths.Combine(dest, Paths.Directory(f.Substring(srcdir.Length)), Paths.File(f)));
				});
			} else {
				if (src == dest) return;
				if (Directory.Exists(src)) {
					//TODO bug
					dest = Paths.Combine(dest, Paths.File(src));
					CreateDirectory(dest);
					var info = new DirectoryInfo(src);
					foreach (var obj in info.EnumerateFileSystemInfos()) Copy(Paths.Combine(src, obj.Name), Paths.Combine(dest, obj.Name));
				} else {
					try {
						File.Copy(Paths.Map(src), Paths.Map(dest), true);
					} catch (Exception ex) {
						var dir = Paths.Directory(dest);
						if (!Directory.Exists(dir)) {
							CreateDirectory(dir);
							File.Copy(Paths.Map(src), Paths.Map(dest), true);
						} else {
							throw ex;
						}
					}
				}
			}
		}

		public static void CreateDirectory(IEnumerable<string> paths) {
			foreach (var path in paths) {
				var diskpath = Paths.Map(path);
				if (!Directory.Exists(diskpath)) {
					Directory.CreateDirectory(diskpath);
				}
			}
		}
		public static void CreateDirectory(string paths) { CreateDirectory(paths.Tokens()); }
		public static void CreateDirectory(params string[] paths) { CreateDirectory(paths); }
	
	}
}