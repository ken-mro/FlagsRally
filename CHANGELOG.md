# Changelog

All notable changes to FlagsRally are recorded here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.1.22] - Unreleased

### Added
- **How-to pages on the first launch.** A new install opens four short pages: the passport (with a note that the cover can be changed under Country of residence in Settings), stamping on the map, boards, and the location. The pages sit just above the tab bar and open each tab behind them, with an arrow pointing at it, so it is clear where everything is; the first three also show the app's own screens with what to tap framed. The last page explains why the location is needed before asking for it. Settings > Information > How to use opens them again. People who already use the app are not shown them after the update.
- **Boards of one series share a chip on the map.** Boards added from the same shared folder, such as the manhole card issues, now appear as one chip like "マンホールカード ▾". Tap it to tick boards one by one, or use Select all / Clear selection; the chip shows how many are chosen. Boards added from a file or before this version are grouped by their name without the issue number.
- **A hint for placing a stamp spot.** Until you have done it once, the map says that pressing and holding the map puts a pin where you want to stamp.
- **Pins at the same spot fan out on the map.** Where pins sit on the same place or within a few metres, such as 香取神宮 on two boards, they now lean apart like a hand of cards with their tips still on the spot, so each one can be seen even zoomed all the way in.

### Changed
- **The classic stamp spot points at its spot.** The orange figure now stands on a leg that tapers to a point, so it is clear which point it marks.
- **Development:** A new `/howto-screens` skill retakes the how-to screenshots on the Android emulator, in English and Japanese, with the button to tap framed.
- **The map's buttons say "stamp".** "Get Location" and "Check in here" are now "Stamp where I am" and "Stamp here" (「現在地でスタンプ」「ここでスタンプを押す」), so they are not mistaken for checking in at a board's place. Android no longer asks for the location the moment the app opens.
- **Boards open and scroll smoothly, with fewer downloads.** A custom board downloaded the stamp image of every place, visited or not (all 109 on 全国一の宮巡り at 0 / 109), and stuttered while they arrived. Only visited places' stamps are loaded now. The regional flags board no longer loads the flags of regions not visited yet (the US state flags came from the internet), and the passport no longer loads region flags while it shows countries. The cards and tiles in these lists are also quicker to build.
- **Development:** The unit tests pass on every machine. Two Japanese test files were saved in Shift_JIS, so two geocoding tests failed everywhere except Japanese Windows; they are UTF-8 now, and a test stops non-UTF-8 source files. The tests that call the real Google Geocoding API are skipped, with the reason, where the real key or the network is missing.
- **Development:** A new `/android-release` skill prepares an Android release: a clean Release build is launched on the emulator and checked for crashes, fixing and re-checking until it is clean, then the version is bumped, the PR merged, the store release notes written, and the signed bundle uploaded to Google Play with those notes after the user confirms (as a draft unless asked to publish).
- **Development:** `.claude/settings.local.json`, where the release signing passwords are kept, is treated like the other local secrets: Claude Code is not allowed to read it, and a commit with it staged is blocked.
- **Development:** A new `/project-skills` skill explains which skills this project has and when to use each, reading `.claude/skills/` each time so new skills are included without editing it.

### Fixed
- **Settings closing the app on an iPhone with a language set just for FlagsRally.** Until a country of residence was chosen, the passport cover stayed empty and opening Settings closed the app. The country now comes from the device's region, so the cover shows and Settings opens.
- **The passport date broken over two lines with large text.** With iOS's largest text sizes a stamp's date wrapped after the month ("09 10月" / "2026"). The date on passport stamps, regional flags and custom boards now gets a little smaller when needed and stays on one line; at normal sizes it looks as before.
- **Checking in the wrong board's place where two boards share a spot.** Where two boards have the same place, such as 香取神宮 on 全国一の宮巡り and 全国神宮二十五社巡り, choosing the second one in "Several pins are here" could leave the first one selected (seen on iPhone), so Check In recorded the first board's place and the second could look already checked in. Each pin is now its own place, so the one you choose is the one checked in.
- **The selected pin hidden behind another pin at the same spot.** The selected pin is now drawn in front, so you can see which one the card is about.

## [1.1.21] - Unreleased

### Fixed
- **The Android app crashing right after launch (1.1.20).** The Google Play build closed as soon as it opened, and clearing the cache did not help. The change in 1.1.20 that left out unused library code also removed code the subscription library needs at startup; it is undone, so the app starts again. A test now stops that change from coming back without the code it needs.

## [1.1.20] - Unreleased

### Added
- **Add several boards at once.** In Add Board, boards now have check boxes: pick as many as you like, even from different folders, or use "Select all" in a folder such as マンホールカード, then tap "Add selected boards". One message at the end lists what was added. Adding from a file on the device also accepts several files.

