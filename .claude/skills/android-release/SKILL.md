---
name: android-release
description: Prepare a FlagsRally Android release up to a merged version bump - check a clean Release build on the emulator for launch crashes and other fatal bugs (fixing and re-checking until none are left), bump versionName/versionCode in AndroidManifest.xml (patch by default), then open and merge the PR. Use whenever the user wants to release, ship or put out a new Android version ("Android をリリースして", "リリース準備", "バージョンを上げて PR", "Play に出す準備", "1.2.0 を出したい"). It stops before anything is uploaded to Google Play.
---

# Prepare an Android release

Three stages, in this order, each finished before the next starts:

1. **Release check**: a clean Release build on the emulator must launch and run without a fatal bug. Fix and re-check until it does.
2. **Version bump** in `Platforms/Android/AndroidManifest.xml`.
3. **PR and merge** through `/ship-pr`.

The checks come first because a version number is a promise to Google Play: once a build with that code is uploaded, the code cannot be used again. Bumping only after the app is known to start avoids burning numbers on broken builds.

**Not part of this flow: uploading to Google Play** (or building the upload bundle for it). An upload cannot be taken back and goes to real users, so it is always the user's own step. End by telling them the release is ready and what to upload, and wait for them.

## 0. Start

- Work from the latest `master`: `git checkout master && git pull --ff-only`. Open PRs that should go out in this release must be merged first (`gh pr list --state open`); ask about any whose status is unclear instead of guessing.
- Pick the version now, so the branch can be named. The user's version wins (`1.2.0`, "マイナーを上げて"). Otherwise bump the patch. Preview it without writing:
  ```bash
  python3 .claude/skills/android-release/scripts/bump_android_version.py --dry-run   # add --minor / --major / --version X.Y.Z when asked
  ```
- Create the release branch: `git checkout -b chore/bump-android-<version>`. Any fixes from stage 1 go on this branch too, so the release is one PR.
- `Repository/Constants.cs` must hold the real keys in the working tree (it does on this machine; never stage it). A Release build with the dummy keys would launch differently from the store build.

## 1. Release check (repeat until clean)

1.1.20 passed every Debug check and still crashed the moment it opened from Google Play: R8 had removed a class the subscription library loads at startup. Only a Release build shows what shrinking, trimming and AOT remove, so the Release launch check is never skipped, even for a version bump with no code changes.

1. `dotnet test FlagsRally_xunit/FlagsRally_xunit/FlagsRally_xunit.csproj`. Compare failures with `master`; only new ones block the release (two `CustomGeolocationTests` fail on master today because of an encoding problem in the test file).
2. Back up the emulator's data: `bash tools/emu.sh db-backup`.
3. Release smoke test (clean Release build, install over the current app, launch, open all four tabs, search logcat):
   ```bash
   bash .claude/skills/android-release/scripts/release_smoke.sh <scratch>/release-check-1
   ```
   Exit 0 means it launched, every tab opened and logcat has no fatal line from the app. Exit 1 lists what was found; the full `logcat.txt`, `fatal.txt` and a screenshot per tab are in the folder. Look at the screenshots yourself as well: a blank tab or an endless spinner is a fatal bug even without a crash.
4. Check what the changes since the last release touch, with the Release build still installed. `git log <last bump commit>..HEAD` shows them (`git log -1 --format=%h -S'android:versionName' -- Platforms/Android/AndroidManifest.xml` finds the last bump). Follow `/android-verify` for how to drive the emulator. For example: check-ins and the map for map changes, adding a board for board changes, the paywall for subscription changes. Use English and Japanese when text changed.
5. **Something fatal?** (crash, freeze, "isn't responding", a screen that does not load, data loss.) Find the cause before changing code (logcat's exception and stack, `bash tools/emu.sh crash`). Fix it on the release branch, add a test that would have caught it when possible, and commit one concern per commit. Add a `### Fixed` entry with `/update-changelog`. If the fix touches shared code or layout, also check iOS with `/ios-verify`. Then **go back to step 1 and run the whole check again with a new output folder**. A fix can break something else, and only a full clean run counts.
6. When a run is clean: `bash tools/emu.sh db-restore` (the printed hash must match the backup), and put the Debug build back with `bash tools/emu.sh install`.

Non-fatal problems found on the way (a typo, a layout glitch) do not hold the release. List them for the user, and fix them here only if they are small and the user would clearly want them in.

## 2. Version bump

1. ```bash
   python3 .claude/skills/android-release/scripts/bump_android_version.py   # same flags as the dry run
   ```
   It writes `android:versionName` and `android:versionCode` (`MMmmpp`, zero-padded: 1.1.22 → `010122`). It keeps the file's BOM and refuses a code that does not go up.
2. `git diff Platforms/Android/AndroidManifest.xml` should show only those two values. Commit just that file: `Bump the Android version to <version> (<code>)`.
3. Changelog heading (follow `/update-changelog` for the format):
   - An `## [Unreleased]` section at the top becomes `## [<version>] - Unreleased`.
   - If the top section is an older `## [<old>] - Unreleased` (that version was bumped but entries kept being added under it), move the entries added after the old bump into a new `## [<version>] - Unreleased` section above it. `git diff <old bump commit>..HEAD -- CHANGELOG.md` shows which ones. Ask the user if you cannot tell whether the old version was uploaded.
   - No user-facing change since the last release? Add a short entry saying what the release is for (for example, a rebuild with updated libraries).
   Commit it on its own.

## 3. PR and merge

Run `/ship-pr` for this branch. It checks for secrets, runs the tests and build, checks the changelog, pushes, opens the PR and merges it. In the PR body's testing section, say which Release checks ran and their result: launch and tabs, the features checked, and the fixes and re-checks if there were any.

## 4. Report to the user

- PR link, version and code, and that `master` now has them.
- The Release check: the number of runs, what was fixed between them, the folder with the final run's screenshots.
- Non-fatal problems left for later.
- Next steps are theirs: build and upload the bundle to Google Play. Offer `/release-notes` for the store text. Do not upload, or start an upload, yourself.
