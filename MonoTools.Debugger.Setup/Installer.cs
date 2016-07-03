using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Mono.Unix.Native;

namespace MonoTools.Debugger.Setup {

	public static class Installer {

		public static string LibPath => "/usr/lib/monodebugger/";
		public static Action<double> NotifyProgress = n => { };
		public static string Password;
		public static string Ports;
		public static string Home;

		/// <summary>
		/// Runs the current program as superuser with the supplied root password, or asks for the root password in the console, when password is omitted. 
		/// </summary>
		/// <param name="password">Password or null.</param>
		public static void Sudo(string password = null) {
			if (OS.IsWindows || Syscall.getuid() == 0) return;
			var self = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			var b = new StringBuilder();
			if (!string.IsNullOrEmpty(password)) b.Append($"-S ");
			b.Append("mono ");
			b.Append(self);
			foreach (var a in Environment.GetCommandLineArgs()) {
				if (a.Contains(" ")) b.Append(" \"");
				else b.Append(" ");
				b.Append(a);
				if (a.Contains(" ")) b.Append("\"");
			}
			b.Append($" \"-home={Home}\"");

			var startInfo = new ProcessStartInfo("sudo", b.ToString()) {
				WindowStyle = ProcessWindowStyle.Hidden,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				RedirectStandardInput = !string.IsNullOrEmpty(password),
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Process.GetCurrentProcess().StartInfo.WorkingDirectory
			};
			var p = Process.Start(startInfo);
			if (!string.IsNullOrEmpty(password)) p.StandardInput.WriteLine(password);
			p.WaitForExit();

			System.Environment.Exit(0);
		}

		public static void SaveResxFile(string resource, string destination) {
			destination = Path.Combine(LibPath, destination);
			using (var src = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
			using (var file = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.Write)) {
				src.CopyTo(file);
			}
		}

		public static void Unzip(Stream zipStream, string outFolder, Func<string, string> filter = null) {
			var notifier = NotifyProgress != null ? new Action<Silversite.Services.ProgressArgs>(args => NotifyProgress(args.Progress*0.6)) : null;
			Silversite.Services.Zip.Extract(zipStream, outFolder, filter, notifier);
		}


		public static void InstallZip() {
			if (!Directory.Exists(LibPath)) Directory.CreateDirectory(LibPath);
			var logPath = Path.Combine(LibPath, "Log");
			if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);

			using (var zip = Assembly.GetExecutingAssembly().GetManifestResourceStream("MonoTools.Debugger.Setup.Server.zip")) {
				Unzip(zip, LibPath);
			}

			var self = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			File.Copy(self, Path.Combine(LibPath, "MonoDebuggerServerSetup.exe"));

			if (OS.IsMono) {
				foreach (var file in Directory.EnumerateFiles(LibPath)) {
					if (file.EndsWith(".exe") || file.EndsWith(".dll")) {
						Syscall.chmod(file, FilePermissions.S_IRWXU | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH | FilePermissions.S_IRGRP | FilePermissions.S_IROTH);
					} else {
						Syscall.chmod(file, FilePermissions.S_IRUSR | FilePermissions.S_IWUSR | FilePermissions.S_IRGRP | FilePermissions.S_IROTH);
					}
				}
			}

		}

		public static void InstallScript() {
			const string script1 = "/usr/sbin/monodebugger";
			var exe1 = Path.Combine(LibPath, "MonoDebuggerServer.exe");
			File.WriteAllText(script1, $"exec mono {exe1}  $@");
			if (OS.IsMono) Syscall.chmod(script1, FilePermissions.S_IRWXU | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH | FilePermissions.S_IRGRP | FilePermissions.S_IROTH);
			const string script2 = "/usr/sbin/monodebugger-setup";
			var exe2 = Path.Combine(LibPath, "MonoDebuggerServerSetup.exe");
			File.WriteAllText(script2, $"exec mono {exe2}  $@");
			if (OS.IsMono) Syscall.chmod(script2, FilePermissions.S_IRWXU | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH | FilePermissions.S_IRGRP | FilePermissions.S_IROTH);
		}

		public static void InstallSession() {
			var xsession = "#! /bin/bash";
			var xsessionfile = Path.Combine(Home, ".xsession");
			var exe = Path.Combine(LibPath, "MonoDebuggerServer.exe");
			if (File.Exists(xsessionfile)) {
				xsession = File.ReadAllText(xsessionfile);
				var ex = new Regex($"^mono {exe} .*$", RegexOptions.Multiline);
				ex.Replace(xsession, "");
			}
			xsession += $"\nmono {exe}";
			if (!string.IsNullOrEmpty(Ports)) xsession += $" -ports={Ports}";
			if (!string.IsNullOrEmpty(Password)) xsession += $" -password={Password}";

			File.WriteAllText(xsessionfile, xsession);
		}

		public enum Setups { Service, Manual, Cancel };

		public static void Help(Setups setup) {
			if (setup == Setups.Service) {
				Window.OpenDialog(@"You can now use this machine as
a debug server with MonoTools.
If you have set a password and custom ports, you need to set them in
the MonoTools options in VisualStudio also.

<!Ok>[    Ok    ]</!Ok>
");
			} else if (setup == Setups.Manual) {
				Window.OpenDialog(@"To run the mono debug server type
monodebugger
in a console window.

<!Ok>[    Ok    ]</!Ok>
");
			}
		}


		public static void Install(string password = null, string ports = null, Setups setup = Setups.Service, string home = null, string sudopwd = null) {
			Home = home;

			Sudo(sudopwd);

			if (password == null && ports == null) {
				var win = Window.OpenDialog(@"MonoTools Debugger Server Setup
===============================

|    Server Password:         <*Password>                           </*Password>
|    (Blank for no password) 

|    Server Ports:            <Ports>                           </Ports>

|    (You must specify three comma separated ports,
|    or leave blank for default.)

<!Service>[  Start as Service  ]</!Service>    <&Manual>[  Start manually  ]</&Manual>    <~Cancel>[  Cancel  ]</~Cancel> 
", new { Password = "", Ports = "" });
				Ports = win.Ports.Value;
				Password = win.Password.Value;
				if (win.Service.Selected) setup = Setups.Service;
				else if (win.Manual.Selected) setup = Setups.Manual;
				else setup = Setups.Cancel;
			} else {
				Ports = ports;
				Password = password;
			}
			if (setup == Setups.Cancel) return;
			InstallZip();
			InstallScript();
			if (setup == Setups.Service) InstallSession();
			Help(setup);
		}
	}
}