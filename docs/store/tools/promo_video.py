"""Promotional video (1920x1080, 16:9) with narration and burned-in subtitles, in English and Japanese.

For YouTube and the Google Play promo video slot. The scenes follow the store description: the brand, then
flag stamps, stamp trails and travel history, each with the real screen recording from the app preview
(ios/preview/iphone_886x1920_<lang>.mp4) playing in a phone. The narration is a macOS system voice (Joelle / Sakura,
through speak.swift);
the subtitles are drawn into the frames and also written as an .srt next to the video.

usage: python3 promo_video.py [lang ...]     (default: en ja)
"""
import os, subprocess, sys, tempfile
from PIL import Image, ImageDraw
import compose as C
import creative as K

OUT = f'{os.path.dirname(C.STORE)}/promo'
W, H, FPS = 1920, 1080, 30
# Joelle (Enhanced) and Sakura ("Voice 2", Siri, Premium): download them in System Settings > Accessibility > Spoken Content > System voice > Manage Voices
VOICE = {'en': 'com.apple.voice.enhanced.en-US.Joelle', 'ja': 'com.apple.ttsbundle.gryphon-neural_Sakura_ja-JP_premium'}

# (kind, footage (start, end) in the app preview or None, feature label, title, narration lines)
SCENES = {
    'en': [
        ('brand', None, None, None,
         ['Your passport to global adventures.', 'This is FlagsRally.']),
        ('feature', (0.0, 9.0), 'Flag stamps', 'Your travels,\nstamped',
         ['Wherever you travel, FlagsRally finds where you are and gives you that country\'s flag stamp, just like a passport.',
          'In supported countries, collect the flags of states and regions too, and track your progress.']),
        ('feature', (21.6, 29.5), 'Stamp trail', 'Stamp trails,\nyour way',
         ['Join stamp trails at shrines, temples and more.', 'You can even create your own.']),
        ('feature', (10.6, 21.3), 'Travel history', 'Every stamp\non the map',
         ['And look back on every stamp you have collected, right on the map.']),
        ('outro', None, None, None,
         ['Safe travels, and happy collecting!']),
    ],
    'ja': [
        ('brand', None, None, None,
         ['世界を旅する、あなたのパスポート。', 'フラッグラリー。']),
        ('feature', (0.0, 9.0), '国旗のスタンプ', '旅の記録を\nスタンプに',
         ['旅先では、位置情報から国を特定して、パスポートのように国旗のスタンプを手に入れられます。',
          '対応する国では、都道府県や州の旗のスタンプも集めて、進み具合を確かめられます。']),
        ('feature', (21.6, 29.5), 'スタンプラリー', 'スタンプラリーを\nもっと自由に',
         ['神社やお寺をめぐるスタンプラリーも楽しめます。', '自分でつくることもできます。']),
        ('feature', (10.6, 21.3), '旅の履歴', '集めたスタンプを\n地図で',
         ['集めたスタンプの場所は、地図でいつでも振り返れます。']),
        ('outro', None, None, None,
         ['良い旅を。そして、たくさんのスタンプを！']),
    ],
}
# the text on the opening and closing cards (under the name), next to the narration they go with
CARD = {
    'en': {'brand': 'Your passport to global adventures', 'outro': 'Safe travels and happy collecting!'},
    'ja': {'brand': '世界を旅するあなたのパスポート', 'outro': '良い旅を。たくさんのスタンプを！'},
}
# what the voice should say where the subtitle reads differently
SPOKEN = {'フラッグラリー。': 'フラッグラリー。', 'This is FlagsRally.': 'This is Flags Rally.'}

LEAD, GAP, TAIL = 0.5, 0.35, 0.9   # silence before the first line, between lines, after the last line of a scene
FADE = 0.35                        # fade through navy between scenes
MAX_SPEED = 1.6                    # the footage may play this much faster to fit its scene

def tts(text, lang, path):
    subprocess.run(['swift', f'{C.STORE}/speak.swift', VOICE[lang], path, SPOKEN.get(text, text)], check=True)
    return float(subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', path],
                                capture_output=True, text=True, check=True).stdout)

def font_for(lang, role):
    if role == 'head':
        return C.FONT_EN_HEAD if lang == 'en' else C.FONT_JA_HEAD
    return C.FONT_EN_SUB if lang == 'en' else C.FONT_JA_SUB

def wrap(d, text, f, max_w, lang):
    """Greedy wrap by words (en) or characters (ja), keeping Japanese punctuation off the start of a line."""
    units = text.split(' ') if lang == 'en' else list(text)
    sep = ' ' if lang == 'en' else ''
    lines, cur = [], ''
    for u in units:
        trial = cur + sep + u if cur else u
        if d.textlength(trial, font=f) <= max_w or not cur or (lang == 'ja' and u in '、。！？」'):
            cur = trial
        else:
            lines.append(cur)
            cur = u
    return lines + [cur]

