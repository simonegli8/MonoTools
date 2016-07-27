using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MonoTools.Server.Setup {

	public class Unzip {

		public static Action<double> NotifyProgress { get; set; } = Installer.NotifyProgress;

		public static Stream Seekable(Stream source) {
			if (!source.CanSeek) {
				var m = new MemoryStream();
				source.CopyTo(m);
				m.Position = 0;
				return m;
			}
			return source;
		}

		/// <summary>
		///  Extracts a Stream of a Zip file to a folder.
		/// </summary>
		/// <param name="zipStream">A zip file stream to extract</param>
		/// <param name="outFolder">The main folder to extract the zip to</param>
		/// <param name="filter">Either returns:
		///		- null, to omit extracting the file
		///		- the unchanged input string to store the file in its intended location
		///		- A relative or absolute path, to extract the file to a different location
		///</param>
		public static void Extract(Stream zipStream, string outFolder, Func<string, string> filter) {
			UnzipDotNetZip(zipStream, outFolder, filter);
		}

		// functions implementing Extract with different providers.

		public static void UnzipCustom(Stream zipStream, string outFolder, Func<string, string> filter = null) {
			var notifier = NotifyProgress != null ? new Action<Silversite.Services.ProgressArgs>(args => NotifyProgress(args.Progress)) : null;
			Silversite.Services.Zip.Extract(zipStream, outFolder, filter, notifier); // broken on Mono
		}

		public static void UnzipUnzip(Stream zipStream, string outFolder, Func<string, string> filter = null) {
			// use unzip unix tool (slow)
			var tmp = Path.GetTempFileName();
			var path = outFolder;

			if (filter != null) path = Path.GetTempPath();

			using (var f = new FileStream(tmp, FileMode.Create, FileAccess.Write)) {
				zipStream.CopyTo(f);
			}
			var list = OS.Runtime.Execute("unzip", "-l " + tmp);
			var n = OS.Runtime.LinesCount(list);
			OS.Runtime.ScriptProgress = p => NotifyProgress(((double)p/n)*0.6);
			OS.Runtime.Execute("unzip", tmp + " -o -d " + path);
			// filter
			if (filter != null) {
				var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).ToArray();
				var zipfiles = files.Select(f => f.StartsWith(path) ? f.Substring(path.Length) : f);
				foreach (var file in zipfiles) {
					var name = filter(file);
					if (name != null) {
						File.Copy(Path.Combine(path, file), Path.Combine(outFolder, name), true);
					}
				}
				Directory.Delete(path, true);
			}
		}

		public static void UnzipDotNetZip(Stream zipStream, string outFolder, Func<string, string> filter = null) { // use DotNetZip
			using (var zip = Ionic.Zip.ZipFile.Read(zipStream)) {
				var n = zip.Count;
				Action<double> progress = p => NotifyProgress(((double)p/n)*0.6);
				int i = 0;
				foreach (var e in zip) {
					var name = e.FileName;
					if (filter != null) name = e.FileName = filter(name);
					if (name != null) e.Extract(outFolder, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
				progress(i++);
				}
			}
		}

		public void UnzipSharpZip(Stream zipStream, string outFolder, Func<string, string> filter = null) { // use SharpZipLib
			const int N = 4096;
			var zip = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(zipStream);
			var e = zip.GetNextEntry();
			var len = zipStream.Length;
			Action<double> progress = p => NotifyProgress(((double)p/len)*0.6);
			while (e != null) {
				var name = e.Name;
				if (filter != null) name = filter(name);
				Stream dest;
				if (name != null) {
					var fullZipToPath = Path.Combine(outFolder, name);
					string directoryName = Path.GetDirectoryName(fullZipToPath);
					if (!string.IsNullOrEmpty(directoryName)) Directory.CreateDirectory(directoryName);
					dest = File.OpenWrite(fullZipToPath);
				} else {
					dest = Stream.Null;
				}
				zip.CopyTo(dest, N);
				progress(zipStream.Position);
				e = zip.GetNextEntry();
			}
		}
	}
}
