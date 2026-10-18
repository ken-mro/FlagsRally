---
name: ios-release
description: Release a FlagsRally iOS version - check a clean Release build on the simulator for crashes and other fatal bugs (fixing and re-checking until none are left), set CFBundleShortVersionString and bump CFBundleVersion in Platforms/iOS/Info.plist on a release/ios-* branch, build the signed .ipa with dotnet publish, and deliver it to App Store Connect after the user confirms. Use whenever the user wants to release, publish or upload an iOS version ("iOS をリリースして", "iOS の ipa を作って", "App Store Connect に上げて", "TestFlight に出したい", "build を上げて 1.2 を出す").
---

# Release an iOS version

After a short start (stage 0), four stages run in this order, each finished before the next starts, and then a report (stage 5):

1. **Release check**: a clean Release build on the simulator must launch and run without a fatal bug. Fix and re-check until it does.
2. **Version** in `Platforms/iOS/Info.plist`: `CFBundleShortVersionString` set, `CFBundleVersion` bumped.
3. **Build the .ipa** with `dotnet publish`.
4. **Deliver to App Store Connect**, after the user says yes.

Every step that changes files ends with its own commit (a fix is one commit per concern; the version is one commit). Steps that change nothing (checks, the build, the upload) make no commit. Nothing is pushed unless the user asks.

`<scratch>` below means Claude's scratchpad directory. Needs a Mac with Xcode, a booted simulator and AXe, as in `/ios-verify`.

