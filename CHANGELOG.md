# Changelog

All notable changes to FlagsRally are recorded here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- **Passport stamps in more designs.** Settings has a new Passport stamps section with thirteen designs besides the original square (now called Entry stamp / 入国スタンプ), each with a name of its own (Postage stamp, Par avion, Travel pass, Cinema ticket, Globe, Compass rose, Sunrise, Medal, Star seal, World map, Voyage, Hexagon, Pennant), in the style of travel stamps and postmarks, each in its own ink: an airmail envelope (its text lined up at the left like an address), a compass hexagon, a sunrise, a scalloped medal with laurels, a world map oval, a ticket, a compass seal, a triangle, a star seal, a globe postmark (its globe drawn from real coastlines), a postage stamp with a solid perforated border, a movie ticket (solid deep burgundy with the country in white, notched at both ends, a barcode on its stub), and a journey compass: a compass ring round a rising sun, clouds, mountains and a plane, a train, a ship, a car and a hiker swirling round a globe, in soft colours so the flag on the globe stands out, with the name and date below. The original square now has the same worn ink as the others. Each is about as large as the original stamp: the designs whose motifs need room above and below leave out ARRIVAL, and long names shrink to stay on one line. Their motifs are globes, compass roses, postmark waves, a world map grid and sparkles; where travel appears it is a row of several ways (plane, ship, train, car, on foot), never one of them alone. Settings has two parts: the design (mix them, one per country, or the same design for every stamp, picked from a list that opens below the row, each design shown with a small preview) and, apart from it, a switch that tilts each stamp a little like one pressed by hand, with any design. Until the switch is used, a mix is tilted as before and the other choices stand upright. A stamp keeps its look every time the passport is opened, and the original square stays the default. Choosing designs and tilt needs a subscription or the user's own Google Maps API key; without one, Settings shows a note with the subscribe button and the passport keeps the Entry stamp design (the original square), upright. Choices made are kept and come back once unlocked. A long place name goes onto two even lines, broken between words ("Saint Vincent and / the Grenadines", "ボスニア・/ヘルツェゴビナ") rather than in the middle of one, and gets smaller instead of taking a third line; on a narrow phone at its largest display size each stamp shrinks as a whole instead of squeezing its name and date, so nothing is cut off in English or Japanese at any text size. View stamp on the map shows the stamp in the same design as the passport, larger, with the country and the region, their flags and the date. After stamping in a new country or region, the popup shows the new stamp in the passport's design, pressed down onto the page with confetti; when both are new, the country's stamp comes first and a swipe brings the region's, and OK comes on once every stamp has been shown. A new region's flag then gets its own popup in the same style: Added to your collection, the region's tile as the country's regional flags in Collections show it, pressed down, with the country's progress bar moving on by one.
- **Each country's regional flags in its own style.** In Collections, a visited region's tile now looks like its country: Japan's waves and Mount Fuji under the sun, America's stars and stripes, Canada's maple leaves, a Paris street plaque for France, Bauhaus shapes for Germany, laurel for Italy, a Moorish arch for Spain, azulejo corners for Portugal, tulips for the Netherlands, crosses for Switzerland, Klimt's gold for Austria, an Art Nouveau curve for Czechia, Selbu stars for Norway, vyshyvanka stitches for Ukraine and songket for Malaysia, each in the country's colours. The flag, name, date and ARRIVAL stay where they were; a region not visited yet keeps its plain tile in the country's frame colour. A name now stays inside its band, getting smaller and then going onto two lines, in English and Japanese at any text size. Added to your collection shows the tile the same way. Each country's tile is drawn once per size, so the lists scroll as before.

### Fixed
- **A passport of Voyage stamps scrolled slowly.** Its picture is drawn once for each size and reused, so scrolling it is as smooth as with the other designs.
- **A stamp recorded in the wrong country.** Google's place lookup started returning a stray Hanoi address among its results for any place, sometimes first. A stamp could then be saved as Vietnam (Saint Vincent and the Grenadines did), or with Hà Nội as the region in Japanese (San Francisco did). The country is now the one the results agree on, and each name is taken from that area's own result in that country; stray results elsewhere are ignored.

## [1.2.2] - Unreleased

