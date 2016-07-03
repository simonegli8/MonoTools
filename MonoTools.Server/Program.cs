using System;
using System.Linq;
using MonoTools.Debugger.Library;

namespace MonoTools.Debugger.Server {

	public class Program {

		public static void Main(string[] args) {

			Console.BackgroundColor = ConsoleColor.DarkBlue;
			Console.Clear();

			Console.WriteLine($"MonoDebugger {Application.Version}, © johnshope.com. Pass ? for help.");

			if (args.Any(a => a.Contains("help") || a.Contains("?"))) {
				Console.WriteLine(@"usage: mono MonoDebugger.exe [-ports=message-port,debugger-port,discovery-port] [-password=server-password]

The ports must be set to free ports, and to the same values
that have been set in the VisualStudio MonoTools options.
The password can be set to protect the server, when you start
the server as service, so it's password protected. You must also set
the password in the MonoTools VisualStudio options.");
			}

			var ports = args.FirstOrDefault(a => a.StartsWith("-ports="))?.Substring("-ports=".Length);
			var password = args.FirstOrDefault(a => a.StartsWith("-password="))?.Substring("-password=".Length);

			MonoLogger.Setup();

			using (var server = new MonoDebugServer(false, ports, password)) {
				server.StartAnnouncing();
				server.Start();

				server.WaitForExit();
			}
		}
	}
}