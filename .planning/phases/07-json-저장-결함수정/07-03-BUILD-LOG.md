# 07-03 Task 1: ASLT-Setup-v1.0.3.exe Build Log

**Plan:** 07-03 (Phase 7 클로즈아웃 — 빌드 + 회귀 + 인스톨러 검증)
**Task:** 1 — csproj 버전 확인 + 인스톨러 빌드
**Build run:** 2026-05-06
**Build script:** `installer/build-installer.ps1` (7단계 자동화)
**Status:** Build Successful (LASTEXITCODE = 0)

---

## 1. 사전 조건

- **csproj `<Version>` 사전 확인**: `1.0.3` (PowerShell `[xml]` 파싱 결과 정확히 일치 — bump 변경 없음)
- **Inno Setup 6 (ISCC.exe)**: 존재 확인 — `C:\Program Files (x86)\Inno Setup 6\ISCC.exe` (`Test-Path` = True)
- **Compiler engine version**: Inno Setup 6.7.1 (Non-commercial use only)
- **stale 1.0.3 인스톨러**: 빌드 전 `installer/Output/ASLT-Setup-v1.0.3.exe` 존재 → 스크립트 [3/7] clean step 에서 삭제 후 재생성
- **사전 옵션 확정**: Task 0 — "approved — 옵션 C" (1.0.2 가 사용자 환경에 이미 설치, 인스톨러 백업 불필요)

## 2. 7단계 빌드 결과

| Step | 단계                       | 결과                                             | 소요 시간 |
| ---- | -------------------------- | ------------------------------------------------ | --------- |
| 1/7  | 실행 중 ASLTv1.exe 종료    | PID 5148 종료                                    | <1s       |
| 2/7  | csproj 버전 확인           | Version: `1.0.3`                                 | <1s       |
| 3/7  | 빌드 산출물 정리 (clean)   | `bin/Release` 삭제, `installer/Output/*.exe` 정리; `installer/ffmpeg/` 미손상 | <1s |
| 4/7  | `dotnet publish`           | win-x64 self-contained 성공                      | **5.2s**  |
| 5/7  | publish 산출물 검증        | `ASLTv1.exe` (0.14 MB managed assembly) OK; `OpenCvSharpExtern.dll` 검증 통과 | <1s |
| 6/7  | ISCC.exe 컴파일            | "Successful compile (61.406 sec)" — Inno Setup 6.7.1 | **61.6s** |
| 7/7  | 인스톨러 검증              | `installer/Output/ASLT-Setup-v1.0.3.exe` 생성 확인 | <1s       |

**총 소요 시간:** 68.4s
**`LASTEXITCODE`:** 0
**빌드 종료 시각:** 2026-05-06 13:03:14 (LastWriteTime)

## 3. 빌드 산출물

| 항목 | 값 |
|---|---|
| Path | `C:\Users\ANNA\AOLTv1.0\installer\Output\ASLT-Setup-v1.0.3.exe` |
| 크기 | **98.18 MB** (102,952,293 bytes) |
| 임계값 | ≥ 50 MB (sanity threshold) — **Pass** |
| Last Modified | 2026-05-06 13:03:14 |
| Version (csproj) | 1.0.3 |
| SHA256 | `08EABFD86E8410559818261491F4E4D3B720B40CCFFCC6CFC19DAA60176403D7` |

비교: 1.0.2 인스톨러 = 98.19 MB → 1.0.3 = 98.18 MB (델타 -0.01 MB, 코드 변경 minimal — SetMode 헬퍼 + 1-BBOX 가드만 추가, 자원 추가 없음. 정상 범위).

## 4. ISCC 컴파일러 출력 핵심 라인 (인용)

```
Inno Setup 6 Command-Line Compiler
Compiler engine version: Inno Setup 6.7.1
Non-commercial use only
...
Successful compile (61.406 sec). Resulting Setup program filename is:
C:\Users\ANNA\AOLTv1.0\installer\Output\ASLT-Setup-v1.0.3.exe
```

### ISCC 경고 (non-blocking, 알려진 사항)

