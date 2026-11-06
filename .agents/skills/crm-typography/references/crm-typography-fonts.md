# Font families, loading and script direction

Paths below are relative to `03.EndPoints/CRM.EndPoint.FrontEnd`.
Source inspection: 2026-11-07. Recheck declarations and layout inclusion before editing.

## Existing font definitions

`wwwroot/css/site.css` sets `body` to `IRANSans` and declares these faces:

| CSS family | Declared weight | Existing file under `wwwroot/Fonts/IRANSans/woff2/` |
| --- | --- | --- |
| `IRANSans` | normal / 400 | `IRANSansWeb(FaNum).woff2` |
| `IRANSansMedium` | 500 | `IRANSansWeb(FaNum)_Medium.woff2` |
| `IRANSansBold` | bold / 700 | `IRANSansWeb(FaNum)_Bold.woff2` |

These are three separate CSS families, not three weights registered under
`IRANSans`. Setting only `font-weight: 500` or `700` on `IRANSans` does not select
the named Medium/Bold face and may produce synthetic weight. For scoped new text,
use the correct existing family and its matching weight. Preserve `.fbold`,
`.fMedium`, `.main-font` callers where suitable; these helpers set families with
`!important`. Headings, `strong`, `b` and `th` also receive the Bold family globally.

`wwwroot/css/supplyFactor.css` registers `SupplyFactorSans` at 400/500/700 using
the same three files, with `font-display: swap`. Reuse its weight-based family
inside existing supply-factor components. Do not copy this alias to other modules
or remove it as part of an unrelated typography change.

Suggested fallback stacks for future scoped adoption:

- Body: `"IRANSans", Tahoma, Arial, sans-serif`, weight 400.
- Emphasis: `"IRANSansMedium", "IRANSans", Tahoma, Arial, sans-serif`, weight 500.
- Headings: `"IRANSansBold", "IRANSans", Tahoma, Arial, sans-serif`, weight 700.

Fallbacks are recommendations; they are not a new font download requirement.
Do not apply a body font selector to icon fonts, SVG text indiscriminately, or all
descendants of widgets. Preserve Font Awesome glyph families and chart settings.

## Loading and duplication

- `_Layout.cshtml` loads Bootstrap RTL before `site.css`, then feature CSS and
  `menu.css`. `site.css` is the main UI font owner. Other layouts/print documents
  must be checked separately; the mere presence of a file does not mean it loads.
- Keep existing font-face declarations central. Do not embed another copy in a
  partial, modal, component or generated style block. Search declarations and
  consumers before proposing consolidation of `SupplyFactorSans` and the aliases.
- The three shared faces currently have no explicit `font-display`. If font
  loading is in scope, assess `swap` with fallback text and layout shift before
  changing the owner declaration. Do not prescribe `block`, preload every weight,
  or add unrelated font files. Verify network requests and rendered fonts.
- Files also present: FaNum Light, UltraLight and Black; non-FaNum regular,
  Light, Medium and Bold. They are not registered by the shared `site.css` faces.
  Do not infer numeric weights from filenames or activate them for ordinary UI.
- No variable font was identified in the inspected application font declarations.
  Prefer the verified 400/500/700 choices; existing 100/600/800/900 declarations do
  not prove corresponding IRANSans faces exist.

## Other observed families and inconsistencies

- `wwwroot/Fonts/fonts-fa.css` defines `WMitra` regular/bold and `WYekan` regular,
  plus a legacy `FontAwesome` face. Mitra/Yekan font files exist. Keep these separate
  from the CRM UI standard; inspect print/report consumers before changing them.
- `Login.css` names `YekanBakh` and then overrides that declaration in the same
  `.mbody` rule with a system stack (Segoe UI, Roboto, Helvetica Neue, Arial,
  Noto Sans and generic sans-serif). No YekanBakh file/face was found in the
  inspected project font sources; do not treat it as an available CRM family.
- Normalization/library styles include `sans-serif` and `monospace`; keep those
  separate from application choices. Audit vendor CSS without rewriting it.
- `IRANSansBold`, `IranSansBold` and lowercase spellings occur in CSS/Razor.
  CSS family names are case-insensitive: casing alone is not a missing-font bug.
  Prefer canonical spelling in touched code. Mixed quotes and repeated inline
  declarations are maintenance issues, not proof of incorrect rendering.
- Some family stacks end in `serif`; `font:` shorthands also reset family, weight
  and line height. Check shorthand and inherited styles before diagnosing a font.

## Persian, Latin, numerals and business values

- Keep Persian prose RTL and `lang="fa"` in the normal shell. Use readable Persian
  punctuation and preserve half spaces in UI text; do not add letter spacing to
  connected Persian text. Do not rewrite stored names or identifiers to fix layout.
- Isolate mixed values with `<bdi>` or a scoped `dir="ltr"` when their content is
  intrinsically LTR: phone numbers, URLs, email, codes and machine identifiers.
  Keep adjacent labels/currency in the natural Persian reading order. An isolation
  element affects ordering, not the font's numeral glyph selection.
- The shared FaNum files influence visible digit shapes. Preserve current display
  formatters and exact values. Do not convert API values, input values, exports,
  decimals, signs or serials merely to change numeral appearance. For explicitly
  required Latin glyphs, inspect the available non-FaNum face and existing loading
  first; do not claim `direction:ltr` produces Latin digits.
- Keep the minus sign attached to its amount, use existing grouping/precision,
  distinguish zero from missing values, and keep units readable. Dates/calendars
  and financial formulas remain unchanged by typography work.
- `font-variant-numeric: tabular-nums` can help aligned numbers only if supported
  by the chosen face; verify it visually rather than assuming digit features.
- Apply the same verified family choices to table cells, labels, inputs, modal
  text and dashboards. Canvas/chart typography needs its own font configuration;
  body CSS does not automatically govern canvas-rendered labels.
