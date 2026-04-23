using System.Collections.ObjectModel;

using TienViewer.Models;

namespace TienViewer.Models
{
	public class FileNode
	{
		public string Name { get; set; } = "";
		public string FullPath { get; set; } = "";
		public bool IsDirectory { get; set; }
		public bool IsVirtual { get; set; }  // ZIP 내부 항목
		public byte[]? VirtualData { get; set; }  // ZIP 내부 파일 데이터
		public ObservableCollection<FileNode> Children { get; set; } = new();

		public string Icon => IsDirectory ? "📁" : GetFileIcon();

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