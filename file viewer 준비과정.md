## **Windows에서 동작하는 file viewer를 만들고 싶어.**

## **화면은 file의 내용을 표시하는 viewer영역이 main이고, 왼쪽에 sidebar: 탐색기와 유사한 형태로 folder tree-view와 file list-view. 지원하는 형식은 image,text,pdf, excel 등. zip 등으로 압축된 파일의 경우에는 메모리 내에서 파일시스템처럼 매핑해서 folder view와 file-list를 보여주고 개별 파일은 viewer영역에 내용 표시.**

## **WIN32 API, MFC등을 사용해서 windows용 S/W개발을 한 경험이 있어서, C, C++, C\#등에는 익숙해. C 계열의 언어를 사용한다면 AI의 도움을 받아서 내가 코딩을 주로 할 수 있지만, 다른 언어를 사용한다면 대부분 AI의 도움을 받아야 해.**

## **이 개발 작업에 적합한 언어와 라이브러리를 추천하고, AI 협업하려면 어떤 방식으로 작업을 진행하는게 좋을지 가이드 제시.**

## 

## 

## **기술 스택 분석**

### **요구사항 매핑**

| 기능 | 복잡도 | 핵심 기술 요소 |
| ----- | ----- | ----- |
| Folder Tree-View | 중 | OS 파일시스템 API |
| File List-View | 중 | 디렉토리 열거 |
| Image Viewer | 하 | 디코딩 라이브러리 |
| Text Viewer | 하 | 인코딩 감지 |
| PDF Viewer | 고 | 렌더링 엔진 필요 |
| Excel Viewer | 고 | OOXML 파싱 |
| ZIP 가상 파일시스템 | 고 | 메모리 내 트리 구조 |

---

## **언어별 옵션 비교**

### **\[Fact\] 핵심 후보 3개**

| 항목 | C++ / MFC | C\# / WPF | Python |
| ----- | ----- | ----- | ----- |
| 사용자 친숙도 | ★★★★★ | ★★★☆☆ | ★☆☆☆☆ |
| UI 개발 속도 | 느림 | 빠름 | 빠름 |
| PDF 지원 | PDFium (복잡) | PdfiumViewer (NuGet) | 쉬움 |
| Excel 지원 | libxlsxwriter | EPPlus / ClosedXML | openpyxl |
| ZIP 가상 FS | libzip / minizip | System.IO.Compression | zipfile |
| 배포 | 단일 EXE 가능 | .NET 런타임 필요 | Python 설치 필요 |
| AI 코드 생성 품질 | 중 (MFC 데이터 희소) | 고 | 고 |

---

## **추천: C\# / WPF**

### **근거**

| 판단 기준 | 내용 |
| ----- | ----- |
| **학습 곡선** | C++ 경험자는 C\# 문법 습득이 1\~2주 수준. 포인터/메모리 관리 없음 |
| **MFC 대응** | `TreeView`, `ListView`, `Splitter` 모두 WPF에 직접 대응 개념 존재 |
| **AI 협업 적합성** | GPT/Claude의 C\# WPF 코드 생성 품질이 MFC 대비 압도적으로 높음 |
| **라이브러리 생태계** | PDF·Excel·ZIP 모두 NuGet 1줄 설치로 해결 |
| **배포** | .NET 8 single-file publish → 사실상 단일 EXE 배포 가능 |

### **\[Inference \- 신뢰도 85%\] MFC 대신 WPF를 권장하는 이유**

MFC는 2024년 기준 AI 학습 데이터 내 샘플 밀도가 낮아, 복잡한 커스텀 컨트롤 생성 시 AI 출력 품질이 저하됨. WPF는 XAML 기반으로 AI가 UI 구조를 텍스트로 표현하기 적합함.

---

## **WPF 라이브러리 스택**

| 기능 | 라이브러리 | NuGet 패키지명 |
| ----- | ----- | ----- |
| PDF 렌더링 | PdfiumViewer | `PdfiumViewer` |
| Excel 파싱 | ClosedXML | `ClosedXML` |
| ZIP 가상 FS | 내장 | `System.IO.Compression` (BCL) |
| 이미지 | 내장 | WPF BitmapImage (BCL) |
| 텍스트 인코딩 감지 | Ude.NetStandard | `Ude.NetStandard` |
| Tree/List UI | 내장 | WPF TreeView, ListView (BCL) |

