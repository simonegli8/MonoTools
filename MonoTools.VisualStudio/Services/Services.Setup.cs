using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.IO;

namespace MonoTools.VisualStudio {

	public partial class Services {

		Window setupForm;
		public async void ServerSetup() {
			try {
				setupForm = new Views.SetupSSHServer();

				setupForm.Show();
			} catch (Exception ex) {
				logger.Error<Exception>(ex);
				MessageBox.Show(ex.Message, "MonoTools", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}
}
