
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TienViewer.Models;

namespace TienViewer.Viewers
{
	public partial class ImageViewer : UserControl
	{
		public ImageViewer(FileNode node)
		{
			InitializeComponent();
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
	}
}