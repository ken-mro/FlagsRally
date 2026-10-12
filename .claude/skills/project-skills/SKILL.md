---
name: project-skills
description: Explain which Claude Code skills this FlagsRally project has, what each one does and when to use it, read live from .claude/skills/ so newly added skills are always included. Use whenever the user asks what skills or slash commands the project has ("どんなスキルがある?", "スキル一覧", "/ で何が使える?", "which skill should I use for X?"), wants to pick a skill for a task, or has just added or edited a skill and wants to check it is picked up.
---

# Explain this project's skills

The list of skills is never written down here, on purpose: it is read from `.claude/skills/` each
time, so a skill added or changed tomorrow is described correctly without editing this file.

1. **Read the skills** from the repository root:
   ```bash
   python3 .claude/skills/project-skills/scripts/list_skills.py
   ```
   It prints a Markdown table of every `.claude/skills/*/SKILL.md` (name and description from the
   frontmatter), then any problems such as a missing description. Use `--json` if you want to
   group or filter the skills yourself. Do not answer from memory or from the skills listed in your
   context: those can be out of date or include skills from plugins and the user's own folder,
   which are not this project's.
2. **Answer in the user's language** (usually Japanese). Translate or summarise the descriptions;
   keep the skill names as they are, written as slash commands (`/ship-pr`), since that is how the
   user invokes them. Include every skill the script found, this one included.
   - Group them by purpose when there are more than a few, for example checking changes on
     devices, writing text or changelogs, shipping. Pick groups that fit the skills actually found
     rather than a fixed set.
   - If the user asked about a task ("PR を出したい"), point to the skill or skills that fit first,
     then mention the rest briefly.
   - A skill marked "user only" is run only when the user types it; say so.
3. **Report problems** the script lists (no frontmatter, no description, a folder without
   SKILL.md): such a skill cannot be chosen automatically, so suggest the fix.
4. If you need more than the description to answer (the steps a skill takes), read that skill's
   `SKILL.md`; the path is in the `--json` output.
