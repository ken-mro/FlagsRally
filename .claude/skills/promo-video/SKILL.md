---
name: promo-video
description: Make or remake FlagsRally's narrated promotional video (16:9, English and Japanese, burned-in subtitles plus .srt) from the store copy and the app preview recordings, with macOS system voices (including Siri voices such as "Voice 2"). Use whenever the user wants a promo, trailer, YouTube or Google Play promo video, wants the narration voice, script, subtitles or scenes of the video changed, or asks for the video again ("プロモーションビデオを作って", "字幕・音声付きの動画", "声を変えて作り直して", "ナレーションが機械的", "promo video", "regenerate the video").
---

# Promotional video

The generator is `docs/store/tools/promo_video.py`; it writes `docs/store/promo/promo_1920x1080_{en,ja}.mp4` and `.srt`. `docs/store/README.md` ("Promotional video") documents it. Everything in the script is data at the top: `VOICE`, `SCENES` (kind, footage range, feature label, title, narration lines), `CARD` (the line under the name on the opening and closing cards) and `SPOKEN` (how a line is read when it differs from the subtitle).

The video is 16:9 for YouTube and the Google Play promo slot (a YouTube link). It is not an App Store app preview: those must be mostly real screen captures in portrait (`ios/preview/`, `/ios-release` does not touch them).

`<scratch>` below means Claude's scratchpad directory.

## 1. Settle what only the user knows

- **Voices: ask, never infer.** No preference file records the voice picked in System Settings (`com.apple.voiceservices` lists *downloaded* assets, not the selection). Ask only about the languages the user is changing; keep the others as they are in `VOICE` without asking again.
- List the candidates with `swift docs/store/tools/speak.swift --list en-US` (or `ja`). Quality `2` is Enhanced/Premium. How voices appear:
  - Siri voices usually show as **"Voice N"**. The identifier may hold a real name (`Voice 2` = `…gryphon-neural_Sakura_ja-JP_premium`, `Voice 4` = `…siri.natural.Nora`) or not (`Voice 6` = `…siri.natural.en-US-G`). Some Siri voices are listed by name instead (`Aaron (Enhanced)` = `…siri_Aaron_en-US_premium`).
  - Settings may label the same voice differently ("Voice 4", "Siri Voice 4", "American (Voice 4)"). Match on the number or the name.
  - One voice can have two entries (Sakura: `gryphon` and `gryphon-neural`). Use the `-neural` one and say so; offer the other if the user finds it different from Settings.
- **The answer must match exactly one entry.** If it fits several (e.g. "Siri" when Voice 4, Voice 6 and Aaron are installed), ask with AskUserQuestion, listing each candidate as name + identifier. If it fits none, the voice is not downloaded: tell the user where (System Settings → Accessibility → Spoken Content → System voice → Manage Voices). Never substitute a voice silently.
- The voice is named in four places; change all of them: `VOICE`, the comment above it, the module docstring in `promo_video.py`, and the voice sentence in the README section. If no Siri voice is left, also reword the README's reason for `speak.swift` (`say` would then work, but `speak.swift` still does).
- Script changes: edit only the language the user asked about, then offer the same change in the other language. When the opening or closing line changes, update its `CARD` text to match.
- Wording: the feature names match the store images (`FEATURES` in `docs/store/tools/creative.py`): Flag stamps / Stamp trail / Travel history, 国旗のスタンプ / スタンプラリー / 旅の履歴. Change both together.
- Background music: none unless the user supplies a track they may use.

## 2. Footage ranges

Each feature scene plays a range of `docs/store/ios/preview/iphone_886x1920_<lang>.mp4` (a real recording, 29.5 s; both languages share the timing). Current ranges:

| Scene | Range (s) | Shows |
|---|---|---|
| Flag stamps | 0.0–9.0 | passport, then regions |
| Stamp trail | 21.6–29.5 | Collections, then the 全国神宮二十五社巡り board |
| Travel history | 10.6–21.3 | world map, then the Map tab with 平安神宮 |

