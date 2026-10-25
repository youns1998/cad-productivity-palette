# CAD Productivity Palette

선택 객체 편집·반복 명령·도면 점검·보고서 출력을 모은 AutoCAD 도킹 팔레트입니다.

**Windows · AutoCAD 2027 · C# · .NET 10 · WPF**

## 실행

.NET 10 SDK와 AutoCAD 2027을 준비하고 저장소 루트에서 빌드합니다.

```powershell
dotnet build -c Release
```

AutoCAD에서 `NETLOAD`로 다음 DLL을 불러온 뒤 **`CADTOOLS`** 를 실행합니다.

```text
src/CadProductivityPalette/bin/Release/net10.0-windows/CadProductivityPalette.dll
```

설치 위치가 다르면 빌드에 `-p:AutoCADDir="설치 경로"`를 지정합니다. DLL 폴더는 AutoCAD의 신뢰할 수 있는 위치에 등록합니다. Autodesk API DLL은 배포하지 않습니다.

## 기능

- 선택 객체 속성 확인·편집, Quick Tool 36개, 최근·자주 사용 목록
- 심플 모드의 기본 도구 8개와 전체 모드, 사용 기록·설정 저장
- 열린 Polyline·직접 색상·빈 Text·단위 미지정 점검, 검토 객체 선택·Zoom
- Handle·판정 근거를 담은 CSV·JSON 출력 (`CADREVIEWEXPORT`, `CADREVIEWBATCH`)

점검은 ModelSpace 직접 객체를 읽고, 자동 수정하지 않습니다. 후보 수는 규칙별 건수 합계이며 같은 객체가 중복 집계될 수 있습니다. Layer 0은 참고 정보입니다.

## 코드 구조

```text
CadProductivityPalette.sln       # 루트 빌드 진입점
src/CadProductivityPalette/
  Commands/                     # AutoCAD 명령
  Core/                         # 점검 모델·규칙·보고서 저장
  Services/                     # 도면·선택·명령·기록 처리
  Models/ · UI/ · Utils/        # 화면 데이터·WPF·명령 도우미
tools/ReviewRunner/             # AutoCAD 없는 규칙 실행·검사
samples/ · examples/reports/   # 가상 입력·기록한 출력
scripts/ · packaging/         # 검증·배포 묶음
```

처음 읽을 코드: [명령](src/CadProductivityPalette/Commands/ReviewCommands.cs) → [도면 스캐너](src/CadProductivityPalette/Services/DrawingStatusService.cs) → [점검 규칙](src/CadProductivityPalette/Core/DrawingReviewEngine.cs).

## 검증

AutoCAD 없이 .NET 10 SDK만으로 실행할 수 있는 검사입니다.

```powershell
dotnet run --project tools/ReviewRunner -- --self-test
```

**독립 검사 22개 통과**, Release 빌드 경고·오류 0개. 실제 AutoCAD 2027 Core Console에서 메인 DLL을 로드하고 객체 3개 → 검토 후보 4건의 CSV·JSON 출력을 확인했습니다.

GUI 도킹·버튼 클릭, AutoCAD 2026 호환 실행은 별도 확인 범위입니다. 점검 기준은 예시이며 기관 납품 표준 판정은 포함하지 않습니다.

[개발·명령·패키징 안내](docs/development.md) · [검증 기록](docs/verification.md) · [실제 엔진 출력](examples/reports/autocad-engine-review.json)
