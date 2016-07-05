using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoTools.VisualStudio {

	public static class Output {

		public static IVsOutputWindowPane Pane {
			get {
				int hr;

				var outputWindow = (IVsOutputWindow)Package.GetGlobalService(typeof(SVsOutputWindow));
				if (outputWindow == null) return null;

				IVsOutputWindowPane pane;
				Guid guidDebugOutputPane = VSConstants.GUID_OutWindowDebugPane;
				hr = outputWindow.GetPane(ref guidDebugOutputPane, out pane);
				if (hr < 0) return null;
				return pane;
			}
		}
		public static void Text(string text) {
			try {
				Pane.OutputString(text);
			} catch { }
		}

		public static void Line(string text) {
			try {
				Pane.OutputString(text+"\r\n");
			} catch { }
		}

		public static void Clear() {
			try {
				Pane.Clear();
			} catch { }

		}
	}
}