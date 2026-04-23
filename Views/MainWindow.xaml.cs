
using System.Windows;
using System.Windows.Controls;
using TienViewer.Helpers;
using TienViewer.Models;
using TienViewer.ViewModels;
using TienViewer.Viewers;

namespace TienViewer
{
	public partial class MainWindow : Window
	{
		private readonly MainViewModel _vm = new();

		public MainWindow()
		{
			InitializeComponent();
			DataContext = _vm;
		}

		private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (e.NewValue is not FileNode node) return;

			if (node.IsDirectory && !node.IsVirtual)
			{
				_vm.LoadChildren(node);
				// 파일 리스트 갱신
				FileList.ItemsSource = node.Children;
			}
			else if (node.IsDirectory && node.IsVirtual)
			{
				FileList.ItemsSource = node.Children;
			}
		}

		private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (FileList.SelectedItem is not FileNode node) return;
			if (node.IsDirectory) return;

			OpenFile(node);
		}

		private void OpenFile(FileNode node)
		{
			var type = FileTypeHelper.GetViewerType(node.Name);

			ViewerArea.Content = type switch
			{
				ViewerType.Image => new ImageViewer(node),
				ViewerType.Text => new TextViewer(node),
				ViewerType.Pdf => new PdfViewer(node),
				ViewerType.Excel => new ExcelViewer(node),
				ViewerType.Zip => OpenZip(node),
				_ => new EmptyViewer("지원하지 않는 파일 형식입니다.")
			};
		}

		private UIElement OpenZip(FileNode node)
		{
			var virtualRoot = VirtualFileSystem.BuildFromZip(node.FullPath);
			// ZIP 루트를 TreeView에 임시 삽입
			_vm.RootNodes.Add(virtualRoot);
			return new EmptyViewer("ZIP 파일이 열렸습니다. 왼쪽 트리에서 탐색하세요.");
		}
	}
}
