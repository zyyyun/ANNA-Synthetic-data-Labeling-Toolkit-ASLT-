# RELI-06 Partial-Fix Diagnosis

## Most Likely Root Cause

**Non-random seeking on newly-opened VideoCapture is inherently slow, compounded by unconditional `Set(PosFrames, N)` on every `LoadFrame` call and an auto-play timer that fires every 33 ms from the moment the video opens.**

The RELI-06 guard only blocked the **timeline** path. Three other paths still pump `LoadFrame(N)` at full speed while the OpenCV codec is still warming up (allocating decoder buffers, building its internal keyframe index, seeking from frame 0), and each of those calls does `videoCapture.Set(VideoCaptureProperties.PosFrames, N)` — which triggers a keyframe re-seek on a cold decoder. For the first few seconds this Set+Read round-trip takes ~500–1000 ms per call, producing the observed "~1 fps per second" pattern.

## Supporting Evidence

1. **Unguarded paths still exist.** `timerPlayback_Tick` (`Forms/MainForm.cs:774`), `ProcessCmdKey` arrow keys (`:2287–2290`), `btnRewind/Forward_Click` (`:797–809`), and waypoint clicks (`:1122,1138,1154,2100`) all call `LoadFrame(N)` without checking any "load settled" flag. Only timeline mouse input was guarded.

2. **Double initial load.** `VideoService.LoadVideoAsync` calls `LoadFrame(0)` at `:145`, then `MainForm.LoadVideoWithSubtitle` calls `_videoService.LoadFrame(0)` again at `:302`. Two cold seeks back-to-back before anything else happens.

3. **Auto-play fires immediately.** `:321` calls `btnPlay_Click(null, ...)` at the end of load — while `LoadLabelingData` / subtitle extraction are still touching disk. `timerPlayback.Interval = 33` (`:751`) so `timerPlayback_Tick` starts asking for new frames before the codec has warmed up.

4. **`framesToMove` masks slowness but doesn't eliminate it.** In `timerPlayback_Tick` (`:779`), `framesToMove = (int)(elapsedMs * speed / msPerFrame)`. Because `lastFrameTime` is refreshed **after** `LoadFrame` returns (`:785`), a slow seek does not cause frames to pile up into a huge jump — it just means the next tick reads a tiny elapsed delta and moves 1 frame. This is exactly why **speed no longer degrades** (RELI-06 fixed the accumulation), but frames still render at whatever rate `LoadFrame` can complete → "1 fps per second."

5. **WinForms message-loop contention.** Timer ticks, Paint, `pictureBoxVideo.Invalidate` (`:434`), and `UpdateBboxListDisplay` all compete on the UI thread while the synchronous `videoCapture.Set` blocks it. No async/offload for `LoadFrame`.

Hypothesis #1 (queued-mouse events) and #4 (bbox cache) are ruled out: guards now drop queued MouseDown events harmlessly, and `_bboxByFrame` is only rebuilt on edits (`InvalidateBoxCache`), not per frame.

## Recommended Fix Strategy

1. **Introduce a "ready" gate.** Add a `_isVideoReady` flag in `MainForm` set to `true` only after the first successful frame is painted AND `LoadLabelingData` completes. Guard every `LoadFrame` entry point (timer tick, keys, waypoints, rewind/forward) with it — not just timeline.
2. **Delay auto-play.** Do not call `btnPlay_Click` inside `LoadVideoWithSubtitle`. Start playback from the `Paint`-after-first-frame callback, or after a short throwaway pre-roll that performs 2–3 `LoadFrame(i)` calls to warm the OpenCV decoder before the timer starts.
3. **Remove the duplicate `LoadFrame(0)`** — either in `VideoService.LoadVideoAsync` or in `MainForm.LoadVideoWithSubtitle`, not both.
4. **Optional:** move `videoCapture.Set`+`Read` off the UI thread (Task.Run with a single-consumer seek queue) so the message loop is never blocked.

Steps 1–3 are cheap and should eliminate the initial-load lag without touching threading.
