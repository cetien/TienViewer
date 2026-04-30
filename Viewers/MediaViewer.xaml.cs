using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using TienViewer.Models;

namespace TienViewer.Viewers
{
    public partial class MediaViewer : UserControl
    {
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
        private bool _isSeeking  = false;
        private bool _isPlaying  = false;
        private string _tempPath = "";

        public MediaViewer(FileNode node)
        {
            InitializeComponent();

            Media.Volume = VolumeBar.Value;

            string path = ResolveMediaPath(node);
            if (string.IsNullOrEmpty(path)) return;

            Media.Source = new Uri(path, UriKind.Absolute);
            Media.Play();
            _isPlaying = true;
            BtnPlayPause.Content = "⏸";

            _timer.Tick += Timer_Tick;
            _timer.Start();

            // UserControl 언로드 시 정리
            Unloaded += (_, _) =>
            {
                _timer.Stop();
                Media.Stop();
                Media.Source = null;
                CleanupTemp();
            };
        }

        // ── 경로 해석 ─────────────────────────────────

        private string ResolveMediaPath(FileNode node)
        {
            if (!node.IsVirtual)
                return node.FullPath;

            // ZIP 내부 파일 → 임시 파일로 추출
            if (node.VirtualData == null) return "";

            var ext  = Path.GetExtension(node.Name);
            _tempPath = Path.Combine(Path.GetTempPath(), $"TienViewer_{Guid.NewGuid()}{ext}");
            File.WriteAllBytes(_tempPath, node.VirtualData);
            App.RegisterTempFile(_tempPath);
            return _tempPath;
        }

        private void CleanupTemp()
        {
            if (string.IsNullOrEmpty(_tempPath)) return;
            try { if (File.Exists(_tempPath)) File.Delete(_tempPath); } catch { }
        }

        // ── MediaElement 이벤트 ───────────────────────

        private void Media_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (Media.NaturalDuration.HasTimeSpan)
                SeekBar.Maximum = Media.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void Media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // 코덱 없음 등 — 텍스트로 표시
            TimeText.Text = $"재생 실패: {e.ErrorException?.Message ?? "알 수 없는 오류"}";
        }

        private void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            Media.Stop();
            Media.Position = TimeSpan.Zero;
            _isPlaying = false;
            BtnPlayPause.Content = "▶";
        }

        // ── 타이머 ────────────────────────────────────

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isSeeking || !_isPlaying) return;

            SeekBar.Value = Media.Position.TotalSeconds;

            var pos = Media.Position;
            var dur = Media.NaturalDuration.HasTimeSpan
                ? Media.NaturalDuration.TimeSpan
                : TimeSpan.Zero;

            TimeText.Text = $"{Format(pos)} / {Format(dur)}";
        }

        private static string Format(TimeSpan t)
            => t.TotalHours >= 1
               ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
               : $"{t.Minutes}:{t.Seconds:D2}";

        // ── 컨트롤 이벤트 ─────────────────────────────

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                Media.Pause();
                _isPlaying = false;
                BtnPlayPause.Content = "▶";
            }
            else
            {
                Media.Play();
                _isPlaying = true;
                BtnPlayPause.Content = "⏸";
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Media.Stop();
            Media.Position = TimeSpan.Zero;
            _isPlaying = false;
            BtnPlayPause.Content = "▶";
            SeekBar.Value = 0;
        }

        private void SeekBar_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isSeeking = true;
        }

        private void SeekBar_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Media.Position = TimeSpan.FromSeconds(SeekBar.Value);
            _isSeeking = false;
        }

        private void VolumeBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Media != null) Media.Volume = e.NewValue;
        }
    }
}
