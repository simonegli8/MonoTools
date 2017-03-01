using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Renci.SshNet;

namespace MonoTools.Debugger {

	public class RemoteSshHost: Host, IDisposable {

		SshConnection connection;
		SshClient Ssh => connection.Ssh;
		SftpClient Sftp => connection.Sftp;

		public RemoteSshHost(Launcher launcher) : base(launcher) { connection = new SshConnection(launcher.Server); }

		public override void ChangeDir(string path) => Ssh.RunCommand($"cd ~/{path}");

		static string lastCmd = null;
		public override async Task<string> Run(string cmd) {
			lastCmd = cmd;
			return await Ssh.Run(cmd, Log);
		}
		public override void Kill() {
			if (!string.IsNullOrEmpty(lastCmd)) Ssh.Run(@"kill $(ps auxww | grep '" + lastCmd + "' | awk '{print $2}')", Log);
			lastCmd = null;
		}
		public override void SetEnvironment(IEnumerable<KeyValuePair<string, string>> variables) {
			var cmd = new StringBuilder();
			foreach (var kv in variables) {
				Ssh.Run($"export {kv.Key}={kv.Value}", Log);
			}
		}
		public override string Map(string path) => $"~/{Regex.Replace(path, "(^~?/?|/$)", "")}";

		public override void UploadFolder(string sourcePath, string remotePath) {
			Log("Uploading program...");

			// Ensure target directory exists:
			var targetDirectories = remotePath.Split('/');
			foreach (var part in targetDirectories) {
				if (!Sftp.Exists(part)) Sftp.CreateDirectory(part);
				Sftp.ChangeDirectory(part);
			}
			int n = targetDirectories.Length;
			while (n > 0) Sftp.ChangeDirectory("..");

			n = 0;
			using (var timer = new Timer(state => { Progress(n++); Log("."); }, null, 0, 1000)) {
				Sftp.SynchronizeDirectories(sourcePath, remotePath, "*.*");
			}

			Log("Done");
		}

		public override void Dispose() { connection.Dispose(); }
	}
}
