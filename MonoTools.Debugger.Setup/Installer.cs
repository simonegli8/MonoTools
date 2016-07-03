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
		public static string SudoPassword;
		public static bool Manual = false;
		public static bool Upgrade = false;
		public static bool NoSudo = false;
		public static Setups Setup = Setups.Service;
		public static uint Owner;

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
			b.Append($" \"-home={Home}\" -owner={Syscall.getuid()}");

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

		public static void Chown(string file, bool exe = false) {
			if (OS.IsMono && !OS.IsWindows) {
				Syscall.chown(file, Owner, Owner);
				if (exe || file.EndsWith(".exe") || file.EndsWith(".dll")) {
					Syscall.chmod(file, FilePermissions.S_IRWXU | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH | FilePermissions.S_IRGRP | FilePermissions.S_IROTH);
				} else {
					Syscall.chmod(file, FilePermissions.S_IRUSR | FilePermissions.S_IWUSR | FilePermissions.S_IRGRP | FilePermissions.S_IROTH);
				}
			}
		}

		public static void SaveResxFile(string resource, string destination) {
			destination = Path.Combine(LibPath, destination);
			using (var src = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
			using (var file = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.Write)) {
				src.CopyTo(file);
			}
			Chown(destination);
		}

		public static void Unzip(Stream zipStream, string outFolder, Func<string, string> filter = null) {
			var files = new List<string>();
			var notifier = NotifyProgress != null ? new Action<Silversite.Services.ProgressArgs>(args => NotifyProgress(args.Progress*0.6)) : null;
			Func<string, string> listfilter = file => {
				file = filter(file);
				if (file != null) files.Add(file);
				return file;
			};
			Silversite.Services.Zip.Extract(zipStream, outFolder, listfilter, notifier);
			foreach (var file in files) Chown(file);
		}


		public static void InstallZip() {
			if (!Directory.Exists(LibPath)) Directory.CreateDirectory(LibPath);
			var logPath = Path.Combine(LibPath, "Log");
			if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
			Chown(LibPath); Chown(logPath);

			using (var zip = Assembly.GetExecutingAssembly().GetManifestResourceStream("MonoTools.Debugger.Setup.Server.zip")) {
				Unzip(zip, LibPath);
			}

			var self = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			var libself = Path.Combine(LibPath, "MonoToolsServerSetup.exe");
			File.Copy(self, libself);
			Chown(libself);
		}

		public static void InstallScript() {
			const string script1 = "/usr/sbin/monotools";
			var exe1 = Path.Combine(LibPath, "MonoToolsServer.exe");
			File.WriteAllText(script1, $"exec mono {exe1}  $@");
			Chown(script1, true);
			const string script2 = "/usr/sbin/monotools-setup";
			var exe2 = Path.Combine(LibPath, "MonoToolsServerSetup.exe");
			File.WriteAllText(script2, $"exec mono {exe2}  $@");
			Chown(script2, true);
		}

		public static void InstallSession() {
			var xsession = "#! /bin/bash";
			var xsessionfile = Path.Combine(Home, ".xsession");
			var exe = Path.Combine(LibPath, "MonoToolsServer.exe");
			if (File.Exists(xsessionfile)) {
				xsession = File.ReadAllText(xsessionfile);
				var ex = new Regex($"^mono {exe} .*$", RegexOptions.Multiline);
				if (Upgrade && !ex.IsMatch(xsession)) return;
				ex.Replace(xsession, "");
			}
			xsession += $"\nmono {exe}";
			if (!string.IsNullOrEmpty(Ports)) xsession += $" -ports={Ports}";
			if (!string.IsNullOrEmpty(Password)) xsession += $" -password={Password}";

			File.WriteAllText(xsessionfile, xsession);
			Chown(xsessionfile, true);
		}

		public enum Setups { Service, Manual, Cancel };

		public static void Help() {
			if (Setup == Setups.Service) {
				Window.OpenDialog(@"You can now use this machine as
a debug server with MonoTools.
If you have set a password and custom ports, you need to set them in
the MonoTools options in VisualStudio also.

<!Ok>[    Ok    ]</!Ok>
");
			} else if (Setup == Setups.Manual) {
				Window.OpenDialog(@"To run the mono debug server type
monodebugger
in a console window.

<!Ok>[    Ok    ]</!Ok>
");
			}
		}

		public static void Configure(string[] args) {
			Ports = args.FirstOrDefault(a => a.StartsWith("-ports="))?.Substring("-ports=".Length);
			Password = args.FirstOrDefault(a => a.StartsWith("-password="))?.Substring("-password=".Length);
			SudoPassword = args.FirstOrDefault(a => a.StartsWith("-sudopwd="))?.Substring("-sudopwd=".Length);
			Upgrade = args.Any(a => a == "-upgrade");
			Manual = args.Any(a => a == "-manual") && !Upgrade;
			NoSudo = args.Any(a => a == "-nosudo") || Upgrade;
			Home = args.FirstOrDefault(a => a.StartsWith("-home="))?.Substring("-home=".Length) ?? Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			if (Manual) Setup = Setups.Manual;
		}

		public static void Install() {

			Sudo(SudoPassword);

			if (Password == null && Ports == null) {
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
				if (win.Service.Selected) Setup = Setups.Service;
				else if (win.Manual.Selected) Setup = Setups.Manual;
				else Setup = Setups.Cancel;
			}
			if (Setup == Setups.Cancel) return;
			InstallZip();
			InstallScript();
			if (Setup == Setups.Service) InstallSession();
			Help();
		}
	}
}