Cuts are not on whole seconds (Map → Collections at ~21.5 s, world map drawn from ~10.6 s). After the previews are re-recorded, find the cuts again before rendering:

```bash
ffmpeg -v error -y -ss 8 -t 6 -i docs/store/ios/preview/iphone_886x1920_en.mp4 -vf "fps=4,scale=111:240,tile=24x1" -frames:v 1 <scratch>/cuts.png
```

A scene longer than its range holds the last frame; a shorter one plays it up to 1.6x faster.

## 3. Render

```bash
python3 docs/store/tools/promo_video.py        # both; or: en / ja  (about 30 s each)
```

Render the languages whose voice, script or scenes changed. A change to `SCENES` footage ranges or layout affects both.

Narration goes through `speak.swift` under the `swift` **interpreter** on purpose:
- `say -v` cannot use Siri voices, and for an unknown or unusable voice it exits 0 and writes a ~0.01 s silent file.
- A compiled Swift binary does not see Siri voices in `AVSpeechSynthesisVoice.speechVoices()`; the interpreter does.

Subtitles are drawn with PIL because Homebrew's ffmpeg here has no `drawtext`/`subtitles` filter. "Broken pipe" lines from ffmpeg are the footage decoders being stopped early; harmless.

## 4. Check before reporting

Run every check for each language rendered. Claude cannot hear the audio, so check what can be measured and say plainly that the user must listen.

```bash
cd docs/store/promo
grep -- "-->" promo_1920x1080_en.srt                                                   # cue times
ffmpeg -i promo_1920x1080_en.mp4 -af volumedetect -f null - 2>&1 | grep -E "mean_volume|max_volume"
ffmpeg -i promo_1920x1080_en.mp4 -af silencedetect=n=-45dB:d=0.6 -f null - 2>&1 | grep -oE "silence_(start|end): [0-9.]+" | paste - -
```

- `speak.swift` fails (non-zero exit, so the script stops) when a voice is missing or produces no audio, so a finished render has sound on every line. Still compare each cue with its sentence: a cue far shorter than reading the line would take means something went wrong.
- Silences fall only at scene changes, matching the gaps between cues.
- A different voice changes the pace, so each scene's length changes. A scene shorter than its footage range ÷ 1.6 cuts the footage short (the last part of the range is not shown); a longer one holds the last frame. The frame check below shows whether that still works.
- Grab one frame 1.5 s into each cue and look at all of them:

```bash
i=0; for t in $(grep -- "-->" promo_1920x1080_ja.srt | awk '{split($1,a,/[:,]/); print a[2]*60+a[3]+1.5}'); do
  ffmpeg -v error -y -ss $t -i promo_1920x1080_ja.mp4 -frames:v 1 -vf scale=640:360 <scratch>/cue_$i.png; i=$((i+1)); done
```

  Look for: the phone shows the screen the narration talks about (not a loading map or the previous tab), the subtitle bar does not cover the phone, Japanese lines break at 、 and not inside a word, nothing is cut off.

## 5. Report

List the files with length and voice per language, what was measured, and that the user should listen. In the README section, re-measure and replace the lengths of the languages rendered (`ffprobe -v error -show_entries format=duration -of csv=p=0 <file>`), and update the voices and scenes if they changed. Commit only when asked (paths listed, never `git add -A`); the videos are about 5 MB each.

## Common mistakes

| Mistake | Instead |
|---|---|
| Reporting a voice read from `com.apple.voiceservices` as "your setting" | Ask for the names; map them with `--list` |
| Picking one of several voices that fit "Siri" | Ask, listing the candidates |
| Trusting `say` exit code | Check durations; use `speak.swift` |
| Compiling `speak.swift` to speed it up | Run it with `swift`; compiled builds miss Siri voices |
| Clip ranges from a 1 fps contact sheet | Check the cuts at 4 fps around them |
| Saying the narration "sounds natural" | Claude cannot hear it; report measurements and ask the user to listen |
