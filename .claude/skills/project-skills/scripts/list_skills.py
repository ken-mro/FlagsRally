#!/usr/bin/env python3
"""List this project's Claude Code skills, read fresh from .claude/skills/ every time.

Each skill is a folder with a SKILL.md whose YAML frontmatter has `name` and `description`.
Nothing here names a skill, so a new skill folder shows up without changing this script.

    python3 .claude/skills/project-skills/scripts/list_skills.py           # Markdown table
    python3 .claude/skills/project-skills/scripts/list_skills.py --json    # for further processing
    python3 .claude/skills/project-skills/scripts/list_skills.py --root <repo>

Standard library only: the frontmatter subset skills use (plain, quoted and block scalars)
is parsed here, so no PyYAML is needed.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

# .claude/skills/project-skills/scripts/list_skills.py -> repository root
DEFAULT_ROOT = Path(__file__).resolve().parents[4]


def read_frontmatter(text: str) -> dict[str, str] | None:
    """Top-level `key: value` pairs of the leading `---` block, or None when there is none."""
    lines = text.lstrip("\ufeff").splitlines()
    if not lines or lines[0].strip() != "---":
        return None
    try:
        end = next(i for i in range(1, len(lines)) if lines[i].strip() == "---")
    except StopIteration:
        return None

    fields: dict[str, str] = {}
    body = lines[1:end]
    i = 0
    while i < len(body):
        match = re.match(r"^([A-Za-z0-9_-]+):\s?(.*)$", body[i])
        i += 1
        if not match:
            continue
        key, value = match.group(1), match.group(2).strip()

        # Block scalar: `>` folds lines into one, `|` keeps them (both may end in - or +).
        if value[:1] in (">", "|"):
            block = []
            while i < len(body) and (not body[i].strip() or body[i][:1] in (" ", "\t")):
                block.append(body[i].strip())
                i += 1
            value = (" " if value[0] == ">" else "\n").join(block).strip()
        else:
            # Plain value continued on indented lines.
            while i < len(body) and body[i][:1] in (" ", "\t") and body[i].strip():
                value += " " + body[i].strip()
                i += 1
            if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
                value = value[1:-1]
        fields[key] = value
    return fields


def collect(root: Path) -> list[dict]:
    skills_dir = root / ".claude" / "skills"
    skills = []
    for folder in sorted(p for p in skills_dir.iterdir() if p.is_dir()) if skills_dir.is_dir() else []:
        skill_md = folder / "SKILL.md"
        entry = {"folder": folder.name, "path": str(skill_md.relative_to(root)), "problems": []}
        if not skill_md.is_file():
            entry["problems"].append("no SKILL.md")
            skills.append(entry)
            continue
        fields = read_frontmatter(skill_md.read_text(encoding="utf-8", errors="replace"))
        if fields is None:
            entry["problems"].append("no frontmatter")
            fields = {}
        entry["name"] = fields.get("name") or folder.name
        entry["description"] = fields.get("description", "")
        entry["user_only"] = fields.get("disable-model-invocation", "").lower() == "true"
        if not fields.get("name"):
            entry["problems"].append("no name (folder name used)")
        if not entry["description"]:
            entry["problems"].append("no description (Claude cannot tell when to use it)")
        skills.append(entry)
    return skills


def to_markdown(skills: list[dict], root: Path) -> str:
    if not skills:
        return f"No skills found in {root / '.claude' / 'skills'}."
    rows = ["| Skill | What it does / when it is used |", "|---|---|"]
    for s in skills:
        name = f"`/{s.get('name', s['folder'])}`" + (" (user only)" if s.get("user_only") else "")
        text = (s.get("description") or "—").replace("|", "\\|").replace("\n", " ")
        rows.append(f"| {name} | {text} |")
    out = [f"{len(skills)} skills in `.claude/skills/`:", "", *rows]
    problems = [f"- `{s['path']}`: {', '.join(s['problems'])}" for s in skills if s["problems"]]
    if problems:
        out += ["", "Problems:", *problems]
    return "\n".join(out)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--root", type=Path, default=DEFAULT_ROOT, help="repository root (default: this repo)")
    parser.add_argument("--json", action="store_true", help="print JSON instead of a Markdown table")
    args = parser.parse_args()

    skills = collect(args.root)
    print(json.dumps(skills, ensure_ascii=False, indent=2) if args.json else to_markdown(skills, args.root))
    return 0


if __name__ == "__main__":
    sys.exit(main())
