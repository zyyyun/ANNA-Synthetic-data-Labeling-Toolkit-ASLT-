---
phase: 07-json-저장-결함수정
plan: 02
subsystem: ui
tags: [winforms, json-export, defect-fix, waypoint, gs-certification]

# Dependency graph
requires:
  - phase: 5.6-결함수정 (v1.0)
    provides: "DF-1-04 0-BBOX 가드 패턴 — btnExportJson_Click 가드 위치/톤 모범"
  - phase: 07-01
    provides: "MainForm.cs 수정 안정화 — wave 1 완료 후 동일 파일 다른 region 수정"
provides:
  - "btnExportJson_Click 의 1-BBOX 가드 — Entry==Exit Waypoint 생성 차단"
  - "결함 객체 식별자 ([Label] [ID:D2]) 표시 안내 메시지 — 사용자 즉시 수정 지점 인지"
  - "JsonService.cs 무수정 정책 검증 — UI-side guard 패턴 확립"
affects: [07-03-회귀-인스톨러]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "UI-side save guard 패턴 — 저장 진입점에서 사전 검증 + 안내 메시지 + early return"
    - "GroupBy(Label, GetBoxId) 기반 트랙 미완성 검출 — Import-side matcher (JsonService.cs:527-531) 와 동일 키"

key-files:
  created: []
  modified:
    - "Forms/MainForm.cs"

key-decisions:
  - "가드 위치를 0-BBOX 가드 직후 (line 774, 0-BBOX 닫는 brace 와 loadingForm 선언 사이) 에 배치 — D-06 락 + 기존 DF-1-04 패턴 일관"
  - "Waypoint 매칭 조건을 JsonService.cs:527-531 와 동일하게 정의 (Label + ObjectId + FrameIndex 범위) — Import/Export 의 round-trip 일관성 보장"
  - "메시지 첫 문장을 line 1104 의 'Exit 프레임은 Entry 프레임보다 뒤에 있어야 합니다.' 와 동일 톤으로 'Exit 는 Entry 이후 프레임에서 설정해주세요.' 로 통일 — 수동 Exit 설정 시 표시되는 메시지와 흐름 일치 (USAB-03)"
  - "결함 객체 식별자 형식 [Label] [ID:D2] 채택 — line 1186 의 'Person ID {entryBox.PersonId:D2}' 자릿수 포맷과 동일"
  - "IsDeleted == true 인 박스는 GroupBy 전 .Where(b => !b.IsDeleted) 로 제외 — 삭제 마커가 미완성 트랙으로 잘못 감지되는 것 방지"
  - "Services/JsonService.cs 무수정 (D-05 락) — Export 측 silent skip 또는 Import 측 패치 채택 안 함, 사용자 흐름 일관성 우선"

patterns-established:
  - "UI-side save guard: 저장 진입점에서 GroupBy + .Any() 로 사전 검증 → MessageBox 표시 + early return → 저장 다이얼로그 미표시"
  - "Round-trip 매칭 키 일관성: UI 측 가드가 사용하는 Waypoint 매칭 predicate 는 JsonService.cs Import/Export 의 동일 predicate 와 1:1 매칭"

requirements-completed: [FUNC-12]

# Metrics
duration: ~2min
completed: 2026-05-06
---

# Phase 7 Plan 2: 1-BBOX 저장 가드 도입 (FUNC-12 / DF-2-06) Summary

**BBOX 1개만 존재하면서 매칭되는 Waypoint 가 없는 객체에 대해 JSON 저장을 차단하는 UI-side 가드를 `btnExportJson_Click` 에 추가 — JsonService 의 Entry==Exit Waypoint 자동 생성 흐름이 사용자 안내 메시지("Exit 는 Entry 이후 프레임") 와 모순되는 결함 0건화**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-05-06T02:34:47Z
- **Completed:** 2026-05-06T02:36:00Z
- **Tasks:** 1
- **Files modified:** 1 (Forms/MainForm.cs)
- **Lines:** +38, -0 (net +38)

## Accomplishments

- `btnExportJson_Click` 에 1-BBOX 가드 분기 추가 — 기존 0-BBOX 가드 직후 (line 774-810), 저장 다이얼로그 직전 위치
- LINQ GroupBy `(Label, GetBoxId)` 기반 미완성 트랙 검출 로직 — `sameObjectBoxes.Count == 1` AND no matching Waypoint 조건
- Waypoint 매칭 predicate 가 `Services/JsonService.cs:527-531` 의 import-side matcher 와 1:1 일치 — round-trip 일관성 보장
- 한국어 안내 메시지 톤이 기존 line 1104 의 "Exit 프레임은 Entry 프레임보다 뒤에 있어야 합니다." 와 매칭 (USAB-03 일관성)
- 메시지 본문에 결함 객체 식별자 `[Label] [ID:D2]` 표시 + "해결 방법:" 안내 (line 1200-1202 패턴 일관)
- `btnExportJson` 헤더 버튼 + `btnExportJsonInLabels` 라벨 그룹박스 버튼 양쪽이 동일 핸들러 (`btnExportJson_Click`) 를 공유하므로 단일 가드로 자동 보호
- Services/JsonService.cs 무수정 (D-05 락) — Import/Export 로직 변경 없음, KTC 1차 패턴 일관

