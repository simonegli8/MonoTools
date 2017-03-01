using System.Collections.Generic;
using MonoTools.Debugger;


namespace MonoTools.VisualStudio {

	public class UserSettings {

		public List<Server> ServerList{ get; set; }

		public UserSettings() { ServerList = new List<Server>(); }

		public static List<Server> Servers => UserSettingsManager.Current.ServerList;

		public void Save() => UserSettingsManager.Instance.Save(this);
	}
}