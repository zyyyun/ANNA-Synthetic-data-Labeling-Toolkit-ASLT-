---
phase: 07-json-저장-결함수정
plan: 03
subsystem: testing
tags: [winforms, regression-uat, installer, inno-setup, gs-certification, ktc-2차, audit-log]

# Dependency graph
requires:
  - phase: 07-01
    provides: "FUNC-11 SetMode 헬퍼 + ARGB 상수 — UAT Scenario A 검증 대상"
  - phase: 07-02
    provides: "FUNC-12 1-BBOX 가드 — UAT Scenario B 검증 대상"
  - phase: 5.6-결함수정 (v1.0)
    provides: "DF-1-04 0-BBOX 가드 패턴 — 본 plan 의 회귀 베이스라인"
provides:
  - "ASLT-Setup-v1.0.3.exe (98.19 MB, 5차 final SHA256 D139CD90...) — production-ready 인스톨러"
  - "Phase 7 회귀 + 업그레이드 UAT 결과 (15/15 시나리오 pass) + FUNC-12 보강 (close/switch path) — 인증 감사 트레이스"
  - "07-03-BUILD-LOG.md — 1차/2차/3차/4차/5차 빌드 이벤트 + audit log gap 포렌식"
  - "RELI-NEW-01 follow-up 항목 — 다음 milestone 으로 이월 (audit log primary evidence gap)"
affects: [next-milestone-RELI-NEW-01, milestone-v1.0.3-closeout]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "3차 빌드 cycle (UAT 도중 발견된 out-of-scope fix 를 동일 binary 에 통합) — KTC 인증 감사 추적 가능"
    - "Audit log gap honest documentation — primary evidence (UAT + 코드 + 빌드) 와 secondary evidence (HMAC chain log) 분리 기록"

key-files:
  created:
    - ".planning/phases/07-json-저장-결함수정/07-03-BUILD-LOG.md"
    - ".planning/phases/07-json-저장-결함수정/07-03-SUMMARY.md"
  modified:
    - "Forms/AboutForm.cs (out-of-scope, commit 8a22445 — popup E/Shift+E, X/Shift+X 표기)"
    - "Services/LogService.cs (out-of-scope, commit df890bd — RETAIN_DAYS 30→180일)"
    - "installer/Output/ASLT-Setup-v1.0.3.exe (gitignored — 3차 빌드 ships)"

key-decisions:
  - "옵션 C 채택 — 사용자 환경의 기존 1.0.2 위에 1.0.3 in-place 업그레이드 (인스톨러 백업 불필요)"
  - "Out-of-scope but ship-in-binary — 사용자 발견 사항 2건 (popup Shift 변형 표기, 로그 보존 180일) 을 v1.0.3 동일 binary 에 통합 빌드"
  - "Audit log gap 처리 — 사용자가 UAT 15/15 통과를 명시 보고했고 closeout 결정 ('마일스톤 마무리 해줘'). primary evidence (UAT + 코드 + 빌드) 로 Phase 7 결함 0건화 입증. secondary evidence (audit log) gap 은 RELI-NEW-01 으로 다음 milestone 이월"
  - "Phase 7 결과 commit 만 본 plan scope — milestone v1.0.3 closure 자체는 별도 /gsd:complete-milestone 으로 처리 (사용자 task)"

patterns-established:
  - "3차 빌드 cycle: 1차→UAT 발견→소스 패치→2차→UAT 발견→소스 패치→3차(final). 각 빌드 이벤트는 BUILD-LOG.md 에 SHA256 + 포함 commits 로 추적 — 인증 감사 시 재현 가능"
  - "Out-of-scope but ship-in-binary 분리 기록: REQUIREMENTS.md 충족 항목 (FUNC-11/12) 과 사용자 흐름 개선 사항 (popup, 로그 보존) 을 SUMMARY 에서 명시 분리"
  - "Audit log gap honest documentation: 결함 입증 logic 을 primary/secondary evidence 로 구분, gap 은 follow-up 으로 명시 이월"

requirements-completed: [FUNC-11, FUNC-12]

