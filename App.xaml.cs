using System.Text;
using System.Windows;

namespace TienViewer
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			// EUC-KR, Shift-JIS 등 레거시 인코딩 활성화
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			base.OnStartup(e);
		}
	}
}