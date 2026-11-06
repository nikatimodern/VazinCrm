# Size scale, weights and line height

## Observed source baseline

Inspection covered CSS declarations, Razor markup and JavaScript templates under
`Views`, `wwwroot/css` and `wwwroot/js` (290 files at inspection). This is a static
inventory, including normalization styles in that CSS directory, not a rendered
size census; third-party `wwwroot/lib`, generated `bin/obj` and computed styles
were not included. Both explicit properties and `font:` shorthands matter.

- The most frequent explicit literal sizes were 12px, 13px and 11px; 10px, 14px,
  16px and 15px also recur. Titles use 16/18/20/22px; dashboard metrics include
  19–24px and container-based `clamp()` values.
- Other observed pixel literals include 8, 9, 12.5, 17, 19, 21, 24, 25, 26, 27,
  30, 32, 35, 72 and 140. Large values may belong to icons or special displays;
  inspect the selector instead of treating them as body-text defects.
- `compact.css` additionally contains 6.8, 10.21, 10.63, 11.06, 11.61, 11.91,
  12.51, 13.4 and 16.17px. These are density values, not a recommended type scale.
- Relative sizes include `.75/.8/.80/.82/.85/.875/.9rem`, 1rem and larger
  1.05/1.1/1.2/1.25/1.5/1.7/2rem; `em`, percentages, `small`, `large`, `x-large`
  and viewport/container-based `clamp()` also appear. Do not equate rem with px
  without checking the root.
- `site.css` sets `html` to 14px. `compact.css` contains 12.51px for
  768–1599.98px and 16.17px from 1600px. No direct `compact.css` reference was
  found in the inspected application source; verify actual loaded sheets before
  assuming those overrides apply. Their documented existence is not runtime proof.
- Weights found: normal/400, 500, 600, 700/bold, 800, 900, 100, bolder and inherit.
  Use the font reference to distinguish real registered weights from requested
  weights. Do not infer that every weight has a real font file.
- Leading ranges from 0/1/1.1/1.15/1.2/1.3/1.35/1.4/1.45 through
  1.5/1.6/1.65/1.7/1.75/1.8/1.9/2, plus `normal`, inherited values and fixed
  13px/24px/60px/1.25rem. Tight or fixed leading requires a content-specific check.

## Recommended scale for scoped new or redesigned UI

This scale is a documented default, not an automatic global replacement. Retain
approved compact components where a change is unnecessary. Prefer the nearest
existing role before inventing another size.

| Role / candidate token | Default size | Face / real weight | Unitless leading |
| --- | --- | --- | --- |
| Micro metadata / `--crm-text-xs` | 11px | Regular 400 | 1.5 |
| Help, secondary details / `--crm-text-sm` | 12px | Regular 400 | 1.6–1.7 |
| Dense table, badge, compact control / `--crm-text-compact` | 13px | Regular 400; Medium 500 for labels | 1.5–1.6 |
| Body, standard control / `--crm-text-base` | 14px | Regular 400; Medium 500 for actions | 1.6–1.7; controls 1.5 |
| Card title / `--crm-title-card` | 16px | Bold 700 | 1.5 |
| Section, modal title / `--crm-title-section` | 18px | Bold 700 | 1.5 |
| Page title / `--crm-title-page` | 22px | Bold 700 | 1.4–1.5 |
| Featured metric/title / `--crm-title-display` | 26px | Bold 700 | 1.4 |

Use 20px as a documented bridge for existing metric/modal designs where moving
to 18 or 22 would cause unnecessary visual churn. A current 13–15px compact card
heading need not become 16px outside a requested redesign. Use long-form leading
1.8 for multi-line reports and explanations. Avoid making an entire paragraph bold.

## Token adoption and exceptions

- No shared CRM typography token system was identified in the inspected source.
  Numbered color variables, local palette variables and `--ui-scale` are not
  typography tokens. Token names above and optional `--crm-leading-compact: 1.5`,
  `--crm-leading-body: 1.7`, `--crm-leading-long: 1.8` are proposals only.
- Do not inject these tokens into runtime CSS by loading this skill. On a future
  authorized implementation, search definitions and consumers first, reuse any
  established equivalents, and define needed tokens in one appropriate owner.
  A shared rollout must account for alias families and all affected callers.
- Values in the table are CSS-pixel defaults. If implementing with rem, calculate
  against the real root and check density boundaries; do not paste conversions
  based on an assumed 16px root or multiply a density factor a second time.
- Prefer component classes/tokens over repeated inline sizes, loosely bounded
  `vw` text or arbitrary decimal sizes. A bounded `clamp()` is appropriate for an
  actual fluid metric/title; keep endpoints tied to documented roles.
- Use explicit longhand declarations when `font:` would reset required family,
  leading or weight. Do not add `!important` to compete with `.fbold` or density
  rules; trace specificity and adjust the intended scoped owner.
- In new UI, keep essential content at least 12px; reserve 11px for secondary
  metadata. Existing 9px prefactor timeline captions and smaller density/footer
  text are legacy exceptions to review when touched, not templates for mobile.
  The user's approved compact timeline is not authorization to enlarge it globally.
- Do not shrink labels to fit a breakpoint. Choose wrapping/truncation with the
  component reference; have `crm-responsive` resolve widths/layout. Mobile body
  and controls retain their roles; check input focus/zoom on real devices before
  choosing platform-specific adjustments. Touch area size is a separate concern.

Known maintenance risks are duplicated inline 11–15px declarations, family-only
bold helpers mixed with numeric weights, tight leading on Persian text, and root
density changes combined with fixed px text. Record the affected source and
observed consequence before proposing any broader cleanup.
