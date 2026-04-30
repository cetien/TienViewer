- [x] UI 2026.4.25
최근 폴더 복원 기능을 구현하고 트리뷰/파일 리스트의 필터링을 적용하기 위해 수정된 파일 목록과 주요 변경 사항은 다음과 같습니다.

수정된 파일 목록
c:\Users\tien7\source\repos\TienViewer\Models\FileNode.cs

UI 상태 관리: IsExpanded(확장 여부), IsSelected(선택 여부) 속성을 추가하여 트리뷰의 상태를 ViewModel에서 제어할 수 있게 했습니다.
트리 필터링: 트리뷰에는 폴더만 표시하기 위해 SubFolders라는 별도의 읽기 전용 속성을 추가했습니다.
인터페이스 구현: 상태 변경을 UI에 즉시 알리기 위해 INotifyPropertyChanged를 구현했습니다.
c:\Users\tien7\source\repos\TienViewer\ViewModels\MainViewModel.cs

경로 저장 및 로드: 로컬 앱 데이터 폴더(AppData)에 마지막 경로를 텍스트 파일로 저장하고 불러오는 SaveLastPath, LoadLastPath 메서드를 추가했습니다.
복원 로직(RestorePath): 앱 시작 시 저장된 경로를 바탕으로 드라이브부터 하위 폴더까지 순차적으로 IsExpanded를 true로 설정하고 자식 노드를 로드하며 탐색하는 로직을 구현했습니다.
UI 갱신 트리거: 자식 노드가 로드될 때 트리뷰의 SubFolders 바인딩이 갱신되도록 RefreshNode 메서드를 호출합니다.
c:\Users\tien7\source\repos\TienViewer\Views\MainWindow.xaml

상태 바인딩: TreeView.ItemContainerStyle을 추가하여 TreeViewItem의 IsExpanded와 IsSelected 속성을 FileNode 모델과 TwoWay 바인딩했습니다.
폴더 전용 트리: HierarchicalDataTemplate의 ItemsSource를 Children에서 SubFolders로 변경하여 트리뷰에서 파일이 보이지 않도록 필터링했습니다.
c:\Users\tien7\source\repos\TienViewer\Views\MainWindow.xaml.cs

시작 시 복원: Loaded 이벤트 핸들러에서 ViewModel의 RestorePath를 호출하여 이전 세션의 위치로 이동하게 했습니다.
경로 저장 트리거: FolderTree_SelectedItemChanged 이벤트가 발생할 때마다 현재 선택된 폴더의 경로를 자동으로 저장합니다.
파일 리스트 필터링: 하단 FileList(ListView)의 ItemsSource를 설정할 때 Where(c => !c.IsDirectory)를 사용하여 폴더를 제외한 파일만 표시되도록 수정했습니다.
이제 앱을 재시작하면 마지막으로 탐색했던 폴더가 자동으로 열리고, 왼쪽 트리에는 폴더만, 아래쪽 리스트에는 파일만 표시되는 구조가 완성되었습니다.

- [x] Hex View 2026.4.29
UnsupportedViewer를 Hex Dump 뷰어로 전면 재구성.

수정된 파일 목록
c:\Users\tien7\source\repos\TienViewer\Viewers\UnsupportedViewer.xaml

