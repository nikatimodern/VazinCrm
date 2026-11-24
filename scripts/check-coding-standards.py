#!/usr/bin/env python3
"""Read-only validation of the ERP coding standards structure."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
skill = root / ".agents/skills/erp-clean-code"
errors = []

def require(condition, message):
    if not condition:
        errors.append(message)

def read(path):
    require(path.is_file(), f"Missing: {path.relative_to(root)}")
    return path.read_text(encoding="utf-8") if path.is_file() else ""

manifest = read(skill / "SKILL.md")
require(manifest.startswith("---\n"), "Missing skill front matter")
require("name: erp-clean-code" in manifest, "Wrong skill name")
require("description:" in manifest, "Missing skill description")
refs = sorted((skill / "references").glob("*.md"))
require(len(refs) == 33, "Expected 33 reference files")
require([p.name[:2] for p in refs] == [f"{i:02}" for i in range(1, 34)], "Reference numbering mismatch")
checklist = read(root / "docs/coding-standards/RULES-CHECKLIST.md")
for path in refs:
    body = read(path)
    for section in ("Classification:", "## Required behavior", "## Prohibited behavior and exceptions", "## Examples", "## Review checklist"):
        require(section in body, f"{path.name}: missing {section}")
    require(f"references/{path.name}" in manifest, f"Unindexed reference: {path.name}")
    require(f"references/{path.name}" in checklist, f"Missing checklist entry: {path.name}")
for name in ("before-development", "after-development", "code-review", "security-review", "database-review", "refactoring-review"):
    read(skill / "checklists" / f"{name}.md")
    require(f"checklists/{name}.md" in manifest, f"Unindexed checklist: {name}")
require(".agents/skills/erp-clean-code/SKILL.md" in read(root / "AGENTS.md"), "AGENTS.md does not load skill")
for path in [skill / "SKILL.md", *refs, *(skill / "checklists").glob("*.md"), root / "docs/coding-standards/README.md", root / "docs/coding-standards/RULES-CHECKLIST.md"]:
    for link in re.findall(r"\]\(([^)]+)\)", read(path)):
        target = link.split("#", 1)[0]
        if target and "://" not in target:
            require((path.parent / target).exists(), f"{path.name}: broken link {target}")
if errors:
    print("\n".join(errors), file=sys.stderr)
    sys.exit(1)
print("ERP coding standards: structural checks passed (not application compliance).")
