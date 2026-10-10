---
name: ship-pr
description: Review, push, open and merge a FlagsRally pull request into master, including any PRs it depends on. Use only when the user asks to push, open a PR or merge.
---

# Ship a branch to master

Pushing, opening and merging are done only when the user asks. Network git commands need `-c http.sslBackend=schannel` on this machine.

1. **Open PRs first:** `gh pr list --state open --json number,title,headRefName,baseRefName,mergeable`.
   If the current branch is built on another open PR's branch (`git merge-base --is-ancestor origin/<that-branch> HEAD`), merge that PR first with `gh pr merge <n> --merge`. Check `gh pr checks <n>`; the repo has no CI, so an empty result is normal.
2. **Review when asked** (for example "performance and security"): run independent reviews of `git diff origin/master...HEAD` in parallel (performance, security), fix what is real, one commit per concern, with tests.
3. **Nothing secret goes out:**
   - `git diff --name-only origin/master...HEAD | grep -iE "Constants.cs|Sample/|\.encrypted"` must print nothing.
   - The guard hook also blocks a push whose commits contain key-like strings.
4. **Tests and build:** `dotnet test FlagsRally_xunit/FlagsRally_xunit/FlagsRally_xunit.csproj` and `dotnet build FlagsRally.csproj -f net10.0-android`.
5. **Changelog:** `git diff --name-only origin/master...HEAD` must include `CHANGELOG.md`. If not, follow the `update-changelog` skill and commit the entry before pushing. The guard hook blocks `gh pr create` without it.
6. **Conflicts:** `git -c http.sslBackend=schannel fetch origin` then `git merge-tree --write-tree origin/master HEAD`.
7. **Push:** `git -c http.sslBackend=schannel push -u origin <branch>`.
8. **PR:** `gh pr create --base master --head <branch> --title "..." --body-file <scratch>/pr_body.md`. Body sections:
   - what changed, grouped by area
   - security and performance changes
   - testing (unit test count, what was checked on the emulator, what was not, e.g. iOS)
   - last line `🤖 Generated with [Claude Code](https://claude.com/claude-code)`
9. **Merge:** `gh pr view <n> --json mergeable,mergeStateStatus` should be `MERGEABLE CLEAN`. Then `gh pr merge <n> --merge` (merge commits, as in earlier PRs).
10. **Report:** the PR URLs, what was merged in which order, review findings that were fixed and any that were left (with the reason).
