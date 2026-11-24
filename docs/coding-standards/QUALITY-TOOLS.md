# Quality tools — installation and staged rollout

The production projects currently target net10.0. FrontEnd already declares Roslynator.Analyzers 5.0.1; inspect actual SDK, restore and diagnostics before adding overlapping packages. This is a setup guide, not a claim that tools or servers are installed.

| Location | Tools |
| --- | --- |
| Repository | .editorconfig, Python governance/architecture checks, analyzer configuration, approved NuGet/npm dev dependencies and lockfiles, CI definition |
| Developer PC | Rider inspections, .NET 10 SDK, Python 3, supported Node.js/npm if lint is adopted |
| Build VM / GitLab Runner | SDK, Python, Git, Node/npm if needed, compatible SonarScanner, isolated test config |
| Dedicated service VM | SonarQube Server or Community Build, supported PostgreSQL, persistent storage, TLS and backup |
| ERP/IIS and production SQL | No quality tooling or CI jobs needed |

## Phase 1: local read-only baseline
From repository root in PowerShell:
```powershell
dotnet --list-sdks
py -3 scripts/check-coding-standards.py
py -3 scripts/check-onion-architecture.py
dotnet restore VazinProject.sln
dotnet build VazinProject.sln --no-restore
dotnet test VazinProject.sln --no-build
dotnet format VazinProject.sln --verify-no-changes --no-restore
dotnet package list --project VazinProject.sln --vulnerable --include-transitive
```
Use a compatible .NET 10 SDK; inspect existing test project type and run executable checks separately if needed. Never connect tests to live systems. SDK analyzers are available without a new NuGet package; use root EditorConfig and optionally approved Directory.Build.props to tune warnings. Baseline before enforcing, avoid mass-format. StyleCop is optional; do not install overlapping analyzer packages without approval.

## Phase 2: frontend
Inspect frontend files, legacy plugins, generated/vendor bundles and browser globals. If approved, install supported Node.js/npm on developer and runner; create a scoped package.json in the owning frontend directory if none exists, install pinned dev versions of ESLint, @eslint/js and Prettier, commit package-lock.json and config. Use npm ci in CI, lint project-owned JS only, check formatting without --write. Start with warnings/baseline; avoid treating vendor or legacy code as newly introduced violations. No runtime dependency is required merely to lint.

## Phase 3: CI and SonarQube
Install GitLab Runner on a dedicated build machine, register a project runner using a runner authentication token from GitLab, use restricted service credentials and isolated test settings. Do not mount live ERP/SQL credentials. Configure .gitlab-ci.yml after determining runner OS/executor and mirror strategy; run governance and architecture checks, restore/build/test, optional lint and vulnerability checks. Start report-only for legacy findings. A GitHub mirror alone does not activate GitLab CI.

Install a supported SonarQube edition with its supported database, TLS, persistence and backups on a separate VM, then create a project and analysis token. Keep token in masked/protected CI secret storage. Install compatible SonarScanner for .NET on the build machine (prefer a pinned local tool manifest); run begin -> build/test -> end against the configured URL. Enable quality-gate waiting and merge blocking only after successful verification and baseline review. Verify edition support for branch and merge-request analysis; Community Build may not support the desired branch/PR workflow. Do not promise that all merge-gate features are available in GitLab CE. Do not push source to a cloud scanner without approval.

Official documentation: [Microsoft analyzers](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview), [GitLab Runner](https://docs.gitlab.com/runner/install/), [ESLint](https://eslint.org/docs/latest/use/getting-started/), [SonarScanner](https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/scanners/dotnet/using).

Record actual tool versions, commands, results and remaining gaps in the rollout checklist. No server installation, new package dependency, CI activation or production change is implied by this guide.
