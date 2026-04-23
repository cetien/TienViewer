using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using PDFtoImage;

using SkiaSharp;

using TienViewer.Models;

namespace TienViewer.Viewers
{
	public partial class PdfViewer : UserControl
	{
		private byte[] _pdfData = [];
		private int _currentPage = 0;
		private int _pageCount = 0;

		public PdfViewer(FileNode node)
		{
			InitializeComponent();

			try
			{
				_pdfData = node.IsVirtual && node.VirtualData != null
					? node.VirtualData
					: File.ReadAllBytes(node.FullPath);

				using var ms = new MemoryStream(_pdfData);
				_pageCount = Conversion.GetPageCount(ms);

				ShowPage(0);
			}
			catch (Exception ex)
			{
				PageInfo.Text = $"PDF 로드 실패: {ex.Message}";
			}
		}

		private void ShowPage(int pageIndex)
		{
			if (pageIndex < 0 || pageIndex >= _pageCount) return;

			_currentPage = pageIndex;
			PageInfo.Text = $"{_currentPage + 1} / {_pageCount}";

			using var ms = new MemoryStream(_pdfData);

			// v5.x API: 두 번째 인수가 page (0-based)
			using var skBitmap = Conversion.ToImage(ms, page: pageIndex);

			using var outMs = new MemoryStream();
			skBitmap.Encode(outMs, SKEncodedImageFormat.Png, 100);
			outMs.Seek(0, SeekOrigin.Begin);

			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.StreamSource = outMs;
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();

			PageImage.Source = bitmap;
		}

		private void BtnPrev_Click(object sender, RoutedEventArgs e) => ShowPage(_currentPage - 1);
		private void BtnNext_Click(object sender, RoutedEventArgs e) => ShowPage(_currentPage + 1);
	}
}