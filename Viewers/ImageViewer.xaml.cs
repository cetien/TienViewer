/*
ScrollViewer (스크롤은 비활성화)
 └── Border (클리핑)
      └── Image
           └── TransformGroup
                ├── ScaleTransform  (Zoom)
                └── TranslateTransform (Pan)

 
✔ 마우스 휠 → 확대/축소
✔ 포인터 기준 zoom
✔ 드래그 → 이동
✔ 더블클릭 → Fit
✔ 창 크기 변경 → 자동 Fit
✔ 부드러운 확대 애니메이션
 */

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

using TienViewer.Models;

namespace TienViewer.Viewers
{
	public partial class ImageViewer : UserControl
	{
		private double _scale = 1.0;
		private const double MinScale = 0.05;
		private const double MaxScale = 20.0;

		private Point _start;
		private Point _origin;
		private bool _isDragging = false;

		public ImageViewer(FileNode node)
		{
			InitializeComponent();

			LoadImage(node);

			//Loaded += (_, __) => FitToWindow();
			Loaded += (_, __) =>
			{
				Dispatcher.BeginInvoke(new Action(FitToWindow),
					System.Windows.Threading.DispatcherPriority.Loaded);
			};

			//SizeChanged += (_, __) => FitToWindow();
			SizeChanged += (_, __) =>
			{
				if (_scale == 1.0) // 초기 상태만
					FitToWindow();
			};
		}

		private void LoadImage(FileNode node)
		{
			var bitmap = new BitmapImage();
			bitmap.BeginInit();

			if (node.IsVirtual && node.VirtualData != null)
				bitmap.StreamSource = new MemoryStream(node.VirtualData);
			else
				bitmap.UriSource = new Uri(node.FullPath);

			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();

			MainImage.Source = bitmap;
		}

		// =========================
		// Zoom (마우스 휠, 포인터 기준)
		// =========================
		private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (MainImage.Source == null)
				return;

			double zoomFactor = e.Delta > 0 ? 1.2 : 0.8;

			// ⭐ 기준 좌표: RootGrid
			var position = e.GetPosition(RootGrid);

			double newScale = Math.Clamp(_scale * zoomFactor, MinScale, MaxScale);

			// ⭐ 핵심 공식 (안정 버전)
			double dx = position.X - TranslateTransform.X;
			double dy = position.Y - TranslateTransform.Y;

			TranslateTransform.X -= dx * (newScale / _scale - 1);
			TranslateTransform.Y -= dy * (newScale / _scale - 1);

			_scale = newScale;

			// ⭐ 애니메이션 제거 (정확성 확보)
			ScaleTransform.ScaleX = _scale;
			ScaleTransform.ScaleY = _scale;
		}

		private void AnimateScale(double targetScale)
		{
			var anim = new DoubleAnimation
			{
				To = targetScale,
				Duration = TimeSpan.FromMilliseconds(120),
				EasingFunction = new QuadraticEase()
			};

			ScaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
			ScaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, anim);
		}

		// =========================
		// Pan (드래그 이동)
		// =========================
		private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				FitToWindow();
				return;
			}

			_isDragging = true;
			_start = e.GetPosition(this);
			_origin = new Point(TranslateTransform.X, TranslateTransform.Y);

			MainImage.CaptureMouse();
			Cursor = Cursors.Hand;
		}

		private void Image_MouseMove(object sender, MouseEventArgs e)
		{
			if (!_isDragging)
				return;

			Vector v = e.GetPosition(this) - _start;

			TranslateTransform.X = _origin.X + v.X;
			TranslateTransform.Y = _origin.Y + v.Y;
		}

		private void Image_MouseLeftButtonUp(object sender, MouseEventArgs e)
		{
			_isDragging = false;
			MainImage.ReleaseMouseCapture();
			Cursor = Cursors.Arrow;
		}

		// =========================
		// Fit to Window
		// =========================
public void FitToWindow()
{
    if (MainImage.Source is not BitmapSource bmp)
        return;

    double containerWidth = RootGrid.ActualWidth;
    double containerHeight = RootGrid.ActualHeight;

    if (containerWidth <= 0 || containerHeight <= 0)
        return;

    double imageWidth = bmp.PixelWidth * (96.0 / bmp.DpiX);
    double imageHeight = bmp.PixelHeight * (96.0 / bmp.DpiY);

    double scaleX = containerWidth / imageWidth;
    double scaleY = containerHeight / imageHeight;

    _scale = Math.Min(scaleX, scaleY);

    ScaleTransform.ScaleX = _scale;
    ScaleTransform.ScaleY = _scale;

    // ⭐ 중앙 기준이면 Translate 0
    TranslateTransform.X = 0;
    TranslateTransform.Y = 0;
}
	}
}