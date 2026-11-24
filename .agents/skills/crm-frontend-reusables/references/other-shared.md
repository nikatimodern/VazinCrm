# Other shared JavaScript

Paths below are under `03.EndPoints/CRM.EndPoint.FrontEnd/wwwroot/js/`. These scripts are included by specific layouts/pages; verify script loading before use. This reference is selective; omitted functions remain available when their source and dependencies fit. Feature-specific files (`order.js`, `factor.js`, `cash.js`, and similar) are not general-purpose contracts.


## `File.js`

Shared upload/preview surface: `processUploadFiles(files,listName,previewIndex,containerUploader)` is async; `FileUpReload(fUpId)`, `ShowPreview(indexEle)`, `createImageBox(fileItem,key=null,indexEle=null,showDelete=false,showWatermark=false,watermarkText='')`, `delFile(index,indexEle)`, `clearUpload(inputId)` manage the existing file UI. `addToList(files,listName,progressElement)` is async; the file contains two declarations under this name, with the later declaration taking effect. Review that implementation before calling. `openDocumentViewer(url)`, `closeDocumentViewer()`, `downloadFileViewer({url,filename='file',extension=''})`, `downloadFile(btn,url,fileName)` (async), `triggerDownload(url,fileName)` cover viewing/download. `isImageFile(fileItem)`, `isPdfFile(fileItem)`, `getImageSrc(fileItem)`, `getBackgroundImage(fileItem,isImage,isPdf)`, `preloadImage(src)`, `setupViewer(wrapper)` support previews. `createUploadProgress(containerUploader)`, `updateUploadProgress(progressElement,percent,text)`, `removeUploadProgress(containerUploader)` support progress UI. These rely on the file uploader's global state and DOM; inspect the page setup.

## `Layout.js`

This script holds both shared layout behavior and feature-bound workflows. Shared candidates include `dateTimePicker(container=document)` and `Mask()` for date/input initialization; `getAllBankAccountsJson(rerender,type)` and `getAllBankAccounts(selects,rerender=false)` for bank selectors; `buildBankAccountText(item)` / `buildBankAccountUi(item)` for their display;  and `showAttachments(id,typeAtt)` for attachment UI. `ShowFilterPopup(filterBtn,filterItemBlackList=[])` implement the existing filter system. Inspect their required global state/DOM before reuse.

The shared bank-account modal is rendered once in `Views/Shared/_Layout.cshtml`. Use `toggleAddBankAccountModal({ id?, bankAccountType?, onSaved? })` from any page. Without `id` it opens the creation form; with `id` it loads that account for editing. The optional `onSaved(account)` callback is awaited after a successful save and is responsible for refreshing page-specific tables or account selectors. `toggleAddBankAccountModal()` also closes the modal when already open. The global `submitAddBankAccount()` performs validation, including a role-dependent required TomSelect for persons from the `Customers` table, and sends the existing accountancy command. Keep request-amount-specific refresh logic in `requestAmount.js`; do not add references to request-amount DOM inside the shared modal handlers.

`ShowFilterPopup` resets the shared date checkbox's disabled state on every open. It is locked only for `filterDashboardConfig`, where a date range is required; other pages can toggle the date filter after navigating from the dashboard.

`onSelectFilterDate` clears the quick-date selection state and removes `bgGreen2` from the filter's preset buttons. It is called by the date picker and by delegated `input`/`change` listeners on `DateFilterDivContent`, covering calendar selection, typing, pasting, and clearing. `getDateFilter` assigns input values directly, so selecting a preset keeps its button highlighted.

## `deal/deal-documents.js`

`refreshDealFactors()` preserves the return value from `getAllFactorsModal(dealId, 4)`. After attempting to reload the current order's invoice list, it dispatches `crm:deal-factors-changed` on `document` with `{ dealId }` in `event.detail`. Existing successful invoice mutations call this refresh path. The notification is also emitted when the list request returns `false`, because the preceding mutation may already have succeeded. Page-specific listeners must match the order ID and respect the page/modal lifecycle; pages without listeners retain their existing behavior. Quality Control uses this notification to fetch fresh invoice summaries without resetting unsaved questionnaire fields.