---

## **AI 협업 전략 (작업 분류)**

### **역할 분담 원칙**

| 작업 유형 | 담당 | 이유 |
| ----- | ----- | ----- |
| 아키텍처 설계, 모듈 분리 | **사용자** | 도메인 판단 필요 |
| XAML UI 레이아웃 | **AI** | 반복적·형식적 코드 |
| 뷰어별 렌더링 로직 | **AI 초안 → 사용자 검토** | 라이브러리 API 숙지 필요 |
| ZIP 가상 파일시스템 트리 | **AI 초안 → 사용자 검토** | 자료구조 설계 포함 |
| 버그 수정, 엣지케이스 | **협업** | 컨텍스트 의존 |

### **권장 작업 플로우**

1\. \[사용자\] 모듈 목록 확정 및 인터페이스 정의  
        ↓  
2\. \[AI\] 모듈별 스켈레톤 코드 생성 (클래스/메서드 시그니처)  
        ↓  
3\. \[사용자\] 구조 검토 및 수정  
        ↓  
4\. \[AI\] 기능별 구현 코드 생성 (단위: 1클래스 or 1기능)  
        ↓  
5\. \[사용자\] 컴파일 → 테스트 → 피드백  
        ↓  
6\. \[AI\] 오류 수정 / 리팩토링

### **AI에게 효과적인 프롬프트 패턴**

| 상황 | 프롬프트 예시 |
| ----- | ----- |
| 신규 기능 | *"WPF C\#. ZIP 파일을 열어 메모리 내 가상 트리를 구성하는 클래스. TreeViewItem 바인딩용 ObservableCollection 포함"* |
| 오류 수정 | *"\[오류 메시지\] \+ \[관련 코드 전체\] 붙여넣기"* |
| 리팩토링 | *"이 클래스를 MVVM 패턴으로 분리해줘. View/ViewModel/Model 각각 제시"* |

---

## **리스크 / 반론**

| \# | 리스크 | 대응 |
| ----- | ----- | ----- |
| 1 | C\# 미경험으로 초기 생산성 저하 | 첫 2주는 WPF 기본 예제(TreeView, GridSplitter) 집중 학습 후 본 개발 진입 |
| 2 | PdfiumViewer가 .NET 8과 호환성 문제 발생 가능 | 대안: `PDFsharp` \+ 직접 렌더링, 또는 WebView2로 브라우저 PDF 렌더링 |
| 3 | Excel 대용량 파일(수만 행)에서 ClosedXML 메모리 과다 사용 | 뷰어 전용이므로 읽기 전용 스트리밍 모드(OpenXML SDK 직접 사용)로 대체 가능 |

---

## **\[Unknown \- 데이터 요청\]**

다음 정보가 확정되어야 세부 아키텍처 설계 가능:

| 항목 | 질문 |
| ----- | ----- |
| 배포 형태 | 단일 EXE 필요? 설치 프로그램 허용? |
| Excel 표시 범위 | 셀 값만 표시? 서식(색상/폰트)도 재현? |
| ZIP 중첩 | ZIP 안의 ZIP도 재귀적으로 탐색 필요? |
| 파일 크기 상한 | 수 GB 파일도 처리 필요? (스트리밍 vs 전체 로드 설계 분기점) |

배포 형태 \-\> 현재 PC에서만 사용 예정이므로 배포 고려할 필요 없어  
Excel 표시 범위 \-\> 서식은 없어도 OK  
ZIP 중첩 \-\> 구현 가능하다면 좋겠지만, 매우 복잡해진다면 없어도 좋음  
파일 크기 상한 \-\> 동영상 파일이 아닌 이상 1G 이상의 파일은 지원하지 않음

WPF 기반이면, visual studio 에서 '클로드 코드' AI를 만나서 작업하는거야?

먼저 Visual Studio에서 프로젝트를 생성하는 방법부터 확인합니다.

## **Step 1 — Visual Studio에서 프로젝트 생성**

