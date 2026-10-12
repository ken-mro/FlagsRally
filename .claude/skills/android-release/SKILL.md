---
name: android-release
description: Release a FlagsRally Android version end to end - check a clean Release build on the emulator for launch crashes and other fatal bugs (fixing and re-checking until none are left), bump versionName/versionCode in AndroidManifest.xml (patch by default), open and merge the PR, write the store release notes with /release-notes, then build the signed bundle and upload it to Google Play with those notes after the user confirms. Use whenever the user wants to release, ship or put out a new Android version ("Android をリリースして", "リリース準備", "バージョンを上げて PR", "Play に出す", "Google Play にアップロード", "1.2.0 を出したい").
---

# Release an Android version

After a short start (stage 0), five stages run in this order, each finished before the next starts, and then a report to the user (stage 6):

1. **Release check**: a clean Release build on the emulator must launch and run without a fatal bug. Fix and re-check until it does.
2. **Version bump** in `Platforms/Android/AndroidManifest.xml`.
3. **PR and merge** through `/ship-pr`.
4. **Release notes** for the store through `/release-notes`, saved to a file.
5. **Upload to Google Play**: the signed bundle and exactly those release notes, after the user says yes.

`<scratch>` below means Claude's scratchpad directory, or another temporary folder outside the repository.

**On Windows** the flow runs in Git Bash, like the project's other skills (`tools/emu.sh` already handles Windows paths):
- Network git commands need `-c http.sslBackend=schannel` (CLAUDE.md), for example `git -c http.sslBackend=schannel pull --ff-only`.
- Where this file says `python3`, use whatever runs Python 3 there (`python`, or `py -3`); `python3` is often missing or the Microsoft Store stub.
- Environment variables are set as Windows user variables or in `~/.bashrc` for Git Bash. Paths may be written `C:\...`; `build_aab.sh` converts them.
- `jarsigner` is looked for in `JAVA_HOME`, the Microsoft OpenJDK and Android Studio folders under `C:\Program Files`. If none is found, set `JAVA_HOME` to the JDK the Android build uses.
- The iOS check in stage 1 step 5 cannot run on Windows. If a fix touches shared code or layout, say that iOS was not checked and leave it for a Mac.

The checks come first because a version number is a promise to Google Play: once a build with that code is uploaded, the code cannot be used again. Bumping only after the app is known to start avoids burning numbers on broken builds.

**The upload is the one step that cannot be taken back.** Once a bundle is committed to Google Play its versionCode is used up for good, and a published release goes to real users. So stage 5 always stops right before the upload, shows the user what will be sent, and goes on only after a clear yes in this conversation. Earlier approval (even "release it" at the start) does not cover it. By default the release is a **draft**: Google Play has the bundle and the notes, and the user starts the rollout in the Play Console. Publish or roll out in stages only when the user asks.

## One-time setup (the user's, before the first upload)

The upload needs the app's upload key and access to Google Play. Both are the user's secrets: Claude never asks for them in chat, types them, prints them or puts them in the repository. They live outside the repository, in files and environment variables the user sets (for example in `~/.zshrc`):

| Variable | What |
|---|---|
| `FLAGSRALLY_KEYSTORE` | path to the upload keystore (`.keystore` / `.jks`), outside the repository |
| `FLAGSRALLY_KEY_ALIAS` | the key's alias in it |
| `FLAGSRALLY_STORE_PASS`, `FLAGSRALLY_KEY_PASS` | its passwords (MSBuild reads them itself through `env:`; they never appear in a command or log) |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | path to a Google Cloud service account key file, outside the repository. In the Play Console, under Users and permissions, the service account needs access to FlagsRally with "Release apps to testing tracks" and "Release to production". |

The upload script also needs two Python packages: `python3 -m pip install --user google-api-python-client google-auth`. They are downloaded from PyPI, so they are installed only after the user says yes; offer it when they are missing.

If anything is missing when stage 5 starts, stop there and tell the user exactly what is missing. Do not create keys, service accounts or Play Console access yourself. Stages 1–4 do not need any of this.

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

After the merge, switch to the merged `master` (`git checkout master && git pull --ff-only`) and run `/release-notes` for the new version. It reads the version's section of `CHANGELOG.md` and the commits since the previous bump, and returns the `<en-US>` / `<ja-JP>` block. Running it on `master` makes the notes describe what was actually released, including fixes made during stage 1. If the release has no user-facing change, the notes say so briefly instead of listing internal work.

Save the block exactly as shown, tags included, to `<scratch>/release-notes-<version>.txt`. Stage 5 uploads this file, so the notes on Google Play are the ones the user saw. If the user asks for changes, change the file and show it again.

## 5. Upload to Google Play

1. Check the one-time setup is in place: the variables above are set and the paths point to files (print only set/unset and exists, never the values), and `python3 -c "import googleapiclient, google.oauth2"` works. If not, stop and report what is missing, offering to install the packages if those are what is missing. Check variables inside `bash -c` so the same lines work whatever the Bash tool's shell is (zsh on a Mac, Git Bash on Windows).
2. Build the signed bundle on the merged `master`:
   ```bash
   bash .claude/skills/android-release/scripts/build_aab.sh
   ```
   It makes a clean Release `.aab` signed with the upload key and prints the bundle's path on its last line. It stops if it is not on `master`, if `Repository/Constants.cs` is the committed version with dummy keys (a store build with them would ship broken maps and subscriptions), or if the bundle is signed with the debug key. A build error here means the same code that passed stage 1 fails in the store build: go back to stage 1 for it.
3. Let Google Play check everything without publishing:
   ```bash
   python3 .claude/skills/android-release/scripts/upload_to_play.py --aab <bundle> --notes <scratch>/release-notes-<version>.txt --version <version> --validate-only
   ```
   It checks the notes locally (both languages present, at most 500 characters each) and that the manifest has `<version>`. It then sends the bundle and notes in an edit, has Google Play validate it and throws the edit away. Nothing is published and the versionCode stays free. Fix whatever it reports before going on.
4. **Stop and ask the user.** Show the version and versionCode, the track (`production`), the release type (draft unless they asked otherwise), the bundle's size and the release notes block. Say that the upload uses up this versionCode for good. Wait for a clear yes in this conversation; anything else means no upload.
5. Upload, with the same arguments as the validation but without `--validate-only`. Add `--status completed` to publish to everyone, or `--status inProgress --fraction 0.2` for a staged rollout, only when the user asked for that. The script prints `UPLOADED: ...` when Google Play has the release.
   - If it stops with "changesNotSentForReview" (the Play Console uses managed publishing), nothing was published: tell the user, and run again with `--not-sent-for-review` only after they say yes.
6. If the upload fails, the script deletes its edit, so nothing half-done is left. If the connection dropped during the final commit, though, Google Play may have committed it anyway. Before any second attempt, look at what the track holds: `python3 .claude/skills/android-release/scripts/upload_to_play.py --show-track` (read-only). If the version is already there, it was uploaded; do not upload again. Report the error, and upload again only after the user says so.

## 6. Report to the user

- PR link, version and code, and that `master` now has them.
- The Release check: the number of runs, what was fixed between them, the folder with the final run's screenshots.
- Non-fatal problems left for later.
- The release notes from stage 4, in their code block.
- The upload: track, release type and the script's `UPLOADED` line. For a draft, say that the rollout is started in the Play Console (Release > Production) and is theirs to start. If the upload did not happen (no setup, the user said no), say so and what is left to do.
