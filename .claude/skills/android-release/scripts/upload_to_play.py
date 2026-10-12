#!/usr/bin/env python3
"""Upload a signed .aab to Google Play with the release notes from /release-notes.

    python3 upload_to_play.py --aab <file.aab> --notes <notes.txt> --version 1.1.22 --validate-only
    python3 upload_to_play.py --aab <file.aab> --notes <notes.txt> --version 1.1.22          # draft
    python3 upload_to_play.py ... --status completed                  # publish to everyone
    python3 upload_to_play.py ... --status inProgress --fraction 0.2  # staged rollout to 20%
    python3 upload_to_play.py --show-track                            # what the track holds now (read-only)

--notes is the file holding exactly the block /release-notes printed (<en-US>...</en-US> and
<ja-JP>...</ja-JP>), so the notes on Google Play are the ones the user saw and approved.

--validate-only sends everything to Google Play and has it checked (signature, versionCode,
track, notes) but throws the edit away: nothing is published and the versionCode stays free.
Without it the edit is committed, which uses up the versionCode for good; the skill asks the
user right before that run.

The default status is "draft": the bundle and notes are on Google Play, and the user starts the
rollout in the Play Console. An edit that fails is deleted, so nothing half-done is left on
Google Play. If the connection drops during the final commit, Google Play may have committed it
anyway: run --show-track before trying again.

Credentials: GOOGLE_PLAY_SERVICE_ACCOUNT_JSON points to the service account key file (outside
the repository), set up once by the user. Its contents are never printed.
Needs: pip install google-api-python-client google-auth  (see the skill's One-time setup).
"""
from __future__ import annotations

import argparse
import os
import re
import sys
from pathlib import Path

PLAY_NOTES_LIMIT = 500  # characters per language, Play Console limit
REPO = Path(__file__).resolve().parents[4]


def read_notes(path: Path) -> dict[str, str]:
    text = path.read_text(encoding="utf-8")
    notes = {}
    for lang in ("en-US", "ja-JP"):
        match = re.search(rf"<{lang}>\s*\n(.*?)\n\s*</{lang}>", text, re.S)
        if not match or not match.group(1).strip():
            raise SystemExit(f"FAIL: no <{lang}> block in {path}")
        body = match.group(1).strip()
        if len(body) > PLAY_NOTES_LIMIT:
            raise SystemExit(f"FAIL: the {lang} notes are {len(body)} characters; Google Play allows {PLAY_NOTES_LIMIT}")
        notes[lang] = body
    return notes


def package_name() -> str:
    match = re.search(r"<ApplicationId>([^<]+)</ApplicationId>", (REPO / "FlagsRally.csproj").read_text(encoding="utf-8"))
    if not match:
        raise SystemExit("FAIL: ApplicationId not found in FlagsRally.csproj")
    return match.group(1).strip()


def manifest_version() -> tuple[str, int]:
    text = (REPO / "Platforms" / "Android" / "AndroidManifest.xml").read_text(encoding="utf-8-sig")
    name = re.search(r'android:versionName="([^"]+)"', text).group(1)
    code = int(re.search(r'android:versionCode="(\d+)"', text).group(1))
    return name, code


