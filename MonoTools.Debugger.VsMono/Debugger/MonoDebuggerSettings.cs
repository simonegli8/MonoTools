using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MonoTools.Debugger;

namespace MonoProgram.Package.Debuggers {

	[Serializable]
	public class MonoDebuggerSettings: IDisposable {

		public ILauncher Launcher { get; }
		public IReadOnlyList<MonoSourceMapping> SourceMappings { get; }

		public MonoDebuggerSettings(ILauncher launcher, params MonoSourceMapping[] sourceMappings) {
			Launcher = launcher;
			SourceMappings = sourceMappings;
		}

		public static MonoDebuggerSettings Load(string saved) {
			var f = new BinaryFormatter();
			var buf = Convert.FromBase64String(saved);
			var m = new MemoryStream(buf);
			return (MonoDebuggerSettings)f.Deserialize(m);
		}

		public string Save() {
			var f = new BinaryFormatter();
			var m = new MemoryStream();
			f.Serialize(m, this);
			return Convert.ToBase64String(m.ToArray());
		}

		public void Dispose() => Launcher.Dispose();
	}
}