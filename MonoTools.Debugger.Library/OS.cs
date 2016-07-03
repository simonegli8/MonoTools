using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoTools.Debugger.Library {

	public class OS {
		public static bool IsMono => Type.GetType("Mono.Runtime") != null;
		public static bool IsNetFX => !IsMono;
		public static bool IsWindows {
			get {
				var id = Environment.OSVersion.Platform;
				return id == PlatformID.Win32NT || id == PlatformID.Win32S || id == PlatformID.Win32Windows || id == PlatformID.Xbox || id == PlatformID.WinCE;
			}
		}
	}
}