def main() -> int:
    parser = argparse.ArgumentParser(description="Upload a signed .aab to Google Play.")
    parser.add_argument("--aab", type=Path)
    parser.add_argument("--notes", type=Path, help="file with the /release-notes block")
    parser.add_argument("--version", help="the versionName being released, e.g. 1.1.22")
    parser.add_argument("--track", default="production", help="production (default), beta, alpha or internal")
    parser.add_argument("--status", default="draft", choices=["draft", "completed", "inProgress"])
    parser.add_argument("--fraction", type=float, help="share of users for --status inProgress, e.g. 0.2")
    parser.add_argument("--validate-only", action="store_true", help="check on Google Play without publishing")
    parser.add_argument("--not-sent-for-review", action="store_true",
                        help="commit with changesNotSentForReview (only when Google Play asks for it)")
    parser.add_argument("--show-track", action="store_true", help="print the track's releases and exit (read-only)")
    args = parser.parse_args()

    if args.show_track:
        edits, pkg = connect()
        edit_id = edits.insert(packageName=pkg, body={}).execute()["id"]
        try:
            track = edits.tracks().get(packageName=pkg, editId=edit_id, track=args.track).execute()
            for release in track.get("releases", []):
                print(f"{args.track}: {release.get('name')} codes={release.get('versionCodes')} status={release.get('status')}")
        finally:
            edits.delete(packageName=pkg, editId=edit_id).execute()
        return 0

    if not (args.aab and args.notes and args.version):
        parser.error("--aab, --notes and --version are needed (or use --show-track)")

    # Everything that can be checked locally is checked before talking to Google Play.
    notes = read_notes(args.notes)
    if not args.aab.is_file():
        raise SystemExit(f"FAIL: {args.aab} not found")
    name, code = manifest_version()
    if name != args.version:
        raise SystemExit(f"FAIL: AndroidManifest.xml says {name}, not {args.version}; upload from the merged master")
    if args.status == "inProgress" and not (args.fraction and 0 < args.fraction < 1):
        raise SystemExit("FAIL: --status inProgress needs --fraction between 0 and 1")
    edits, pkg = connect()
    from googleapiclient.http import MediaFileUpload

    edit_id = edits.insert(packageName=pkg, body={}).execute()["id"]
    committed = False
    try:
        print(f"{pkg}: uploading {args.aab.name} ({args.aab.stat().st_size // (1024 * 1024)} MB)...")
        bundle = edits.bundles().upload(
            packageName=pkg, editId=edit_id,
            media_body=MediaFileUpload(str(args.aab), mimetype="application/octet-stream", resumable=True)).execute()
        uploaded_code = int(bundle["versionCode"])
        if uploaded_code != code:
            raise SystemExit(f"FAIL: the bundle has versionCode {uploaded_code}, the manifest {code}; rebuild it. Nothing was published.")
        committed = publish(edits, pkg, edit_id, args, notes, uploaded_code)
    finally:
        if not committed:
            try:
                edits.delete(packageName=pkg, editId=edit_id).execute()
            except Exception:
                pass  # an edit not committed expires on its own
    return 0


def connect():
    key_file = os.environ.get("GOOGLE_PLAY_SERVICE_ACCOUNT_JSON", "")
    if not key_file or not Path(key_file).is_file():
        raise SystemExit("FAIL: GOOGLE_PLAY_SERVICE_ACCOUNT_JSON is not set to a file (see the skill's One-time setup)")
    if Path(key_file).resolve().is_relative_to(REPO):
        raise SystemExit("FAIL: the service account key is inside the repository; keep it outside")
    try:
        from google.oauth2 import service_account
        from googleapiclient.discovery import build
    except ImportError:
        raise SystemExit("FAIL: run `python3 -m pip install --user google-api-python-client google-auth` (see the skill's One-time setup)")
    creds = service_account.Credentials.from_service_account_file(
        key_file, scopes=["https://www.googleapis.com/auth/androidpublisher"])
    return build("androidpublisher", "v3", credentials=creds, cache_discovery=False).edits(), package_name()


def publish(edits, pkg, edit_id, args, notes, uploaded_code) -> bool:
    """Put the bundle on the track, then validate (and discard) or commit. True when committed."""
    release = {
        "name": args.version,
        "versionCodes": [str(uploaded_code)],
        "status": args.status,
        "releaseNotes": [{"language": lang, "text": text} for lang, text in notes.items()],
    }
    if args.status == "inProgress":
        release["userFraction"] = args.fraction
    edits.tracks().update(packageName=pkg, editId=edit_id, track=args.track,
                          body={"track": args.track, "releases": [release]}).execute()

    if args.validate_only:
        edits.validate(packageName=pkg, editId=edit_id).execute()
        print(f"VALID: {args.version} ({uploaded_code}) to {args.track} as {args.status} would be accepted. "
              "Nothing was published; the edit was discarded.")
        return False

    try:
        edits.commit(packageName=pkg, editId=edit_id, changesNotSentForReview=args.not_sent_for_review).execute()
    except Exception as error:
        if "changesNotSentForReview" in str(error):
            raise SystemExit("FAIL: Google Play asks for changesNotSentForReview (managed publishing). "
                             "Nothing was published. Ask the user, then run again with --not-sent-for-review.")
        raise
    print(f"UPLOADED: {args.version} ({uploaded_code}) to {args.track} as {args.status}, with en-US and ja-JP notes.")
    if args.status == "draft":
        print("It is a draft: start the rollout in the Play Console (Release > Production) when ready.")
    return True


if __name__ == "__main__":
    sys.exit(main())
