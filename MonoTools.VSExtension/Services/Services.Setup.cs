﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using Renci.SshNet;

namespace MonoTools.VSExtension {

	public partial class Services {

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

				var setup = Assembly.GetExecutingAssembly().GetManifestResourceStream("MonoTools.VSExtension.MonoToolsServerSetup.exe");
				ssh.Sftp.UploadFile(setup, "MonoDebuggerSetup.exe");
				var args = $"-sudopwd={password}";
				if (!string.IsNullOrEmpty(ports)) args += $" -ports={ports}";
				if (!string.IsNullOrEmpty(debugPassword)) args += $" -password={debugPassword}";
				if (manual) args += " -manual";
				logger.Trace($"run ssh://{username}@{url} => mono MonoToolsServerSetup.exe {args}");
				ssh.Ssh.RunCommand($"mono MonoToolsServerSetup.exe {args}");
			}
		}

		public async Task ServerUpgrade(string url, string ports, string password) {
			var server = new MonoClient.DebugClient(Debugger.Library.ApplicationTypes.DesktopApplication, "", "", false, Debugger.Library.Frameworks.Net4, ports, password);
			var session = await server.ConnectToServerAsync(new Uri(url).Host);
			session.UpgradeServer(ports, password);
		}
	}
}
