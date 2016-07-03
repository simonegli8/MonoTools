using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace MonoTools.VisualStudio {

	public static class StatusBarProgress {
		public static IVsStatusbar Bar;
		private static uint cookie = 0;
		private static string label = "Publish to debug server...";

		public static void Clear() {
			Bar.Progress(ref cookie, 0, "", 0, 0);
		}

		public static void Init() {
			Bar.Progress(ref cookie, 1, "", 0, 0);
		}

		public static void Initialize(IServiceProvider serviceProvider) {
			Bar = (IVsStatusbar)serviceProvider.GetService(typeof(SVsStatusbar));
		}

		public static void Progress(double d) {
			Bar.Progress(ref cookie, 1, label, (uint)((d * 1000.0) + 0.5), 0x3e8);
		}
	}
}
