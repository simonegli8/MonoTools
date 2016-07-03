using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;

namespace MonoTools.VisualStudio {

	public static class Options {

		static Properties properties;
		 
		public static void Initialize(IServiceProvider service) {
			var dte = (DTE)service.GetService(typeof(DTE));
			properties = dte.Properties["MonoTools", "General"];
		}

		public static string Ports => (string)properties.Item("MonoDebuggerPorts").Value;
		public static string Password => (string)properties.Item("MonoDebuggerPassword").Value;
		public static string MonoPath => (string)properties.Item("MonoInstallationPath").Value;
		public static UserSettings Settings => UserSettingsManager.Current;
	}
}
