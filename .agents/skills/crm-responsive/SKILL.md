---
name: crm-responsive
description: Plan, implement, or review responsive Vazin CRM UI when creating pages or components, changing layouts, sidebar or navigation, tables, forms, modals, dashboards, or fixing mobile, tablet, laptop, and desktop display. Skip for backend-only and business-logic-only work.
---

# CRM Responsive UI

Own viewport-dependent layout and interaction fit. Inspect the affected Razor/React component, loaded CSS, scripts, layout, and shared components before choosing a rule. Preserve the current behavior and use mobile-first CSS for new scoped work.

Read only the references needed for the task:

- [Breakpoints](references/crm-responsive-breakpoints.md) when adding or reviewing media queries.
- [Layout](references/crm-responsive-layout.md) for page shells, spacing, overflow, orientation, and navigation.
- [Components](references/crm-responsive-components.md) for the affected UI component.
- [Checklist](references/crm-responsive-checklist.md) for responsive verification and reporting.

## Integration With Existing Skills

- This skill complements [Project UI Design System](../project-ui-design-system/SKILL.md) for palette, fonts, and visual styling; [CRM Frontend](../crm-frontend/SKILL.md) for frontend implementation; and [CRM Frontend Reusables](../crm-frontend-reusables/SKILL.md) for shared JavaScript. Follow those skills for UI work as required by `AGENTS.md`.
- Leave architecture, APIs, JavaScript contracts, and business rules to their specialist instructions. Avoid copying their rules here.
- Consolidate related responsive rules in the affected component stylesheet or existing shared layout stylesheet. Avoid duplicate CSS and scattered media queries.
- Report a genuine conflict with another skill or existing behavior; do not rewrite its rules without a reason and the task's authorization.
