using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using EnvDTE;
using EnvDTE80;

namespace MonoTools.VisualStudio {

	public partial class Services {

		public void MoMA(string path, IEnumerable<string> files, bool gui, OutputWindowPane output) {
			string str = DetermineMonoPath();
			string fileName = string.Format(@"{0}\MoMA\MoMA.exe", str);
			string outpath = path + @"\MoMA Report.html";
			string[] textArray1 = new string[] { gui ? "" : "--nogui ", "--out \"", outpath, "\" ", string.Join(" ", files.Select(file => "\"" + file + "\"")) };
			string arguments = string.Concat(textArray1);
			Task task = new TaskFactory().StartNew(delegate {
				System.Diagnostics.Process process1 = new System.Diagnostics.Process();
				ProcessStartInfo info1 = new ProcessStartInfo {
					FileName = fileName,
					Arguments = arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				};
				process1.StartInfo = info1;
				System.Diagnostics.Process process = process1;
				process.Start();
				process.WaitForExit();
				System.Diagnostics.Process.Start(outpath);
			});
		}

		public void MoMAProject() {
			try {
				Project project = null;
				OutputWindowPane output = PrepareOutputWindowPane();
				Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
				if ((activeSolutionProjects != null) && (activeSolutionProjects.Length > 0)) {
					project = activeSolutionProjects.GetValue(0) as Project;
				}
				if (project == null) {
					output.OutputString("No project selected.\r\n\r\n");
				} else {
					string absoluteOutputPath = GetAbsoluteOutputPath(project);
					IEnumerable<string> files = Directory.EnumerateFiles(absoluteOutputPath, "*.exe").Concat<string>(Directory.EnumerateFiles(absoluteOutputPath, "*.dll"));
					MoMA(Path.GetDirectoryName(project.FileName), files, false, output);
				}
			} catch (Exception ex) {
				logger.Error<Exception>(ex);
				MessageBox.Show(ex.Message, "MonoTools", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		public void MoMASolution() {
			try {
				OutputWindowPane output = PrepareOutputWindowPane();
				if (string.IsNullOrEmpty((dte.Solution == null) ? null : dte.Solution.FileName)) {
					output.OutputString("No solution.\r\n\r\n");
				}


				IEnumerable<string> source = dte.Solution.Projects.OfType<Project>()
					.SelectMany(proj => {
						var absoluteOutputPath = GetAbsoluteOutputPath(proj);
						if (absoluteOutputPath == null) return new string[0];
						return Directory.EnumerateFiles(absoluteOutputPath, "*.exe").Concat(Directory.EnumerateFiles(absoluteOutputPath, "*.dll"));
					});
				if (source.Count() > 0) MoMA(Path.GetDirectoryName(dte.Solution.FileName), source, false, output);
			} catch (Exception ex) {
				logger.Error<Exception>(ex);
				MessageBox.Show(ex.Message, "MonoTools", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}
}