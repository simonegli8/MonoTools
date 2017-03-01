using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MonoTools.Debugger;
using MonoTools.VisualStudio.Views;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NLog;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Task = System.Threading.Tasks.Task;
using System.Windows;
using Microsoft.VisualStudio.Web.Application;

namespace MonoTools.VisualStudio {

	public enum ApplicationTypes { WebApplication, WindowsApplication, ConsoleApplication }

	public partial class Services {

		[System.Diagnostics.Conditional("DEBUG")]
		void Dump(Properties props) {
			foreach (Property p in props) {
				try {
					System.Diagnostics.Debugger.Log(1, "", $"Property: {p.Name}={p.Value.ToString()}\r\n");
				} catch { }
			}
		}

		[System.Diagnostics.Conditional("DEBUG")]
		void Dump(Project proj) {
			System.Diagnostics.Debugger.Log(1, "", "project.Properties");
			Dump(proj.Properties);
			System.Diagnostics.Debugger.Log(1, "", "proj.ConfigurationManager.ActiveConfiguration.Properties");
			Dump(proj.ConfigurationManager.ActiveConfiguration.Properties);
		}

		

		public async void Start() {
			try {
				BuildSolution();

				string target = GetStartupAssemblyPath();
				string exe = null;
				string outputDirectory = Path.GetDirectoryName(target);
				string url = null;
				string serverurl = null;
				string page = null;
				string workingDirectory = null;
				string arguments = null;
				Project startup = GetStartupProject();
				var props = startup.ConfigurationManager.ActiveConfiguration.Properties;

				//Dump(startup);

				bool isWeb = ((object[])startup.ExtenderNames).Any(x => x.ToString() == "WebApplication") || startup.Object is VsWebSite.VSWebSite;

				var isNet4 = true;
				var frameworkprop = props.Get("TargetFrameworkMoniker")
					?.Split(',')
					.Where(t => t.StartsWith("Version="))
					.Select(t => t.Substring("Version=".Length))
					.FirstOrDefault();
				isNet4 = (frameworkprop == null || string.Compare(frameworkprop, "v4.0") >= 0);

				var action = "0";
				action = props.Get("StartAction");
				exe = props.Get("StartProgram");
				arguments = props.Get("StartArguments");
				workingDirectory = props.Get("StartWorkingDirectory");
				url = props.Get("StartURL");
				page = props.Get("StartPage");

				if (isWeb) {
					var ext = (WAProjectExtender)startup.Extender["WebApplication"];
					action = ext.DebugStartAction.ToString();
					outputDirectory = new Uri(ext.OpenedURL).LocalPath;
					serverurl = ext.BrowseURL ?? ext.NonSecureUrl ?? ext.SecureUrl ?? ext.IISUrl ?? "http://127.0.0.1:9000";
					var uri = new Uri(serverurl);
					var port = uri.Port;
					if (ext.UseIIS) { // when running IIS, use random xsp port
						port = 15000 + new Random(target.GetHashCode()).Next(5000);
						uri = new Uri($"{uri.Scheme}://{uri.Host}:{port}");
						serverurl = uri.AbsoluteUri;
					}
					var ssl = uri.Scheme.StartsWith("https");

					var task = new StartWebTask() {
						Framework = isNet4 ? Frameworks.Net4 : Frameworks.Net2,
						LogLevels = "All",
						NetFXBuild = false,
						SourcePath = outputDirectory,
						Ssl = ssl,
						XspPort = port
					};

					if (action == "2") {
						task.StartProgram = new StartProgramTask() {
							Program = ext.StartExternalProgram,
							Arguments = ext.StartCmdLineArguments,
							WorkingDir = ext.StartWorkingDirectory,
						};
					} else if (action == "3") {
						task.OpenUrl = ext.StartExternalUrl;
						return;
					} else if (action == "0") {
						task.OpenUrl = ext.CurrentDebugUrl;
					} else if (action == "1") {
						task.OpenUrl = ext.StartPageUrl;
					}

					Server.Default.Start(task);
				} else {
					if (action == "0" || action == "2") {
						exe = target;
					}

					var task = new StartProgramTask() {
						Program = exe,
						Arguments = arguments,
						WorkingDir = workingDirectory
					};

					Server.Default.Start(task);

					if (action == "2") System.Diagnostics.Process.Start(url);
				}
			} catch (Exception ex) {
				logger.Error(ex);
				MessageBox.Show(ex.Message, "MonoTools.Debugger", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public async void StartDebug() {
			try {
				var startup = GetStartupProject();
				bool isWeb = ((object[])startup.ExtenderNames).Any(x => x.ToString() == "WebApplication") || startup.Object is VsWebSite.VSWebSite;
				string host = null;
				if (isWeb) {
					var ext = (WAProjectExtender)startup.Extender["WebApplication"];
					var serverurl = ext.BrowseURL ?? ext.NonSecureUrl ?? ext.SecureUrl ?? ext.IISUrl ?? "http://127.0.0.1:9000";
					var uri = new Uri(serverurl);
					if (uri.Host == "*" || uri.Host == "") host = "*";
					else {
						host = uri.Host;
						var localips = Dns.GetHostAddresses(host).Concat(Dns.GetHostAddresses(Environment.MachineName));
						var local = host == "localhost" || string.Compare(host, Environment.MachineName, true) == 0 || 
							Dns.GetHostAddresses(host).Any(a => a.IsIPv6LinkLocal || a == IPAddress.Parse("127.0.0.1") || localips.Any(b => a == b));
						if (local) host = null;
					}
				} else {
					var props = startup.ConfigurationManager.ActiveConfiguration.Properties;
					try {
						if (props.Get("RemoteDebugEnabled")?.ToLower()  == "true") {
							host = props.Get("RemoteDebugMachine");
						}
					} catch { }
				}
				StartDebug(host);
			} catch (Exception ex) {
				logger.Error(ex);
				MessageBox.Show(ex.Message, "MonoTools.Debugger", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public async void StartDebug(string host) {
			try {

				BuildSolution();

				if (string.IsNullOrEmpty(host)) await AttachDebugger(host, true);
				else await AttachDebugger(host, false);
			} catch (Exception ex) {
				logger.Error(ex);
				MessageBox.Show(ex.Message, "MonoTools.Debugger", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		Task consoleTask;

		public async Task AttachDebugger(string host, bool local = false) {
			string target = GetStartupAssemblyPath();
			string exe = null, debug = null;
			string outputDirectory = Path.GetDirectoryName(target);
			string url = null;
			string serverurl = null;
			string page = null;
			string workingDirectory = null;
			string arguments = null;
			Project startup = GetStartupProject();
			var props = startup.ConfigurationManager.ActiveConfiguration.Properties;

			//Dump(startup);
			
			bool isWeb = ((object[])startup.ExtenderNames).Any(x => x.ToString() == "WebApplication") || startup.Object is VsWebSite.VSWebSite;

			var isNet4 = true;
			var frameworkprop = props.Get("TargetFrameworkMoniker")
				?.Split(',')
				.Where(t => t.StartsWith("Version="))
				.Select(t => t.Substring("Version=".Length))
				.FirstOrDefault();
			isNet4 = (frameworkprop == null || string.Compare(frameworkprop, "v4.0") >= 0);
			Frameworks framework = isNet4 ? Frameworks.Net4 : Frameworks.Net2;

			var action = "0";
			action = props.Get("StartAction");
			exe = props.Get("StartProgram");
			arguments = props.Get("StartArguments");
			workingDirectory = props.Get("StartWorkingDirectory");
			url = props.Get("StartURL");
			page = props.Get("StartPage");

			if (isWeb) {
				outputDirectory = Path.GetDirectoryName(outputDirectory);
				var ext = (WAProjectExtender)startup.Extender["WebApplication"];
				action = ext.DebugStartAction.ToString();
				serverurl = ext.BrowseURL ?? ext.NonSecureUrl ?? ext.SecureUrl ?? ext.IISUrl ?? "http://127.0.0.1:9000";
				var uri = new Uri(serverurl);
				var port = uri.Port;
				if (ext.UseIIS) { // when running IIS, use random xsp port
					port = 15000 + new Random(target.GetHashCode()).Next(5000);
					uri = new Uri($"{uri.Scheme}://{uri.Host}:{port}");
					serverurl = uri.AbsoluteUri;
				}

				var task = new StartWebTask() {
					Framework = framework, LogLevels = "All", NetFXBuild = false, OpenUrl = serverurl, Ssl = uri.Scheme.StartsWith("https"), SourcePath = outputDirectory, XspPort = port
				};

				if (action == "2") {
					task.StartProgram = new StartProgramTask() {
						Program = ext.StartExternalProgram, Arguments = ext.StartCmdLineArguments, WorkingDir = ext.StartWorkingDirectory
					};
				} else if (action == "3") {
					task.OpenUrl = ext.StartExternalUrl;
				} else if (action == "0") {
					task.OpenUrl = ext.CurrentDebugUrl;
				} else if (action == "1") {
					task.OpenUrl = ext.StartPageUrl;
				}

				await StartDebugger(Server.Default, task);
			} else {

				var outputType = startup.Properties.Get("OutputType"); // output type (0: windows app, 1: console app, 2: class lib)
				if (outputType == "2") throw new InvalidOperationException("Cannot start a class library.");

				var appType = outputType == "0" ? ApplicationTypes.WindowsApplication : ApplicationTypes.ConsoleApplication;

				var task = new StartProgramTask() {
					Framework = framework, NetFXBuild = false, Program = target, Arguments = arguments, SourcePath = outputDirectory, WorkingDir = workingDirectory
				};

				if (action == "2") System.Diagnostics.Process.Start(url);
				else if (action == "1") {
					var run = new ProcessStartInfo(exe, arguments) {
						WorkingDirectory = workingDirectory,
						UseShellExecute = false,
						CreateNoWindow = false
					};
					System.Diagnostics.Process.Start(run);
				}
			}
		}

		public async Task StartDebugger(Server server, StartTask task) {

			// start debugger
			IntPtr pInfo = GetDebugInfo($"{host}:{client.DebuggerPort}", Path.GetFileName(targetExe), outputDir);
			var sp = new ServiceProvider((IServiceProvider)dte);
			try {
				var dbg = (IVsDebugger)sp.GetService(typeof(SVsShellDebugger));
				int hr = dbg.LaunchDebugTargets(1, pInfo);
				Marshal.ThrowExceptionForHR(hr);

			} catch (Exception ex) {
				logger.Error(ex);
				string msg;
				var sh = (IVsUIShell)sp.GetService(typeof(SVsUIShell));
				sh.GetErrorInfo(out msg);

				if (!string.IsNullOrWhiteSpace(msg)) logger.Error(msg);

				throw;
			} finally {
				if (pInfo != IntPtr.Zero) Marshal.FreeCoTaskMem(pInfo);
			}
		}

		private IntPtr GetDebugInfo(string args, string targetExe, string outputDirectory) {
			var info = new VsDebugTargetInfo();
			info.cbSize = (uint)Marshal.SizeOf(info);
			info.dlo = DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;

			info.bstrExe = Path.Combine(outputDirectory, targetExe);
			info.bstrCurDir = outputDirectory;
			info.bstrArg = args; // no command line parameters
			info.bstrRemoteMachine = null; // debug locally
			info.grfLaunch = (uint)__VSDBGLAUNCHFLAGS.DBGLAUNCH_StopDebuggingOnEnd;
			info.fSendStdoutToOutputWindow = 0;
			info.clsidCustom = AD7Guids.EngineGuid;
			info.grfLaunch = 0;

			IntPtr pInfo = Marshal.AllocCoTaskMem((int)info.cbSize);
			Marshal.StructureToPtr(info, pInfo, false);
			return pInfo;
		}

		public void OpenLogFile() {
			if (File.Exists(MonoLogger.LoggerPath)) {
				System.Diagnostics.Process.Start(MonoLogger.LoggerPath);
			}
		}

	}
}