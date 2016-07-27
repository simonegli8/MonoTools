using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

namespace MonoTools.Server.Setup {

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
					var file = exe.GetManifestResourceNames().First(f => f.EndsWith(filename));
					if (file == null) return null;
					var reader = new BinaryReader(exe.GetManifestResourceStream(file));
					// load assembly
					//Debugger.Log(1, "", $"Loading assembly {args.Name}");
					var name = new AssemblyName(assemblyName);
					return Assembly.Load(reader.ReadBytes((int)reader.BaseStream.Length));
				} catch {
				} finally {
					lock (loading) loading.Remove(args.Name);
				}
				return null;
			};
		}
	}
}
