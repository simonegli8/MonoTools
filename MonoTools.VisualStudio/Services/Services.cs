﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell;
using NLog;

namespace MonoTools.VisualStudio {

	public partial class Services {

		private readonly DTE2 dte;
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		public static Services Current { get; private set; }

		public Services(DTE dte) {
			this.dte = (DTE2)dte;
			Current = this;
		}

		internal void BuildSolution() {
			var sb = (SolutionBuild2)dte.Solution.SolutionBuild;
			sb.Build(true);
		}

		internal string GetStartupAssemblyPath() {
			Project startupProject = GetStartupProject();
			return GetAssemblyPath(startupProject);
		}

		public Project GetStartupProject() {
			var sb = (SolutionBuild2)dte.Solution.SolutionBuild;
			string project = ((Array)sb.StartupProjects).Cast<string>().First();
			Project startupProject;
			try {
				startupProject = dte.Solution.Item(project);
			} catch (ArgumentException aex) {
				throw new ArgumentException($"The parameter '{project}' is incorrect.", aex);
			}

			return startupProject;
		}

		internal string GetAssemblyPath(Project vsProject) {
			string fullPath = vsProject.Properties.Item("FullPath").Value.ToString();
			string outputPath =
				 vsProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
			string outputDir = Path.Combine(fullPath, outputPath);
			string outputFileName = vsProject.Properties.Item("OutputFileName").Value.ToString();
			string assemblyPath = Path.Combine(outputDir, outputFileName);
			return assemblyPath;
		}

		private string DetermineMonoPath() {
			OutputWindowPane outputWindowPane = PrepareOutputWindowPane();

			var monoPath = Options.MonoPath;

			if (!string.IsNullOrEmpty(monoPath)) {
				//outputWindowPane.OutputString("MonoTools: Mono Installation Path is set.\r\n");
			} else {
				//outputWindowPane.OutputString("MonoTools: Mono Installation Path is not set. Trying to get it from registry.\r\n");

				RegistryKey openSubKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Novell\\Mono");

				if (openSubKey == null) {
					openSubKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Novell\\Mono");
				}

				if (openSubKey == null) {
					monoPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86), "Mono");
					if (!Directory.Exists(monoPath))
						throw new Exception(
							"Mono Runtime not found. Please install Mono and ensure that Mono Installation Path is set via Tools \\ Options \\ Mono Helper or that the necessary registry settings are existing.");
				} else {
					string value = openSubKey.GetSubKeyNames().OrderByDescending(x => x).First();
					monoPath = (string)openSubKey.OpenSubKey(value).GetValue("SdkInstallRoot");
				}
			}

			return monoPath;
		}

		private string GetAbsoluteOutputPath(Project project) {
			Property property = null;
			try {
				property = GetProperty(project.ConfigurationManager.ActiveConfiguration.Properties, "OutputPath");
			} catch {
				return null;
			}
			string str = ((string)property.Value as string) ?? "";
			if (str.StartsWith(Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString())) {
				return str;
			}
			if ((str.Length >= 2) && (str[1] == Path.VolumeSeparatorChar)) {
				return str;
			}
			if (str.IndexOf(@"..\") != -1) {
				string directoryName = Path.GetDirectoryName(project.FullName);
				while (str.StartsWith(@"..\")) {
					str = str.Substring(3);
					directoryName = Path.GetDirectoryName(directoryName);
				}
				return Path.Combine(directoryName, str);
			}
			return Path.Combine(Path.GetDirectoryName(project.FullName), str);
		}

		private string GetProgramFileName(Project project) {
			switch (((int)GetProperty(project.ConfigurationManager.ActiveConfiguration.Properties, "StartAction").Value)) {
			case 0: {
					Property property = GetProperty(project.Properties, "OutputFileName");
					return Path.Combine(GetAbsoluteOutputPath(project), (string)property.Value);
				}
			case 1:
				return (string)GetProperty(project.ConfigurationManager.ActiveConfiguration.Properties, "StartProgram").Value;

			case 2:
				return GetAbsoluteOutputPath(project);
			}
			throw new InvalidOperationException("Unknown StartAction");
		}

		private Property GetProperty(Properties properties, string propertyName) {
			if (properties != null) {
				foreach (Property property in properties) {
					if ((property != null) && (property.Name == propertyName)) {
						return property;
					}
				}
			}
			return null;
		}

		private OutputWindowPane PrepareOutputWindowPane() {
			dte.ExecuteCommand("View.Output");

			OutputWindow outputWindow = dte.ToolWindows.OutputWindow;

			OutputWindowPane outputWindowPane = null;

			foreach (OutputWindowPane pane in outputWindow.OutputWindowPanes) {
				if (pane.Name == "MonoTools") {
					outputWindowPane = pane;
					break;
				}
			}

			if (outputWindowPane == null) {
				outputWindowPane = outputWindow.OutputWindowPanes.Add("MonoTools");
			}

			outputWindowPane.Activate();

			outputWindowPane.Clear();
			outputWindowPane.OutputString($"MonoTools {App.Version} © johnshope.com 2016\r\n");

			return outputWindowPane;
		}

		public void Help() {
			System.Diagnostics.Process.Start("https://github.com/simonegli8/MonoTools/wiki/Help");
		}
	}
}