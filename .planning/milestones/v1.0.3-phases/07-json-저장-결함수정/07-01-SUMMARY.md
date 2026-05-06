---
phase: 07-json-저장-결함수정
plan: 01
subsystem: ui
tags: [winforms, ui-state, defect-fix, mode-toggle, gs-certification]

# Dependency graph
requires:
  - phase: 5.6-결함수정 (v1.0)
    provides: "DF-1-04 0-BBOX 가드 패턴 — 본 plan 의 패턴 참조원"
provides:
  - "SetMode(DrawMode) 헬퍼 — currentMode + BackColor + Cursor 단일 진실의 원천"
  - "ModeButtonActiveColor / ModeButtonInactiveColor static readonly 상수 (ARGB 인라인 중복 제거)"
  - "JSON 저장 자동 reload 사이클에서 좌측 모드 버튼 시각 동기화 보장"
affects: [07-02-FUNC-12, 07-03-회귀-인스톨러]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "단일 진실의 원천 헬퍼 (currentMode 와 시각 상태 묶어서 갱신)"
    - "static readonly Color 상수 캡슐화 (ARGB 인라인 제거)"

key-files:
  created: []
  modified:
    - "Forms/MainForm.cs"

key-decisions:
  - "SetMode 헬퍼는 Drawing/Painting region 내, btnSelectAll_Click 직전에 배치하여 모드 전환 코드의 응집도 유지"
  - "ARGB 상수는 헬퍼 인근의 private static readonly Color 필드로 캡슐화 — class-level 상수 영역으로 분산하지 않음 (모드 전환 컨텍스트와 결합도 유지)"
  - "MainForm_Load 의 SetMode 호출은 DarkTheme.Apply(this) 직후에 배치 — 다크 테마가 BackColor 를 덮어쓴 후 모드 시각이 최종 결정되도록"
  - "currentMode 필드 기본값 (line 61) 은 그대로 유지 — CONTEXT.md D-02 락"
  - "D1/D2 단축키 핸들러는 무수정 — btnSelectAll_Click/btnEdit_Click 호출 경유로 SetMode 자동 적용 (D-03 락)"

patterns-established:
  - "모드/상태 전환 헬퍼 패턴: 필드 + 시각 + 보조 UI(커서) 를 한 메서드에서 원자적으로 갱신"
  - "ARGB 색상 상수화 패턴: private static readonly Color {Name}Active/Inactive — inline FromArgb 등장을 0건으로 유지"

requirements-completed: [FUNC-11]

# Metrics
duration: ~12min
completed: 2026-05-06
---

# Phase 7 Plan 1: SetMode 헬퍼 도입 + 좌측 모드 버튼 시각 동기화 Summary

**SetMode(DrawMode) 헬퍼 신규 도입으로 currentMode + btnSelectAll/btnEdit BackColor + pictureBoxVideo.Cursor 를 4개 진입점(Click 핸들러 2 + LoadLabelingData reset + MainForm_Load) 에서 원자적으로 동기화 — KTC 2차 DF-2-05 결함 0건화**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-05-06T02:18Z
- **Completed:** 2026-05-06T02:30:35Z
- **Tasks:** 1
- **Files modified:** 1 (Forms/MainForm.cs)
- **Lines:** +31, -9 (net +22)

## Accomplishments

- `private void SetMode(DrawMode mode)` 헬퍼 신규 도입 — 모드 전환 단일 진실의 원천
- `ModeButtonActiveColor` / `ModeButtonInactiveColor` static readonly Color 상수 캡슐화 — 인라인 `Color.FromArgb(59, 130, 246)` BackColor 대입을 2건 → 0건으로 제거
- 4개 호출 지점 통일: `btnSelectAll_Click`, `btnEdit_Click`, `LoadLabelingData` reset 블록 (line 679), `MainForm_Load` (line 179, DarkTheme.Apply 직후)
- JSON 저장 자동 reload 사이클(MainForm.cs:796-798 → LoadLabelingData → reset 블록) 에서 좌측 버튼 시각이 정상 동기화되어 DF-2-05 결함 해결
- 폼 최초 표시 시점에서도 currentMode 기본값(Select) 과 시각 일치 보장

