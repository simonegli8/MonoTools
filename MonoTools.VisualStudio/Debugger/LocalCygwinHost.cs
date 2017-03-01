using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MonoTools.Debugger {

	public class LocalCygwinHost : LocalBashHost, IDisposable {

		protected override string BashPath => "";
		protected override string RootPath => "/cygdrive/";

		public LocalCygwinHost(Server server, StartTask task) : base(server, task) { }
	}
}
