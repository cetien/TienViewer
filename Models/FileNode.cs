﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TienViewer.Models
{
	public class FileNode : INotifyPropertyChanged
	{
		public string Name { get; set; } = "";
		public string FullPath { get; set; } = "";
		public bool IsDirectory { get; set; }
		public bool IsVirtual { get; set; }  // ZIP 내부 항목
		public byte[]? VirtualData { get; set; }  // ZIP 내부 파일 데이터
		public ObservableCollection<FileNode> Children { get; set; } = new();

		// TreeView용 폴더 필터링 속성
		public IEnumerable<FileNode> SubFolders => Children.Where(x => x.IsDirectory);

		private bool _isExpanded;
		public bool IsExpanded
		{
			get => _isExpanded;
			set { _isExpanded = value; OnPropertyChanged(); }
		}

		private bool _isSelected;
		public bool IsSelected
		{
			get => _isSelected;
			set { _isSelected = value; OnPropertyChanged(); }
		}

		public string Icon => IsDirectory ? "📁" : GetFileIcon();

		public void RefreshSubFolders() => OnPropertyChanged(nameof(SubFolders));

		public event PropertyChangedEventHandler? PropertyChanged;
		public void OnPropertyChanged([CallerMemberName] string? name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		private string GetFileIcon()
		{
			var ext = System.IO.Path.GetExtension(Name).ToLowerInvariant();
			return ext switch
			{
				".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp" => "🖼️",
				".pdf" => "📄",
				".xlsx" or ".xls" => "📊",
				".txt" or ".log" or ".md" => "📝",
				".zip" or ".rar" or ".7z" => "🗜️",
				_ => "📃"
			};
		}
	}
}