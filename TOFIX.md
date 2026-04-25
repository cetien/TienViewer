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
