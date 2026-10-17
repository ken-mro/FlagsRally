"""Store icons and the Google Play feature graphic."""
import os, sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont
import compose as C

STORE = C.STORE
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(STORE)
NAVY = C.NAVY

def on_navy(path):
    """The SVG rendered on the navy background, cropped to the artwork (kept opaque: it sits on navy)."""
    im = Image.open(path).convert('RGB')
    box = ImageChops.difference(im, Image.new('RGB', im.size, NAVY)).convert('L').point(lambda v: 255 if v > 12 else 0).getbbox()
    pad = 8
    im = im.crop((box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad))
    # alpha from the distance to navy, so the artwork can sit on any backdrop
    alpha = ImageChops.difference(im, Image.new('RGB', im.size, NAVY)).convert('L').point(lambda v: 0 if v < 6 else min(255, (v - 6) * 8))
    out = im.convert('RGBA'); out.putalpha(alpha)
    return out

passport = on_navy(f'{STORE}/svg/appiconfg_navy.svg.png')
globe = on_navy(f'{STORE}/svg/splash_navy.svg.png')

def icon(size, scale):
    """Navy square with the passport; scale = passport height / canvas."""
    img = Image.new('RGBA', (size, size), NAVY + (255,))
    h = round(size * scale)
    p = passport.resize((round(passport.width * h / passport.height), h), Image.LANCZOS)
    img.alpha_composite(p, ((size - p.width) // 2, (size - p.height) // 2))
    return img.convert('RGB')

# Google Play feature graphic: 1024x500, the same for both languages except the text.
COPY = {
    'en': ('FLAGSRALLY', 'Stamp your travels, collect the world'),
    'ja': ('FlagsRally', '旅をスタンプに。世界を集めよう'),
}
if __name__ == '__main__':
    os.makedirs(f'{OUT}/ios', exist_ok=True)
    os.makedirs(f'{OUT}/android', exist_ok=True)
    # App Store: 1024x1024, no transparency, iOS rounds the corners itself. Same 1.4x scale as the app.
    icon(1024, 0.676).save(f'{OUT}/ios/app_icon_1024.png')
    # Google Play: 512x512, Play only rounds the corners (radius 20%), so the passport can fill most of it.
    icon(512, 0.78).save(f'{OUT}/android/app_icon_512.png')

    for lang, (title, tagline) in COPY.items():
        W, H = 1024, 500
        img = C.backdrop(W, H).convert('RGBA')
        d = ImageDraw.Draw(img)
        # emblem on the left
        gh = 330
        g = globe.resize((round(globe.width * gh / globe.height), gh), Image.LANCZOS)
        gx, gy = 70, (H - gh) // 2 - 6
        sh, pad = C.shadow_of(g, 14, 0.35)
        img.alpha_composite(sh, (gx - pad, gy - pad + 8))
        img.alpha_composite(g, (gx, gy))
        # title and tagline on the right
        tx = gx + g.width + 60
        max_w = W - tx - 56
        tf = C.fit_lines(d, title, C.FONT_EN_HEAD, 104, max_w)
        ty = 150
        d.text((tx, ty), title, font=tf, fill=C.PAPER)
        ry = ty + tf.size + 18
        d.rounded_rectangle([tx, ry, tx + 110, ry + 7], radius=4, fill=C.GOLD)
        sf = C.fit_lines(d, tagline, C.FONT_EN_SUB if lang == 'en' else C.FONT_JA_SUB, 40, max_w)
        d.text((tx, ry + 30), tagline, font=sf, fill=C.GOLD)
        img.convert('RGB').save(f'{OUT}/android/feature_graphic_1024x500_{lang}.png')
    print('ok')
