
namespace TienViewer.Viewers
{
	public partial class EmptyViewer : System.Windows.Controls.UserControl
	{
		public EmptyViewer(string message = "파일을 선택하세요.")
		{
			InitializeComponent();
			MessageText.Text = message;
		}
	}
}