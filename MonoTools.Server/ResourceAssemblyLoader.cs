using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

namespace MonoTools.Server {

	public class ResourceAssemblyLoader {

		static HashSet<string> loading = new HashSet<string>();

		public static void Init() {

			AppDomain.CurrentDomain.AssemblyResolve += (s, args) => {
				try {
					lock (loading) {  // put a lock, so we don't do an infinite loop
						if (loading.Contains(args.Name)) return null;
						loading.Add(args.Name);
					}
					// get the assembly filename for the requested assembly
					var assemblyName = args.Name;
					var comma = assemblyName.IndexOf(',');
					if (comma >= 0) assemblyName = assemblyName.Substring(0, comma).Trim();

					var filename = assemblyName + ".dll";
					// find file in probing paths
					var exe = Assembly.GetExecutingAssembly();
					var names = exe.GetManifestResourceNames();
					var dll = names.FirstOrDefault(f => f.EndsWith(filename));
					if (dll == null) return null;
					var reader = new BinaryReader(exe.GetManifestResourceStream(dll));
					var dllbytes = reader.ReadBytes((int)reader.BaseStream.Length);

					var pdbname = OS.Runtime.IsMono ? filename + ".mdb" : assemblyName + ".pdb";
					var pdb = names.FirstOrDefault(f => f.EndsWith(pdbname));
					if (pdb != null) {
						reader = new BinaryReader(exe.GetManifestResourceStream(pdb));
						var pdbbytes = reader.ReadBytes((int)reader.BaseStream.Length);
						// load assembly
						return Assembly.Load(dllbytes, pdbbytes);
					} else {
						// load assembly
						return Assembly.Load(dllbytes);
					}
				} catch {
				} finally {
					lock (loading) loading.Remove(args.Name);
				}
				return null;
			};
		}
	}
}
