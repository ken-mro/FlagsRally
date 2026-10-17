"""App Store creative asset videos (iOS 27+): the product page header (21:9) and the search results video (3:2).

Specs (Creative assets specifications): header 3840x1646, search results 1920x1280 up to 3840x2560, 30 or 60 fps,
5-30 s, .mov/.m4v/.mp4. Both are muted and loop, so each video ends where it starts.

  header_3840x1646.mp4            no text, so one video serves every language: the stamps land one by one around
                                  the passport emblem, stay, then fade so the loop starts again on the emblem
  search_results_1920x1280_<lang>.mp4
                                  the headline on the left, and an iPhone on the right playing the app preview
                                  (ios/preview/iphone_886x1920_<lang>.mp4, a real screen recording)
"""
import os, subprocess, sys
from PIL import Image, ImageDraw
import compose as C
import creative as K

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(C.STORE)
FPS = 30

SILENCE = ['-f', 'lavfi', '-i', 'anullsrc=channel_layout=stereo:sample_rate=48000']

def x264(level, seconds, path):
    """Output options: H.264 High with a silent stereo AAC track (the placements play muted either way)."""
    return ['-c:v', 'libx264', '-profile:v', 'high', '-level:v', level, '-pix_fmt', 'yuv420p', '-preset', 'slow', '-crf', '18',
            '-c:a', 'aac', '-b:a', '128k', '-t', str(seconds), '-movflags', '+faststart', path]

def ease_out_back(t):
    c1, c3 = 1.70158, 2.70158
    return 1 + c3 * (t - 1) ** 3 + c1 * (t - 1) ** 2

