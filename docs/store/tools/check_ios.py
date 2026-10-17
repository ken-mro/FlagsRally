"""Check the App Store files against App Store Connect's rules: screenshot sizes and alpha, and the listing text limits."""
import json, os, subprocess, sys
from PIL import Image

STORE = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IOS = f'{STORE}/ios'
META = f'{IOS}/metadata'

# Accepted portrait sizes per screenshot slot (App Store Connect "Screenshot specifications").
SIZES = {
    'iphone_6.9in_1320x2868': [(1320, 2868), (1290, 2796), (1260, 2736)],
    'iphone_6.5in_1284x2778': [(1284, 2778), (1242, 2688)],
    'iphone_6.3in_1206x2622': [(1206, 2622), (1179, 2556)],
    'iphone_duo_outer_1398x2034': [(1398, 2034)],
    'iphone_duo_inner_2007x2853': [(2007, 2853)],
    'ipad_13in_2064x2752': [(2064, 2752), (2048, 2732)],
}
# (file, limit, unit). Keywords and review notes are counted in UTF-8 bytes, the rest in characters.
LIMITS = [
    ('name.txt', 30, 'chars'), ('subtitle.txt', 30, 'chars'), ('promotional_text.txt', 170, 'chars'),
    ('description.txt', 4000, 'chars'), ('release_notes.txt', 4000, 'chars'), ('keywords.txt', 100, 'bytes'),
]
REQUIRED = ['name.txt', 'description.txt', 'keywords.txt', 'support_url.txt', 'privacy_url.txt']

# Apple publishes no per-image file size limit; 8 MB keeps well under the 10 MB most guides report.
MAX_IMAGE_BYTES = 8 * 1024 * 1024
# App previews: 15-30 s, H.264 at most 30 fps, 500 MB at most (App preview specifications).
PREVIEWS = {'iphone_886x1920': (886, 1920), 'ipad_1200x1600': (1200, 1600)}
MAX_PREVIEW_BYTES = 500 * 1000 * 1000

errors = []

def read(path):
    with open(path, encoding='utf-8') as f:
        return f.read().strip()

def check_image(path, sizes):
    if os.path.getsize(path) > MAX_IMAGE_BYTES:
        errors.append(f'{path}: {os.path.getsize(path) / 1048576:.1f} MB (keep under 8 MB)')
    with Image.open(path) as im:
        if sizes and im.size not in sizes:
            errors.append(f'{path}: {im.size[0]}x{im.size[1]} is not one of {sizes}')
        if im.mode in ('RGBA', 'LA', 'PA') or 'transparency' in im.info:
            errors.append(f'{path}: has an alpha channel')
        if im.format not in ('PNG', 'JPEG'):
            errors.append(f'{path}: {im.format} (PNG or JPEG only)')

for root in (IOS, f'{IOS}/plain'):
    for slot, sizes in SIZES.items():
        if not os.path.isdir(f'{root}/{slot}'):
            errors.append(f"{root}/{slot}: missing"); continue
        for lang in ('en', 'ja'):
            folder = f'{root}/{slot}/{lang}'
            shots = sorted(n for n in os.listdir(folder) if n.endswith(('.png', '.jpg', '.jpeg')))
            if not 1 <= len(shots) <= 10:
                errors.append(f'{folder}: {len(shots)} screenshots (1-10 allowed)')
            for n in shots:
                check_image(f'{folder}/{n}', sizes)
check_image(f'{IOS}/app_icon_1024.png', [(1024, 1024)])

# Product page header (21:9) and search results (3:2, 1920x1280 up to 3840x2560), iOS 27 and later.
check_image(f'{IOS}/creative/header_3840x1646.png', [(3840, 1646)])
for lang in ('en', 'ja'):
    check_image(f'{IOS}/creative/header_brand_3840x1646_{lang}.png', [(3840, 1646)])
