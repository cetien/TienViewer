using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using PDFtoImage;

using SkiaSharp;

using TienViewer.Models;

namespace TienViewer.Viewers
{
    public partial class PdfViewer : UserControl
    {
        private byte[] _pdfData = [];
        private int    _currentPage = 0;
        private int    _pageCount   = 0;

        // ── Zoom ──────────────────────────────────────
        private double _zoom       = 1.0;
        private const double ZoomStep = 0.15;
        private const double ZoomMin  = 0.2;
        private const double ZoomMax  = 5.0;

        // ── Pan (drag) ────────────────────────────────
        private bool  _isPanning   = false;
        private Point _panStart;
        private double _panScrollX, _panScrollY;

        public PdfViewer(FileNode node)
        {
            InitializeComponent();

            // Pan 이벤트 등록 — Preview(터널링)로 ScrollViewer 내부 소비 우회
            Scroll.PreviewMouseLeftButtonDown += Scroll_MouseLeftButtonDown;
            Scroll.PreviewMouseLeftButtonUp   += Scroll_MouseLeftButtonUp;
            Scroll.PreviewMouseMove           += Scroll_MouseMove;
            Scroll.MouseLeave                 += Scroll_MouseLeave;

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

        // ── 페이지 렌더 ───────────────────────────────

        private void ShowPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _pageCount) return;

            _currentPage = pageIndex;
            PageInfo.Text = $"{_currentPage + 1} / {_pageCount}";

            using var ms = new MemoryStream(_pdfData);
            using var skBitmap = Conversion.ToImage(ms, page: pageIndex);
            using var outMs = new MemoryStream();

            skBitmap.Encode(outMs, SKEncodedImageFormat.Png, 100);
            outMs.Seek(0, SeekOrigin.Begin);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = outMs;
            bitmap.CacheOption  = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            PageImage.Source = bitmap;
            ApplyZoom();
        }

        // ── Zoom ──────────────────────────────────────

        private void ApplyZoom()
        {
            ImageScale.ScaleX = _zoom;
            ImageScale.ScaleY = _zoom;
            ZoomText.Text = $"{(int)Math.Round(_zoom * 100)}%";
        }

        private void SetZoom(double newZoom, Point? anchor = null)
        {
            newZoom = Math.Clamp(newZoom, ZoomMin, ZoomMax);
            if (Math.Abs(newZoom - _zoom) < 0.001) return;

            // 마우스 위치를 anchor로 ScrollViewer 오프셋 보정
            if (anchor.HasValue)
            {
                double ratioX = (Scroll.HorizontalOffset + anchor.Value.X) / (_zoom * (PageImage.ActualWidth  > 0 ? PageImage.ActualWidth  : 1));
                double ratioY = (Scroll.VerticalOffset   + anchor.Value.Y) / (_zoom * (PageImage.ActualHeight > 0 ? PageImage.ActualHeight : 1));

                _zoom = newZoom;
                ApplyZoom();

                // 레이아웃 반영 후 스크롤 위치 재조정
                Scroll.UpdateLayout();
                Scroll.ScrollToHorizontalOffset(ratioX * _zoom * PageImage.ActualWidth  - anchor.Value.X);
                Scroll.ScrollToVerticalOffset  (ratioY * _zoom * PageImage.ActualHeight - anchor.Value.Y);
            }
            else
            {
                _zoom = newZoom;
                ApplyZoom();
            }
        }

        // ── 이벤트 핸들러 ─────────────────────────────

        private void BtnPrev_Click(object sender, RoutedEventArgs e)  => ShowPage(_currentPage - 1);
        private void BtnNext_Click(object sender, RoutedEventArgs e)  => ShowPage(_currentPage + 1);

        private void BtnZoomIn_Click   (object sender, RoutedEventArgs e) => SetZoom(_zoom + ZoomStep);
        private void BtnZoomOut_Click  (object sender, RoutedEventArgs e) => SetZoom(_zoom - ZoomStep);
        private void BtnZoomReset_Click(object sender, RoutedEventArgs e) => SetZoom(1.0);

        // ── Pan ───────────────────────────────────────

        private void Scroll_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPanning  = true;
            _panStart   = e.GetPosition(Scroll);
            _panScrollX = Scroll.HorizontalOffset;
            _panScrollY = Scroll.VerticalOffset;
            Scroll.CaptureMouse();
            Scroll.Cursor = Cursors.Hand;
            e.Handled = true;
        }

        private void Scroll_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopPan();
        }

        private void Scroll_MouseLeave(object sender, MouseEventArgs e)
        {
            StopPan();
        }

        private void Scroll_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;

            var pos   = e.GetPosition(Scroll);
            double dx = _panStart.X - pos.X;
            double dy = _panStart.Y - pos.Y;

            Scroll.ScrollToHorizontalOffset(_panScrollX + dx);
            Scroll.ScrollToVerticalOffset  (_panScrollY + dy);
            e.Handled = true;
        }

        private void StopPan()
        {
            if (!_isPanning) return;
            _isPanning    = false;
            Scroll.ReleaseMouseCapture();
            Scroll.Cursor = Cursors.Arrow;
        }

        private void Scroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var anchor = e.GetPosition(Scroll);
                SetZoom(_zoom + (e.Delta > 0 ? ZoomStep : -ZoomStep), anchor);
                e.Handled = true;   // ScrollViewer 기본 스크롤 차단
            }
            // Ctrl 없으면 ScrollViewer가 기본 세로 스크롤 처리
        }
    }
}