def split_lines(d, text, f, max_w, lang):
    """One line if it fits; else two, broken at the comma nearest the middle when both halves fit."""
    if d.textlength(text, font=f) <= max_w:
        return [text]
    marks = [i + 1 for i, ch in enumerate(text) if ch in ('、' if lang == 'ja' else ',')]
    for i in sorted(marks, key=lambda i: abs(i - len(text) / 2)):
        a, b = text[:i].strip(), text[i:].strip()
        if d.textlength(a, font=f) <= max_w and d.textlength(b, font=f) <= max_w:
            return [a, b]
    return wrap(d, text, f, max_w, lang)

def subtitle(img, text, lang):
    d = ImageDraw.Draw(img)
    f = C.font(font_for(lang, 'sub'), 46)
    lines = split_lines(d, text, f, 1600, lang)
    lh = round(f.size * 1.4)
    pad = 22
    bw = max(d.textlength(l, font=f) for l in lines) + 2 * pad * 2
    bh = lh * len(lines) + 2 * pad
    x0, y1 = (W - bw) / 2, H - 30
    box = Image.new('RGBA', img.size, (0, 0, 0, 0))
    ImageDraw.Draw(box).rounded_rectangle([x0, y1 - bh, x0 + bw, y1], radius=18, fill=(8, 14, 30, 200))
    img.alpha_composite(box)
    y = y1 - bh + pad
    for l in lines:
        d.text((W / 2, y + lh / 2), l, font=f, fill=(255, 255, 255), anchor='mm')
        y += lh

def phone_parts(target_h):
    """The bezel and the screen mask, made once; the screen is pasted into them every frame."""
    blank = Image.new('RGB', (886, 1920), (0, 0, 0))
    frame = C.device(blank, target_h, 'phone_ios')
    sw = round(886 * target_h / 1920)
    bezel = round(sw * 0.035)
    mask = C.rounded(Image.new('RGB', (sw, target_h)), round(sw * 0.12)).split()[-1]
    sh, pad = C.shadow_of(frame, 26, 0.55)
    return frame, mask, (sw, target_h), bezel, sh, pad

