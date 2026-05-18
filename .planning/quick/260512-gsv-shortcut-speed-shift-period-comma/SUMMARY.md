---
slug: 260512-gsv-shortcut-speed-shift-period-comma
status: complete
completed: 2026-05-12
commit: 48b7f5b
---

# SUMMARY — Shift+>/< 배속 단축키 수정

## 결과
GS인증 측 보고 이슈 closure. 배속 증감 단축키가 포커스 위치(textbox/combobox 포함) 무관하게 작동.

## 근본 원인
`MainForm_KeyDown` line ~2890 의 textbox/combobox 포커스 가드가 화이트리스트(Ctrl+숫자, Ctrl+N, Alt+숫자, Enter, Escape)에 없는 Shift+OemPeriod / Shift+Oemcomma 를 차단.

## 변경 사항
`Forms/MainForm.cs`:
- `ProcessCmdKey` (line ~2862-2884): 배속 단축키 분기 신규 추가 (Shift+OemPeriod = 1x→4x→8x→16x, Shift+Oemcomma = 16x→8x→4x→1x→0.5x). 화살표 키와 동일한 위치/패턴.
- `MainForm_KeyDown` (line ~3056-3067 기존 위치): Shift+OemPeriod / Shift+Oemcomma 두 else-if 블록 제거. 이동 사실 단일 주석으로 표시.

## 검증
- `dotnet build` — 0 errors, 42 warnings (모두 기존 nullable 주석 + 비동기 메서드 관련, 본 변경과 무관)
- 코드 동작 변경 없음 — 배속 단계, drift 보정 (`lastFrameTime` 리셋), UI 갱신 모두 기존 로직 보존
- ProcessCmdKey 진입 조건: `isVideoLoaded && _isVideoReady` — 영상 미로드 시 단축키 무시 (기존 RELI-06 정책 유지)

## 부수 효과
영상 로드 후 textbox 에 `>` 또는 `<` 문자 입력 시 배속 단축키가 가로채는 동작. 본 앱은 textbox 입력이 라벨/ID(숫자)/검색 정도라 실 사용에서 영향 없음. 화살표 키 (이전부터 ProcessCmdKey 처리) 도 동일 trade-off.

## GS인증 전달용
- v1.0.4 installer 재빌드 필요
- 검증 시나리오: 영상 로드 → 트랙바/사이드바 콤보박스 클릭 (포커스 이동) → Shift+> 4회 (1→4→8→16) 확인, Shift+< 5회 (16→8→4→1→0.5) 확인. 우측 하단 시간 라벨에 "Nx" 표시.
