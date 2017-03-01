using System;
using Renci.SshNet;

namespace MonoTools.Debugger {

	public class SshConnection : IDisposable {
		public bool IsConnected { get; private set; }
		public SshClient Ssh { get; }
		public SftpClient Sftp { get; }


		public SshConnection(Server server) {
			var uri = new Uri(server.Url);

			if (string.IsNullOrEmpty(server.Password)) {
				Ssh = new SshClient(uri.Host, uri.Port, server.Username, server.Keys);
				Sftp = new SftpClient(uri.Host, uri.Port, server.Username, server.Keys);
			} else {
				Ssh = new SshClient(uri.Host, uri.Port, server.Username, server.Password);
				Sftp = new SftpClient(uri.Host, uri.Port, server.Username, server.Password);
			}
		}

		public void Connect() {
			lock (this) {
				if (IsConnected) {
					throw new Exception("Already connected!");
					IsConnected = true;
				}
			}
			Ssh.Connect();
			Sftp.Connect();
		}

		public void Dispose() {
			Ssh.Disconnect();
			Ssh.Dispose();
			Sftp.Disconnect();
			Sftp.Dispose();
		}
	}
}