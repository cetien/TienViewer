using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace TienViewer
{
    public partial class App : Application
    {
        // UnsupportedViewer가 ZIP에서 추출한 임시 파일 목록
        private static readonly List<string> _tempFiles = new();

        public static void RegisterTempFile(string path)
        {
            lock (_tempFiles) { _tempFiles.Add(path); }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // EUC-KR, Shift-JIS 등 레거시 인코딩 활성화
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 앱 종료 시 임시 파일 일괄 삭제
            lock (_tempFiles)
            {
                foreach (var f in _tempFiles)
                {
                    try { if (File.Exists(f)) File.Delete(f); }
                    catch { /* 삭제 실패는 무시 */ }
                }
            }
            base.OnExit(e);
        }
    }
}
