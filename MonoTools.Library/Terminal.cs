using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Threading;

namespace MonoTools.Library {

	public static class Terminal {

		public static bool IsInstalled(string exe) => OS.IsInstalled(exe);

		public static string Template(string template) {
			if (template == null) {
				if (IsInstalled("gnome-terminal")) template = "gnome";
				else if (IsInstalled("lxterminal")) template = "lxe";
				else if (IsInstalled("konsole")) template = "kde";
				else if (IsInstalled("xfce4-terminal")) template = "xfce4";
				else if (IsInstalled("xterm")) template = "xterm";
				else if (IsInstalled("tilda")) template = "tilda";
				else if (IsInstalled("terminaor")) template = "terminator";
				else if (IsInstalled("guake")) template = "guake";
				else if (IsInstalled("yakuake")) template = "yakuake";
				else if (IsInstalled("roxterm")) template = "roxxterm";
				else if (IsInstalled("eterm")) template = "eterm";
				else if (IsInstalled("rxvt")) template = "rcvt";
				else if (IsInstalled("wterm")) template = "wterm";
				else if (IsInstalled("lilyterm")) template = "lilyterm";
				else return null;
			}
			switch (template) {
			case "gnome": template = "gnome-terminal -x {0}"; break;
			case "lxe": template = "lxterminal -e {0}"; break;
			case "kde": template = "konsole -e {0}"; break;
			case "xfce4": template = "xfce4-terminal -x {0}"; break;
			case "xterm": template = "xterm -e \"{0}\""; break;
			case "tilda": template = "tilda -e \"{0}\""; break;
			case "terminaor": template = "terminator -e \"{0}\""; break;
			case "guake": template = "guake -e \"{0}\""; break;
			case "yakuake": template = "yakuake -e \"{0}\""; break;
			case "roxterm": template = "roxxterm -e \"{0}\""; break;
			case "eterm": template = "eterm -e \"{0}\""; break;
			case "rxvt": template = "rcvt -e \"{0}\""; break;
			case "wterm": template = "wterm -e \"{0}\""; break;
			case "lilyterm": template = "lilyterm -e \"{0}\""; break;
			default: break;
			}
			return template;
		}

		public static void Open(string template, string cmd) {

			if (template == null) throw new NotSupportedException("No compatible terminal installed.");

			var p = template.IndexOf(' ');
			string exe;
			if (p > 0) {
				exe = template.Substring(0, p);
				template = template.Substring(p+1);
			} else {
				exe = template;
				template = "{0}";
			}
			cmd = string.Format(template, cmd);

			Process.Start(exe, cmd);
		}

		public static void Open(string template, Process p, ClientSession session, CancellationToken cancel) {
			var dll = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			var monoserver = Path.Combine(Path.GetDirectoryName(dll), "MonoToolsServer.exe");
			var mirror = ConsoleMirror.StartTerminalServer(p, session, cancel);
			if (mirror != null) Open(template, $"mono {monoserver} -mirror={mirror.Pipes}");
		}
	}
}
