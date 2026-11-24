# Vazin ERP coding standards

Start with [AGENTS.md](../../AGENTS.md), [erp-clean-code](../../.agents/skills/erp-clean-code/SKILL.md) and [چک لیست قوانین](RULES-CHECKLIST.md). This is an incremental governance rollout: no global rewrite or warning-as-error switch. Existing specialist skills remain authoritative for exact implementation.

## Local checks (run from repository root)

- `python3 scripts/check-coding-standards.py` — validate manifest, numbered references, checklist links and root instruction integration. Windows: `py -3 scripts/check-coding-standards.py`.
- `dotnet build VazinProject.sln` — use the actual compatible SDK and restore configuration; do not assume global12.json pins the SDK.
- `dotnet test VazinProject.sln --no-build` — inspect test projects first; run any non-test executable checks using their documented entry point. Do not run tests targeting production data.
- `dotnet format VazinProject.sln --verify-no-changes --no-restore` — optional read-only baseline after successful restore; never mass-format as part of a scoped change.
- `dotnet list VazinProject.sln package --vulnerable --include-transitive` — requires compatible SDK, package restore/network and advisory feed; record unavailable results.

## Tool rollout

| Tool | Status / action |
| --- | --- |
| EditorConfig | Conservative root defaults added; verify existing conventions before extending. |
| .NET/Roslyn analyzers | Inspect installed SDK/project settings and baseline diagnostics; no blanket warning-as-error. |
| StyleCop | Pending approval and compatibility/baseline; do not add a dependency silently. |
| ESLint / Prettier | Pending frontend manifest and lint scope; exclude vendor/generated files and preserve legacy baseline. |
| SonarQube | Pending server, credentials in secret store, scanner and baseline configuration. |
| Build/test/format | Commands documented; actual execution and outcomes pending. |
| Dependency scan | Command documented; actual advisory scan pending. |
| CI quality gate | Pending configured runner, secrets/restore, actual passing run and branch protection. Start with changed-code and structural checks, then tighten based on baseline. |

Critical security/data issues must never be accepted merely because they predate a baseline. Document exceptions with rule, scope, rationale, risk, owner approval and expiry. No claimed automation is active until verified.