Visual Studio 2022 실행  
→ "새 프로젝트 만들기"  
→ "WPF 애플리케이션" 선택 (C\# 항목, .NET Core 계열)  
→ 프로젝트 이름: FileViewer  
→ 프레임워크: .NET 8.0  
→ 만들기

---

## **Step 2 — NuGet 패키지 설치**

**도구 → NuGet 패키지 관리자 → 패키지 관리자 콘솔** 에서:

Install-Package PdfiumViewer  
Install-Package PdfiumViewer.Native.x86\_x64.v8-xfa  
Install-Package ClosedXML  
Install-Package Ude.NetStandard

---

## **Step 3 — 프로젝트 폴더 구조**

생성 후 아래 폴더를 **솔루션 탐색기에서 직접 추가**:

FileViewer/  
├── Models/  
│   ├── FileNode.cs          \# 파일/폴더 트리 노드  
│   └── VirtualFileSystem.cs \# ZIP 가상 파일시스템  
├── ViewModels/  
│   ├── MainViewModel.cs     \# 메인 윈도우 VM  
│   └── SidebarViewModel.cs  \# 트리/리스트 VM  
├── Views/  
│   └── MainWindow.xaml      \# (자동 생성됨)  
├── Viewers/  
│   ├── ImageViewer.xaml     \# 이미지 뷰어 UserControl  
│   ├── TextViewer.xaml      \# 텍스트 뷰어  
│   ├── PdfViewer.xaml       \# PDF 뷰어  
│   ├── ExcelViewer.xaml     \# 엑셀 뷰어  
│   └── EmptyViewer.xaml     \# 기본/미지원 화면  
└── Helpers/  
    ├── FileTypeHelper.cs    \# 확장자 → 뷰어 매핑  
    └── ZipHelper.cs         \# ZIP 처리

---

## **Step 4 — 코드 파일 전체**

### **`Models/FileNode.cs`**

using System.Collections.ObjectModel;

namespace FileViewer.Models  
{  
    public class FileNode  
    {  
        public string Name { get; set; } \= "";  
        public string FullPath { get; set; } \= "";  
        public bool IsDirectory { get; set; }  
        public bool IsVirtual { get; set; }  // ZIP 내부 항목  
        public byte\[\]? VirtualData { get; set; }  // ZIP 내부 파일 데이터  
        public ObservableCollection\<FileNode\> Children { get; set; } \= new();

        public string Icon \=\> IsDirectory ? "📁" : GetFileIcon();

        private string GetFileIcon()  
        {  
            var ext \= System.IO.Path.GetExtension(Name).ToLowerInvariant();  
            return ext switch  
            {  
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp" \=\> "🖼️",  
                ".pdf" \=\> "📄",  
                ".xlsx" or ".xls" \=\> "📊",  
                ".txt" or ".log" or ".md" \=\> "📝",  
                ".zip" or ".rar" or ".7z" \=\> "🗜️",  
                \_ \=\> "📃"  
            };  
        }  
    }  
}

---

### **`Models/VirtualFileSystem.cs`**

using System.Collections.ObjectModel;  
using System.IO.Compression;

namespace FileViewer.Models  
{  
    public static class VirtualFileSystem  
    {  
        public static FileNode BuildFromZip(string zipPath)  
        {  
            var root \= new FileNode  
            {  
                Name \= System.IO.Path.GetFileName(zipPath),  
                FullPath \= zipPath,  
                IsDirectory \= true,  
                IsVirtual \= true  
            };

            using var archive \= ZipFile.OpenRead(zipPath);  
            foreach (var entry in archive.Entries)  
            {  
                var parts \= entry.FullName.Split('/');  
                InsertNode(root, parts, entry);  
            }

            return root;  
        }

        private static void InsertNode(FileNode parent, string\[\] parts, ZipArchiveEntry entry)  
        {  
            if (parts.Length \== 0 || string.IsNullOrEmpty(parts\[0\])) return;

            var existing \= parent.Children.FirstOrDefault(c \=\> c.Name \== parts\[0\]);

            if (parts.Length \== 1\)  
            {  
                // 파일 노드  
                if (\!string.IsNullOrEmpty(parts\[0\]))  
                {  
                    using var stream \= entry.Open();  
                    using var ms \= new System.IO.MemoryStream();  
                    stream.CopyTo(ms);

                    parent.Children.Add(new FileNode  
                    {  
                        Name \= parts\[0\],  
                        FullPath \= entry.FullName,  
                        IsDirectory \= false,  
                        IsVirtual \= true,  
                        VirtualData \= ms.ToArray()  
                    });  
                }  
            }  
            else  
            {  
                // 폴더 노드  
                if (existing \== null)  
                {  
                    existing \= new FileNode  
                    {  
                        Name \= parts\[0\],  
                        FullPath \= parts\[0\],  
                        IsDirectory \= true,  
                        IsVirtual \= true  
                    };  
                    parent.Children.Add(existing);  
                }  
                InsertNode(existing, parts\[1..\], entry);  
            }  
        }  
    }  
}

---

### **`Helpers/FileTypeHelper.cs`**

namespace FileViewer.Helpers  
{  
    public enum ViewerType  
    {  
        Image, Text, Pdf, Excel, Zip, Unsupported  
    }

    public static class FileTypeHelper  
    {  
        public static ViewerType GetViewerType(string fileName)  
        {  
            var ext \= System.IO.Path.GetExtension(fileName).ToLowerInvariant();  
            return ext switch  
            {  
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp" \=\> ViewerType.Image,  
                ".txt" or ".log" or ".md" or ".cs" or ".xml" or ".json" or ".csv" \=\> ViewerType.Text,  
                ".pdf" \=\> ViewerType.Pdf,  
                ".xlsx" or ".xls" \=\> ViewerType.Excel,  
                ".zip" \=\> ViewerType.Zip,  
                \_ \=\> ViewerType.Unsupported  
            };  
        }  
    }  
}

---

### **`ViewModels/MainViewModel.cs`**

using System.Collections.ObjectModel;  
using System.ComponentModel;  
using System.IO;  
using System.Runtime.CompilerServices;  
using FileViewer.Models;

namespace FileViewer.ViewModels  
{  
    public class MainViewModel : INotifyPropertyChanged  
    {  
        public ObservableCollection\<FileNode\> RootNodes { get; } \= new();

        private FileNode? \_selectedFile;  
        public FileNode? SelectedFile  
        {  
            get \=\> \_selectedFile;  
            set { \_selectedFile \= value; OnPropertyChanged(); }  
        }

        public MainViewModel()  
        {  
            LoadDrives();  
        }

        private void LoadDrives()  
        {  
            RootNodes.Clear();  
            foreach (var drive in DriveInfo.GetDrives().Where(d \=\> d.IsReady))  
            {  
                RootNodes.Add(new FileNode  
                {  
                    Name \= drive.Name,  
                    FullPath \= drive.RootDirectory.FullName,  
                    IsDirectory \= true  
                });  
            }  
        }

        public void LoadChildren(FileNode node)  
        {  
            if (\!node.IsDirectory || node.IsVirtual) return;  
            if (node.Children.Count \> 0\) return; // 이미 로드됨

            try  
            {  
                var dirs \= Directory.GetDirectories(node.FullPath)  
                    .Select(d \=\> new FileNode  
                    {  
                        Name \= Path.GetFileName(d),  
                        FullPath \= d,  
                        IsDirectory \= true  
                    });

                var files \= Directory.GetFiles(node.FullPath)  
                    .Select(f \=\> new FileNode  
                    {  
                        Name \= Path.GetFileName(f),  
                        FullPath \= f,  
                        IsDirectory \= false  
                    });

                foreach (var item in dirs.Concat(files))  
                    node.Children.Add(item);  
            }  
            catch { /\* 접근 불가 폴더 무시 \*/ }  
        }

        public event PropertyChangedEventHandler? PropertyChanged;  
        protected void OnPropertyChanged(\[CallerMemberName\] string? name \= null)  
            \=\> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));  
    }  
}

