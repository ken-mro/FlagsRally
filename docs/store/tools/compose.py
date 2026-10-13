"""Compose store screenshots: navy backdrop, headline, the raw screenshot in a rounded device frame."""
import os, sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
STORE = os.path.dirname(os.path.abspath(__file__))  # docs/store/tools; raw/ and svg/ are working folders here (not committed)
RAW = f'{STORE}/raw'
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(STORE)

NAVY = (24, 42, 82)
NAVY_DEEP = (15, 28, 58)
GOLD = (233, 196, 106)
PAPER = (248, 242, 228)
STAMP = (185, 64, 71)

FONT_EN_HEAD = f'{ROOT}/Resources/Fonts/JerseyclubGrungeBold-JRXVK.otf'
FONT_EN_SUB = f'{ROOT}/Resources/Fonts/OpenSans-Semibold.ttf'
FONT_JA_HEAD = f'{ROOT}/Resources/Fonts/craftmincho.otf'
FONT_JA_SUB = '/System/Library/Fonts/ヒラギノ角ゴシック W6.ttc'

# (headline, subline) per screen, in the order of the raw files 1..6
COPY = {
    'en': [
        ('Your travels,\nstamped', 'Every arrival becomes a passport stamp'),
        ('Collect regional\nflags', 'States, prefectures and provinces'),
        ('See where\nyou have been', 'Visited countries on a world map'),
        ('Check in on\nthe map', 'Record places within 50 m of you'),
        ('Fill your\ncollections', 'Progress for every country and stamp trail'),
        ('Stamp trails,\nyour way', 'Add trails and gather every stamp'),
    ],
    'ja': [
        ('旅の記録を\nスタンプに', '訪れた場所がパスポートのスタンプになる'),
        ('地域の旗を\n集めよう', '都道府県・州・地方の旗をコレクション'),
        ('訪れた国を\n世界地図で', '行った国がひと目でわかる'),
        ('地図で\nチェックイン', '現在地から50m以内の場所を記録'),
        ('コレクションを\n埋めよう', '国やスタンプラリーごとの達成度を表示'),
        ('スタンプラリーを\nもっと自由に', '好きなスタンプラリーを追加して集める'),
    ],
}

def font(path, size):
    return ImageFont.truetype(path, size)

def fit_lines(draw, text, path, size, max_w):
    """Shrink the font until every line fits the width."""
    while size > 10:
        f = font(path, size)
        if all(draw.textlength(line, font=f) <= max_w for line in text.split('\n')):
            return f
        size -= 2
    return font(path, size)

