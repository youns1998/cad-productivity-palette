# 개발 안내

## 수정할 위치

`src/CadProductivityPalette/` 기준입니다.

| 변경 | 파일 |
|---|---|
| 명령 등록·보고서 실행 | `Commands/PaletteCommand.cs`, `Commands/ReviewCommands.cs` |
| 점검 데이터 | `Core/ReviewModels.cs` |
| 점검 규칙 | `Core/DrawingReviewEngine.cs` |
| CSV·JSON 저장 | `Core/DrawingReviewWriter.cs` |
| 실제 도면 스캔 | `Services/DrawingStatusService.cs` |
| 도구 목록 | `Services/ToolCatalog.cs` |
| 명령·사용 기록·설정 | `Services/AutoCadCommandService.cs`, `ToolUsageTracker.cs`, `ToolUsageService.cs`, `UserSettingsService.cs` |
| 화면·바인딩 | `UI/ProductivityPalette.xaml`, `UI/ProductivityViewModel.cs` |

스캐너가 읽기 전용 트랜잭션으로 ModelSpace 객체를 추출합니다. 독립 규칙은 Handle로 결과를 만들고, UI는 ObjectId로 매핑해 선택·Zoom합니다. `tools/ReviewRunner`는 같은 Core 코드를 링크해 AutoCAD 없이 실행합니다.

## 점검 규칙

| ID | 대상 |
|---|---|
| `OPEN_POLYLINE` | 열린 2D Polyline |
| `DIRECT_COLOR` | ByLayer가 아닌 색상·ByBlock |
| `EMPTY_TEXT` | 공백 Text·MText |
| `UNSPECIFIED_UNITS` | INSUNITS=0 |
| `LAYER_ZERO` | Layer 0 참고 정보 |

Block 내부·외부 참조·PaperSpace는 재귀 검사하지 않습니다. 규칙은 설계 의도에 따라 정상일 수 있는 검토 후보를 찾습니다. 기존 편집 도구는 사용자의 명령에 따라 도면을 변경하지만 점검·보고서는 자동 수정하지 않습니다.

## 실행·배포 명령

저장소 루트에서 실행합니다.

```powershell
# 샘플 분석: JSON 입력으로 CSV·JSON 생성
dotnet run --project tools/ReviewRunner -- samples/inspection-demo.json .codex-preview/reports/demo-review

# 독립 검사·샘플 분석·플러그인 빌드
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1

# 실제 AutoCAD Core Console 검사
powershell -ExecutionPolicy Bypass -File scripts/verify-autocad.ps1

# AutoCAD 2027 배포 묶음
powershell -ExecutionPolicy Bypass -File scripts/package.ps1
```

`verify-autocad.ps1`은 새 임시 도면에서 메인 DLL과 보고서 출력을 확인합니다. DLL 폴더를 일시적으로 신뢰 경로에 추가하고 로드 직후 복원하며, `SECURELOAD`는 변경하지 않습니다.

배포 결과는 `.artifacts/`의 bundle과 ZIP입니다. AutoCAD 종료 후 bundle을 `%APPDATA%/Autodesk/ApplicationPlugins/`에 복사합니다. GUI 명령은 `CADTOOLS`, 저장 대화상자는 `CADREVIEWEXPORT`, 경로 입력 방식은 `CADREVIEWBATCH`입니다.

AutoCAD 2026 대상 설정은 `.NET 8`이며 다음과 같이 빌드합니다. 해당 버전 API·SDK가 필요하고 이번 검증 대상은 2027입니다.

```powershell
dotnet build -c Release -p:AutoCADVersion=2026
```

`examples/reports/demo-review.*`는 JSON 샘플 분석, `autocad-engine-review.*`는 실제 엔진 출력입니다. 검증 스크립트는 기록한 예제를 덮어쓰지 않고 `.codex-preview/`에 결과를 저장합니다.
CI 설정은 자동 등록하지 않은 [템플릿](ci/verify.yml)입니다. 대형 도면의 응답 시간과 기관별 규칙은 별도 검증 대상입니다.
