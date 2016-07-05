using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace MonoTools.Library {

	public class ConsoleStream : Stream {

		public enum Channels { Output, Error };
		ClientSession Session;
		Encoding Encoding;

		public MemoryStream Buffer;
		public Stream BaseStream;
		public CancellationTokenSource Cancel;
		public Channels Channel;

		public ConsoleStream(Channels channel, ClientSession session, Encoding encoding, Stream baseStream) {
			BaseStream = baseStream;
			Channel = channel;
			Encoding = encoding;
			Session = session;
		}

		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length { get { throw new NotImplementedException(); } }
		public override long Position { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
	
		public override void WriteByte(byte value) {
			lock (this) {
				BaseStream?.WriteByte(value);
				Buffer.WriteByte(value);
			}
		}

		public override void Write(byte[] buffer, int offset, int count) {
			lock (this) {
				BaseStream?.Write(buffer, offset, count);
				Buffer.Write(buffer, offset, count);
			}
		}

		public void Send() {
			lock (this) {
				if (Buffer.Length > 0) {
					var m = new StatusMessage(Commands.Info);
					try {
						var text = Encoding.GetString(Buffer.ToArray()); // might throw an exception, if buffer length is not on character borders. just wait for more text.
						if (Channel == Channels.Output) Session.SendOutput(text);
						else Session.SendError(text);
						Buffer.SetLength(0);
					} catch { }
				}
			}
		}

		public void Publish() {
			Task.Run((Action)(() => {
				try {
					while (!Cancel.IsCancellationRequested) {
						Thread.Sleep(100); Send();
					}
				} catch { }
			}));
		}

		public override void Flush() {
			lock (this) {
				BaseStream?.Flush();
				Send();
			}
		}

		public override void Close() {
			lock (this) {
				Cancel.Cancel();
				Flush();
				BaseStream?.Close();
			}
		}

		public new void Dispose() { Close(); }

		public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }
		public override void SetLength(long value) { throw new NotImplementedException(); }
		public override int Read(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
	}
}
