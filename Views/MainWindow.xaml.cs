
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using TienViewer.Helpers;
using TienViewer.Models;
using TienViewer.Viewers;
using TienViewer.ViewModels;

namespace TienViewer
{
	public partial class MainWindow : Window
	{
		private readonly MainViewModel _vm = new();

		// ✅ 전체화면 상태 보관
		private bool _isViewerFullscreen = false;
		private GridLength _savedSidebarWidth;

		// ✅ MainWindow 생성자에 추가
		public MainWindow()
		{
			InitializeComponent();
			DataContext = _vm;

			// ✅ MouseDoubleClick → PreviewMouseDoubleClick
			ViewerArea.PreviewMouseDoubleClick += ViewerArea_DoubleClick;

			// ✅ 윈도우 레벨 키 이벤트 — 포커스 무관하게 항상 동작
			PreviewKeyDown += MainWindow_PreviewKeyDown;            
			
			// 휠 이벤트 — ViewerArea 위에서 동작
			ViewerArea.PreviewMouseWheel += ViewerArea_MouseWheel;

			Loaded += (s, e) =>
			{
				var path = _vm.LoadLastPath();
				if (!string.IsNullOrEmpty(path))
				{
					_vm.RestorePath(path);
					FolderTree.UpdateLayout();
					if (_vm.SelectedFile != null)
						FolderTree_SelectedItemChanged(FolderTree,
							new RoutedPropertyChangedEventArgs<object>(null, _vm.SelectedFile));
				}
			};
		}

		private void ViewerArea_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) return;

			var mainGrid = (Grid)Content;
			var sidebarCol = mainGrid.ColumnDefinitions[0];  // 사이드바
			var splitterCol = mainGrid.ColumnDefinitions[1]; // 구분선

			if (!_isViewerFullscreen)
			{
				_savedSidebarWidth = sidebarCol.Width;
				sidebarCol.MinWidth = 0;          // ✅ MinWidth 런타임 제거
				sidebarCol.Width = new GridLength(0);
				splitterCol.Width = new GridLength(0);
				_isViewerFullscreen = true;
			}
			else
			{
				sidebarCol.Width = _savedSidebarWidth;
				sidebarCol.MinWidth = 150;        // ✅ MinWidth 복원
				splitterCol.Width = new GridLength(4);
				_isViewerFullscreen = false;
			}

