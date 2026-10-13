# Store assets

Screenshots, icons and the Play feature graphic for the redesigned app, in English (`en`) and Japanese (`ja`).

| Folder | Store slot | Size |
|---|---|---|
| `ios/iphone_6.9in_1320x2868/{en,ja}` | App Store, iPhone 6.9" | 1320 x 2868 |
| `ios/iphone_6.5in_1284x2778/{en,ja}` | App Store, iPhone 6.5" (older listings) | 1284 x 2778 |
| `ios/ipad_13in_2064x2752/{en,ja}` | App Store, iPad 13" | 2064 x 2752 |
| `ios/app_icon_1024.png` | App Store icon (the build supplies it too) | 1024 x 1024 |
| `android/phone_1080x1920/{en,ja}` | Google Play, phone screenshots | 1080 x 1920 |
| `android/app_icon_512.png` | Google Play, app icon | 512 x 512 |
| `android/feature_graphic_1024x500_{en,ja}.png` | Google Play, feature graphic | 1024 x 500 |

Screenshot-only versions (the app screens with nothing added) are in `ios/plain/…` and `android/plain/phone_1110x2220/{en,ja}`, with the same file names:

| Folder | Size | How it was fitted |
|---|---|---|
| `ios/plain/iphone_6.9in_1320x2868` | 1320 x 2868 | the capture as is |
| `ios/plain/iphone_6.5in_1284x2778` | 1284 x 2778 | scaled to the width, 6 rows trimmed top and bottom |
| `ios/plain/ipad_13in_2064x2752` | 2064 x 2752 | the capture as is |
| `android/plain/phone_1110x2220` | 1110 x 2220 | Google Play allows at most 2:1, so the system status bar and gesture handle were cut off and the edge columns repeated 15 px on each side |

Six screens per set, in listing order: passport, regional flags, world map, map check-in, collections, stamp-rally board.
All files are PNG without transparency.

## How they were made

1. `tools/make_seed.py <app.db3> <dir>` writes demo databases (`seed_en.db3`, `seed_ja.db3`): 16 arrivals in 12 countries and 9 check-ins on the 全国神宮二十五社巡り board. Place names are stored in the recording language, so each language has its own database.
2. The app ran on the iPhone 17 Pro Max and iPad Pro 13" (M5) simulators (iOS 26.5) and the Pixel emulator (Android 16) with the demo database, residence US (en) or JP (ja), the app language set accordingly, a 9:41 status bar and the location set to Kyoto. The raw captures go in `tools/raw/{ios69,ipad13,android}_{en,ja}/1_passport.png … 6_board.png` (not committed).
   - On the phones the passport and regions are shown with Display options → No duplicate countries, so the first rows are not all Japan. The iPad shows every arrival.
   - The map is centred with Show my location and zoomed out until the Kyoto pins spread out, with 平安神宮 selected.
   - On the iOS simulator, write the app's settings with `xcrun simctl spawn <udid> defaults write <container>/Library/Preferences/com.companyname.flagsrally <key> …`. Editing the plist file directly is not picked up once the app has run.
3. `tools/plain.py` writes the screenshot-only versions. `tools/compose.py` adds the headline and frame and writes every size; `tools/assets.py` draws the icons and feature graphics from the app's SVGs (rendered to `tools/svg/*_navy.svg.png` with `qlmanage`).
4. The PNGs were compressed with `pngquant --quality=85-98`.

Headlines are in `COPY` at the top of `compose.py` (screenshots) and `assets.py` (feature graphic).
