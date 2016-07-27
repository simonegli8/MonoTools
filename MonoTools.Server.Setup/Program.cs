using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.IO.Pipes;

namespace MonoTools.Server.Setup {
	public class Program {

		public static void Main(string[] args) {

			ResourceAssemblyLoader.Init();

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

			Installer.Configure(args);
			Installer.Install();
		}
	}
}
