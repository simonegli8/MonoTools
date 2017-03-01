using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MonoTools.Debugger {

	[Serializable]
	public class RemoteSshLauncher : Launcher {

		protected string SourcePath;
		protected string RemotePath;

		public RemoteSshLauncher(Server server, StartTask task) : base(server, task) { }

		public override void Open() {
			base.Open();
			Host = new RemoteSshHost(this);
		}

		public virtual void Upload() => Host.UploadFolder(SourcePath, RemotePath);

	}
}