**How the iOS numbers are kept:** they live only on `release/ios-*` branches, which are not merged back (`master`'s Info.plist still says build 5 and has no `CFBundleShortVersionString`). So the last build number comes from those branches, not from `master`. Info.plist values win over `ApplicationDisplayVersion` / `ApplicationVersion` in `FlagsRally.csproj`; the .ipa carries the Info.plist numbers.

**The upload is the one step that cannot be taken back.** App Store Connect accepts each build number once per version; after an upload that number is used up even if the build is never released. So stage 4 always stops right before the upload, shows what will be sent and goes on only after a clear yes in this conversation. Earlier approval ("release it") does not cover it. The upload only puts the build in App Store Connect (TestFlight); submitting it for review is the user's.

## One-time setup (the user's, before the first upload)

The upload uses an App Store Connect API key (App Store Connect > Users and Access > Integrations > App Store Connect API, a Team key with the App Manager role). It is the user's secret: Claude never asks for it in chat, prints it or puts it in the repository. The `.p8` file lives outside the repository. The variables go in the `env` of `.claude/settings.local.json` (git-ignored, unreadable to Claude Code, blocked from commits by `guard_secrets.py`); Claude Code reads it at start, so restart it after a change.

| Variable | What |
|---|---|
| `ASC_API_KEY_ID` | the key's ID (10 characters) |
| `ASC_API_ISSUER_ID` | the issuer ID shown above the key list |
| `ASC_API_KEY_P8` | path to the downloaded `AuthKey_<id>.p8`, outside the repository |

Signing needs the "Apple Distribution: Ken Murao (NQJ2NB4BG5)" certificate in the keychain and the "FlagsRally" App Store provisioning profile (`security find-identity -v -p codesigning` lists the certificate). Do not create keys, certificates or profiles yourself; if something is missing, tell the user what.

## 0. Start

- **The version.** Use the one the user gave (`1.2`, `1.2.1`). If they gave none, ask for it with AskUserQuestion before anything else, showing the current one from the last release branch (below). Do not guess.
- Update `master`: `git checkout master && git pull --ff-only`. Open PRs that should go out must be merged first (`gh pr list --state open`); ask about any whose status is unclear.
- Find the last iOS release branch and the next build number:
  ```bash
  git for-each-ref --sort=-committerdate --format='%(refname:short)' 'refs/heads/release/ios-*' 'refs/remotes/origin/release/ios-*'
  uv run .claude/skills/ios-release/scripts/bump_ios_version.py --version <version> --dry-run
  ```
- Create the branch from `master`: `git checkout -b release/ios-<version>-build<N>`.
- **Carry over the iOS-only commits** of the last release branch that `master` lacks, except its version commit ("Set the iOS version to ..."). `git cherry -v master <last release branch>` lists them (`+` = not on master). Show the list and cherry-pick them in order (`git cherry-pick <sha>`), unless the user says otherwise. An example is the `ValidateXcodeVersion=false` property, needed while Xcode is newer than the iOS SDK.
- `Repository/Constants.cs` must hold the real keys in the working tree (`git diff --quiet Repository/Constants.cs` exits 1). Never stage it. A build with the dummy keys ships broken maps and subscriptions.

## 1. Release check (repeat until clean)

Only a Release build shows what trimming removes, so the Release launch check is never skipped, even when nothing but the version changed.

1. `dotnet test FlagsRally_xunit/FlagsRally_xunit/FlagsRally_xunit.csproj`. Every test must pass.
2. Release smoke test (clean Release simulator build, install over the current app, launch in English, open all four tabs, look for crash reports):
   ```bash
   bash .claude/skills/ios-release/scripts/release_smoke.sh <scratch>/ios-release-check-1 [--udid <udid>]
   ```
   Exit 0 means it launched, every tab opened and no new crash report appeared. Exit 1 lists what was found (`report.txt`, `build.log`, crash `.ips` files and a screenshot per tab are in the folder). Look at the screenshots yourself too: a blank tab or an endless spinner is fatal even without a crash. Without `--udid` it takes the first booted simulator; pick an iPhone.
3. Check what changed since the last iOS release, with the Release build installed: `git log --oneline <last release branch>..HEAD --no-merges`. Drive the simulator as in `/ios-verify` (check-ins and the map for map changes, adding a board for board changes, the paywall for subscription changes; English and Japanese when text changed).
4. **Something fatal?** (crash, freeze, a screen that does not load, data loss.) Find the cause before changing code (the crash report's exception and stack, `xcrun simctl spawn booted log show --last 5m --predicate 'process == "FlagsRally"'`). Fix it on the release branch, add a test that would have caught it when possible, run the tests and commit one concern per commit. If the fix touches shared code or layout, also check Android with `/android-verify`. Then **go back to step 1 with a new output folder**: only a full clean run counts.

Non-fatal problems (a typo, a layout glitch) do not hold the release. List them for the user; fix them here only if small and clearly wanted.

## 2. Version

1. ```bash
   uv run .claude/skills/ios-release/scripts/bump_ios_version.py --version <version>   # --build <N> only when the user names one
   ```
   It sets `CFBundleShortVersionString` (adding it when missing) and sets `CFBundleVersion` to one more than the highest found in the working tree and on every `release/ios-*` branch. It refuses a number that does not go up. If the user says App Store Connect already has a higher build (an upload from another machine), use `--build`.
2. `git diff Platforms/iOS/Info.plist` must show only those values, and `plutil -lint Platforms/iOS/Info.plist` must say OK.
3. Commit just that file: `git add Platforms/iOS/Info.plist`, message `Set the iOS version to <version> (build <N>)`.

## 3. Build the .ipa

On the release branch, with the version committed and the real keys in `Repository/Constants.cs`:

```bash
dotnet publish FlagsRally.csproj -f net10.0-ios -c Release -p:ArchiveOnBuild=true -p:RuntimeIdentifier=ios-arm64 \
  -p:CodesignKey="Apple Distribution: Ken Murao (NQJ2NB4BG5)" -p:CodesignProvision="FlagsRally" > <scratch>/publish.log 2>&1
```

It takes several minutes (full AOT); run it in the background. The package is `bin/Release/net10.0-ios/ios-arm64/publish/FlagsRally.ipa` (the log ends with `Created the package: ...`), and an `.xcarchive` is added to Xcode's Organizer. A signing error means the certificate or profile is missing: report it, do not work around it. A compile or link error that the simulator build did not show goes back to stage 1.

Check the numbers inside the package match stage 2 (`upload_ipa.sh` also prints them):
```bash
unzip -p bin/Release/net10.0-ios/ios-arm64/publish/FlagsRally.ipa 'Payload/FlagsRally.app/Info.plist' > <scratch>/ipa-Info.plist
plutil -extract CFBundleShortVersionString raw <scratch>/ipa-Info.plist; plutil -extract CFBundleVersion raw <scratch>/ipa-Info.plist
```

## 4. Deliver to App Store Connect

1. `bash .claude/skills/ios-release/scripts/upload_ipa.sh --check` reports whether the setup variables are set (never their values).
   - **Missing:** do not upload. Open the package in Transporter for the user instead: `open -a Transporter bin/Release/net10.0-ios/ios-arm64/publish/FlagsRally.ipa`, and tell them to sign in and press Deliver. Also tell them the one-time setup above lets the next release upload directly.
2. Let Apple check the package without uploading: `bash .claude/skills/ios-release/scripts/upload_ipa.sh --validate <ipa>`. Fix whatever it reports (a build number already used means stage 2 again with a higher `--build`, then stage 3).
3. **Stop and ask the user.** Show the version and build, the .ipa's size and the branch and commit it was built from. Say that the upload uses up this build number. Wait for a clear yes in this conversation.
4. `bash .claude/skills/ios-release/scripts/upload_ipa.sh --upload <ipa>`. It prints `UPLOADED: ...` when App Store Connect has the build. Processing then takes a while before it appears in TestFlight.
5. If the upload fails, report the error. Before any second attempt, ask the user to look in App Store Connect (TestFlight > iOS builds) whether the build arrived; if it did, do not upload again.

## 5. Report to the user

- The branch and its commits (carried over, fixes, version), and that nothing was pushed unless they asked.
- The Release check: the number of runs, what was fixed between them, the folder with the final run's screenshots.
- Fixes made on the release branch are not on `master`. Offer to bring them to `master` in a PR (a branch from `master`, cherry-pick, `/update-changelog`, `/ship-pr`).
- Non-fatal problems left for later.
- The .ipa path, its version and build, and the upload: the `UPLOADED` line, or that it was handed to Transporter, or why it did not happen.
