using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TienViewer.Helpers
{
	public enum ViewerType
	{
		Image, Text, Pdf, Excel, Zip, Unsupported
	}

	public static class FileTypeHelper
	{
		public static ViewerType GetViewerType(string fileName)
		{
			var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
			return ext switch
			{
				".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp" => ViewerType.Image,
				".txt" or ".log" or ".md" or ".cs" or ".xml" or ".json" or ".csv" => ViewerType.Text,
				".pdf" => ViewerType.Pdf,
				".xlsx" or ".xls" => ViewerType.Excel,
				".zip" => ViewerType.Zip,
				_ => ViewerType.Unsupported
			};
		}
	}
}