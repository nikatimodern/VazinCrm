# Vazin component theme rules

Use the official token reference for values. Follow current shared utilities and
component selectors; the following rules describe intended appearance and behavior.

## Cards and actions

- Keep compact spacing and avoid tall empty action panels. Use a clearly visible
  outer document border (the established document cards use 2px), lighter internal
  separators and restrained shadows. Do not force a fixed height that clips content.
- Order cards across open/successful/unsuccessful columns should have equal visual
  height for comparable content. A financial-approval badge must not enlarge the
  successful column's cards. Reserve consistent heading/action space.
- Long customer names can use one-line ellipsis where approved, with the full name
  available through an accessible detail/tooltip. Do not truncate financial amounts
  or rely on a mouse-only tooltip for essential information.
- Place the finance-link action in the operations toolbar. Keep space for all
  permission/status-dependent controls, including the maximum visible set.
- Use a horizontal compact operations row on normal/narrow cards and the existing
  vertical rail only on sufficiently wide cards. Tooltips appear below horizontal
  controls and to the right of vertical controls; support focus as well as hover.
- Ordinary operations inherit the card's dark accent. Confirm/save/pay use teal;
  create-order/create-person actions use theme purple; reject/delete/void use
  magenta; cancel/close use neutral styling. Do not erase semantic danger styling
  merely to make every button match an alternating card.
- Reuse available project icons; avoid unrelated icon families or packages.

## Motion and scrolling

- For animated document operations, reuse the main menu's actual orbit and bounce
  definitions after inspecting `wwwroot/css/menu.css`. The outline follows the
  full hover-box perimeter. Match its timing/easing rather than inventing a
  constant-speed approximation. Bounce up, land, pause, then repeat while hovered;
  avoid continuous up/down bobbing. Use the card accent for the orbit.
- Respect reduced motion, visible keyboard focus and mobile interaction. Animation
  must not shift layout or replace an action's accessible label.
- Scrollbars follow their column/card accent: teal, purple or magenta for the
  order columns; gray for neutral/completed regions where appropriate.
- Use the shared [inertia contract](../../crm-frontend-reusables/references/site-core.md#local-wheel-inertia)
  for scrollable new/touched UI, including dynamic Cartable tabs. Do not copy wheel
  handlers or run a second animation. Preserve native gestures, reduced motion,
  nested scrolling, boundary chaining and cleanup. Inertial scrolling is behavior,
  not a reason to add scrolling to content that can fit naturally.

## Tabs, searches and filters

- Use white/neutral surfaces, subtle theme borders, compact spacing and a clear
  teal selected tab. Counts sit inside the tab beside its title as compact badges;
  use soft purple when inactive and a readable light treatment when active.
  Avoid floating red count circles over tab corners.
- Place compact search and segmented filters in the same toolbar row when space
  permits. Wrap deliberately on small screens; active filter chips occupy their
  own row when needed. Keep search purpose clear and its icon/focus purple.
- Filter groups use light teal for selection and dark teal readable labels.
  Filter modal section borders must remain distinguishable from white surfaces;
  preserve readable disabled states. Customer/sales-expert/date section headings
  use the established bold font, not synthetic weight alone.
- Preserve filter state, clearing, date shortcuts, enhanced-select dropdowns and
  activation switches. Styling must not make disabled controls interactive.

## Tables, modals and shell

- Tables: pale teal headers with dark teal text, subtle neutral stripes, pale teal
  hover, clear separators and purple selected pagination. Preserve sorting,
  responsive expansion, document links, selection and all mobile-accessible fields.
  Group financial actions in a consistent row with wrapping, rather than scattered
  isolated buttons. Keep confirm, reject and allocations visually distinguishable.
- Modals: white surface, pale teal header, dark teal title, subtle separators and a
  compact coherent footer. Confirm teal, cancel neutral. Preserve Bootstrap focus
  management, close controls, body scrolling and dropdown placement.
- Main header: restrained teal icons on soft teal tiles, purple messenger/action
  emphasis and count badges; magenta only for genuine alerts. Scope changes to the
  intended shell, not every secondary header.
- Activities: pending cards can use soft purple, completed cards neutral gray.
  Preserve editing, drag/drop, selection and completion behavior.
- Ratings: retain gold/yellow stars as a meaningful rating convention; do not
  recolor them purple solely for palette uniformity.
- Keep loading, empty, validation and error states consistent with their semantics.
  Always retain explanatory text and accessible names.

## Review

Check actual rendered contrast, hover/focus, long Persian labels, visible counts,
all permission combinations, narrow layouts, zoom and reduced motion. Use the
responsive skill's supported sizes and typography skill's font hierarchy rather
than maintaining competing breakpoint or font scales here. A code-only check does
not establish that responsive appearance has been verified.
