#!/usr/bin/env python3
"""Set CFBundleShortVersionString and bump CFBundleVersion in Platforms/iOS/Info.plist.

    uv run .claude/skills/ios-release/scripts/bump_ios_version.py --version 1.2            # build = last + 1
    uv run .claude/skills/ios-release/scripts/bump_ios_version.py --version 1.2 --build 20
    uv run .claude/skills/ios-release/scripts/bump_ios_version.py --version 1.2 --dry-run  # print only

The iOS numbers live only on the release/ios-* branches (master keeps an old CFBundleVersion),
so "last" is the highest CFBundleVersion found in the working tree and on every local and remote
release/ios-* branch. App Store Connect rejects a build number it has already seen for the
version, so the script stops if the new one would not go up. Info.plist values win over
ApplicationDisplayVersion / ApplicationVersion in FlagsRally.csproj. The file's formatting is kept.
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
PLIST_REL = "Platforms/iOS/Info.plist"
SHORT_RE = re.compile(r"(<key>CFBundleShortVersionString</key>\s*<string>)([^<]*)(</string>)")
BUILD_RE = re.compile(r"([ \t]*)<key>CFBundleVersion</key>(\s*)<string>([^<]*)</string>")


def build_number(text: str) -> int | None:
    match = BUILD_RE.search(text)
    return int(match.group(3)) if match and match.group(3).isdigit() else None


def release_branch_builds() -> dict[str, int]:
    refs = subprocess.run(
        ["git", "for-each-ref", "--format=%(refname:short)", "refs/heads/release/ios-*", "refs/remotes/*/release/ios-*"],
        cwd=ROOT, capture_output=True, text=True, check=True,
    ).stdout.split()
    builds = {}
    for ref in refs:
        shown = subprocess.run(["git", "show", f"{ref}:{PLIST_REL}"], cwd=ROOT, capture_output=True, text=True)
        if shown.returncode == 0 and (number := build_number(shown.stdout)) is not None:
            builds[ref] = number
    return builds


def main() -> int:
    parser = argparse.ArgumentParser(description="Set the iOS version and bump the build number.")
    parser.add_argument("--version", required=True, help="CFBundleShortVersionString, e.g. 1.2 or 1.2.1")
    parser.add_argument("--build", type=int, help="CFBundleVersion to use instead of last + 1")
    parser.add_argument("--dry-run", action="store_true", help="print the change without writing it")
    args = parser.parse_args()

    if not re.fullmatch(r"\d+(\.\d+){0,2}", args.version):
        raise SystemExit(f"--version must look like 1.2 or 1.2.3, not {args.version}")

    plist = ROOT / PLIST_REL
    raw = plist.read_bytes()
    text = raw.decode("utf-8")
    build_match = BUILD_RE.search(text)
    if not build_match:
        raise SystemExit(f"CFBundleVersion not found in {plist}")
    short_match = SHORT_RE.search(text)
    old_short = short_match.group(2) if short_match else "(none)"
    old_build = build_number(text) or 0

    branches = release_branch_builds()
    for ref, number in sorted(branches.items()):
        print(f"  {ref}: build {number}")
    last = max([old_build, *branches.values()])
    new_build = args.build if args.build is not None else last + 1
    if new_build <= last:
        raise SystemExit(f"CFBundleVersion would not go up: last is {last}, asked for {new_build}")

    print(f"CFBundleShortVersionString {old_short} -> {args.version}")
    print(f"CFBundleVersion {old_build} -> {new_build} (highest so far: {last})")
    if args.dry_run:
        return 0

    indent, gap = build_match.group(1), build_match.group(2)
    text = BUILD_RE.sub(lambda m: f"{indent}<key>CFBundleVersion</key>{gap}<string>{new_build}</string>", text, count=1)
    if short_match:
        text = SHORT_RE.sub(lambda m: f"{m.group(1)}{args.version}{m.group(3)}", text, count=1)
    else:
        # Insert it right before CFBundleVersion, with the same indentation and line breaks.
        newline = "\r\n" if "\r\n" in text else "\n"
        key = f"{indent}<key>CFBundleVersion</key>"
        text = text.replace(key, f"{indent}<key>CFBundleShortVersionString</key>{gap}<string>{args.version}</string>{newline}{key}", 1)
    plist.write_bytes(text.encode("utf-8"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
