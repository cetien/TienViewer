using System.Collections.ObjectModel;
using System.IO.Compression;

using TienViewer.Models;

namespace TienViewer.Models
{
	public static class VirtualFileSystem
	{
		public static FileNode BuildFromZip(string zipPath)
		{
			var root = new FileNode
			{
				Name = System.IO.Path.GetFileName(zipPath),
				FullPath = zipPath,
				IsDirectory = true,
				IsVirtual = true
			};

			using var archive = ZipFile.OpenRead(zipPath);
			foreach (var entry in archive.Entries)
			{
				var parts = entry.FullName.Split('/');
				InsertNode(root, parts, entry);
			}

			return root;
		}

		private static void InsertNode(FileNode parent, string[] parts, ZipArchiveEntry entry)
		{
			if (parts.Length == 0 || string.IsNullOrEmpty(parts[0])) return;

			var existing = parent.Children.FirstOrDefault(c => c.Name == parts[0]);

			if (parts.Length == 1)
			{
				// 파일 노드
				if (!string.IsNullOrEmpty(parts[0]))
				{
					using var stream = entry.Open();
					using var ms = new System.IO.MemoryStream();
					stream.CopyTo(ms);

					parent.Children.Add(new FileNode
					{
						Name = parts[0],
						FullPath = entry.FullName,
						IsDirectory = false,
						IsVirtual = true,
						VirtualData = ms.ToArray()
					});
				}
			}
			else
			{
				// 폴더 노드
				if (existing == null)
				{
					existing = new FileNode
					{
						Name = parts[0],
						FullPath = parts[0],
						IsDirectory = true,
						IsVirtual = true
					};
					parent.Children.Add(existing);
				}
				InsertNode(existing, parts[1..], entry);
			}
		}
	}
}