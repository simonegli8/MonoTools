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

namespace MonoTools.Server {

	public static class Installer {

		public static string LibPath => "/usr/lib/monodebugger/";
		public const string ExeName = "MonoDebugger.exe";
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
		public static string TerminalTemplate;

		/// <summary>
		/// Runs the current program as superuser with the supplied root password, or asks for the root password in the console, when password is omitted. 
		/// </summary>
		/// <param name="password">Password or null.</param>
		public static void Sudo(string password = null) {
			if (OS.Runtime.IsWindows || Syscall.getuid() == 0) return;
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
			if (OS.Runtime.IsMono && !OS.Runtime.IsWindows) {
				Syscall.chown(file, Owner, Owner);
				if (exe || file.EndsWith(".exe") || file.EndsWith(".dll")) {
					Syscall.chmod(file, FilePermissions.S_IRWXU | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH | FilePermissions.S_IRGRP | FilePermissions.S_IROTH);
				} else {
					Syscall.chmod(file, FilePermissions.S_IRUSR | FilePermissions.S_IWUSR | FilePermissions.S_IRGRP | FilePermissions.S_IROTH);
				}
			}
		}


		public static void InstallSelf() {
			if (!Directory.Exists(LibPath)) Directory.CreateDirectory(LibPath);
			var logPath = Path.Combine(LibPath, "Log");
			if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
			Chown(LibPath); Chown(logPath);

			var self = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			var libself = Path.Combine(LibPath, ExeName);
			File.Copy(self, libself, true);
			Chown(libself);
		}

		public static void InstallScript() {
			const string script = "/usr/bin/monodebug";
			var exe = Path.Combine(LibPath, ExeName);
			File.WriteAllText(script, $"exec mono {exe} $@");
			Chown(script, true);
		}

		public static void InstallSession() {
			var xsession = "#! /bin/bash";
			var xsessionfile = Path.Combine(Home, ".xsession");
			var exe = "monodebug";
			if (File.Exists(xsessionfile)) {
				xsession = File.ReadAllText(xsessionfile);
				var ex = new Regex($"^{exe} .*$", RegexOptions.Multiline);
				if (Upgrade && !ex.IsMatch(xsession)) return;
				ex.Replace(xsession, "");
			}
			xsession += $"\n{exe}";
			if (!string.IsNullOrEmpty(Ports)) xsession += $" -ports={Ports}";
			if (!string.IsNullOrEmpty(Password)) xsession += $" -password={Password}";
			if (!string.IsNullOrEmpty(TerminalTemplate)) xsession += $" -termtempl={TerminalTemplate}";

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
			TerminalTemplate = args.FirstOrDefault(a => a.StartsWith("-termtempl="))?.Substring("-termtempl=".Length);
			switch (TerminalTemplate) {
			case "gnome": TerminalTemplate = "gnome-terminal -e {0}"; break;
			case "xfce4": TerminalTemplate = "xfce4-terminal -e {0}"; break;
			case "kde": TerminalTemplate = "kde-terminal -e {0}"; break;
			default: break;
			}
			if (Manual) Setup = Setups.Manual;
		}

		public static void Install() {

			Sudo(SudoPassword);

			if (Password == null && Ports == null) {
				var win = Window.OpenDialog(@"MonoDebugger Setup
=======================




|    Server Password:         <*Password>                           </*Password>
|    (Blank for no password) 

|    Server Ports:            <Ports>                           </Ports>

|    (You must specify three comma separated ports,
|    or leave blank for default.)



<!Service>[  Start as Service  ]</!Service>      <&Manual>[  Start manually  ]</&Manual>      <~Cancel>[  Cancel  ]</~Cancel> 
", new { Password = "", Ports = "" });
				Ports = win.Ports.Value;
				Password = win.Password.Value;
				if (win.Service.Selected) Setup = Setups.Service;
				else if (win.Manual.Selected) Setup = Setups.Manual;
				else Setup = Setups.Cancel;
			}
			if (Setup == Setups.Cancel) return;
			InstallSelf();
			InstallScript();
			if (Setup == Setups.Service) InstallSession();
			Help();
		}
	}
}