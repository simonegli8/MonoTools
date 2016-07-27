using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Unix.Native;

namespace MonoTools.Server.OS {


	public enum Platforms { Windows, Linux, Mac, Solaris, BSD, FreeBSD, Unix, Fedora, Debian, Ubuntu, CentOS, RedHat, Suse, Mandriva, ArchLinux, Other };

	public enum Runtimes { NetFX, Mono, CoreCLR, Other }

	public static class Runtime {

		public static bool IsMono => Type.GetType("Mono.Runtime") != null;
		public static bool IsNetFX => !IsMono;
		public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT ||
			Environment.OSVersion.Platform == PlatformID.Win32S ||
			Environment.OSVersion.Platform == PlatformID.Win32Windows;
		public static bool IsLinux => Platform == Platforms.Linux;
		public static bool IsMac => Platform == Platforms.Mac;
		public static bool IsSolaris => Platform == Platforms.Solaris;
		public static bool IsBsd => Platform == Platforms.BSD;
		public static bool IsUbuntu => Platform == Platforms.Ubuntu;
		public static bool IsFedora => Platform == Platforms.Fedora;
		public static bool IsDebian => Platform == Platforms.Debian;
		public static bool IsFreeBSD => Platform == Platforms.FreeBSD;
		public static bool IsRedHat => Platform == Platforms.RedHat;
		public static bool IsCentOS => Platform == Platforms.CentOS;
		public static bool IsArchLinux => Platform == Platforms.ArchLinux;
		public static bool IsMandriva => Platform == Platforms.Mandriva;
		public static bool IsSuse => Platform == Platforms.Suse;
		public static bool IsUnix => Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;

		public static Runtimes CLR => IsMono ? Runtimes.Mono : Runtimes.NetFX;

		static string ossimplename = null;
		//TODO Linux & Unix flavors
		public static Platforms Platform {
			get {
				switch (Environment.OSVersion.Platform) {
				case PlatformID.Unix:
					// Well, there are chances MacOSX is reported as Unix instead of MacOSX.
					// Instead of platform check, we'll do a feature checks (Mac specific root folders)
					if (System.IO.Directory.Exists("/Applications")
						& System.IO.Directory.Exists("/System")
						& System.IO.Directory.Exists("/Users")
						& System.IO.Directory.Exists("/Volumes"))
						return Platforms.Mac;
					else {
						var name = ossimplename ?? (ossimplename = Runtime.ExecuteScript(@"#!/bin/sh
							# Determine OS platform
							UNAME=$(uname | tr ""[:upper:]"" ""[:lower:]"")
							# If Linux, try to determine specific distribution
							if [""$UNAME"" == ""linux""]; then
								# If available, use LSB to identify distribution
								if [-f /etc/lsb-release -o -d /etc/lsb-release.d]; then
									export DISTRO=$(lsb_release -i | cut -d: -f2 | sed s/'^\t'//)
								elif [-f /etc/os-release]; then
									# Otherwise, if available use os-release
									export DISTRO=$(cat /etc/os-release | sed -n 's/^NAME=\(.*\)$/\1/p')
								else
									# otherwise use release file
									export DISTRO =$(ls -d /etc/[A-Za-z]*[_-][rv]e[lr]* | grep -v ""lsb"" | grep -v ""os-release"" | cut -d'/' -f3 | cut -d'-' -f1 | cut -d'_' -f1)
								fi
							fi
							# For everything else (or if above failed), just use generic identifier
							[""$DISTRO"" == """"] && export DISTRO=$UNAME
							unset UNAME
							echo $DISTRO
							")
							.ToLower());
						if (name.Contains("ubuntu")) return Platforms.Ubuntu;
						else if (name.Contains("fedora")) return Platforms.Fedora;
						else if (name.Contains("debian")) return Platforms.Debian;
						else if (name.Contains("centos")) return Platforms.CentOS;
						else if (name.Contains("redhat")) return Platforms.RedHat;
						else if (name.Contains("freebsd")) return Platforms.FreeBSD;
						else if (name.Contains("bsd")) return Platforms.BSD;
						else if (name.Contains("archlinux")) return Platforms.ArchLinux;
						else if (name.Contains("mandriva")) return Platforms.Mandriva;
						else if (name.IndexOf("suse", StringComparison.OrdinalIgnoreCase) == 0) return Platforms.Suse;
						else if (name.Contains("solaris")) return Platforms.Solaris;
						return Platforms.Linux;
					}
				case PlatformID.MacOSX:
					return Platforms.Mac;

				default:
					return Platforms.Windows;
				}
			}
		}

		public static string ExecuteScript(string scriptcontent, string outputFile = null, bool append = false) {
			if (!IsWindows) {
				var script = Path.Combine("/tmp/" + Guid.NewGuid() + (IsWindows ? ".bat" : ".sh"));
				File.WriteAllText(script, scriptcontent.Replace("\r\n", "\n")); // change to unix line endings
				Syscall.chmod(script, FilePermissions.S_IXUSR | FilePermissions.S_IWUSR | FilePermissions.S_IRUSR);
				var res = Execute(script, "", outputFile, append);
				File.Delete(script);
				return res;
			} else {
				return "";
			}
		}

		public static Action<int> ScriptProgress = null;

		public static int LinesCount(string s) {
			int n = 0;
			foreach (var c in s) {
				if (c == '\n') n++;
			}
			return n+1;
		}


		public static string Execute(string filePath, string args, string outputFile = null, bool append = false) {
			if (Runtime.IsWindows) return "";
			if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

			if (outputFile != null) {
				var dir = Path.GetDirectoryName(outputFile);
				if (!string.IsNullOrEmpty(dir) && dir != "/" && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
			}

			// launch system process
			var startInfo = new ProcessStartInfo(filePath, args) {
				WindowStyle = ProcessWindowStyle.Hidden,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Path.GetDirectoryName(filePath)
			};

			// get working directory from executable path
			var proc = new Process();
			int n = 0, m = 0;
			var output = new StringBuilder();
			if (ScriptProgress != null) {
				proc.OutputDataReceived += (sender, data) => {
					if (data.Data != null) {
						output.AppendLine(data.Data);
						if (append && outputFile != null) {
							File.AppendAllText(outputFile, data.Data+"\n");
							n += LinesCount(data.Data);
						}
						ScriptProgress(n);
					}
				};
			}
			proc.StartInfo = startInfo;
			proc.Start();
			if (ScriptProgress != null) {
				proc.EnableRaisingEvents = true;
				proc.BeginOutputReadLine();
			}
			while (!proc.HasExited) {
				if (ScriptProgress != null && n != m) {
					m = n; ScriptProgress(n);
					if (UI.IsWin) {
						try {
							//System.Windows.Forms.Application.DoEvents();
						} catch (TypeInitializationException ex) {
							//UI.Current = new ConsoleUI();
						}
					}
				}
				Thread.Sleep(20);
			}

			// analyze results
			var results = ""; var errors = "";
			if (proc == null) return results;
			if (ScriptProgress == null) {
				output.AppendLine(proc.StandardOutput.ReadToEnd());
				output.AppendLine(errors = proc.StandardError.ReadToEnd());
			} else {
				output.AppendLine();
				output.AppendLine(errors = proc.StandardError.ReadToEnd());
				if (append && outputFile != null) {
					File.AppendAllText(outputFile, errors + "\n");
				}
			}
			results = output.ToString().Trim() + "\n";
			if (outputFile != null) {
				if (!append) File.WriteAllText(outputFile, results);
			}
			return results;
		}
	}
}
