using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MonoTools.Debugger {

	[Serializable]
	public class LocalCygwinLauncher: Launcher {

		public LocalCygwinLauncher(Server server, StartTask task): base(server, task) {
		}
		public override void Open() {
			base.Open();
			Host = new LocalCygwinHost(this);
		}
		public override string Map(string path) => Path.Combine(RemotePath, path);
	}

}
