using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace MonoTools.VisualStudio {

	public static class StatusBar {
		public static IVsStatusbar Bar;
		private static uint cookie = 0;
		private static string label = "Publish to debug server...";

		public static void InitProgress() {
			Bar.Progress(ref cookie, 1, "", 0, 1000);
		}

		public static void ClearProgress() {
			Bar.Progress(ref cookie, 0, "", 0, 1000);
		}

		public static void Initialize(IServiceProvider serviceProvider) {
			Bar = (IVsStatusbar)serviceProvider.GetService(typeof(SVsStatusbar));
		}

		public static void Progress(double d) {
			Bar.Progress(ref cookie, 1, label, (uint)((d * 1000.0) + 0.5), 1000);
		}

		public static void Text(string text) {
			Bar.SetText(text);
		}
	}
}
