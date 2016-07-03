﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace MonoTools.Debugger.Library {

	public abstract class MonoProcess {
		public int DebuggerPort = 11000;
		public Process process;
		public bool RedirectOutput = false;
		public Action<string> Output = null;
		public event EventHandler ProcessStarted;
		internal abstract Process Start(string workingDirectory);

		protected void RaiseProcessStarted() {
			EventHandler handler = ProcessStarted;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		protected string GetProcessArgs(bool debug) {
			//IPAddress ip = GetLocalIp();
			IPAddress ip = IPAddress.Any;
			return debug ? $"--debugger-agent=address={ip}:{DebuggerPort},transport=dt_socket,server=y --debug=mdb-optimizations" : "";
		}

		protected ProcessStartInfo GetProcessStartInfo(string workingDirectory, string monoBin) {
			var procInfo = new ProcessStartInfo(monoBin) {
				WorkingDirectory = Path.GetFullPath(workingDirectory),
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

		internal static MonoProcess Start(ApplicationTypes type, string targetExe, Frameworks framework, string arguments, string url, bool debug) {
			if (type == ApplicationTypes.DesktopApplication)
				return new MonoDesktopProcess(targetExe, arguments, debug);
			if (type == ApplicationTypes.WebApplication)
				return new MonoWebProcess(framework, url, debug);

			throw new Exception("Unknown ApplicationType");
		}
	}
}