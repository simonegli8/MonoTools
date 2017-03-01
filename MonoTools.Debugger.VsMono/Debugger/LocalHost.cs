using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace MonoTools.Debugger {

	public class LocalHost : Host {

		protected string OutputPath { get; private set; }
		protected Process Process { get; private set; }

		public LocalHost(Launcher launcher) : base(launcher) { OutputPath = launcher.Task.SourcePath.TrimEnd('/', '\\'); }

		public override string ScriptExtension => ".bat";

		int LinesCount(string s) {
			int n = 0;
			foreach (var c in s) {
				if (c == '\n') n++;
			}
			return n+1;
		}

		public override async Task<string> Run(string cmd) {
			if (string.IsNullOrEmpty(cmd)) throw new ArgumentNullException(nameof(cmd));

			// launch system process
			string args;
			if (cmd.StartsWith("\"")) args = cmd.Substring(cmd.IndexOf('"', 1));
			else args = cmd.Substring(cmd.IndexOf(' ')+1);

			var startInfo = new ProcessStartInfo(cmd, args) {
				WindowStyle = ProcessWindowStyle.Hidden,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Environment.CurrentDirectory
			};

			// get working directory from executable path
			Process = new Process();
			int n = 0, m = 0;
			var output = new StringBuilder();
			if (Progress != null) {
				Process.OutputDataReceived += (sender, data) => {
					if (data.Data != null) {
						if (Log != null) Log?.Invoke(data.Data);
						output.AppendLine(data.Data);
						n += LinesCount(data.Data);
					}
					//ScriptProgress?.Invoke(n);
				};
			}
			Process.StartInfo = startInfo;
			Process.Start();
			if (Progress != null) {
				Process.EnableRaisingEvents = true;
				Process.BeginOutputReadLine();
			}
			var task = System.Threading.Tasks.Task.Run(() => {
				while (!Process.HasExited) {
					if (Progress != null && n != m) {
						m = n; Progress?.Invoke(n);
					}
					Thread.Sleep(20);
				}
			});

			await task;

			// analyze results
			var results = ""; var errors = "";
			if (Process == null) return results;

			output.AppendLine(Process.StandardOutput.ReadToEnd());
			output.AppendLine(errors = Process.StandardError.ReadToEnd());

			return output.ToString().Trim() + "\n";
		}

		public override void ChangeDir(string path) => Environment.CurrentDirectory = path;
		public override string Map(string path) => Path.Combine(OutputPath, path);
		public override void SetEnvironment(IEnumerable<KeyValuePair<string, string>> variables) {
			foreach (var variable in variables) {
				Environment.SetEnvironmentVariable(variable.Key, variable.Value);
			}
		}
		public override void Kill() { Process.Kill(); }
		public override void UploadFolder(string sourcePath, string remotePath) { }
		public override void Dispose() { }
	}
}
