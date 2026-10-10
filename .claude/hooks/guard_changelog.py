"""PreToolUse hook: every FlagsRally pull request updates CHANGELOG.md.

Blocks (exit code 2) `gh pr create` when CHANGELOG.md is not changed between the PR's base
(origin/<base>, default master) and its head (--head, default the current branch).
A PR the user chose to keep out of the changelog is opened with `--label no-changelog`.
Input is the hook JSON on stdin; only Bash/PowerShell commands are inspected.
"""
import json
import re
import subprocess
import sys

QUOTED = re.compile(r'"(?:\\.|[^"\\])*"|\'[^\']*\'')
SEGMENTS = re.compile(r"&&|\|\||[;|\n]")
PR_CREATE = re.compile(r"^\s*gh\s+pr\s+create\b")
OPT_OUT = re.compile(r"(--label|-l)[\s=]+[\"']?[^\"'\s]*\bno-changelog\b")


def git(*args):
    result = subprocess.run(["git", *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    return result.stdout.strip() if result.returncode == 0 else None


def option(segment, *names):
    match = re.search(r"(?:%s)[\s=]+[\"']?([^\"'\s]+)" % "|".join(map(re.escape, names)), segment)
    return match.group(1) if match else None


def main():
    try:
        data = json.loads(sys.stdin.buffer.read().decode("utf-8", errors="replace"))
    except ValueError:
        return
    command = (data.get("tool_input") or {}).get("command") or ""
    if "gh" not in command:
        return

    # Find `gh pr create` outside quoted text (a PR body may mention the command).
    stripped = SEGMENTS.split(QUOTED.sub('""', command))
    if not any(PR_CREATE.search(s) for s in stripped):
        return
    if OPT_OUT.search(command):
        return
    segment = next(s for s in SEGMENTS.split(command) if PR_CREATE.search(s))

    base = option(segment, "--base", "-B") or "master"
    head = option(segment, "--head", "-H") or "HEAD"
    base_ref = next((r for r in ("origin/" + base, base) if git("rev-parse", "--verify", "--quiet", r)), None)
    head_ref = next((r for r in (head, "origin/" + head) if git("rev-parse", "--verify", "--quiet", r)), None)
    if not base_ref or not head_ref:
        return  # Let gh report an unknown branch itself.

    changed = git("diff", "--name-only", f"{base_ref}...{head_ref}") or ""
    if "CHANGELOG.md" not in changed.splitlines():
        print(f"Blocked: {head_ref} does not update CHANGELOG.md (compared with {base_ref}). "
              "Add an entry with the update-changelog skill and commit it first. "
              "If the user said this PR should stay out of the changelog, add `--label no-changelog`.",
              file=sys.stderr)
        sys.exit(2)


if __name__ == "__main__":
    main()