## Task Commits

1. **Task 1: btnExportJson_Click 1-BBOX 가드 추가** — `9eb6940` (fix)

가드 삽입 위치: `Forms/MainForm.cs:774-810` (0-BBOX 가드 닫는 brace `}` 직후, `Form loadingForm = new Form` 선언 직전).

## 가드 로직 의사코드

```
unfinishedTracks = boundingBoxes
    .Where(IsDeleted == false)
    .GroupBy((Label, GetBoxId(box)))
    .Where(group =>
        group.Count == 1 AND
        not exists waypoint w where
            w.Label == group.only.Label AND
            w.ObjectId == GetBoxId(group.only) AND
            group.only.FrameIndex in [w.EntryFrame, w.ExitFrame]
    )
    .Select(g => (g.Key.Label, g.Key.ObjectId))

if unfinishedTracks.Count > 0:
    show MessageBox("JSON 저장 차단", warning, OK)
    return  # early return — 저장 다이얼로그 미표시
```

Waypoint 매칭 predicate 는 `Services/JsonService.cs:527-531` 의 import/export-side matcher 와 동일 (Label + ObjectId + FrameIndex 범위 in [EntryFrame, ExitFrame]).

## 표시되는 메시지 본문 전문

**Caption:** `JSON 저장 차단`
**Icon:** `MessageBoxIcon.Warning`
**Buttons:** `MessageBoxButtons.OK`

```
Exit 는 Entry 이후 프레임에서 설정해주세요. 하나의 BBOX 만 존재하는 객체가 있어 Waypoint 를 생성할 수 없습니다.

해당 객체: [person] [01], [vehicle] [03]

해결 방법: 해당 객체의 다른 프레임에서 BBOX 를 추가하거나, Entry/Exit 를 명시적으로 설정한 후 다시 저장해주세요.
```

(`해당 객체:` 뒤의 식별자 목록은 `string.Join(", ", ...)` 로 동적 생성. 결함 객체 N개에 대해 모두 나열)

## Files Created/Modified

- `Forms/MainForm.cs` — `btnExportJson_Click` 메서드 본문에 1-BBOX 가드 분기 추가 (+38 lines, line 774-810)

미수정 (가드):
- `Services/JsonService.cs` — D-05 락 (Import/Export 로직 변경 없음)
- `Forms/MainForm.Designer.cs` — 단일 핸들러 공유로 양쪽 진입점 자동 보호

## Decisions Made

CONTEXT.md D-05..D-10 의 모든 결정을 그대로 반영:

- **D-05 (저장 자체 차단):** Export 단계 silent skip 또는 Import 측 패치는 채택 안 함 — 채택. UI 측 진입점에서 차단
- **D-06 (가드 위치):** btnExportJson_Click 내 0-BBOX 가드 직후 — 채택. line 774 (0-BBOX 가드 닫는 `}` 직후, `Form loadingForm = new Form` 직전)
- **D-07 (검사 로직):** GroupBy(Label, GetBoxId) + sameObjectBoxes.Count == 1 + Waypoint 매칭 없음 — 채택
- **D-08 (메시지 톤):** 기존 line 1104 톤과 일관 + 결함 객체 식별자 노출 — 채택
- **D-09 (정상 케이스 회귀 0):** BBOX ≥ 2 또는 명시적 Waypoint 케이스는 가드 통과 — 채택. 빌드 + 빌드 경고 0 증가로 회귀 없음 확인
- **D-10 (DF-1-04 패턴 따름):** 0-BBOX 가드 위치/톤/early return 패턴 그대로 모방 — 채택

추가 미세 결정:
- 메시지 caption 을 `JSON 저장 차단` 으로 결정 — 기존 line 1203 의 `ID 불일치 경고` / line 1104 의 `Warning` 캡션과 톤 매칭 (한국어 + 차단 의미 명시)
- LINQ 표현식 들여쓰기를 0-BBOX 가드와 동일한 16-space (가드 본문 기준) 로 통일

## Deviations from Plan

None — plan executed exactly as written. CONTEXT.md D-05..D-10 모두 충족, plan 의 acceptance criteria 11개 모두 통과.

## Issues Encountered

None.

## 빌드 로그 요약

```
dotnet build C:/Users/ANNA/AOLTv1.0/ASLTv1.0.csproj -c Debug -v minimal
→ 경고 39개 (베이스라인 39개 — 변동 없음)
→ 오류 0개
→ 경과 시간: 00:00:01.37
```

기존 경고 39건 (CS8632 nullable annotation 38건 + CS1998 async-without-await 1건) 은 본 plan 의 수정 범위 밖에 위치한 사전 발생 경고. 신규 경고 0건. Plan 07-01 빌드 베이스라인과 동일.

## FUNC-12 Acceptance Criteria 체크리스트

