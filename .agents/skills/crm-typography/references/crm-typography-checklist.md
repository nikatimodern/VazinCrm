# Typography verification checklist

## Source and font selection

- [ ] Trace the affected view/partial, layout, CSS order and dynamic templates.
- [ ] Use the real IRANSans face/weight pair; check inherited weight and `font:`
  resets. Do not assume 600/800 or a variable font exists.
- [ ] Reuse existing font faces/helpers; add no duplicate font declaration or
  unnecessary font download. Keep icon-font glyphs intact.
- [ ] If loading changed, verify requested resources, fallback appearance, loading
  shift and the browser's rendered font. File existence is not evidence of use.
- [ ] Compare size/leading with the documented scale; explain an exception by
  component need. Do not add scattered inline values or `!important` overrides.
- [ ] Check real root size and any loaded density CSS before using rem or scale.

## Readability and content

- [ ] Persian letters, half spaces, punctuation and descenders remain legible;
  line height does not crop glyphs. Avoid letter spacing on Persian prose.
- [ ] Mixed Persian/Latin names, URLs, phones and codes have coherent ordering.
- [ ] Zero, negative amounts, grouping, precision, currency, long serials and dates
  remain complete and correct. Presentation changes do not rewrite stored values.
- [ ] Long titles/names, paragraphs, button labels, badges and validation wrap or
  truncate deliberately. Full essential content is available on focus/touch/details.
- [ ] Labels remain persistent; placeholder/help/error sizes remain readable.
- [ ] Table headers/body, chart labels, legends and canvas font settings use
  compatible roles. Check after redraw, asynchronous font load and data refresh.
- [ ] Mobile body/controls are not reduced to tiny metadata text to fit layout.
  Check input-focus zoom and usable labels on a real device when available.

## Scope and visual checks

- [ ] Check affected mobile/tablet/desktop sizes and density boundaries using the
  [responsive checklist](../../crm-responsive/references/crm-responsive-checklist.md).
  Do not duplicate or redefine its breakpoint policy.
- [ ] Check browser text zoom at 200%, Persian long text, keyboard focus and
  touch access; information remains readable without clipped essential actions.
- [ ] Inspect shared callers, dropdown portals, modal open/reopen, tabs, toast and
  error states where the changed typography reaches them.
- [ ] No unrelated page restyling, duplicate typography tokens, global resets,
  new font/package, changed permissions or financial formatting logic.
- [ ] Review the final diff against the requested scope and existing skill rules.

Report the source checks, browser/device sizes and interactions actually tested.
If a browser was unavailable, explicitly say visual verification was not performed;
compilation, syntax validation and source inspection do not establish visual success.
