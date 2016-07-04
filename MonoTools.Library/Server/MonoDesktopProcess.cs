﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MonoTools.Library {

	internal class MonoDesktopProcess : MonoProcess {

		public MonoDesktopProcess(ExecuteMessage msg, MonoDebugServer server): base(msg, server) { RedirectOutput = msg.ApplicationType == ApplicationTypes.WindowsApplication; }

		public override Process Start() {
			return Start(MonoUtils.GetMonoPath(), args => {
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