
# 📄 TienViewer 프로젝트 통합 스펙 가이드 (PROJECT_SPEC.md)

## 1. 프로젝트 개요
- **목적**: Windows용 경량 파일 뷰어 (이미지/텍스트/PDF/Excel/ZIP 지원)
- **핵심 컨셉**: 왼쪽 사이드바(트리뷰/리스트뷰) 탐색 + 오른쪽 메인 뷰어 영역
- **기술 스택**: C# / WPF / .NET 8.0 / Visual Studio 2022 기반
- **네임스페이스**: `TienViewer` (모든 파일 공통)

---

## 2. 기술적 제약 및 구현 원칙 (Gemini 준수 사항)
AI가 코드를 생성할 때 반드시 지켜야 할 **5계명**입니다.

1.  **PDFtoImage API 제약**: 버전 5.x 사용 시 `Conversion.ToImage(stream, page: N)` 형식을 사용하며, `pageIndex`라는 파라미터명은 절대 사용하지 않는다.
2.  **안전한 스트림 관리**: `MemoryStream`과 `FileStream` 분기 시 3항 연산자 대신 `if/else` 문과 `using` 블록을 명확히 사용한다.
3.  **인코딩 처리**: `App.xaml.cs`에 `CodePagesEncodingProvider`가 이미 등록되어 있으므로, EUC-KR 등 레거시 인코딩을 즉시 사용할 수 있다.
4.  **UI 패턴**: MVVM 패턴을 지향하며, 새 뷰어 추가 시 `FileTypeHelper.cs`의 열거형과 `MainWindow.xaml.cs`의 `OpenFile` 스위치 로직을 반드시 동기화한다.
5.  **ZIP 가상화**: 압축 파일은 메모리 내에서 가상 트리 구조(`FileNode`)로 매핑하여 탐색기에 마운트한다.

---

## 3. 파일 및 클래스 구조
```
TienViewer/
├── Models/
│   ├── FileNode.cs          # [완료] 트리/리스트 공통 데이터 모델 (IsVirtual 필드 포함)
│   └── VirtualFileSystem.cs # [완료] ZIP 압축 해제 및 메모리 트리 구성 로직
├── ViewModels/
│   └── MainViewModel.cs     # [완료] 드라이브 열거, 폴더 Lazy Load, 선택 파일 관리
├── Helpers/
│   └── FileTypeHelper.cs    # [완료] 확장자별 ViewerType 매핑 (이미지/텍스트/PDF 등)
├── Views/
│   └── MainWindow.xaml      # [완료] GridSplitter 기반 사이드바/뷰어 레이아웃
└── Viewers/
    ├── ImageViewer.xaml     # [완료] 이미지 표시 (BitmapImage)
    ├── TextViewer.xaml      # [완료] 텍스트 표시 (인코딩 자동감지 포함)
    ├── PdfViewer.xaml       # [진행중] PDF 페이지 렌더링 및 페이지 전환
    └── ExcelViewer.xaml     # [진행중] ClosedXML 기반 시트 데이터 표시
```

---

## 4. 현재 구현 상태 및 다음 작업 (Backlog)

### **✅ 구현 완료 기능**
- [x] 로컬 드라이브 목록 및 폴더 클릭 시 하위 항목 로드 (Lazy Load)
- [x] 이미지 및 텍스트(EUC-KR 대응) 기본 뷰어
- [x] ZIP 파일 선택 시 사이드바 트리에 가상 폴더로 마운트 기능

### **🛠 우선 구현 예정 (Next Task)**
1.  **PdfViewer 상세 구현**: 페이지 이동 버튼 및 현재 페이지/총 페이지 표시 기능
2.  **ExcelViewer 상세 구현**: DataGrid를 활용한 셀 값 표시 및 하단 시트 탭 전환 기능
3.  **UI 개선**: Tree-View에서 하위 폴더가 있을 때만 화살표(`▶`) 표시 최적화

### **🚀 추후 개선 사항**
- 이미지 뷰어 확대/축소 및 회전 기능
- PDF 뷰어 고해상도 DPI 렌더링 옵션
- 앱 전체 다크 모드 적용 및 파일 검색 기능

---

## 5. 라이브러리 의존성 (NuGet)
| 패키지명 | 목적 |
| :--- | :--- |
| `PDFtoImage` | PDF 파일 렌더링 |
| `ClosedXML` | Excel(OOXML) 데이터 파싱 |
| `Ude.NetStandard` | 텍스트 인코딩(Charset) 자동 판별 |

---

### **💡 AI 협업 팁**
* "**PROJECT_SPEC.md의 구현 제약 1번(PDF API)을 참고해서 PdfViewer.xaml.cs를 작성해줘**"와 같이 질문하면 오류 없는 코드를 얻을 수 있습니다.
* 새로운 기능을 추가할 때마다 위 문서의 **'구현 완료 기능'** 칸을 업데이트하여 AI가 중복 코드를 생성하지 않게 관리하세요.