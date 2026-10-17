"""App Store creative assets (iOS 27+): the product page header (21:9) and the search results image (3:2).

Apple's templates keep the key art inside a centred safe area, because each device crops these differently:
header 3840x1646 -> safe x 1097-2743, y 493-1154; search results 3840x2560 -> safe x 836-3004, y 765-1795.
"""
import os, sys
from PIL import Image, ImageChops, ImageDraw
import compose as C

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(C.STORE)
RAW = C.RAW

HEADER = (3840, 1646)
HEADER_SAFE = (1097, 493, 2743, 1154)
SEARCH = (3840, 2560)
SEARCH_SAFE = (836, 765, 3004, 1795)

COPY = {
    'en': ('Your travels,\nstamped', 'Passport stamps and\nstamp trails on the map'),
    'ja': ('旅の記録を\nスタンプに', 'パスポートのスタンプと\n地図で集めるスタンプラリー'),
}

# Stamp crops from the iPhone captures (1320x2868): passport ARRIVAL stamps and stamp trail stamps.
STAMPS = [
    ('ios69_en/1_passport', (8, 820, 652, 1284)),
    ('ios69_ja/6_board', (60, 564, 612, 1116)),
    ('ios69_en/1_passport', (668, 820, 1312, 1284)),
    ('ios69_ja/6_board', (668, 1320, 1280, 1944)),
    ('ios69_en/1_passport', (8, 1300, 652, 1760)),
    ('ios69_ja/6_board', (692, 564, 1272, 1132)),
    ('ios69_en/1_passport', (668, 1776, 1312, 2236)),
    ('ios69_ja/6_board', (44, 1324, 644, 1944)),
]

def emblem(height):
    """The passport from the App Store icon, cut out of its navy square."""
    im = Image.open(f'{OUT}/ios/app_icon_1024.png').convert('RGB')
    diff = ImageChops.difference(im, Image.new('RGB', im.size, C.NAVY)).convert('L')
    im = im.crop(diff.point(lambda v: 255 if v > 12 else 0).getbbox())
    alpha = ImageChops.difference(im, Image.new('RGB', im.size, C.NAVY)).convert('L').point(lambda v: 0 if v < 6 else min(255, (v - 6) * 8))
    out = im.convert('RGBA')
    out.putalpha(alpha)
    return out.resize((round(out.width * height / out.height), height), Image.LANCZOS)

