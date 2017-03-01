using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using MonoTools.Library;

namespace MonoTools.Debugger {

	[Serializable]
	public class Launcher: ILauncher {

		public Server Server { get; set; }
		public StartTask Task { get; set; }

		public Action<int> Progress { get; set; }
		public Action<string> Log { get; set; }
		public Host Host { get; set; }
		public LocalHost Local { get; set; }

		protected string SourcePath => Task.SourcePath;
		protected string RemotePath => Server.RemotePath;
		public virtual bool CustomBuild => false;
		public StartWebTask Web => (StartWebTask)Task;
		public StartProgramTask Program => (StartProgramTask)Task;
		public bool IsWeb => Task is StartWebTask;
		public bool IsProgram => Task is StartProgramTask;
		public virtual string MonoOptions => "$MONO_OPTIONS--debug=mdb-optimizations--debugger-agent=transport=dt_socket,address=0.0.0.0:{task.MonoDebuggerPort},server=y";
		public virtual string MonoCommand => $@"mono {MonoOptions} ""{Host.Map(Program.Program)}"" {Program.Arguments}";
		public virtual string SslOptions => Web.Ssl ? $" --https --p12file={Map(RemotePath)}/.monotools.cert.pvk --cert={Map(RemotePath)}/.monotools.cert.cer --pkpwd=1234" : "";
		public virtual string XspCommand => $"{(Web.Framework == Library.Frameworks.Net4 ? "xsp4" : "xsp2")} {MonoOptions}  --root={Map(RemotePath)} --verbose --printlog --loglevels={Web.LogLevels} --applications=/:. --nonstop --port={Web.XspPort}{SslOptions}";

		public Launcher(Server server, StartTask task) {
			Task = task;
			Task.SourcePath =  SourcePath?.TrimEnd('/', '\\');
			Server.RemotePath = RemotePath?.TrimEnd('/', '\\');
			Server = server;
		}

		public virtual void Open() {
			Local = new LocalHost(Server, Task);
			if (!string.IsNullOrEmpty(Server.Display)) Host.SetEnvironment(new[] { new KeyValuePair<string, string>("DISPLAY", Server.Display) });
			if (!string.IsNullOrEmpty(Server.InitScript)) Host.Run(Server.InitScript);
		}

		public virtual void Kill() => Host.Kill();

		public virtual string Map(string path) => path;

		public virtual void Publish() { }

		public virtual void Build() {

			if (Task.NetFXBuild) {
				var mdbs = new Pdb2MdbGenerator();
				mdbs.GeneratePdb2Mdb(SourcePath);
			}
			if (IsWeb && Web.Ssl) { // save xsp certificates to source path
				var assembly = Assembly.GetExecutingAssembly();
				using (var cer = File.Create(Path.Combine(SourcePath, ".monotools.cert.cer"))) { 
					assembly.GetManifestResourceStream("MonoTools.VisualStudio.Debugger..monotools.cert.cer").CopyTo(cer);
				}
				using (var pvk = File.Create(Path.Combine(SourcePath, ".monotools.cert.pvk"))) {
					assembly.GetManifestResourceStream("MonoTools.VisualStudio.Debugger..monotools.cert.pvk").CopyTo(pvk);
				}
			}
		}

		public virtual void Upload() { }

		public virtual void Start() {

			Log("Launching application");

			if (IsProgram) {
				Host.ChangeDir(Program.WorkingDir);
				Host.Run(MonoCommand).Wait(60000);
			} else {
				if (Web.XspPort == default(int)) Web.XspPort = 9000;
				Host.ChangeDir(Web.Path);
				Host.Run(XspCommand).Wait(60000);

				var url = $"http://{new Uri(Server.Url).Host}:{Web.XspPort}";
				OpenBrowser(url);
			}
		}
		public virtual void OpenBrowser(string url) => Local.Run($"start {url}");

		public virtual void Launch() {
			Open();
			Kill();
			Publish();
			Build();
			Upload();
			Start();
		}

		public string Save() {
			var f = new BinaryFormatter();
			var m = new MemoryStream();
			f.Serialize(m, this);
			return Convert.ToBase64String(m.ToArray());
		}

		public static Launcher Load(string saved) {
			var f = new BinaryFormatter();
			var m = new MemoryStream(Convert.FromBase64String(saved));
			return (Launcher)f.Deserialize(m);
		}

		public static Launcher New(Server server, StartTask app) {
			switch (server.Type) {
			case ServerTypes.LocalMono: return new LocalLauncher(server, app);
			case ServerTypes.RemoteSsh: return new RemoteSshLauncher(server, app);
			case ServerTypes.WSLBash: return new LocalBashLauncher(server, app);
			case ServerTypes.Cygwin: return new LocalCygwinLauncher(server, app);
			default: throw new NotSupportedException();
			}
		}

		public static void Start(Server server, StartTask app) => New(server, app).Launch();

	}

}
