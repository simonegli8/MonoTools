using System;
using System.Linq;

namespace MonoTools.Server {

	public class Program {

		public static void Main(string[] args) {

			ResourceAssemblyLoader.Init();

			var install = args.Any(a => a.Contains("-i"));
			var pipes = args.FirstOrDefault(a => a == "-mirror=")?.Substring("-mirror=".Length);
			var ports = args.FirstOrDefault(a => a.StartsWith("-ports="))?.Substring("-ports=".Length);
			var password = args.FirstOrDefault(a => a.StartsWith("-password="))?.Substring("-password=".Length);
			var terminalTemplate = args.FirstOrDefault(a => a.StartsWith("-terminal="))?.Substring("-terminal=".Length);

			if (args.Any(a => a.Contains("help") || a.Contains("?"))) UI.Help();

			Server.Run(install, pipes, ports, password, terminalTemplate);
		}
	}
}