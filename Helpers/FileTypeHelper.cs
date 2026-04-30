using System.IO;

namespace TienViewer.Helpers
{
    public enum ViewerType
    {
        Image, Text, Pdf, Excel, Zip, Media, Unsupported
    }

    public static class FileTypeHelper
    {
        // ── 기존 API: 확장자만 (호환성 유지) ───────────────────────────────
        public static ViewerType GetViewerType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp" => ViewerType.Image,
                ".txt" or ".log" or ".md" or ".cs" or ".xml" or ".json"
                    or ".csv" or ".html" or ".htm" or ".yaml" or ".yml"
                    or ".ini" or ".cfg" or ".toml" or ".sql" or ".py"
                    or ".js" or ".ts" or ".css" or ".sh" or ".bat"        => ViewerType.Text,
                ".pdf"  => ViewerType.Pdf,
                ".xlsx" or ".xls" => ViewerType.Excel,
                ".zip"  => ViewerType.Zip,
                ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv"
                    or ".webm" or ".m4v" or ".mp3" or ".wav" or ".flac"
                    or ".aac" or ".ogg" or ".wma" or ".m4a"               => ViewerType.Media,
                _       => ViewerType.Unsupported,
            };
        }

        // ── byte[] 오버로드: Magic Number 우선, 실패 시 확장자 폴백 ─────────
        public static ViewerType GetViewerType(string fileName, byte[]? header)
        {
            if (header != null && header.Length >= 4)
            {
                // PDF: %PDF (25 50 44 46)
                if (header[0] == 0x25 && header[1] == 0x50 &&
                    header[2] == 0x44 && header[3] == 0x46)
                    return ViewerType.Pdf;

                // PNG (89 50 4E 47)
                if (header[0] == 0x89 && header[1] == 0x50 &&
                    header[2] == 0x4E && header[3] == 0x47)
                    return ViewerType.Image;

                // JPEG (FF D8 FF)
                if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                    return ViewerType.Image;

                // GIF (47 49 46 38)
                if (header[0] == 0x47 && header[1] == 0x49 &&
                    header[2] == 0x46 && header[3] == 0x38)
                    return ViewerType.Image;

                // BMP (42 4D)
                if (header[0] == 0x42 && header[1] == 0x4D)
                    return ViewerType.Image;

                // WebP: RIFF????WEBP — bytes 0-3=RIFF, 8-11=WEBP
                if (header.Length >= 12 &&
                    header[0] == 0x52 && header[1] == 0x49 &&
                    header[2] == 0x46 && header[3] == 0x46 &&
                    header[8] == 0x57 && header[9] == 0x45 &&
                    header[10] == 0x42 && header[11] == 0x50)
                    return ViewerType.Image;

                // OLE2 Compound (D0 CF 11 E0) — XLS, 구 Office
                if (header[0] == 0xD0 && header[1] == 0xCF &&
                    header[2] == 0x11 && header[3] == 0xE0)
                    return ViewerType.Excel;

                // PK (50 4B 03 04) — ZIP 컨테이너 (XLSX/ZIP 공유) → 확장자로 분기
                if (header[0] == 0x50 && header[1] == 0x4B &&
                    header[2] == 0x03 && header[3] == 0x04)
                {
                    var ext = Path.GetExtension(fileName).ToLowerInvariant();
                    return (ext == ".xlsx" || ext == ".xls")
                        ? ViewerType.Excel
                        : ViewerType.Zip;
                }
            }

            // Magic Number 판별 실패 → 확장자 폴백
            return GetViewerType(fileName);
        }
    }
}