## Task Commits

1. **Task 1: SetMode 헬퍼 도입 + 4개 호출 지점 통일** — `18f3126` (fix)

각 호출 지점 (수정 후):
- `MainForm.cs:179` — `SetMode(DrawMode.Select);` (MainForm_Load, DarkTheme.Apply 직후)
- `MainForm.cs:679` — `SetMode(DrawMode.Select);` (LoadLabelingData reset 블록)
- `MainForm.cs:1612` — `SetMode(DrawMode.Select);` (btnSelectAll_Click 본문)
- `MainForm.cs:1617` — `SetMode(DrawMode.Draw);` (btnEdit_Click 본문)

SetMode 정의 위치: `Forms/MainForm.cs:1591` (Drawing/Painting region 시작부, btnSelectAll_Click 직전)

```csharp
// FUNC-11 (DF-2-05): 모드 전환 시 currentMode + 좌측 버튼 BackColor + Cursor 를
// 한 곳에서 원자적으로 갱신하여 시각 동기화 결함 방지.
// (ARGB: 활성 = (59, 130, 246) — 파란색, 비활성 = (62, 62, 66) — 다크 톤)
private static readonly Color ModeButtonActiveColor   = Color.FromArgb(59, 130, 246);
private static readonly Color ModeButtonInactiveColor = Color.FromArgb(62, 62, 66);

private void SetMode(DrawMode mode)
{
    currentMode = mode;

    if (mode == DrawMode.Select)
    {
        btnSelectAll.BackColor = ModeButtonActiveColor;
        btnEdit.BackColor      = ModeButtonInactiveColor;
        pictureBoxVideo.Cursor = Cursors.Hand;
    }
    else // DrawMode.Draw
    {
        btnEdit.BackColor      = ModeButtonActiveColor;
        btnSelectAll.BackColor = ModeButtonInactiveColor;
        pictureBoxVideo.Cursor = Cursors.Cross;
    }
}
```

## Files Created/Modified

- `Forms/MainForm.cs` — SetMode 헬퍼 + ARGB 상수 추가, 4개 호출 지점 통일, FUNC-11 한국어 주석 보강

미수정 (가드):
- `Services/JsonService.cs` — D-05 락, plan 07-02 대상
- `Forms/MainForm.Designer.cs` — 버튼 바인딩 무변경

## Decisions Made

CONTEXT.md D-01..D-04 의 모든 결정을 그대로 반영:

- **D-01 (단일 헬퍼):** `SetMode(DrawMode)` 1개로 통합 — 채택
- **D-02 (4개 호출지점 통일):** btnSelectAll_Click, btnEdit_Click, LoadLabelingData reset, MainForm_Load — 채택. 필드 기본값(line 61) 무수정
- **D-03 (단축키 무수정):** D1/D2 → btnSelectAll_Click/btnEdit_Click 위임 유지 — 채택
- **D-04 (ARGB 캡슐화):** 헬퍼 내부 인접 위치에 `private static readonly Color ModeButton{Active,Inactive}Color` 정의 — 채택. inline FromArgb BackColor 대입은 0건

추가 미세 결정:
- 헬퍼 위치를 Drawing/Painting region 시작부(btnSelectAll_Click 직전) 에 배치 — 모드 전환 관련 코드의 응집도 유지
- MainForm_Load 의 SetMode 호출은 DarkTheme.Apply 직후 배치 — 다크 테마가 컨트롤 BackColor 를 적용한 후 모드 시각이 최종 갱신되도록 (호출 순서 보장)

## Deviations from Plan

None — plan executed exactly as written. CONTEXT.md D-01..D-04 모두 충족, plan 의 acceptance criteria 11개 모두 통과.

## Issues Encountered

None.

## 빌드 로그 요약

```
dotnet build C:/Users/ANNA/AOLTv1.0/ASLTv1.0.csproj -c Debug -v minimal
→ ASLTv1.0 -> C:\Users\ANNA\AOLTv1.0\bin\Debug\net8.0-windows\win-x64\ASLTv1.dll
→ 경고 39개 (베이스라인 39개 — 변동 없음)
→ 오류 0개
→ 경과 시간: 00:00:04.49
```

