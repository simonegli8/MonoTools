using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace MonoTools.Library {

	public enum Roles {  Server, Client };

	public class TcpCommunication {
		private readonly BinaryFormatter serializer;
		public readonly Socket socket;
		public NetworkStream Stream;
		public string RootPath;
		public bool Compressed = false;
		public string Password = null;
		public bool IsLocal = false;
		public static PipeQueue<Message> server = new PipeQueue<Message>();
		public static PipeQueue<Message> client = new PipeQueue<Message>();
		public Stack<Message> buffer;
		public BinaryWriter writer;
		public BinaryReader reader;
		public Roles Role;
		public Action<double> Progress = progress => { };

		public TcpCommunication(Socket socket, bool compressed, Roles role, bool local, string password) {
			IsLocal = local;
			Role = role;
			Password = password;
			if (!IsLocal) {
				this.socket = socket;
				if (!OS.IsMono) {
					socket.ReceiveTimeout = -1;
					socket.SendTimeout = -1;
				}
				serializer = new BinaryFormatter();
				serializer.AssemblyFormat = FormatterAssemblyStyle.Simple;
				serializer.TypeFormat = FormatterTypeStyle.TypesAlways;
				serializer.Binder = new SimpleDeserializationBinder();
				Stream = new NetworkStream(socket, FileAccess.ReadWrite, false);
				writer = new BinaryWriter(Stream);
				reader = new BinaryReader(Stream);
			}
			Compressed = compressed;
			buffer = new Stack<Message>();
		}

		public bool IsConnected {
			get { return IsLocal || socket.IsSocketConnected(); }
		}

		public virtual void Send(Message msg) {
			if (IsLocal) {
				if (Role == Roles.Client) server.Enqueue(msg);
				else client.Enqueue(msg);
			} else {
				var m = new MemoryStream();
				serializer.Serialize(m, msg);
				writer.Write((Int32)m.Length);
				writer.Write(m.ToArray());
				if (msg is IExtendedMessage) {
					((IExtendedMessage)msg).Send(this);
				}
			}
		}

		public virtual Message Receive() {
			try {
				lock (this) if (buffer.Count > 0) return buffer.Pop();
				if (IsLocal) {
					if (Role == Roles.Client) return client.Dequeue();
					else return server.Dequeue();
				}
				var len = reader.ReadInt32();
				var buf = new byte[len];
				//while (socket.Available <= 0) System.Threading.Thread.Sleep(10);
				reader.Read(buf, 0, len);
				var m = new MemoryStream(buf);
				var msg = (Message)serializer.Deserialize(m);
				if (msg is IExtendedMessage) {
					((IExtendedMessage)msg).Receive(this);
				}
				return msg;
			} catch { return null; }
		}
		public T Receive<T>() where T : Message, new() => Receive() as T;

		public async void SendAsync(Message msg) {
			await Task.Run(() => Send(msg));
		}

		public async Task<Message> ReceiveAsync() {
			return await Task.Run(() => Receive());
		}
		public async Task<T> ReceiveAsync<T>() where T : Message, new() {
			return await Task.Run(() => Receive<T>());
		}

		public void PushBack(Message msg) {
			lock (this) buffer.Push(msg);
		}

		public void Disconnect() {
			if (!IsLocal && socket != null) {
				socket.Close(1);
				socket.Dispose();
			}
		}
	}

	sealed class SimpleDeserializationBinder : SerializationBinder {
		public override Type BindToType(string assemblyName, string typeName) {
			var assembly = Assembly.Load(assemblyName);
			return assembly.GetType(typeName);
		}
	}

}