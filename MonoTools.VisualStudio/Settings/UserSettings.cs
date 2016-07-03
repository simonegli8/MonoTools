namespace MonoTools.VisualStudio {

	public class UserSettings {
		public UserSettings() {
			LastIp = "127.0.0.1";
		}

		public string LastIp { get; set; }

		public string LastSSHUrl { get; set; }
		public string LastSSHUser { get; set; }
		public string LastSSHPassword { get; set; }
		public bool? LastSetupManualOption { get; set; }

		public void Save() => UserSettingsManager.Instance.Save(this);
	}
}