---

### **`MainWindow.xaml`**

\<Window x:Class="FileViewer.MainWindow"  
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"  
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"  
        Title="File Viewer" Height="700" Width="1100"  
        WindowStartupLocation="CenterScreen"\>

    \<Grid\>  
        \<Grid.ColumnDefinitions\>  
            \<ColumnDefinition Width="280" MinWidth="150"/\>  
            \<ColumnDefinition Width="4"/\>  
            \<ColumnDefinition Width="\*"/\>  
        \</Grid.ColumnDefinitions\>

        \<\!-- 사이드바 \--\>  
        \<Grid Grid.Column="0"\>  
            \<Grid.RowDefinitions\>  
                \<RowDefinition Height="\*"/\>  
                \<RowDefinition Height="4"/\>  
                \<RowDefinition Height="\*"/\>  
            \</Grid.RowDefinitions\>

            \<\!-- Folder Tree \--\>  
            \<Border Grid.Row="0" BorderBrush="\#CCCCCC" BorderThickness="0,0,0,0"\>  
                \<TreeView x:Name="FolderTree"  
                          ItemsSource="{Binding RootNodes}"  
                          SelectedItemChanged="FolderTree\_SelectedItemChanged"\>  
                    \<TreeView.ItemTemplate\>  
                        \<HierarchicalDataTemplate ItemsSource="{Binding Children}"\>  
                            \<StackPanel Orientation="Horizontal"\>  
                                \<TextBlock Text="{Binding Icon}" Margin="0,0,4,0"/\>  
                                \<TextBlock Text="{Binding Name}"/\>  
                            \</StackPanel\>  
                        \</HierarchicalDataTemplate\>  
                    \</TreeView.ItemTemplate\>  
                \</TreeView\>  
            \</Border\>

            \<GridSplitter Grid.Row="1" Height="4" HorizontalAlignment="Stretch"  
                          Background="\#CCCCCC"/\>

            \<\!-- File List \--\>  
            \<ListView x:Name="FileList" Grid.Row="2"  
                      SelectionChanged="FileList\_SelectionChanged"\>  
                \<ListView.View\>  
                    \<GridView\>  
                        \<GridViewColumn Header="이름" Width="160"  
                            DisplayMemberBinding="{Binding Name}"/\>  
                        \<GridViewColumn Header="유형" Width="60"  
                            DisplayMemberBinding="{Binding Icon}"/\>  
                    \</GridView\>  
                \</ListView.View\>  
            \</ListView\>  
        \</Grid\>

        \<\!-- 세로 구분선 \--\>  
        \<GridSplitter Grid.Column="1" Width="4" HorizontalAlignment="Stretch"  
                      Background="\#CCCCCC"/\>

        \<\!-- 뷰어 영역 \--\>  
        \<ContentControl x:Name="ViewerArea" Grid.Column="2"  
                        Margin="4"/\>  
    \</Grid\>  
