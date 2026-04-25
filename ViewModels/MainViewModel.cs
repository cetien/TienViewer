using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using TienViewer.Models;

namespace TienViewer.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<FileNode> RootNodes { get; } = new();

		private static readonly string ConfigPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TienViewer", "last_path.txt");

		// MainViewModel.cs에 추가
		public bool ShowAllFiles { get; set; } = false;

		private FileNode? _selectedFile;
		public FileNode? SelectedFile
		{
			get => _selectedFile;
			set { _selectedFile = value; OnPropertyChanged(); }
		}

		public MainViewModel()
		{
			LoadDrives();
		}

		private void LoadDrives()
		{
			RootNodes.Clear();
			foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
			{
				RootNodes.Add(new FileNode
				{
					Name = drive.Name,
					FullPath = drive.RootDirectory.FullName,
					IsDirectory = true
				});
			}
		}

		public void RefreshNode(FileNode node)
		{
			node.RefreshSubFolders();
		}

		public void LoadChildren(FileNode node)
		{
			if (!node.IsDirectory || node.IsVirtual) return;
			if (node.Children.Count > 0) return; // 이미 로드됨

			try
			{
				var dirs = Directory.GetDirectories(node.FullPath)
					.Select(d => new FileNode
					{
						Name = Path.GetFileName(d),
						FullPath = d,
						IsDirectory = true
					});

				var files = Directory.GetFiles(node.FullPath)
					.Select(f => new FileNode
					{
						Name = Path.GetFileName(f),
						FullPath = f,
						IsDirectory = false
					});

				foreach (var item in dirs.Concat(files))
					node.Children.Add(item);

				RefreshNode(node);
			}
			catch { /* 접근 불가 폴더 무시 */ }
		}

		public async Task LoadChildrenAsync(FileNode node)
		{
			if (!node.IsDirectory || node.IsVirtual) return;
			if (node.Children.Count > 0) return;

			try
			{
				// ✅ 파일시스템 I/O를 백그라운드 스레드에서 실행
				var (dirs, files) = await Task.Run(() =>
				{
					var d = Directory.GetDirectories(node.FullPath)
						.Select(p => new FileNode
						{
							Name = Path.GetFileName(p),
							FullPath = p,
							IsDirectory = true
						}).ToList();

					var f = Directory.GetFiles(node.FullPath)
						.Select(p => new FileNode
						{
							Name = Path.GetFileName(p),
							FullPath = p,
							IsDirectory = false
						}).ToList();

					return (d, f);
				});

				// ✅ UI 스레드에서 ObservableCollection 업데이트
				foreach (var item in dirs.Concat(files))
					node.Children.Add(item);

				RefreshNode(node);
			}
			catch { }
		}

		public void SaveLastPath(string path)
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
				File.WriteAllText(ConfigPath, path);
			}
			catch { }
		}

		public string? LoadLastPath()
		{
			try { return File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath) : null; }
			catch { return null; }
		}

		public void RestorePath(string targetPath)
		{
			if (string.IsNullOrEmpty(targetPath) || !Path.IsPathRooted(targetPath)) return;

			// 1. 드라이브 찾기
			FileNode? current = RootNodes.FirstOrDefault(n => targetPath.StartsWith(n.FullPath, StringComparison.OrdinalIgnoreCase));
			if (current == null) return;

			var pathParts = targetPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			
			// 2. 계층별로 내려가며 확장 및 자식 로드
			current.IsExpanded = true;
			LoadChildren(current);

			foreach (var part in pathParts.Skip(1)) // 드라이브 이후 명칭들 탐색
			{
				var next = current.Children.FirstOrDefault(c => c.IsDirectory && c.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
				if (next == null) break;
				
				current = next;
				current.IsExpanded = true;
				LoadChildren(current);
			}
			SelectedFile = current;
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string? name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}