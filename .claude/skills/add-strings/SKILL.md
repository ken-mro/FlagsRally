---
name: add-strings
description: Add user-facing text to FlagsRally in English and Japanese (AppResources.resx, AppResources.ja.resx and AppResources.Designer.cs together). Use whenever new UI text, alerts or labels are needed.
---

# Add localized strings

All user-facing text goes through `AppResources`; never hard-code it in XAML or C#.

1. Pick PascalCase keys. Search `Resources/AppResources.resx` first and reuse an existing key when the text already exists (for example `Cancel`, `Error`, `Yes`, `No`, `Confirmation`, `PleaseTryAgain`, `InvalidOrCorruptedFile`).
2. Write a JSON file in your scratchpad: `{"Key": ["English", "日本語"], ...}`.
   - Format placeholders are `{0}`, `{1}` and are filled with `string.Format(AppResources.Key, ...)`.
   - Japanese: natural, polite UI wording (です・ます for sentences, short nouns for buttons), full-width brackets 「」 for quoted names.
3. Run `python tools/add_strings.py <file.json>` from the repository root. Existing keys are skipped; BOM and CRLF are kept.
4. Use it: XAML `Text="{x:Static strings:AppResources.Key}"` (namespace `xmlns:strings="clr-namespace:FlagsRally.Resources"`), C# `AppResources.Key`.
5. Build, and check the text on the emulator in both languages if it is visible (`/android-verify`).

Changing an existing string: edit the `<value>` in both `.resx` files and the summary line in `AppResources.Designer.cs`.