## `accountancy.js`

After `refreshInvoiceSuccess()` reloads a customer payment list (`Invo_type === 5`), it dispatches `crm:customer-payments-changed` on `document` with `{ customerId: Invo_id }` in `event.detail`. The Quality Control page matches that customer ID and refetches its summary without clearing unsaved answers. Other payment-list callers do not need to handle the event.

## `deal/deal-details-modal.js`

Loaded once by `_Layout.cshtml` with `Modals/_DealDetailsModal`. Add `data-deal-details` to a same-origin link to `/Deal/Details/{id}` to open the shared Bootstrap dialog. Ordinary clicks load the existing document page with `embedded=true` in an isolated iframe, keeping document-modal globals separate from the calling page. Modified clicks preserve normal link navigation. Closing unloads the iframe and restores focus to the link. The server still enforces deal visibility through the existing data endpoint; the attribute grants no access. Embedded-only CSS hides the surrounding application shell.

## `bottom-sheet.js`

`new BottomSheet({title='',content='',closeOnOverlay=true,closeOnEsc=true,showCloseButton=true,destroyOnClose=false,className='',snapPoints=[],actions=[]})` creates a sheet. Actions accept `{text,html,className,close=true,onClick}`. Instance methods `open`, `close`, `destroy`, `setTitle`, `setContent`, `setActions`, and `on` are synchronous; `on(event,callback)` registers lifecycle events `beforeOpen`, `open`, `beforeClose`, `close`, `drag`, `snap`, and `contentChanged`. Only one active sheet is tracked through `BottomSheet.activeSheet`. The file and its page CSS must be loaded before use; inspect current methods before calling.

## `tomSelectOverflow.js`

Loaded immediately after Tom Select in `_Layout.cshtml` and `_CustomerLayout.cshtml`. Existing `new TomSelect(...)` calls automatically get viewport-aware dropdown positioning when the control is inside a clipping container. The dropdown uses a manual popover where available to preserve its DOM parent and scope. Older browsers use a fixed dropdown hosted in the nearest Bootstrap modal or the document body for the duration of the opening. Tom Select retains ownership of searching, selection, keyboard behavior, and lifecycle.

## `PublicFunction.js`

`renderExternalFinancialPreview({ fields, columns, rows, totals, detailsHtml? })` renders the shared summary and totals. Omit `detailsHtml` to keep the standard table. Pass trusted, feature-rendered markup with dynamic values escaped when the preview needs a custom detail layout.

`renderExternalFinancialPreviewTable(columns, rows)` renders the same preview table separately for layouts with multiple tables. Use it inside the shared preview wrapper to inherit table styles. It escapes all headings and cell values.

`appendExternalItemEditRow(list, { itemTitle, row, quantity, fee })` adds an item matching row with the document row number, quantity, and unit price under the item title. It returns the container for the destination item and warehouse controls.

## `externalFinancialWizard.js`

Shared UI class for transferring CRM documents to financial software. `new ExternalFinancialWizard({ modal, container, back, confirm })` takes DOM nodes or IDs of the existing Bootstrap wizard controls and relies on `renderExternalFinancialStage` from `PublicFunction.js`. Static helpers: `ExternalFinancialWizard.selectField({ id, label, options, selected, placeholder })` renders the same searchable select markup for quotation and invoice steps, while `ExternalFinancialWizard.crmCustomer(name)` renders their shared CRM-side identity. For sales quotation and direct sales invoice, `ExternalFinancialWizard.linkedStockField(target, { id, softwareId, cache, isCurrent, onChange, onLoaded, onLoading, retainedStock })` appends a dependent warehouse TomSelect below each item; it stays disabled until an item is selected and requests only its linked stocks via `/Factor/PrepareExternalStockMapping`. `retainedStock` is an optional original `{ itemId, stockId, title }` from a Sepidar quotation; it preserves that one previously mapped item/stock option even when inactive, without expanding available replacement stocks. Cache maps are destination-scoped and stale asynchronous results are ignored. Methods: `render({ destination, position, title, subtitle, icon, step, content, large })`, `setActions({ confirmText, backText, confirmDisabled, backDisabled, backHidden })`, `clearSelects()`, and `destroy()`. It clears active TomSelect instances before replacing a stage, and removes expanded modal width when destroyed. It does **not** own business validations or submit HTTP requests.