레이아웃 변경: StackPanel → DockPanel. 상단 메타+버튼 영역(Border)과 하단 Hex 영역(ScrollViewer+ItemsControl) 분리.
상단 메타 패널: fileName, fileSize, created date 표시.
버튼 행: Mount (ZIP 전용, Collapsed), External Viewer, Delete (Virtual 파일 시 비활성화).
Hex 패널: 열 헤더(Offset/Hex/ASCII) + ItemsControl, Consolas 고정폭 폰트, 다크 배경(#1E1E1E).
c:\Users\tien7\source\repos\TienViewer\Viewers\UnsupportedViewer.xaml.cs

이벤트 추가: DeleteRequested (Action<FileNode>) — MainWindow의 OnInfoPanel_Delete 패턴 재사용.
LoadMeta(): FileInfo로 fileSize, creationTime 주입. VirtualNode는 VirtualData.Length 사용, created는 "(ZIP 내부)" 표시.
LoadHex() / ReadFirst1K(): 최대 1024 bytes 읽기. Virtual/실제 파일 분기 처리.
BuildHexLines(): 16바이트/행. 형식: {Offset:X8}  {hex 8컬럼 공백 8컬럼}  {ASCII}. 비인쇄 문자 → '.'.
Delete_Click(): MessageBox 확인 후 DeleteRequested 발생. Virtual 파일이면 버튼 비활성.
c:\Users\tien7\source\repos\TienViewer\Views\MainWindow.xaml.cs

FileList_SelectionChanged: ZIP 뷰어 생성 시 viewer.DeleteRequested += OnInfoPanel_Delete 구독 추가.
OpenFile(): ViewerType.Unsupported 분기를 switch 외부로 분리하여 DeleteRequested 구독 추가.

- [x] 좌우 슬라이드 패널 + 앱 종료 버튼 2026.4.29

수정된 파일 목록
c:\Users\tien7\source\repos\TienViewer\Views\MainWindow.xaml

레이아웃 전면 변경: 기존 3-Column Grid(사이드바 고정) → 단일 컬럼 ViewerContainer(전체화면) + 좌우 overlay 패널 구조.
LeftPanel (Border): 폴더트리 + 파일리스트를 Width=280 overlay Border로 이동. HorizontalAlignment=Left, 초기 Opacity=0/IsHitTestVisible=False.
InfoPanel: 기존과 동일, HorizontalAlignment=Right overlay 유지.
GridSplitter 제거: overlay 구조이므로 컬럼 분리선 불필요.

c:\Users\tien7\source\repos\TienViewer\Views\MainWindow.xaml.cs

필드 추가: _isLeftPanelVisible, LeftPanelWidth(280), LeftTriggerZone(80), _leftPanelTranslate(TranslateTransform X=-280).
생성자: LeftPanel.RenderTransform = _leftPanelTranslate 할당.
ViewerContainer_MouseMove(): 오른쪽(기존) + 왼쪽 트리거 로직 통합. mouseX < 80이면 ShowLeftPanel(), mouseX > 280이면 HideLeftPanel().
ViewerContainer_MouseLeave(): HideInfoPanel() + HideLeftPanel() 동시 호출.
ShowLeftPanel/HideLeftPanel(): InfoPanel과 동일 패턴, TranslateTransform X: -280↔0 애니메이션.
AnimatePanel(): 좌우 공통 정적 메서드 AnimatePanelX()로 리팩토링.
ToggleFullScreen(): overlay 구조 전환으로 Column 조작 불필요 — 플래그 토글만 유지.
_savedSidebarWidth 필드 제거.

c:\Users\tien7\source\repos\TienViewer\Views\FileInfoPanel.xaml

앱 종료 버튼 추가: BtnDelete 아래 Separator + BtnExit("⏻  앱 종료", DangerButton 스타일).

c:\Users\tien7\source\repos\TienViewer\Views\FileInfoPanel.xaml.cs

BtnExit_Click(): Application.Current.Shutdown() 호출.
BtnDelete_Click(): 주석 처리된 MessageBox 코드 제거, DeleteRequested 직접 호출로 정리.

- [x] MediaViewer + TextViewer IOException 수정 2026.4.29

수정된 파일 목록
c:\Users\tien7\source\repos\TienViewer\Viewers\TextViewer.xaml.cs
  File.ReadAllBytes() → FileStream(FileShare.ReadWrite)으로 교체.
  다른 프로세스가 쓰는 중인 파일(.log 등) IOException 해결.

c:\Users\tien7\source\repos\TienViewer\Viewers\MediaViewer.xaml (신규)
  MediaElement 기반 동영상/음악 플레이어.
  컨트롤: 재생/일시정지, 정지, 시크바, 시간표시, 볼륨.
  ZIP 내부 파일은 임시 경로로 추출 후 재생, Unloaded 시 정리.

c:\Users\tien7\source\repos\TienViewer\Viewers\MediaViewer.xaml.cs (신규)
  DispatcherTimer(500ms)로 시크바/시간 갱신.
  SeekBar 드래그 중 타이머 갱신 일시 중단(_isSeeking).

c:\Users\tien7\source\repos\TienViewer\Helpers\FileTypeHelper.cs
  ViewerType.Media 추가.
  Media 확장자: mp4, mkv, avi, mov, wmv, flv, webm, m4v, mp3, wav, flac, aac, ogg, wma, m4a.
  Text 확장자 확장: html, htm, yaml, yml, ini, cfg, toml, sql, py, js, ts, css, sh, bat 추가.

c:\Users\tien7\source\repos\TienViewer\Views\MainWindow.xaml.cs
  OpenFile() switch에 ViewerType.Media => new MediaViewer(node) 추가.

- [ ] 향후 구현 계획 (embed viewer)

[ ] SVG Viewer
    방법: SharpVectors.Wpf NuGet 패키지 (SharpVectors.Wpf)
    대상: .svg
    난이도: 낮음

[ ] HTML Viewer (현대적 렌더링)
    방법: Microsoft.Web.WebView2 NuGet 패키지 (Chromium/Edge 기반)
    대상: .html, .htm (현재는 TextViewer로 소스 표시)
    난이도: 낮음. Edge WebView2 런타임 필요.
    비고: Markdown도 Markdig로 HTML 변환 후 WebView2로 표시 가능.

[ ] Word/DOCX 뷰어
    방법: DocumentFormat.OpenXml으로 파싱 → FlowDocument 렌더링
    대상: .docx
    난이도: 높음 (복잡한 서식 재현 어려움). 외부열기 병행 권장.

[ ] 코덱 없는 동영상 재생 (mkv, flac 등)
    현재 MediaElement는 Windows Media Foundation 의존 → H.264/AAC만 보장.
    방법: LibVLCSharp.WPF NuGet 패키지 (코덱 내장)
    난이도: 중간.
