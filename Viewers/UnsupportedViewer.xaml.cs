using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using TienViewer.Models;

namespace TienViewer.Viewers
{
    public partial class UnsupportedViewer : UserControl
    {
        private readonly FileNode _node;

        // MainWindow가 구독해서 OpenZip 실행
        public event Action<FileNode>? MountRequested;

        public UnsupportedViewer(FileNode node, bool isZip = false)
        {
            InitializeComponent();
            _node = node;

            FileNameText.Text = node.Name;

            if (isZip)
            {
                IconText.Text        = "🗜️";
                DescText.Text        = "ZIP 아카이브입니다.";
                MountButton.Visibility = Visibility.Visible;
                HintText.Text        = "마운트하면 왼쪽 트리에서 내용을 탐색할 수 있습니다.";
                HintText.Visibility  = Visibility.Visible;
            }
        }

        // ── ZIP 마운트 버튼 ─────────────────────────────
        private void Mount_Click(object sender, RoutedEventArgs e)
        {
            MountRequested?.Invoke(_node);
        }

        // ── Windows 기본 앱으로 열기 ────────────────────
        private void OpenExternal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string targetPath;

                if (_node.IsVirtual && _node.VirtualData != null)
                {
                    var ext = Path.GetExtension(_node.Name);
                    var tmp = Path.Combine(
                        Path.GetTempPath(),
                        $"TienViewer_{Guid.NewGuid()}{ext}");
                    File.WriteAllBytes(tmp, _node.VirtualData);
                    App.RegisterTempFile(tmp);
                    targetPath = tmp;
                }
                else
                {
                    targetPath = _node.FullPath;
                }

                OpenWithShell(targetPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"외부 앱 실행 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenWithShell(string path)
        {
            var psi = new ProcessStartInfo
            {
                FileName        = path,
                UseShellExecute = true,
            };

            try
            {
                Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                HintText.Text       = "이 파일 형식에 연결된 앱이 없습니다.";
                HintText.Visibility = Visibility.Visible;
                psi.Verb = "openas";
                try { Process.Start(psi); } catch { }
            }
        }
    }
}