for lang in ('en', 'ja'):
    path = f'{IOS}/creative/search_results_3840x2560_{lang}.png'
    check_image(path, None)
    with Image.open(path) as im:
        w, h = im.size
        if w * 2 != h * 3 or not 1920 <= w <= 3840:
            errors.append(f'{path}: {w}x{h} is not 3:2 between 1920x1280 and 3840x2560')

def probe(path):
    out = subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'stream=codec_type,codec_name,width,height,avg_frame_rate,channels',
                          '-show_entries', 'format=duration', '-of', 'json', path], capture_output=True, text=True, check=True)
    return json.loads(out.stdout)

for slot, size in PREVIEWS.items():
    for lang in ('en', 'ja'):
        path = f'{IOS}/preview/{slot}_{lang}.mp4'
        if not os.path.exists(path):
            errors.append(f'{path}: missing')
            continue
        info = probe(path)
        video = next(s for s in info['streams'] if s['codec_type'] == 'video')
        audio = [s for s in info['streams'] if s['codec_type'] == 'audio']
        num, den = map(int, video['avg_frame_rate'].split('/'))
        duration = float(info['format']['duration'])
        if (video['width'], video['height']) != size:
            errors.append(f"{path}: {video['width']}x{video['height']}, expected {size[0]}x{size[1]}")
        if video['codec_name'] != 'h264' or num / den > 30.01:
            errors.append(f"{path}: {video['codec_name']} at {num / den:.2f} fps (H.264, at most 30 fps)")
        if not audio or audio[0]['codec_name'] != 'aac' or audio[0].get('channels') != 2:
            errors.append(f'{path}: needs a stereo AAC audio track')
        if not 15 <= duration <= 30:
            errors.append(f'{path}: {duration:.1f} s (15-30 s)')
        if os.path.getsize(path) > MAX_PREVIEW_BYTES:
            errors.append(f'{path}: over 500 MB')

# Header and search results videos: 30 or 60 fps, 5-30 s (Creative assets specifications).
for name, size in [('header_3840x1646.mp4', (3840, 1646))] + [(f'search_results_1920x1280_{l}.mp4', None) for l in ('en', 'ja')]:
    path = f'{IOS}/creative/{name}'
    if not os.path.exists(path):
        errors.append(f'{path}: missing')
        continue
    info = probe(path)
    video = next(s for s in info['streams'] if s['codec_type'] == 'video')
    w, h = video['width'], video['height']
    num, den = map(int, video['avg_frame_rate'].split('/'))
    duration = float(info['format']['duration'])
    if size and (w, h) != size:
        errors.append(f'{path}: {w}x{h}, expected {size[0]}x{size[1]}')
    if not size and (w * 2 != h * 3 or not 1920 <= w <= 3840):
        errors.append(f'{path}: {w}x{h} is not 3:2 between 1920x1280 and 3840x2560')
    if round(num / den) not in (30, 60):
        errors.append(f'{path}: {num / den:.2f} fps (30 or 60)')
    if not 5 <= duration <= 30:
        errors.append(f'{path}: {duration:.1f} s (5-30 s)')

for lang in ('en-US', 'ja'):
    for name in REQUIRED:
        path = f'{META}/{lang}/{name}'
        if not os.path.exists(path) or not read(path):
            errors.append(f'{path}: missing or empty')
    for name, limit, unit in LIMITS:
        path = f'{META}/{lang}/{name}'
        if not os.path.exists(path):
            continue
        text = read(path)
        size = len(text.encode('utf-8')) if unit == 'bytes' else len(text)
        if size > limit:
            errors.append(f'{path}: {size} {unit} (limit {limit})')
    keywords = read(f'{META}/{lang}/keywords.txt').split(',')
    short = [k for k in keywords if len(k.strip()) <= 2]
    if short:
        errors.append(f'{META}/{lang}/keywords.txt: each keyword must be longer than two characters: {short}')
notes = f'{META}/review_information/notes.txt'
if len(read(notes).encode('utf-8')) > 4000:
    errors.append(f'{notes}: over 4000 bytes')

print('\n'.join(errors) if errors else 'All App Store files pass.')
sys.exit(1 if errors else 0)