\</Window\>

---

### **`MainWindow.xaml.cs`**

using System.Windows;  
using System.Windows.Controls;  
using FileViewer.Helpers;  
using FileViewer.Models;  
using FileViewer.ViewModels;  
using FileViewer.Viewers;

namespace FileViewer  
{  
    public partial class MainWindow : Window  
    {  
        private readonly MainViewModel \_vm \= new();

        public MainWindow()  
        {  
            InitializeComponent();  
            DataContext \= \_vm;  
        }

        private void FolderTree\_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs\<object\> e)  
        {  
            if (e.NewValue is not FileNode node) return;

            if (node.IsDirectory && \!node.IsVirtual)  
            {  
                \_vm.LoadChildren(node);  
                // 파일 리스트 갱신  
                FileList.ItemsSource \= node.Children;  
            }  
            else if (node.IsDirectory && node.IsVirtual)  
            {  
                FileList.ItemsSource \= node.Children;  
            }  
        }

        private void FileList\_SelectionChanged(object sender, SelectionChangedEventArgs e)  
        {  
            if (FileList.SelectedItem is not FileNode node) return;  
            if (node.IsDirectory) return;

            OpenFile(node);  
        }

        private void OpenFile(FileNode node)  
        {  
            var type \= FileTypeHelper.GetViewerType(node.Name);

            ViewerArea.Content \= type switch  
            {  
                ViewerType.Image \=\> new ImageViewer(node),  
                ViewerType.Text  \=\> new TextViewer(node),  
                ViewerType.Pdf   \=\> new PdfViewer(node),  
                ViewerType.Excel \=\> new ExcelViewer(node),  
                ViewerType.Zip   \=\> OpenZip(node),  
                \_                \=\> new EmptyViewer("지원하지 않는 파일 형식입니다.")  
            };  
        }

        private UIElement OpenZip(FileNode node)  
        {  
            var virtualRoot \= VirtualFileSystem.BuildFromZip(node.FullPath);  
            // ZIP 루트를 TreeView에 임시 삽입  
            \_vm.RootNodes.Add(virtualRoot);  
            return new EmptyViewer("ZIP 파일이 열렸습니다. 왼쪽 트리에서 탐색하세요.");  
        }  
    }  
}