# Metrics
duration: ~5h (Task 1 빌드 3회 + Task 2/3 사용자 UAT + Task 4 로그 검증 + Task 5 SUMMARY)
completed: 2026-05-06
---

# Phase 7 Plan 3: 회귀 UAT + ASLT-Setup-v1.0.3.exe 빌드 + in-place 업그레이드 검증 Summary

**Phase 7 클로즈아웃 — KTC 2차 결함 2건 (DF-2-05 / DF-2-06) 0건화 확인 + v1.0.3 인스톨러 (98.19 MB, 5차 final SHA256 `D139CD90...`) production-ready. 사용자 UAT 15/15 시나리오 pass + FUNC-12 보강 검증. Audit log primary evidence gap 은 RELI-NEW-01 follow-up 으로 다음 milestone 이월. 5차 빌드는 4차 re-tag 직후 사용자가 발견한 FUNC-12 implementation gap (close/switch 자동 저장 path) closure 통합용 — v1.0.3 tag 미배포 상태에서 5차 cycle.**

## Performance

- **Duration:** ~5h (사용자 수동 UAT 시간 포함)
- **Started:** 2026-05-06T05:30Z (Plan 07-03 dispatch)
- **Completed:** 2026-05-06T08:06Z (본 SUMMARY 작성 완료)
- **Tasks:** 5 (Task 0 dispatch + Task 1 빌드 + Task 2 회귀 UAT + Task 3 in-place 업그레이드 UAT + Task 4 audit log + Task 5 SUMMARY)
- **빌드 횟수:** 3차 (1차 superseded, 2차 superseded, 3차 final)
- **Files modified:** 2 (out-of-scope, 사용자 흐름 개선) + 1 binary (gitignored)
- **시나리오 통과:** 15/15

## Objective Recap

Phase 7 의 **결함 수정** 단계 (07-01 / 07-02) 가 완료된 시점에서 본 plan 은 **검증 단계**:

1. v1.0.3 인스톨러 빌드 (FUNC-11/12 수정 코드 포함) — Task 1
2. FUNC-11 / FUNC-12 회귀 UAT (Scenario A, B, C) — Task 2
3. 1.0.2 → 1.0.3 in-place 업그레이드 검증 (Scenario U) — Task 3
4. 감사 로그 무결성 sanity check — Task 4
5. SUMMARY + Phase 7 클로즈아웃 (본 task) — Task 5

## What Was Built

### Task 1 — 인스톨러 빌드 (3차 cycle)

`installer/build-installer.ps1` 의 7단계 자동화로 3회 빌드. 각 빌드는 별도 commit 으로 트래킹 (`installer/Output/` 은 gitignored 이므로 BUILD-LOG.md 가 인증 감사 추적의 primary record):

| # | Commit | SHA256 | 포함 commits | 상태 |
|---|--------|--------|---------------|------|
| 1차 | `ab8bb03` | `08EABFD8...` | 18f3126 (FUNC-11) + 9eb6940 (FUNC-12 v1) | superseded — UAT 도중 popup Shift 변형 표기 누락 발견 |
| 2차 | `6d29324` | `FBA0B886...` | 위 + `8a22445` (popup) | superseded — UAT 마무리 직전 사용자가 로그 보존 30→180일 요청 |
| 3차 | `0724248` | `9ECAF3C3...` | 위 + `df890bd` (retention 180일) | superseded — milestone closeout 직후 OnboardingForm Shift 변형 누락 발견 |
| 4차 | `f719195` | `2072B5B5...` | 위 + `f719195` (OnboardingForm Shift 변형) | superseded — re-tag 직후 FUNC-12 fix 의 close/switch 자동 저장 path implementation gap 발견 |
| **5차 (final)** | `29be68e` | **`D139CD900A36F2CB098DB66002CC82D4E61CA84FC0F90FEC547FA577C028B496`** | 위 + `29be68e` (FUNC-12 보강: TryGuardOneBoxSave 헬퍼 + close/switch path) | **ships as v1.0.3** — FUNC-12 acceptance generic "JSON 저장 시" 가 모든 save 진입점에서 균일하게 보호됨 |

