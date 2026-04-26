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
├── App.xaml / App.xaml.cs
├── Models/
│   ├── FileNode.cs              # 파일/폴더 트리 노드 (IsVirtual, SubFolders, IsExpanded 포함)
│   └── VirtualFileSystem.cs    # ZIP 가상 파일시스템 (ZipFile 기반)
├── Helpers/
│   └── FileTypeHelper.cs       # 확장자 → ViewerType 열거형 매핑
├── ViewModels/
│   └── MainViewModel.cs        # 드라이브 열거, Lazy Load, 경로 저장/복원
├── Views/
│   ├── MainWindow.xaml(.cs)    # 메인 창: 사이드바 + 뷰어 + 슬라이드-인 패널
│   ├── FileInfoPanel.xaml(.cs) # 슬라이드-인 파일정보/작업 패널 (신규)
│   └── RenameDialog.xaml(.cs)  # 이름 변경 다이얼로그 (신규)
└── Viewers/
    ├── EmptyViewer.xaml(.cs)
    ├── ImageViewer.xaml(.cs)
    ├── TextViewer.xaml(.cs)
    ├── PdfViewer.xaml(.cs)
    └── ExcelViewer.xaml(.cs)
```

---

## NuGet 패키지

| 패키지 | 버전 | 용도 |
|--------|------|------|
| `PDFtoImage` | 5.2.1 | PDF → SKBitmap 렌더링 |
| `SkiaSharp.NativeAssets.Win32` | 최신 | PDFtoImage 네이티브 DLL |
| `ClosedXML` | 최신 | Excel 파일 파싱 |
| `Ude.NetStandard` | 최신 | 텍스트 인코딩 자동 감지 |

> ⚠️ `UseWindowsForms`는 `System.Drawing` 충돌로 **절대 추가 금지**.
> 폴더 선택 다이얼로그는 `Microsoft.Win32.OpenFolderDialog` (.NET 8 WPF 내장) 사용.

---

## 주요 클래스 요약

### `Models/FileNode.cs`
```csharp
public class FileNode : INotifyPropertyChanged
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsDirectory { get; set; }
    public bool IsVirtual { get; set; }
    public byte[]? VirtualData { get; set; }
    public bool IsExpanded { get; set; }   // TwoWay 바인딩용
    public bool IsSelected { get; set; }   // TwoWay 바인딩용
    public ObservableCollection<FileNode> Children { get; set; }
    public IEnumerable<FileNode> SubFolders => Children.Where(c => c.IsDirectory); // TreeView 전용
    public string Icon { get; }
}
```

### `Views/FileInfoPanel.xaml.cs`
```csharp
// 공개 API
public void SetFile(FileNode? node, UIElement? currentViewer = null)
// 이벤트
public event Action<FileNode>? DeleteRequested;
public event Action<FileNode, string>? RenameRequested;   // (node, newName)
public event Action<FileNode, string>? MoveRequested;     // (node, destFolder)
```

### `Views/MainWindow.xaml.cs` — 슬라이드-인 패널 관련
```csharp
// 필드
private readonly TranslateTransform _panelTranslate = new() { X = 260 };
private bool _isPanelVisible = false;
private const double PanelWidth = 260;
private const double TriggerZone = 80; // 우측 경계 80px 이내 진입 시 패널 등장

// 생성자에서 반드시:
InfoPanel.RenderTransform = _panelTranslate;  // XAML x:Name 대신 코드 할당

// 슬라이드 애니메이션
_panelTranslate.BeginAnimation(TranslateTransform.XProperty, ...);
```

### `Viewers/PdfViewer.xaml.cs`
```csharp
// PDFtoImage 5.x API
Conversion.GetPageCount(stream)
Conversion.ToImage(stream, page: N)   // ← 'pageIndex' 파라미터명 사용 금지
```

---

## 해결된 주요 이슈 이력 (→ HISTORY.md 참조)

---

## 현재 구현 완료 기능

- [x] 드라이브 목록 Tree-View (폴더만 표시, SubFolders 바인딩)
- [x] 폴더 Lazy Load (비동기 LoadChildrenAsync)
- [x] 파일 List-View (파일만 표시, Where !IsDirectory)
- [x] 하위 폴더 포함 토글 (재귀 파일 수집)
- [x] 마지막 경로 저장/복원 (AppData)
- [x] 텍스트 뷰어 (EUC-KR 포함 인코딩 자동 감지)
- [x] 이미지 뷰어
- [x] PDF 뷰어 (페이지 이전/다음)
- [x] Excel 뷰어 (시트 탭 전환)
- [x] ZIP 가상 파일시스템 (Tree-View에 마운트)
- [x] 뷰어 더블클릭 전체화면 토글
- [x] 마우스 휠 파일 순차 탐색
- [x] Delete 키 파일 삭제 (휴지통)
- [x] **FileInfoPanel 슬라이드-인** (뷰어 우측 80px 진입 시 등장)
  - 파일 정보: 이름/크기/수정일/경로
  - 뷰어별 속성: 이미지(해상도/DPI/색깊이), PDF(페이지수), Excel(시트수/목록), Text(인코딩/줄수)
  - 작업: 탐색기 열기 / 이름 바꾸기 / 이동 / 삭제

## 미구현 / 개선 예정

- [ ] Tree-View ▶ 화살표 표시 최적화
- [ ] 이미지 뷰어 확대/축소
- [ ] PDF 뷰어 DPI 옵션
- [ ] ZIP 중첩 지원
- [ ] 파일 검색 기능
- [ ] 다크 모드

---

## Claude 코딩 제약 (반드시 준수)

1. **namespace**: 모든 파일 `TienViewer.*`
2. **PDFtoImage API**: `Conversion.ToImage(stream, page: N)` — `pageIndex` 파라미터명 사용 금지
3. **Stream 분기**: 3항 연산자 대신 `if/else` + `using(stream)` 패턴
4. **인코딩**: `CodePagesEncodingProvider.Instance` App.xaml.cs에 이미 등록됨
5. **UseWindowsForms 금지**: `System.Drawing` 충돌 발생. 폴더 다이얼로그는 `Microsoft.Win32.OpenFolderDialog` 사용
6. **TranslateTransform x:Name 금지**: UserControl이 포함된 경우 XAML 스코프 충돌. 코드비하인드에서 `element.RenderTransform = new TranslateTransform()` 으로 할당
7. **새 Viewer 추가 시**: `FileTypeHelper.cs` enum + `MainWindow.xaml.cs` OpenFile switch 동기화 필수
