---
name: crm-typography
description: >
  Apply consistent font families, sizes, weights, line heights, text hierarchy,
  Persian RTL typography, and text overflow when creating or editing CRM frontend
  pages, components, forms, tables, modals, dashboards, or navigation, including
  UI work that does not explicitly mention fonts. Use alongside crm-responsive
  and the project design system. Skip backend-only changes and business logic.
---

# CRM Typography

Own typography and readable text hierarchy in the affected UI. Inspect the actual
component and loaded styles before choosing a size or family. This skill guides
scoped UI work; it does not authorize a global font migration or redesign.

## Workflow

1. Trace the view, shared partials, layout, relevant CSS and dynamic templates.
   Check existing family aliases, shorthand declarations, inherited weights and
   density overrides before adding styles.
2. Read only the references relevant to the change:
   - [Fonts](references/crm-typography-fonts.md): font loading, valid weights,
     fallback fonts, mixed scripts, numerals and the observed font inventory.
   - [Sizes](references/crm-typography-sizes.md): hierarchy, line heights,
     candidate tokens and existing size inconsistencies.
   - [Components](references/crm-typography-components.md): component-specific
     sizes, wrapping and mobile text behavior, with actual source locations.
   - [Checklist](references/crm-typography-checklist.md): verification before delivery.
3. Reuse the affected component's typography where it fits the scale. Consolidate
   repeated touched declarations rather than adding arbitrary sizes, duplicate
   font faces or broad overrides. Keep unrelated callers unchanged.
4. Verify Persian, mixed-direction text, long content and actual rendered font
   selection. Report code checks separately from browser checks and name any
   unverified visual behavior.

## Integration With Existing Skills

- [Project UI Design System](../project-ui-design-system/SKILL.md) owns palette and
  visual component conventions; preserve its IRANSans baseline. This skill adds
  typography detail without replacing that standard.
- [CRM Responsive](../crm-responsive/SKILL.md) owns breakpoints, layout, dimensions
  and viewport behavior. This skill owns text size, weight, leading and text
  overflow strategy. Select sizes from the scale within existing responsive
  rules; do not introduce breakpoints to hide a text-fit problem.
- [CRM Frontend](../crm-frontend/SKILL.md) and
  [CRM Frontend Reusables](../crm-frontend-reusables/SKILL.md) own implementation,
  shared JavaScript and lifecycle contracts. Reuse formatters instead of changing
  data to achieve a visual result.
- Leave architecture, persistence and financial rules to their specialist skills.
  Do not delete or rewrite existing skills. If instructions genuinely conflict,
  report the affected rule and a scoped resolution before changing that rule;
  distinguish documented defaults from explicit user requirements.

The references describe inspected source and proposed defaults, not proof of
computed browser styles. Recheck source when it changes. Activating this skill
does not add runtime CSS, load fonts, or change `AGENTS.md`.
