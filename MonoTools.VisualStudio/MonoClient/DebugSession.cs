using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;
using MonoTools.Debugger.Contracts;
using MonoTools.Library;

namespace MonoTools.VisualStudio.MonoClient {

	public class DebugSession : IDebugSession {

		private readonly TimeSpan Delay = TimeSpan.FromMinutes(5);
		private readonly TcpCommunication communication;
		string rootPath;

		public DebugSession(DebugClient debugClient, Socket socket, bool compress = false) {
			Client = debugClient;
			communication = new TcpCommunication(socket, compress, Roles.Client, Client.IsLocal, Client.Password);
			communication.Progress = progress => StatusBarProgress.Progress(progress);
		}

		public DebugClient Client { get; private set; }

		public void Disconnect() {
			communication.Disconnect();
		}

		public void Execute(ApplicationTypes type, Frameworks framework, string targetExe, string arguments, string rootDirectory, string workingDirectory, string url) {
			var info = new DirectoryInfo(rootDirectory);
			if (!info.Exists)
				throw new DirectoryNotFoundException("Directory not found");

			var msg = new ExecuteMessage() {
				Command = Commands.Execute,
				ApplicationType = type,
				Framework = framework,
				Executable = targetExe,
				Arguments = arguments,
				WorkingDirectory = workingDirectory,
				Url = url,
				RootPath = rootDirectory,
				IsLocal = communication.IsLocal,
				Debug = true,
				LocalPath = rootDirectory
			};
			if (!communication.IsLocal) msg.Files.AddFolder(rootPath);

			communication.RootPath = rootDirectory;
			communication.Send(msg);
		}

		public async Task ExecuteAsync(string targetExe, string arguments, ApplicationTypes type, Frameworks framework, string rootDirectory, string workingDirectory, string url) {
			await Task.Run(() => Execute(type, framework, targetExe, arguments, rootDirectory, workingDirectory, url));
		}

		public void UpgradeServer(string ports = null, string password = null) {
			if (communication.IsLocal) return;

			var root = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
			var setupexe = Path.Combine(root, "MonoToolsServerSetup.exe");

			ports = ports ?? Options.Ports;
			password = password ?? Options.Password;
			var args = new StringBuilder();
			if (!string.IsNullOrEmpty(ports)) {
				args.Append("-ports=");
				args.Append(ports);
			}
			if (!string.IsNullOrEmpty(password)) {
				if (args.Length > 0) args.Append(" ");
				args.Append("-password=");
				args.Append(password);
			}
			if (!string.IsNullOrEmpty(password)) {
				if (args.Length > 0) args.Append(" ");
				args.Append("-upgrade");
			}

			var msg = new ExecuteMessage() {
				Command = Commands.Upgrade,
				ApplicationType = ApplicationTypes.DesktopApplication,
				Framework = Frameworks.Net4,
				Executable = setupexe,
				Arguments = args.ToString(),
				WorkingDirectory = null,
				Url = null,
				RootPath = root,
				IsLocal = false,
				Debug = false,
				LocalPath = root
			};
			msg.Files.Add(setupexe);

			communication.Send(msg);
		}

		public async Task<Message> WaitForAnswerAsync() {
			Task delay = Task.Delay(Delay);
			Task res = await Task.WhenAny(communication.ReceiveAsync(), delay);

			if (res is Task<Message>) return ((Task<Message>)res).Result;

			if (res == delay) throw new Exception("Did not receive an answer in time...");
			throw new Exception("Cant start debugging");
		}

		public void PushBack(Message msg) => communication.PushBack(msg);

	}
}