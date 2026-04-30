using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TienViewer.Models;

namespace TienViewer.Views
{
    public partial class FileInfoPanel : UserControl
    {
        private FileNode? _node;

        public event Action<FileNode>? DeleteRequested;
        public event Action<FileNode, string>? RenameRequested;
        public event Action<FileNode, string>? MoveRequested;

        public FileInfoPanel()
        {
            InitializeComponent();
        }

        public void SetFile(FileNode? node, UIElement? currentViewer = null)
        {
            _node = node;
            if (node == null || node.IsDirectory)
            {
                ClearAll();
                return;
            }

            FileNameText.Text = node.Name;

            if (!node.IsVirtual && File.Exists(node.FullPath))
            {
                var info = new FileInfo(node.FullPath);
                FileSizeText.Text     = FormatSize(info.Length);
                FileModifiedText.Text = info.LastWriteTime.ToString("yyyy-MM-dd  HH:mm:ss");
                FilePathText.Text     = info.DirectoryName ?? "";
            }
            else if (node.IsVirtual && node.VirtualData != null)
            {
                FileSizeText.Text     = FormatSize(node.VirtualData.Length);
                FileModifiedText.Text = "(ZIP 내부)";
                FilePathText.Text     = "";
            }

            BtnExplorer.IsEnabled = !node.IsVirtual;
            BtnRename.IsEnabled   = !node.IsVirtual;
            BtnMove.IsEnabled     = !node.IsVirtual;
            BtnDelete.IsEnabled   = !node.IsVirtual;

            PopulateViewerProps(node, currentViewer);
        }

        private void PopulateViewerProps(FileNode node, UIElement? viewer)
        {
            ViewerPropsPanel.Children.Clear();

            var ext = Path.GetExtension(node.Name).ToLowerInvariant();

            if (ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp")
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    if (node.IsVirtual && node.VirtualData != null)
                        bmp.StreamSource = new MemoryStream(node.VirtualData);
                    else
                        bmp.UriSource = new Uri(node.FullPath);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();

                    AddProp("해상도", $"{bmp.PixelWidth} × {bmp.PixelHeight} px");
                    AddProp("DPI",    $"{bmp.DpiX:0} × {bmp.DpiY:0}");
                    AddProp("색 깊이", $"{bmp.Format.BitsPerPixel} bpp");
                }
                catch { AddProp("정보", "읽기 실패"); }
            }
            else if (ext == ".pdf")
            {
                try
                {
                    using Stream stream = node.IsVirtual && node.VirtualData != null
                        ? new MemoryStream(node.VirtualData)
                        : File.OpenRead(node.FullPath);
                    int pages = PDFtoImage.Conversion.GetPageCount(stream);
                    AddProp("페이지 수", $"{pages} 페이지");
                }
                catch { AddProp("페이지 수", "확인 불가"); }
            }
            else if (ext is ".xlsx" or ".xls")
            {
                try
                {
                    using Stream stream = node.IsVirtual && node.VirtualData != null
                        ? new MemoryStream(node.VirtualData)
                        : File.OpenRead(node.FullPath);
                    using var wb = new ClosedXML.Excel.XLWorkbook(stream);
                    AddProp("시트 수",   $"{wb.Worksheets.Count} 시트");
                    AddProp("시트 목록", string.Join(", ", wb.Worksheets.Select(s => s.Name)));
                }
                catch { AddProp("시트 수", "확인 불가"); }
            }
            else if (ext is ".txt" or ".log" or ".md" or ".cs" or ".xml" or ".json" or ".csv")
            {
                try
                {
                    byte[] data = node.IsVirtual && node.VirtualData != null
                        ? node.VirtualData
                        : File.ReadAllBytes(node.FullPath);

                    var detector = new Ude.CharsetDetector();
                    detector.Feed(data, 0, data.Length);
                    detector.DataEnd();
                    string enc = detector.Charset ?? "UTF-8";

                    var encoding = System.Text.Encoding.GetEncoding(enc);
                    int lines = encoding.GetString(data).Split('\n').Length;

                    AddProp("인코딩", enc);
                    AddProp("줄 수",  $"{lines:N0} 줄");
                }
                catch { AddProp("인코딩", "확인 불가"); }
            }
        }

        private void AddProp(string label, string value)
        {
            ViewerPropsPanel.Children.Add(new TextBlock
            {
                Text        = label,
                Foreground  = System.Windows.Media.Brushes.Gray,
                FontSize    = 11,
                Margin      = new Thickness(0, 4, 0, 1)
            });
            ViewerPropsPanel.Children.Add(new TextBlock
            {
                Text         = value,
                Foreground   = System.Windows.Media.Brushes.WhiteSmoke,
                FontSize     = 12,
                TextWrapping = TextWrapping.Wrap
            });
        }

        private void ClearAll()
        {
            FileNameText.Text     = "";
            FileSizeText.Text     = "";
            FileModifiedText.Text = "";
            FilePathText.Text     = "";
            ViewerPropsPanel.Children.Clear();
        }

        // ── 이벤트 핸들러 ──

        private void FilePath_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_node == null || string.IsNullOrEmpty(_node.FullPath)) return;
            var dir = Path.GetDirectoryName(_node.FullPath);
            if (dir != null) Process.Start("explorer.exe", dir);
        }

        private void BtnExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (_node == null) return;
            Process.Start("explorer.exe", $"/select,\"{_node.FullPath}\"");
        }

        private void BtnRename_Click(object sender, RoutedEventArgs e)
        {
            if (_node == null) return;
            var dlg = new RenameDialog(_node.Name) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true && dlg.NewName != _node.Name)
                RenameRequested?.Invoke(_node, dlg.NewName);
        }

        private void BtnMove_Click(object sender, RoutedEventArgs e)
        {
            if (_node == null) return;

            // .NET 8 WPF 네이티브 폴더 선택 다이얼로그
            var dlg = new Microsoft.Win32.OpenFolderDialog
            {
                Title            = "이동할 폴더를 선택하세요.",
                InitialDirectory = Path.GetDirectoryName(_node.FullPath) ?? ""
            };

            if (dlg.ShowDialog() == true)
                MoveRequested?.Invoke(_node, dlg.FolderName);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_node == null) return;
            DeleteRequested?.Invoke(_node);
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)                return $"{bytes} B";
            if (bytes < 1024 * 1024)         return $"{bytes / 1024.0:0.#} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024:0.##} MB";
            return $"{bytes / 1024.0 / 1024 / 1024:0.##} GB";
        }
    }
}
