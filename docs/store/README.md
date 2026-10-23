# Store assets

Screenshots, icons, the Play feature graphic and the App Store listing text for the redesigned app, in English (`en`) and Japanese (`ja`).

| Folder | Store slot | Size |
|---|---|---|
| `ios/iphone_6.9in_1320x2868/{en,ja}` | App Store, iPhone 6.9" | 1320 x 2868 |
| `ios/iphone_6.5in_1284x2778/{en,ja}` | App Store, iPhone 6.5" (older listings) | 1284 x 2778 |
| `ios/iphone_6.3in_1206x2622/{en,ja}` | App Store, iPhone with Dynamic Island (medium display), the slot App Store Connect requires | 1206 x 2622 |
| `ios/iphone_duo_outer_1398x2034/{en,ja}`, `ios/iphone_duo_inner_2007x2853/{en,ja}` | App Store, iPhone Duo outer and inner display | 1398 x 2034, 2007 x 2853 |
| `ios/ipad_13in_2064x2752/{en,ja}` | App Store, iPad 13" | 2064 x 2752 |
| `ios/app_icon_1024.png` | App Store icon (the build supplies it too) | 1024 x 1024 |
| `ios/metadata/` | App Store listing text (see below) | |
| `ios/creative/header_3840x1646.png` | App Store product page header (iOS 27+), no text, for every language | 3840 x 1646 |
| `ios/creative/search_results_3840x2560_{en,ja}.png` | App Store search results image (iOS 27+) | 3840 x 2560 |
| `ios/creative/header_brand_3840x1646_{en,ja}.png` | App Store product page header in the Google Play feature graphic's layout (globe, name, tagline), inside the safe area | 3840 x 1646 |
| `ios/creative/header_3840x1646.mp4` | App Store product page header video (iOS 27+), no text, loops | 3840 x 1646, 10 s |
| `ios/creative/search_results_1920x1280_{en,ja}.mp4` | App Store search results video (iOS 27+), loops | 1920 x 1280, 29.5 s |
| `ios/preview/iphone_886x1920_{en,ja}.mp4` | App preview for iPhone (6.9", 6.5" and iPhone Duo all take 886 x 1920) | 886 x 1920, 29.5 s |
| `ios/preview/ipad_1200x1600_{en,ja}.mp4` | App preview for iPad 13" | 1200 x 1600, 29.5 s |
| `android/phone_1080x1920/{en,ja}` | Google Play, phone screenshots | 1080 x 1920 |
| `android/app_icon_512.png` | Google Play, app icon | 512 x 512 |
| `android/feature_graphic_1024x500_{en,ja}.png` | Google Play, feature graphic | 1024 x 500 |

Screenshot-only versions (the app screens with nothing added) are in `ios/plain/…` and `android/plain/phone_1110x2220/{en,ja}`, with the same file names:

| Folder | Size | How it was fitted |
|---|---|---|
| `ios/plain/iphone_6.9in_1320x2868` | 1320 x 2868 | the capture as is |
| `ios/plain/iphone_6.5in_1284x2778` | 1284 x 2778 | the capture as is |
| `ios/plain/iphone_6.3in_1206x2622` | 1206 x 2622 | the capture as is |
| `ios/plain/iphone_duo_{outer,inner}_*` | 1398 x 2034, 2007 x 2853 | the capture as is |
| `ios/plain/ipad_13in_2064x2752` | 2064 x 2752 | the capture as is |
| `android/plain/phone_1110x2220` | 1110 x 2220 | Google Play allows at most 2:1, so the system status bar and gesture handle were cut off and the edge columns repeated 15 px on each side |

Eight screens per set, in listing order: passport, passport in mixed stamp designs, passport by region, world map, map check-in, collections, a country's regional flags (Japan), stamp-rally board.
All files are PNG without transparency.

## How they were made

