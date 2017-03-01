using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MonoTools.Library;

namespace MonoTools.Debugger {

	public class DebugClient {

		public ExecuteMessage Message;

		public bool IsLocal;
		public const int DefaultMessagePort = MonoDebugServer.DefaultMessagePort;
		public const int DefaultDebuggerPort = MonoDebugServer.DefaultDebuggerPort;
		public const int DefaultDiscoveryPort = MonoDebugServer.DefaultDiscoveryPort;

		public int MessagePort;
		public int DebuggerPort;
		public int DiscoveryPort;
		public string Password;
		public IPAddress CurrentServer { get; private set; }


		public DebugServer(string url, NameValueCollection environmentVariabled, string script) {
			IsLocal = local;
			Password = password;
			MonoDebugServer.ParsePorts(ports, out MessagePort, out DebuggerPort, out DiscoveryPort);
		}

		public async Task<DebugSession> ConnectToServerAsync(string ipAddressOrHostname) {

			if (IsLocal) {
				CurrentServer = new IPAddress(new byte[]{ 127, 0, 0, 1});
				return new DebugSession(this, null, false);
			}
			IPAddress server;
			if (IPAddress.TryParse(ipAddressOrHostname, out server)) {
				CurrentServer = server;
			} else {
				IPAddress[] adresses = Dns.GetHostEntry(ipAddressOrHostname).AddressList;
				CurrentServer = adresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
			}

			bool compress = TraceRoute.GetTraceRoute(CurrentServer.ToString()).Count() > 1;

			var tcp = new TcpClient();

			await tcp.ConnectAsync(CurrentServer, MessagePort);
			return new DebugSession(this, tcp.Client, compress);
		}
	}
}