#!/usr/bin/env bash
# Put the Debug build back on the emulator over a Release build, keeping the app's data, and
# optionally restore the database backup made with `tools/emu.sh db-backup`.
#
#   bash .claude/skills/android-release/scripts/debug_build_back.sh             # before db-backup
#   bash .claude/skills/android-release/scripts/debug_build_back.sh --restore   # after the checks
#
# Why not just `tools/emu.sh install`: its MSBuild Install step remembers what it installed last
# and skips the install when the Debug output has not changed, even if a Release build was put on
# the device since. The Release build is not debuggable, so `run-as` (used by db-backup and
# db-restore) then fails. This builds Debug and installs the APK with adb, which always replaces it.
set -euo pipefail
export MSYS_NO_PATHCONV=1
PKG=com.companyname.flagsrally
APK=bin/Debug/net10.0-android/$PKG-Signed.apk

bash tools/emu.sh install
[ -f "$APK" ] || { echo "FAIL: $APK not found after the Debug build"; exit 1; }
adb install -r "$APK" > /dev/null
if ! adb shell dumpsys package $PKG | grep -q DEBUGGABLE; then
  echo "FAIL: the installed app is still not debuggable"; exit 1
fi
echo "Debug build installed (debuggable, data kept)"

if [ "${1:-}" = "--restore" ]; then
  bash tools/emu.sh db-restore   # prints the hash; compare it with the one db-backup printed
fi
