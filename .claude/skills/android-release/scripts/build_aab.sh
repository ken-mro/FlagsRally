#!/usr/bin/env bash
# Build the signed Android App Bundle (.aab) for Google Play, from a clean Release build.
#
#   bash .claude/skills/android-release/scripts/build_aab.sh
#
# Prints the path of the .aab on its last line. Must run on the merged master with the real
# Repository/Constants.cs in the working tree; it stops otherwise.
#
# The upload key is set up once by the user (see the skill's "One-time setup") in environment
# variables. The passwords are passed to MSBuild as env:NAME, so MSBuild reads them itself and they
# never appear in a command line, a log or this output:
#   FLAGSRALLY_KEYSTORE        path to the upload keystore (.keystore / .jks), outside the repository
#   FLAGSRALLY_KEY_ALIAS       key alias in it
#   FLAGSRALLY_STORE_PASS      keystore password
#   FLAGSRALLY_KEY_PASS        key password (often the same as the keystore password)
set -euo pipefail
cd "$(dirname "$0")/../../../.."   # the repository root, wherever this is run from

# The store build must be the released code with the real API keys.
[ "$(git rev-parse --abbrev-ref HEAD)" = master ] || { echo "FAIL: not on master; build the bundle from the merged master"; exit 1; }
if git diff --quiet HEAD -- Repository/Constants.cs; then
  echo "FAIL: Repository/Constants.cs is the committed version with dummy keys; the store build needs the real keys in the working tree"; exit 1
fi

missing=()
for name in FLAGSRALLY_KEYSTORE FLAGSRALLY_KEY_ALIAS FLAGSRALLY_STORE_PASS FLAGSRALLY_KEY_PASS; do
  [ -n "${!name:-}" ] || missing+=("$name")
done
if [ ${#missing[@]} -gt 0 ]; then
  echo "FAIL: not set: ${missing[*]} (the user sets these up once; see the skill's One-time setup)"; exit 1
fi
[ -f "$FLAGSRALLY_KEYSTORE" ] || { echo "FAIL: FLAGSRALLY_KEYSTORE does not point to a file"; exit 1; }
case "$(cd "$(dirname "$FLAGSRALLY_KEYSTORE")" && pwd)/" in
  "$(pwd)/"*) echo "FAIL: the keystore is inside the repository; keep it outside so it can never be committed"; exit 1 ;;
esac

OUT=bin/Release/net10.0-android/publish
rm -rf "$OUT"
LOG=$(mktemp -t flagsrally-aab.XXXXXX)
if ! dotnet publish FlagsRally.csproj -f net10.0-android -c Release \
    -p:AndroidPackageFormat=aab \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore="$FLAGSRALLY_KEYSTORE" \
    -p:AndroidSigningKeyAlias="$FLAGSRALLY_KEY_ALIAS" \
    -p:AndroidSigningStorePass=env:FLAGSRALLY_STORE_PASS \
    -p:AndroidSigningKeyPass=env:FLAGSRALLY_KEY_PASS \
    > "$LOG" 2>&1; then
  grep -E " error " "$LOG" | sort -u | head -20
  echo "FAIL: the bundle build failed (full log: $LOG)"; exit 1
fi

AAB=$(ls "$OUT"/*-Signed.aab 2>/dev/null | head -1)
[ -n "$AAB" ] || { echo "FAIL: no signed .aab in $OUT (log: $LOG)"; exit 1; }
# The upload key, not the debug key: Google Play rejects a bundle signed with the debug keystore.
# The Android build's own JDK first: macOS's /usr/bin/jarsigner is a stub that fails without a
# system Java, which would make a correctly signed bundle look unsigned.
JARSIGNER=$(ls "$HOME"/Library/Developer/Android/jdk/*/bin/jarsigner 2>/dev/null | head -1)
if [ -z "$JARSIGNER" ] && jarsigner -help > /dev/null 2>&1; then JARSIGNER=jarsigner; fi
[ -n "$JARSIGNER" ] || { echo "FAIL: no working jarsigner, so the signature cannot be checked"; exit 1; }
SIGNER=$("$JARSIGNER" -verify -verbose -certs "$AAB" 2>/dev/null | grep -m1 -o "CN=[^,]*" || true)
[ -n "$SIGNER" ] || { echo "FAIL: $AAB is not signed"; exit 1; }
if [ "$SIGNER" = "CN=Android Debug" ]; then
  echo "FAIL: $AAB is signed with the debug key"; exit 1
fi
echo "bundle built: $(du -h "$AAB" | cut -f1), signed by $SIGNER"
echo "$AAB"
