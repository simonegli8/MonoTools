using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoTools.Library;

namespace MonoTools.Server {

	public class Server {

		public static void HandleKey(ConsoleKeyInfo key) {
			if (key.KeyChar == '?') UI.Help();
			else if (key.KeyChar == 'i' || key.KeyChar == 'I') Installer.Install();
			else if (key.KeyChar == 'q' || key.KeyChar == 'Q' || key.Key == ConsoleKey.Escape) {
				debugServer.Stop();
			}
		}

		static MonoDebugServer debugServer;

		public static void Run(bool install, string pipes, string ports, string password, string terminalTemplate) {

			if (pipes != null) ConsoleMirror.StartClient(pipes);
			else {

				if (install) Installer.Install();

				UI.Clear();

				MonoLogger.Setup();

				using (debugServer = new MonoDebugServer(false, ports, password, terminalTemplate)) {
					debugServer.KeyPress += HandleKey;
					debugServer.StartKeyInput();
					debugServer.StartAnnouncing();
					debugServer.Start();
					debugServer.WaitForExit();
				}
			}

		}
	}
}
