# CAD Productivity Palette

AutoCAD에서 반복적으로 쓰는 명령, 현재 선택 객체의 정보와 후속 작업, 도면 상태 점검을 하나의 도킹 팔레트에 모은 생산성 도구입니다. 실무에서 계속 열어 두고 사용하는 흐름을 목표로 하며, 화면에 보이는 작업은 AutoCAD 명령 또는 .NET API 동작에 연결했습니다.

## 해결하려는 문제

도면 편집 중에는 리본 탭과 명령 이름을 계속 오가게 되고, 선택 객체의 핵심 속성이나 도면 정리 상태를 확인하려면 여러 창과 명령을 반복해서 열어야 합니다. 이 플러그인은 다음 작업 흐름을 한곳으로 줄입니다.

- 자주 쓰는 작성·수정·주석·레이어·블록·측정 명령 실행
- 현재 선택 객체의 종류와 핵심 속성 확인
- 객체 종류에 맞는 편집 작업 바로 실행
- 열린 폴리라인, 비 ByLayer 색상, 빈 텍스트 등 도면 상태 점검
- 문제가 있는 객체를 선택하고 해당 범위로 줌

## 환경

- AutoCAD 2026.1.1
- .NET 8 / `net8.0-windows`
- C# / WPF
- Autodesk AutoCAD .NET API (`AcMgd`, `AcDbMgd`, `AcCoreMgd`)

NuGet 패키지를 추가하지 않았습니다. Autodesk DLL은 저장소에 복사하지 않고 로컬 AutoCAD 설치 폴더를 `Private=false`로 참조합니다.

## 주요 기능

### 화면 구성과 기능 정리

- **작업 도구 탭:** 선택 객체 정보를 맨 위에 표시하고, 자주 쓰는 8개 도구를 바로 아래에 배치합니다.
- **자주 쓰는 도구:** Line, Polyline, Move, Copy, Offset, Trim, MText, Dimension. 실제 사용 통계가 아닌 초기 기본 구성입니다.
- **추가 도구:** 나머지 28개 명령은 6개 접이식 그룹으로 제공하며 기본적으로 접혀 있습니다. 자주 쓰는 도구와 중복 배치하지 않습니다.
- **선택 객체 액션:** 객체 전용 편집만 강조합니다. 반복되던 Move/Copy/Offset/Match Props/Explode 버튼과 다중 선택 Erase 버튼을 컨텍스트에서 제거했습니다. 공통 편집은 Quick Tools에서 실행합니다.
- **도면 점검 탭:** 검토 항목 3개를 우선 표시하고, 스타일·객체 수와 레이어·블록 참고 정보는 접어서 표시합니다.
- **가독성:** 섹션·설명은 한국어, 익숙한 도구 이름은 영어로 표시합니다. 동작 가능한 항목에만 선택 + 줌 버튼이 나타나며, 0건 결과와 참고 정보도 흐려지지 않습니다.

### Quick Tools

명령은 사용 목적별로 접을 수 있는 그룹에 배치했습니다. 버튼은 언어 독립적인 AutoCAD 명령 형식(`_.COMMAND`)을 사용합니다.

- **DRAW:** LINE, PLINE, RECTANG, CIRCLE
- **MODIFY:** OFFSET, TRIM, EXTEND, FILLET, CHAMFER, JOIN, COPY, MOVE, ROTATE, MIRROR, SCALE, STRETCH, ALIGN, MATCHPROP
- **ANNOTATION:** HATCH, TEXT, MTEXT, DIM, DIMLINEAR, DIMALIGNED
- **LAYER:** LAYISO, LAYUNISO, LAYOFF, LAYON
- **BLOCK:** BLOCK, INSERT, BEDIT, EXPLODE
- **UTILITY:** DIST, AREA, PURGE, ZOOM EXTENTS

버튼은 명령을 시작하며, 이후 점 지정과 옵션 입력은 평소 AutoCAD 명령처럼 명령행과 도면 화면에서 계속합니다.

### Selection Context

AutoCAD의 implied selection이 바뀌면 다음 정보를 갱신합니다.

- 공통: 객체 종류, 레이어, 색상(ByLayer/ByBlock/ACI)
- Polyline: 길이, 면적, 열림/닫힘 및 Close/Open·Polyline Edit 작업
- Line: 길이
- Text/MText: 내용, 높이, 문자 스타일 및 Text Edit 작업
- Block Reference: 블록 이름, 속성 유무 및 BEDIT 작업
- Hatch: 패턴, 축척 및 Hatch Edit 작업
- Dimension: 치수 스타일, 측정값 및 DIMEDIT 작업
- Circle: 반지름, 지름, 면적
- Arc: 반지름, 길이
- 다중 선택: 객체 수, 공통 레이어/종류

Polyline의 Close/Open은 문서를 잠근 뒤 짧은 쓰기 트랜잭션으로 `Closed` 속성을 직접 변경합니다. `Polyline Edit`는 PEDIT를 시작하며 Join 등 옵션은 사용자가 지정합니다. 나머지 작업은 AutoCAD 기본 명령에 연결되어 있으며 명령별 프롬프트에 따라 대상이나 옵션을 추가로 지정할 수 있습니다.

### Drawing Status

현재 Model Space를 읽기 전용 트랜잭션으로 분석합니다.

