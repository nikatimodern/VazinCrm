"""Inventory role-dependent authorization candidates without exposing source text.

A match is not necessarily a defect: role administration, account classification,
business assignment, disabled-flag fallback and historical audience selection
require contextual review. This report intentionally does not rewrite source code.
"""
from collections import Counter
from pathlib import Path
import os
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
PATTERNS = {
    "role-attribute": re.compile(r"Authorize\s*\([^\n]*\bRoles\s*=|\bRequireRole\s*\("),
    "role-check": re.compile(r"\b(?:IsInRole|HasRole|IsAdmin|IsSales(?:Admin|Expert)?|IsTamin(?:Admin|Expert)?|IsFinancial(?:Admin|Expert)?|IsManagingDirector|IsTanKhah|IsCustomer)\s*\("),
    "role-membership": re.compile(r"\b(?:GetRolesAsync|GetInRoleAsync|GetUsersInRoleAsync|IsInRoleAsync|GetRoleNames)\s*\("),
    "role-dependent-ui": re.compile(r"\b(?:checkRoles|rollNames|roleNames|CanEditOthers|ReadAssigned)\b|\bRoles\s*\.\s*(?:Contains|Any)\s*\("),
}


def tracked_sources():
    entries = subprocess.check_output(["git", "ls-files", "-z"], cwd=ROOT)
    for item in entries.decode("utf-8", errors="replace").split("\0"):
        path = Path(item)
        parts = {part.lower() for part in path.parts}
        if not item.startswith(("01.Core/", "02.Infrastructures/", "03.EndPoints/")):
            continue
        if path.suffix.lower() not in {".cs", ".cshtml", ".js", ".ts", ".tsx"}:
            continue
        if parts & {"migrations", "bin", "obj", "lib", "libs", "vendor", "vendors", "node_modules", "plugins"} or path.name.endswith(".min.js"):
            continue
        yield path


def main():
    counts = Counter()
    findings = []
    scanned = 0
    for path in tracked_sources():
        scanned += 1
        for number, line in enumerate((ROOT / path).read_text(encoding="utf-8-sig", errors="replace").splitlines(), 1):
            matches = [kind for kind, pattern in PATTERNS.items() if pattern.search(line)]
            if matches:
                counts.update(matches)
                findings.append(f"{path.as_posix()}:{number} ({', '.join(matches)})")
    summary = [f"Scanned {scanned} tracked source files; {len(findings)} candidate lines.",
               *[f"{key}: {value}" for key, value in sorted(counts.items())],
               "Candidates require semantic review; this is not an authorization test."]
    print("\n".join(summary + findings))
    if target := os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(target, "a", encoding="utf-8") as output:
            output.write("### Role authority audit\n\n" + "\n".join(summary) + "\n\n")
            output.write("<details><summary>Candidate locations</summary>\n\n")
            output.write("\n".join("- " + item for item in findings) + "\n\n</details>\n")


if __name__ == "__main__":
    main()