			e.Handled = true;
		}

		private async Task LoadChildrenRecursiveAsync(FileNode node)
		{
			if (!node.IsDirectory || node.IsVirtual) return;

			await _vm.LoadChildrenAsync(node);

			foreach (var child in node.Children.Where(c => c.IsDirectory).ToList())
				await LoadChildrenRecursiveAsync(child);
		}

		private async Task RefreshFileListAsync(FileNode node)
		{
			if (node == null) return;

			bool showAll = ShowAllFilesToggle.IsChecked == true;

			if (showAll)
			{
				// ✅ 로딩 중 표시
				FileCountText.Text = "로딩 중...";
				FileList.ItemsSource = null;

				await LoadChildrenRecursiveAsync(node);

				FileList.ItemsSource = GetAllNodes(node)
					.Where(c => !c.IsDirectory);
			}
			else
			{
				FileList.ItemsSource = node.Children
					.Where(c => !c.IsDirectory);
			}

			FileCountText.Text = $"{FileList.Items.Count}개";
		}

		// ✅ Delete 로직 수정 — FullPath 기반, ItemsSource 재수집
		private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key != System.Windows.Input.Key.Delete) return;
			if (FileList.SelectedItem is not FileNode node) return;
			if (node.IsVirtual) return;
			if (ViewerArea.Content is EmptyViewer) return;

			//var result = MessageBox.Show(
			//	$"'{node.Name}' 을(를) 삭제하시겠습니까?",
			//	"파일 삭제",
			//	MessageBoxButton.YesNo,
			//	MessageBoxImage.Warning);

			//if (result != MessageBoxResult.Yes) return;

			try
			{
				int idx = FileList.SelectedIndex;

				Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
					node.FullPath,
					Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
					Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

				// ✅ FileNode 트리에서 제거
				var parent = _vm.RootNodes
					.SelectMany(r => GetAllNodes(r))
					.FirstOrDefault(n => n.Children.Contains(node));
				parent?.Children.Remove(node);

				// ✅ 현재 선택 폴더 기준으로 ItemsSource 재수집
				if (_vm.SelectedFile is FileNode folder && folder.IsDirectory)
					RefreshFileList(folder);

				int count = FileList.Items.Count;
				if (count > 0)
				{
					FileList.SelectedIndex = Math.Min(idx, count - 1);
					FileList.ScrollIntoView(FileList.SelectedItem);
				}
				else
				{
					ViewerArea.Content = new EmptyViewer("파일이 없습니다.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"삭제 실패: {ex.Message}", "오류",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}

			e.Handled = true;
		}

		// ✅ 신규 메서드
		private void ViewerArea_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			// Ctrl+Wheel은 ImageViewer가 처리 — 여기선 무시
			if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
				System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
				return;

			// 이미 ImageViewer가 Handled 처리했으면 무시
			if (e.Handled) return;

			int count = FileList.Items.Count;
			if (count == 0) return;

			int current = FileList.SelectedIndex;
			int next = e.Delta < 0
				? Math.Min(current + 1, count - 1)   // 휠 다운 → 다음
				: Math.Max(current - 1, 0);           // 휠 업   → 이전

			if (next == current) return;

			FileList.SelectedIndex = next;
			FileList.ScrollIntoView(FileList.SelectedItem);

			e.Handled = true;
		}

		private async void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (e.NewValue is not FileNode node) return;

			_vm.SelectedFile = node;
			Title = $"TienViewer — {node.FullPath}";

			if (node.IsDirectory && !node.IsVirtual)
			{
				await _vm.LoadChildrenAsync(node);
				_vm.SaveLastPath(node.FullPath);
			}
			else if (node.IsDirectory && node.IsVirtual)
			{
				_vm.SaveLastPath(node.FullPath);
			}

			await RefreshFileListAsync(node);

			// ✅ BeginInvoke → InvokeAsync (awaitable)
			await Dispatcher.InvokeAsync(() =>
			{
				if (FileList.Items.Count > 0)
				{
					FileList.SelectedIndex = 0;
					FileList.ScrollIntoView(FileList.SelectedItem);
				}
				else
				{
					ViewerArea.Content = new EmptyViewer("파일이 없습니다.");
				}
			}, System.Windows.Threading.DispatcherPriority.Loaded);
		}

		private async void ShowAllFilesToggle_Changed(object sender, RoutedEventArgs e)
		{
			if (_vm.SelectedFile is FileNode node && node.IsDirectory)
			{
				await RefreshFileListAsync(node);

				// ✅ BeginInvoke → InvokeAsync (awaitable)
				await Dispatcher.InvokeAsync(() =>
				{
					if (FileList.Items.Count > 0)
					{
						FileList.SelectedIndex = 0;
						FileList.ScrollIntoView(FileList.SelectedItem);
					}
					else
					{
						ViewerArea.Content = new EmptyViewer("파일이 없습니다.");
					}
				}, System.Windows.Threading.DispatcherPriority.Loaded);
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

		// ✅ 하위 폴더 전체 강제 로드 (재귀)
		private void LoadChildrenRecursive(FileNode node)
		{
			if (!node.IsDirectory || node.IsVirtual) return;

			_vm.LoadChildren(node); // 미로드 시 로드, 이미 로드된 경우 무해

			foreach (var child in node.Children.Where(c => c.IsDirectory))
				LoadChildrenRecursive(child);
		}

		// ✅ RefreshFileList 수정
		private void RefreshFileList(FileNode node)
		{
			if (node == null) return;

			bool showAll = ShowAllFilesToggle.IsChecked == true;

			if (showAll)
			{
				// 미로드 하위 폴더 전체 강제 로드
				LoadChildrenRecursive(node);

				FileList.ItemsSource = GetAllNodes(node)
					.Where(c => !c.IsDirectory);
			}
			else
			{
				FileList.ItemsSource = node.Children
					.Where(c => !c.IsDirectory);
			}

			FileCountText.Text = $"{FileList.Items.Count}개";
		}


		private void FileList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key != System.Windows.Input.Key.Delete) return;
			if (FileList.SelectedItem is not FileNode node) return;
			if (node.IsVirtual) return; // ZIP 내부 파일 삭제 금지

			var result = MessageBox.Show(
				$"'{node.Name}' 을(를) 삭제하시겠습니까?",
				"파일 삭제",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes) return;

			try
			{
				// ✅ 삭제 전 인접 항목 기억
				int idx = FileList.SelectedIndex;
				int nextIdx = FileList.Items.Count > 1
					? Math.Min(idx, FileList.Items.Count - 2)
					: -1;

				Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
					node.FullPath,
					Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
					Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

				// ✅ FileNode 목록에서 제거
				var parent = _vm.RootNodes
					.SelectMany(r => GetAllNodes(r))
					.FirstOrDefault(n => n.Children.Contains(node));
				parent?.Children.Remove(node);

				// ✅ ItemsSource 갱신
				FileList.ItemsSource = parent?.Children.Where(c => !c.IsDirectory)
									   ?? Enumerable.Empty<FileNode>();

				// ✅ 인접 파일 선택 or Empty
				if (nextIdx >= 0)
				{
					FileList.SelectedIndex = nextIdx;
					FileList.ScrollIntoView(FileList.SelectedItem);
				}
				else
				{
					ViewerArea.Content = new EmptyViewer("파일이 없습니다.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"삭제 실패: {ex.Message}", "오류",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}

			e.Handled = true;
		}

		// ✅ 트리 전체 노드 순회 헬퍼
		private IEnumerable<FileNode> GetAllNodes(FileNode node)
		{
			yield return node;
			foreach (var child in node.Children)
				foreach (var n in GetAllNodes(child))
					yield return n;
		}

	}
}