### Changed
- **The Passport counts show arrivals, countries and regions.** Check-ins on custom boards are no longer counted here (they belong to Collections); the first count is now the number of arrival stamps. The counts sit on a dashed card with an icon above each: a flag for arrivals, like the arrival pins on the map, a globe for countries and a folded map for regions. On a narrow screen with large text, the card moves below the passport cover so every label stays on one line.
- **A my-location button in the app's style.** Google's square button at the top right of the map is replaced by a round button above the zoom buttons, which moves up with them above the cards.
- **Get Location works like a long press.** It puts the check-in spot at where you are and shows its card, just as a long press does at the place you pressed. Recording is then always "Check in here" on that card, instead of Get Location going straight to the confirmation.
- **The pin card's Directions button is gone.** It did the same as the directions button on the map.
- **Directions and open-in-Maps are back on the map.** They are round buttons above the zoom buttons, shown while a card shows a place, like Google's own toolbar, instead of small icons squeezed into the cards' title rows. The cards have room for their titles again on narrow phones.
- **Leaner Android build.** The Android app now leaves out unused code from the libraries it includes.
- **Development:** Every pull request now has to update this changelog. A hook blocks opening one that does not.
- **Development:** A test now fails when any text other than a board, country or region name is set to be cut off, or when the app's own text is used as a placeholder. Layout changes are checked on both Android and iOS, with large text in English and Japanese.
- **Development:** The emulator helper script now installs only to the emulator, even when a phone is plugged in too.

### Fixed
- **The map's compass hidden under the filter chips.** When the map is turned, the compass now shows just below the chips (Android: left, iPhone: right).
- **The Passport counts running over the title after scrolling.** On iPhone with large text, the counts covered "Passport" and wrapped onto two lines. The title bar now shows them as icons with numbers on one line, the same icons as in the passport header.
- **Long board names cut off in the board's title and on Collections cards.** The title bar shows the whole name, on two lines and in a smaller font when needed, and card titles are no longer cut after two lines. On iPhone, the last row of cards is no longer hidden behind the tab bar.
- **Galaxy Z Fold cover screen.** The Passport toolbar's country filter shows "All" / すべて, or the selected country's flag, when "All Countries" or a long country name does not fit, instead of breaking into a letter per line or taking a second row. The tab names shrink to fit instead of showing "コレクシ…" with Japanese and large text.
- **Map card text cut off after the directions and open-in-Maps icons were added.** The check-in spot guide's title ("Check in at this pin" / このピンの場所で記録) and the pin details card's title and subtitle now wrap instead of ending in "…". The pin card's buttons move to a second line when they do not fit, instead of squeezing Check In down to "チ" in Japanese with large text.
- **Long board names cut off in Add Board and Manage Boards.** Add Board shows the whole name, and Manage Boards shows up to two lines.
- **"All Countries…" cut off on the Passport tab on iPhone.** The country filter now fits at the normal text size and wraps onto a second line with large text.
- **Stamp and tile dates cut off with large text.** The arrival date on passport stamps and board tiles ("09 Oct 20…") wraps instead.
- **The Google Maps API key hint cut off in Settings.** The hint ("使用する場合は入力してくだ…") is now shown in full above the box instead of inside it.
- **The selected tab's name cut off on Android** ("コレクシ…" with Japanese and large text). It is now the same size as the other tabs.
- **Custom board tiles still leaving empty space on narrow screens.** On narrow screens such as the Galaxy Z Fold cover screen, a visited tile was shorter than the unvisited tiles next to it, leaving a gap under it, for example on 全国神宮二十五社巡り. Unvisited tiles are now exactly as tall as visited ones on every screen width.

## [1.1.17] - 2026-10-10

### Fixed
- **Custom board tiles leaving empty space on some devices.** A visited tile's image took whatever height the device measured for the downloaded picture (it depends on screen width and density), so visited and unvisited tiles in a row could differ in height and leave a gap below, for example on 全国神宮二十五社巡り. The image height now comes from the column width and the board's image shape. Devices that never showed the gap look the same as before.
- **The map moving when a card appeared.** Showing the check-in spot guide or the pin details shrank the map and shifted what was on screen. The map now keeps its size and the cards float over its lower edge.
- **Zoom buttons hidden under the cards.** The map's zoom buttons are now the app's own + / − buttons at the bottom right. They move up to sit just above whichever card is showing, and back down when it closes.
- **Google's directions and open-in-Maps buttons hidden under the cards.** These are now on the cards themselves. The check-in spot guide has directions and open-in-Maps icons, and the pin details card has an open-in-Maps icon beside its Directions button.
- **The map jumping to a far-away place after returning to the app.** Starting the app while it was already open (from another app, a notification or a link) created a second screen whose map opened at Google's default position. The app now reuses its existing screen. If Android rebuilds the map anyway, the map returns to where it was left.
