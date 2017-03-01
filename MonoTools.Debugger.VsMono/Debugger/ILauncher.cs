using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoTools.Debugger {

	public interface ILauncher : IDisposable {
		void Launch();
		Server Server { get; }
		StartTask Task { get; }
	}

}
