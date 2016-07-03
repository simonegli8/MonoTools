using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using Renci.SshNet;

namespace MonoTools.VSExtension {

	public partial class Services {

		public async void ServerSetup() {
			try {
				var dlg = new Views.SetupSSHServer();

				if (dlg.ShowDialog().GetValueOrDefault()) {

					await ServerSetup(dlg.Url.Text, dlg.Username.Text, dlg.Password.Password, dlg.DebugPassword.Password, dlg.Ports.Text, dlg.Manual.IsChecked.GetValueOrDefault());
				}
			} catch (Exception ex) {
				logger.Error<Exception>(ex);
				MessageBox.Show(ex.Message, "MonoTools", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		public async Task ServerSetup(string url, string username, string password, string debugPassword, string ports, bool manual) {
			using (var ssh = new Connection(url, username, password)) {

				var setup = Assembly.GetExecutingAssembly().GetManifestResourceStream("MonoTools.VSExtension.MonoDebuggerServerSetup.exe");
				ssh.Sftp.UploadFile(setup, "MonoDebuggerSetup.exe");
				var args = $"-sudopwd={password}";
				if (!string.IsNullOrEmpty(ports)) args += $" -ports={ports}";
				if (!string.IsNullOrEmpty(debugPassword)) args += $" -password={debugPassword}";
				if (manual) args += " -manual";
				ssh.Ssh.RunCommand($"mono MonoDebuggerServerSetup.exe {args}");
			}

		}
	}
}
