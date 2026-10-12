---
name: android-release
description: Prepare a FlagsRally Android release, from checks to store release notes - check a clean Release build on the emulator for launch crashes and other fatal bugs (fixing and re-checking until none are left), bump versionName/versionCode in AndroidManifest.xml (patch by default), open and merge the PR, and finally write the store release notes with /release-notes. Use whenever the user wants to release, ship or put out a new Android version ("Android をリリースして", "リリース準備", "バージョンを上げて PR", "Play に出す準備", "1.2.0 を出したい"). It stops before anything is uploaded to Google Play.
---

# Prepare an Android release

Four stages, in this order, each finished before the next starts:

1. **Release check**: a clean Release build on the emulator must launch and run without a fatal bug. Fix and re-check until it does.
2. **Version bump** in `Platforms/Android/AndroidManifest.xml`.
3. **PR and merge** through `/ship-pr`.
4. **Release notes** for the store through `/release-notes`.

The checks come first because a version number is a promise to Google Play: once a build with that code is uploaded, the code cannot be used again. Bumping only after the app is known to start avoids burning numbers on broken builds.

**Not part of this flow: uploading to Google Play** (or building the upload bundle for it). An upload cannot be taken back and goes to real users, so it is always the user's own step. The flow ends with the release notes ready to paste: tell the user the release is ready and what to upload, and wait for them.

## 0. Start

- Work from the latest `master`: `git checkout master && git pull --ff-only`. Open PRs that should go out in this release must be merged first (`gh pr list --state open`); ask about any whose status is unclear instead of guessing.
- Find the last version bump; stages 1 and 2 need it. Use `-G` (`-S` misses bumps, because a bump changes the value without changing how often the attribute appears):
  ```bash
  git log -1 --format=%h -G'android:versionName' -- Platforms/Android/AndroidManifest.xml
  ```
- **Was the current version already uploaded?** The repository cannot tell: every changelog heading stays `- Unreleased`, even for versions that are on Google Play. When the top changelog section is that version, ask the user once, here at the start. If it was not uploaded, it can still go out as is: skip the bump in stage 2, keep the changelog entries under that version, and release it under the current number. Otherwise go on with a new version.
- Pick the version now, so the branch can be named. The user's version wins (`1.2.0`, "マイナーを上げて"). Otherwise bump the patch. Preview it without writing:
  ```bash
  python3 .claude/skills/android-release/scripts/bump_android_version.py --dry-run   # add --minor / --major / --version X.Y.Z when asked
  ```
- Create the release branch: `git checkout -b chore/bump-android-<version>`. Any fixes from stage 1 go on this branch too, so the release is one PR.
- `Repository/Constants.cs` must hold the real keys in the working tree (it does on this machine; never stage it). A Release build with the dummy keys would launch differently from the store build.

## 1. Release check (repeat until clean)

1.1.20 passed every Debug check and still crashed the moment it opened from Google Play: R8 had removed a class the subscription library loads at startup. Only a Release build shows what shrinking, trimming and AOT remove, so the Release launch check is never skipped, even for a version bump with no code changes.

1. `dotnet test FlagsRally_xunit/FlagsRally_xunit/FlagsRally_xunit.csproj`. Compare failures with `master`; only new ones block the release (two `CustomGeolocationTests` fail on master today because of an encoding problem in the test file).
2. Back up the emulator's data: `bash .claude/skills/android-release/scripts/debug_build_back.sh`, then `bash tools/emu.sh db-backup`. The backup uses `run-as`, which only works while a Debug build is installed, and an earlier run may have left the Release build on the emulator. The script replaces it with the Debug build (data kept). `tools/emu.sh install` alone can skip the install and leave the Release build in place. The backup covers the database only, not settings (pin style, country of residence, API key): note them and change none during the checks, or put them back by hand.
3. Release smoke test (clean Release build, install over the current app, launch, open all four tabs, search logcat):
   ```bash
   bash .claude/skills/android-release/scripts/release_smoke.sh <scratch>/release-check-1
   ```
   Exit 0 means it launched, every tab opened and logcat has no fatal line from the app. Exit 1 lists what was found; the full `logcat.txt`, `fatal.txt` and a screenshot per tab are in the folder. Look at the screenshots yourself as well: a blank tab or an endless spinner is a fatal bug even without a crash. The script leaves the app on the Passport tab.
