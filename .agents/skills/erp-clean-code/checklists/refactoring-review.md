# refactoring review

- [ ] Trace all callers including reflection, DI, Razor, JS, SQL and external contracts.
- [ ] Preserve business behavior, authorization and public contracts unless requested.
- [ ] Remove only verified dead code; avoid unrelated restructuring and unnecessary abstractions.
- [ ] Compare before/after behavior and run relevant regression checks; document remaining legacy issues.

Mark N/A with a reason; record actual evidence. Never mark not-run checks as passed.
