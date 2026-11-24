# Access catalog rollout: entities 24–47

## Status

The registration SQL and migration cover the 24 remaining entity definitions and 89 permission keys. The 347 role assignments are proposed initial grants, not a verified copy of effective production permissions. Existing grants are preserved. Review the SQL preview before applying.

Only SentSms and ReceivedSms are enabled in Development in this batch. The earlier 23 flags are unchanged. The remaining flags are intentionally disabled pending completion of the checks below. Production configuration is unchanged.

## Staged work and outstanding checks

| Entity | Work | Outstanding |
|---|---|---|
| QualityControl / QualityControlQuestion | Endpoint permission checks | Customer and item relationships, review/note operations and UI, question administration |
| Product / ProductAmount | Lookup, price endpoint checks | Embedded selectors, price-role configuration, nested reads and UI |
| ProductGroup | Catalog registration | Group relationships and actual use cases |
| Contract | Scoped controller checks | Attachment persistence and private file delivery, views |
| Carizo | Assigned-user scope, controller/list checks | Shared task children, side effects and notifications, UI |
| Report / CallRecording | Report/participant scope, export and listen checks, dashboard count | Parent customer/deal permissions, other call routes, nested projections, background jobs and UI |
| Chat / Email | Participant and author/recipient checks, catalog gates | Attachments, hub authorization and notifications, UI and counts |
| Contact / Agent | Catalog registration | Parent customer scope, nested read/write paths |
| ReleaseNote | Administration gates | Preserve audience-based reader behavior, administration UI |
| LogSystem / SystemWarning | Dashboard and warning gates | Entity-specific log and warning source access, indirect routes and UI |
| Company / ASetting / BaseItem / BaseDescription | Selected endpoint gates | Shared configuration use, embedded forms, files and other direct/indirect paths |
| ExternalSoftware / ExternalMapping | Management and mapping endpoint gates | Transfer/export workflows, related entity checks and UI |
| SentSms / ReceivedSms | Admin-only page and API gates, separate view/sync controls, live state | Build, permission revocation and runtime tests |

## Deployment

1. Back up the UserConnection database. Preview RegisterRemainingAccessCatalog.sql with @Apply=0, validate entity namespaces and grant choices, and apply using the normal deployment procedure or the corresponding EF migration.
2. Build the entire solution and run integration tests on a non-production database. Test authorized and denied direct URLs, mixed grants, explicit denies, ownership and selected-user scopes, and role compatibility.
3. Complete remaining indirect, frontend and real-time checks before enabling each staged flag.
4. Do not interpret catalog registration as a completed authorization rollout. No production database changes or production configuration activation have been performed.
