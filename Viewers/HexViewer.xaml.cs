using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;

using TienViewer.Models;

namespace TienViewer.Viewers
{
    public partial class HexViewer : UserControl
    {
        // UnsupportedViewer와 동일한 삭제 이벤트 — MainWindow 패턴 호환
        public event Action<FileNode>? DeleteRequested;

        public HexViewer(FileNode node)
        {
            InitializeComponent();
            HexLines.ItemsSource = BuildHexLines(ReadBytes(node));
        }

        // ── 데이터 읽기 ───────────────────────────────────────────────────────

        private static byte[] ReadBytes(FileNode node)
        {
            if (node.IsVirtual && node.VirtualData != null)
                return node.VirtualData;

            if (!node.IsVirtual && File.Exists(node.FullPath))
            {
                using var fs = new FileStream(
                    node.FullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);
                using var ms = new MemoryStream();
                fs.CopyTo(ms);
                return ms.ToArray();
            }

            return Array.Empty<byte>();
        }

        // ── Hex Dump 생성 ─────────────────────────────────────────────────────

        /// <summary>
        /// 16바이트/행 형식:
        /// 00000000  50 4B 03 04 14 00 00 00  08 09 0A 0B 0C 0D 0E 0F   PK.....????....
        /// </summary>
        private static List<string> BuildHexLines(byte[] data)
        {
            var lines   = new List<string>();
            const int rowSize = 16;

            for (int offset = 0; offset < data.Length; offset += rowSize)
            {
                var sb  = new StringBuilder();
                int end = Math.Min(offset + rowSize, data.Length);

                // Offset
                sb.Append($"{offset:X8}  ");

                // Hex bytes — 8+8 with middle gap
                for (int i = offset; i < offset + rowSize; i++)
                {
                    if (i == offset + 8) sb.Append(' ');
                    sb.Append(i < end ? $"{data[i]:X2} " : "   ");
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
    }
}
