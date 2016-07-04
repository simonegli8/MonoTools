using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MonoTools.Library {

	public abstract class MonoProcess {
		public int DebuggerPort => Server.DebuggerPort;
		public Process process;
		public bool RedirectOutput = false;
		public Action<string> Output = null;
		public ExecuteMessage Message = null;
		public event EventHandler ProcessStarted;
		public bool CreateWindow = false;
		public MonoDebugServer Server;

		public MonoProcess(ExecuteMessage msg, MonoDebugServer server) { Message = msg; Server = server; }

		public Process Start(string exe, Action<StringBuilder> arguments = null, Action<ProcessStartInfo> infos = null) {
			var args = new StringBuilder();
			string template = null;

			if (!Directory.Exists(Message.WorkingDirectory)) Directory.CreateDirectory(Message.WorkingDirectory);

			if (Message.ApplicationType != ApplicationTypes.ConsoleApplication) RedirectOutput = true;

			CreateWindow = false;
			if (!OS.IsWindows && !RedirectOutput && !string.IsNullOrEmpty(Server.TerminalTemplate)) { // use TerminalTemplate to start process
				var p = Server.TerminalTemplate.IndexOf(' ');
				if (p > 0) {
					args.Append(exe);
					exe = Server.TerminalTemplate.Substring(0, p);
					template = Server.TerminalTemplate.Substring(p+1);
				} else {
					args.Append(exe);
					exe = Server.TerminalTemplate;
				}
				CreateWindow = true;
			} else if (OS.IsWindows && !RedirectOutput) {
				CreateWindow = true;
			} else {
				Console.BackgroundColor = ConsoleColor.Black;
				Console.Clear();
			}

			var pa = GetProcessArgs();
			if (pa != "") {
				if (args.Length > 0) args.Append(" ");
				args.Append(pa);
			}
			arguments?.Invoke(args);

			ProcessStartInfo procInfo = GetProcessStartInfo(exe);
			procInfo.Arguments = template == null ? args.ToString() : string.Format(template, args.ToString());
			infos?.Invoke(procInfo);

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

		public abstract Process Start();

		protected void RaiseProcessStarted() {
			EventHandler handler = ProcessStarted;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected string GetProcessArgs() {
			//IPAddress ip = GetLocalIp();
			IPAddress ip = IPAddress.Any;
			return Message.Debug ? $"--debugger-agent=address={ip}:{DebuggerPort},transport=dt_socket,server=y --debug=mdb-optimizations" : "";
		}

		protected ProcessStartInfo GetProcessStartInfo(string monoBin) {
			var procInfo = new ProcessStartInfo(monoBin) {
				WorkingDirectory = Path.GetFullPath(Message.WorkingDirectory),
				RedirectStandardError = RedirectOutput,
				RedirectStandardOutput = RedirectOutput,
				UseShellExecute = false,
				CreateNoWindow = !CreateWindow,
				WindowStyle = ProcessWindowStyle.Normal
			};
			return procInfo;
		}

		public static IPAddress GetLocalIp() {
			/* IPAddress[] adresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
			IPAddress adr = adresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
			return adr; */
			return IPAddress.Parse("127.0.0.1");
		}

		public static MonoProcess Start(ExecuteMessage msg, MonoDebugServer server) {
			if (msg.ApplicationType == ApplicationTypes.WebApplication) return new MonoWebProcess(msg, server);
			return new MonoDesktopProcess(msg, server);
		}
	}
}