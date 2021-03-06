﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.Win32;
using MonoTools.Library;
using MonoTools.Debugger;
using MonoProgram.Package.Debuggers;

using Process = System.Diagnostics.Process;

namespace MonoTools.VisualStudio {

	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", App.Version, IconResourceID = 400)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExists_string)]
	[ProvideOptionPage(typeof(MonoToolsOptionsDialogPage), "MonoTools", "General", 0, 0, true)]
	[Guid(Guids.MonoToolsPkgString)]
	[ProvideDebugEngine("Mono Debug Engine", typeof(MonoProgramProvider), typeof(MonoEngine), Guids.EngineId, true, true, false)]
	public sealed class VSPackage : Package, IDisposable {
		//private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private Services services;

		protected override void Initialize() {
			var settingsManager = new ShellSettingsManager(this);
			var configurationSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
			UserSettingsManager.Initialize(configurationSettingsStore);
			base.Initialize();
			var dte = (DTE)GetService(typeof(DTE));
			services = new Services(dte);
			//TryRegisterAssembly();

			ErrorsWindow.Initialize(this);
			StatusBar.Initialize(this);
			Options.Initialize(this);

			/* Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
				Source = new Uri("/MonoTools;component/Resources/Resources.xaml", UriKind.Relative)
			}); */

			var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			InstallMenu(mcs);
		}

		private void InstallMenu(OleMenuCommandService mcs) {
			if (mcs != null) {
				// Create the commands for the menu item.
				var xBuildMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.XBuildSolution);
				var xBuildMenuItem = new OleMenuCommand(XBuildMenuItemClicked, xBuildMenuID);
				xBuildMenuItem.BeforeQueryStatus += HasSolution;
				mcs.AddCommand(xBuildMenuItem);

				var xRebuildMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.XRebuildSolution);
				var xRebuildMenuItem = new OleMenuCommand(XRebuildMenuItemClicked, xRebuildMenuID);
				xRebuildMenuItem.BeforeQueryStatus += HasSolution;
				mcs.AddCommand(xRebuildMenuItem);

				var xBuildProjectMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.XBuildProject);
				var xBuildProjectMenuItem = new OleMenuCommand(XBuildProjectMenuItemClicked, xBuildProjectMenuID);
				xBuildProjectMenuItem.BeforeQueryStatus += HasCurrentProject;
				mcs.AddCommand(xBuildProjectMenuItem);

