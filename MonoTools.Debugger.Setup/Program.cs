using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MonoTools.Debugger.Setup {
	public class Program {

		public static void Main(string[] args) {

			if (args.Any(a => a.Contains("help") || a.Contains("?"))) {
				Console.WriteLine(@"usage: mono MonoDebugger.exe [-ports=message-port,debugger-port,discovery-port] [-password=server-password] [-manual] [-sudopwd=sudo-password]

If you omit -ports and -password, MonoDebugger.exe will prompt how to setup
the mono debug server.
with the -manual switch you can set that the debugger must be started
manually with the monodebugger command, as opposed to automatically
in your .xsession file.

The ports must be set to free ports, and to the same values
that have been set in the VisualStudio MonoTools options.
The password can be set to protect the server, when you start
the server as service, so it's password protected. You must also set
the password in the MonoTools VisualStudio options.");
			}

			var ports = args.FirstOrDefault(a => a.StartsWith("-ports="))?.Substring("-ports=".Length);
			var password = args.FirstOrDefault(a => a.StartsWith("-password="))?.Substring("-password=".Length);
			var sudopwd = args.FirstOrDefault(a => a.StartsWith("-sudopwd="))?.Substring("-sudopwd=".Length);
			var manual = args.Any(a => a == "-manual");
			var home = args.FirstOrDefault(a => a.StartsWith("-home="))?.Substring("-home=".Length) ?? Environment.GetFolderPath(Environment.SpecialFolder.Personal);

			Installer.Install(password, ports, manual ? Installer.Setups.Manual : Installer.Setups.Service, home, sudopwd);
			
		}
	}
}
