#!/usr/bin/env python3
"""Validate direct project references of the five existing production layers; read-only."""
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
projects = {
    "Framework": "00.Framework/CRM.Framework/CRM.Framework.csproj",
    "Domain": "01.Core/CRM.Core.Domain/CRM.Core.Domain.csproj",
    "Application": "01.Core/CRM.Core.ApplicationServices/CRM.Core.ApplicationServices.csproj",
    "Sql": "02.Infrastructures/CRM.Infrastructures.Sql/Generics.Infrastructures.Sql/CRM.Infrastructures.Sql.csproj",
    "FrontEnd": "03.EndPoints/CRM.EndPoint.FrontEnd/CRM.EndPoint.FrontEnd.csproj",
}
allowed = {
    "Framework": set(),
    "Domain": {"Framework"},
    "Application": {"Domain", "Framework"},
    "Sql": {"Domain", "Framework"},
    "FrontEnd": {"Application", "Domain", "Sql", "Framework"},
}
by_path = {(root / path).resolve(): name for name, path in projects.items()}
errors = []
for layer, path in projects.items():
    file = root / path
    try:
        tree = ET.parse(file)
    except (OSError, ET.ParseError) as exc:
        errors.append(f"{layer}: cannot read {path}: {exc}")
        continue
    for node in tree.iter():
        if node.tag.rsplit("}", 1)[-1] != "ProjectReference":
            continue
        include = node.get("Include", "")
        target = (file.parent / include.replace(chr(92), "/")).resolve()
        target_layer = by_path.get(target)
        if target_layer not in allowed[layer]:
            errors.append(f"{layer}: prohibited or unclassified ProjectReference {include}")
        elif not target.is_file():
            errors.append(f"{layer}: missing target {include}")
        else:
            print(f"{layer} -> {target_layer}")
if errors:
    print("\n".join(errors), file=sys.stderr)
    sys.exit(1)
print("Onion Architecture: direct project references passed; source and MSBuild-evaluated dependencies require separate review.")
