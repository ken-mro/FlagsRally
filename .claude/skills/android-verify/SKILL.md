---
name: android-verify
description: Build FlagsRally, install it on the Android emulator and check a change with screenshots or a video (English and Japanese). Use after any UI or behaviour change, or when the user asks to see how something works.
---

# Check a change on the Android emulator

Run from the repository root in Git Bash. `tools/emu.sh` wraps the adb details (it sets `MSYS_NO_PATHCONV=1`).

1. **Tests first:** `dotnet test FlagsRally_xunit/FlagsRally_xunit/FlagsRally_xunit.csproj`
2. **Build and install:** `bash tools/emu.sh install`, then `bash tools/emu.sh launch`.
   - Still not "running", or logcat shows `valid TextAppearance ... Theme.MaterialComponents`, fonts fell back to Roboto, or the splash is plain purple: `bash tools/emu.sh rebuild`. This is stale build output, not your code.
   - Hangs at launch after a Visual Studio debug session: `launch` already clears `debug.mono.extra`. If it still hangs, cold-boot the emulator.
   - Anything else: `bash tools/emu.sh crash`.
3. **Before anything that changes data** (deleting boards, re-importing, check-ins): `bash tools/emu.sh db-backup`.
4. **Look at the change:** `bash tools/emu.sh tab <name>`, `adb shell input tap X Y` / `swipe`, then `bash tools/emu.sh shot <scratch>/x.png` and read the image yourself. Screen is 1080x2400, so screenshots are shown scaled by 1/1.2.
   - Drag a map pin: `adb shell input motionevent DOWN x y`, wait 1.5 s, a few `MOVE x y`, then `UP`.
   - Long press: `adb shell input swipe x y x y 1200`.
   - Taps sent in quick succession can be dropped; leave 2–3 s between taps that change the page.
5. **Japanese:** `bash tools/emu.sh locale ja-JP`, relaunch, check, then `bash tools/emu.sh locale ""`.
6. **Video for the user:** `bash tools/emu.sh record <scratch>/clip.mp4 60` in the background while you drive the app, or write a small script of taps (see `tools/emu.sh` for the pattern) and send the file.
7. **Put things back:** `bash tools/emu.sh db-restore` (it prints the hash; it must match the backup), locale back to default, and any setting you changed (pin style, folded sections) back to how it was.

Report what you saw, including anything that looked wrong, and say what you could not check (for example iOS, or the confirmation after a check-in when the paywall appears first).
