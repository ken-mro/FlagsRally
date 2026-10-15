# /// script
# requires-python = ">=3.10"
# dependencies = ["pillow"]
# ///
"""Take the screenshots shown on the how-to pages, in English and Japanese, on the Android emulator.

    uv run tools/howto_screens.py            # both languages
    uv run tools/howto_screens.py --lang ja  # one language

For each language it puts sample data in the app (a few arrivals, the map hint already seen), opens the
screen, finds the button to point at by its text, crops the part that matters, draws a stamp-red frame
around the button and saves Resources/Images/howto_<page>_<lang>.png. The app's own database and
settings are saved first and put back at the end (the database hash is printed; it must not change).

Run it from the repository root with the Debug build installed (bash tools/emu.sh install), after any
change to the passport, the map's bottom panel, the Collections cards or the Information card in Settings.
"""
import argparse
import html
import os
import re
import sqlite3
import subprocess
import sys
import tempfile
import time

from PIL import Image, ImageDraw

PKG = "com.companyname.flagsrally"
DB = "files/FlagsRally.db3"
PREFS = f"shared_prefs/{PKG}_preferences.xml"
OUT_DIR = os.path.join("Resources", "Images")
OUT_WIDTH = 720          # 2x of the 360-dp page width
STAMP_RED = (185, 64, 71)

# Texts the script looks for, per language.
TEXTS = {
    "en": {"stamp": "Stamp where I am", "add_board": "Add stamp trail", "custom_boards": "Stamp trails", "tab": "Passport",
           "information": "Information", "how_to": "How to use", "privacy": "Privacy Policy"},
    "ja": {"stamp": "現在地でスタンプ", "add_board": "スタンプラリーを追加", "custom_boards": "スタンプラリー", "tab": "パスポート",
           "information": "情報", "how_to": "アプリの使い方", "privacy": "プライバシーポリシー"},
}
# Country of residence for the passport cover in the screenshots.
COVER = {"en": "US", "ja": "JP"}

# A few arrivals so the passport has stamps: (country code, country name (en, ja), region code, region name (en, ja)).
SAMPLE_ARRIVALS = [
    ("JP", ("Japan", "日本"), "13", ("Tokyo", "東京都")),
    ("JP", ("Japan", "日本"), "26", ("Kyoto", "京都府")),
    ("JP", ("Japan", "日本"), "01", ("Hokkaido", "北海道")),
    ("US", ("United States", "アメリカ合衆国"), "CA", ("California", "カリフォルニア州")),
    ("US", ("United States", "アメリカ合衆国"), "NY", ("New York", "ニューヨーク州")),
    ("FR", ("France", "フランス"), "IDF", ("Île-de-France", "イル＝ド＝フランス")),
]


def adb(*args, input=None, check=True):
    env = dict(os.environ, MSYS_NO_PATHCONV="1")
    return subprocess.run(["adb", *args], input=input, capture_output=True, check=check, env=env).stdout


def run_as_read(path):
    return adb("exec-out", "run-as", PKG, "cat", path, check=False)


def run_as_write(path, data):
    with tempfile.NamedTemporaryFile(delete=False) as f:
        f.write(data)
    adb("push", f.name, "/data/local/tmp/howto.bin")
    os.unlink(f.name)
    adb("shell", f"cat /data/local/tmp/howto.bin | run-as {PKG} sh -c 'cat > {path}'")
    adb("shell", "rm -f /data/local/tmp/howto.bin")


def sha1(path):
    return adb("shell", "run-as", PKG, "sha1sum", path).decode().split()[0]


def emu(*args):
    subprocess.run(["bash", "tools/emu.sh", *args], check=True, capture_output=True)


def screen():
    raw = adb("exec-out", "screencap", "-p")
    path = tempfile.mktemp(suffix=".png")
    with open(path, "wb") as f:
        f.write(raw)
    return Image.open(path).convert("RGB")


def find(text, tries=8, exact=False):
    """Bounds (left, top, right, bottom) of the first element whose text contains (or is) `text`."""
    for _ in range(tries):
        adb("shell", "uiautomator", "dump", "/sdcard/howto.xml", check=False)
        xml = adb("exec-out", "cat", "/sdcard/howto.xml", check=False).decode("utf-8", "replace")
        for node in re.finditer(r"<node [^>]*>", xml):
            n = node.group(0)
            label = html.unescape((re.search(r' text="([^"]*)"', n) or [None, ""])[1])
            if (label == text) if exact else (text in label):
                return tuple(map(int, re.findall(r"\d+", re.search(r'bounds="([^"]*)"', n)[1])))
        time.sleep(1)
    sys.exit(f"could not find '{text}' on the screen")


def swipe_up(times):
    for _ in range(times):
        adb("shell", "input", "swipe", "540", "1800", "540", "600", "250")
        time.sleep(0.5)


def save(image, crop, mark, name):
    """Crop, frame the button (screen coordinates) and scale to OUT_WIDTH."""
    if mark:
        draw = ImageDraw.Draw(image)
        pad = 16
        left, top, right, bottom = mark
        draw.rounded_rectangle((left - pad, top - pad, right + pad, bottom + pad), radius=28, outline=STAMP_RED, width=10)
    part = image.crop(crop)
    height = round(part.height * OUT_WIDTH / part.width)
    part = part.resize((OUT_WIDTH, height), Image.LANCZOS)
    path = os.path.join(OUT_DIR, name)
    part.save(path, optimize=True)
    print(f"  {path} ({os.path.getsize(path) // 1024} KB)")


