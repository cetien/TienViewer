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
			}
			catch { /* 접근 불가 폴더 무시 */ }
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string? name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}