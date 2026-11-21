#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["pyjwt[crypto]", "requests"]
# ///
"""Submit an uploaded FlagsRally iOS build for App Review through the App Store Connect API.

    uv run --locked submit_for_review.py --version 1.4.3 --build 28 --notes <notes.txt> --dry-run
    uv run --locked submit_for_review.py --version 1.4.3 --build 28 --notes <notes.txt>

Steps: find (or create) the App Store version, set its "What's New" text in every language of the
notes file, wait until Apple has finished processing the build, attach the build to the version,
then create a review submission holding that version and submit it. The version is released
automatically once approved (AFTER_APPROVAL), as earlier versions were.

--notes is the same file the Android release uploads (<en-US>...</en-US> and <ja-JP>...</ja-JP>
from /release-notes), so both stores show the same notes. en-US goes to App Store Connect's
"en-US" and ja-JP to "ja".

--dry-run only reads: it prints the version, the build and its processing state, the notes that
would be set and whether a submission is already open, and changes nothing.

Prints "SUBMITTED: <version> (<build>) state=WAITING_FOR_REVIEW" on success. Exit 1 on any problem.

Credentials: ASC_API_KEY_ID, ASC_API_ISSUER_ID and ASC_API_KEY_P8 (the same as upload_ipa.sh),
set in the env of .claude/settings.local.json. Their values are never printed.
"""
from __future__ import annotations

import argparse
import os
import re
import sys
import time
from pathlib import Path

import jwt
import requests

BASE = "https://api.appstoreconnect.apple.com"
BUNDLE_ID = "com.companyname.flagsrally"
LOCALES = {"en-US": "en-US", "ja-JP": "ja"}  # notes tag -> App Store Connect locale


def token() -> str:
    key = Path(os.environ["ASC_API_KEY_P8"]).read_text()
    now = int(time.time())
    return jwt.encode(
        {"iss": os.environ["ASC_API_ISSUER_ID"], "iat": now, "exp": now + 600, "aud": "appstoreconnect-v1"},
        key, algorithm="ES256", headers={"kid": os.environ["ASC_API_KEY_ID"]})


def call(method: str, path: str, body: dict | None = None, **params) -> dict:
    r = requests.request(method, BASE + path, params=params or None, json=body,
                         headers={"Authorization": "Bearer " + token()}, timeout=60)
    if r.status_code >= 400:
        sys.exit(f"HTTP {r.status_code} {method} {path}\n{r.text}")
    return r.json() if r.text else {}


def read_notes(path: Path) -> dict[str, str]:
    text = path.read_text(encoding="utf-8")
    notes = {}
    for tag, locale in LOCALES.items():
        m = re.search(rf"<{tag}>\s*\n(.*?)\n\s*</{tag}>", text, re.S)
        if not m or not m.group(1).strip():
            sys.exit(f"notes: no <{tag}> block in {path}")
        if len(m.group(1)) > 4000:
            sys.exit(f"notes: <{tag}> is longer than App Store Connect's 4000 characters")
        notes[locale] = m.group(1).strip()
    return notes