def stamp(i, width, angle):
    src, box = STAMPS[i % len(STAMPS)]
    tile = Image.open(f'{RAW}/{src}.png').convert('RGB').crop(box)
    tile = tile.resize((width, round(tile.height * width / tile.width)), Image.LANCZOS)
    # a thin paper border, like a stamp stuck on the page
    edge = max(6, width // 40)
    card = Image.new('RGBA', (tile.width + 2 * edge, tile.height + 2 * edge), C.PAPER + (255,))
    card.paste(tile, (edge, edge))
    return card.rotate(angle, resample=Image.BICUBIC, expand=True)

def place(img, layer, x, y, blur):
    sh, pad = C.shadow_of(layer, blur, 0.5)
    img.alpha_composite(sh, (x - pad, y - pad + blur))
    img.alpha_composite(layer, (x, y))

def save(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.convert('RGB').save(path, optimize=True)
    print(path, f'{os.path.getsize(path) / 1048576:.1f} MB')

def header():
    W, H = HEADER
    img = C.backdrop(W, H).convert('RGBA')
    # stamps scattered on both sides, outside the safe area so any crop keeps the emblem whole
    spots = [  # (x, y, width, angle)
        (40, 120, 560, 8), (520, 760, 600, -6), (-120, 1020, 520, -10), (700, 80, 480, -4),
        (3240, 90, 560, -8), (2780, 780, 600, 6), (3420, 1000, 520, 10), (2690, 60, 480, 5),
    ]
    for i, (x, y, w, a) in enumerate(spots):
        place(img, stamp(i, w, a), x, y, 22)
    e = emblem(round((HEADER_SAFE[3] - HEADER_SAFE[1]) * 0.94))
    cx, cy = (HEADER_SAFE[0] + HEADER_SAFE[2]) // 2, (HEADER_SAFE[1] + HEADER_SAFE[3]) // 2
    place(img, e, cx - e.width // 2, cy - e.height // 2, 28)
    save(img, f'{OUT}/ios/creative/header_3840x1646.png')

BRAND = {  # same title and tagline as the Google Play feature graphic (assets.py)
    'en': ('FLAGSRALLY', 'Stamp your travels, collect the world'),
    'ja': ('FlagsRally', '旅をスタンプに。世界を集めよう'),
}

def header_brand(lang):
    """The Google Play feature graphic's layout as a header: the globe with the flag, the name and the tagline,
    all inside the safe area so every crop keeps them; the navy backdrop fills the rest."""
    import assets as A  # loads the rendered SVGs in tools/svg (see README)
    W, H = HEADER
    img = C.backdrop(W, H).convert('RGBA')
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = HEADER_SAFE
    k = (y1 - y0) / 400                       # the feature graphic's globe is 330 px in a 500 px tall canvas
    gh = round(330 * k)
    g = A.globe.resize((round(A.globe.width * gh / A.globe.height), gh), Image.LANCZOS)
    title, tagline = BRAND[lang]
    gap = round(60 * k)
    max_w = x1 - x0 - g.width - gap
    tf = C.fit_lines(d, title, C.FONT_EN_HEAD, round(104 * k), max_w)
    sf = C.fit_lines(d, tagline, C.FONT_EN_SUB if lang == 'en' else C.FONT_JA_SUB, round(40 * k), max_w)
    text_w = max(d.textlength(title, font=tf), d.textlength(tagline, font=sf))
    left = round(x0 + (x1 - x0 - (g.width + gap + text_w)) / 2)   # centre the whole group in the safe area
    gy = (y0 + y1 - gh) // 2 - round(6 * k)
    place(img, g, left, gy, round(14 * k))
    tx = left + g.width + gap
    block = tf.size + round(18 * k) + round(7 * k) + round(30 * k) + sf.size
    ty = (y0 + y1 - block) // 2
    d.text((tx, ty), title, font=tf, fill=C.PAPER)
    ry = ty + tf.size + round(18 * k)
    d.rounded_rectangle([tx, ry, tx + round(110 * k), ry + round(7 * k)], radius=round(4 * k), fill=C.GOLD)
    d.text((tx, ry + round(30 * k)), tagline, font=sf, fill=C.GOLD)
    save(img, f'{OUT}/ios/creative/header_brand_3840x1646_{lang}.png')

def search(lang):
    W, H = SEARCH
    img = C.backdrop(W, H).convert('RGBA')
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = SEARCH_SAFE
    mid = x0 + round((x1 - x0) * 0.47)
    # the headline on the left half of the safe area
    head, sub = COPY[lang]
    head_path = C.FONT_EN_HEAD if lang == 'en' else C.FONT_JA_HEAD
    hf = C.fit_lines(d, head, head_path, 230, mid - x0 - 120)
    sf = C.fit_lines(d, sub, C.FONT_EN_SUB if lang == 'en' else C.FONT_JA_SUB, 96, mid - x0 - 120)
    hl = round(hf.size * (1.08 if lang == 'en' else 1.22))
    sl = round(sf.size * 1.35)
    rule = round(hf.size * 0.5)
    block = hl * len(head.split('\n')) + rule + sl * len(sub.split('\n'))
    y = (y0 + y1 - block) // 2
    for line in head.split('\n'):
        d.text((x0, y), line, font=hf, fill=C.PAPER)
        y += hl
    y += round(hf.size * 0.14)
    d.rounded_rectangle([x0, y, x0 + 300, y + 14], radius=7, fill=C.GOLD)
    y = y - round(hf.size * 0.14) + rule
    for line in sub.split('\n'):
        d.text((x0, y), line, font=sf, fill=C.GOLD)
        y += sl
    # two phones on the right: the passport and a stamp trail; the safe area shows their top halves
    phone_h = 2150
    for name, cx, top, angle in (('1_passport', mid + 680, 470, 0), ('6_board', mid + 1290, 640, 0)):
        frame = C.device(Image.open(f'{RAW}/ios69_{lang}/{name}.png'), phone_h, 'phone_ios')
        if angle:
            frame = frame.rotate(angle, resample=Image.BICUBIC, expand=True)
        place(img, frame, cx - frame.width // 2, top, 36)
    save(img, f'{OUT}/ios/creative/search_results_3840x2560_{lang}.png')

if __name__ == '__main__':
    header()
    for lang in ('en', 'ja'):
        header_brand(lang)
        search(lang)