def header(seconds=10.0):
    W, H = K.HEADER
    base = C.backdrop(W, H).convert('RGBA')
    e = K.emblem(round((K.HEADER_SAFE[3] - K.HEADER_SAFE[1]) * 0.94))
    cx, cy = (K.HEADER_SAFE[0] + K.HEADER_SAFE[2]) // 2, (K.HEADER_SAFE[1] + K.HEADER_SAFE[3]) // 2
    K.place(base, e, cx - e.width // 2, cy - e.height // 2, 28)
    spots = [  # same layout as the still header, in the order they land
        (40, 120, 560, 8), (2780, 780, 600, 6), (520, 760, 600, -6), (3240, 90, 560, -8),
        (700, 80, 480, -4), (3420, 1000, 520, 10), (-120, 1020, 520, -10), (2690, 60, 480, 5),
    ]
    order = [0, 5, 1, 4, 3, 6, 2, 7]  # which STAMPS crop each spot uses (matches the still header)
    layers = []
    for (x, y, w, a), k in zip(spots, order):
        card = K.stamp(k, w, a)
        sh, pad = C.shadow_of(card, 22, 0.5)
        layers.append((card, sh, pad, x, y))
    land, step, drop = 0.6, 0.55, 0.35        # first stamp lands at 0.6 s, then one every 0.55 s
    fade_from, fade_to = seconds - 1.2, seconds - 0.2
    path = f'{OUT}/ios/creative/header_3840x1646.mp4'
    ff = subprocess.Popen(['ffmpeg', '-v', 'error', '-y', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-s', f'{W}x{H}', '-r', str(FPS),
                           '-i', '-', *SILENCE, '-map', '0:v', '-map', '1:a', *x264('5.1', seconds, path)], stdin=subprocess.PIPE)
    for f in range(round(seconds * FPS)):
        t = f / FPS
        img = base.copy()
        fade = 1.0 if t < fade_from else max(0.0, 1 - (t - fade_from) / (fade_to - fade_from))
        for i, (card, sh, pad, x, y) in enumerate(layers):
            p = (t - (land + i * step)) / drop
            if p <= 0 or fade <= 0:
                continue
            p = min(p, 1.0)
            scale = 1.35 - 0.35 * ease_out_back(p)          # pressed down from above, with a little overshoot
            alpha = min(1.0, p * 2.5) * fade
            cw, ch = round(card.width * scale), round(card.height * scale)
            c = card.resize((cw, ch), Image.BILINEAR) if scale != 1 else card
            ox, oy = x - (cw - card.width) // 2, y - (ch - card.height) // 2
            s = sh if scale == 1 else sh.resize((round(sh.width * scale), round(sh.height * scale)), Image.BILINEAR)
            if alpha < 1:
                c = c.copy(); c.putalpha(c.getchannel('A').point(lambda v: int(v * alpha)))
                s = s.copy(); s.putalpha(s.getchannel('A').point(lambda v: int(v * alpha * min(1.0, p))))
            elif p < 1:
                s = s.copy(); s.putalpha(s.getchannel('A').point(lambda v: int(v * p)))
            img.alpha_composite(s, (ox - round(pad * scale), oy - round(pad * scale) + 22))
            img.alpha_composite(c, (ox, oy))
        ff.stdin.write(img.convert('RGB').tobytes())
    ff.stdin.close()
    ff.wait()
    print(path, f'{os.path.getsize(path) / 1048576:.1f} MB')

def search(lang, seconds=29.5):
    W, H = 1920, 1280
    sx = W / K.SEARCH[0]
    x0, y0, x1, y1 = [round(v * sx) for v in K.SEARCH_SAFE]
    img = C.backdrop(W, H).convert('RGBA')
    d = ImageDraw.Draw(img)
    mid = x0 + round((x1 - x0) * 0.5)
    head, sub = K.COPY[lang]
    hf = C.fit_lines(d, head, C.FONT_EN_HEAD if lang == 'en' else C.FONT_JA_HEAD, 116, mid - x0 - 50)
    sf = C.fit_lines(d, sub, C.FONT_EN_SUB if lang == 'en' else C.FONT_JA_SUB, 48, mid - x0 - 50)
    hl, sl = round(hf.size * (1.08 if lang == 'en' else 1.22)), round(sf.size * 1.35)
    rule = round(hf.size * 0.5)
    y = (y0 + y1 - (hl * len(head.split('\n')) + rule + sl * len(sub.split('\n')))) // 2
    for line in head.split('\n'):
        d.text((x0, y), line, font=hf, fill=C.PAPER); y += hl
    y += round(hf.size * 0.14)
    d.rounded_rectangle([x0, y, x0 + 150, y + 7], radius=4, fill=C.GOLD)
    y = y - round(hf.size * 0.14) + rule
    for line in sub.split('\n'):
        d.text((x0, y), line, font=sf, fill=C.GOLD); y += sl
    # the phone: a bezel with a transparent screen, drawn over the playing preview
    screen_h = 1060
    screen_w = round(screen_h * 886 / 1920) // 2 * 2
    bezel = round(screen_w * 0.035)
    r = round(screen_w * 0.12)
    px = mid + (x1 - mid) // 2 - screen_w // 2 + 40
    py = y0 - 40
    frame = Image.new('RGBA', (screen_w + 2 * bezel, screen_h + 2 * bezel), (0, 0, 0, 0))
    fd = ImageDraw.Draw(frame)
    fd.rounded_rectangle([0, 0, frame.width - 1, frame.height - 1], radius=r + bezel, fill=(17, 20, 28, 255))
    fd.rounded_rectangle([2, 2, frame.width - 3, frame.height - 3], radius=r + bezel - 2, outline=(70, 76, 92, 255), width=3)
    hole = Image.new('L', frame.size, 0)
    ImageDraw.Draw(hole).rounded_rectangle([bezel, bezel, bezel + screen_w - 1, bezel + screen_h - 1], radius=r, fill=255)
    frame.putalpha(Image.composite(Image.new('L', frame.size, 0), frame.getchannel('A'), hole))  # cut out the screen
    sh, pad = C.shadow_of(Image.new('RGBA', frame.size, (0, 0, 0, 255)), 18, 0.5)
    img.alpha_composite(sh, (px - bezel - pad, py - bezel - pad + 10))
    bg = f'{OUT}/ios/creative/.search_bg_{lang}.png'
    over = f'{OUT}/ios/creative/.search_frame_{lang}.png'
    img.convert('RGB').save(bg)
    full = Image.new('RGBA', (W, H), (0, 0, 0, 0)); full.alpha_composite(frame, (px - bezel, py - bezel)); full.save(over)
    path = f'{OUT}/ios/creative/search_results_1920x1280_{lang}.mp4'
    src = f'{OUT}/ios/preview/iphone_886x1920_{lang}.mp4'
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-loop', '1', '-r', str(FPS), '-i', bg, '-i', src, '-loop', '1', '-r', str(FPS), '-i', over,
                    *SILENCE, '-filter_complex',
                    f'[1:v]scale={screen_w}:{screen_h}:flags=lanczos,fps={FPS}[s];[0:v][s]overlay={px}:{py}[a];[a][2:v]overlay=0:0,format=yuv420p[v]',
                    '-map', '[v]', '-map', '3:a', *x264('4.2', seconds, path)], check=True)
    os.remove(bg); os.remove(over)
    print(path, f'{os.path.getsize(path) / 1048576:.1f} MB')

if __name__ == '__main__':
    header()
    for lang in ('en', 'ja'):
        search(lang)
