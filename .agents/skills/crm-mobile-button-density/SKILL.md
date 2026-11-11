---
name: crm-mobile-button-density
description: "Apply to EVERY new or modified CRM page button, action, tab, filter-button, form button, and modal action when working on frontend UI. Enforce the user's compact mobile button sizing even when not explicitly requested. Use alongside crm-compact-form-ui, crm-responsive, crm-typography, and the project UI design system; do not alter non-size styling."
---

# CRM Mobile Button Density

## Goal and activation

Keep **ordinary text/action buttons across CRM pages compact on mobile**, using the existing small Cartable tabs as the reference. This is a **project-local, additive** skill; it does not replace other frontend skills.

Applies to frontend Razor/HTML, CSS, and JavaScript-rendered controls in the page, modal and drawer UI whenever a button is added or modified, whether or not "mobile" appears in the request.

## Reference dimensions (mobile only)

At CSS viewport widths **up to 767px**:

| Allowed property | Default |
| --- | --- |
| Visible single-line button height | Approximately **23px**; avoid clipping long labels |
| `min-height` | **23px** for ordinary text actions |
| `font-size` | **11px** (retain current font family and weight) |
| `padding` | **1px 5px** |
| `margin-block` | **0**; preserve intentional horizontal spacing |

A button with a long or wrapped label may exceed 23px naturally. **Do not impose fixed heights that clip text, icons or validation messages.** Existing controls already smaller and working well need not be enlarged.

## Permitted changes — strict scope

Change **only height / min-height, font size, margin and padding** of buttons. Never change their width, max-width, colors, backgrounds, borders, radii, shadows, font families, font weights, line heights, icons/SVG dimensions, positioning, events, permissions, or JavaScript behavior as part of this skill. Do not alter surrounding cards, lists, grids, input fields or spacing between unrelated elements.

Desktop and tablet widths **768px and above** are unchanged.

## Runtime baseline

- Shared rule: `03.EndPoints/CRM.EndPoint.FrontEnd/wwwroot/css/mobileButtonDensity.css`, loaded by `Views/Shared/_Layout.cshtml`.
- Targets ordinary buttons in `#mainContent`, modals, offcanvas and confirmation dialogs, and project-specific text-action classes. Prefer using that baseline instead of copying button rules into each page.
- For a nonsemantic element that is nevertheless an action, add `.crm-mobile-dense-action` where appropriate; prefer actual `<button>` elements when building new UI.
- For a specialized control that must retain its dimensions, use `.crm-mobile-density-exempt` and document the reason locally.
- Exempt icon-only controls, Excel/print/filter icons, close controls, pagination, and mobile shell/navigation controls. Keep their hit areas and functionality intact.
- Check the final cascade: feature styles and inline `!important` values may override the baseline. If needed, adjust the **same four properties only** in the affected component's mobile rule. Do not change other properties to force a match.

## Coordination

Follow `crm-responsive` for the 767/768 breakpoint, `crm-typography` for established Persian typefaces, `crm-compact-form-ui` for form density and touch ergonomics, and `project-ui-design-system` for visual design. An explicit user request or accessibility requirement for larger tap targets takes precedence over the 23px visual reference.

When changing a page, review Persian text visibility, desktop unaffected, focus states and tappability, icon-only exceptions, modal actions, and the button's complete label at narrow widths. Report what was verified in the code and what still needs mobile browser testing.
