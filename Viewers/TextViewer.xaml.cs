using System.IO;
using System.Text;
using System.Windows.Controls;

using TienViewer.Models;

using Ude;

namespace TienViewer.Viewers
{
	public partial class TextViewer : UserControl
	{
		public TextViewer(FileNode node)
		{
			InitializeComponent();

			byte[] data = node.IsVirtual && node.VirtualData != null
				? node.VirtualData
				: File.ReadAllBytes(node.FullPath);

			var encoding = DetectEncoding(data) ?? Encoding.UTF8;
			TextContent.Text = encoding.GetString(data);
		}

		private static Encoding? DetectEncoding(byte[] data)
		{
			try
			{
				var detector = new CharsetDetector();
				detector.Feed(data, 0, data.Length);
				detector.DataEnd();

				if (detector.Charset == null) return null;

				// 감지된 인코딩명 정규화
				var charsetName = detector.Charset.ToUpperInvariant() switch
				{
					"EUC-KR" or "EUCKR" => "EUC-KR",
					"UTF-8" => "UTF-8",
					"UTF-16LE" => "UTF-16",
					"SHIFT_JIS" => "Shift-JIS",
					_ => detector.Charset
				};

				return Encoding.GetEncoding(charsetName);
			}
			catch
			{
				return Encoding.UTF8; // 감지 실패 시 UTF-8 폴백
			}
		}
	}
}