```
Warning: Architecture identifier "x64" is deprecated. Substituting "x64os",
  but note that "x64compatible" is preferred in most cases.
Warning: The [Setup] section directive "PrivilegesRequired" is set to "admin"
  but per-user areas (localappdata) are used by the script.
```

두 경고 모두 build success 에 영향 없음. 다음 milestone 에서 ASLT-Setup.iss 정리 시 검토 가능 (현재 plan scope 밖).

## 5. dotnet 빌드 경고 (non-blocking)

`CS8632 nullable 참조 형식` 경고 다수 — `<Nullable>disable</Nullable>` 환경에서 nullable 주석(`?`) 사용 시 발생. 코드 동작에는 영향 없음. Phase 7 scope (FUNC-11/12 결함 수정) 외 사항이므로 조치 안 함.

## 6. 빌드 스크립트 7단계 출력 (요약 인용)

```
=== ASLT Installer Build ===
[1/7] 실행 중인 ASLTv1.exe 종료...
  - PID 5148 종료
[2/7] csproj 버전 확인...
  - csproj Version: 1.0.3
[3/7] 빌드 산출물 정리...
  - bin/Release 삭제 완료
  - installer/Output 정리 완료
[4/7] dotnet publish 실행 중...
  ASLTv1.0 -> C:\Users\ANNA\AOLTv1.0\bin\Release\net8.0-windows\win-x64\publish\
  - publish 완료 (소요: 5.2s)
[5/7] publish 산출물 검증...
  - ASLTv1.exe (0.14 MB) OK
[6/7] ISCC.exe 컴파일 중...
  - ISCC 완료 (소요: 61.6s)
[7/7] 인스톨러 검증...

=== Build Successful ===
  Path:      C:\Users\ANNA\AOLTv1.0\installer\Output\ASLT-Setup-v1.0.3.exe
  Size:      98.18 MB
  Modified:  05/06/2026 13:03:14
  Version:   1.0.3
  Total:     68.4s
```

## 7. Acceptance Criteria 매핑 (Plan 07-03 Task 1)

| Criterion | 결과 |
|---|---|
| csproj Version == "1.0.3" 확인 (변경 없음) | Pass — XML 파싱 결과 `1.0.3` |
| `installer/build-installer.ps1` 7단계 모두 통과 — exit code 0 | Pass — `LASTEXITCODE = 0`, FINAL_EXIT_CODE=0 |
| `installer/Output/ASLT-Setup-v1.0.3.exe` 생성됨 | Pass — `Test-Path` = True |
| 인스톨러 크기 ≥ 50MB | Pass — 98.18 MB |
| 빌드 로그에 "Build Successful" 표시 | Pass — line 610 of full log |
| dotnet publish + ISCC 컴파일 모두 성공 (exit code 0) | Pass — publish 5.2s, ISCC 61.6s, 모두 success |
| 빌드 로그가 SUMMARY 인용 가능 형태로 보존됨 | Pass — 본 BUILD-LOG.md 가 committable docs 로 보존 |

## 8. 다음 단계

이 빌드 산출물은 Plan 07-03 의 다음 task 들이 사용:

- **Task 2** (수동 UAT — FUNC-11/12 회귀 검증): 본 인스톨러를 실행하여 라벨링 시나리오 14건 (A-1..A-4, B-1..B-4, C-1, C-2) 수행
- **Task 3** (수동 UAT — in-place 업그레이드): 옵션 C — 환경의 1.0.2 위에 본 인스톨러를 실행하여 업그레이드 검증
- **Task 4** (자동 — 감사 로그 무결성): Task 2/3 동작 후 `%LOCALAPPDATA%\ANNA\ASLT\logs\` 의 [AUDIT] 엔트리 검증
- **Task 5** (SUMMARY 작성): 본 BUILD-LOG.md 를 인용하여 07-03-SUMMARY.md 작성

## 9. Note: Binary 커밋 정책

`installer/Output/` 은 `.gitignore` 에 등록되어 있어 빌드 산출물 자체는 git tracked 되지 않음. 본 BUILD-LOG.md 가 빌드 이벤트의 인증 감사 추적 기록 역할을 한다 — 경로, 크기, SHA256, 빌드 일시가 모두 포함되어 재현/검증 가능.
