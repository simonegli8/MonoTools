using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using MonoTools.Debugger;

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
		}

		protected override void OnClosing(CancelEventArgs e) {
			Options.Settings.LastSSHUrl = Url.Text;
			Options.Settings.LastSSHUser = Username.Text;
			Options.Settings.LastSSHPassword = Password.Password;
			Options.Settings.LastSetupManualOption = Manual.IsChecked.GetValueOrDefault();
			Options.Settings.Save();
			base.OnClosing(e);
		}

		private void SaveClicked(object sender, RoutedEventArgs e) {
			DialogResult = true;
			Close();
		}

		private void CancelClicked(object sender, RoutedEventArgs e) {
			DialogResult = false;
			Close();
		}

		public void KeyFileClicked(object sender, RoutedEventArgs e) {
			var openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true)
				txtEditor.Text = openFileDialog.FileName;
		}
	}
}