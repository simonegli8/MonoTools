using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using MonoTools.VisualStudio.MonoClient;

namespace MonoTools.VisualStudio.Views {
	/// <summary>
	///     Interaktionslogik für ServersFound.xaml
	/// </summary>
	public partial class SetupSSHServer : Window {

		public SetupSSHServer() {
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Ports.Text = Options.Ports;
			DebugPassword.Password = Options.Password;
			Url.Text = Options.Settings.LastSSHUrl;
			Username.Text = Options.Settings.LastSSHUser;
			Password.Password = Options.Settings.LastSSHPassword;
			Manual.IsChecked = Options.Settings.LastSetupManualOption.GetValueOrDefault();
			Service.IsChecked = !Manual.IsChecked;
		}

		protected override void OnClosing(CancelEventArgs e) {
			Options.Settings.LastSSHUrl = Url.Text;
			Options.Settings.LastSSHUser = Username.Text;
			Options.Settings.LastSSHPassword = Password.Password;
			Options.Settings.LastSetupManualOption = Manual.IsChecked.GetValueOrDefault();
			Options.Settings.Save();
			base.OnClosing(e);
		}

		private void InstallClicked(object sender, RoutedEventArgs e) {
			Services.Current.ServerSetup(Url.Text, Username.Text, Password.Password, DebugPassword.Password, Ports.Text, Manual.IsChecked.GetValueOrDefault());
			DialogResult = true;
			Close();
		}

		private async void UpgradeClicked(object sender, RoutedEventArgs e) {
			await Services.Current.ServerUpgrade(Url.Text, Ports.Text, Password.Password);
			DialogResult = true;
			Close();
		}

		private void CancelClicked(object sender, RoutedEventArgs e) {
			DialogResult = false;
			Close();
		}

		private void UrlModified(object sender, RoutedEventArgs e) {
			Task.Run(() => {
				try {
					var host = new Uri(Url.Text).Host;
					var ip = Dns.GetHostAddresses(host).FirstOrDefault();
					if (ip != null) {
						var client = new DebugClient(false, Ports.Text ?? Options.Ports, DebugPassword.Password ?? Options.Password);
						bool outdated = false;
						try {
							using (var session = client.ConnectToServerAsync(ip.ToString()).Result) {
								outdated = new Version(App.Version) > session.GetServerVersion();
							}
						} catch {
							outdated = false;
						}
						Update.IsEnabled = outdated;
					} else {
						Update.IsEnabled = false;
					}
				} catch { }
			});
		}
	}
}