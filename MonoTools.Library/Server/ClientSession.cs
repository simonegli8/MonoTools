﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using NLog;

namespace MonoTools.Library {

	internal class ClientSession {
		private Process process;
		private readonly TcpCommunication communication;
		private readonly Logger logger = LogManager.GetCurrentClassLogger();
		private readonly IPAddress remoteEndpoint = null;
		public readonly string RootPath;
		public readonly MonoDebugServer Server;
		public bool IsLocal => Server.IsLocal;
		public int DebuggerPort => Server.DebuggerPort;
		public string Password => Server.Password;
		public const bool CanCompress = true;

		public ClientSession(Socket socket, MonoDebugServer server) {
			Server = server;
			if (socket != null) remoteEndpoint = ((IPEndPoint)socket.RemoteEndPoint).Address;
			RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MonoToolsServer", remoteEndpoint.ToString());
			communication = new TcpCommunication(socket,CanCompress, Roles.Server, IsLocal, Password);
			communication.Progress = progress => {
				const int Width = 60;
				Console.CursorLeft = 0;
				var n = (int)(Width*progress+0.5);
				var p = ((int)(progress*100+0.5)).ToString()+"%";
				var t = (Width - p.Length)/2;
				var backColor = Console.BackgroundColor;
				for (int i = 0; i < Width; i++) {
					Console.BackgroundColor = i < n ? ConsoleColor.DarkGreen : ConsoleColor.DarkGray;
					Console.Write((i < t || i >= t + p.Length) ? ' ' : p[i-t]);
				}
				Console.BackgroundColor = backColor;
			};
		}

		public void HandleSession() {
			try {
				logger.Trace("New Session from {0}", remoteEndpoint?.ToString() ?? "localhost");

				while (communication.IsConnected) {
					if (process != null && process.HasExited)
						return;

					//communication.SendAsync(new StatusMessage { Command = Commands.Info });

					var msg = communication.Receive<CommandMessage>();

					if (msg == null) return;

					switch (msg.Command) {
					case Commands.Execute:
						var dbg = msg as ExecuteMessage;
						if (dbg != null) {
							if (!dbg.CheckSecurityToken(communication)) {
								communication.SendAsync(new StatusMessage { Command = Commands.InvalidPassword });
#if !DEBUG
								logger.Error("Wait one minute after invalid password failure.");
								System.Threading.Thread.Sleep(TimeSpan.FromMinutes(1));
#endif
							} else {
								StartExecuting(dbg);
							}
						}
						break;
					case Commands.Info: return;
					case Commands.Shutdown:
						logger.Trace("Shutdown-Message received");
						return;
					}
				}
			} catch (Exception ex) {
				logger.Error(ex);
			} finally {
				if (process != null && !process.HasExited)
					process.Kill();
			}
		}

		private void StartExecuting(ExecuteMessage msg) {

			if (!Directory.Exists(msg.RootPath)) Directory.CreateDirectory(msg.RootPath);

			logger.Trace("Extracted content to {1}", remoteEndpoint, msg.RootPath);

			if (!msg.HasMdbs) {
				var generator = new Pdb2MdbGenerator();
				string binaryDirectory = msg.ApplicationType == ApplicationTypes.DesktopApplication ? msg.RootPath : Path.Combine(msg.RootPath, "bin");
				generator.GeneratePdb2Mdb(binaryDirectory);
			}

			StartMono(msg);
		}

		private void StartMono(ExecuteMessage msg) {
			if (OS.IsMono) {
				Console.BackgroundColor = ConsoleColor.Black;
				Console.Clear();
			}
			MonoDebugServer.Current.SuspendCancelKey();

			msg.WorkingDirectory = string.IsNullOrEmpty(msg.WorkingDirectory) ? msg.RootPath : msg.WorkingDirectory;
			MonoProcess proc = MonoProcess.Start(msg, Server);
			proc.ProcessStarted += MonoProcessStarted;
			proc.Output = SendOutput;
			process = proc.Start();
			logger.Trace($"{proc.GetType().Name} started: \"{proc.process.StartInfo.FileName}\" {proc.process.StartInfo.Arguments}");
			process.Exited += MonoExited;
			EnsureSentStarted();
		}

		bool startedSent = false;
		public void EnsureSentStarted() {
			lock (this) {
				if (!startedSent) {
					startedSent = true;
					communication.SendAsync(new StatusMessage() { Command = Commands.Started });
				}
			}
		}

		private void SendOutput(string text) {
			EnsureSentStarted();
			logger.Info(text);
			if (text != null) lock (communication) communication.SendAsync(new ConsoleOutputMessage() { Text = text });
		}

		private void MonoProcessStarted(object sender, EventArgs e) {
			var web = sender as MonoWebProcess;
			if (web != null) Process.Start(web.Url);
		}

		private void MonoExited(object sender, EventArgs e) {
			startedSent = false;
			logger.Info("Program closed: " + process.ExitCode);
			try {
				Directory.Delete(RootPath, true);
			} catch (Exception ex) {
				logger.Trace("Cant delete {0} - {1}", RootPath, ex.Message);
			}
			MonoDebugServer.Current.ResumeCancelKey();
		}
	}
}