- 현재 레이어, 문자 스타일, 치수 스타일
- Model Space 객체 수
- 열린 Polyline
- ByLayer가 아닌 색상을 가진 객체
- 빈 Text/MText
- Layer 0에 놓인 객체
- 참조 없는 일반 Block 정의
- 잠긴 Layer

열린 Polyline·ByLayer 외 색상·빈 문자를 검토 항목으로 표시합니다. 건수가 있는 항목의 **선택 + 줌** 버튼을 누르면 해당 객체를 선택하고 객체 extents에 맞춰 현재 뷰를 이동합니다. 0건은 `없음`으로 표시합니다. 열린 폴리라인이나 직접 지정한 색상이 곧 오류를 뜻하지는 않습니다.

Layer 0 사용·참조 없는 블록·잠긴 레이어는 **참고 정보**에 표시합니다. Layer 0 객체는 선택 + 줌을 지원하고 나머지는 숫자만 표시합니다. 기존 미사용 Layer/Text Style 집계는 Model Space만 읽어 오판할 수 있어 제거했습니다. 정의 정리는 AutoCAD PURGE에서 직접 검토합니다.

## 안전성과 이벤트 처리

- 활성 문서가 바뀌면 이전 문서 이벤트를 해제하고 새 문서에 연결합니다.
- 문서 종료 시 이벤트와 화면 상태를 정리합니다.
- 선택/명령 이벤트는 180ms 동안 모아 한 번만 갱신합니다.
- AutoCAD 명령 실행 중에는 데이터베이스 분석을 미루어 중첩 트랜잭션을 피합니다.
- 삭제된 ObjectId와 다른 문서의 ObjectId는 명령/줌 실행 전에 제외합니다.
- 예외는 AutoCAD 명령행에 `[CAD Productivity Palette]` 접두어로 간단히 출력합니다.
- 진단 기능은 도면을 자동 수정하지 않습니다.

## 빌드

AutoCAD 2026이 기본 경로에 설치되어 있어야 합니다.

```powershell
dotnet clean
dotnet build
```

기존 Debug DLL이 AutoCAD에 로드되어 파일이 잠긴 경우에는 실행 중인 AutoCAD를 유지한 채 별도 Release 산출물을 만들 수 있습니다.

```powershell
dotnet build -c Release
```

Release 결과: `bin\Release\net8.0-windows\CadProductivityPalette.dll`. 새 버전 적용은 도면을 저장한 뒤 AutoCAD를 재시작하고 해당 DLL을 NETLOAD합니다.

참조 경로가 다른 환경에서는 `CadProductivityPalette.csproj`의 세 `HintPath`만 설치 위치에 맞게 수정합니다. 빌드 결과는 다음 경로에 생성됩니다.

```text
bin\Debug\net8.0-windows\CadProductivityPalette.dll
```

## AutoCAD에서 실행

1. AutoCAD 2026.1.1을 실행합니다.
2. 명령행에 `NETLOAD`를 입력합니다.
3. 빌드된 `CadProductivityPalette.dll`을 선택합니다.
4. 명령행에 `CADTOOLS`를 입력합니다.
5. 같은 명령을 다시 입력하면 팔레트가 닫히고, 다시 입력하면 열립니다.

AutoCAD의 보안 정책에 따라 DLL 폴더를 `TRUSTEDPATHS`에 추가해야 할 수 있습니다.

## 프로젝트 구조

```text
Commands/   CADTOOLS 진입 명령
Models/     선택 정보, 도면 상태, 도구 정의
Services/   명령 실행, 선택 분석, 도면 진단
UI/         PaletteSet 호스트, WPF 화면, ViewModel
Utils/      WPF ICommand 구현
Properties/ AutoCAD 확장/명령 어셈블리 등록
```

## 현재 구현 범위와 확인 사항

초기 버전은 `dotnet clean`/`dotnet build`로 검증했습니다. UI 정리 버전은 기존 DLL이 실행 중인 AutoCAD에 잠겨 있어 Release 빌드로 검증했습니다. AutoCAD를 로드하지 않는 별도 WPF 미리보기에서 320px/370px 폭, 두 탭, 그룹 펼치기/접기, 선택 없음, 데이터 바인딩과 36개 명령의 중복 없는 구성을 확인했습니다. 미리보기 수치는 화면 검증용 예시이며 실제 도면 분석 결과가 아닙니다.

`NETLOAD`, 실제 팔레트 도킹, 각 대화형 명령의 프롬프트 흐름, 임의 DWG에 대한 진단 결과는 실행 중인 AutoCAD UI에서 별도로 확인해야 합니다. AutoCAD 내부 실행을 수행하지 않은 항목을 성공한 것으로 간주하지 않습니다.

대형 도면에서는 전체 Model Space 진단에 시간이 걸릴 수 있습니다. 분석은 팔레트를 열 때, Refresh를 누를 때, 또는 AutoCAD 명령이 끝난 뒤 실행됩니다.

## 다음 확장 후보

1. 사용자별 즐겨찾기/최근 사용 Quick Tool 순서 저장
2. Block·Layer·Text Style 사용량을 중첩 블록과 Paper Space까지 분석하는 상세 모드
3. 안전 항목별 검토 화면과 실행 취소 가능한 선택적 Auto Fix
