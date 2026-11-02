# Official design tokens and component guidance

## Palette

This is the approved specification for new UI, not a claim that these variables are already shipped in application CSS. Earlier mockups are not authoritative where they differ from this palette.

```css
:root {
    /* Primary */
    --color-primary: #004944;
    --color-primary-action: #00665C;
    --color-primary-light: #E7F3F0;

    /* Secondary */
    --color-secondary: #5811B4;
    --color-secondary-light: #F3EDFC;

    /* Danger, errors, and discrepancies */
    --color-danger: #C2185B;
    --color-danger-light: #FCE8F0;

    /* Neutrals */
    --color-gray: #6C757D;
    --color-gray-dark: #545961;
    --color-gray-light: #838E98;

    /* Text */
    --color-text: #212529;
    --color-text-muted: #545961;

    /* Backgrounds and surfaces */
    --color-background: #F0F1F7;
    --color-surface: #FFFFFF;
    --color-border: #DDE1E8;

    /* Pure black */
    --color-black: #000000;
}
```

| Role | Usage |
| --- | --- |
| Primary | Main actions, save, confirmation, success, important interactive elements. Use primary-action for filled action buttons and primary for emphasis. |
| Secondary | Secondary actions, selection, selected menu elements, pending processes, limited emphasis. Avoid competing teal and purple emphasis everywhere. |
| Danger | Delete, validation errors, important warnings, discrepancies, sensitive or irreversible operations. |
| Gray | Secondary information, neutral actions, disabled controls, separators. Gray-light is not automatically suitable for small text. |
| Text | Default headings and body copy; prefer charcoal over pure black for long text. |
| Surfaces | Background for the page, white for cards/modals, border for subtle separation. |

## Actual source map

Paths below are relative to `03.EndPoints/CRM.EndPoint.FrontEnd/`. Inspected during skill creation; recheck when implementing.

| Area | Existing source and relevant facts |
| --- | --- |
| Layout | `Views/Shared/_Layout.cshtml`: Persian language, RTL body, shared menu/header, Bootstrap RTL followed by shared and feature CSS. Page styles and body use `template[data-page-styles]` and `template[data-page-body]`; preserve the page-navigation lifecycle. |
| Navigation | `Views/Shared/Menu/_Menu.cshtml` obtains menus through `IMenuService`, separating desktop, mobile bottom-menu, and mobile drawer items. `Services/Menu/MenuService.cs`, `Services/Menu/MenuIcons.cs`, `wwwroot/css/menu.css`, and `layoutResponsive.css` support this shell. Do not invent a replacement shell for a page redesign. |
| Shared styling | `wwwroot/css/site.css`, `table.css`, `layoutResponsive.css`, and `pageSkeleton.css`; the layout also loads feature files such as `dashboard.css`, `order.css`, and `cartableStyle.css`. Verify actual load order and selectors. |
| Existing tokens | `site.css` defines numbered `--color1` through `--color28`, dark/Excel colors, and `--shadow`. For example, `--color8` is `#04857c`, not official primary. `warehouse.css` defines separate `--warehouse-*` tokens. The semantic official palette above was not present in the inspected CSS. |
| Fonts | `site.css` declares IRANSans, IRANSansBold, and IRANSansMedium from `wwwroot/Fonts/IRANSans/woff2/` FaNum files; root font size is 14px. Preserve these fonts and Persian presentation. |
| Libraries | The loaded `wwwroot/lib/Bootstrap/bootstrap5.3.rtl.min.css` identifies itself as Bootstrap **5.3.8**. Loaded Font Awesome Pro solid CSS is **5.15.1**. Verify icon availability; inline SVG is also used in the layout. No new icon/font package is needed. |
| Buttons and badges | `site.css` contains `.buttonGreen3`, `.buttonPurple`, `.buttonCancel2`, and `.badge-*`. Legacy `.buttonGreen3` uses `#02514C`; `.badge-green` uses `--color8`, `.badge-red` uses `#851b1d`, and `.badge-purple` uses `rebeccapurple`. Reusing a class does not guarantee official-palette compliance. |
| Forms and modals | Shared `.form-control:not(.bootstrap-only)` styling is in `site.css`. `Views/Shared/Modals/` and `Views/QualityControl/Index.cshtml` demonstrate Bootstrap modal structure, scrollable dialogs, fields, and action footers. Legacy error-modal dark-red styling is not the official danger specification. |
| Tables | `table.css` styles shared tables and DataTables responsive controls. `Views/QualityControl/Index.cshtml` and `wwwroot/js/qualityControl.js` currently use a native table with search, filters, and pagination. Do not assume all modules use the same table plugin. |
| Shared behavior | `wwwroot/js/site.js` supplies `sendApiRequest` and shared feedback/formatting helpers. Use the frontend-reusables skill to discover actual contracts; preserve page cleanup and cancellation. |

