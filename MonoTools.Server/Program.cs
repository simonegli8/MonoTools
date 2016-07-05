using System;
using System.Linq;
using MonoTools.Library;

namespace MonoTools.Server {

	public class Program {

		public static void Main(string[] args) {

			Console.BackgroundColor = ConsoleColor.DarkBlue;
			Console.Clear();

			Console.WriteLine($"MonoTools Server {App.Version} © johnshope.com. Pass ? for help. Press Return to exit.");

			if (args.Any(a => a.Contains("help") || a.Contains("?"))) {
				Console.WriteLine(@"usage: mono MonoDebugger.exe [-ports=message-port,debugger-port,discovery-port] [-password=server-password]

The ports must be set to free ports, and to the same values
that have been set in the VisualStudio MonoTools options.
The password can be set to protect the server, when you start
the server as service, so it's password protected. You must also set
the password in the MonoTools VisualStudio options.");
			}


			var pipes = args.FirstOrDefault(a => a == "-console=")?.Substring("-console=".Length);

			if (pipes != null) ConsolePipes.StartClient(pipes);
			else {

				var ports = args.FirstOrDefault(a => a.StartsWith("-ports="))?.Substring("-ports=".Length);
				var password = args.FirstOrDefault(a => a.StartsWith("-password="))?.Substring("-password=".Length);
				var terminalTemplate = args.FirstOrDefault(a => a.StartsWith("-termtempl="))?.Substring("-termtempl=".Length);

				MonoLogger.Setup();

				using (var server = new MonoDebugServer(false, ports, password, terminalTemplate)) {
					server.StartAnnouncing();
					server.Start();
					server.WaitForExit();
				}
			}
		}
	}
}