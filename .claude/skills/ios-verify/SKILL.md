---
name: ios-verify
description: Build FlagsRally for the iOS simulator, install it and check a change with screenshots (English and Japanese, normal and large text). Use after any UI or layout change, together with /android-verify, or when the user asks to check something on iOS.
---

# Check a change on the iOS simulator

Needs a Mac with Xcode and a booted simulator (`xcrun simctl list devices booted`; boot one with `xcrun simctl boot <udid>` and `open -a Simulator`). Taps use AXe (`brew install cameroncooke/axe/axe`), which drives the simulator directly; do not move the user's mouse with cliclick or AppleScript.

1. **Tests first:** `dotnet test FlagsRally_xunit/FlagsRally_xunit/FlagsRally_xunit.csproj`
2. **Build and install:**
   ```bash
   dotnet build FlagsRally.csproj -f net10.0-ios -p:ValidateXcodeVersion=false
   xcrun simctl install booted bin/Debug/net10.0-ios/iossimulator-arm64/FlagsRally.app
   ```
3. **Launch in a language:** `xcrun simctl launch --terminate-running-process booted com.companyname.flagsrally -AppleLanguages "(ja)"` (or `"(en)"`), then wait about 8 s.
4. **Look at the change:**
   - Screenshot: `xcrun simctl io booted screenshot <scratch>/x.png`, then `sips -Z 1000 <scratch>/x.png` and read the image yourself.
   - Tap: `axe tap -x X -y Y --udid <udid>` in points (iPhone 17: 402 x 874; a 1000 px tall screenshot is 460 px wide, so points = pixels x 0.874).
   - Tabs on iPhone 17: Passport (72, 826), Map (157, 826), Collections (243, 826), Settings (329, 826). Back button (38, 84).
   - Long press (map check-in spot): `axe touch -x X -y Y --down`, wait 1.5 s, `axe touch -x X -y Y --up`. Swipe: `axe swipe --start-x .. --start-y .. --end-x .. --end-y ..`.
   - `axe describe-ui --udid <udid>` lists what is on screen with frames, when coordinates are unclear.
5. **Large text, English and Japanese:** `xcrun simctl ui booted content_size extra-extra-large`, relaunch, check every screen the change touches in both languages. Every word of the app's own text must be visible: no `…`, no word broken in the middle. Fix the layout (wrap, another row), never by truncating. Set it back with `xcrun simctl ui booted content_size large`.
6. **Then Android again** (`/android-verify`) if you changed anything, and repeat until neither platform needs a fix.

Report what you saw on each platform, and what you could not check (for example the subscription plans, which do not load in the simulator).