### Added
- **Hide collections with no stamps.** A display options button at the top right of Collections can leave off the countries never visited and the stamp trails with nothing checked in, so only those with stamps are listed. Add stamp trail stays, the countries' count still counts every country, and the choice is remembered.
- **Choose the map's pins freely.** Each chip on the Map is now selected exactly while its pins are shown, and tapping it shows or hides just those pins. All Pins is replaced by one Select all / Clear all button. While every pin is shown it looks like a selected chip, with two white ticks, and a tap clears them all; when some are shown it has a box with a dash, when none are an empty box, and a tap shows them all. It and a Filter chip stay at the left while the trails' chips scroll. Filter opens every stamp trail and the arrival location in one list with Select all and Clear all, and a series' list ticks the boards that are shown.
- **See a stamp again from its pin on the map.** Selecting an arrival or a checked-in place on the Map now shows a View stamp button on its card. An arrival opens its passport stamp (the country and region flags, names and date); a checked-in place opens the trail's stamp, without the confetti of a new check-in.

### Changed
- **A checked-in place no longer offers Check In on the map.** Checking in again replaced the date of the first visit; the card now shows View stamp and Reset check-in instead. To record a new date, reset the check-in and check in again.
- **Development:** The iOS and Mac Catalyst app now start through a scene (`SceneDelegate` and `UIApplicationSceneManifest` in `Info.plist`). Apps built with Xcode 27 must use the UIScene lifecycle or they close at launch; it also works with the current Xcode 26.
- **Development:** .NET MAUI is updated to 10.0.110 (from 10.0.101), the version the .NET for iOS release for Xcode 27 recommends. Builds need .NET SDK 10.0.401 with workload set 10.0.401.1, and Xcode 27.0 for iOS and Mac Catalyst.
- **Development:** The Mac Catalyst app now needs Mac Catalyst 17.0 (macOS 14) or later, up from 15.0. The .NET for iOS release for Xcode 27 refuses to build for anything older.
- **Development:** The App Store files are complete: the listing text in English and Japanese, screenshots retaken on a simulator of each size (6.9", 6.5", 6.3", iPad 13" and both iPhone Duo screens, none scaled from another size), app previews for iPhone and iPad, and the iOS 27 header and search results images and videos. `docs/store/tools/check_ios.py` checks them against App Store Connect's sizes, formats and text limits.
- **Development:** A new `/ios-release` skill prepares an iOS release: a clean Release build is launched on the simulator and checked for crashes, fixing and re-checking until it is clean, then the version and build number are set in `Info.plist` on a `release/ios-*` branch, the signed `.ipa` is built, and it is delivered to App Store Connect after the user confirms (or opened in Transporter when no API key is set up).
- **Development:** App Store Connect API key files (`.p8`) are treated like the other local secrets: a commit with one staged is blocked.
- **Development:** The iOS version is set to 1.2.2 (build 20) in `Info.plist`, the build sent to App Store Connect for this release, so the next iOS release counts on from it.
- **Development:** iOS and Mac Catalyst builds no longer stop when the installed Xcode is newer than the one the .NET for iOS SDK was made for.

### Fixed
- **The how-to pages on the iPhone Duo.** On the outer screen the card ran off the left edge and its arrow pointed at nothing, because the tab bar stands down the right side there. The card now sits beside that tab bar with an arrow pointing sideways at the tab; in landscape the picture goes beside the text so everything fits. The pages also follow the screen when the phone is folded or unfolded, and on the iPad the arrow now points at the right tab.

## [1.2.1] - Unreleased

### Added
- **A how-to page showing where to see the pages again.** Before the location page, the how-to pages now open Settings and point at How to use under Information, at the bottom of Settings, with a picture of it framed, so it is clear how to open them again later.
- **Checked-in places are marked when choosing among pins at the same spot.** In the list that opens where pins overlap, a place already checked in shows a check and its date, such as "✓ 鹿島神宮 – 全国一の宮巡り (15 Oct 2026)".

### Changed
- **Stamp trails scroll smoothly with many stamps.** Each stamp image sized itself when it arrived, so the tile and the list were laid out again for every stamp, and a trail with many visited places stuttered while scrolling (on 全国一の宮巡り with all 109 visited, the slowest frames took 45–100 ms). The images now have a fixed size from the start (about 20 ms). Region flags in the passport and on a country's regions page get a fixed box for the same reason.
- **The list of pins at the same spot keeps its order.** Tapping where pins overlap lists them to choose from; the list now has the same order whichever pin was tapped or chosen before, and choosing one no longer brings it to the front of the others.

### Fixed
- **Confetti before the stamp.** After a check-in the confetti could fall on an empty frame while the stamp was still downloading; it now starts once the stamp is shown.

## [1.2.0] - Unreleased

