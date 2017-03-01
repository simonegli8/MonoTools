using System.Linq;
using System.Net;

namespace MonoTools.Debugger {

	public static class DnsResolve {

		public static IPAddress HostOrIPAddress(string hostOrIPAddress) {

			IPAddress result;
			if (IPAddress.TryParse(hostOrIPAddress, out result)) return result;
			else return Dns.GetHostEntry(hostOrIPAddress).AddressList.First();

		}
	}
}