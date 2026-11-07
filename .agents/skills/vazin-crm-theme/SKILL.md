---
name: vazin-crm-theme
description: >
  Apply Vazin CRM-specific theme rules to prefactors, invoices, cards, timelines,
  actions, tabs, searches, tables and modals. Use for theme decisions and UI
  changes in this repository, including alternating teal/purple cards and gray
  unsuccessful documents. This is a project-local skill, not a theme for other projects.
---

# Vazin CRM Theme

## Scope and authority

This skill records the approved Vazin CRM theme and document-state styling.
It applies only to this repository and its UI previews. Keep it under
`.agents/skills/vazin-crm-theme`; do not install it as a personal/global skill.
The `vazin-crm-` prefix distinguishes it from future project themes.

Use the [official design tokens](../project-ui-design-system/references/design-tokens.md)
as the single palette source. This skill adds project-specific state and component
rules; do not duplicate the palette or globally remap legacy numbered variables.
Explicit user instructions and verified business behavior take precedence.

## Essential rules

- Alternate ordinary prefactor and invoice cards between light teal and light
  purple, independent of approval status. Unsuccessful/invalid documents use a
  gray variant that overrides the alternating variant.
- Derive the border, side strip, icons, counters, timeline and secondary controls
  from one card variant. Keep inner information panels white and text readable.
- Approval of the related order must not turn the entire prefactor green. Show
  approval through its description and icon, using the card's darker accent.
- Keep compact cards, clear borders, consistent heights, restrained shadows and
  accessible actions. Color supplements explicit status text; it never replaces it.
- Preserve every permission/status condition, callback, identifier, financial
  calculation and document relationship when restyling.

Read [document variants and state mapping](references/document-states.md) when
working on prefactors, invoices or timelines. Read
[component rules](references/components.md) for other theme work and interactions.

## Implementation and verification

1. Inspect the affected Razor, shared partials, loaded CSS and actual state enums.
   Treat the source pointers in the references as discovery aids, not immutable APIs.
2. Follow [the design system](../project-ui-design-system/SKILL.md),
   [frontend](../crm-frontend/SKILL.md) and
   [frontend reusables](../crm-frontend-reusables/SKILL.md).
   Use [responsive](../crm-responsive/SKILL.md) for layout/breakpoints and
   [typography](../crm-typography/SKILL.md) for fonts, weights and text overflow.
3. Reuse matching component tokens and markup; add only scoped styles needed for
   the requested change. A theme task does not authorize a global migration.
4. Check alternating, invalid, pending, approved, canceled and returned examples;
   long Persian names/amounts; zero and maximum permitted actions; keyboard focus;
   reduced motion; modal/dropdown overflow; and supported viewport/container widths.
5. Run relevant checks and report results truthfully. Documentation creation does
   not apply these rules retroactively to runtime CSS. Distinguish documented policy,
   implemented behavior and browser-verified behavior in the delivery.
