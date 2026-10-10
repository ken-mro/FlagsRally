---
name: update-changelog
description: Add the current branch's changes to FlagsRally's CHANGELOG.md (Keep a Changelog format). Use before opening any pull request, and whenever the user asks to update the changelog.
---

# Update CHANGELOG.md

Every pull request updates `CHANGELOG.md`. A hook (`.claude/hooks/guard_changelog.py`) blocks `gh pr create` when the branch does not change it.

1. **What changed:** `git log --oneline origin/master..HEAD` and `git diff --stat origin/master...HEAD`.
2. **Where it goes:** the top version section if its heading ends in `- Unreleased` (for example `## [1.1.17] - Unreleased`).
   If the top section already has a release date, add `## [Unreleased]` above it. The version-bump PR later renames it to `## [x.y.z] - Unreleased`.
3. **Category:** one of `### Added`, `### Changed`, `### Fixed`, `### Removed`, `### Security`, in that order. Reuse an existing heading in the section instead of adding a second one.
4. **Entry:** one bullet per change that a user would notice, in English:
   - Start with a short bold summary of what the user saw or can now do, then one to three sentences on what changed for them.
   - Board names and screen names are fine here (the store text is written from this later by `/release-notes`, which drops them).
   - Leave out class names, file names and commit hashes.
   - Several commits for one change are one bullet.
5. **Internal-only branches** (skills, hooks, docs, tests, refactors, build scripts with no effect on the app): add a bullet under `### Changed` that starts with `**Development:**`, for example `- **Development:** A hook now blocks opening a pull request that does not update this changelog.`
   Only if the user says this PR should not appear in the changelog, skip the edit and open the PR with `--label no-changelog` (the hook lets that through).
6. **Keep the file's format:** UTF-8 without BOM, CRLF line endings, one blank line between headings and lists.
7. **Commit it on its own:** `git add CHANGELOG.md` and a message such as `Add <change> to the changelog`.
