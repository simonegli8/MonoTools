using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Threading;

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
		public ClientSession Session;
		public CancellationTokenSource Cancel;

		public MonoProcess(ExecuteMessage msg, MonoDebugServer server, ClientSession session) {
			Message = msg; Server = server; Session = session; Cancel = new CancellationTokenSource();
		}

		public Process Start(string exe, Action<StringBuilder> arguments = null, Action<ProcessStartInfo> infos = null) {
			var args = new StringBuilder();
			var template = Server.TerminalTemplate;
			bool terminal = false;

			if (!Directory.Exists(Message.WorkingDirectory)) Directory.CreateDirectory(Message.WorkingDirectory);

			if (Message.ApplicationType != ApplicationTypes.ConsoleApplication) RedirectOutput = true;

			var codeBase = Assembly.GetExecutingAssembly().CodeBase ?? Assembly.GetEntryAssembly().CodeBase;

			var server = new Uri(codeBase).LocalPath;

			CreateWindow = false;
			if (!OS.IsWindows && !string.IsNullOrEmpty(Server.TerminalTemplate)) { // use TerminalTemplate to start process
				terminal = true;
				CreateWindow = true;
			} else if (OS.IsWindows && !RedirectOutput) {
				CreateWindow = true;
			} else if (!Server.IsLocal) {
				Console.BackgroundColor = ConsoleColor.Black;
				Console.Clear();
			}

			arguments?.Invoke(args);

			ProcessStartInfo procInfo = GetProcessStartInfo(exe);
			procInfo.Arguments = template == null ? args.ToString() : string.Format(template, args.ToString());
			infos?.Invoke(procInfo);

			process = new System.Diagnostics.Process();
			process.StartInfo = procInfo;
			process.EnableRaisingEvents = false;

			process.Start();

			if (terminal) Terminal.Open(Server.TerminalTemplate, process, Session, Cancel.Token);
			else ConsoleMirror.StartServer(process, Session, Cancel.Token);

			RaiseProcessStarted();

			return process;
		}

		public abstract Process Start();

		protected void RaiseProcessStarted() {
			EventHandler handler = ProcessStarted;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected string DebugArgs => Message.Debug ? $"--debugger-agent=address={IPAddress.Any}:{DebuggerPort},transport=dt_socket,server=y --debug=mdb-optimizations" : "";

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

		public static MonoProcess Start(ExecuteMessage msg, MonoDebugServer server, ClientSession session) {
			if (msg.ApplicationType == ApplicationTypes.WebApplication) return new MonoWebProcess(msg, server, session);
			return new MonoDesktopProcess(msg, server, session);
		}
	}
}