def backdrop(w, h):
    img = Image.new('RGB', (w, h), NAVY)
    # soft vertical gradient to the deeper navy at the bottom
    grad = Image.linear_gradient('L').resize((w, h))
    img = Image.composite(Image.new('RGB', (w, h), NAVY_DEEP), img, grad)
    d = ImageDraw.Draw(img)
    # faint passport-style guilloche lines
    for i in range(0, w + h, max(w, h) // 28):
        d.line([(i, 0), (i - h, h)], fill=(30, 50, 94), width=max(1, w // 600))
    return img

def rounded(im, radius):
    mask = Image.new('L', im.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, im.width - 1, im.height - 1], radius=radius, fill=255)
    out = im.convert('RGBA')
    out.putalpha(mask)
    return out

def device(shot, target_h, kind):
    """The screenshot inside a dark bezel, scaled to target_h."""
    scale = target_h / shot.height
    sw, sh = round(shot.width * scale), target_h
    screen = shot.convert('RGB').resize((sw, sh), Image.LANCZOS)
    bezel = round(sw * (0.035 if kind != 'tablet' else 0.025))
    r_screen = round(sw * (0.12 if kind == 'phone_ios' else 0.06 if kind == 'phone_android' else 0.035))
    frame = Image.new('RGBA', (sw + 2 * bezel, sh + 2 * bezel), (0, 0, 0, 0))
    fd = ImageDraw.Draw(frame)
    fd.rounded_rectangle([0, 0, frame.width - 1, frame.height - 1], radius=r_screen + bezel, fill=(17, 20, 28, 255))
    fd.rounded_rectangle([2, 2, frame.width - 3, frame.height - 3], radius=r_screen + bezel - 2, outline=(70, 76, 92, 255), width=max(2, bezel // 8))
    frame.alpha_composite(rounded(screen, r_screen), (bezel, bezel))
    return frame

def shadow_of(layer, blur, opacity=0.45):
    a = layer.split()[-1].point(lambda v: int(v * opacity))
    sh = Image.new('RGBA', layer.size, (0, 0, 0, 0))
    sh.putalpha(a)
    pad = blur * 3
    big = Image.new('RGBA', (layer.width + 2 * pad, layer.height + 2 * pad), (0, 0, 0, 0))
    big.alpha_composite(sh, (pad, pad))
    return big.filter(ImageFilter.GaussianBlur(blur)), pad

def compose(raw_path, out_path, size, lang, idx, kind):
    W, H = size
    img = backdrop(W, H).convert('RGBA')
    d = ImageDraw.Draw(img)
    head, sub = COPY[lang][idx]
    margin = round(W * 0.07)
    top = round(H * 0.045)

    head_path = FONT_EN_HEAD if lang == 'en' else FONT_JA_HEAD
    head_size = round(W * (0.105 if kind != 'tablet' else 0.072))
    hf = fit_lines(d, head, head_path, head_size, W - 2 * margin)
    line_h = round(hf.size * (1.08 if lang == 'en' else 1.22))
    y = top
    for line in head.split('\n'):
        tw = d.textlength(line, font=hf)
        d.text(((W - tw) / 2, y), line, font=hf, fill=PAPER)
        y += line_h
    # gold rule between headline and subline
    rule_w = round(W * 0.12)
    y += round(hf.size * 0.12)
    d.rounded_rectangle([(W - rule_w) / 2, y, (W + rule_w) / 2, y + max(4, W // 180)], radius=4, fill=GOLD)
    y += round(hf.size * 0.32)
    sf = fit_lines(d, sub, FONT_EN_SUB if lang == 'en' else FONT_JA_SUB, round(W * (0.042 if kind != 'tablet' else 0.03)), W - 2 * margin)
    tw = d.textlength(sub, font=sf)
    d.text(((W - tw) / 2, y), sub, font=sf, fill=GOLD)
    text_bottom = y + sf.size * 1.4

    shot = Image.open(raw_path)
    # the device runs off the bottom edge a little, like most store listings
    avail = H - text_bottom - round(H * 0.03)
    if kind == 'tablet':
        # a tablet is wide: size it by width and let the bottom run off the canvas
        frame = device(shot, round(W * 0.88 * shot.height / shot.width), kind)
    else:
        frame = device(shot, round(avail * 1.04), kind)
    max_w = W * (0.92 if kind == 'tablet' else 0.86)
    if frame.width > max_w:
        frame = frame.resize((round(max_w), round(frame.height * max_w / frame.width)), Image.LANCZOS)
    x = (W - frame.width) // 2
    fy = round(text_bottom + H * 0.025)
    sh, pad = shadow_of(frame, max(8, W // 60))
    img.alpha_composite(sh, (x - pad, fy - pad + W // 90))
    img.alpha_composite(frame, (x, fy))
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    img.convert('RGB').save(out_path, optimize=True)

# store target sizes
TARGETS = [
    # (raw folder prefix, out folder, size, kind)
    ('ios69', 'ios/iphone_6.9in_1320x2868', (1320, 2868), 'phone_ios'),
    ('ios69', 'ios/iphone_6.5in_1284x2778', (1284, 2778), 'phone_ios'),
    ('ipad13', 'ios/ipad_13in_2064x2752', (2064, 2752), 'tablet'),
    ('android', 'android/phone_1080x1920', (1080, 1920), 'phone_android'),
]
NAMES = ['1_passport', '2_regions', '3_worldmap', '4_map', '5_collections', '6_board']

if __name__ == '__main__':
    for prefix, folder, size, kind in TARGETS:
        for lang in ('en', 'ja'):
            for i, name in enumerate(NAMES):
                src = f'{RAW}/{prefix}_{lang}/{name}.png'
                dst = f'{OUT}/{folder}/{lang}/{i + 1:02d}_{name.split("_", 1)[1]}.png'
                compose(src, dst, size, lang, i, kind)
            print('done', folder, lang)