1. `tools/make_seed.py <app.db3> <dir>` writes demo databases (`seed_en.db3`, `seed_ja.db3`): 16 arrivals in 12 countries and 9 check-ins on the 全国神宮二十五社巡り board. Place names are stored in the recording language, so each language has its own database.
2. The app ran on the iPhone 17 Pro Max and iPad Pro 13" (M5) simulators (iOS 26.5) and the Pixel emulator (Android 16) with the demo database, residence US (en) or JP (ja), the app language set accordingly, a 9:41 status bar and the location set to Kyoto. The raw captures go in `tools/raw/{ios69,ipad13,android}_{en,ja}/1_passport.png … 8_flags.png` (not committed); their listing order is `NAMES` in `compose.py`.
   - On the phones the passport and regions are shown with Display options → No duplicate countries, so the first rows are not all Japan. The iPad shows every arrival.
   - The map is centred with Show my location and zoomed out until the Kyoto pins spread out, with 平安神宮 selected.
   - Stamp designs (`7_designs`) is the passport with No duplicate countries and the design set to a mix. Choosing designs needs a subscription or the user's own API key, so for the capture the `ApiKey` preference was set to a placeholder and `StampStyle` to `Mixed`, and both were removed afterwards.
   - Regional flags (`8_flags`) is Collections → Japan, from a copy of the demo database with seven more prefectures visited (Miyagi, Aomori, Osaka, Hiroshima, Nara, Ishikawa, Fukuoka) so the first screen is full of tiles.
   - On the iOS simulator, write the app's settings with `xcrun simctl spawn <udid> defaults write <container>/Library/Preferences/com.companyname.flagsrally <key> …`. Editing the plist file directly is not picked up once the app has run.
3. `tools/plain.py` writes the screenshot-only versions. `tools/compose.py` adds the headline and frame and writes every size; both take raw names after the output folder to redo only those screens (`compose.py docs/store 7_designs 8_flags`); `tools/assets.py` draws the icons and feature graphics from the app's SVGs (rendered to `tools/svg/*_navy.svg.png` with `qlmanage`).
4. The PNGs were compressed with `pngquant --quality=85-98`.

Headlines are in `COPY` at the top of `compose.py` (screenshots), `creative.py` (search results) and `assets.py` (feature graphic).

### Simulators (iOS 27, Xcode 27.1)

Every size is its own simulator capture at the store resolution; nothing is scaled from another size.

| Set | Simulator | Runtime |
|---|---|---|
| 6.9" | iPhone 17 Pro Max | iOS 27.0 |
| 6.5" | iPhone 14 Plus | iOS 27.0 |
| 6.3" | iPhone 17 | iOS 27.0 |
| iPad 13" | iPad Pro 13-inch (M5) | iOS 27.0 |
| iPhone Duo outer / inner | iPhone Duo, closed / open (inner screen in portrait) | iOS 27.1 |

