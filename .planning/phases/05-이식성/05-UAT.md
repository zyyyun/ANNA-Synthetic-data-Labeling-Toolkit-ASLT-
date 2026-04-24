---
status: partial
phase: 05-이식성
source:
  - .planning/phases/05-이식성/05-01-SUMMARY.md
  - .planning/phases/05-이식성/05-02-SUMMARY.md
started: 2026-04-17T00:00:00Z
updated: 2026-04-17T00:05:00Z
---

## Current Test

number: 2
name: Clean VM 설치 (PORT-01)
expected: |
  클린 Windows 10/11 x64 VM (.NET Runtime 미설치 상태)에서 `ASLT-Setup-v1.0.0.exe` 실행 →
  한국어 마법사 표시 → `C:\Program Files\ANNA\ASLT`에 설치 완료.
  설치 후 추가 설정 없이 시작 메뉴에서 ASLT 실행 가능.
awaiting: user response

## Tests

### 1. Installer Build (build.bat 실행)
expected: repo root에서 `installer\build.bat` 실행 시, prereq 체크 → dotnet publish → ISCC 컴파일이 순서대로 성공하고 `installer\Output\ASLT-Setup-v1.0.0.exe` (약 150-200MB)가 생성된다.
result: pass
actual: installer/Output/ASLT-Setup-v1.0.0.exe (99MB) 정상 생성. 크기는 예상보다 작으나 Inno Setup LZMA2 압축 효율로 설명됨.

### 2. Clean VM 설치 (PORT-01)
expected: 클린 Windows 10/11 x64 VM (.NET Runtime 미설치 상태)에서 `ASLT-Setup-v1.0.0.exe` 실행 → 한국어 마법사 표시 → `C:\Program Files\ANNA\ASLT`에 설치 완료. 설치 후 추가 설정 없이 시작 메뉴에서 ASLT 실행 가능.
result: blocked
blocked_by: other
reason: "사용자가 우선순위 낮춤 — 다른 항목 테스트 후 재개 예정. VM 미준비. 이전 시도에서 설치 완료 메시지는 떴으나 실제 설치 확인 안 됨 — VM에서 재검증 필요."

### 3. Self-contained Runtime 동작 (PORT-02)
expected: 클린 VM (.NET 8 Runtime 미설치)에서 설치된 `ASLTv1.exe` 실행 시 Runtime 오류 없이 앱이 정상 시작된다 (메인 창 표시, 다크 테마 적용). `coreclr.dll`이 설치 디렉토리에 있어 런타임이 번들되어 있음.
result: blocked
blocked_by: other
reason: "사용자가 우선순위 낮춤 — 다른 항목 테스트 후 재개 예정. VM 미준비. 이전 시도에서 설치 완료 메시지는 떴으나 실제 설치 확인 안 됨 — VM에서 재검증 필요."

### 4. 번들 FFmpeg 동작 (PORT-02)
expected: 설치된 앱에서 영상 파일 로드 후 자막 추출 기능이 정상 동작한다 (`<InstallDir>\ffmpeg\ffmpeg.exe`가 자동 탐지됨). FFmpeg 미설치 경고 MessageBox는 표시되지 않는다.
result: blocked
blocked_by: other
reason: "사용자가 우선순위 낮춤 — 다른 항목 테스트 후 재개 예정. VM 미준비. 이전 시도에서 설치 완료 메시지는 떴으나 실제 설치 확인 안 됨 — VM에서 재검증 필요."

### 5. 시작 메뉴 바로가기
expected: 설치 후 시작 메뉴에 "ASLT" (또는 "ANNA 합성데이터 라벨링 툴킷") 폴더 생성, 앱 실행 + 언인스톨 바로가기 존재. 클릭 시 앱이 정상 실행된다.
result: blocked
blocked_by: other
reason: "사용자가 우선순위 낮춤 — 다른 항목 테스트 후 재개 예정. VM 미준비. 이전 시도에서 설치 완료 메시지는 떴으나 실제 설치 확인 안 됨 — VM에서 재검증 필요."

### 6. 바탕화면 바로가기 (선택)
expected: 설치 중 "바탕화면 바로가기 만들기" 체크 시 → 바탕화면에 바로가기 아이콘 생성, 더블 클릭 시 앱 실행. 체크 해제 시 생성되지 않는다.
result: blocked
blocked_by: other
reason: "사용자가 우선순위 낮춤 — 다른 항목 테스트 후 재개 예정. VM 미준비. 이전 시도에서 설치 완료 메시지는 떴으나 실제 설치 확인 안 됨 — VM에서 재검증 필요."

### 7. 클린 제거 - 파일 (PORT-03)
expected: 제어판 또는 `unins000.exe`로 제거 시 `C:\Program Files\ANNA\ASLT\` 디렉토리 전체 삭제 (`logs\`, `ffmpeg\` 포함). 시작 메뉴 바로가기와 바탕화면 바로가기도 제거된다.
result: blocked
blocked_by: other
reason: "사용자가 우선순위 낮춤 — 다른 항목 테스트 후 재개 예정. VM 미준비. 이전 시도에서 설치 완료 메시지는 떴으나 실제 설치 확인 안 됨 — VM에서 재검증 필요."

### 8. 클린 제거 - 레지스트리 (PORT-03)
expected: 제거 후 `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\{B4A2C1F0-8E4D-4A6B-9F3A-ASLT10000001}_is1` 키가 삭제되고, 그 외 ANNA/ASLT 관련 커스텀 레지스트리 엔트리가 전혀 없다 (regedit 검색으로 "ASLT" 또는 "ANNA" 검색 시 no match).
result: blocked
blocked_by: other
reason: "사용자가 우선순위 낮춤 — 다른 항목 테스트 후 재개 예정. VM 미준비. 이전 시도에서 설치 완료 메시지는 떴으나 실제 설치 확인 안 됨 — VM에서 재검증 필요."

## Summary

total: 8
passed: 1
issues: 0
pending: 0
skipped: 0
blocked: 7

## Gaps

[none yet — VM 테스트 후 재평가]
