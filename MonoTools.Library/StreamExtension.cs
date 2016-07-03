using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MonoTools.Library {

	public static class StreamExtension {

		const int BufferSize = 32*1024;

		public static void CopyTo(this Stream source, Stream dest, Action<long> progress) {
			byte[] buffer = new byte[BufferSize];
			long pos = 0;
			int n;
			while ((n = source.Read(buffer, 0, buffer.Length)) > 0) {
				dest.Write(buffer, 0, n);
				pos += n;
				progress(pos);
			}
		}
	}
}
