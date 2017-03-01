using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoTools.Library {

	public class OS {
		public static bool IsMono => Type.GetType("Mono.Runtime") != null;
		public static bool IsNetFX => !IsMono;
		public static bool IsWindows {
			get {
				var id = Environment.OSVersion.Platform;
				return id == PlatformID.Win32NT || id == PlatformID.Win32S || id == PlatformID.Win32Windows || id == PlatformID.Xbox || id == PlatformID.WinCE;
			}
		}

		public static bool IsInstalled(string exe) => Where(exe) != null;
		
		public static string Where(string exe) {

			string cmd = IsWindows ? "where.exe" : "which";
			var info = new System.Diagnostics.ProcessStartInfo(cmd) {
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				Arguments = exe
			};
			var p = System.Diagnostics.Process.Start(info);
			p.WaitForExit();
			while (!p.HasExited) System.Threading.Thread.Sleep(10);
			if (p.ExitCode != 0) return p.StandardOutput.ReadToEnd();
			return null;
		}
	}
}
