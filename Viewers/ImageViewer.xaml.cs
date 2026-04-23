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
		// Zoom (마우스 휠 보정)
		// =========================
		private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (MainImage.Source == null) return;

			double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
			double newScale = Math.Clamp(_scale * zoomFactor, MinScale, MaxScale);

			// 현재 마우스 위치(RootGrid 기준)를 유지하며 확대/축소
			Point relative = e.GetPosition(MainImage);
			double abosoluteX = relative.X * _scale + TranslateTransform.X;
			double abosoluteY = relative.Y * _scale + TranslateTransform.Y;

			_scale = newScale;
			ScaleTransform.ScaleX = _scale;
			ScaleTransform.ScaleY = _scale;

			// 마우스 지점 보정
			TranslateTransform.X = abosoluteX - relative.X * _scale;
			TranslateTransform.Y = abosoluteY - relative.Y * _scale;
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
			// Stretch="Uniform" 덕분에 변환값만 초기화하면 창에 꽉 찹니다.
			_scale = 1.0;
			ScaleTransform.ScaleX = 1.0;
			ScaleTransform.ScaleY = 1.0;
			TranslateTransform.X = 0;
			TranslateTransform.Y = 0;
		}
	}
}