For sequential transfers, `setDestinations(ids)` deduplicates the selected software IDs; `currentDestination`, `hasNextDestination`, `nextDestination()`, and `positionLabel` track progress. These helpers do not persist anything or skip unsuccessful destinations. Call the feature-specific service only after the user confirms each destination. Loaded by `Views/Shared/_Layout.cshtml` immediately after `PublicFunction.js`, before `preFactor.js` and `factorExternalSoftware.js`. Existing quotation, invoice, and supply workflows keep their own API calls and validation; the cash transfer still uses the shared destination chooser and confirmation because it has no staged preview UI.

Financial wizard stages apply the shared `.required-mark` class. Set `required: true` on `selectField` or `linkedStockField`, or set the native `required` attribute on custom controls. Use `required-label` inside a `required-mark` container for required control groups or labels separated from their controls. Keep step and server validation in the feature.

## `preFactorExternalSoftware.js`

Feature-specific quotation export actions previously located in `preFactor.js` (the block starting with `sendPreFactorToExternalSoftware`) now live in this separate global classic script. It is loaded immediately after `preFactor.js`. The global entrypoint and all API request contracts remain unchanged. Reuse the shared `ExternalFinancialWizard` class for modal presentation, but keep the quotation's business state and step validations here; invoice and supply use their own feature-specific flows.

## `externalFinancialHistory.js`

`ExternalFinancialHistory({ modal, list })` manages the shared financial-history partial. Use `render(records, { emptyText, card })` with a card factory that returns `{ title, fields, registration?, view?, operations?, action?, related?, status?, missing? }`. The common header keeps the view action visible and opens edit and destination deletion in the document operations menu. Pass the missing-mapping cleanup control as the visible card action. Use `action(label, { href?, onClick?, danger?, icon? })` to build controls. Use `related: { title, entries?, load?, action?, open?, emptyText? }` and `relatedItem({ title, fields, registration?, view?, action?, status?, missing? })` for related documents and follow-up actions. Pass two document fields and a registration object with date and by for the shared two-row layout. A lazy loader returns related DOM nodes or null on request failure. Each module owns its requests and permissions. Quotations, sales invoices, supply invoices, and deposits use `wwwroot/css/externalFinancialHistory.css`.

`await ExternalFinancialHistory.chooseDestinations({ ids, mapType, signal? })` uses the existing `showExternalSoftwareModal` selector. It **does not hide the underlying history modal**. It waits for the selector's Bootstrap `hidden.bs.modal` event before returning selected software IDs; returns `null` on cancellation, blocked destinations, or abort, and guards concurrent invocation. Feature modules then start their own export wizard with the unchanged server API and document-specific validation. The global modal stacking handler remains in `Layout.js`; do not add custom backdrops or manually close history to open the destination chooser.

For sales quotation and invoice history, missing target documents are returned as records with `isMissing` and a scoped `mappingId` (not silently omitted). The common `ExternalFinancialHistory.missingMappingAction({ modal, sourceId, softwareId, mappingId, softwareTitle, url, onDeleted })` renders a confirmation-required removal control; it submits the modal anti-forgery token and refreshes history on success. The server must verify the mapping belongs to the exact CRM record and destination and **recheck the target database before deleting only the CRM ExternalMapping**. A failed target lookup caused by a database exception must never be interpreted as proof of deletion. Supply has its own existing missing-record handling and must continue to use its current CQRS command.
