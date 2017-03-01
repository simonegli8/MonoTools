using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoTools.Debugger {

	public abstract class Host : IDisposable {

		public Action<int> Progress { get; set; }
		public Action<string> Log { get; set; }
		public Server Server { get; set; }
		public StartTask Task { get; set; }
		public virtual string ScriptExtension => "";

		public Host(Launcher launcher) { Server = launcher.Server; Task = launcher.Task; }

		public abstract void ChangeDir(string path);
		public abstract Task<string> Run(string scriptcontent);
		public abstract void SetEnvironment(IEnumerable<KeyValuePair<string, string>> variables);
		public abstract string Map(string path);
		public abstract void Kill();
		public abstract void UploadFolder(string sourcePath, string remotePath);
		public abstract void Dispose();
	}
}
