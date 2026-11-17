# Other shared JavaScript

Paths below are under `03.EndPoints/CRM.EndPoint.FrontEnd/wwwroot/js/`. These scripts are included by specific layouts/pages; verify script loading before use. This reference is selective; omitted functions remain available when their source and dependencies fit. Feature-specific files (`order.js`, `factor.js`, `cash.js`, and similar) are not general-purpose contracts.


## `File.js`

Shared upload/preview surface: `processUploadFiles(files,listName,previewIndex,containerUploader)` is async; `FileUpReload(fUpId)`, `ShowPreview(indexEle)`, `createImageBox(fileItem,key=null,indexEle=null,showDelete=false,showWatermark=false,watermarkText='')`, `delFile(index,indexEle)`, `clearUpload(inputId)` manage the existing file UI. `addToList(files,listName,progressElement)` is async; the file contains two declarations under this name, with the later declaration taking effect. Review that implementation before calling. `openDocumentViewer(url)`, `closeDocumentViewer()`, `downloadFileViewer({url,filename='file',extension=''})`, `downloadFile(btn,url,fileName)` (async), `triggerDownload(url,fileName)` cover viewing/download. `isImageFile(fileItem)`, `isPdfFile(fileItem)`, `getImageSrc(fileItem)`, `getBackgroundImage(fileItem,isImage,isPdf)`, `preloadImage(src)`, `setupViewer(wrapper)` support previews. `createUploadProgress(containerUploader)`, `updateUploadProgress(progressElement,percent,text)`, `removeUploadProgress(containerUploader)` support progress UI. These rely on the file uploader's global state and DOM; inspect the page setup.

## `Layout.js`

This script holds both shared layout behavior and feature-bound workflows. Shared candidates include `dateTimePicker(container=document)` and `Mask()` for date/input initialization; `getAllBankAccountsJson(rerender,type)` and `getAllBankAccounts(selects,rerender=false)` for bank selectors; `buildBankAccountText(item)` / `buildBankAccountUi(item)` for their display;  and `showAttachments(id,typeAtt)` for attachment UI. `ShowFilterPopup(filterBtn,filterItemBlackList=[])` implement the existing filter system. Inspect their required global state/DOM before reuse.

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

## `externalFinancialWizard.js`

Shared UI class for transferring CRM documents to financial software. `new ExternalFinancialWizard({ modal, container, back, confirm })` takes DOM nodes or IDs of the existing Bootstrap wizard controls and relies on `renderExternalFinancialStage` from `PublicFunction.js`. Static helpers: `ExternalFinancialWizard.selectField({ id, label, options, selected, placeholder })` renders the same searchable select markup for quotation and invoice steps, while `ExternalFinancialWizard.crmCustomer(name)` renders their shared CRM-side identity. For sales quotation and direct sales invoice, `ExternalFinancialWizard.linkedStockField(target, { id, softwareId, cache, isCurrent, onChange, onLoaded, onLoading, retainedStock })` appends a dependent warehouse TomSelect below each item; it stays disabled until an item is selected and requests only its linked stocks via `/Factor/PrepareExternalStockMapping`. `retainedStock` is an optional original `{ itemId, stockId, title }` from a Sepidar quotation; it preserves that one previously mapped item/stock option even when inactive, without expanding available replacement stocks. Cache maps are destination-scoped and stale asynchronous results are ignored. Methods: `render({ destination, position, title, subtitle, icon, step, content, large })`, `setActions({ confirmText, backText, confirmDisabled, backDisabled, backHidden })`, `clearSelects()`, and `destroy()`. It clears active TomSelect instances before replacing a stage, and removes expanded modal width when destroyed. It does **not** own business validations or submit HTTP requests.

For sequential transfers, `setDestinations(ids)` deduplicates the selected software IDs; `currentDestination`, `hasNextDestination`, `nextDestination()`, and `positionLabel` track progress. These helpers do not persist anything or skip unsuccessful destinations. Call the feature-specific service only after the user confirms each destination. Loaded by `Views/Shared/_Layout.cshtml` immediately after `PublicFunction.js`, before `preFactor.js` and `factorExternalSoftware.js`. Existing quotation, invoice, and supply workflows keep their own API calls and validation; the cash transfer still uses the shared destination chooser and confirmation because it has no staged preview UI.

## `preFactorExternalSoftware.js`

Feature-specific quotation export actions previously located in `preFactor.js` (the block starting with `sendPreFactorToExternalSoftware`) now live in this separate global classic script. It is loaded immediately after `preFactor.js`. The global entrypoint and all API request contracts remain unchanged. Reuse the shared `ExternalFinancialWizard` class for modal presentation, but keep the quotation's business state and step validations here; invoice and supply use their own feature-specific flows.

## `externalFinancialHistory.js`

`ExternalFinancialHistory({ modal, list })` manages the existing shared financial-history partial without replacing its IDs or changing the feature-specific HTTP actions. `show({ title, loadingText })`, `setTitle(title)`, `message(text)`, and `render(records, { emptyText, fields, actions })` present history consistently. Static DOM helpers `fields(entries)`, `actions(elements)`, and `card(entries, elements, extraClass)` are also used for purchase invoices with nested warehouse-receipt records. History actions remain owned by each feature. Styles for `external-pf-*` are maintained in `wwwroot/css/externalFinancialHistory.css` (extracted unchanged from `site.css`) and shared by quotations, sales invoices, and supply invoices.

`await ExternalFinancialHistory.chooseDestinations({ ids, mapType, signal? })` uses the existing `showExternalSoftwareModal` selector. It **does not hide the underlying history modal**. It waits for the selector's Bootstrap `hidden.bs.modal` event before returning selected software IDs; returns `null` on cancellation, blocked destinations, or abort, and guards concurrent invocation. Feature modules then start their own export wizard with the unchanged server API and document-specific validation. The global modal stacking handler remains in `Layout.js`; do not add custom backdrops or manually close history to open the destination chooser.
