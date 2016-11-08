using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.IO;
using Renci.SshNet;

namespace MonoTools.VisualStudio {

	public partial class Services {

		const string DebugExe = "monodebug.exe";

		Window setupForm;
		public async void ServerSetup() {
			try {
				setupForm = new Views.SetupSSHServer();

				setupForm.Show();
			} catch (Exception ex) {
				logger.Error<Exception>(ex);
				MessageBox.Show(ex.Message, "MonoTools", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		public void ServerSetup(string url, string username, string password, string debugPassword, string ports, bool manual) {
			using (var ssh = new Connection(url, username, password)) {

				var exe = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
				var path = Path.Combine(Path.GetDirectoryName(exe), DebugExe);
				using (var setup = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					ssh.Sftp.UploadFile(setup, DebugExe);
				}
				var args = $"-sudopwd={password}";
				if (!string.IsNullOrEmpty(ports)) args += $" -ports={ports}";
				if (!string.IsNullOrEmpty(debugPassword)) args += $" -password={debugPassword}";
				if (manual) args += " -manual";
				logger.Trace($"run ssh://{username}@{url} => mono {DebugExe} {args}");
				ssh.Ssh.RunCommand($"mono {DebugExe} {args}");
			}
		}

		public async Task ServerUpgrade(string url, string ports, string password) {
			var server = new MonoClient.DebugClient(false, ports, password);
			var session = await server.ConnectToServerAsync(new Uri(url).Host);
			session.UpgradeServer(ports, password);
		}
	}
}