def sample_database(original, lang):
    """The app's database with the sample arrivals in place of its own, and no boards."""
    path = tempfile.mktemp(suffix=".db3")
    with open(path, "wb") as f:
        f.write(original)
    con = sqlite3.connect(path)
    con.execute("DELETE FROM ArrivalLocation")
    # No boards yet, as on a first launch: the boards page shows the "Add board" card alone.
    for table in ("CustomLocation", "CustomBoard"):
        if con.execute("SELECT 1 FROM sqlite_master WHERE type='table' AND name=?", (table,)).fetchone():
            con.execute(f"DELETE FROM {table}")
    ticks = 639271947338696231  # .NET ticks, October 2026
    i = 1 if lang == "ja" else 0
    for n, (cc, country, rc, region) in enumerate(SAMPLE_ARRIVALS):
        con.execute(
            "INSERT INTO ArrivalLocation (ArrivalDate, CountryName, AdminAreaName, LocalityName, EnCountryName, "
            "EnAdminAreaName, EnLocalityName, CountryCode, AdminAreaCode, Latitude, Longitude) VALUES (?,?,?,?,?,?,?,?,?,?,?)",
            (ticks - n * 864_000_000_000 * 20, country[i], region[i], "", country[0], region[0], "", cc, rc, 35.0 + n, 135.0 + n))
    con.commit()
    con.close()
    with open(path, "rb") as f:
        return f.read()


def sample_prefs(lang):
    return (
        "<?xml version='1.0' encoding='utf-8' standalone='yes' ?>\n<map>\n"
        '    <boolean name="HasSeenOnboarding" value="true" />\n'
        '    <boolean name="HasSeenLongPressHint" value="true" />\n'
        '    <string name="PinStyle">Classic</string>\n'
        f'    <string name="SelectedCountryOrRegion">{COVER[lang]}</string>\n'
        "</map>\n"
    ).encode()


def take(lang, original_db):
    print(f"[{lang}]")
    t = TEXTS[lang]
    adb("shell", "am", "force-stop", PKG)
    run_as_write(DB, sample_database(original_db, lang))
    run_as_write(PREFS, sample_prefs(lang))
    emu("locale", "ja-JP" if lang == "ja" else "")
    emu("launch")
    time.sleep(6)

    # Passport: the cover and the first rows of stamps, below the title bar.
    emu("tab", "passport")
    time.sleep(4)
    save(screen(), (0, 282, 1080, 1560), None, f"howto_passport_{lang}.png")

    # Map: the bottom of the map with "Stamp where I am" framed.
    emu("tab", "map")
    time.sleep(6)
    button = find(t["stamp"])
    save(screen(), (0, max(282, button[1] - 600), 1080, button[3] + 30), button, f"howto_stamp_{lang}.png")

    # Collections: the custom boards, none yet, with the "Add board" card framed.
    emu("tab", "collections")
    time.sleep(3)
    swipe_up(8)
    time.sleep(1.5)
    label = find(t["add_board"])
    plus = find("+", exact=True)
    # The whole card: from above its "+" to below its title, as wide as the title.
    card = (label[0] - 4, plus[1] - 40, label[2] + 4, label[3] + 14)
    heading = find(t["custom_boards"], exact=True)
    tab_bar = find(t["tab"], exact=True)[1] - 60   # stop above the tab bar
    top = max(282, heading[1] - 30)
    save(screen(), (0, top, 1080, min(tab_bar, card[3] + 40)), card, f"howto_boards_{lang}.png")

    # Settings: the Information card at the bottom, with the "How to use" row framed.
    emu("tab", "settings")
    time.sleep(3)
    swipe_up(8)
    time.sleep(1.5)
    heading = find(t["information"], exact=True)
    row = find(t["how_to"], exact=True)
    last = find(t["privacy"], exact=True)
    # The whole row, across the card (the card has the same margins as the screen's 16-dp gutter)
    mark = (64, row[1] - 24, 1080 - 64, row[3] + 24)
    save(screen(), (0, max(282, heading[1] - 30), 1080, last[3] + 70), mark, f"howto_settings_{lang}.png")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--lang", choices=["en", "ja"], action="append")
    langs = parser.parse_args().lang or ["en", "ja"]

    adb("shell", "am", "force-stop", PKG)
    original_db = run_as_read(DB)
    original_prefs = run_as_read(PREFS)
    before = sha1(DB)
    print(f"database {before} saved")
    try:
        for lang in langs:
            take(lang, original_db)
    finally:
        adb("shell", "am", "force-stop", PKG)
        run_as_write(DB, original_db)
        if original_prefs:
            run_as_write(PREFS, original_prefs)
        emu("locale", "")
        after = sha1(DB)
        print(f"database {after} put back" + ("" if after == before else "  <-- DIFFERENT, restore it by hand"))


if __name__ == "__main__":
    main()
