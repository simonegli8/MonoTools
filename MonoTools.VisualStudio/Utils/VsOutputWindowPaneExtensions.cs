using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace MonoTools.VisualStudio {

	public static class VsOutputWindowPaneExtensions {

		public static void Log(this IVsOutputWindowPane pane, string message) {
			pane.OutputString(message + "\r\n");
		}

		public static void Log(this IVsOutputWindowPane pane, VsLogSeverity severity, string project, string file,
			 string consoleMessage, int lineNumber = 0, int column = 0) {
			pane.Log(severity, project, file, consoleMessage, consoleMessage, lineNumber, column);
		}

		public static void Log(this IVsOutputWindowPane pane, VsLogSeverity severity, string project, string file, string consoleMessage, string taskMessage, int lineNumber = 0, int column = 0) {
			VSTASKPRIORITY priority;
			switch (severity) {
			case VsLogSeverity.Message:
				priority = VSTASKPRIORITY.TP_LOW;
				break;
			case VsLogSeverity.Warning:
				priority = VSTASKPRIORITY.TP_NORMAL;
				break;
			case VsLogSeverity.Error:
				priority = VSTASKPRIORITY.TP_HIGH;
				break;
			default:
				throw new Exception();
			}

			var pane2 = (IVsOutputWindowPane2)pane;
			pane2.OutputTaskItemStringEx2(consoleMessage + "\r\n", priority, VSTASKCATEGORY.CAT_BUILDCOMPILE, "FOO",
				 0, file, (uint)lineNumber, (uint)column, project, taskMessage, null);
		}
	}
}