def footage(path, start, end, seconds, size):
    """Frames of the app preview between start and end, sped up (up to MAX_SPEED) or held on the last frame to last `seconds`."""
    speed = min(MAX_SPEED, max(1.0, (end - start) / seconds))
    w, h = size
    p = subprocess.Popen(['ffmpeg', '-v', 'quiet', '-ss', str(start), '-t', str(end - start), '-i', path,
                          '-vf', f'setpts=PTS/{speed},fps={FPS},scale={w}:{h}:flags=lanczos', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'],
                         stdout=subprocess.PIPE)
    last = None
    for _ in range(round(seconds * FPS)):
        buf = p.stdout.read(w * h * 3)
        if len(buf) == w * h * 3:
            last = Image.frombytes('RGB', (w, h), buf)
        yield last
    p.kill()  # the rest of the clip is not needed
    p.wait()

def brand_still(lang, outro):
    import assets as A  # the rendered SVGs in tools/svg (see README)
    img = C.backdrop(W, H).convert('RGBA')
    d = ImageDraw.Draw(img)
    gh = 330
    g = A.globe.resize((round(A.globe.width * gh / A.globe.height), gh), Image.LANCZOS)
    title = 'FLAGSRALLY' if lang == 'en' else 'FlagsRally'
    tagline = CARD[lang]['outro' if outro else 'brand']
    tf = C.fit_lines(d, title, C.FONT_EN_HEAD, 120, 900)
    sf = C.fit_lines(d, tagline, font_for(lang, 'sub'), 46, 900)
    text_w = max(d.textlength(title, font=tf), d.textlength(tagline, font=sf))
    gap = 60
    left = round((W - (g.width + gap + text_w)) / 2)
    cy = 470
    K.place(img, g, left, cy - gh // 2, 14)
    tx = left + g.width + gap
    d.text((tx, cy - 40), title, font=tf, fill=C.PAPER, anchor='ls')
    d.rounded_rectangle([tx, cy - 10, tx + 130, cy], radius=5, fill=C.GOLD)
    d.text((tx, cy + 30), tagline, font=sf, fill=C.GOLD, anchor='lt')
    if outro:
        stores = 'App Store  ·  Google Play'
        f = C.font(font_for('en', 'sub'), 40)
        d.text((W / 2, cy + 230), stores, font=f, fill=C.PAPER, anchor='mm')
    return img

def feature_base(label, title, lang):
    img = C.backdrop(W, H).convert('RGBA')
    d = ImageDraw.Draw(img)
    x = 170
    c = K.chip(label, font_for(lang, 'sub'), 44, C.NAVY, C.GOLD + (255,))
    img.alpha_composite(c, (x, 230))
    hf = C.fit_lines(d, title, font_for(lang, 'head'), 112, 900)
    y = 230 + c.height + 50
    lh = round(hf.size * (1.1 if lang == 'en' else 1.25))
    for line in title.split('\n'):
        d.text((x, y), line, font=hf, fill=C.PAPER)
        y += lh
    return img

def build(lang):
    os.makedirs(OUT, exist_ok=True)
    tmp = tempfile.mkdtemp(prefix=f'promo_{lang}_')
    # 1. narration: one file per line, laid out on the timeline
    timeline, cues, t = [], [], 0.0
    n = 0
    for kind, clip, label, title, lines in SCENES[lang]:
        start = t
        t += LEAD
        for line in lines:
            path = f'{tmp}/{n:02d}.caf'
            dur = tts(line, lang, path)
            cues.append((t, t + dur, line, path))
            t += dur + GAP
            n += 1
        t += TAIL - GAP
        timeline.append((kind, clip, label, title, start, t))
    total = t

    # 2. audio: every line delayed to its place and mixed into one track
    audio = f'{tmp}/narration.wav'
    inputs, chains = [], []
    for i, (s, _, _, path) in enumerate(cues):
        inputs += ['-i', path]
        ms = round(s * 1000)
        chains.append(f'[{i}:a]aresample=48000,adelay={ms}|{ms}[a{i}]')
    mix = ''.join(f'[a{i}]' for i in range(len(cues)))
    subprocess.run(['ffmpeg', '-v', 'error', '-y', *inputs, '-filter_complex',
                    ';'.join(chains) + f';{mix}amix=inputs={len(cues)}:normalize=0,apad,atrim=0:{total},'
                    'aformat=sample_rates=48000:channel_layouts=stereo[out]', '-map', '[out]', audio], check=True)

    # 3. video: draw every frame, mux with the narration
    preview = f'{os.path.dirname(C.STORE)}/ios/preview/iphone_886x1920_{lang}.mp4'
    frame, mask, ssize, bezel, shadow, spad = phone_parts(780)
    px, py = 1330 - frame.width // 2, 50
    navy = C.backdrop(W, H).convert('RGBA')
    path = f'{OUT}/promo_1920x1080_{lang}.mp4'
    ff = subprocess.Popen(['ffmpeg', '-v', 'error', '-y', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-s', f'{W}x{H}', '-r', str(FPS),
                           '-i', '-', '-i', audio, '-map', '0:v', '-map', '1:a',
                           '-c:v', 'libx264', '-profile:v', 'high', '-pix_fmt', 'yuv420p', '-preset', 'slow', '-crf', '18',
                           '-c:a', 'aac', '-b:a', '192k', '-t', f'{total:.3f}', '-movflags', '+faststart', path], stdin=subprocess.PIPE)
    fi = 0
    for kind, clip, label, title, s, e in timeline:
        frames = round(e * FPS) - fi
        if kind == 'feature':
            base = feature_base(label, title, lang)
            base.alpha_composite(shadow, (px - spad, py - spad + 26))
            shots = footage(preview, clip[0], clip[1], frames / FPS, ssize)
        else:
            base = brand_still(lang, kind == 'outro')
            shots = iter([None] * frames)
        for k in range(frames):
            t = fi / FPS
            img = base.copy()
            shot = next(shots)
            if shot is not None:
                ph = frame.copy()
                ph.paste(shot, (bezel, bezel), mask)
                img.alpha_composite(ph, (px, py))
            for cs, ce, line, _ in cues:
                if cs - 0.15 <= t < ce + 0.25:
                    subtitle(img, line, lang)
            # fade through navy at scene edges (not at the very start, so the poster frame is the brand)
            edge = min(t - s if s > 0 else FADE, e - t if e < total else FADE)
            if edge < FADE:
                img = Image.blend(navy, img, max(0.0, edge / FADE))
            ff.stdin.write(img.convert('RGB').tobytes())
            fi += 1
    ff.stdin.close()
    ff.wait()

    # 4. the same subtitles as an .srt (for YouTube captions)
    def ts(x):
        ms = round(x * 1000)
        return f'{ms // 3600000:02d}:{ms // 60000 % 60:02d}:{ms // 1000 % 60:02d},{ms % 1000:03d}'
    with open(f'{OUT}/promo_1920x1080_{lang}.srt', 'w', encoding='utf-8') as f:
        for i, (cs, ce, line, _) in enumerate(cues, 1):
            f.write(f'{i}\n{ts(max(0, cs - 0.15))} --> {ts(ce + 0.25)}\n{line}\n\n')
    print(path, f'{total:.1f} s', f'{os.path.getsize(path) / 1048576:.1f} MB')

if __name__ == '__main__':
    for lang in sys.argv[1:] or ['en', 'ja']:
        build(lang)