기존 경고 39건 (CS8632 nullable annotation 38건 + CS1998 async-without-await 1건) 은 본 plan 의 수정 범위 밖에 위치한 사전 발생 경고. 신규 경고 0건.

## FUNC-11 Acceptance Criteria 체크리스트

REQUIREMENTS.md FUNC-11 acceptance criteria 4개:

- [x] 메뉴 → JSON 저장 시 '전체선택' 모드가 내부적으로 활성화됨 (현재 동작) — `LoadLabelingData` reset 블록에서 `SetMode(DrawMode.Select)` 호출
- [x] 좌측 모드 버튼의 시각 표시가 '전체선택' 활성 상태로 갱신됨 (수정) — `SetMode` 헬퍼가 `btnSelectAll.BackColor = ModeButtonActiveColor` 적용
- [x] 메뉴 호출 후 사용자가 다른 모드를 누르지 않은 상태에서 좌측 버튼을 봤을 때 0번 (편집) 활성 표시가 남아 있지 않음 — 동일 헬퍼가 `btnEdit.BackColor = ModeButtonInactiveColor` 동시 적용
- [x] 회귀: 좌측 버튼 클릭으로 모드 전환 시에도 표시가 정확히 따라감 — `btnSelectAll_Click`/`btnEdit_Click` 핸들러가 SetMode 단일 호출로 위임

Plan acceptance criteria 11개 모두 통과:

- [x] `dotnet build` 성공 (warning 0 증가, error 0)
- [x] `grep -c "private void SetMode(DrawMode mode)" Forms/MainForm.cs` == 1
- [x] `grep -c "SetMode(DrawMode\." Forms/MainForm.cs` == 4
- [x] btnSelectAll_Click 본문이 `SetMode(DrawMode.Select);` 단일 statement
- [x] btnEdit_Click 본문이 `SetMode(DrawMode.Draw);` 단일 statement
- [x] line 675 reset 블록(현재 line 679)의 `currentMode = DrawMode.Select;` 가 `SetMode(DrawMode.Select);` 로 교체됨
- [x] MainForm_Load 본문에 `SetMode(DrawMode.Select);` 호출 1개 추가 (DarkTheme.Apply 직후, line 179)
- [x] `ModeButtonActiveColor` / `ModeButtonInactiveColor` 상수 정의됨, 헬퍼 내부에서 사용 (총 6 매치)
- [x] currentMode 필드 기본값(line 61) 변경되지 않음 (`DrawMode.Select` 그대로)
- [x] D1/D2 단축키 코드(현재 line 2935-2936) 변경되지 않음
- [x] Services/JsonService.cs 변경되지 않음 (`git diff --stat Services/JsonService.cs` 출력 비어있음)

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Plan 07-02 (FUNC-12, BBOX 1개 시 저장 차단) — Forms/MainForm.cs `btnExportJson_Click` (line ~716) 에서 가드 추가. 본 plan 과 무충돌 영역.
- Plan 07-03 (회귀 + 인스톨러 v1.0.3) — 본 plan 의 SetMode 동작이 통합 회귀에 포함되어야 함:
  - 시나리오: 영상 로드 → JSON 저장 → 자동 reload → 좌측 버튼 시각이 '전체선택' 활성으로 표시되는지 육안 확인
  - 시나리오: D1/D2 단축키 동작 회귀 0
  - 시나리오: 폼 최초 시작 시 좌측 버튼 시각이 '전체선택' 활성

블로커 없음.

## Self-Check: PASSED

자동 검증:
- 파일 존재: `Forms/MainForm.cs` ✓
- 커밋 존재: `18f3126` ✓
- SetMode 헬퍼 정의 1개 ✓
- SetMode 호출 4개 ✓
- 인라인 BackColor ARGB 0개 ✓
- ModeButton 상수 6 매치 ✓
- JsonService.cs 무수정 ✓
- D1/D2 단축키 무수정 ✓
- currentMode 필드 무수정 ✓

---
*Phase: 07-json-저장-결함수정*
*Completed: 2026-05-06*
