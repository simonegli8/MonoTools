using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoTools.Server {

	public class UI {

		public const bool IsWin = false;

		public static void Clear() {
			Console.BackgroundColor = ConsoleColor.DarkBlue;
			Console.Clear();
			Console.WriteLine($@"MonoTools Debug Server {App.Version} © johnshope.com.
Press ? for help. Press I to install server into xsession. Press Q to exit.");
		}

		public static void Help() {
			Window.OpenDialog(@"MonoTools Debug Server
======================

|    usage: mono monodebug.exe [-i]
|      [-ports=message-port,debugger-port,discovery-port]
|      [-password=server-password]
|      [-terminal=terminal-format-string]

|    The ports must be set to free ports, and to the same values
|    that have been set in the VisualStudio MonoTools options.
|    The password can be set to protect the server, when you start
|    the server as service, so it's password protected. You must also set
|    the password in the MonoTools VisualStudio options.
|    Use the -i option to install monodebug in your xsession.
|    Supply a terminal command format string, if mondebug is
|    unable to auto detect your terminal client.");

			Clear();
		}
	}
}