				var xRebuildProjectMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.XRebuildProject);
				var xRebuildProjectMenuItem = new OleMenuCommand(XRebuildProjectMenuItemClicked, xRebuildProjectMenuID);
				xRebuildProjectMenuItem.BeforeQueryStatus += HasCurrentProject;
				mcs.AddCommand(xRebuildProjectMenuItem);

				var addPdb2MdbToProjectMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.AddPdb2MdbToProject);
				var addPdb2MdbToProjectMenuItem = new OleMenuCommand(AddPdb2MdbToProjectMenuItemClicked, addPdb2MdbToProjectMenuID);
				addPdb2MdbToProjectMenuItem.BeforeQueryStatus += HasCurrentProject;
				mcs.AddCommand(addPdb2MdbToProjectMenuItem);

				var suppressXBuildForProjectMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.SuppressXBuildForProject);
				var suppressXBuildForProjectMenuItem = new OleMenuCommand(SuppressXBuildForProjectMenuItemClicked, suppressXBuildForProjectMenuID);
				suppressXBuildForProjectMenuItem.BeforeQueryStatus += HasCurrentProject;
				mcs.AddCommand(suppressXBuildForProjectMenuItem);

				var startMonoMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.StartMono);
				var startMonoMenuItem = new OleMenuCommand(StartMonoMenuItemClicked, startMonoMenuID);
				startMonoMenuItem.BeforeQueryStatus += HasStartupProject;
				mcs.AddCommand(startMonoMenuItem);

				var debugMonoLocallyID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.DebugMono);
				var debugMonoCmd = new OleMenuCommand(DebugMonoClicked, debugMonoLocallyID);
				debugMonoCmd.BeforeQueryStatus += HasStartupProject;
				mcs.AddCommand(debugMonoCmd);

				/* var logFileID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.OpenLogFile);
				var logFileCmd = new OleMenuCommand(OpenLogFile, logFileID);
				logFileCmd.BeforeQueryStatus += (o, e) => logFileCmd.Enabled = File.Exists(MonoLogger.LoggerPath);
				mcs.AddCommand(logFileCmd); */

				var MoMAID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.MoMASolution);
				var MoMACmd = new OleMenuCommand(MoMAClicked, MoMAID);
				MoMACmd.BeforeQueryStatus += HasSolution;
				mcs.AddCommand(MoMACmd);

				var MoMAProjectID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.MoMAProject);
				var MoMAProjectCmd = new OleMenuCommand(MoMAProjectClicked, MoMAProjectID);
				MoMAProjectCmd.BeforeQueryStatus += HasCurrentProject;
				mcs.AddCommand(MoMAProjectCmd);

				var ServerSetupID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.ServerSetup);
				var ServerSetupCmd = new MenuCommand(ServerSetupClicked, ServerSetupID);
				mcs.AddCommand(ServerSetupCmd);

				var HelpID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.Help);
				var HelpCmd = new MenuCommand(HelpClicked, HelpID);
				mcs.AddCommand(HelpCmd);

			}
		}

		private void OpenLogFile(object sender, EventArgs e) {
			services.OpenLogFile();
		}

		/* private void TryRegisterAssembly() {
			try {
				RegistryKey regKey = Registry.ClassesRoot.OpenSubKey($"CLSID\\{MonoEngine.ClassGuid}");

				if (regKey != null)
					return;

				string location = new Uri(typeof(MonoEngine).Assembly.CodeBase).LocalPath;

				string regasm = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe";
				if (!Environment.Is64BitOperatingSystem)
					regasm = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe";

				var p = new ProcessStartInfo(regasm, location);
				p.Verb = "runas";
				p.RedirectStandardOutput = true;
				p.UseShellExecute = false;
				p.CreateNoWindow = true;

				Process proc = Process.Start(p);
				while (!proc.HasExited) {
					string txt = proc.StandardOutput.ReadToEnd();
				}

				using (RegistryKey config = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration)) {
					MonoToolsDebuggerInstaller.RegisterDebugEngine(location, config);
				}
			} catch (UnauthorizedAccessException) {
				MessageBox.Show(
					 "Failed finish installation of MonoTools.Debugger - Please run Visual Studio once als Administrator...",
					 "MonoTools.Debugger", MessageBoxButton.OK, MessageBoxImage.Error);
			} catch (Exception ex) {
				logger.Error(ex);
			}
		} */

		private void HasStartupProject(object sender, EventArgs e) {
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null) {
				var dte = GetService(typeof(DTE)) as DTE;
				var sb = (SolutionBuild2)dte.Solution?.SolutionBuild;
				menuCommand.Visible = true;
				if (menuCommand.Visible)
					menuCommand.Enabled = ((Array)sb?.StartupProjects)?.Cast<string>().Count() == 1;
			}
		}

		private void HasSolution(object sender, EventArgs e) {
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null) {
				menuCommand.Visible = true;
				var dte = GetService(typeof(DTE)) as DTE;
				menuCommand.Enabled = dte.Solution != null;
			}
		}

		private void HasCurrentProject(object sender, EventArgs e) {
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null) {
				menuCommand.Visible = true;
				var dte = GetService(typeof(DTE)) as DTE;
				Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
				var proj = activeSolutionProjects.OfType<Project>().FirstOrDefault();
				menuCommand.Enabled = activeSolutionProjects.Length > 0;
				string cmd;
				switch ((uint)menuCommand.CommandID.ID) {
				case PkgCmdID.AddPdb2MdbToProject: cmd = "Add pdb2mdb to"; break;
				case PkgCmdID.SuppressXBuildForProject: cmd = "Suppress XBuild for"; break;
				case PkgCmdID.MoMAProject: cmd = "MoMA"; break;
				case PkgCmdID.XBuildProject: cmd = "XBuild"; break;
				case PkgCmdID.XRebuildProject: cmd = "XRebuild"; break;
				default: throw new NotSupportedException();
				}
				menuCommand.Text = cmd + " " + (proj != null ? proj.Name : "Project");
			}
		}


		private readonly CancellationTokenSource cts = new CancellationTokenSource();

		private DTE2 GetDTE() {
			return GetService(typeof(DTE)) as DTE2;
		}

		private void StartMonoMenuItemClicked(object sender, EventArgs e) {
			services.Start();
		}
		private async void DebugMonoClicked(object sender, EventArgs e) {
			services.StartDebug();
		}

		private void XBuildMenuItemClicked(object sender, EventArgs e) {
			services.XBuild();
		}

		private void XRebuildMenuItemClicked(object sender, EventArgs e) {
			services.XBuild(true);
		}

		private void XBuildProjectMenuItemClicked(object sender, EventArgs e) {
			services.XBuildProject();
		}

		private void XRebuildProjectMenuItemClicked(object sender, EventArgs e) {
			services.XBuildProject(true);
		}

		private void AddPdb2MdbToProjectMenuItemClicked(object sender, EventArgs e) {
			services.AddPdb2MdbToProject();
		}

		private void SuppressXBuildForProjectMenuItemClicked(object sender, EventArgs e) {
			services.SuppressXBuildForProject();
		}

		private void MoMAClicked(object sender, EventArgs e) {
			services.MoMASolution();
		}

		private void MoMAProjectClicked(object sender, EventArgs e) {
			services.MoMAProject();
		}
		
		private void DebugRemoteClicked(object sender, EventArgs e) {
			services.StartSearching();
		}

		private void ServerSetupClicked(object sender, EventArgs e) {
			services.ServerSetup();
		}

		private void HelpClicked(object sender, EventArgs e) {
			services.Help();
		}

		#region IDisposable Members
		private bool disposed = false;
		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);
			
			if (this.disposed)
				return;
			/*
			if (disposing) {
				//Dispose managed resources
				if (Services.Server != null) {
					Services.Server.Dispose();
					Services.Server = null;
				}
			} */
	
			//Dispose unmanaged resources here.

			disposed = true;
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~VSPackage() {
			Dispose(false);
		}
		#endregion

	}
}