# Vazin AI Core — draft design v0.1
Status: design proposal, not implemented. HRM is the first new module integration. GapGPT now; private VM provider later. Existing Vira/Avanegar remains a specialized speech adapter. No assumption about VM runtime, API compatibility, capacity or model capabilities.

## Existing baseline
Main contains FrontEnd Services/AI/AiService.cs (legacy orchestration with direct CRMContextDb), GapgptAi.cs (invoice and audio Gemini 2.5 Flash, text GPT-5 Mini), AiPrompts.cs, ViraAi.cs, Models/Ai/AiGapgptModel.cs and Startup registrations for typed HTTP clients, AI queue/background/pending workers. Preserve existing routes, DTOs and jobs; assess callers before adapting. Models are hard-coded in existing methods; credentials in source are a critical remediation item (rotate externally, use secure runtime configuration; no secret values in docs). Live API capability, usage and pricing unverified.

## Proposed boundaries
MVC/API -> existing CQRS/application use cases (reuse existing contracts where approved) -> authorization + data policy -> AI orchestration -> capability-aware provider adapter -> GapGPT or Local AI. Domain owns durable business entities and repository contracts; SQL implements persistence; application owns orchestration and inward-facing AI ports; concrete HTTP adapters stay in appropriate existing outer layer, composition in FrontEnd. No new project or dependency assumed. Legacy AiService is not the new domain service; keep compatibility facade until tested. Existing deterministic ERP services remain authoritative.
Provider contract (conceptual): GenerateAsync(request, cancellation), optional streaming, structured output, tool call and usage capabilities. Normalized result includes content, finish reason, tool requests, usage when available, provider/model and error category. Provider-specific DTOs never leak into module code. Route by task, classification, capability, health and explicit policy. LocalOnly must fail closed if VM unavailable; never silently fall back to external providers. External calls only for approved minimized data. No automatic migration of historical chats to VM. Pilot synthetic/de-identified HR data only until privacy approval.
Tool pipeline: register typed tool with permission, scope, schema, classification and read/write risk; server validates all arguments and filters results; read-only first; write preview bound to actor, parameters and expiry, recheck permission and idempotency at execution. Treat prompts, files, retrieved text and model output as untrusted. Never execute arbitrary SQL/shell. Report citations to authorized record IDs, query ranges and source timestamps; do not invent facts. Background agents use explicit scoped service identity, budgets, schedule locks and human approval for sensitive changes.

## Candidate logical entities (names/types not final)
- AiProvider 1:N AiModel; AiTaskRoute references model/provider and allowed data classes, optional permitted fallback. Secret reference only, never API key in business DB.
- AiPrompt 1:N AiPromptVersion (immutable published versions); AiToolDefinition describes registered code tool, not arbitrary executable code.
- AiConversation 1:N AiMessage; ownership/ACL, retention and deletion; AiToolExecution links conversation/message or agent run, actor, approval reference, redacted metadata and idempotency key.
- AiAgent 1:N AiAgentSchedule, AiAgentRun; AiFinding links run and authorized entity reference; status/assignment/dedup key.
- AiUsageRecord links request/provider/model/user/module, nullable actual usage and cost, currency and timestamp; quotas, rates and audit policy.
- Later: AiKnowledgeDocument 1:N versions/chunks, ACL before retrieval, AiMemory entries opt-in/scoped, expiry/deletion; AiFeedback references message.
Use existing Identity user references and audited base conventions after inspection. Index owner/time, route key, unique run idempotency and provider/model identity. No migrations before model review. Define retention, encryption, redaction and access revocation before storage.

## Delivery gates
1. Validate existing call paths and API capabilities with sandbox/synthetic data, identify secrets and rotate via owner; no live calls yet.
2. Minimal provider abstraction + GapGPT adapter + mock tests; preserve invoice/audio features; local adapter only when API is known. Settings for model selection and per-task classification; no fallback without policy.
3. Read-only tool registry, permission/scoped data retrieval, global assistant UI and auditable conversations. HR first: aggregate attendance summaries, incomplete punches, approved policy Q&A.
4. Controlled action previews, scheduled agents, knowledge base and module-by-module rollout; financial and employment decisions remain human-led.
5. Local VM cutover: test capabilities, Persian quality, latency, load, TLS/auth/network, model/version and data retention, compare synthetic cases; switch route via configuration; rollback only within approved data policy.
Verification: test permission/scope isolation, revocation, injection, output schemas, retries/cancellation, budgets, offline behavior, provider fallback, old invoice/audio regression and UI RTL. No runtime tests run in this documentation phase.
