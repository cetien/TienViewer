using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

		// WPF Stretch=Uniform이 배치한 이미지의 좌상단 오프셋
		private double _baseOffsetX = 0;
		private double _baseOffsetY = 0;

		public ImageViewer(FileNode node)
		{
			InitializeComponent();
			LoadImage(node);

			Loaded += (_, __) =>
				Dispatcher.BeginInvoke(new Action(ResetView),
					System.Windows.Threading.DispatcherPriority.Render);

			SizeChanged += (_, __) =>
			{
				if (_scale == 1.0) ResetView();
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

		// 기준 오프셋 계산 (Stretch=Uniform 후 이미지 실제 좌상단)
		private void UpdateBaseOffset()
		{
			double cW = RootGrid.ActualWidth;
			double cH = RootGrid.ActualHeight;
			_baseOffsetX = (cW - MainImage.ActualWidth) / 2.0;
			_baseOffsetY = (cH - MainImage.ActualHeight) / 2.0;
		}

		// scale=1, TX=TY=0 리셋 (Stretch가 자동 Fit+중앙배치)
		public void ResetView()
		{
			_scale = 1.0;
			ScaleTransform.ScaleX = 1.0;
			ScaleTransform.ScaleY = 1.0;
			TranslateTransform.X = 0;
			TranslateTransform.Y = 0;
			UpdateBaseOffset();
		}

		// 화면 좌표 → 이미지 로컬 좌표
		private Point ScreenToImage(Point screen)
		{
			return new Point(
				(screen.X - _baseOffsetX - TranslateTransform.X) / _scale,
				(screen.Y - _baseOffsetY - TranslateTransform.Y) / _scale);
		}

		// 이미지가 화면에서 차지하는 Rect
		private Rect GetImageScreenRect()
		{
			double w = MainImage.ActualWidth * _scale;
			double h = MainImage.ActualHeight * _scale;
			double x = _baseOffsetX + TranslateTransform.X;
			double y = _baseOffsetY + TranslateTransform.Y;
			return new Rect(x, y, w, h);
		}

		private void ClampTranslate()
		{
			double imgW = MainImage.ActualWidth * _scale;
			double imgH = MainImage.ActualHeight * _scale;
			double cW = RootGrid.ActualWidth;
			double cH = RootGrid.ActualHeight;

			if (imgW <= cW)
				TranslateTransform.X = (cW - imgW) / 2.0 - _baseOffsetX;
			else
				TranslateTransform.X = Math.Clamp(TranslateTransform.X,
					cW - imgW - _baseOffsetX,
					-_baseOffsetX);

			if (imgH <= cH)
				TranslateTransform.Y = (cH - imgH) / 2.0 - _baseOffsetY;
			else
				TranslateTransform.Y = Math.Clamp(TranslateTransform.Y,
					cH - imgH - _baseOffsetY,
					-_baseOffsetY);
		}

		// =========================
		// Zoom (Ctrl+Wheel)
		// =========================
		private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
				return;

			e.Handled = true;
			if (MainImage.Source == null) return;

			double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
			double newScale = Math.Clamp(_scale * zoomFactor, MinScale, MaxScale);
			if (newScale == _scale) return;

			Point mouseOnContainer = e.GetPosition(RootGrid);
			Rect imageRect = GetImageScreenRect();

			Point pivot = imageRect.Contains(mouseOnContainer)
				? mouseOnContainer
				: new Point(RootGrid.ActualWidth / 2.0, RootGrid.ActualHeight / 2.0);

			// pivot 기준 이미지 로컬 좌표
			double imgX = (pivot.X - _baseOffsetX - TranslateTransform.X) / _scale;
			double imgY = (pivot.Y - _baseOffsetY - TranslateTransform.Y) / _scale;

			_scale = newScale;
			ScaleTransform.ScaleX = _scale;
			ScaleTransform.ScaleY = _scale;

			TranslateTransform.X = pivot.X - _baseOffsetX - imgX * _scale;
			TranslateTransform.Y = pivot.Y - _baseOffsetY - imgY * _scale;

			ClampTranslate();
		}

		// =========================
		// Pan
		// =========================
		private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
				{
					ResetView();
					e.Handled = true;
				}
				return;
			}

			_isDragging = true;
			_start = e.GetPosition(RootGrid);
			_origin = new Point(TranslateTransform.X, TranslateTransform.Y);
			RootGrid.CaptureMouse();
			Cursor = Cursors.Hand;
		}

		private void Image_MouseMove(object sender, MouseEventArgs e)
		{
			if (!_isDragging) return;
			Vector v = e.GetPosition(RootGrid) - _start;
			TranslateTransform.X = _origin.X + v.X;
			TranslateTransform.Y = _origin.Y + v.Y;
			ClampTranslate();
		}

		private void Image_MouseLeftButtonUp(object sender, MouseEventArgs e)
		{
			_isDragging = false;
			RootGrid.ReleaseMouseCapture();
			Cursor = Cursors.Arrow;
		}
	}
}