using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using Mono.Unix;
using Mono.Unix.Native;

namespace MonoTools.Library {

	public class ConsoleMirror : IDisposable {

		public UnixStream Input;
		public UnixStream Output;
		public UnixStream Error;
		public CancellationTokenSource Cancel = new CancellationTokenSource();

		public string Pipes => string.Join(",", Input.Handle, Output.Handle, Error.Handle);
		public static ConsoleMirror Parse(string pipes) {
			var a = pipes.Split(',').Select(t => int.Parse(t)).ToArray();
			return new ConsoleMirror { Input = new UnixStream(a[0]), Output = new UnixStream(a[1]), Error = new UnixStream(a[2]) };
		}

		bool closed = false;
		public void Close() {
			lock (this) {
				if (closed) return;
				closed = true;
				Cancel.Cancel();
				Input.Close();
				Output.Close();
				Error.Close();
			}
		}

		public void Dispose() => Close();

		public static void StartClient(string pipes) {

			if (!OS.IsWindows) {

				using (var p = Parse(pipes)) {

					Task.Run(() => { try { Console.OpenStandardInput().CopyTo(p.Input); } catch { p.Close(); Environment.Exit(0); } });
					Task.Run(() => { try { p.Error.CopyTo(Console.OpenStandardError()); } catch { p.Close(); Environment.Exit(0); } });

					try {
						p.Output.CopyTo(Console.OpenStandardOutput());
					} catch {
						p.Close();
						return;
					}
				}
			}
		}

		public static ConsoleMirror StartTerminalServer(Process p, ClientSession session, CancellationToken token) {

			if (OS.IsWindows) StartServer(p, session, token);
			else {

				ConsoleMirror server, client;

				CreatePipes(out server, out client);

				Task.Run(() => {
					try {
						using (var output = new ConsoleStream(ConsoleStream.Channels.Output, session, p.StandardOutput.CurrentEncoding, server.Output))
						using (var err = new ConsoleStream(ConsoleStream.Channels.Error, session, p.StandardError.CurrentEncoding, server.Error)) {
							Task.Run(() => { try { p.StandardOutput.BaseStream.CopyTo(output); } catch { p.Close(); } }, token);
							Task.Run(() => { try { p.StandardError.BaseStream.CopyTo(err); } catch { p.Close(); } }, token);
							try { server.Input.CopyTo(p.StandardInput.BaseStream); } catch { p.Close(); }
						}
					} catch { }
				}, token);

				return client;
			}
			return null;
		}
		

		public static void StartServer(Process p, ClientSession session, CancellationToken token) {
			Task.Run(() => {
				try {
					using (var output = new ConsoleStream(ConsoleStream.Channels.Output, session, p.StandardOutput.CurrentEncoding, null))
					using (var err = new ConsoleStream(ConsoleStream.Channels.Error, session, p.StandardError.CurrentEncoding, null)) {
						Task.Run(() => { try { p.StandardError.BaseStream.CopyTo(err); } catch { } }, token);
						try { p.StandardOutput.BaseStream.CopyTo(output); } catch { }
					}
				} catch { }
			}, token);
		}

		public static void CreatePipes(out ConsoleMirror server, out ConsoleMirror client) {
			var input = UnixPipes.CreatePipes();
			var output = UnixPipes.CreatePipes();
			var err = UnixPipes.CreatePipes();
			server = new ConsoleMirror { Input = input.Reading, Output = output.Writing, Error = err.Writing };
			client = new ConsoleMirror { Input = input.Writing, Output = output.Reading, Error = err.Reading };
		}
	}
}
