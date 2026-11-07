# Document variants and state mapping

## Variant selection

Resolve a document's semantic state before selecting its visual variant:

1. An explicitly unsuccessful, invalid or canceled document gets the gray variant.
2. Other prefactor/invoice cards alternate light teal and light purple by their
   position in the rendered document list. Gray cards retain their list position;
   they do not restart alternation. Ignore toolbar/empty-state siblings when counting.
3. Approval, payment progress and the related order's success do not override parity.

Use primary-light/primary for teal, secondary-light/secondary for purple and
background/gray-dark for gray, from the official token reference. For operations
on purple cards, use the existing subdued dark-purple treatment rather than bright
purple icons: inspect the current `--pf-operation-accent` definition. A successful
status is information; alternating purple is not a pending-state declaration.

The selected variant controls the outer tint, strong outer border, official/nonofficial
side strip, ordinary operation icons, hover orbit, tooltips, invoice counters,
secondary buttons, confirmation description and completed/current timeline accents.
Do not leave teal connectors or bright purple icons inside gray cards. Preserve
semantic danger controls and important validation cues as explicit exceptions.

## Verified state distinctions

Inspect the current enums before implementation; compare enum members rather than
matching display strings or magic integer values.

| State or condition | Theme rule |
| --- | --- |
| `PreFactorEnum.فاقد_اعتبار` | Gray card and related accents; the visible `فاقد اعتبار` badge has danger/magenta background and white text. |
| Explicit unsuccessful prefactor | Gray card. Map the existing failure condition in that feature; there is no literal `ناموفق` member in the inspected `PreFactorEnum`. |
| `PreFactorEnum.حذف_شده`, if shown | Gray document with its actual deleted label; retain existing availability rules. |
| `PreFactorEnum.برگشت_خورده_از_طرف_مدیر_فروش` | A rejected/returned prefactor uses gray while this rejected state is active; keep its explanation and any allowed correction actions. |
| `PreFactorEnum.بسته_شده` | Do not infer failure from closure alone; inspect whether the workflow completed successfully or failed. |
| Waiting for supply/customer/sales/finance/production | Ordinary alternating variant, explicit waiting label and actual progress. |
| Financial rejection substep | Keep a clear rejection message. Use gray when the displayed document's effective state is rejected/unsuccessful; do not overwrite the prefactor's business status or silently classify a recoverable substep as terminal failure. |
| `FactorStatus.ابطال_شده` | Gray invoice and related accents, explicit cancellation label; a void/delete action can remain magenta. |
| `FactorStatus.در_دست_تایید`, `FactorStatus.تایید_شده` | Ordinary alternating variant with the actual pending/approved status. |
| Return invoice or an invoice with returned items | A return is a financial document, not automatically an unsuccessful invoice. Retain explicit return text/magenta semantic cue; use gray only when its effective document state is canceled/unsuccessful. |

Never infer failure from unpaid amounts, overdue dates, missing attachments,
incomplete timeline steps or a permission-disabled action. Do not invent a new
business state to satisfy theme styling. Gray presentation does not disable valid
actions and is not a reason to reduce the opacity of the entire card.

These rules concern prefactor/invoice document cards. The order kanban has a
separate semantic palette: open purple, successful teal, unsuccessful magenta.
Do not turn every failure indicator in the application gray.

## Card structure and timeline

- Keep prefactor serial and related order serial distinct: one order can have
  several prefactors. Display date/validity once. Give serials more room than dates.
- Keep the compact facts row on large cards; at intermediate widths use a balanced
  two-by-two arrangement rather than three fields plus an isolated fourth field.
- Arrange information from the right: customer, financial, invoices. Financial
  information can be narrower. The invoices panel uses the same information-card
  appearance. Stack internal/Sepidar controls on wide cards; place them side by
  side on narrow cards according to the existing responsive rules.
- Show all timeline stages, including the next stage, in a compact region. Use the
  document accent for completed checks/connectors and the current marker; neutral
  gray for future stages. Preserve actual workflow completion conditions.
- Fit captions with the responsive/typography rules; do not hide stages, introduce
  horizontal timeline scrolling or shrink unrelated text to fit a fixed width.

## Implementation evidence and gaps

Paths are relative to `03.EndPoints/CRM.EndPoint.FrontEnd/`:

- `Views/PreFactor/_PreFactorCards.cshtml`, `_PreFactorActions.cshtml`,
  `_PreFactorFactorsCountPartial.cshtml`, `_PreFactorDescription.cshtml`,
  `_PreFactorProgressTracker.cshtml`: shared prefactor structure and controls.
- `Views/Factor/_FactorCards.cshtml`: invoice cards and financial controls.
- `Views/Cartable/GetPreFactorFinancialExpert.cshtml`,
  `GetPreFactorSalesAdmin.cshtml`, `GetPreFactorTaminExpert.cshtml`: role-specific cards.
- `wwwroot/css/CartableStyle.css`: `.pf-accepted-card`,
  `.pf-accepted-card--invalid`, `.factor-card--returned` and `--pf-*` tokens.
- Domain enums: `01.Core/CRM.Core.Domain/PreFactors/Enum/PreFactorEnum.cs` and
  `01.Core/CRM.Core.Domain/Factors/Enum/FactorStatus.cs` (repository-relative).

At skill creation, the shared prefactor renderer marks only `فاقد_اعتبار` as
`--invalid`; the invoice renderer marks returns but has no canceled-state gray
variant. Returns currently have a whole-card magenta variant. These are existing
implementation details, not evidence that all policy above is already implemented.
When asked to update those components, apply this policy with state-specific
selectors and preserve return-document meaning. Do not edit runtime as a side
effect of maintaining this skill.
