# 22. Automated quality control

**Classification:** Mandatory for applicable checks; rollout staged

## Required behavior
Use root EditorConfig, built-in .NET analyzers where supported, and scripts/check-coding-standards.py for structural validation. Run dotnet build/test/format verification and JS lint when configured. Introduce StyleCop, ESLint, Prettier, SonarQube and dependency scans only with compatible configuration and baseline; configure CI/quality gates with approval.

## Prohibited behavior and exceptions
Do not auto-format the entire repository, add packages blindly, assume an installed server, or report tooling as active before actual execution. Legacy findings need a baseline, not blanket suppression.

## Examples
Good: run a read-only check and report new warnings; bad: set every existing warning to error immediately.

## Review checklist
- [ ] Record tool versions, exact commands, baseline, failures, missing tools and CI activation evidence.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.

## Architecture and setup
Run `python3 scripts/check-onion-architecture.py` for the direct project-reference boundary check. Review source-level dependencies and CQRS separately. See [quality tooling setup](../../../../docs/coding-standards/QUALITY-TOOLS.md) for installation locations, safe baseline and CI rollout. Existing .NET 10 SDK analyzers and the FrontEnd Roslynator package are preferable to redundant installations.
