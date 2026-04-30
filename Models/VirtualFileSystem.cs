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
					byte[]? data = null;
					try
					{
						// 100MB 이상의 파일은 메모리 보호를 위해 데이터를 직접 로드하지 않음 (선택 사항)
						if (entry.Length > 0 && entry.Length < 100 * 1024 * 1024)
						{
							using var stream = entry.Open();
							using var ms = new System.IO.MemoryStream();
							stream.CopyTo(ms);
							data = ms.ToArray();
						}
					}
					catch { /* 데이터 로드 실패 시 노드만 생성하고 데이터는 null 유지 */ }

					parent.Children.Add(new FileNode
					{
						Name = parts[0],
						FullPath = entry.FullName,
						IsDirectory = false,
						IsVirtual = true,
						VirtualData = data
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