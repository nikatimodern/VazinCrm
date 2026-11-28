# Vazin AI Core — implementation checklist
Update only after verified evidence. Counts are task counts, not effort. Design proposals are not implemented features. Baseline: main at branch creation, 2026-11-28. See ARCHITECTURE.md and root AGENTS.md. HRM has its own checklist on hrm.

## A01 — Preparation
- [x] Create ai-core from main (GitHub branch creation confirmed).
- [x] Read root AGENTS and mandatory Clean Code baseline/Onion and backend skills.
- [x] Identify existing GapGPT, Vira, AiService, prompt/DTO and background registrations (main source inspection).
- [x] Record initial architecture, logical entities and scoped documentation instructions.
- [x] Inspect direct project references, supply invoice CQRS path, AI queue/worker and provider dependencies (TECHNICAL-DESIGN.md).
- [ ] Complete remaining call-site inventory and verify actual DI resolution, access enforcement and runtime architecture checker.
- [ ] Confirm API capabilities, tool calling/streaming, live model availability and usage.
- [ ] Inventory and rotate exposed credentials with owner; secure runtime settings; review repository visibility/history.
- [x] Draft normalized provider contract, capability-aware routing and candidate relational schema (TECHNICAL-DESIGN.md).
- [ ] Review design and obtain entity/schema approval.
## A02 — Provider foundation
- [x] Add normalized text/JSON request/result, capability enum and IAiProvider source contract (uncompiled; no live capability claim).
- [x] Add isolated GapgptTextProvider source for text/JSON; no DI registration or runtime exposure yet.
- [ ] Configure dedicated authenticated HttpClient, secure settings, approved synthetic live test and regression-compatible legacy integration.
- [x] Add request validation, timeout/cancellation, bounded response and safe error classification in adapter source.
- [ ] Verify timeout/cancellation/errors with tests and implement approved retry policy (no automatic retries in first slice).
- [x] Add explicit provider/model routing and fail-closed data policy source; no automatic fallback.
- [ ] Implement persistent catalog, authorized task classification, safe fallback policy, quotas, secure endpoint allowlist and registration.
- [ ] Implement local VM adapter after actual API and deployment details are known.
- [ ] Validate local VM Persian quality, tool/stream support, load, network security, cutover and rollback.
- [ ] Test deterministic mock adapters and invoice/audio regression.
## A03 — Persistence and security
- [ ] Finalize logical entities, actual key types, retention and access model.
- [ ] Domain repository contracts, SQL implementations/mappings, application use cases.
- [ ] Generate approved migration and reviewable SQL; apply only with explicit authorization.
- [ ] Register permissions/Scope and enforce direct/indirect paths, including tool execution, reports, search and exports.
- [ ] Enforce data classification, redaction, LocalOnly fail-closed and no secret disclosure.
- [ ] Protect against prompt injection, untrusted files, unsafe outputs, cross-user cache/memory leakage.
- [ ] Audit requests/operations with minimal metadata and current actor; revoke permissions immediately.
## A04 — Management center
- [ ] Provider and model configuration, secure secret references, health/test.
- [ ] Per-task model/routing and feature switches.
- [ ] Versioned prompts, tools, agents and scheduling.
- [ ] Usage, pricing/actual cost, budgets, error/status views and feedback.
## A05 — Tools and assistant
- [ ] Typed registry; schema validation, scoped read-only ERP tools and source links.
- [ ] Tool calls and result bounds, safe iteration limits; no arbitrary SQL.
- [ ] Global RTL responsive assistant UI with page context, conversation management, tables/charts and links.
- [ ] Streaming/cancel where supported, attachments, voice, copy/export and feedback.
- [ ] Conversation storage and scoped memory, retention/deletion and reauthorization.
- [ ] Controlled write previews, confirmation, expiry, replay/idempotency and audit.
## A06 — Knowledge and automation
- [ ] Versioned documents, ACL-aware retrieval/RAG, references and retention.
- [ ] Agent service identities, schedule/event triggers, durable runs, lock/retry/budget.
- [ ] Findings, severity, deduplication, assignment, resolution and authorized SignalR/cartebl.
- [ ] Daily/weekly/monthly report workflows with human review.
## A07 — HRM first integration
- [ ] Agree shared AI contract with hrm; no direct GapGPT calls from HRM.
- [ ] Attendance aggregates, incomplete punches and policy Q&A using synthetic/minimized data.
- [ ] Shift capacity and overtime/cost insights; local-only sensitive analysis after VM readiness.
- [ ] Document extraction and employee self-service under appropriate privacy policy; human review.
- [ ] Scoped HR agent, pilot and regression; no automated employment decisions.
## A08 — ERP rollout
- [ ] Persons/customers and sales.
- [ ] PreFactors, factors and orders.
- [ ] Supply, supplier comparisons and invoice extraction.
- [ ] Finance, receivables, cashflow, checks, petty cash and controlled SMS.
- [ ] Quality control and customer feedback.
- [ ] PBX/Avanegar and SMS.
- [ ] Warehouse and production (verify actual data/modules).
- [ ] Sepidar matching and tax error explanations.
- [ ] IT monitoring, backups and logs (approved read-only access).
- [ ] Cross-module executive dashboard and management agent.
## A09 — Verification and release
- [ ] Unit/integration/security, scope/revocation/injection, schema, financial accuracy and fallback tests.
- [ ] Rate limits, performance, token cost and resilience/offline tests.
- [ ] UI accessibility/RTL/mobile, architecture checker and actual build.
- [ ] Documentation, staged release, approval before deployment or main merge.

Evidence: v0.3 source foundation added and read back from ai-core (IAiProvider, AiProviderPolicy, AiProviderRouter, GapgptTextProvider). No changes to Startup/DI, legacy AI flows, application endpoints, packages, database or main. Local VM adapter, live verification, secret rotation and AccLevel integration remain open. The environment has no checked-out repository or dotnet SDK; build and tests not run. Source tasks checked only for code presence, not operational completion.