REQUIREMENTS.md FUNC-12 acceptance criteria:

- [x] BBOX 1개만 존재하는 객체가 있고 해당 객체에 매칭되는 Waypoint 가 없으면 JSON 저장이 차단됨 — 가드 발동 + early return + 저장 다이얼로그 미표시
- [x] 사용자에게 표시되는 안내 메시지 톤이 기존 수동 Exit 안내 ("Exit 프레임은 Entry 프레임보다 뒤에 있어야 합니다.") 와 일관됨 — 메시지 첫 문장 "Exit 는 Entry 이후 프레임에서 설정해주세요." 적용
- [x] 정상 케이스 (모든 객체에 BBOX ≥ 2개 또는 명시적 Waypoint) 는 동작 변경 없이 정상 저장 — 빌드 경고 0 증가로 검증, 통합 회귀 테스트는 07-03
- [x] btnExportJson 헤더 버튼 + btnExportJsonInLabels 라벨 그룹박스 버튼 양쪽 진입점이 동일하게 보호됨 — Designer.cs:138, 721 양쪽이 동일 핸들러 공유 (가드 1회 추가로 양쪽 자동 보호)

Plan acceptance criteria 11개 모두 통과:

- [x] `dotnet build` 성공 (warning 0 증가, error 0)
- [x] btnExportJson_Click 에 1-BBOX 가드 분기 추가됨 — 기존 0-BBOX 가드 직후 위치 (line 774-810)
- [x] 가드 로직: `boundingBoxes.Where(!IsDeleted).GroupBy(Label, GetBoxId)` + sameObjectBoxes.Count == 1 + Waypoint 매칭 없음 검사
- [x] Waypoint 매칭 조건: `w.Label == box.Label && w.ObjectId == GetBoxId(box) && FrameIndex in [EntryFrame, ExitFrame]`
- [x] 가드 발동 시 MessageBox 메시지에 "Exit 는 Entry 이후 프레임에서 설정" 문구 포함
- [x] 메시지에 결함 객체 식별자 (`[Label] [ID]`) 표시
- [x] 메시지 caption "JSON 저장 차단", icon Warning, buttons OK
- [x] 가드 발동 시 early return — 저장 다이얼로그 미표시
- [x] IsDeleted == true 인 박스는 그룹핑에서 제외
- [x] Services/JsonService.cs 변경 없음 (`git diff --stat Services/JsonService.cs` 출력 비어있음)
- [x] Forms/MainForm.Designer.cs 변경 없음 (양쪽 진입점이 자동 보호됨)
- [x] 한국어 주석 prefix `// FUNC-12 (DF-2-06):` 사용 (기존 `// DF-1-04 (D-08):` 패턴 일관)

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Plan 07-03 (회귀 테스트 + 인스톨러 v1.0.3) 의 통합 회귀 시나리오에 본 가드 검증 시나리오 5개 포함 필요:
  1. BBOX 1개 (Waypoint 없음) → JSON 저장 → 가드 메시지 표시 + 저장 다이얼로그 미표시 확인
  2. BBOX 2개 (같은 객체) → JSON 저장 → 정상 저장 확인 (회귀 0)
  3. BBOX 0개 + Waypoint 0개 + 기존 JSON 파일 있음 → 저장 → 기존 0-BBOX 가드 (삭제 프롬프트) 동작 확인 (변경 없음)
  4. BBOX 1개 + 명시적 Waypoint 1개 (사용자가 수동 Entry/Exit 설정) → 저장 → 정상 통과 확인 (사용자 의도 존중)
  5. 결함 객체 다수 (예: person 1개 + vehicle 1개) → 메시지 본문에 두 객체 모두 식별자 표시 확인
- Plan 07-01 의 SetMode 회귀 시나리오와 함께 한 회차 사용자 회귀 테스트로 통합 가능

블로커 없음.

## Self-Check: PASSED

자동 검증:
- 파일 존재: `Forms/MainForm.cs` ✓
- 파일 존재: `.planning/phases/07-json-저장-결함수정/07-02-SUMMARY.md` ✓ (본 파일)
- 커밋 존재: `9eb6940` ✓
- 빌드 성공 (오류 0, 경고 39 = 베이스라인) ✓
- `FUNC-12 (DF-2-06)` 토큰 1개 ✓
- `unfinishedTracks` 토큰 3개 (선언 + .Count 사용 + .Select 사용) ✓
- `Exit 는 Entry 이후 프레임에서 설정` 토큰 2개 (주석 + 메시지) ✓
- `하나의 BBOX 만 존재하는 객체가 있어` 토큰 1개 ✓
- `JSON 저장 차단` 토큰 1개 ✓
- `JsonService.cs` 무수정 (`git diff --stat` 빈 출력) ✓
- `MainForm.Designer.cs` 무수정 (`git diff --stat` 빈 출력) ✓
- `btnExportJson_Click` 핸들러 바인딩 2개 유지 (헤더 + 라벨 그룹박스) ✓

---
*Phase: 07-json-저장-결함수정*
*Completed: 2026-05-06*
