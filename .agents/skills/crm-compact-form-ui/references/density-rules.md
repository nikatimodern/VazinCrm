# Compact form density rules

These are **default targets**, not absolute pixel mandates. Prefer existing design tokens when they are already appropriately compact. Make reasonable exceptions for multiline, accessible, touch-first, and content-heavy components.

| Element | Desktop default target | Mobile guidance |
| --- | --- | --- |
| Text, number, date input and select | 34–38px visible height | Usually 40–44px; keep touch interaction comfortable |
| Primary and secondary action buttons | 32–36px visible height | Usually 40–44px and clear tap targets |
| Horizontal input padding | 8–10px | 10–12px if necessary |
| Vertical input padding | Around 6–8px, consistent with height | Size to target height; avoid clipped text |
| Label-to-control gap | 4–6px | 4–6px |
| Gap between adjacent field groups | 8–12px | 10–12px |
| Gap between major form sections | 12–16px | 12–16px |
| Form section/card padding | 10–12px | 10–12px, where content fits |
| Modal body padding | 12–16px | 12–16px |
| Modal title/header/footer padding | 10–16px | 10–16px |
| Label font size | 12–14px, per typography tokens | Readable; prefer project tokens |
| Input text size | 14–15px, per typography tokens | 16px when needed to avoid iOS input zoom |
| Border radius | Small / medium, project tokens | Same visual language |

## Layout

- A straightforward label + field should not consume a 110–150px-high bordered card.
- Use a two-column grid for ordinary fields **when space allows**; switch to one column on narrow screens based on the responsive skill.
- Choose the modal width from the actual fields and reading comfort, not merely viewport width. Do not force `modal-xl` or near-full-screen widths for a few controls.
- For single-line fields, keep any unit (e.g., ریال / نفر / درصد) adjacent to the input via an input group, suffix, or concise helper line; avoid an extra empty band.
- Avoid arbitrary `margin-top: 40px`, `padding: 24px`, `gap: 24px` on routine field groups. Add large spacing only where the information hierarchy genuinely needs it.
- Make footer buttons content-sized and consistently aligned. Avoid huge full-width buttons on desktop unless form semantics or existing product patterns require them.
- For long modal bodies, scroll **only** the appropriate content area; keep actions reachable and avoid trapping content behind sticky sections.
- Avoid hardcoded heights that cut off validation errors, localized strings, textarea content, or select/dropdown overlays.

## Screenshot anti-pattern (new target definition modal)

The supplied reference had overly tall controls, separate large bordered containers around every field, large vertical gaps (notably near the Persian year field), and a modal spanning almost the whole screen. These are **negative examples**. For similar annual/monthly target forms:

1. Keep the section heading and the Persian year field near one another.
2. Present metrics in a tight, consistent grid with plain label + control instead of one padded card per metric.
3. Show unit text inline or next to its input when feasible.
4. Use a narrow, content-appropriate modal width and a compact sticky-footer arrangement only if needed.
5. Preserve the four-step wizard, current values, RTL, focus states, responsive behavior, and all validation/submission logic.

## Exceptions

- Accessibility, browser zoom, user-requested larger controls, mobile touch requirements, or complex controls may require larger measurements.
- Multiline textareas are not forced into one-row height.
- A wizard with many fields may legitimately scroll; the goal is eliminating **unnecessary** scrolling, not all scrolling.
- Do not silently shrink target areas so much that checkboxes, radios, select menus, calendars, or file pickers become hard to operate.