The app was built with the iOS 27.1 SDK (Xcode 27.1, `ValidateXcodeVersion=false`). AXe drives the screens by accessibility label, so the same steps work on every size. On the iPhone Duo, AXe touches do not reach the screens, so touches, the fold and screenshots go through [duoctl](https://github.com/skarol/duoctl) (third party, uses private HID events; reviewed at efd5e3d).

### Header and search results (`tools/creative.py`)

Each device crops these differently, so the key art sits in the safe area of Apple's templates: header x 1097-2743, y 493-1154; search results x 836-3004, y 765-1795. The header is the passport emblem with stamps from the captures around it, and has no text so one image serves every language. The search results image has the headline and two phones (passport and a stamp trail) per language. A 16:9 universal asset (5244 x 2950 PNG) could replace both, but is not made.

`tools/creative_video.py` makes the video versions (H.264, 30 fps, muted when shown, so the silent audio track is only there for players that expect one): the header's stamps land one by one around the emblem and fade before the loop restarts; the search results video plays the iPhone app preview inside a phone next to the headline.

### App previews (`tools/preview.sh`)

Real screen recordings (`xcrun simctl io <udid> recordVideo`) of the app with the demo database, driven with AXe: passport, regions, world map, the Map tab with the 平安神宮 pin and its card, Collections and the 全国神宮二十五社巡り trail. The simulator's location is flagged as simulated and the app refuses to check in with it, so the previews do not show a check-in. `preview.sh <recording> <out.mp4> <w> <h> [start] [length]` crops and scales to the preview size and writes H.264 High 4.0 at 30 fps and about 10 Mbps, with a silent stereo AAC track, as App Store Connect expects. Choose the poster frame (default 5 s) in App Store Connect.

### File sizes

Apple publishes no per-image limit, so `check_ios.py` keeps every image under 8 MB (most guides report 10 MB); the largest is about 3 MB. Previews may be up to 500 MB; these are 10-17 MB.

## App Store listing (`ios/metadata/`)

The text App Store Connect asks for, in the folder layout `fastlane deliver` reads (`en-US/`, `ja/`), so it can be pasted in or uploaded as is.
Run `python3 docs/store/tools/check_ios.py` after any change: it checks the screenshot, header and search results sizes, alpha, counts and file sizes, the previews (size, codec, frame rate, audio, length), and the text limits below.

| File | Field | Limit |
|---|---|---|
| `name.txt` | Name | 30 characters |
| `subtitle.txt` | Subtitle | 30 characters |
| `promotional_text.txt` | Promotional text (can be changed without a review) | 170 characters |
| `description.txt` | Description (plain text) | 4000 characters |
| `keywords.txt` | Keywords, comma separated, each longer than two characters, not the app name | 100 bytes (a Japanese character is 3) |
| `release_notes.txt` | What's New in This Version | 4000 characters |
| `support_url.txt` / `privacy_url.txt` | Support URL (must lead to a way to contact us) / Privacy Policy URL | |
| `copyright.txt`, `primary_category.txt`, `secondary_category.txt` | Copyright (© is added by Apple), categories | |
| `review_information/notes.txt` | Notes for App Review | 4000 bytes |

Screenshot rules (App Store Connect, Screenshot specifications): PNG or JPEG without alpha, 1–10 per size.
App Store Connect requires the iPhone with Dynamic Island (medium display) set (`iphone_6.3in`); the 6.9" and 6.5" sets are used on those devices instead of scaled copies. iPad needs the 13" set because the app runs on iPad (`UIDeviceFamily` 1 and 2).
The 1024 icon is taken from the build's asset catalog, so it is not uploaded in App Store Connect.

`release_notes.txt` covers everything since the previous iOS release (build 5, November 2025), so rewrite it for the next version with `/release-notes`.

### Set in App Store Connect, not in these files

- **App Review contact**: name, email and phone (with a leading `+`).
- **App Privacy**: no tracking. Declare these as collected, not linked to the user unless noted:
  - Location > Precise Location, App Functionality: the stamped place is sent to Google's Geocoding API to get the country and region name.
  - From the Google Maps SDK's privacy manifest: Crash Data, Performance Data and Product Interaction (Analytics); Device ID and Other Data Types (Analytics, App Functionality; linked).
  - From the RevenueCat SDK's privacy manifest: Purchase History (App Functionality).
  - Re-read `GoogleMaps.bundle/PrivacyInfo.xcprivacy` and `RevenueCat.framework/PrivacyInfo.xcprivacy` in the built `.app` after updating those packages.
- **Age rating**: no objectionable content; the app has no web browser and no user-to-user contact, so the answers give 4+.
- **Subscriptions** (monthly and yearly, auto-renewable, first 3 months free as the paywall says): each needs a display name, a description and a review screenshot of the paywall, and the group needs its localized name. Submit them with the version if they are new or changed.
- **Terms of Use (EULA)**: the description links to our Terms of Use, as Apple asks of apps with auto-renewable subscriptions.
