using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MonoTools.Debugger {

	public class LocalBashHost : LocalHost, IDisposable {

		protected virtual string BashPath => "";
		protected virtual string BashExe => "bash.exe";
		protected virtual string RootPath => "/mnt/";

		protected Process bash;
		protected StreamWriter StandardInput => bash.StandardInput;
		protected StreamReader StandardOutput => bash.StandardOutput;

		public LocalBashHost(Launcher launcher): base(launcher) {
			// TODO launch bash
		}

		public override void ChangeDir(string path) => Run($"cd {Map(path)}");
		public virtual async Task<string> Run(string scriptcontent) {
			StandardInput.Write(scriptcontent.Replace(Environment.NewLine, "\\n")+"\\n");
			return StandardOutput.ReadToEnd();
		}
		public virtual void SetEnvironment(IEnumerable<KeyValuePair<string, string>> variables) {
			var cmd = new StringBuilder();
			foreach (var kv in variables) {
				Run($"export {kv.Key}={kv.Value}");
			}
		}
		public virtual string Map(string path) => RootPath + (OutputPath + path).Replace('\\', '/');
		public virtual void UploadFolder(string sourcePath, string remotePath) { }
		public virtual void Dispose() { bash.Kill();  }
	}
}
