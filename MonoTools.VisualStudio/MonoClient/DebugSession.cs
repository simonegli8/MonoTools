using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;
using MonoTools.Debugger.Contracts;
using MonoTools.Library;

namespace MonoTools.VisualStudio.MonoClient {

	public class DebugSession : IDebugSession, IDisposable {

		private readonly TimeSpan Delay = TimeSpan.FromMinutes(5);
		private readonly TcpCommunication communication;

		public DebugSession(DebugClient debugClient, Socket socket, bool compress = false) {
			Client = debugClient;
			communication = new TcpCommunication(socket, compress, Roles.Client, Client.IsLocal, Client.Password);
			communication.ExtendedSendStart += (sender, args) => StatusBar.InitProgress();
			communication.Progress = progress => StatusBar.Progress(progress);
			communication.ExtendedSendEnd += (sender, args) => StatusBar.ClearProgress();
		}

		public DebugClient Client { get; private set; }

		public void Dispose() { Disconnect(); }

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
			if (!communication.IsLocal) msg.Files.AddFolder(rootDirectory);

			communication.RootPath = rootDirectory;
			communication.Send(msg);
		}

		public async Task ExecuteAsync(string targetExe, string arguments, ApplicationTypes type, Frameworks framework, string rootDirectory, string workingDirectory, string url) {
			await Task.Run(() => Execute(type, framework, targetExe, arguments, rootDirectory, workingDirectory, url));
		}

		public void UpgradeServer(string ports = null, string password = null) {
			if (communication.IsLocal) return;

			var root = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
			var setupexe = Path.Combine(root, "monodebug.exe");

			ports = ports ?? Options.Ports;
			password = password ?? Options.Password;
			var args = new StringBuilder("-i ");
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
				ApplicationType = ApplicationTypes.ConsoleApplication,
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

		public Version GetServerVersion() {
			communication.Send(new StatusMessage(Commands.Info));
			return WaitForAnswerAsync().Result.Version;
		}

		public async Task<Message> WaitForAnswerAsync() {
			Task delay = Task.Delay(Delay);
			Task res = await Task.WhenAny(communication.ReceiveAsync(), delay);

			if (res is Task<Message>) return ((Task<Message>)res).Result;

			if (res == delay) throw new Exception("Did not receive an answer in time...");
			throw new Exception("Cant start debugging");
		}

		public async Task<T> WaitForAnswerAsync<T>() where T: Message, new() {
			var msg = await WaitForAnswerAsync();
			if (msg is T) return (T)msg;
			PushBack(msg);
			return null;
		}


		public async Task<Message> WaitForAnswerAsync(CancellationToken token) {
			Task delay = Task.Delay(Delay);
			Task res = await Task.WhenAny(communication.ReceiveAsync(token), delay);

			if (res is Task<Message>) return ((Task<Message>)res).Result;

			if (res == delay) throw new Exception("Did not receive an answer in time...");
			throw new Exception("Cant start debugging");
		}

		public async Task<T> WaitForAnswerAsync<T>(CancellationToken token) where T : Message, new() {
			var msg = await WaitForAnswerAsync(token);
			if (msg is T) return (T)msg;
			PushBack(msg);
			return null;
		}



		public void PushBack(Message msg) => communication.PushBack(msg);

	}
}