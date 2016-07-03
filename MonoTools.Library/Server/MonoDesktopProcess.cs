using System.Diagnostics;
using System.IO;
using System.Text;

namespace MonoTools.Library {

	internal class MonoDesktopProcess : MonoProcess {

		public MonoDesktopProcess(ExecuteMessage msg, MonoDebugServer server): base(msg, server) { }

		public override Process Start() {
			string monoBin = MonoUtils.GetMonoPath();
			var args = new StringBuilder();
			string template = null;

			if (!Directory.Exists(Message.WorkingDirectory)) Directory.CreateDirectory(Message.WorkingDirectory);

			if (!OS.IsWindows && !RedirectOutput && !string.IsNullOrEmpty(Server.TerminalTemplate)) { // use TerminalTemplate to start process
				var p = Server.TerminalTemplate.IndexOf(' ');
				if (p > 0) {
					monoBin = Server.TerminalTemplate.Substring(0, p);
					template = Server.TerminalTemplate.Substring(p+1);
				} else {
					args.Append(monoBin);
					monoBin = Server.TerminalTemplate;
				}
			}

			var pa = GetProcessArgs();
			if (pa != "") {
				if (args.Length > 0) args.Append(" ");
				args.Append(pa);
			}
			if (args.Length > 0) args.Append(" ");
			args.Append("\"");
			args.Append(Message.Executable);
			args.Append("\"");
			if (!string.IsNullOrEmpty(Message.Arguments)) {
				if (args.Length > 0) args.Append(" ");
				args.Append(Message.Arguments);
			}
			ProcessStartInfo procInfo = GetProcessStartInfo(monoBin);
			procInfo.Arguments = template == null ? args.ToString() : string.Format(template, args.ToString());

			process = new System.Diagnostics.Process();
			process.StartInfo = procInfo;
			process.EnableRaisingEvents = true;
			if (RedirectOutput) {
				process.ErrorDataReceived += (sender, data) => Output(data.Data + "\r\n");
				process.OutputDataReceived += (sender, data) => Output(data.Data + "\r\n");
				process.BeginOutputReadLine();
			} else {
				procInfo.UseShellExecute = true;
			}

			process.Start();

			RaiseProcessStarted();
			return process;
		}
	}
}