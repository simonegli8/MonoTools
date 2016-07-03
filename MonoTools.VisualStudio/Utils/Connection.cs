using System;
using Renci.SshNet;

namespace MonoTools.VisualStudio {

	public class Connection : IDisposable {

		public bool IsConnected { get; private set; }
		public SshClient Ssh { get; }
		public SftpClient Sftp { get; }

		public Connection(string host, string username, string password) {
			Ssh = new SshClient(host, username, password);
			Sftp = new SftpClient(host, username, password);
		}

		public void Connect() {
			if (IsConnected) {
				throw new Exception("Already connected!");
			}
			Ssh.Connect();
			Sftp.Connect();
			IsConnected = true;
		}

		public void Dispose() {
			Ssh.Disconnect();
			Ssh.Dispose();
			Sftp.Disconnect();
			Sftp.Dispose();
		}
	}
}