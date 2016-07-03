using System.Windows;

namespace MonoTools.VSExtension.Views {
	/// <summary>
	///     Interaktionslogik für ServersFound.xaml
	/// </summary>
	public partial class SetupSSHServer : Window {

		public SetupSSHServer() {
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

		}

		private void Install(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}

		private void Cancel(object sender, RoutedEventArgs e) {
			DialogResult = false;
		}
	}
}