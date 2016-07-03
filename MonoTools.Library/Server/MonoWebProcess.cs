using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Reflection;
using System.IO;
using System.Text;
using NLog;

namespace MonoTools.Library {

	public class MonoWebProcess : MonoProcess {
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		public string Url => Message.Url;
		Frameworks Framework => Message.Framework;

		public MonoWebProcess(ExecuteMessage msg, MonoDebugServer server): base(msg, server) { RedirectOutput = true; }

		public static string SSLXpsArguments() {
			var a = Assembly.GetExecutingAssembly();
			var path = Path.GetTempPath();
			var cer = Path.Combine(path, "MonoTools.CARoot.cer");
			var pvk = Path.Combine(path, "MonoTools.CARoot.pvk");
			using (var r = a.GetManifestResourceStream("MonoTools.Library.Server.CARoot.cer"))
			using (var f = new FileStream(cer, FileMode.Create, FileAccess.Write, FileShare.None)) {
				r.CopyTo(f);
			}
			using (var r = a.GetManifestResourceStream("MonoTools.Library.Server.CARoot.pvk"))
			using (var f = new FileStream(pvk, FileMode.Create, FileAccess.Write, FileShare.None)) {
				r.CopyTo(f);
			}
			return $" --https --cert=\"{cer}\" --pkfile=\"{pvk}\" --pkpwd=0192iw0192IW";
		}

		public override Process Start() {
			var monoBin = MonoUtils.GetMonoXsp(Framework);

			var monoOptions = GetProcessArgs();
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
			ProcessStartInfo procInfo = GetProcessStartInfo(monoBin);

			procInfo.CreateNoWindow = true;
			procInfo.UseShellExecute = false;
			procInfo.EnvironmentVariables["MONO_OPTIONS"] = monoOptions;
			if (Url != null) {
				var uri = new Uri(Url);
				var port = uri.Port;
				var ssl = uri.Scheme.StartsWith("https");
				args.Append(" --port="); args.Append(port);
				if (ssl) args.Append(SSLXpsArguments());
			}
			procInfo.Arguments = template == null ? args.ToString() : string.Format(template, args.ToString());

			process = new System.Diagnostics.Process();
			process.StartInfo = procInfo;
			process.EnableRaisingEvents = true;
			if (RedirectOutput) {
				process.ErrorDataReceived += (sender, data) => Output(data.Data + "\r\n");
				process.OutputDataReceived += (sender, data) => Output(data.Data + "\r\n");
			}
			process.Start();
			if (RedirectOutput) process.BeginOutputReadLine();

			RaiseProcessStarted();

			return process;
		}
	}
}