"""PreToolUse hook: keep FlagsRally's local secrets out of git.

Repository/Constants.cs is kept modified in the working tree with real API keys, and
Resources/Sample/*.json.encrypted are local-only. This hook blocks (exit code 2):
  - blanket staging: `git add -A`, `git add .`, `git add --all`, `git commit -a`
  - a commit while a secret file is staged
  - a push whose outgoing commits contain something that looks like a real key
Input is the hook JSON on stdin; only Bash/PowerShell commands are inspected.
"""
import json
import re
import subprocess
import sys

SECRET_PATHS = re.compile(r"^(Repository/Constants\.cs|Resources/Sample/.*\.encrypted)$")
# Written so that this file never matches itself (the Syncfusion prefix is split by a character class).
KEY_PATTERNS = re.compile(r"AIza[0-9A-Za-z_\-]{30,}|goog_[A-Za-z]{20,}|appl_[A-Za-z]{20,}|Ngo9Big[B]OggjHTQxAR8[A-Za-z0-9+/=]{20,}")
QUOTED = re.compile(r'"(?:\\.|[^"\\])*"|\'[^\']*\'')
SEGMENTS = re.compile(r"&&|\|\||[;|\n]")
# Matched against one command at a time, so heredoc or message text that merely mentions them is not caught.
BLANKET_ADD = re.compile(r"^\s*git\b.*\badd\s+(-A\b|--all\b|\.(\s|$))")
COMMIT_ALL = re.compile(r"^\s*git\b.*\bcommit\b.*\s-(a|[a-zA-Z]*a[a-zA-Z]*)\b")


def git(*args):
    result = subprocess.run(["git", *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    return result.stdout if result.returncode == 0 else ""


def block(message):
    print(message, file=sys.stderr)
    sys.exit(2)


def main():
    try:
        data = json.loads(sys.stdin.buffer.read().decode("utf-8", errors="replace"))
    except ValueError:
        return
    command = (data.get("tool_input") or {}).get("command") or ""
    if "git" not in command:
        return

    # Ignore quoted text such as commit messages, and check each command separately.
    segments = SEGMENTS.split(QUOTED.sub('""', command))
    if any(BLANKET_ADD.search(s) or COMMIT_ALL.search(s) for s in segments):
        block("Blocked: stage files by explicit path. Repository/Constants.cs holds real keys and "
              "Resources/Sample/*.encrypted are local-only, so `git add -A/.` and `git commit -a` are not allowed.")

    if re.search(r"\bcommit\b", command):
        staged = [p for p in git("diff", "--cached", "--name-only").splitlines() if SECRET_PATHS.match(p)]
        if staged:
            block("Blocked: secret files are staged: " + ", ".join(staged) +
                  ". Unstage them with `git restore --staged <path>`.")

    if re.search(r"\bpush\b", command):
        outgoing = git("log", "-p", "--format=%H", "HEAD", "--not", "--remotes")
        if KEY_PATTERNS.search(outgoing):
            block("Blocked: the commits about to be pushed contain what looks like a real API key "
                  "(probably Repository/Constants.cs). Remove it from those commits before pushing.")


if __name__ == "__main__":
    main()
