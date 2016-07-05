using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoTools.Server.Setup {

	public static class OS {

		public static bool IsMono => Type.GetType("Mono.Runtime") != null;
		public static bool IsWindows => !IsMono;

	}
}
