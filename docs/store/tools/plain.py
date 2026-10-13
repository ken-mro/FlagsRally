"""Screenshot-only versions: the raw captures, fitted to each store's size rules without any added design."""
import os, sys
from PIL import Image

TOOLS = os.path.dirname(os.path.abspath(__file__))
RAW = f'{TOOLS}/raw'
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(TOOLS)
NAMES = ['1_passport', '2_regions', '3_worldmap', '4_map', '5_collections', '6_board']

def iphone_65(im):
    # 1320x2868 -> 1284x2778: scale to the width, then trim the few extra rows top and bottom.
    h = round(im.height * 1284 / im.width)
    im = im.resize((1284, h), Image.LANCZOS)
    top = (h - 2778) // 2
    return im.crop((0, top, 1284, top + 2778))

def android_phone(im):
    # Google Play allows at most 2:1. Drop the system status bar (rows 0-129) and the gesture
    # handle (rows 2350+), then widen 1080 -> 1110 by repeating the edge columns.
    im = im.crop((0, 130, 1080, 2350))
    out = Image.new('RGB', (1110, 2220))
    out.paste(im, (15, 0))
    out.paste(im.crop((0, 0, 1, 2220)).resize((15, 2220)), (0, 0))
    out.paste(im.crop((1079, 0, 1080, 2220)).resize((15, 2220)), (1095, 0))
    return out

TARGETS = [
    ('ios69', 'ios/plain/iphone_6.9in_1320x2868', lambda im: im),
    ('ios69', 'ios/plain/iphone_6.5in_1284x2778', iphone_65),
    ('ipad13', 'ios/plain/ipad_13in_2064x2752', lambda im: im),
    ('android', 'android/plain/phone_1110x2220', android_phone),
]

if __name__ == '__main__':
    for prefix, folder, fit in TARGETS:
        for lang in ('en', 'ja'):
            os.makedirs(f'{OUT}/{folder}/{lang}', exist_ok=True)
            for i, name in enumerate(NAMES):
                im = Image.open(f'{RAW}/{prefix}_{lang}/{name}.png').convert('RGB')
                fit(im).save(f'{OUT}/{folder}/{lang}/{i + 1:02d}_{name.split("_", 1)[1]}.png', optimize=True)
            print('done', folder, lang)
