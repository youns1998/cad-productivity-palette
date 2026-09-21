# CAD 생산성 도구

> AutoCAD에서 선택 객체 확인, 반복 명령 실행, 도면 상태 점검을 하나의 도킹 Palette로 처리하는 실무형 플러그인입니다.

![AutoCAD](https://img.shields.io/badge/AutoCAD-2026.1.1-E51050?logo=autodesk&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![UI](https://img.shields.io/badge/UI-WPF-0078D4?logo=windows&logoColor=white)
![Release build](https://img.shields.io/badge/Release_build-verified-2EA44F)

리본 탭과 명령 이름을 반복해서 오가는 시간을 줄이는 것이 목표입니다. 자주 쓰는 CAD 용어는 영어로 유지하고, 설명·상태·진단 문구는 한국어로 표시합니다.

## 핵심 기능

- 선택한 객체의 종류와 주요 속성을 즉시 표시
- 객체 종류에 맞는 편집 작업 제공
- 최근 사용 및 사용 빈도에 따라 도구 자동 정렬
- Palette 버튼, 명령행, 단축키 사용을 동일하게 집계
- 36개 Quick Tool을 목적별 그룹으로 제공
- 열린 Polyline, ByLayer가 아닌 색상, 빈 Text 등 도면 상태 점검
- 점검 대상 객체를 선택하고 해당 범위로 Zoom
- 처음 사용자를 위한 심플 모드와 전체 모드 지원

## 화면 구성

```text
CAD 생산성 도구
├─ 작업 도구
│  ├─ 선택 객체
│  │  ├─ 주요 속성
│  │  └─ 객체 전용 편집
│  ├─ 스마트 도구
│  │  ├─ 최근 사용
│  │  └─ 자주 사용
│  ├─ 기본 도구          ← 심플 모드
│  └─ 모든 도구          ← 전체 모드, 6개 접이식 그룹
└─ 도면 점검             ← 전체 모드
   ├─ 검토 항목
   ├─ 현재 도면 정보
   └─ Layer / Block 참고 정보
```

### 심플 모드와 전체 모드

| 구분 | 심플 모드 `ON` | 심플 모드 `OFF` |
|---|---|---|
| 기본 상태 | 최초 실행 시 기본값 | 사용자가 전환 |
| 선택 객체 정보 | 표시 | 표시 |
| 최근·자주 사용 | 표시 | 표시 |
| 명령 도구 | 기본 도구 8개 | 모든 도구 36개 |
| 도면 점검 | 숨김 | 표시 |

선택한 모드는 다음 실행에도 유지됩니다. 전체 모드에서는 기본 도구와 추가 도구를 중복 배치하지 않고 **모든 도구** 한 영역으로 통합했습니다.

### 색상 의미

| 표현 | 의미 |
|---|---|
| 진한 회색 버튼 | 일반 AutoCAD 명령 실행 |
| 파란색 버튼 | 현재 선택 객체에만 적용되는 편집 작업 |
| 파란색 밑줄 | 현재 선택한 Tab |
| 초록색 토글 | 심플 모드 `ON` |
| 황갈색 `검토` Badge | 확인이 필요한 후보가 있음 |
| 회색 `참고` Badge | 오류가 아닌 도면 참고 정보 |
| 초록색 `없음` Badge | 해당 검토 대상이 없음 |

점검 결과는 오류를 단정하지 않습니다. 예를 들어 열린 Polyline이나 직접 지정한 색상은 의도된 설계일 수 있습니다.

## Quick Tools

도구 이름은 AutoCAD에서 익숙한 용어를 유지하며, Tooltip은 한국어로 설명합니다. 실제 실행에는 언어 독립 명령 형식인 `_.COMMAND`를 사용합니다.

| 그룹 | 도구 |
|---|---|
| 그리기 | Line, Polyline, Rectangle, Circle |
| 수정 | Move, Copy, Offset, Trim, Extend, Fillet, Chamfer, Join, Rotate, Mirror, Scale, Stretch, Align, Match Props |
| 주석 | Hatch, Text, MText, Dimension, Linear Dim, Aligned Dim |
| Layer | Layer Isolate, Layer 복원, Layer 끄기, 모든 Layer 켜기 |
| Block | Block 생성, Insert, Block 편집, Explode |
| 측정 · 정리 | Distance, Area, Purge, Zoom Extents |

버튼은 명령을 시작하는 역할만 합니다. 점 지정과 세부 Option 입력은 일반 AutoCAD 명령처럼 명령행과 도면 화면에서 계속합니다.

## 스마트 도구와 사용 기록

- **최근 사용:** 마지막으로 완료한 서로 다른 도구를 최대 6개 표시
- **자주 사용:** 2회 이상 완료한 도구를 횟수순으로 최대 6개 표시
- Palette 버튼뿐 아니라 명령행과 단축키로 실행한 대상 명령도 집계
- 정상 완료된 명령만 기록하고 취소·실패한 명령은 제외
- `NETLOAD` 이후에는 Palette가 닫혀 있어도 계속 집계
- **기록 초기화** 버튼으로 언제든 사용 기록 삭제 가능

집계 대상은 Quick Tools에 등록된 36개 명령입니다. 선택 객체 전용 작업은 사용 빈도에 포함하지 않습니다.

## 선택 객체 정보

AutoCAD의 implied selection이 바뀌면 다음 정보를 갱신합니다.

| 객체 | 표시 정보와 전용 작업 |
|---|---|
| 공통 | 종류, Layer, 색상(ByLayer/ByBlock/ACI/RGB) |
| Polyline | 길이, 면적, 열림/닫힘, 닫기/열기, Polyline 편집 |
| Line | 길이 |
| Text / MText | 내용, 높이, Text Style, Text 편집 |
| Block 참조 | Block 이름, 속성 유무, Block 편집 |
| Hatch | Hatch Pattern, 축척, Hatch 편집 |
| Dimension | Dimension Style, 측정값, Dimension 편집 |
| Circle | 반지름, 지름, 면적 |
| Arc | 반지름, 길이 |
| 다중 선택 | 객체 수, 공통 Layer 또는 객체 종류 |

Polyline의 **닫기 / 열기**만 짧은 쓰기 Transaction으로 `Closed` 속성을 직접 변경합니다. 나머지 편집 버튼은 AutoCAD 기본 명령에 연결됩니다.

## 도면 점검

현재 Model Space를 읽기 전용 Transaction으로 분석합니다.

| 분류 | 점검 항목 | 선택 + Zoom |
|---|---|:---:|
| 검토 | 열린 Polyline | 지원 |
| 검토 | ByLayer가 아닌 색상 | 지원 |
| 검토 | 빈 Text / MText | 지원 |
| 참고 | Layer 0 사용 | 지원 |
| 참고 | 미사용 일반 Block 정의 | 미지원 |
| 참고 | 잠긴 Layer | 미지원 |

현재 Layer, Text Style, Dimension Style과 Model Space 객체 수도 함께 표시합니다. 미사용 정의의 실제 제거는 AutoCAD `Purge`에서 사용자가 확인하도록 하며, 점검 기능이 도면을 자동 수정하지는 않습니다.

## 설치 및 실행

### 요구 환경

- Windows
- AutoCAD 2026.1.1
- .NET 8 SDK
- C# / WPF
- Autodesk AutoCAD .NET API (`AcMgd`, `AcDbMgd`, `AcCoreMgd`)

Autodesk Runtime DLL은 저장소에 포함하지 않습니다. 프로젝트는 기본 AutoCAD 설치 경로의 DLL을 `Private=false`로 참조합니다.

### 빌드

```powershell
git clone https://github.com/youns1998/cad-productivity-palette.git
cd cad-productivity-palette
dotnet build -c Release
```

결과 파일:

```text
bin\Release\net8.0-windows\CadProductivityPalette.dll
```

AutoCAD 설치 경로가 다르면 [CadProductivityPalette.csproj](CadProductivityPalette.csproj)의 `HintPath` 세 곳을 수정합니다.

### AutoCAD에서 실행

1. AutoCAD 명령행에서 `NETLOAD`를 실행합니다.
2. 빌드한 `CadProductivityPalette.dll`을 선택합니다.
3. `CADTOOLS`를 입력해 Palette를 엽니다.
4. `CADTOOLS`를 다시 입력하면 Palette가 닫히며, 한 번 더 입력하면 다시 열립니다.

환경에 따라 DLL 폴더를 AutoCAD `TRUSTEDPATHS`에 추가해야 할 수 있습니다. 새 DLL 적용 전에는 도면을 저장하고 AutoCAD를 재시작하는 것이 안전합니다.

## 로컬 저장 데이터

| 파일 | 내용 |
|---|---|
| `%LOCALAPPDATA%\CadProductivityPalette\tool-usage.json` | 최근 사용 시각과 누적 횟수 |
| `%LOCALAPPDATA%\CadProductivityPalette\user-settings.json` | 심플 모드 선택 상태 |

두 파일은 사용자 PC에만 저장되며 Git 저장소나 다른 PC에 공유되지 않습니다. 파일이 없거나 읽을 수 없을 때는 빈 사용 기록과 심플 모드로 안전하게 시작합니다.

## 안전성과 성능

- 문서 전환 시 이전 문서 Event를 해제하고 활성 문서에 다시 연결
- 문서 종료 시 Event와 화면 상태 정리
- 선택·명령 Event를 180ms 동안 모아 중복 갱신 방지
- 현재 보이는 Tab만 갱신하고 숨겨진 Palette에서는 도면 분석 중지
- AutoCAD 명령 실행 중 Database 분석을 미뤄 중첩 Transaction 방지
- 삭제되었거나 다른 문서에 속한 `ObjectId`를 작업 전에 제외
- 예외를 AutoCAD 명령행에 `[CAD 생산성 도구]` 접두어로 출력
- 대형 도면의 전체 Model Space 분석은 도면 점검 화면이 필요할 때만 실행

## 프로젝트 구조

```text
Commands/       CADTOOLS 진입 명령
Models/         선택 정보, 도면 상태, 도구 정의
Services/       명령 실행, 사용 기록, 선택 분석, 도면 점검
UI/             PaletteSet Host, WPF 화면, ViewModel
Utils/          WPF ICommand 구현
Properties/     AutoCAD 확장 및 명령 Assembly 등록
```

## 검증 상태

- Release 빌드: 경고 0개, 오류 0개
- 36개 명령의 중복 없는 Catalog 구성 확인
- 명령행 이름 정규화 및 Catalog 연결 확인
- 사용 기록 저장·재로딩·빈도 집계·초기화 확인
- 심플 모드 설정 저장 및 재로딩 확인
- 별도 WPF Preview에서 320px/370px 폭, 두 모드, Tab, 접이식 그룹과 Binding 확인

실제 Palette 도킹, 대화형 명령 Prompt, DWG별 점검 결과는 AutoCAD 내부 환경과 도면 내용의 영향을 받으므로 `NETLOAD` 후 최종 확인이 필요합니다.
