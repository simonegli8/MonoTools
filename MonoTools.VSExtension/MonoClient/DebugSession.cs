using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;
using MonoTools.Debugger.Contracts;
using MonoTools.Debugger.Library;

namespace MonoTools.VSExtension.MonoClient {

	public class DebugSession : IDebugSession {

		private readonly TimeSpan Delay = TimeSpan.FromMinutes(5);
		private readonly TcpCommunication communication;
		private readonly ApplicationTypes type;
		string rootPath;
		public bool IsLocal = false;
		Frameworks framework;

		public DebugSession(DebugClient debugClient, ApplicationTypes type, Socket socket, string rootPath, Frameworks framework, bool compress = false, bool local = false, string password = null) {
			Client = debugClient;
			this.type = type;
			IsLocal = local;
			this.rootPath = rootPath;
			this.framework = framework;
			communication = new TcpCommunication(socket, rootPath, compress, local, Roles.Client, password);
			communication.Progress = progress => StatusBarProgress.Progress(progress);
		}

		public DebugClient Client { get; private set; }

		public void Disconnect() {
			communication.Disconnect();
		}

		public void TransferFiles() {
			var info = new DirectoryInfo(Client.OutputDirectory);
			if (!info.Exists)
				throw new DirectoryNotFoundException("Directory not found");

			var msg = new ExecuteMessage() {
				Command = Commands.Execute,
				ApplicationType = type,
				Framework = framework,
				Executable = Client.TargetExe,
				Arguments = Client.Arguments,
				WorkingDirectory = Client.WorkingDirectory,
				Url = Client.Url,
				RootPath = rootPath,
				IsLocal = IsLocal,
				Debug = true,
				LocalPath = Client.OutputDirectory
			};
			if (!IsLocal) msg.Files.AddFolder(rootPath);

			communication.Send(msg);
		}

		public async Task TransferFilesAsync() {
			await Task.Run(() => TransferFiles());
		}

		public void UpgradeServer(string ports = null, string password = null) {
			var updater = Assembly.GetExecutingAssembly().GetManifestResourceStream("MonoTools.VSExtension.MonoToolsServerSetup.exe");
			var path = Path.Combine(Path.GetTempPath(), "MonoToolsServerSetup.exe");
			using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write)) {
				updater.CopyTo(file);
			}
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
				Executable = path,
				Arguments = args.ToString(),
				WorkingDirectory = Client.WorkingDirectory,
				Url = Client.Url,
				RootPath = rootPath,
				IsLocal = IsLocal,
				Debug = true,
				LocalPath = Client.OutputDirectory
			};
			if (!IsLocal) msg.Files.AddFolder(rootPath);

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