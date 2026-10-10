---
name: release-notes
description: Write the Google Play / App Store release notes for a FlagsRally version in English and Japanese, in the <en-US>/<ja-JP> format the Play Console accepts. Use when the user asks for release notes, "what's new" text or store update text.
---

# Store release notes

The readers are app users, not developers. Say what got better for them, not how it was fixed.

1. Find what changed: the version's section in `CHANGELOG.md`, and `git log --oneline` since the previous version bump (`Bump the Android version to ...`). Skip internal changes (refactors, tests, docs, build, version bumps).
2. Group related fixes into one line each, and keep to about 2–4 lines in total.
3. Write each line from the user's point of view, as an outcome:
   - Good: "The map stays where you left it when you come back to the app."
   - Too detailed: "Starting the app from a notification created a second screen whose map opened at Google's default position."
   - Leave out causes, device conditions, screen-internal names (spot guide, panel, tile height) and examples such as board names.
   - No heading such as "Bug fixes", and no version number.
4. Japanese is natural, polite UI wording (〜ようになりました／〜しました), with each line starting with `・`. English lines start with `- `.
5. Keep each language under 500 characters (the Play Console limit).

Return it in a code block in exactly this form, so it can be pasted into the Play Console:

```
<en-US>
- The map stays where you left it when you come back to the app.
</en-US>
<ja-JP>
・アプリに戻ったときに、地図が前に見ていた場所のままになりました。
</ja-JP>
```

Example (1.1.17, approved by the user):

```
<en-US>
- Custom board tiles now line up neatly.
- The map stays steady and is easier to use while cards are showing.
- The map stays where you left it when you come back to the app.
</en-US>
<ja-JP>
・カスタムボードのタイルがきれいに並ぶようになりました。
・カードを表示しているときも、地図が動かず操作しやすくなりました。
・アプリに戻ったときに、地図が前に見ていた場所のままになりました。
</ja-JP>
```
