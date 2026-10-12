#!/usr/bin/env bash
# Release smoke test on the Android emulator: a clean Release build, installed over the current
# app (data kept), launched and walked through every tab, then logcat is searched for crashes.
#
#   bash .claude/skills/android-release/scripts/release_smoke.sh <out-dir> [--no-build]
#
# Exit code 0 = no fatal problem found, 1 = something to look at (details in <out-dir>/report.txt).
# Screenshots of each tab go to <out-dir>. Run from the repository root.
# 1.1.20 passed every Debug check and still crashed on launch from Google Play: only a Release
# build shows what R8 / trimming / AOT remove, so this always builds Release from clean.
set -uo pipefail
export MSYS_NO_PATHCONV=1

OUT=${1:?usage: release_smoke.sh <out-dir> [--no-build]}
BUILD=1; [ "${2:-}" = "--no-build" ] && BUILD=0
PKG=com.companyname.flagsrally
mkdir -p "$OUT"
REPORT="$OUT/report.txt"; : > "$REPORT"
log() { echo "$*" | tee -a "$REPORT"; }

if [ -z "${ANDROID_SERIAL:-}" ]; then
  ANDROID_SERIAL=$(adb devices | awk '/^emulator-[0-9]+	device$/ { print $1; exit }')
  [ -n "$ANDROID_SERIAL" ] || { log "FAIL: no emulator running"; exit 1; }
fi
export ANDROID_SERIAL

if [ "$BUILD" = 1 ]; then
  log "== Clean Release build"
  if ! dotnet build FlagsRally.csproj -f net10.0-android -c Release -t:Rebuild > "$OUT/build.log" 2>&1; then
    grep -E " error " "$OUT/build.log" | sort -u | head -20 | tee -a "$REPORT"
    log "FAIL: Release build failed (see build.log)"; exit 1
  fi
  log "build succeeded"
fi

APK=$(ls -t bin/Release/net10.0-android/*-Signed.apk 2>/dev/null | head -1)
[ -n "$APK" ] || { log "FAIL: no signed Release APK in bin/Release/net10.0-android"; exit 1; }
log "== Install $APK"
adb install -r "$APK" >> "$REPORT" 2>&1 || { log "FAIL: install failed (a different signature? see report)"; exit 1; }

in_front() { adb shell dumpsys window | grep -q "mCurrentFocus.*$PKG"; }
alive() { [ -n "$(adb shell pidof $PKG | tr -d '\r')" ]; }

log "== Launch"
adb shell setprop debug.mono.extra "''"
adb shell am force-stop $PKG
adb logcat -c
adb shell am start -n "$(adb shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER $PKG | tail -1 | tr -d '\r')" > /dev/null
for _ in $(seq 1 12); do sleep 5; in_front && break; done
sleep 8   # startup work (subscriptions, database, maps) runs after the first frame
adb exec-out screencap -p > "$OUT/01_launch.png"

# Pixel 7 (1080x2400) tab positions, as in tools/emu.sh
step=2
for tab in passport:135 map:405 collections:675 settings:945; do
  name=${tab%%:*}; x=${tab##*:}
  alive || break
  adb shell input tap "$x" 2290; sleep 6
  adb exec-out screencap -p > "$OUT/0${step}_${name}.png"; step=$((step+1))
done
sleep 3

adb logcat -d > "$OUT/logcat.txt"
PROBLEMS=0
if ! alive; then log "FAIL: the app is no longer running"; PROBLEMS=1; fi
if ! in_front; then log "FAIL: the app is not in front (crashed, or a system dialog such as 'isn't responding')"; PROBLEMS=1; fi
APP_PIDS=$(grep -oE "Start proc [0-9]+:$PKG" "$OUT/logcat.txt" | grep -oE "[0-9]+" | sort -u | tr '\n' '|' | sed 's/|$//')
# A line counts when it comes from the app's process (logcat's third column) or names the
# package (ANRs are reported by the system process).
grep -E "FATAL EXCEPTION|AndroidRuntime: Process: $PKG|UNHANDLED EXCEPTION|ANR in $PKG|Main dispatcher is missing|Unable to start activity|java.lang.(NoClassDefFound|ClassNotFound|UnsatisfiedLink)Error|System.(TypeLoad|MissingMethod|EntryPointNotFound)Exception" "$OUT/logcat.txt" \
  | awk -v pids="|${APP_PIDS}|" -v pkg="$PKG" 'index(pids, "|" $3 "|") || index($0, pkg)' > "$OUT/fatal.txt"
if [ -s "$OUT/fatal.txt" ]; then
  log "FAIL: fatal lines in logcat (first 15, all in fatal.txt):"; head -15 "$OUT/fatal.txt" | tee -a "$REPORT"; PROBLEMS=1
fi
[ -n "$APP_PIDS" ] && log "app process ids: $APP_PIDS"

if [ "$PROBLEMS" = 0 ]; then log "OK: launched, opened all four tabs, no fatal lines in logcat"; fi
log "screenshots and logcat.txt: $OUT"
exit $PROBLEMS
