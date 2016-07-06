using System;
using System.Diagnostics;
using System.Threading;

namespace MonoTools.Library {

	internal class MonoDesktopProcess : MonoProcess {

		public MonoDesktopProcess(ExecuteMessage msg, MonoDebugServer server, ClientSession session): base(msg, server, session) { RedirectOutput = msg.ApplicationType == ApplicationTypes.WindowsApplication; }

		public override Process Start() {
			return Start(MonoUtils.GetMonoPath(), args => {
				var da = DebugArgs;
				if (da != "") {
					if (args.Length > 0) args.Append(" ");
					args.Append(da);
				}
				if (args.Length > 0) args.Append(" ");
				args.Append("\"");
				args.Append(Message.Executable);
				args.Append("\"");
				if (!string.IsNullOrEmpty(Message.Arguments)) {
					if (args.Length > 0) args.Append(" ");
					args.Append(Message.Arguments);
				}
			});
		}
	}
}