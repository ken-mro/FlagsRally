#!/usr/bin/env python3
"""Bump android:versionName / android:versionCode in Platforms/Android/AndroidManifest.xml.

    uv run .claude/skills/android-release/scripts/bump_android_version.py            # 1.1.21 -> 1.1.22
    uv run .claude/skills/android-release/scripts/bump_android_version.py --minor    # 1.1.21 -> 1.2.0
    uv run .claude/skills/android-release/scripts/bump_android_version.py --version 1.2.3
    uv run .claude/skills/android-release/scripts/bump_android_version.py --dry-run  # print only

versionCode is the version written as MMmmpp with zero padding (1.1.21 -> 010121), as in every
release so far. Google Play rejects a code that is not higher than the last upload, so the
script stops if the new code would not go up. The file's BOM and line endings are kept.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

MANIFEST = Path(__file__).resolve().parents[4] / "Platforms" / "Android" / "AndroidManifest.xml"
NAME_RE = re.compile(r'android:versionName="(\d+)\.(\d+)\.(\d+)"')
CODE_RE = re.compile(r'android:versionCode="(\d+)"')


def version_code(major: int, minor: int, patch: int) -> str:
    if not (0 <= minor <= 99 and 0 <= patch <= 99 and 0 <= major <= 99):
        raise SystemExit(f"{major}.{minor}.{patch} does not fit the MMmmpp versionCode scheme")
    return f"{major:02d}{minor:02d}{patch:02d}"


def main() -> int:
    parser = argparse.ArgumentParser(description="Bump the Android versionName and versionCode.")
    group = parser.add_mutually_exclusive_group()
    group.add_argument("--version", help="set this exact version, e.g. 1.2.0")
    group.add_argument("--minor", action="store_true", help="bump the minor version (x.y+1.0)")
    group.add_argument("--major", action="store_true", help="bump the major version (x+1.0.0)")
    parser.add_argument("--dry-run", action="store_true", help="print the change without writing it")
    parser.add_argument("--manifest", type=Path, default=MANIFEST)
    args = parser.parse_args()

    raw = args.manifest.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")

    name_match, code_match = NAME_RE.search(text), CODE_RE.search(text)
    if not name_match or not code_match:
        raise SystemExit(f"versionName x.y.z or versionCode not found in {args.manifest}")
    major, minor, patch = map(int, name_match.groups())
    old_name, old_code = f"{major}.{minor}.{patch}", code_match.group(1)

    if args.version:
        if not re.fullmatch(r"\d+\.\d+\.\d+", args.version):
            raise SystemExit(f"--version must look like 1.2.3, not {args.version}")
        major, minor, patch = map(int, args.version.split("."))
    elif args.major:
        major, minor, patch = major + 1, 0, 0
    elif args.minor:
        minor, patch = minor + 1, 0
    else:
        patch += 1

    new_name, new_code = f"{major}.{minor}.{patch}", version_code(major, minor, patch)
    if int(new_code) <= int(old_code):
        raise SystemExit(f"versionCode would not go up: {old_code} -> {new_code} ({old_name} -> {new_name})")

    print(f"versionName {old_name} -> {new_name}")
    print(f"versionCode {old_code} -> {new_code}")
    if args.dry_run:
        return 0

    text = NAME_RE.sub(f'android:versionName="{new_name}"', text, count=1)
    text = CODE_RE.sub(f'android:versionCode="{new_code}"', text, count=1)
    args.manifest.write_bytes((b"\xef\xbb\xbf" if bom else b"") + text.encode("utf-8"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
