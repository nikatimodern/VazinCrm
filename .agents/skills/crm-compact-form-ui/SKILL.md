---
name: crm-compact-form-ui
description: "Use for EVERY creation, change, redesign, review, or refactor of a form or form-like interface in the CRM UI: inputs, selects, textareas, labels, buttons, validation, filters, modal forms, drawers, wizards, and their Razor/HTML/CSS/JavaScript. Apply compact form density by default even when the user does not explicitly request it. Coordinate with existing responsive, typography, and component skills. Do not use for unrelated non-form pages."
---

# CRM Compact Form UI

## Objective

Create **compact, balanced, readable, usable** forms. A form is not better because its inputs, buttons, boxes, margins, or modal are large. Make each form occupy only the space that its content requires.

This skill applies to **new forms and changes to existing forms**. It applies whether a user explicitly asks for compact UI or merely asks to add an input, button, validation message, filter, or modal.

## Required workflow

1. Inspect relevant existing page/component and nearby styles **before** changing markup or CSS. Reuse the project's current form components and design tokens wherever possible.
2. Read `references/density-rules.md` when creating or editing form UI; use its sizing targets and exceptions.
3. Read `references/implementation-patterns.md` when a new layout or styling pattern is needed. Treat its code as an example, not a mandatory new framework.
4. Check existing responsive, font/typography, accessibility, and component skills. **Complement, do not duplicate or override** them. If there is a conflict, preserve functionality, accessibility, and responsive behavior while achieving the most compact reasonable result.
5. Scope any new CSS to the relevant component or a unique wrapper class. Prefer adjusting existing local styles rather than adding another competing styling system.
6. Review the result at desktop and mobile widths. Reduce needless whitespace without causing clipped text, errors, unreadable labels, tiny mobile touch targets, or horizontal overflow.

## Non-negotiable form design defaults

- **Compact by default**: inputs and buttons must not be visually oversized; card, section, and modal paddings must not be inflated.
- **No ornamental whitespace**: never add large empty rows or `min-height` solely to make the form look spacious.
- **No card per field by default**: label + control should be a simple field group. Put a border/background around a set of related fields only when it adds real structure.
- **Avoid double padding**: nested modal > section > card > field wrappers should not each contribute substantial spacing.
- **Short, consistent vertical rhythm**: align label, control, validation, and unit text closely; do not put a unit alone in a new tall row if it fits inline.
- **Modal size follows content**: do not use a nearly full-screen modal for a short form. If the form is genuinely long, make the body scrollable and keep header/footer practical.
- **Do not shrink everything indiscriminately**: preserve legibility, focus visibility, validation feedback, touch ergonomics, and content hierarchy.
- **No global styling blast radius**: do not apply new global rules to `input`, `button`, `.form-control`, `.btn`, or shared modal selectors unless the task explicitly requests an application-wide refactor.
- **Do not change JavaScript behaviors, field names, form submission, validation, permissions, or API contracts** just to compact the layout.
- **RTL first** for Persian forms; preserve readable number entry, icons, labels, and localized text. Use the project's existing font skill.

## Coordination and activation

- This skill is additive. Do not rename, remove, or rewrite existing project skills.
- Use the existing responsive skill for breakpoints and layout adaptation; this skill governs **form density and space usage**.
- Use the existing font skill for font family/typography decisions; this skill only sets practical size ranges when no design token exists.
- If a form is intentionally large for accessibility, a kiosk, a touch-first device, or a user-specified design, respect that context and document the exception.
- The `description` above supports implicit Codex activation, but implicit selection is not guaranteed. For strict project-wide enforcement, also add the snippet in `references/integration.md` to the repository's **existing** `AGENTS.md`, without replacing its content.

## Final self-check

Before declaring a form change complete, ask:

1. Can any oversized input, button, inner padding, outer margin, or empty vertical area be reduced without hurting usability?
2. Is there any unnecessary bordered card around an individual field?
3. Are units, helper text, and errors located close to their relevant fields?
4. Are desktop controls compact, while mobile controls remain sufficiently easy to tap?
5. Did I reuse current project styles, preserve RTL, and avoid global CSS regressions?

If any answer identifies wasted space, improve the layout before finishing.
