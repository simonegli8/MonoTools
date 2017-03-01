using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Renci.SshNet;

namespace MonoTools.Debugger {

	public enum ServerTypes { LocalMono, WSLBash, Cygwin, RemoteSsh };

	[Serializable]
	public class Server {

		public string Url { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public PrivateKeyFile[] Keys { get; set; }
		public string Display { get; set; }
		public string InitScript { get; set; }
		public string RemotePath { get; set; }
		public ServerTypes Type { get; set; }


		public void Launcher(StartTask app) {
			switch (Type) {
			case ServerTypes.LocalMono: return new LocalLauncher(this, app);
			case ServerTypes.RemoteSsh: return new RemoteSshLauncher(this, app);
			case ServerTypes.WSLBash: return new LocalBashLauncher(this, app);
			case ServerTypes.Cygwin: return new LocalCygwinLauncher(this, app);
			}
		}

		public void Start(StartTask app) {
			Launcher(app).Launch();
		}
	}

}