def main() -> int:
    p = argparse.ArgumentParser(description="Submit an uploaded iOS build for App Review.")
    p.add_argument("--version", required=True, help="CFBundleShortVersionString, e.g. 1.4.3")
    p.add_argument("--build", required=True, help="CFBundleVersion of the uploaded build, e.g. 28")
    p.add_argument("--notes", required=True, type=Path, help="file with the /release-notes block")
    p.add_argument("--dry-run", action="store_true", help="read only, change nothing")
    p.add_argument("--wait", type=int, default=900, help="seconds to wait for build processing (default 900)")
    args = p.parse_args()

    missing = [v for v in ("ASC_API_KEY_ID", "ASC_API_ISSUER_ID", "ASC_API_KEY_P8") if not os.environ.get(v)]
    if missing or not Path(os.environ["ASC_API_KEY_P8"]).is_file():
        sys.exit("missing setup: " + (", ".join(missing) or "ASC_API_KEY_P8 does not point to a file"))
    notes = read_notes(args.notes)

    apps = call("GET", "/v1/apps", **{"filter[bundleId]": BUNDLE_ID})["data"]
    if not apps:
        sys.exit(f"no app with bundle id {BUNDLE_ID}")
    app_id = apps[0]["id"]

    # The App Store version (App Store Connect copies the previous version's texts into a new one).
    versions = call("GET", f"/v1/apps/{app_id}/appStoreVersions",
                    **{"filter[platform]": "IOS", "filter[versionString]": args.version})["data"]
    version = versions[0] if versions else None
    state = version["attributes"]["appVersionState"] if version else "(not created yet)"
    print(f"version {args.version}: {state}")
    if version and state not in ("PREPARE_FOR_SUBMISSION", "DEVELOPER_REJECTED", "REJECTED", "METADATA_REJECTED"):
        sys.exit(f"version {args.version} is {state}; nothing to submit")

    builds = call("GET", "/v1/builds", **{"filter[app]": app_id, "filter[version]": args.build,
                                          "filter[preReleaseVersion.version]": args.version})["data"]
    if not builds:
        sys.exit(f"build {args.build} of {args.version} is not in App Store Connect (upload it first)")
    build = builds[0]
    print(f"build {args.build}: {build['attributes']['processingState']}")
    for locale, text in notes.items():
        print(f"\nWhat's New ({locale}):\n{text}")

    open_subs = [s for s in call("GET", "/v1/reviewSubmissions",
                                 **{"filter[app]": app_id, "filter[platform]": "IOS"})["data"]
                 if s["attributes"]["state"] in ("READY_FOR_REVIEW", "WAITING_FOR_REVIEW", "IN_REVIEW", "UNRESOLVED_ISSUES")]
    for s in open_subs:
        print(f"\nopen review submission {s['id']}: {s['attributes']['state']}")
    if args.dry_run:
        print("\nDRY RUN: nothing changed")
        return 0
    if any(s["attributes"]["state"] != "READY_FOR_REVIEW" for s in open_subs):
        sys.exit("another submission is already with App Review; resolve it in App Store Connect first")

    if not version:
        version = call("POST", "/v1/appStoreVersions", {"data": {
            "type": "appStoreVersions",
            "attributes": {"platform": "IOS", "versionString": args.version, "releaseType": "AFTER_APPROVAL"},
            "relationships": {"app": {"data": {"type": "apps", "id": app_id}}}}})["data"]
        print(f"created version {args.version}")
    vid = version["id"]

    locs = {l["attributes"]["locale"]: l["id"] for l in
            call("GET", f"/v1/appStoreVersions/{vid}/appStoreVersionLocalizations")["data"]}
    for locale, text in notes.items():
        if locale in locs:
            call("PATCH", f"/v1/appStoreVersionLocalizations/{locs[locale]}", {"data": {
                "type": "appStoreVersionLocalizations", "id": locs[locale], "attributes": {"whatsNew": text}}})
        else:
            call("POST", "/v1/appStoreVersionLocalizations", {"data": {
                "type": "appStoreVersionLocalizations", "attributes": {"locale": locale, "whatsNew": text},
                "relationships": {"appStoreVersion": {"data": {"type": "appStoreVersions", "id": vid}}}}})
        print(f"set What's New ({locale})")

    deadline = time.time() + args.wait
    while True:
        attrs = call("GET", f"/v1/builds/{build['id']}")["data"]["attributes"]
        if attrs["processingState"] == "VALID":
            break
        if attrs["processingState"] in ("FAILED", "INVALID"):
            sys.exit(f"build {args.build} processing ended {attrs['processingState']}")
        if time.time() > deadline:
            sys.exit(f"build {args.build} still {attrs['processingState']} after {args.wait}s; run again later")
        time.sleep(15)
    if attrs.get("usesNonExemptEncryption") is None:
        sys.exit("the build has no export compliance answer (ITSAppUsesNonExemptEncryption); "
                 "answer it in App Store Connect, then run again")

    call("PATCH", f"/v1/appStoreVersions/{vid}/relationships/build",
         {"data": {"type": "builds", "id": build["id"]}})
    print(f"attached build {args.build}")

    if open_subs:
        sub_id = open_subs[0]["id"]
    else:
        sub_id = call("POST", "/v1/reviewSubmissions", {"data": {
            "type": "reviewSubmissions", "attributes": {"platform": "IOS"},
            "relationships": {"app": {"data": {"type": "apps", "id": app_id}}}}})["data"]["id"]
    items = call("GET", f"/v1/reviewSubmissions/{sub_id}/items", include="appStoreVersion")["data"]
    if not any((i["relationships"].get("appStoreVersion", {}).get("data") or {}).get("id") == vid for i in items):
        call("POST", "/v1/reviewSubmissionItems", {"data": {
            "type": "reviewSubmissionItems",
            "relationships": {"reviewSubmission": {"data": {"type": "reviewSubmissions", "id": sub_id}},
                              "appStoreVersion": {"data": {"type": "appStoreVersions", "id": vid}}}}})
    sub = call("PATCH", f"/v1/reviewSubmissions/{sub_id}", {"data": {
        "type": "reviewSubmissions", "id": sub_id, "attributes": {"submitted": True}}})["data"]
    print(f"SUBMITTED: {args.version} ({args.build}) state={sub['attributes']['state']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
