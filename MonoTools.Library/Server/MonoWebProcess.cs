using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Reflection;
using System.IO;
using System.Text;
using NLog;

namespace MonoTools.Library {

	public class MonoWebProcess : MonoProcess {
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		public string Url => Message.Url;
		Frameworks Framework => Message.Framework;

		public MonoWebProcess(ExecuteMessage msg, MonoDebugServer server): base(msg, server) { RedirectOutput = true; }

		public static string SSLXpsArguments() {
			var a = Assembly.GetExecutingAssembly();
			var path = Path.GetTempPath();
			var cer = Path.Combine(path, "MonoTools.CARoot.cer");
			var pvk = Path.Combine(path, "MonoTools.CARoot.pvk");
			using (var r = a.GetManifestResourceStream("MonoTools.Library.Server.CARoot.cer"))
			using (var f = new FileStream(cer, FileMode.Create, FileAccess.Write, FileShare.None)) {
				r.CopyTo(f);
			}
			using (var r = a.GetManifestResourceStream("MonoTools.Library.Server.CARoot.pvk"))
			using (var f = new FileStream(pvk, FileMode.Create, FileAccess.Write, FileShare.None)) {
				r.CopyTo(f);
			}
			return $" --https --cert=\"{cer}\" --pkfile=\"{pvk}\" --pkpwd=0192iw0192IW";
		}

		public override Process Start() {
			return Start(MonoUtils.GetMonoXsp(Framework),
				args => {
					if (Url != null) {
						var uri = new Uri(Url);
						var port = uri.Port;
						var ssl = uri.Scheme.StartsWith("https");
						args.Append(" --port="); args.Append(port);
						if (ssl) args.Append(SSLXpsArguments());
					}
				},
				infos => {
					infos.CreateNoWindow = true;
					infos.UseShellExecute = false;
					infos.EnvironmentVariables["MONO_OPTIONS"] = GetProcessArgs();
				});
		}
	}
}