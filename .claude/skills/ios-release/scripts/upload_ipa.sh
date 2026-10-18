#!/usr/bin/env bash
# Validate or upload a FlagsRally .ipa to App Store Connect with altool and an App Store Connect API key.
#
#   bash .claude/skills/ios-release/scripts/upload_ipa.sh --check                 # setup only, nothing sent
#   bash .claude/skills/ios-release/scripts/upload_ipa.sh --validate <ipa>        # Apple checks it, nothing uploaded
#   bash .claude/skills/ios-release/scripts/upload_ipa.sh --upload <ipa>          # uses up the build number
#
# Needs ASC_API_KEY_ID, ASC_API_ISSUER_ID and ASC_API_KEY_P8 (path to the AuthKey_<id>.p8 file,
# outside the repository), set in the env of .claude/settings.local.json. Their values are never printed.
# Prints "UPLOADED: <ipa> <version> (<build>)" on success. Exit 1 on any problem.
set -uo pipefail

MODE=${1:?usage: upload_ipa.sh --check | --validate <ipa> | --upload <ipa>}
IPA=${2:-}

missing=0
for var in ASC_API_KEY_ID ASC_API_ISSUER_ID ASC_API_KEY_P8; do
  if [ -z "${!var:-}" ]; then echo "missing: $var is not set"; missing=1; else echo "$var: set"; fi
done
if [ -n "${ASC_API_KEY_P8:-}" ] && [ ! -f "$ASC_API_KEY_P8" ]; then echo "missing: ASC_API_KEY_P8 does not point to a file"; missing=1; fi
[ "$missing" = 0 ] || exit 1
[ "$MODE" = --check ] && { echo "setup OK"; exit 0; }

case "$MODE" in
  --validate) ACTION=(--validate-app "$IPA") ;;
  --upload) ACTION=(--upload-app -f "$IPA") ;;
  *) echo "unknown mode: $MODE"; exit 1 ;;
esac
[ -f "$IPA" ] || { echo "no such file: $IPA"; exit 1; }

# The numbers inside the package, so the report names exactly what was sent.
TMP=$(mktemp -d); trap 'rm -rf "$TMP"' EXIT
unzip -q "$IPA" 'Payload/*.app/Info.plist' -d "$TMP" || { echo "not an app package: $IPA"; exit 1; }
PLIST=$(ls "$TMP"/Payload/*.app/Info.plist)
VERSION=$(plutil -extract CFBundleShortVersionString raw "$PLIST")
BUILD=$(plutil -extract CFBundleVersion raw "$PLIST")
echo "package: $IPA, version $VERSION ($BUILD)"

xcrun altool "${ACTION[@]}" -t ios \
  --api-key "$ASC_API_KEY_ID" --api-issuer "$ASC_API_ISSUER_ID" --p8-file-path "$ASC_API_KEY_P8" \
  --show-progress
status=$?
[ "$status" = 0 ] || { echo "FAILED: altool exited with $status"; exit 1; }
if [ "$MODE" = --upload ]; then echo "UPLOADED: $IPA $VERSION ($BUILD)"; else echo "VALID: $IPA $VERSION ($BUILD)"; fi
