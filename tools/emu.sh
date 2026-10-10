#!/usr/bin/env bash
# Helpers for checking FlagsRally on an Android emulator from Git Bash.
#
#   bash tools/emu.sh install            build and install the Debug APK
#   bash tools/emu.sh rebuild            clean rebuild + install (launch crashes, missing fonts/splash)
#   bash tools/emu.sh launch             (re)start the app and wait until it is in front
#   bash tools/emu.sh tab passport|map|collections|settings
#   bash tools/emu.sh shot <file.png>    save a screenshot
#   bash tools/emu.sh record <file.mp4> <seconds>
#   bash tools/emu.sh locale ja-JP|""    app language (empty = follow the system)
#   bash tools/emu.sh fontscale 1.3|1.0  system text size (restarts the app; set it back to 1.0)
#   bash tools/emu.sh db-backup          copy the app database aside (before destructive checks)
#   bash tools/emu.sh db-restore         put it back and print its hash
#   bash tools/emu.sh crash              show the last fatal exception in logcat
#
# Tab coordinates are for the Pixel 7 emulator (1080x2400).
set -euo pipefail
export MSYS_NO_PATHCONV=1   # keep Git Bash from rewriting /sdcard and similar paths

PKG=com.companyname.flagsrally
DB=files/FlagsRally.db3
BACKUP=files/FlagsRally.db3.emu-backup
# A Windows path: MSYS_NO_PATHCONV keeps /d/... from being converted, and dotnet reads it as a switch.
ROOT=$(cd "$(dirname "$0")/.." && pwd -W 2>/dev/null || pwd)

in_front() { adb shell dumpsys window | grep -q "mCurrentFocus.*$PKG"; }

case "${1:-}" in
  install)
    dotnet build "$ROOT/FlagsRally.csproj" -f net10.0-android -t:Install | grep -E " error |Build succeeded" | sort -u ;;
  rebuild)
    dotnet build "$ROOT/FlagsRally.csproj" -f net10.0-android -t:Rebuild | grep -E " error |Build succeeded" | sort -u
    dotnet build "$ROOT/FlagsRally.csproj" -f net10.0-android -t:Install | grep -E " error |Build succeeded" | sort -u ;;
  launch)
    # A failed Visual Studio debug session can leave this set and make every launch hang.
    adb shell setprop debug.mono.extra "''"
    adb shell am force-stop $PKG
    adb shell monkey -p $PKG -c android.intent.category.LAUNCHER 1 >/dev/null 2>&1
    for _ in $(seq 1 12); do sleep 5; in_front && break; done
    sleep 5
    in_front && echo "running" || { echo "not in front - try: bash tools/emu.sh crash"; exit 1; } ;;
  tab)
    case "${2:-}" in
      passport) x=135 ;; map) x=405 ;; collections) x=675 ;; settings) x=945 ;;
      *) echo "tab: passport|map|collections|settings"; exit 1 ;;
    esac
    adb shell input tap $x 2290 ;;
  shot)
    adb exec-out screencap -p > "${2:?file.png}" ;;
  record)
    adb shell screenrecord --time-limit "${3:-30}" /sdcard/emu-record.mp4
    adb pull /sdcard/emu-record.mp4 "${2:?file.mp4}" >/dev/null
    adb shell rm /sdcard/emu-record.mp4 ;;
  locale)
    adb shell cmd locale set-app-locales $PKG --locales "${2:-}" ;;
  fontscale)
    adb shell settings put system font_scale "${2:?1.3 or 1.0}" ;;
  db-backup)
    adb shell am force-stop $PKG
    adb shell run-as $PKG cp $DB $BACKUP
    adb shell run-as $PKG sha1sum $DB ;;
  db-restore)
    adb shell am force-stop $PKG
    adb shell run-as $PKG cp $BACKUP $DB
    adb shell run-as $PKG rm $BACKUP
    adb shell run-as $PKG sha1sum $DB ;;
  crash)
    adb logcat -d | grep -E "FATAL EXCEPTION|AndroidRuntime: (java|Caused)|MonoDroid: [A-Z]" | tail -10 || echo "no crash in the log" ;;
  *)
    sed -n '2,16p' "$0" ;;
esac
