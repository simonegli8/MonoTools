using System;
using Renci.SshNet;
using Renci.SshNet.Common;
using MonoProgram.Package.Debuggers;

namespace MonoTools.Debugger {

	public class SshConnection : IDisposable {
		public bool IsConnected { get; private set; }
		public SshClient Ssh { get; }
		public SftpClient Sftp { get; }

		ForwardedPort forwardLocal, forwardRemote;

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

			var forwardLocal = new ForwardedPortLocal("127.0.0.1", MonoEngine.MonoDebuggerPort, "127.0.0.1", MonoEngine.MonoDebuggerPort);
			var forwardRemote = new ForwardedPortRemote("127.0.0.1", MonoEngine.MonoDebuggerPort, "127.0.0.1", MonoEngine.MonoDebuggerPort);
			Ssh.AddForwardedPort(forwardLocal);
			Ssh.AddForwardedPort(forwardRemote);

			forwardLocal.Exception += (sender, e) => { throw e.Exception; };
			forwardRemote.Exception += (sender, e) => { throw e.Exception; };

			forwardLocal.Start();
			forwardRemote.Start();
		}

		public void Dispose() {
			forwardRemote.Stop();
			forwardLocal.Stop();
			Ssh.Disconnect();
			Ssh.Dispose();
			Sftp.Disconnect();
			Sftp.Dispose();
		}
	}
}