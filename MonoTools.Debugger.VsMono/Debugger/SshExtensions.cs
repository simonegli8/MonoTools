using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Renci.SshNet;

namespace MonoTools.Debugger {

	public static class SshExtensions {
		public static SshCommand BeginCommand(this SshClient ssh, string commandText, Action<string> output) {
			IAsyncResult asyncResult;
			return ssh.BeginCommand(commandText, output, out asyncResult);
		}

		public static SshCommand BeginCommand(this SshClient ssh, string commandText, Action<string> output, AsyncCallback callback) {
			IAsyncResult asyncResult;
			return ssh.BeginCommand(commandText, output, out asyncResult);
		}

		public static SshCommand BeginCommand(this SshClient ssh, string commandText, Action<string>output, out IAsyncResult asyncResult) {
			return ssh.BeginCommand(commandText, output, null, out asyncResult);
		}

		private static readonly Regex errorRegex = new Regex(@"(.*\..*)\((.*)\,(.*)\)\: (.*)");
		private static readonly Regex locationlessErrorRegex = new Regex(@"(.*\..*?)\:(.*)");

		private static SshCommand BeginCommand(this SshClient ssh, string commandText, Action<string> output, AsyncCallback callback, out IAsyncResult asyncResult) {
			var command = ssh.CreateCommand(commandText);
			var isRunning = true;
			asyncResult = command.BeginExecute(ar => {
				isRunning = false;
				callback?.Invoke(ar);
			});
			var asyncHandle = asyncResult;
			Task.Run(() => {
				using (var reader = new StreamReader(command.OutputStream)) {
					// This is so convoluted because strangely reader.ReadLine() will return null even when the program
					// is still running.  
					do {
						for (var line = reader.ReadLine(); line != null; line = reader.ReadLine()) {
							output?.Invoke(line);
						}
						Thread.Sleep(10);
					}
					while (isRunning);

					var s = command.EndExecute(asyncHandle);
					if (!string.IsNullOrEmpty(s)) {
						output?.Invoke(s);
					}
				}
			});
			return command;
		}

		public static Task<string> Run(this SshClient ssh, string commandText, Action<string> output) {
			var command = ssh.CreateCommand(commandText);
			var isRunning = true;
			var asyncHandle = command.BeginExecute(ar => { isRunning = false; });
			return Task.Run(() => {
				var sb = new StringBuilder();
				using (var reader = new StreamReader(command.OutputStream)) {
					// This is so convoluted because strangely reader.ReadLine() will return null even when the program
					// is still running.  
					do {
						for (var line = reader.ReadToEnd(); line != null; line = reader.ReadToEnd()) {
							sb.Append(line);
							output?.Invoke(line);
						}
						Thread.Sleep(10);
					}
					while (isRunning);

					var s = command.EndExecute(asyncHandle);
					if (!string.IsNullOrEmpty(s)) {
						sb.Append(s);
						output?.Invoke(s);
					}
					return sb.ToString();
				}
			});
		}

		public enum Severity { Message, Warning, Error }

		public static int Run(this SshClient ssh, string commandText, string project, Action<string> output = null, Action<Severity, string, string, string, string, int, int> error = null) {
			var command = ssh.CreateCommand(commandText);
			var asyncResult = command.BeginExecute(null);

			using (var reader = new StreamReader(command.OutputStream)) {
				var s = reader.ReadToEnd();
				var atWarnings = false;
				var atErrors = false;
				foreach (var line in s.Split('\n')) {
					if (line == "Warnings:")
						atWarnings = true;
					if (line == "Errors:")
						atErrors = true;

					var errorMatch = errorRegex.Match(line);
					var locationlessErrorMatch = locationlessErrorRegex.Match(line);
					var match = errorMatch.Success ? errorMatch : locationlessErrorMatch;
					if ((atWarnings || atErrors) && match.Success) {
						var file = match.Groups[1].Value.Trim();
						var lineNumber = 1;
						var column = 0;
						string message;
						if (errorMatch.Success) {
							lineNumber = int.Parse(errorMatch.Groups[2].Value);
							column = int.Parse(errorMatch.Groups[3].Value);
							message = errorMatch.Groups[4].Value.Trim();
						} else {
							message = locationlessErrorMatch.Groups[2].Value.Trim();
						}
					
						Severity severity = message.StartsWith("error") ? Severity.Error : message.StartsWith("warning") ? Severity.Warning : Severity.Message;
						/* VsLogSeverity severity = message.StartsWith("error") ? VsLogSeverity.Error : message.StartsWith("warning") ? VsLogSeverity.Warning : VsLogSeverity.Message;
						int firstSpace = message.IndexOf(' ');
						message = message.Substring(firstSpace + 1).TrimStart(' ', ':');
						outputPane.Log(severity, project, file, line, message, lineNumber - 1, column); */
						error?.Invoke(severity, project, file, line, message, lineNumber-1, column);
					} else {
						output?.Invoke(line);
					}
				}
			}

			command.EndExecute(asyncResult);
			return command.ExitStatus;
		}
	}
}