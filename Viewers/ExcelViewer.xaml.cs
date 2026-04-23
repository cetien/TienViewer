using System.Data;
using System.Windows.Controls;

using ClosedXML.Excel;

using TienViewer.Models;

namespace TienViewer.Viewers
{
	public partial class ExcelViewer : UserControl
	{
		private readonly List<(string SheetName, DataTable Data)> _sheets = new();

		public ExcelViewer(FileNode node)
		{
			InitializeComponent();

			try
			{
				System.IO.Stream stream;
				if (node.IsVirtual && node.VirtualData != null)
					stream = new System.IO.MemoryStream(node.VirtualData);
				else
					stream = System.IO.File.OpenRead(node.FullPath);

				using (stream)
				using (var workbook = new XLWorkbook(stream))
				{
					foreach (var worksheet in workbook.Worksheets)
					{
						var table = WorksheetToDataTable(worksheet);
						_sheets.Add((worksheet.Name, table));

						var tab = new TabItem { Header = worksheet.Name };
						SheetTabs.Items.Add(tab);
					}

					if (_sheets.Count > 0)
					{
						SheetTabs.SelectedIndex = 0;
						DataGrid.ItemsSource = _sheets[0].Data.DefaultView;
					}
				}
			}
			catch (Exception ex)
			{
				DataGrid.ItemsSource = null;
				// 오류 표시
				var errorTable = new DataTable();
				errorTable.Columns.Add("오류");
				errorTable.Rows.Add(ex.Message);
				DataGrid.ItemsSource = errorTable.DefaultView;
			}
		}

		private static DataTable WorksheetToDataTable(IXLWorksheet worksheet)
		{
			var table = new DataTable();
			var range = worksheet.RangeUsed();
			if (range == null) return table;

			// 첫 행을 헤더로 사용
			var firstRow = range.FirstRow();
			foreach (var cell in firstRow.Cells())
			{
				var colName = cell.GetString();
				table.Columns.Add(string.IsNullOrWhiteSpace(colName)
					? $"열{cell.WorksheetColumn().ColumnNumber()}"
					: colName);
			}

			// 나머지 행 데이터
			foreach (var row in range.Rows().Skip(1))
			{
				var dataRow = table.NewRow();
				int i = 0;
				foreach (var cell in row.Cells())
				{
					if (i >= table.Columns.Count) break;
					dataRow[i++] = cell.GetString();
				}
				table.Rows.Add(dataRow);
			}

			return table;
		}

		private void SheetTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var idx = SheetTabs.SelectedIndex;
			if (idx >= 0 && idx < _sheets.Count)
				DataGrid.ItemsSource = _sheets[idx].Data.DefaultView;
		}
	}
}