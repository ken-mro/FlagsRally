#!/usr/bin/env bash
# Release smoke test on the iOS simulator: a clean Release build, installed over the current app
# (data kept), launched in English and walked through every tab, then checked for crashes.
#
#   bash .claude/skills/ios-release/scripts/release_smoke.sh <out-dir> [--udid <udid>] [--no-build]
#
# Exit code 0 = no fatal problem found, 1 = something to look at (details in <out-dir>/report.txt).
# Screenshots of each tab go to <out-dir>; the app is left on the Passport tab. Run from the repository root.
# Only a Release build shows what trimming removes, so this always builds Release from clean.
# The simulator build is not the App Store build (the device build is fully AOT-compiled), but it
# runs the same trimmed assemblies, which is where Release-only crashes have come from.
set -uo pipefail

OUT=${1:?usage: release_smoke.sh <out-dir> [--udid <udid>] [--no-build]}
shift
UDID=""; BUILD=1
while [ $# -gt 0 ]; do
  case "$1" in
    --udid) UDID=$2; shift 2 ;;
    --no-build) BUILD=0; shift ;;
    *) echo "unknown argument: $1"; exit 1 ;;
  esac
done
BUNDLE=com.companyname.flagsrally
APP=bin/Release/net10.0-ios/iossimulator-arm64/FlagsRally.app
mkdir -p "$OUT"
REPORT="$OUT/report.txt"; : > "$REPORT"
log() { echo "$*" | tee -a "$REPORT"; }

if [ -z "$UDID" ]; then
  UDID=$(xcrun simctl list devices booted | grep -oE '\([0-9A-F-]{36}\) \(Booted\)' | head -1 | grep -oE '[0-9A-F-]{36}')
  [ -n "$UDID" ] || { log "FAIL: no booted simulator"; exit 1; }
fi
log "simulator: $(xcrun simctl list devices | grep "$UDID" | sed 's/^ *//')"
command -v axe > /dev/null || { log "FAIL: axe not found (brew install cameroncooke/axe/axe)"; exit 1; }

if [ "$BUILD" = 1 ]; then
  log "== Clean Release build (simulator)"
  if ! dotnet build FlagsRally.csproj -f net10.0-ios -c Release -r iossimulator-arm64 -t:Rebuild > "$OUT/build.log" 2>&1; then
    grep -E " error " "$OUT/build.log" | sort -u | head -20 | tee -a "$REPORT"
    log "FAIL: Release build failed (see build.log)"; exit 1
  fi
  log "build succeeded"
fi
[ -d "$APP" ] || { log "FAIL: $APP not found"; exit 1; }

log "== Install $APP"
xcrun simctl install "$UDID" "$APP" >> "$REPORT" 2>&1 || { log "FAIL: install failed (see report)"; exit 1; }

# Crash reports of simulator apps land on the Mac, named after the process.
crash_reports() { ls ~/Library/Logs/DiagnosticReports 2>/dev/null | grep -E '^FlagsRally.*\.ips$' | sort; }
alive() { xcrun simctl spawn "$UDID" launchctl list 2>/dev/null | grep "UIKitApplication:$BUNDLE" | grep -qE '^[0-9]+'; }
BEFORE=$(crash_reports)

log "== Launch"
xcrun simctl launch --terminate-running-process "$UDID" $BUNDLE -AppleLanguages "(en)" >> "$REPORT" 2>&1 \
  || { log "FAIL: launch failed"; exit 1; }
sleep 15   # startup work (subscriptions, database, maps) runs after the first frame
xcrun simctl io "$UDID" screenshot "$OUT/01_launch.png" > /dev/null 2>&1

# Tabs are found by their accessibility label, so this works on any simulator size.
tab_center() {
  axe describe-ui --udid "$UDID" 2>/dev/null | python3 -c '
import json, sys
want = sys.argv[1]
def walk(n):
    if isinstance(n, list):
        for c in n: walk(c)
        return
    if n.get("AXLabel") == want and "Radio" in str(n.get("type")):
        f = n["frame"]; print(int(f["x"] + f["width"] / 2), int(f["y"] + f["height"] / 2)); sys.exit()
    walk(n.get("children", []))
walk(json.load(sys.stdin))' "$1"
}

PROBLEMS=0
step=2
for tab in Passport Map Collections Settings; do
  alive || break
  xy=$(tab_center "$tab")
  if [ -z "$xy" ]; then
    log "FAIL: the $tab tab was not found on screen (see 0${step}_${tab}.png)"; PROBLEMS=1
  else
    axe tap -x "${xy% *}" -y "${xy#* }" --udid "$UDID" > /dev/null 2>&1; sleep 6
  fi
  xcrun simctl io "$UDID" screenshot "$OUT/0${step}_${tab}.png" > /dev/null 2>&1; step=$((step+1))
done
# Back to the Passport tab, so later checks do not start on Settings.
xy=$(tab_center Passport); [ -n "$xy" ] && axe tap -x "${xy% *}" -y "${xy#* }" --udid "$UDID" > /dev/null 2>&1
sleep 3

if ! alive; then log "FAIL: the app is no longer running"; PROBLEMS=1; fi
NEW=$(comm -13 <(echo "$BEFORE") <(crash_reports))
if [ -n "$NEW" ]; then
  log "FAIL: new crash reports in ~/Library/Logs/DiagnosticReports:"; echo "$NEW" | tee -a "$REPORT"
  for f in $NEW; do cp ~/Library/Logs/DiagnosticReports/"$f" "$OUT/"; done
  PROBLEMS=1
fi

if [ "$PROBLEMS" = 0 ]; then log "OK: launched, opened all four tabs, no crash report"; fi
log "screenshots: $OUT"
exit $PROBLEMS
