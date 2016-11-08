using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using NLog;

namespace MonoTools.Library {

	public class MonoDebugServer : IDisposable {
		public const int DefaultMessagePort = 13881;
		public const int DefaultDebuggerPort = 11000;
		public const int DefaultDiscoveryPort = 13883;

		public int MessagePort = DefaultMessagePort;
		public int DebuggerPort = DefaultDebuggerPort;
		public int DiscoveryPort = DefaultDiscoveryPort;

		public string Password = null;
		public static int InvalidPasswordAttempts = 0;

		public static readonly Logger logger = LogManager.GetCurrentClassLogger();
		public CancellationTokenSource Cancel = new CancellationTokenSource();
		public string TerminalTemplate;

		private Task listeningTask;
		private TcpListener tcp;

		public bool IsLocal = false;
		static bool Running = false;
		static bool ListeningForInput = false;

		public event Action<ConsoleKeyInfo> KeyPress;

		public static void ParsePorts(string ports, out int messagePort, out int debuggerPort, out int discoveryPort) {
			if (!string.IsNullOrEmpty(ports)) {
				var tokens = ports.Trim(' ', '"').Split(',', ';').Select(s => s.Trim());
				var first = tokens.FirstOrDefault();
				var second = tokens.Skip(1).FirstOrDefault();
				var third = tokens.Skip(2).FirstOrDefault();
				if (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(second) && !string.IsNullOrEmpty(third) &&
					int.TryParse(first, out messagePort) && int.TryParse(second, out debuggerPort) && int.TryParse(third, out discoveryPort)) return;
			}
			messagePort = DefaultMessagePort;
			debuggerPort = DefaultDebuggerPort;
			discoveryPort = DefaultDiscoveryPort;
		}

		public MonoDebugServer(bool local = false, string ports = null, string password = null, string terminalTemplate = null) {
			IsLocal = local;
			ParsePorts(ports, out MessagePort, out DebuggerPort, out DiscoveryPort);
			Password = password;
			if (!string.IsNullOrEmpty(Password)) logger.Info("Password protected");
			Current = this;
			TerminalTemplate = Terminal.Template(terminalTemplate);
		}

		public void Dispose() {
			Stop();
		}

		public static MonoDebugServer Current { get; private set; }

		public static void InvalidPassword() {
			if (InvalidPasswordAttempts++ >= 5) {
				logger.Error($"Wait 5 minutes after {InvalidPasswordAttempts} invalid password failures.");
				System.Threading.Thread.Sleep(TimeSpan.FromMinutes(5));
				MonoDebugServer.InvalidPasswordAttempts = 0;
			}
			throw new InvalidOperationException("Invalid password.");
		}

		public void Start() {
			lock (Current) {
				if (Running) return;
				Running = true;
			}
			if (!IsLocal) {
				tcp = new TcpListener(IPAddress.Any, MessagePort);
				tcp.Start();
				StartKeyInput();
			}
			listeningTask = Task.Run(() => {
				try {
					StartListening(Cancel.Token);
				} catch { }
			}, Cancel.Token);
		}

		private CancellationTokenSource ConsoleCancel = new CancellationTokenSource();

		public void StartKeyInput() {
			lock (Current) {
				if (ListeningForInput) return;
				ListeningForInput = true;
			}
			ConsoleCancel = new CancellationTokenSource();
			ConsoleKeyInfo input;
			Task.Run((Action)(() => {
				while (true) {
					bool doSleep = false;
					try {
						input = Console.ReadKey();
						//if (input.Key == ConsoleKey.Escape) break;
						if (KeyPress != null) KeyPress(input);
					} catch (IOException) {
						// This might happen on appdomain unload
						// until the previous threads are terminated.
						doSleep = true;
					} catch (ThreadAbortException) {
						doSleep = true;
					}

					Thread.Sleep(doSleep ? 500 : 0);
				}
				Stop();
				Environment.Exit(0);
			}), ConsoleCancel.Token);
		}

		public void SuspendKeyInput() {
			lock (Current) {
				if (!ListeningForInput) return;
				ListeningForInput = false;
			}
			if (!IsLocal && ListeningForInput) ConsoleCancel.Cancel();
		}

		public void ResumeKeyInput() {
			if (!IsLocal && ListeningForInput) StartKeyInput();
		}

		private void StartListening(CancellationToken token) {
			try {
				if (IsLocal) {
					var clientSession = new ClientSession(null, this);
					Task.Run((Action)clientSession.HandleSession, token).Wait();
					token.ThrowIfCancellationRequested();
				} else {
					while (true) {
						logger.Info("Waiting for client...");
						if (tcp == null) {
							token.ThrowIfCancellationRequested();
							return;
						}

						TcpClient client = tcp.AcceptTcpClient();
						token.ThrowIfCancellationRequested();

						logger.Info("Accepted client: " + client.Client.RemoteEndPoint);
						var clientSession = new ClientSession(client.Client, this);

						Task.Run((Action)clientSession.HandleSession, token).Wait();
					}
				}
			} catch (Exception ex) {
				if (!Cancel.IsCancellationRequested) logger.Error(ex.ToString());
			}
		}

		public void Stop() {
			lock (Current) {
				if (!Running) return;
				Running = false;
				ListeningForInput = false;
			}
			try {
				Cancel.Cancel();
				ConsoleCancel.Cancel();

				if (tcp != null && tcp.Server != null) {
					tcp.Server.Close(0);
					tcp = null;
				}
				if (IsLocal) 
				if (listeningTask != null) {
					if (!Task.WaitAll(new Task[] { listeningTask }, 5000))
						logger.Error("listeningTask timeout!!!");
				}

			} catch (Exception ex) {
				logger.Error(ex.ToString());
			} finally {
				logger.Info("\r\nStop Server");
			}
		}

		public void StartAnnouncing() {
			Task.Run(() => {
				try {
					CancellationToken token = Cancel.Token;
					logger.Trace("Start announcing");
					using (var client = new UdpClient()) {
						var ip = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
						client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

						while (true) {
							token.ThrowIfCancellationRequested();
							byte[] bytes = Encoding.ASCII.GetBytes($"MonoServer {App.Version}");
							client.Send(bytes, bytes.Length, ip);
							Thread.Sleep(100);
						}
					}
				} catch (OperationCanceledException) {
				} catch (Exception ex) {
					logger.Trace(ex);
				}
			});
		}

		public void WaitForExit() {
			try {
				listeningTask.Wait();
			} catch { }
		}
	}
}