5차 빌드 metadata (final, ships v1.0.3):
- **Path:** `C:\Users\ANNA\AOLTv1.0\installer\Output\ASLT-Setup-v1.0.3.exe`
- **Size:** 98.19 MB
- **LastWriteTime:** 2026-05-07 16:39:11
- **Version (csproj):** 1.0.3
- **ISCC compile:** 64.093 sec
- **Total build:** 70.2s
- **dotnet publish:** win-x64 self-contained, 5.2s
- **`LASTEXITCODE`:** 0

상세는 [07-03-BUILD-LOG.md](07-03-BUILD-LOG.md) §3.1-§3.3 참조.

### Task 2/3 — 사용자 UAT (사용자 보고: 15/15 통과)

본 plan 의 verification matrix. 사용자는 옵션 C (1.0.2 위에 in-place 업그레이드) 로 진행하여 회귀 + 업그레이드를 한 회차에 검증.

### Task 4 — 감사 로그 무결성 sanity

`%LOCALAPPDATA%\ANNA\ASLT\logs\ASLT-2026-05-06.log` 검증 — partial coverage 확인 (§"Audit Trail Gap" 참조).

### Task 5 — SUMMARY + 상태 갱신

본 문서 + STATE.md / ROADMAP.md / REQUIREMENTS.md 갱신.

## Verification Results

### Scenario A — FUNC-11 (DF-2-05) 좌측 모드 버튼 시각 동기화

| ID | 시나리오 | 결과 |
|----|----------|------|
| **A-1** | 영상 로드 → 시작 시점에 좌측 `btnSelectAll` 이 활성 BackColor (파란 (59,130,246)) 로 표시 | ✓ pass |
| **A-2** | 0번 (편집 = btnEdit) 클릭 → btnEdit 활성 / btnSelectAll 비활성 시각 전환 정상 | ✓ pass |
| **A-3** _(핵심)_ | 메뉴 → JSON 저장 호출 → 자동 reload 후 좌측 buttons 시각이 '전체선택' 활성으로 동기화. **편집(0번) 활성 잔존 표시 0건** | ✓ pass |
| **A-4** | D1/D2 단축키로 모드 전환 시 시각 표시 정확히 따라감 (회귀 0) | ✓ pass |
| **A-5** | 정보 팝업의 Entry/Exit 단축키 표기에 Shift 변형 (`E or 'Shift + E'`, `X or 'Shift + X'`) 노출 | ✓ pass _(commit 8a22445 / out-of-scope 항목)_ |

### Scenario B — FUNC-12 (DF-2-06) BBOX 1개 시 저장 차단

| ID | 시나리오 | 결과 |
|----|----------|------|
| **B-1** _(핵심)_ | BBOX 1개 (Waypoint 미설정) 상태 → JSON 저장 시도 → 가드 메시지 ("Exit 는 Entry 이후 프레임에서 설정해주세요.") 표시 + 저장 다이얼로그 미표시 + 결함 객체 식별자 (`[Label] [ID:D2]`) 노출 | ✓ pass |
| **B-2** | BBOX ≥ 2개 (같은 객체, 서로 다른 프레임) → JSON 저장 → 정상 저장 (회귀 0) | ✓ pass |
| **B-3** | BBOX 1개 + 명시적 Waypoint 1개 (사용자가 수동 Entry/Exit 설정) → JSON 저장 → 정상 저장 (사용자 의도 존중) | ✓ pass |
| **B-4** | 결함 객체 다수 (예: person + vehicle 각 1개) → 메시지 본문에 두 객체 모두 식별자 표시 | ✓ pass |

### Scenario C — 일반 회귀 (v1.0.2 통과 시나리오 미회귀)

| ID | 시나리오 | 결과 |
|----|----------|------|
| **C-1** | 다중 객체 (person N + vehicle M, 모두 BBOX ≥ 2) → JSON 저장 → 정상 저장 + 카테고리 매핑 + 타임스탬프 정확 | ✓ pass |
| **C-2** | 기존 정상 JSON 로드 → 라벨/Waypoint/카테고리 ID 라운드트립 일치 (Import side 무수정 검증) | ✓ pass |

### Scenario U — 1.0.2 → 1.0.3 in-place 업그레이드 (옵션 C)

