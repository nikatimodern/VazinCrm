# Responsive verification checklist

Widths below are CSS viewport widths. Check the affected page at representative sizes: mobile `320`, `360`, `390`, `430`; tablet `768`, `820`, `1024`; laptop `1280`, `1366`; desktop `1440`, `1600`, `1920`, `2560`. Also check immediately below and above any breakpoint the change uses, especially `575/576`, `767/768`, `991/992`, `1199/1200`, `1399/1400`, and `1919/1920`.

## Rendered browser checks

- [ ] No unintended page-level horizontal overflow; intentional table/board scrolling is usable.
- [ ] Persian and mixed-direction text wraps without clipping; spacing and alignment remain coherent.
- [ ] Forms, validation, tables, chart labels, modals, and dropdowns fit and scroll where needed.
- [ ] Sidebar, mobile bottom menu, drawer, tabs, and action menus open, close, and remain reachable.
- [ ] Touch targets, focus indicators, and keyboard navigation are usable.
- [ ] RTL order, numeric content, icons, and popup alignment are correct.
- [ ] Internal scroll areas work in portrait, landscape, short heights, and with the mobile keyboard when available.
- [ ] JavaScript behavior still works after resizing and repeated open/close or redraw.

## Code checks

- [ ] Confirm the affected stylesheet is actually loaded and inspect cascade around existing `compact.css` and feature queries.
- [ ] Inspect responsive rules near breakpoint boundaries, fixed widths, `min-width`, overflow, and `!important` usage.
- [ ] Review affected event listeners and component lifecycle for responsive state changes.
- [ ] Record which widths and interactions were actually rendered in a browser. Code inspection alone is not visual verification; if no browser was available, say so explicitly.