4. Check what the changes since the last release touch, with the Release build still installed. `git log <last bump>..HEAD` shows them (the commit found in stage 0). Follow `/android-verify` for how to drive the emulator. For example: check-ins and the map for map changes, adding a board for board changes, the paywall for subscription changes. Use English and Japanese when text changed.
5. **Something fatal?** (crash, freeze, "isn't responding", a screen that does not load, data loss.) Find the cause before changing code (logcat's exception and stack, `bash tools/emu.sh crash`). Fix it on the release branch, add a test that would have caught it when possible, and commit one concern per commit. Add a `### Fixed` entry with `/update-changelog`. If the fix touches shared code or layout, also check iOS with `/ios-verify`. Then **go back to step 1 and run the whole check again with a new output folder**. A fix can break something else, and only a full clean run counts.
6. When a run is clean: `bash .claude/skills/android-release/scripts/debug_build_back.sh --restore`. It puts the Debug build back (data kept) and then restores the database; the printed hash must match the backup. Restoring first fails, because `db-restore` needs the debuggable build.

Non-fatal problems found on the way (a typo, a layout glitch) do not hold the release. List them for the user, and fix them here only if they are small and the user would clearly want them in.

## 2. Version bump

1. ```bash
   python3 .claude/skills/android-release/scripts/bump_android_version.py   # same flags as the dry run
   ```
   It writes `android:versionName` and `android:versionCode` (`MMmmpp`, zero-padded: 1.1.22 → `010122`). It keeps the file's BOM and refuses a code that does not go up.
2. `git diff Platforms/Android/AndroidManifest.xml` should show only those two values. Commit just that file: `Bump the Android version to <version> (<code>)`.
3. Changelog heading (follow `/update-changelog` for the format):
   - An `## [Unreleased]` section at the top becomes `## [<version>] - Unreleased`.
   - If the top section is an older `## [<old>] - Unreleased` that was uploaded (stage 0), entries kept being added under it after its bump. Move the entries added after the old bump into a new `## [<version>] - Unreleased` section above it. `git diff <last bump>..HEAD -- CHANGELOG.md` shows which ones. Drop any category heading (`### Changed`) left empty by the move.
   - No user-facing change since the last release? Add a short entry saying what the release is for (for example, a rebuild with updated libraries).
   Commit it on its own.

## 3. PR and merge

Run `/ship-pr` for this branch. It checks for secrets, runs the tests and build, checks the changelog, pushes, opens the PR and merges it. In the PR body's testing section, say which Release checks ran and their result: launch and tabs, the features checked, and the fixes and re-checks if there were any.

## 4. Release notes

After the merge, run `/release-notes` for the new version. It reads the version's section of `CHANGELOG.md` and the commits since the previous bump, and returns the `<en-US>` / `<ja-JP>` block for the Play Console. Run it on the merged `master`, so the notes describe what was actually released, including fixes made during stage 1. If the release has no user-facing change, the notes say so briefly instead of listing internal work. The notes are a draft for the user: show them, and change them if the user asks. Do not paste them into the Play Console.

## 5. Report to the user

- PR link, version and code, and that `master` now has them.
- The Release check: the number of runs, what was fixed between them, the folder with the final run's screenshots.
- Non-fatal problems left for later.
- The release notes from stage 4, in their code block, ready to paste.
- Next steps are theirs: build the bundle, upload it to Google Play and paste the notes. Do not upload, or start an upload, yourself.
