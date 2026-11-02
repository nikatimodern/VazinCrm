---
name: project-ui-design-system
description: Apply Vazin CRM's official visual design system when creating or editing UI designs, Razor pages, forms, modals, tables, or frontend components, including responsive previews and palette decisions.
---

# Project UI Design System

Use this as the visual reference for new and edited Vazin CRM interfaces. Read [design tokens and component guidance](references/design-tokens.md) before choosing colors or styling components. The official palette supersedes earlier preview palettes; existing legacy CSS is evidence of implementation, not permission to change the approved colors.

## Workflow

1. Inspect the affected view, its layout, loaded CSS, shared components, and current interactions. The source map in the reference is a starting point; verify the current files.
2. For implementation, also follow [CRM Frontend](../crm-frontend/SKILL.md) and [CRM Frontend Reusables](../crm-frontend-reusables/SKILL.md). This skill owns visual decisions; those skills cover frontend implementation and shared utility contracts.
3. Reuse suitable existing markup, classes, icons, and utilities. Check their actual appearance and behavior before adopting them. Where legacy styling conflicts with the palette, use a scoped adjustment for the requested component rather than recoloring every caller.
4. Use semantic color variables. Search existing definitions and consumers before adding tokens; do not paste another global palette into each page. Follow the reference's incremental adoption rules when official variables are not yet available.
5. Keep designs modern, restrained, readable, and organizational: roughly 80% neutral surfaces and text, with color reserved for actions, states, and limited emphasis. This is a visual guide, not a numeric constraint. Avoid unnecessary decoration and animation.
6. Preserve Persian content, the existing IRANSans fonts, RTL, authorization, business meaning, and HTTP behavior. A visual task does not authorize new business features, packages, architecture, or unrelated redesigns.

## CSS and interaction rules

- Prefer existing shared styling; add CSS only for a demonstrated need. Scope new selectors to the page/component and use logical spacing properties where appropriate.
- Avoid scattered hardcoded colors, unnecessary Bootstrap overrides, and `!important`. Do not globally redefine `.btn`, `.badge`, `.modal`, or `.form-control` to fix one page.
- Use the official palette for new designs. Explain any genuine need for an outside color; never use success or danger merely as decoration.
- Use vanilla JavaScript and existing project helpers rather than duplicate components. Verify the loaded `site.js` contracts before using requests, notifications, loading, validation, or formatting.
- Make loading, empty, error, disabled, selected, hover, and keyboard-focus states understandable. Prevent duplicate submission and preserve focus, scrolling, and cleanup for modals and enhanced selects.

## Verification and maintenance

- Review desktop and narrow mobile layouts: long Persian labels, RTL alignment, readable tables/cards, accessible controls, and modal/select overflow. Card conversion is a task-specific choice, not a requirement for every table.
- Check semantic states use text or meaningful icons as well as color; check contrast and visible keyboard focus in actual rendered states. Do not assume every palette pairing is readable.
- Inspect shared-style callers when changing shared code and check affected neighboring pages for regressions. Keep broader refactoring subject to the repository's approval rule.
- Report changed files and checks actually performed in Persian. If no browser verification was possible, say so.
- Maintain token values and component guidance in the reference, not duplicate copies in other skills or AGENTS.md. Updating this documentation alone does not authorize editing application CSS or migrating existing pages.
