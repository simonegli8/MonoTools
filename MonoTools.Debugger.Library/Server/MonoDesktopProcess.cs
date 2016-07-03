using System.Diagnostics;
using System.IO;

namespace MonoTools.Debugger.Library {

	internal class MonoDesktopProcess : MonoProcess {

		private readonly string _targetExe;
		string arguments;

		public MonoDesktopProcess(string targetExe, string arguments) {
			_targetExe = targetExe;
			this.arguments = arguments;
		}

		internal override Process Start(string workingDirectory) {
			string monoBin = MonoUtils.GetMonoPath();

			if (!Directory.Exists(workingDirectory)) Directory.CreateDirectory(workingDirectory);

			string args = GetProcessArgs();
			ProcessStartInfo procInfo = GetProcessStartInfo(workingDirectory, monoBin);
			procInfo.Arguments = args + " \"" + _targetExe + "\"";
			if (!string.IsNullOrEmpty(arguments)) procInfo.Arguments += " " + arguments;

			process = new System.Diagnostics.Process();
			process.StartInfo = procInfo;
			process.EnableRaisingEvents = true;
			if (RedirectOutput) {
				process.ErrorDataReceived += (sender, data) => Output(data.Data + "\r\n");
				process.OutputDataReceived += (sender, data) => Output(data.Data + "\r\n");
				process.BeginOutputReadLine();
			} else {
				procInfo.UseShellExecute = true;
			}

			process.Start();

			RaiseProcessStarted();
			return process;
		}
	}
}