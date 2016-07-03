using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;

namespace MonoTools.Library {

	public class Pdb2MdbGenerator {
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public void GeneratePdb2Mdb(string directoryName) {
			//logger.Trace(directoryName);
			IEnumerable<string> files =
				 Directory.GetFiles(directoryName, "*.dll")
					  .Concat(Directory.GetFiles(directoryName, "*.exe"))
					  .Where(x => !x.Contains(".vshost.exe"));
			//logger.Trace(files.Count());

			var dirInfo = new DirectoryInfo(directoryName);

			Parallel.ForEach(files, file => {
				try {
					string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
					string pdbFile = Path.Combine(Path.GetDirectoryName(file), fileNameWithoutExt + ".pdb");
					string mdbFile = file + ".mdb";
					if (File.Exists(pdbFile) && (!File.Exists(mdbFile) || File.GetLastWriteTimeUtc(pdbFile) >= File.GetLastWriteTimeUtc(mdbFile))) {
						logger.Trace("Generate mdb for: " + Path.GetFileName(file));
						Pdb2Mdb.Converter.Convert(file);
					}
				} catch (Exception ex) {
					logger.Trace(ex);
				}
			});

			logger.Trace("Transformed Debuginformation pdb2mdb");
		}

		public void RemoveMdbs(string directoryName) {
			IEnumerable<string> files =
				 Directory.GetFiles(directoryName, "*.dll")
					  .Concat(Directory.GetFiles(directoryName, "*.exe"))
					  .Where(x => !x.Contains(".vshost.exe"))
					  .Select(f => f + ".mdb")
					  .Where(f => File.Exists(f));
			Parallel.ForEach(files, file => File.Delete(file));
		}
	}
}