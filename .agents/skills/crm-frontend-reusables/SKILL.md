---
name: crm-frontend-reusables
description: Reuse and maintain the existing shared JavaScript utilities in every Vazin CRM frontend module when writing or changing Razor, JavaScript, or frontend components.
---

# CRM frontend reusables

## Reuse Priority

1. Check the shared source and relevant reference for an existing function or class before writing one.
2. Use an existing utility when its contract fits the need.
3. If it needs extension, inspect all relevant callers and preserve compatibility before changing it.
4. Create a new shared utility only when no suitable one exists; keep feature-specific code in its feature.
5. Avoid duplicate functions, unnecessary wrappers, and alternate implementations without a technical reason.

## Vanilla JavaScript

Write new and changed utility logic in vanilla JavaScript and native DOM APIs; do not add jQuery code or dependencies. Reuse compatible public helpers without copying their legacy internals into feature code. Inspect existing callers before converting a shared helper, preserving its public contract and lifecycle behavior. Follow [the frontend vanilla JavaScript requirement](../crm-frontend/SKILL.md#vanilla-javascript-requirement) for existing jQuery-dependent plugins and migration scope.

## Application HTTP requests

Prefer `sendApiRequest` for compatible application HTTP calls. Do not use standalone Fetch or Axios merely to add success messages, error handling, loading, callbacks, or session/cookie expiry handling: inspect and use the helper's actual options first. It awaits Axios and its success/error callbacks, returns response data (or the full Axios response when requested), and returns `null` for request errors and cancellation. Loading ends before callbacks run. See [site-core.md](references/site-core.md) for precise behavior. If a need is unsupported, verify that limitation in source and choose a solution compatible with existing callers and project structure.

The references describe the current implementation, not a guaranteed stable external API or an exhaustive list. An omitted function remains available if its source and page dependencies support it; do not restore deliberately omitted entries merely to expand the list. Check actual scripts included by the page layout and inspect call sites before use. `site.js` is loaded broadly; other shared scripts vary by layout. Global names are case sensitive: the modal helper is `ModalToggle`, not `modalToggle`.

- Read [site-core.md](references/site-core.md) for requests, feedback, modals, selection, forms, formatting, dates, and DOM helpers.
- Read [other-shared.md](references/other-shared.md) for shared functionality in the remaining public JavaScript files.

Before changing a shared utility, inspect all relevant callers (`rg -n 'Name' 03.EndPoints/CRM.EndPoint.FrontEnd`) and avoid breaking them. Update the matching reference in the same change whenever a shared utility is created or its public contract changes. Follow `crm-frontend` for general frontend implementation, UI, CSS, RTL, and responsive rules.