## Incremental token adoption

- Search loaded shared and feature CSS for the semantic names and equivalent variables before adding anything. Reuse an existing matching definition; do not redefine it or create duplicate literal values unnecessarily.
- Similar-looking legacy colors are not interchangeable with the official values. Do not remap numbered legacy variables globally without inspecting consumers and obtaining approval for a broader migration.
- When an authorized component change needs missing tokens, define only the necessary tokens in one appropriate scope. A page-scoped definition is suitable for an isolated change; shared adoption belongs in a deliberately reviewed common location. Prefer referencing an existing equivalent token over duplicating its value.
- Do not overwrite an existing variable with a different meaning. Inspect its consumers and choose a scoped, compatible adoption strategy. Documentation updates alone must not inject the CSS block into the application.

## Component guidance

| Component | Design and behavior |
| --- | --- |
| Buttons | Primary teal; secondary purple or neutral gray according to importance; dangerous actions magenta; disabled neutral gray. Use clear labels and visible hover/focus; primary hover can use primary instead of primary-action. Disabled controls must also be functionally disabled. |
| Cards | White surface, subtle border, restrained shadow, radii consistent with adjacent components, and regular spacing. Avoid decorative clutter or imposing a new global radius scale. |
| Forms | Persistent labels, readable help text, clear required/disabled states. Validation uses danger plus an explanatory message, not a colored border alone. Preserve existing validation and prefer TomSelect for enhanced selects. |
| Modals | White surface, clear title, limited accent color, distinct actions, appropriate body scrolling and RTL. Reuse Bootstrap interaction and focus management. Choose mobile sizing/fullscreen only when suited to the form; check close controls, keyboard, and dropdown visibility. |
| Tables | Clear headers, subtle hover, sufficient cell spacing, readable state badges. Avoid saturating whole rows. Choose responsive scrolling, existing responsive controls, or mobile cards according to content and existing structure; preserve access to every necessary field/action. |
| Menus and filters | Limited teal/purple emphasis for selection; explicit active state and keyboard focus. Preserve responsive navigation and role-based visibility. |

Status pairs (foreground on background):

| Meaning | Foreground | Background | Additional cue |
| --- | --- | --- | --- |
| Success | `--color-primary` | `--color-primary-light` | Success text and optional check icon |
| Pending process | `--color-secondary` | `--color-secondary-light` | Pending text and optional clock icon |
| Error or discrepancy | `--color-danger` | `--color-danger-light` | Error/discrepancy text and optional warning icon |
| Neutral or unchecked | `--color-gray-dark` | `--color-background` | Neutral/unchecked text and optional status icon |

Choose status semantics from the business state: an unchecked item is not automatically a pending process. Color must never be the only indicator. Use accessible names for icon-only actions and hide purely decorative icons from assistive technology.

Keep spacing consistent with the existing module and Bootstrap utilities, prefer logical properties for RTL, allow Persian labels to wrap, and use comfortable mobile tap targets. Review actual text contrast, focus visibility, zoom, loading/error/empty states, and repeated interaction. Avoid unnecessary animation; respect reduced-motion preferences when adding motion.
