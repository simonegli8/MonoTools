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
		public const int DefaultDebuggerPort = 13882;
		public const int DefaultDiscoveryPort = 13883;

		public int MessagePort = DefaultMessagePort;
		public int DebuggerPort = DefaultDebuggerPort;
		public int DiscoveryPort = DefaultDiscoveryPort;

		public string Password = null;

		public static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private readonly CancellationTokenSource cts = new CancellationTokenSource();
		public string TerminalTemplate;

		private Task listeningTask;
		private TcpListener tcp;

		public bool IsLocal = false;

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

		public string Which(string exe) {
			var info = new System.Diagnostics.ProcessStartInfo("which") {
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				Arguments = exe
			};
			var p = System.Diagnostics.Process.Start(info);
			p.WaitForExit();
			while (!p.HasExited) System.Threading.Thread.Sleep(10);
			if (p.ExitCode == 0) {
				return p.StandardOutput.ReadToEnd();
			}
			return null;
		}

		public MonoDebugServer(bool local = false, string ports = null, string password = null, string terminalTemplate = null) {
			IsLocal = local;
			ParsePorts(ports, out MessagePort, out DebuggerPort, out DiscoveryPort);
			Password = password;
			if (!string.IsNullOrEmpty(Password)) logger.Info("Password protected");
			Current = this;
			if (terminalTemplate == null) {
				if (Which("gnome-terminal") != null) TerminalTemplate = "gnome-terminal -x {0}";
				else if (Which("lxterminal") != null) TerminalTemplate = "lxterminal -e {0}";
				else if (Which("konsole") != null) TerminalTemplate = "konsole -e {0}";
				else if (Which("xfce4-terminal") != null) TerminalTemplate = "xfce4-terminal -x {0}";
				else TerminalTemplate = null;
			} else {
				switch (terminalTemplate) {
				case "gnome": TerminalTemplate = "gnome-terminal -x {0}"; break;
				case "lxe": TerminalTemplate = "lxterminal -e {0}"; break;
				case "kde": TerminalTemplate = "konsole -e {0}"; break;
				case "xfce4": TerminalTemplate = "xfce4-terminal -x {0}"; break;
				default: TerminalTemplate = terminalTemplate; break;
				}
			}
		}

		public void Dispose() {
			Stop();
		}

		public static MonoDebugServer Current { get; private set; }

		public void Start() {
			Current = this;
			if (!IsLocal) {
				tcp = new TcpListener(IPAddress.Any, MessagePort);
				tcp.Start();
			}
			ListenForCancelKey();
			listeningTask = Task.Run(() => StartListening(cts.Token), cts.Token);
		}

		private CancellationTokenSource consoleCancellationToken = new CancellationTokenSource();

		public void ListenForCancelKey() {
			Task.Run((Action)(() => {
				while (true) {
					bool doSleep;
					try {
						Console.ReadLine();
						break;
					} catch (IOException) {
						// This might happen on appdomain unload
						// until the previous threads are terminated.
						doSleep = true;
					} catch (ThreadAbortException) {
						doSleep = true;
					}

					if (doSleep)
						Thread.Sleep(500);
				}
				Stop();
				Environment.Exit(0);
			}), consoleCancellationToken.Token);
		}

		public void SuspendCancelKey() {
			consoleCancellationToken.Cancel();
		}

		public void ResumeCancelKey() {
			consoleCancellationToken = new CancellationTokenSource();
			ListenForCancelKey();
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
				logger.Error(ex.ToString());
			}
		}

		public void Stop() {
			try {
				cts.Cancel();
				if (tcp != null && tcp.Server != null) {
					tcp.Server.Close(0);
					tcp = null;
				}
				if (listeningTask != null) {
					if (!Task.WaitAll(new Task[] { listeningTask }, 5000))
						logger.Error("listeningTask timeout!!!");
				}

			} catch (Exception ex) {
				logger.Error(ex.ToString());
			} finally {
				logger.Info("Stop Server");
			}
		}

		public void StartAnnouncing() {
			Task.Run(() => {
				try {
					CancellationToken token = cts.Token;
					logger.Trace("Start announcing");
					using (var client = new UdpClient()) {
						var ip = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
						client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

						while (true) {
							token.ThrowIfCancellationRequested();
							byte[] bytes = Encoding.ASCII.GetBytes("MonoServer");
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