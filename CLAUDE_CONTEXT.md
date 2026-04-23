# TienViewer — Claude 협업 컨텍스트

## 프로젝트 개요
- **앱 이름**: TienViewer
- **목적**: Windows용 파일 뷰어 (이미지/텍스트/PDF/Excel/ZIP)
- **언어**: C# / WPF / .NET 8
- **IDE**: Visual Studio 2022
- **경로**: `C:\Users\tien7\source\repos\TienViewer`

---

## 파일 구조

```
TienViewer/
├── App.xaml
├── App.xaml.cs
├── Models/
│   ├── FileNode.cs              # 파일/폴더 트리 노드 모델
│   └── VirtualFileSystem.cs    # ZIP 가상 파일시스템 (ZipFile 기반)
├── Helpers/
│   ├── FileTypeHelper.cs       # 확장자 → ViewerType 열거형 매핑
│   └── ZipHelper.cs            # (예비)
├── ViewModels/
│   └── MainViewModel.cs        # 드라이브 열거, 폴더 Lazy Load, SelectedFile
├── Views/
│   └── MainWindow.xaml(.cs)    # GridSplitter, TreeView, ListView, ContentControl
└── Viewers/
    ├── EmptyViewer.xaml(.cs)   # 기본/미지원 안내 화면
    ├── ImageViewer.xaml(.cs)   # BitmapImage 기반 이미지 표시
    ├── TextViewer.xaml(.cs)    # Consolas 폰트 TextBox, 인코딩 자동 감지
    ├── PdfViewer.xaml(.cs)     # PDFtoImage 기반 페이지 렌더링
    └── ExcelViewer.xaml(.cs)   # ClosedXML + DataGrid, 시트 탭
```

---

## NuGet 패키지

| 패키지 | 버전 | 용도 |
|--------|------|------|
| `PDFtoImage` | 5.2.1 | PDF → SKBitmap 렌더링 |
| `SkiaSharp.NativeAssets.Win32` | 최신 | PDFtoImage 네이티브 DLL |
| `ClosedXML` | 최신 | Excel 파일 파싱 |
| `Ude.NetStandard` | 최신 | 텍스트 인코딩 자동 감지 |

---

## 주요 클래스 요약

### `Models/FileNode.cs`
```csharp
public class FileNode
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsDirectory { get; set; }
    public bool IsVirtual { get; set; }       // ZIP 내부 항목 여부
    public byte[]? VirtualData { get; set; }  // ZIP 내부 파일 데이터
    public ObservableCollection<FileNode> Children { get; set; }
    public string Icon { get; }               // 확장자별 이모지
}
```

### `Helpers/FileTypeHelper.cs`
```csharp
public enum ViewerType { Image, Text, Pdf, Excel, Zip, Unsupported }

public static ViewerType GetViewerType(string fileName)
// 확장자 매핑:
// .jpg/.jpeg/.png/.bmp/.gif/.webp → Image
// .txt/.log/.md/.cs/.xml/.json/.csv → Text
// .pdf → Pdf
// .xlsx/.xls → Excel
// .zip → Zip
```

### `ViewModels/MainViewModel.cs`
```csharp
public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<FileNode> RootNodes { get; } // 드라이브 목록
    public FileNode? SelectedFile { get; set; }
    public void LoadChildren(FileNode node)  // 폴더 Lazy Load
}
```

### `Views/MainWindow.xaml.cs`
```csharp
// FolderTree_SelectedItemChanged → LoadChildren + FileList 갱신
// FileList_SelectionChanged → OpenFile 호출
// OpenFile → ViewerType별 UserControl을 ViewerArea.Content에 할당
// OpenZip → VirtualFileSystem.BuildFromZip → RootNodes에 추가
```

### `Viewers/PdfViewer.xaml.cs`
```csharp
// PDFtoImage 5.x API 사용
// Conversion.GetPageCount(stream)
// Conversion.ToImage(stream, page: pageIndex)  ← pageIndex 명명 파라미터 없음, page 사용
// SKBitmap.Encode → MemoryStream → BitmapImage → WPF Image
```

### `Viewers/TextViewer.xaml.cs`
```csharp
// Ude.CharsetDetector로 인코딩 감지
// 감지 실패 시 UTF-8 폴백
// EUC-KR 등 레거시 인코딩: App.xaml.cs에서 RegisterProvider 필수
```

### `App.xaml.cs`
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // EUC-KR 등 활성화
    base.OnStartup(e);
}
```

---

## 해결된 주요 이슈 이력

| 날짜 | 파일 | 이슈 | 해결 |
|------|------|------|------|
| 초기 | 전체 | namespace FileViewer → TienViewer | Ctrl+Shift+H 일괄 치환 |
| 초기 | ExcelViewer.xaml.cs | MemoryStream/FileStream 3항 연산자 타입 오류 | if/else로 분리 후 using(stream) |
| 초기 | PdfViewer | WebView2 어셈블리 로드 실패 | PDFtoImage로 교체 |
| 초기 | PdfViewer.xaml.cs | ToImage pageIndex 파라미터 오류 | page: pageIndex 로 수정 |
| 초기 | TextViewer.xaml.cs | EUC-KR not supported encoding | App.xaml.cs RegisterProvider 추가 |

---

## 현재 구현 완료 기능

- [x] 드라이브 목록 Tree-View
- [x] 폴더 Lazy Load (클릭 시 하위 로드)
- [x] 파일 List-View (폴더 선택 시 하위 파일 표시)
- [x] 텍스트 뷰어 (EUC-KR 포함 인코딩 자동 감지)
- [x] 이미지 뷰어
- [x] PDF 뷰어 (페이지 이전/다음)
- [x] Excel 뷰어 (시트 탭 전환)
- [x] ZIP 가상 파일시스템 (Tree-View에 마운트)

## 미구현 / 개선 예정

- [ ] Tree-View Lazy Load 개선 (▶ 화살표 표시)
- [ ] 이미지 뷰어 확대/축소
- [ ] PDF 뷰어 DPI 옵션
- [ ] ZIP 중첩 지원
- [ ] 파일 검색 기능
- [ ] 다크 모드

---

## Claude에게 요청 시 참고사항

1. **namespace**: 모든 파일 `TienViewer.*`
2. **PDFtoImage API**: `Conversion.ToImage(stream, page: N)` — `pageIndex` 파라미터명 사용 금지
3. **Stream 분기**: 3항 연산자 대신 if/else + `using(stream)` 패턴 사용
4. **인코딩**: `CodePagesEncodingProvider.Instance` 이미 등록됨
5. **새 Viewer 추가 시**: `FileTypeHelper.cs`의 enum과 switch, `MainWindow.xaml.cs`의 `OpenFile` 메서드도 함께 수정 필요
