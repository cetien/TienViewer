using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using TienViewer.Models;

namespace TienViewer.Viewers
{
    public partial class UnsupportedViewer : UserControl
    {
        private readonly FileNode _node;

        /// <summary>삭제 요청 — MainWindow의 OnInfoPanel_Delete 패턴으로 처리</summary>
        public event Action<FileNode>? DeleteRequested;

        // ── 생성자 ──────────────────────────────────────────

        public UnsupportedViewer(FileNode node, bool isZip = false)
        {
            InitializeComponent();
            _node = node;

            // 메타 정보 표시
            FileNameText.Text = node.Name;
            LoadMeta(node);

            // ZIP 여부와 무관하게 Mount 버튼은 숨김 (마운트는 FileList DoubleClick에서 처리)
            MountButton.Visibility = Visibility.Collapsed;

            // Virtual(ZIP 내부) 파일이면 Delete 비활성화
            if (node.IsVirtual)
            {
                DeleteButton.IsEnabled = false;
                DeleteButton.ToolTip   = "ZIP 내부 파일은 직접 삭제할 수 없습니다.";
            }

            // Hex dump 로드 (최대 1 KB)
            LoadHex(node);
        }

        // ── 메타 로드 ────────────────────────────────────────

        private void LoadMeta(FileNode node)
        {
            if (node.IsVirtual && node.VirtualData != null)
            {
                FileSizeText.Text    = FormatSize(node.VirtualData.Length);
                FileCreatedText.Text = "(ZIP 내부)";
            }
            else if (!node.IsVirtual && File.Exists(node.FullPath))
            {
                var info = new FileInfo(node.FullPath);
                FileSizeText.Text    = FormatSize(info.Length);
                FileCreatedText.Text = info.CreationTime.ToString("yyyy-MM-dd");
            }
            else
            {
                FileSizeText.Text    = "–";
                FileCreatedText.Text = "–";
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)        return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return                          $"{bytes / (1024.0 * 1024):F1} MB";
        }

        // ── Hex Dump ─────────────────────────────────────────

        private void LoadHex(FileNode node)
        {
            byte[] data = ReadFirst1K(node);
            HexLines.ItemsSource = BuildHexLines(data);
        }

        private static byte[] ReadFirst1K(FileNode node)
        {
            const int max = 1024;
            if (node.IsVirtual && node.VirtualData != null)
            {
                int len = Math.Min(max, node.VirtualData.Length);
                return node.VirtualData[..len];
            }
            if (!node.IsVirtual && File.Exists(node.FullPath))
            {
                using var fs = File.OpenRead(node.FullPath);
                var buf = new byte[max];
                int read = fs.Read(buf, 0, max);
                return buf[..read];
            }
            return Array.Empty<byte>();
        }

        /// <summary>
        /// 16바이트/행 형식:
        /// 00000000  50 4B 03 04 14 00 00 00  00 00 8B 52 39 59 00 00   PK.....????9Y..
        /// </summary>
        private static List<string> BuildHexLines(byte[] data)
        {
            var lines = new List<string>();
            const int rowSize = 16;

            for (int offset = 0; offset < data.Length; offset += rowSize)
            {
                var sb = new StringBuilder();

                // Offset
                sb.Append($"{offset:X8}  ");

                int end = Math.Min(offset + rowSize, data.Length);

                // Hex — 8+8 with gap
                for (int i = offset; i < offset + rowSize; i++)
                {
                    if (i == offset + 8) sb.Append(' ');   // 8바이트 중간 공백
                    if (i < end)
                        sb.Append($"{data[i]:X2} ");
                    else
                        sb.Append("   ");
                }

                sb.Append("  ");

                // ASCII
                for (int i = offset; i < end; i++)
                {
                    char c = data[i] is >= 0x20 and <= 0x7E ? (char)data[i] : '.';
                    sb.Append(c);
                }

                lines.Add(sb.ToString());
            }

            if (data.Length == 0)
                lines.Add("(데이터 없음)");

            return lines;
        }

        // ── 버튼 이벤트 ──────────────────────────────────────

        private void Mount_Click(object sender, RoutedEventArgs e) { /* DoubleClick에서 처리 */ }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_node.IsVirtual) return;

            var result = MessageBox.Show(
                $"'{_node.Name}' 을(를) 휴지통으로 삭제하시겠습니까?",
                "삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
                DeleteRequested?.Invoke(_node);
        }

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
                psi.Verb = "openas";
                try { Process.Start(psi); } catch { }
            }
        }
    }
}
