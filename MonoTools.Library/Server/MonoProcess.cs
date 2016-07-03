using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace MonoTools.Library {

	public abstract class MonoProcess {
		public int DebuggerPort => Server.DebuggerPort;
		public Process process;
		public bool RedirectOutput = false;
		public Action<string> Output = null;
		public ExecuteMessage Message = null;
		public event EventHandler ProcessStarted;
		public MonoDebugServer Server;

		public MonoProcess(ExecuteMessage msg, MonoDebugServer server) { Message = msg; Server = server; }

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
				CreateNoWindow = false,
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
			if (msg.ApplicationType == ApplicationTypes.DesktopApplication)
				return new MonoDesktopProcess(msg, server);
			if (msg.ApplicationType == ApplicationTypes.WebApplication)
				return new MonoWebProcess(msg, server);

			throw new Exception("Unknown ApplicationType");
		}
	}
}