using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

using System.Windows.Media;
using TienViewer.Helpers;
using TienViewer.Models;
using TienViewer.Viewers;
using TienViewer.ViewModels;

namespace TienViewer
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new();

        // ── 오른쪽 패널 슬라이드 상태 ──
        private bool _isPanelVisible = false;
        private const double PanelWidth = 260;
        private const double TriggerZone = 80;
        private readonly System.Windows.Media.TranslateTransform _panelTranslate = new() { X = PanelWidth };

        // ── 왼쪽 패널 슬라이드 상태 ──
        private bool _isLeftPanelVisible = false;
        private const double LeftPanelWidth = 280;
        private const double LeftTriggerZone = 80;
        private readonly System.Windows.Media.TranslateTransform _leftPanelTranslate = new() { X = -LeftPanelWidth };

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;

            PreviewKeyDown += MainWindow_PreviewKeyDown;
            ViewerContainer.PreviewMouseWheel += ViewerArea_MouseWheel;
            FileList.PreviewMouseWheel        += ViewerArea_MouseWheel;

            // EmptyViewer 등 내용이 투명한 상태에서도 마우스 이벤트를 감지할 수 있도록 배경 설정 및 이벤트 연결
            ViewerContainer.Background = Brushes.Transparent;
            ViewerContainer.MouseMove += ViewerContainer_MouseMove;
            ViewerContainer.MouseLeave += ViewerContainer_MouseLeave;

            // InfoPanel RenderTransform 코드에서 할당 (XAML 스코프 충돌 회피)
            InfoPanel.RenderTransform  = _panelTranslate;
            LeftPanel.RenderTransform  = _leftPanelTranslate;

            // InfoPanel 이벤트 연결
            InfoPanel.DeleteRequested += OnInfoPanel_Delete;
            InfoPanel.RenameRequested += OnInfoPanel_Rename;
            InfoPanel.MoveRequested   += OnInfoPanel_Move;

            Loaded += (s, e) =>
            {
                var path = _vm.LoadLastPath();
                if (!string.IsNullOrEmpty(path))
                {
                    _vm.RestorePath(path);
                    if (_vm.SelectedFile != null) _vm.SelectedFile.IsSelected = true;
                    FolderTree.UpdateLayout();
                    if (_vm.SelectedFile != null)
                        FolderTree_SelectedItemChanged(FolderTree,
                            new RoutedPropertyChangedEventArgs<object>(new object(), _vm.SelectedFile));
                }
            };
        }

        // ══════════════════════════════════════════════
        //  슬라이드-인 패널 제어 (좌/우 공통)
        // ══════════════════════════════════════════════

        private void ViewerContainer_MouseMove(object sender, MouseEventArgs e)
        {
            double containerWidth  = ViewerContainer.ActualWidth;
            double containerHeight = ViewerContainer.ActualHeight;
            var    pos             = e.GetPosition(ViewerContainer);
            double mouseX          = pos.X;
            double mouseY          = pos.Y;

            // 마우스가 패널 위에 있으면 해당 패널은 숨기지 않음
            bool mouseOverRightPanel = _isPanelVisible   && mouseX >= containerWidth - PanelWidth;
            bool mouseOverLeftPanel  = _isLeftPanelVisible && mouseX <= LeftPanelWidth;

            // Y 범위 15% ~ 85% 벗어나면 패널 숨김 (단, 패널 위에 있으면 면제)
            double yLow    = containerHeight * 0.15;
            double yHigh   = containerHeight * 0.85;
            bool   inYRange = mouseY >= yLow && mouseY <= yHigh;

            if (!inYRange)
            {
                if (!mouseOverRightPanel) HideInfoPanel();
                if (!mouseOverLeftPanel)  HideLeftPanel();
                return;
            }

            // ── 오른쪽 패널 ──
            bool shouldShowRight = (containerWidth - mouseX) < TriggerZone;
            if (shouldShowRight && !_isPanelVisible)
                ShowInfoPanel();
            else if (!shouldShowRight && _isPanelVisible && !mouseOverRightPanel)
                HideInfoPanel();

            // ── 왼쪽 패널 ──
            bool shouldShowLeft = mouseX < LeftTriggerZone;
            if (shouldShowLeft && !_isLeftPanelVisible)
                ShowLeftPanel();
            else if (!shouldShowLeft && _isLeftPanelVisible && !mouseOverLeftPanel)
                HideLeftPanel();
        }

        private void ViewerContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            // ViewerContainer 밖으로 나갈 때: 패널 위로 포커스가 이동한 경우가 아닌지 확인
            // InfoPanel / LeftPanel 은 ViewerContainer 의 자식이므로
            // 실제로 창 바깥으로 나갈 때만 숨겨야 함
            var posOnWindow = e.GetPosition(this);
            bool leftWindow = posOnWindow.X < 0 || posOnWindow.Y < 0
                           || posOnWindow.X > ActualWidth || posOnWindow.Y > ActualHeight;
            if (leftWindow)
            {
                HideInfoPanel();
                HideLeftPanel();
            }
        }

        // ── 오른쪽(Info) 패널 ──

        private void ShowInfoPanel()
        {
            if (_isPanelVisible) return;
            _isPanelVisible = true;
            InfoPanel.IsHitTestVisible = true;
            var currentFile = FileList.SelectedItem as FileNode;
            InfoPanel.SetFile(currentFile, ViewerArea.Content as UIElement);
            AnimatePanelX(_panelTranslate, InfoPanel, from: PanelWidth, to: 0, opacityFrom: 0, opacityTo: 1);
        }

        private void HideInfoPanel()
        {
            if (!_isPanelVisible) return;
            _isPanelVisible = false;
            InfoPanel.IsHitTestVisible = false;
            AnimatePanelX(_panelTranslate, InfoPanel, from: 0, to: PanelWidth, opacityFrom: 1, opacityTo: 0);
        }

        // ── 왼쪽(탐색) 패널 ──

        private void ShowLeftPanel()
        {
            if (_isLeftPanelVisible) return;
            _isLeftPanelVisible = true;
            LeftPanel.IsHitTestVisible = true;
            AnimatePanelX(_leftPanelTranslate, LeftPanel, from: -LeftPanelWidth, to: 0, opacityFrom: 0, opacityTo: 1);
        }

        private void HideLeftPanel()
        {
            if (!_isLeftPanelVisible) return;
            _isLeftPanelVisible = false;
            LeftPanel.IsHitTestVisible = false;
            AnimatePanelX(_leftPanelTranslate, LeftPanel, from: 0, to: -LeftPanelWidth, opacityFrom: 1, opacityTo: 0);
        }

        // ── 공통 애니메이션 ──

        private static void AnimatePanelX(
            System.Windows.Media.TranslateTransform transform,
            UIElement element,
            double from, double to,
            double opacityFrom, double opacityTo)
        {
            var duration = new Duration(TimeSpan.FromMilliseconds(180));
            var ease     = new CubicEase { EasingMode = EasingMode.EaseOut };

            transform.BeginAnimation(
                System.Windows.Media.TranslateTransform.XProperty,
                new DoubleAnimation(from, to, duration) { EasingFunction = ease });

            element.BeginAnimation(OpacityProperty,
                new DoubleAnimation(opacityFrom, opacityTo, duration));
        }

        // ══════════════════════════════════════════════
        //  InfoPanel 이벤트 처리
        // ══════════════════════════════════════════════

        private void OnInfoPanel_Delete(FileNode node)
        {
            try
            {
                int idx = FileList.SelectedIndex;

                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    node.FullPath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                RemoveNodeFromTree(node);

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

                HideInfoPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"삭제 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnInfoPanel_Rename(FileNode node, string newName)
        {
            try
            {
                string dir = Path.GetDirectoryName(node.FullPath)!;
                string newPath = Path.Combine(dir, newName);
                File.Move(node.FullPath, newPath);

                // FileNode 갱신
                node.Name     = newName;
                node.FullPath = newPath;

                // InfoPanel 갱신
                InfoPanel.SetFile(node, ViewerArea.Content as UIElement);

                // 뷰어 재로드
                OpenFile(node);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이름 변경 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnInfoPanel_Move(FileNode node, string destFolder)
        {
            try
            {
                string newPath = Path.Combine(destFolder, node.Name);
                if (File.Exists(newPath))
                {
                    MessageBox.Show("대상 폴더에 같은 이름의 파일이 존재합니다.", "이동 실패",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                File.Move(node.FullPath, newPath);
                RemoveNodeFromTree(node);

                if (_vm.SelectedFile is FileNode folder && folder.IsDirectory)
                    RefreshFileList(folder);

                if (FileList.Items.Count > 0)
                    FileList.SelectedIndex = 0;
                else
                    ViewerArea.Content = new EmptyViewer("파일이 없습니다.");

                HideInfoPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이동 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════════════
        //  헬퍼 / 파일 로드
        // ══════════════════════════════════════════════

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
                FileCountText.Text = "로딩 중...";
                FileList.ItemsSource = null;
                await LoadChildrenRecursiveAsync(node);
                FileList.ItemsSource = GetAllNodes(node).Where(c => !c.IsDirectory);
            }
            else
            {
                FileList.ItemsSource = node.Children.Where(c => !c.IsDirectory);
            }

            FileCountText.Text = $"{FileList.Items.Count}개";
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;
            if (FileList.SelectedItem is not FileNode node) return;
            if (node.IsVirtual) return;
            if (ViewerArea.Content is EmptyViewer) return;

            try
            {
                int idx = FileList.SelectedIndex;

                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    node.FullPath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                RemoveNodeFromTree(node);

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

        private void ViewerArea_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) return;
            if (e.Handled) return;

            // LeftPanel이 열려 있고 마우스가 패널 영역 안이면 TreeView/ListView 자체 스크롤에 위임
            if (_isLeftPanelVisible)
            {
                double mouseX = e.GetPosition(ViewerContainer).X;
                if (mouseX < LeftPanelWidth) return;
            }

            int count = FileList.Items.Count;
            if (count == 0) return;

            int current = FileList.SelectedIndex;
            int next = e.Delta < 0
                ? Math.Min(current + 1, count - 1)
                : Math.Max(current - 1, 0);

            if (next == current) return;

            FileList.SelectedIndex = next;
            FileList.ScrollIntoView(FileList.SelectedItem);
            e.Handled = true;
        }

        private async void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not FileNode node) return;

            node.IsSelected = true;
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

            await Dispatcher.InvokeAsync(() =>
            {
                if (FileList.Items.Count > 0)
                {
                    // ZIP이 아닌 첫 파일을 자동 선택 (ZIP은 더블클릭 전까지 마운트 방지)
                    int selectIdx = 0;
                    for (int i = 0; i < FileList.Items.Count; i++)
                    {
                        if (FileList.Items[i] is FileNode fn &&
                            FileTypeHelper.GetViewerType(fn.Name) != ViewerType.Zip)
                        {
                            selectIdx = i;
                            break;
                        }
                    }
                    FileList.SelectedIndex = selectIdx;
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

        // ZIP 여부를 Magic Number 포함해 판별하는 헬퍼 (파일 16바이트만 읽음)
        private ViewerType DetectType(FileNode node)
        {
            if (node.IsVirtual && node.VirtualData != null)
            {
                int len = Math.Min(16, node.VirtualData.Length);
                return FileTypeHelper.GetViewerType(node.Name, node.VirtualData[..len]);
            }
            if (!node.IsVirtual && System.IO.File.Exists(node.FullPath))
            {
                byte[] header = new byte[16];
                int read;
                using (var fs = System.IO.File.OpenRead(node.FullPath))
                    read = fs.Read(header, 0, header.Length);
                return FileTypeHelper.GetViewerType(node.Name, header[..read]);
            }
            return FileTypeHelper.GetViewerType(node.Name);
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileList.SelectedItem is not FileNode node) return;
            if (node.IsDirectory) return;

            // ZIP은 선택 시 UnsupportedViewer(hex+meta)만 표시 — 실제 마운트는 DoubleClick
            if (DetectType(node) == ViewerType.Zip)
            {
                var viewer = new UnsupportedViewer(node, isZip: true);
                viewer.DeleteRequested += OnInfoPanel_Delete;
                ViewerArea.Content = viewer;
            }
            else
            {
                OpenFile(node);
            }

            // 패널이 열려 있으면 내용 즉시 갱신
            if (_isPanelVisible)
                InfoPanel.SetFile(node, ViewerArea.Content as UIElement);
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileList.SelectedItem is not FileNode node) return;
            if (node.IsDirectory) return;

            if (DetectType(node) == ViewerType.Zip)
            {
                // ZIP → 마운트
                ViewerArea.Content = OpenZip(node);
            }
            else
            {
                // 그 외 → 외부 뷰어
                InvokeExternalViewer(node);
            }
        }

        private void InvokeExternalViewer(FileNode node)
        {
            try
            {
                string targetPath;

                if (node.IsVirtual && node.VirtualData != null)
                {
                    var ext = Path.GetExtension(node.Name);
                    var tmp = Path.Combine(Path.GetTempPath(), $"TienViewer_{Guid.NewGuid()}{ext}");
                    File.WriteAllBytes(tmp, node.VirtualData);
                    App.RegisterTempFile(tmp);
                    targetPath = tmp;
                }
                else if (!node.IsVirtual && File.Exists(node.FullPath))
                {
                    targetPath = node.FullPath;
                }
                else return;

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = targetPath,
                    UseShellExecute = true,
                };
                try
                {
                    System.Diagnostics.Process.Start(psi);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    psi.Verb = "openas";
                    System.Diagnostics.Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"외부 앱 실행 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFile(FileNode node)
        {
            // DetectType 재사용 (ZIP은 SelectionChanged에서 이미 분기됨)
            var type = DetectType(node);

            if (type == ViewerType.Unsupported)
            {
                var hv = new HexViewer(node);
                hv.DeleteRequested += OnInfoPanel_Delete;
                ViewerArea.Content = hv;
            }
            else
            {
                ViewerArea.Content = type switch
                {
                    ViewerType.Image => new ImageViewer(node),
                    ViewerType.Text  => new TextViewer(node),
                    ViewerType.Pdf   => new PdfViewer(node),
                    ViewerType.Excel => new ExcelViewer(node),
                    ViewerType.Media => new MediaViewer(node),
                    _                => new HexViewer(node),
                };
            }
        }

        private UIElement OpenZip(FileNode node)
        {
            var virtualRoot = VirtualFileSystem.BuildFromZip(node.FullPath);
            _vm.RootNodes.Add(virtualRoot);

            // TreeView 렌더링 후 수동으로 선택 & FileList 로드
            Dispatcher.InvokeAsync(async () =>
            {
                virtualRoot.IsExpanded = true;
                virtualRoot.IsSelected = true;
                FolderTree.UpdateLayout();

                // FolderTree_SelectedItemChanged가 IsSelected 바인딩으로 안 오는 경우를 대비해 직접 호출
                await RefreshFileListAsync(virtualRoot);

                await Dispatcher.InvokeAsync(() =>
                {
                    if (FileList.Items.Count > 0)
                    {
                        FileList.SelectedIndex = 0;
                        FileList.ScrollIntoView(FileList.SelectedItem);
                    }
                }, System.Windows.Threading.DispatcherPriority.Loaded);

            }, System.Windows.Threading.DispatcherPriority.Loaded);

            return new EmptyViewer("ZIP 파일이 열렸습니다. 왼쪽 트리에서 탐색하세요.");
        }

        // ── 헬퍼 ──

        private void LoadChildrenRecursive(FileNode node)
        {
            if (!node.IsDirectory || node.IsVirtual) return;
            _vm.LoadChildren(node);
            foreach (var child in node.Children.Where(c => c.IsDirectory))
                LoadChildrenRecursive(child);
        }

        private void RefreshFileList(FileNode node)
        {
            if (node == null) return;
            bool showAll = ShowAllFilesToggle.IsChecked == true;

            if (showAll)
            {
                LoadChildrenRecursive(node);
                FileList.ItemsSource = GetAllNodes(node).Where(c => !c.IsDirectory);
            }
            else
            {
                FileList.ItemsSource = node.Children.Where(c => !c.IsDirectory);
            }

            FileCountText.Text = $"{FileList.Items.Count}개";
        }

        private void RemoveNodeFromTree(FileNode node)
        {
            var parent = _vm.RootNodes
                .SelectMany(r => GetAllNodes(r))
                .FirstOrDefault(n => n.Children.Contains(node));
            parent?.Children.Remove(node);
        }

        private IEnumerable<FileNode> GetAllNodes(FileNode node)
        {
            yield return node;
            foreach (var child in node.Children)
                foreach (var n in GetAllNodes(child))
                    yield return n;
        }
    }
}