### Added
- **How-to pages on the first launch.** A new install opens four short pages: the passport (with a note that the cover can be changed under Country of residence in Settings), stamping on the map, boards, and the location. The pages sit just above the tab bar and open each tab behind them, with an arrow pointing at it, so it is clear where everything is; the first three also show the app's own screens with what to tap framed. The last page explains why the location is needed before asking for it. Settings > Information > How to use opens them again. People who already use the app are not shown them after the update.
- **Boards of one series share a chip on the map.** Boards added from the same shared folder, such as the manhole card issues, now appear as one chip like "マンホールカード ▾". Tap it to tick boards one by one, or use Select all / Clear selection; the chip shows how many are chosen. In a subfolder the series is the whole path, such as マンホールカード／関東, so subfolders with the same name in different series stay apart. Boards added from a file or before this version are grouped by their name without the issue number.
- **A hint for placing a stamp spot.** Until you have done it once, the map says that pressing and holding the map puts a pin where you want to stamp.
- **Pins at the same spot fan out on the map.** Where pins sit on the same place or within a few metres, such as 香取神宮 on two boards, they now lean apart like a hand of cards with their tips still on the spot, so each one can be seen even zoomed all the way in.

### Changed
- **Development:** Code that the how-to and Collections changes left unused is removed, and 20 app strings that nothing used.
- **Boards are now stamp trails (スタンプラリー).** The screens and messages say "Add stamp trail", "Manage Stamp Trails" and so on in English, and 「スタンプラリーを追加」, 「スタンプラリーの地点」 in Japanese, instead of board / ボード.
- **The classic stamp spot points at its spot.** The orange figure now stands on a leg that tapers to a point, so it is clear which point it marks.
- **Development:** A new `/howto-screens` skill retakes the how-to screenshots on the Android emulator, in English and Japanese, with the button to tap framed.
- **The map's buttons say "stamp".** "Get Location" and "Check in here" are now "Stamp where I am" and "Stamp here" (「現在地でスタンプ」「ここでスタンプを押す」), so they are not mistaken for checking in at a board's place. Android no longer asks for the location the moment the app opens.
- **Boards open and scroll smoothly, with fewer downloads.** A custom board downloaded the stamp image of every place, visited or not (all 109 on 全国一の宮巡り at 0 / 109), and stuttered while they arrived. Only visited places' stamps are loaded now. The regional flags board no longer loads the flags of regions not visited yet (the US state flags came from the internet), and the passport no longer loads region flags while it shows countries. The cards and tiles in these lists are also quicker to build.
- **Development:** Store screenshots for Google Play and the App Store (iPhone 6.9" and 6.5", iPad 13"), in English and Japanese, with and without headlines, are kept with the scripts that make them, together with the Play icon and feature graphic. They show the current screens, and the headlines say stamp trail instead of board.

### Fixed
- **An error after closing Create backup.** Leaving the save screen without saving showed "Error: A task was canceled."; it now just goes back to Settings.
- **The delete button in Manage boards looking disabled.** After ticking a board the button stayed grey, though it worked; it now turns red once something is ticked.
- **Settings closing the app on an iPhone with a language set just for FlagsRally.** Until a country of residence was chosen, the passport cover stayed empty and opening Settings closed the app. The country now comes from the device's region, so the cover shows and Settings opens.
- **The passport date broken over two lines with large text.** With iOS's largest text sizes a stamp's date wrapped after the month ("09 10月" / "2026"). The date on passport stamps, regional flags and custom boards now gets a little smaller when needed and stays on one line; at normal sizes it looks as before.

## [1.1.22] - Unreleased

### Changed
- **Development:** The unit tests pass on every machine. Two Japanese test files were saved in Shift_JIS, so two geocoding tests failed everywhere except Japanese Windows; they are UTF-8 now, and a test stops non-UTF-8 source files. The tests that call the real Google Geocoding API are skipped, with the reason, where the real key or the network is missing.
- **Development:** A new `/android-release` skill prepares an Android release: a clean Release build is launched on the emulator and checked for crashes, fixing and re-checking until it is clean, then the version is bumped, the PR merged, the store release notes written, and the signed bundle uploaded to Google Play with those notes after the user confirms (as a draft unless asked to publish).
- **Development:** `.claude/settings.local.json`, where the release signing passwords are kept, is treated like the other local secrets: Claude Code is not allowed to read it, and a commit with it staged is blocked.
- **Development:** A new `/project-skills` skill explains which skills this project has and when to use each, reading `.claude/skills/` each time so new skills are included without editing it.

### Fixed
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
