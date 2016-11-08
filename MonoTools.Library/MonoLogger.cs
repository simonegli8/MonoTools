using System;
using System.IO;
using System.Reflection;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;

namespace MonoTools.Library {
	public static class MonoLogger {
		public static string LoggerPath { get; private set; }

		public static void Setup() {

			var codeBase = Assembly.GetExecutingAssembly().CodeBase ?? Assembly.GetEntryAssembly().CodeBase;

			var basePath = new FileInfo(new Uri(codeBase).LocalPath).Directory.FullName;
			var logPath = Path.Combine(basePath, "Log");
			if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
			LoggerPath = Path.Combine(logPath, "MonoTools.Debugger.log");

			var config = new LoggingConfiguration();
			var target = new NLog.Targets.DebuggerTarget();
			target.Layout = new NLog.Layouts.SimpleLayout("${message}");
			config.AddTarget("file", target);
#if DEBUG
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, target));
#else
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, target));
#endif

			var fileTarget = new FileTarget { FileName = LoggerPath };
			config.AddTarget("file", fileTarget);
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, fileTarget));
			var console = new ColoredConsoleTarget();
			console.Layout = new NLog.Layouts.SimpleLayout("${message}");
			config.AddTarget("file", console);
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, console));

			LogManager.Configuration = config;
		}
	}
}