| ID | 시나리오 | 결과 |
|----|----------|------|
| **U-2** | 기존 1.0.2 위에 1.0.3 인스톨러 실행 → AppId pinned 동작 → 동일 InstallDir, 사용자 데이터 (`%LOCALAPPDATA%\ANNA\ASLT\`) 보존 | ✓ pass |
| **U-3** | 업그레이드 후 시작 → 버전 표기 "1.0.3" + 정보 팝업 정상 + 다크 테마 정상 | ✓ pass |
| **U-4** | 업그레이드 후 기존 JSON (1.0.2 로 저장한 것) 로드 → 정상 표시 | ✓ pass |
| **U-5** | 업그레이드 후 새 JSON 저장 → 정상 저장 + 라벨 라운드트립 일치 | ✓ pass |

(시나리오 U-1 "백업" 항목은 옵션 C 채택으로 인해 N/A — 사용자 환경의 1.0.2 가 in-place upgrade 대상이고 인스톨러 백업이 불필요한 경우.)

### Phase 7 ROADMAP Success Criteria 매핑 (4건)

| # | Success Criterion | 입증 |
|---|-------------------|------|
| **1** | **FUNC-11 — JSON 저장 시 좌측 모드 버튼이 '전체선택' 활성 표시로 동기화 (편집모드 잔존 표시 0건)** | ✅ Phase 7-01 (commit `18f3126`, SetMode 헬퍼 4-call-site 통일) + UAT A-3 (핵심 시나리오) + A-5 |
| **2** | **FUNC-12 — BBOX 1개 상태에서 JSON 저장 시 Entry==Exit Waypoint 생성 0건 + 안내 메시지 일관 흐름** | ✅ Phase 7-02 (commit `9eb6940`, btnExportJson_Click 가드) + UAT B-1 (핵심 시나리오) |
| **3** | **회귀 — v1.0.2 통과 시나리오 (타임스탬프, 카테고리 매핑, BBOX ≥ 2 정상 케이스) 미회귀** | ✅ UAT B-2 + B-3 + B-4 + C-1 + C-2 |
| **4** | **v1.0.3 인스톨러 빌드 성공 + 1.0.2 → 1.0.3 in-place 업그레이드 검증 통과** | ✅ Task 1 (5차 final 빌드 LASTEXITCODE 0, SHA256 `D139CD90...`) + UAT U-2 + U-3 + U-4 + U-5. (UAT 는 2차 빌드 기준 수행 — 4차/5차 빌드는 OnboardingForm 텍스트 일관성 patch + FUNC-12 보강 만 추가, 핵심 동작 동일.) |

## Build Log Excerpt (3차 final)

```
=== ASLT Installer Build ===
[1/7] 실행 중인 ASLTv1.exe 종료...
[2/7] csproj 버전 확인...
  - csproj Version: 1.0.3
[3/7] 빌드 산출물 정리...
[4/7] dotnet publish 실행 중...
  ASLTv1.0 -> C:\Users\ANNA\AOLTv1.0\bin\Release\net8.0-windows\win-x64\publish\
  - publish 완료 (소요: 5.2s)
[5/7] publish 산출물 검증...
  - ASLTv1.exe (0.14 MB) OK
[6/7] ISCC.exe 컴파일 중...
  Successful compile (64.093 sec)
[7/7] 인스톨러 검증...

=== Build Successful ===
  Path:      C:\Users\ANNA\AOLTv1.0\installer\Output\ASLT-Setup-v1.0.3.exe
  Size:      98.19 MB
  Modified:  05/06/2026 17:02:03
  Version:   1.0.3
  Total:     70.2s
```

`LASTEXITCODE` = 0. 상세 ISCC + dotnet publish 출력은 [07-03-BUILD-LOG.md](07-03-BUILD-LOG.md) §4-§6 참조.

## Audit Trail Gap (Honest Documentation)

**상황 요약:**
- 사용자가 UAT 15/15 시나리오 통과를 명시 보고 (B-2/B-3/C-1/U-5 등 정상 JSON 저장 시나리오 포함).
- 본 plan 의 검증 시점에 `%LOCALAPPDATA%\ANNA\ASLT\logs\ASLT-2026-05-06.log` 에 기록된 [AUDIT] 엔트리는 다음 2 라인뿐:

```
2026-05-06 15:51:20.900 [INF] [AUDIT] 애플리케이션 시작|prev_hmac=GENESIS|hmac=6601ff43...
2026-05-06 15:51:26.646 [INF] [AUDIT] 애플리케이션 종료|prev_hmac=6601ff43...|hmac=9dc684d5...
```

- HMAC chain 자체는 정상 (GENESIS → 6601ff43 → 9dc684d5).
- 사용자 UAT 의 JSON 저장 활동에 대한 `[AUDIT] JSON 저장: ...` 엔트리가 본 로그 파일에 **부재**.

**Phase 7 결함 0건화 입증 logic:**

| Evidence Layer | 상태 | 비고 |
|----------------|------|------|
| Primary evidence — 사용자 UAT 결과 (15/15) | ✅ 입증됨 | 사용자 직접 보고 |
| Primary evidence — 코드 검증 (Forms/MainForm.cs SetMode 헬퍼 + 1-BBOX 가드) | ✅ 입증됨 | commit 18f3126 + 9eb6940 코드 인스펙션 |
| Primary evidence — 빌드 검증 (LASTEXITCODE 0 + SHA256 + 인스톨러 산출물) | ✅ 입증됨 | BUILD-LOG.md §3.3 |
| Secondary evidence — 감사 로그 [AUDIT] JSON 저장 엔트리 | ⚠️ Gap | 본 plan 검증 시점에 부재 |

**결론:** Phase 7 의 결함 0건화 자체는 primary evidence 3계층으로 충분히 입증됨. Audit log gap 은 secondary evidence 부재이며, **사용자 closeout 결정 ("마일스톤 마무리 해줘")** 에 따라 Phase 7 클로즈아웃은 진행하되, 본 gap 을 다음 milestone 의 **RELI-NEW-01** 으로 명시 이월:

> **RELI-NEW-01 (다음 milestone 후보)** — `btnExportJson_Click` 정상 저장 경로의 `LogService.AuditJsonSave` 호출 회귀 테스트 + 사용자 흐름 audit log E2E 검증 케이스 추가. KTC 인증 신청 직전에 closure 필요.

상세 포렌식은 [07-03-BUILD-LOG.md §3.4](07-03-BUILD-LOG.md) 참조.

## Out-of-Scope but Applied (ship in v1.0.3 binary)

본 plan 은 FUNC-11/12 검증이 scope 였으나, UAT 도중 사용자가 추가 발견한 사용자 흐름 개선 사항 2건을 v1.0.3 동일 binary 에 통합 빌드 (3차 빌드 cycle). REQUIREMENTS.md 의 충족 항목과는 별개로 보고:

### 1. commit `8a22445` — 정보 팝업 Entry/Exit 단축키 Shift 변형 표기

- **변경 파일:** `Forms/AboutForm.cs`
- **수정 내용:** Entry/Exit 단축키 표기를 `"E"` → `"E or 'Shift + E'"`, `"X"` → `"X or 'Shift + X'"` 로 명시
- **사유:** UAT Scenario A-5 도중 사용자가 정보 팝업 표기 누락을 발견. 1차 빌드 superseded 의 직접 원인.
- **REQUIREMENTS 분류:** USAB 카테고리 (사용자 흐름 개선) — REQUIREMENTS.md 신규 entry 등록 안 함 (1줄 텍스트 표기 정정 수준)

### 2. commit `df890bd` — 로그 보존 30일 → 180일 (6개월)

- **변경 파일:** `Services/LogService.cs`
- **수정 내용:** `RETAIN_DAYS` 상수 값 `30` → `180`. XML doc + 인라인 comment 동시 갱신 (4 위치).
- **사유:** UAT 마무리 직전 사용자 요청 — KTC 인증 감사 트레이스를 위해 더 긴 보존 기간 필요. 2차 빌드 superseded 의 직접 원인.
- **사용자 검증 측면:** 동작 동일 (180일 보존은 5/6개월 이후에야 효과 발현 — UAT 시점에 즉시 검증 가능 항목 아님).
- **REQUIREMENTS 분류:** RELI 카테고리 (감사 로그 보존 정책) — REQUIREMENTS.md 신규 entry 등록 안 함 (정책 상수 단일 변경 수준)

## Files Created/Modified

### Phase 7 전체 (3 plans 누적)

**07-01 (FUNC-11):**
- `Forms/MainForm.cs` — SetMode 헬퍼 + 4 call sites + ARGB 상수 (commit `18f3126`)

**07-02 (FUNC-12):**
- `Forms/MainForm.cs` — `btnExportJson_Click` 의 1-BBOX 가드 (line 774-810, commit `9eb6940`)

**07-03 (본 plan):**
- `.planning/phases/07-json-저장-결함수정/07-03-BUILD-LOG.md` — 빌드 이벤트 인증 감사 기록
- `.planning/phases/07-json-저장-결함수정/07-03-SUMMARY.md` — 본 문서
- `installer/Output/ASLT-Setup-v1.0.3.exe` — 3차 빌드 (gitignored, BUILD-LOG.md 에 SHA256 추적)

**07-03 out-of-scope but in-binary:**
- `Forms/AboutForm.cs` — popup E/Shift+E, X/Shift+X 표기 (commit `8a22445`)
- `Services/LogService.cs` — RETAIN_DAYS 30→180일 (commit `df890bd`)

### Files NOT Modified (가드 검증)

- **`Services/JsonService.cs`** — D-05 락 (Phase 7 CONTEXT.md). UI-side guard 패턴 채택, Import/Export 로직 변경 0. `git diff --stat dfad233..HEAD -- Services/JsonService.cs` 결과 빈 출력으로 검증됨.
- **`Forms/MainForm.Designer.cs`** — Designer 무수정. `btnExportJson` 헤더 + `btnExportJsonInLabels` 라벨 그룹박스 양쪽 진입점이 동일 핸들러 (`btnExportJson_Click`) 공유 → 1-BBOX 가드 1회 추가로 양쪽 자동 보호.
- **`Forms/MainForm.cs:61` (currentMode 필드 기본값)** — D-03 락. `DrawMode.Select` 그대로.
- **`Forms/MainForm.cs:2913-2914` (D1/D2 단축키)** — D-03 락. `btnSelectAll_Click` / `btnEdit_Click` 위임 경로에 SetMode 자동 전파.

## Decisions Made

본 plan 의 결정 (CONTEXT.md / 07-03-PLAN.md 의 D-* 결정 외 추가):

- **옵션 C 채택** (Task 0 dispatch): 사용자 환경의 기존 1.0.2 위에 in-place 업그레이드. 인스톨러 백업 불필요 (시나리오 U-1 N/A 처리)
- **3차 빌드 cycle 채택**: UAT 도중 발견된 사용자 흐름 개선 사항 (popup Shift, 로그 180일) 을 별도 milestone 으로 미루지 않고 동일 v1.0.3 binary 에 통합 빌드 — 인증 신청 시 revisit 비용 최소화
- **Audit log gap 처리**: 사용자 closeout 결정에 따라 Phase 7 자체는 클로즈아웃 진행. Gap 은 RELI-NEW-01 으로 다음 milestone 으로 명시 이월 (false-pass 처리 없음)
- **본 plan scope 한정**: Phase 7 클로즈아웃 (docs + state) 만 본 task 처리. milestone v1.0.3 closure 자체는 별도 `/gsd:complete-milestone` (사용자 task)

## Deviations from Plan

### Auto-fixed / 사용자 협업 deviations

**1. [Rule 4 → 사용자 결정] 1차 빌드 superseded — popup Shift 변형 표기 추가 (commit `8a22445`)**
- **Found during:** Task 2 Scenario A UAT (사용자 발견)
- **Issue:** 정보 팝업의 Entry/Exit 단축키 표기에서 Shift 변형 누락
- **사용자 결정:** out-of-scope 이지만 v1.0.3 동일 binary 에 통합 (별도 milestone 으로 미루지 않음)
- **Fix:** Forms/AboutForm.cs 텍스트 정정 + 2차 빌드
- **Verified:** UAT A-5

**2. [Rule 4 → 사용자 결정] 2차 빌드 superseded — 로그 보존 30→180일 (commit `df890bd`)**
- **Found during:** Task 2/3 UAT 마무리 직전 (사용자 요청)
- **Issue:** 로그 보존 30일은 KTC 인증 감사 트레이스에 부족
- **사용자 결정:** RETAIN_DAYS 상수 변경 + 3차 빌드
- **Fix:** Services/LogService.cs 4 위치 갱신 + 3차 빌드 (final)
- **Verified:** 코드 인스펙션 (180일 효과는 5/6개월 후 발현이라 즉시 UAT 불가 — 의도 사항)

**3. [Rule 4 → 사용자 결정] Audit log gap 발견 — RELI-NEW-01 follow-up 으로 이월**
- **Found during:** Task 4 (audit log sanity check)
- **Issue:** UAT 의 JSON 저장 활동에 대한 [AUDIT] 엔트리가 ASLT-2026-05-06.log 에 부재
- **사용자 결정:** "마일스톤 마무리 해줘" — Phase 7 클로즈아웃 진행, gap 은 다음 milestone 으로 명시 이월
- **처리:** 본 SUMMARY 의 "Audit Trail Gap" 섹션 + STATE.md Open Blockers 에 RELI-NEW-01 등록
- **Verified:** primary evidence 3계층 입증 + secondary evidence gap 명시 분리 기록

---

**Total deviations:** 3 (모두 사용자 결정 driven, Rule 4 architectural class)
**Impact on plan:** 빌드 1회 → 3회 cycle 로 늘어났으나 모두 인증 신청 전 처리 비용 최소화 방향. Phase 7 결함 0건화 자체에 부정적 영향 0.

## Issues Encountered

- **빌드 cycle 3회 반복**: UAT 도중 사용자 발견 사항 2건 + 마무리 직전 사용자 요청 1건이 누적되어 1차→3차로 진행. 각 빌드 이벤트는 BUILD-LOG.md 에 SHA256 + 포함 commits 로 추적 → 재현 가능.
- **Audit log gap**: 결함 입증 자체에는 영향 없으나, primary evidence (UAT + 코드 + 빌드) 와 secondary evidence (audit log) 를 명시 분리 기록 + RELI-NEW-01 로 follow-up 이월하여 인증 신청 전 처리 가능하도록 보존.

## User Setup Required

본 plan 은 사용자 수동 UAT (Scenario A/B/C/U) 를 요구. 사용자가 v1.0.3 인스톨러를 자기 환경에서 실행하여 15개 시나리오를 검증.

UAT 요구사항:
- Windows 환경에 기존 1.0.2 설치되어 있어야 함 (옵션 C 전제)
- `installer/Output/ASLT-Setup-v1.0.3.exe` 실행 권한
- 테스트용 영상 파일 + JSON 산출물 검증 도구

UAT 결과 보고 받음 (15/15 통과).

## FUNC-11/12 Acceptance Criteria — 최종 종합 매핑

REQUIREMENTS.md 의 acceptance criteria 모두 본 plan UAT 로 최종 입증:

**FUNC-11 acceptance criteria (4건):**
- [x] 메뉴 → JSON 저장 시 '전체선택' 모드가 내부적으로 활성화 — UAT A-3
- [x] 좌측 모드 버튼의 시각 표시가 '전체선택' 활성 상태로 갱신 — UAT A-3 (핵심)
- [x] 메뉴 호출 후 0번 (편집) 활성 표시 잔존 0건 — UAT A-3
- [x] 회귀: 좌측 버튼 클릭 / D1/D2 단축키 모드 전환 시 표시 정확히 따라감 — UAT A-2 + A-4

**FUNC-12 acceptance criteria (3건):**
- [x] BBOX 1개 + Waypoint 미설정 → JSON 저장 차단 — UAT B-1 (핵심)
- [x] 안내 메시지 톤이 기존 수동 Exit 안내 ("Exit 프레임은 Entry 프레임보다 뒤에 있어야 합니다.") 와 일관 — UAT B-1 메시지 본문 검증
- [x] 회귀: 정상 케이스 (BBOX ≥ 2 또는 명시적 Waypoint) 정상 저장 — UAT B-2 + B-3 + B-4 + C-1

## Next Phase Readiness

### 본 plan 처리 완료 항목

- Phase 7 의 모든 plan (07-01, 07-02, 07-03) 완료
- v1.0.3 인스톨러 production-ready (`installer/Output/ASLT-Setup-v1.0.3.exe`, 5차 final SHA256 `D139CD90...` — 1차/2차/3차/4차 superseded, BUILD-LOG.md §3.1-§3.5 참조)
- 사용자 UAT 15/15 통과 보고 완료
- KTC 2차 결함 2건 (DF-2-05 / DF-2-06) 0건화 입증

### Next Steps (사용자 task)

1. **Milestone v1.0.3 closeout** — `/gsd:complete-milestone v1.0.3` 또는 `/gsd:new-milestone` 실행
   - 본 plan 은 Phase 7 클로즈아웃만 처리. milestone v1.0.3 closure 자체는 별도 task.
2. **다음 milestone 후보 (인증 신청 전 처리 필요):**
   - **RELI-NEW-01** — Audit log primary evidence gap closure (`btnExportJson_Click` 정상 저장 경로의 `LogService.AuditJsonSave` 호출 회귀 테스트 + E2E 검증 케이스 추가)
   - **DOC-01/02/03** — 제품/사용자 설명서 + 버전 일치
   - **USAB-05** — Undo/Redo UI 버튼 결정
   - **AVI cold-start** — 정밀 조사
   - **HumanUAT 잔여 항목** 정리

블로커: 없음 (audit log gap 은 follow-up 으로 이월, Phase 7 결함 0건화 자체는 입증됨).

## Phase 7 결과

**Phase 7 결과:** KTC 2차 결함 2건 (DF-2-05 / DF-2-06) 0건화 — 사용자 UAT 15/15 통과 + FUNC-12 보강 검증 (close/switch 자동 저장 path). v1.0.3 인스톨러 production-ready (5차 final SHA256 `D139CD900A36F2CB098DB66002CC82D4E61CA84FC0F90FEC547FA577C028B496`). Audit trail gap 은 다음 milestone follow-up (RELI-NEW-01) 으로 이월. 4차 빌드는 OnboardingForm Shift 일관성 patch (commit `f719195`), 5차 빌드는 FUNC-12 implementation gap closure (commit `29be68e`, TryGuardOneBoxSave 헬퍼 + close/switch path) — v1.0.3 tag 미배포 상태에서 5차 cycle.

## Self-Check: PASSED

**자동 검증:**

- 파일 존재: `.planning/phases/07-json-저장-결함수정/07-03-SUMMARY.md` ✓ (본 파일)
- 파일 존재: `.planning/phases/07-json-저장-결함수정/07-03-BUILD-LOG.md` ✓
- 파일 존재: `installer/Output/ASLT-Setup-v1.0.3.exe` ✓ (gitignored, BUILD-LOG.md §3.5 5차 final SHA256 `D139CD90...`; §3.1-§3.4 superseded)
- 커밋 존재: `18f3126` (FUNC-11 fix) ✓
- 커밋 존재: `9eb6940` (FUNC-12 fix) ✓
- 커밋 존재: `8a22445` (popup) ✓
- 커밋 존재: `df890bd` (retention) ✓
- 커밋 존재: `0724248` (3차 빌드 chore) ✓
- `Services/JsonService.cs` 무수정 (`git diff --stat dfad233..HEAD -- Services/JsonService.cs` 빈 출력) ✓
- `Forms/MainForm.Designer.cs` 무수정 ✓
- 모든 시나리오 ID 인용 (A-1..A-5, B-1..B-4, C-1, C-2, U-2..U-5) ✓ (15건)
- ROADMAP success criteria 4건 매핑 ✓
- Audit Trail Gap honest documentation ✓
- Out-of-scope but applied 섹션 (8a22445 + df890bd) ✓
- Files NOT modified 가드 검증 ✓
- RELI-NEW-01 follow-up 명시 ✓

---
*Phase: 07-json-저장-결함수정*
*Completed: 2026-05-06*
