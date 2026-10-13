---
name: howto-screens
description: Retake the screenshots on FlagsRally's how-to pages (Resources/Images/howto_*_en.png / _ja.png) on the Android emulator, with the button to tap framed in stamp red. Use whenever the passport, the map's bottom panel ("Stamp where I am") or the Collections cards change, when the how-to pages look out of date, or when the user asks to update the how-to screenshots ("使い方の画像を撮り直して", "how to のスクショ更新").
---

# Retake the how-to screenshots

The first three how-to pages (a card over the app, with the tab bar left in sight; Settings > Information > How to use) show the real screens: the passport, the map with "Stamp where I am" framed, and Collections with the "Add board" card framed. One set per language: `Resources/Images/howto_{passport,stamp,boards}_{en,ja}.png`. They are taken on Android and used on iOS too, so they are cut to leave out the status bar and the tab bar, which differ between the two.

`tools/howto_screens.py` does the whole thing. For each language it puts sample data in the app (six arrivals in Japan, the United States and France; no boards, as on a first launch; the map hint already seen; Classic pins; a US cover for English and a Japanese one for Japanese), opens each screen, finds the button by its text, crops, draws the frame and writes the PNG. It saves the app's database and settings first and puts them back at the end.

1. **Install the current build:** `bash tools/emu.sh install` (the screenshots show what is installed).
2. **Take them:** `uv run tools/howto_screens.py` (or `--lang ja` for one language). It prints each file and, at the end, the database hash before and after: they must be the same. If not, restore with `bash tools/emu.sh db-restore` from a backup or by hand.
3. **Look at all six yourself** before using them, side by side:
   - the frame sits around the whole button or card, not cut off and not over the neighbouring card;
   - no status bar, tab bar, popup or half-loaded map in the picture;
   - the text in the picture is in the picture's language (English in `_en`, Japanese in `_ja`).
   If a screen moved (a new row, another panel), change the crop in `take()` in the script, not the PNGs by hand.
4. **Check the pages:** `bash tools/emu.sh install`, open Settings > How to use in English and Japanese, with large text too (`/android-verify`), and on iOS (`/ios-verify`).
5. **Commit** the six PNGs (and the script if it changed) as one change, and add a changelog line only if users would notice (`/update-changelog`).

When the text the script looks for changes (for example the button is renamed), update `TEXTS` in the script. A page that should frame another button: add it in `take()` and in `OnboardingViewModel.ScreenshotFor`, and keep `Every_screenshot_a_page_names_is_in_the_app` passing.

The images are 720 px wide (2x of a 360-dp page) and kept as they are (`Resize="False"` for `howto_*.png` in `FlagsRally.csproj`), about 1 MB for all six.
