"""Add localized strings to Resources/AppResources.resx, AppResources.ja.resx and AppResources.Designer.cs.

Usage (from the repository root):
    python tools/add_strings.py strings.json
where strings.json is {"Key": ["English text", "日本語のテキスト"], ...}

Keys that already exist are left untouched. The files keep their BOM and CRLF line endings.
"""
import json
import os
import sys
from xml.sax.saxutils import escape

RESOURCES = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Resources")


def rewrite(path, change):
    with open(path, encoding="utf-8-sig", newline="") as f:
        text = f.read()
    text = change(text)
    with open(path, "w", encoding="utf-8-sig", newline="") as f:
        f.write(text)


def add_to_resx(strings, index):
    def change(text):
        block = ""
        for key, values in strings.items():
            if f'name="{key}"' in text:
                continue
            block += f'  <data name="{key}" xml:space="preserve">\r\n    <value>{escape(values[index])}</value>\r\n  </data>\r\n'
        return text.replace("</root>", block + "</root>")
    return change


def add_to_designer(strings):
    def change(text):
        end = text.rstrip().rfind("    }")
        block = ""
        for key, values in strings.items():
            if f"string {key} " in text:
                continue
            summary = escape(values[0]).replace("\n", " ")
            block += (f"        \r\n        /// <summary>\r\n        ///   Looks up a localized string similar to {summary}.\r\n"
                      f"        /// </summary>\r\n        internal static string {key} {{\r\n            get {{\r\n"
                      f"                return ResourceManager.GetString(\"{key}\", resourceCulture);\r\n            }}\r\n        }}\r\n")
        return text[:end] + block + text[end:] if block else text
    return change


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    with open(sys.argv[1], encoding="utf-8") as f:
        strings = json.load(f)

    rewrite(os.path.join(RESOURCES, "AppResources.resx"), add_to_resx(strings, 0))
    rewrite(os.path.join(RESOURCES, "AppResources.ja.resx"), add_to_resx(strings, 1))
    rewrite(os.path.join(RESOURCES, "AppResources.Designer.cs"), add_to_designer(strings))
    print("added:", ", ".join(strings))


if __name__ == "__main__":
    main()
