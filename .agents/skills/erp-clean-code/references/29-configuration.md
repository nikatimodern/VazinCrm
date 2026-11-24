# 29. Configuration and secrets

**Classification:** Critical

## Required behavior
Use typed options and existing configuration patterns, validate required settings at startup where suitable. Separate environments, keep secrets out of source and logs, use secure secret stores/environment variables and safe defaults.

## Prohibited behavior and exceptions
No hardcoded credentials, unsafe production fallbacks, accidental environment switching or secret exposure in reports. Do not modify existing live configuration without authorization.

## Examples
Good: resolve a configured provider through validated options; bad: embed an API key in C#.

## Review checklist
- [ ] Check configuration sources, environment, validation, secret redaction and rotation needs.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