---

### **`Viewers/EmptyViewer.xaml` \+ `.cs`**

\<UserControl x:Class="FileViewer.Viewers.EmptyViewer"  
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"  
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"\>  
    \<Grid\>  
        \<TextBlock x:Name="MessageText"  
                   HorizontalAlignment="Center"  
                   VerticalAlignment="Center"  
                   FontSize="16" Foreground="Gray"/\>  
    \</Grid\>  
\</UserControl\>

namespace FileViewer.Viewers  
{  
    public partial class EmptyViewer : System.Windows.Controls.UserControl  
    {  
        public EmptyViewer(string message \= "파일을 선택하세요.")  
        {  
            InitializeComponent();  
            MessageText.Text \= message;  
        }  
    }  
}

---

### **`Viewers/ImageViewer.xaml` \+ `.cs`**

\<UserControl x:Class="FileViewer.Viewers.ImageViewer"  
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"  
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"\>  
    \<ScrollViewer HorizontalScrollBarVisibility="Auto"  
                  VerticalScrollBarVisibility="Auto"\>  
        \<Image x:Name="MainImage" Stretch="Uniform"/\>  
    \</ScrollViewer\>  
\</UserControl\>

using System.IO;  
using System.Windows.Controls;  
using System.Windows.Media.Imaging;  
using FileViewer.Models;

namespace FileViewer.Viewers  
{  
    public partial class ImageViewer : UserControl  
    {  
        public ImageViewer(FileNode node)  
        {  
            InitializeComponent();  
            var bitmap \= new BitmapImage();  
            bitmap.BeginInit();  
            if (node.IsVirtual && node.VirtualData \!= null)  
                bitmap.StreamSource \= new MemoryStream(node.VirtualData);  
            else  
                bitmap.UriSource \= new Uri(node.FullPath);  
            bitmap.CacheOption \= BitmapCacheOption.OnLoad;  
            bitmap.EndInit();  
            MainImage.Source \= bitmap;  
        }  
    }  
}

---

### **`Viewers/TextViewer.xaml` \+ `.cs`**

\<UserControl x:Class="FileViewer.Viewers.TextViewer"  
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"  
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"\>  
    \<ScrollViewer\>  
        \<TextBox x:Name="TextContent"  
                 IsReadOnly="True"  
                 FontFamily="Consolas"  
                 FontSize="13"  
                 TextWrapping="Wrap"  
                 VerticalScrollBarVisibility="Auto"  
                 HorizontalScrollBarVisibility="Auto"  
                 BorderThickness="0"/\>  
    \</ScrollViewer\>  
\</UserControl\>

using System.IO;  
using System.Text;  
using System.Windows.Controls;  
using FileViewer.Models;  
using Ude;

namespace FileViewer.Viewers  
{  
    public partial class TextViewer : UserControl  
    {  
        public TextViewer(FileNode node)  
        {  
            InitializeComponent();

            byte\[\] data \= node.IsVirtual && node.VirtualData \!= null  
                ? node.VirtualData  
                : File.ReadAllBytes(node.FullPath);

            var encoding \= DetectEncoding(data) ?? Encoding.UTF8;  
            TextContent.Text \= encoding.GetString(data);  
        }

        private static Encoding? DetectEncoding(byte\[\] data)  
        {  
            var detector \= new CharsetDetector();  
            detector.Feed(data, 0, data.Length);  
            detector.DataEnd();  
            if (detector.Charset \!= null)  
                return Encoding.GetEncoding(detector.Charset);  
            return null;  
        }  
    }  
}

---

## **다음 단계 순서**

| 순서 | 작업 | 상태 |
| ----- | ----- | ----- |
| 1 | 위 파일 전부 붙여넣기 후 빌드 확인 | ← **지금** |
| 2 | PdfViewer 구현 | 빌드 성공 후 요청 |
| 3 | ExcelViewer 구현 | 빌드 성공 후 요청 |
| 4 | TreeView Lazy Load 개선 | 기본 동작 확인 후 |

**빌드 오류 발생 시 오류 메시지 전체를 붙여넣으면 바로 수정합니다.**


> md 사용 추천
# md test
*# md
# md1
## md2
### md3

* a
*b
*. 3
1.
2.
*dd*
**aaa**
