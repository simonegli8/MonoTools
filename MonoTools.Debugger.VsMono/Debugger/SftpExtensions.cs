using System.Collections.Generic;
using System.IO;
using Renci.SshNet;

namespace MonoTools.VisualStudio {

	public static class SftpExtensions {
		public static void Clear(this SftpClient client) {
			Clear(client, ".");
		}

		public static void Clear(this SftpClient client, string path) {
			foreach (var file in client.ListDirectory(path)) {
				if (file.Name == "." || file.Name == "..")
					continue;

				if (file.IsDirectory)
					client.Clear($"{path}/{file.Name}");
				file.Delete();
			}
		}

		public static void Upload(this SftpClient client, string basePath, string path, HashSet<string> createdDirectories) {
			var fullPath = Path.Combine(basePath, path);
			if (Directory.Exists(fullPath)) {
				var directory = new DirectoryInfo(path);
				foreach (var file in directory.EnumerateFiles()) {
					using (var fileStream = file.OpenRead()) {
						client.UploadFile(fileStream, file.Name);
					}
				}
			} else {
				var directory = Path.GetDirectoryName(path);
				if (createdDirectories.Add(directory))
					client.CreateFullDirectory(directory);
				using (var fileStream = new FileInfo(fullPath).OpenRead()) {
					client.UploadFile(fileStream, path.Replace('\\', '/'));
				}
			}
		}

		public static void CreateFullDirectory(this SftpClient client, string path) {
			var parts = path.Split('/');
			var currentPath = ".";
			foreach (var part in parts) {
				var newPath = currentPath + "/" + part;
				if (!client.Exists(newPath))
					client.CreateDirectory(newPath);
				currentPath = newPath;
			}
		}
	}
}