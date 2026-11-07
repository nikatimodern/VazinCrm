# `wwwroot/js/site.js` contracts

Source: `03.EndPoints/CRM.EndPoint.FrontEnd/wwwroot/js/site.js`. Global classic script. Verify the current source and its script order on the target layout before changing or calling a contract. This reference is selective; omitted utilities are not prohibited.

## HTTP and feedback

| API | Inputs and output | Behavior / limits |
| --- | --- | --- |
| `sendApiRequest(options)` | `url`; `method='GET'`; `data=null`, `params=null`, `headers=null`, `responseType='json'`, `timeout=30000`, `signal=null`, `withCredentials=false`, `onUploadProgress`, `onDownloadProgress`, `onSuccess(response)`, `onError(error)`, `successMsg`, `showLoading=true`, `showError=true`, `returnFullResponse=false`. Returns `Promise<response.data|null>` or a full Axios response. | Uses Axios, AJAX/current-page headers, shared loading counter, default success toast for POST/PUT/PATCH/DELETE, error toast, and login redirect only for 401 with `x-auth-expired: true`. Cancellation/error returns `null`; callbacks are awaited. `onSuccess` receives the full response; `onError` receives the Axios error and replaces default error display. `showError:false` suppresses default error display when no `onError` exists. `successMsg:null` suppresses the default success toast. Loading closes before callbacks; callback exceptions are not converted into `null` (except exceptions inside `onError`, which are logged). Requires Axios, Toastify, and the loading UI where enabled. |
| `updateApiLoading(show)` / `handleApiError(err)` | `boolean` / Axios error; latter async. | Internal collaborators of `sendApiRequest`; prefer the request helper. |
| `FLshowLoading(show=true)` | Boolean; void. | Toggles shared loading element; prefer request-managed loading for HTTP. |
| `createToast(type,text,options={})` | `success|error|info`, text, Toastify options (`duration`, `node`, `close`, `gravity`, `position`, `stopOnFocus`, `escapeMarkup`, `style`); returns Toastify instance. | Shows immediately. `toastifySuccess(text,duration=3000)`, `toastifyError(text,duration=5000)`, `toastifyInfo(text,duration=3000)` are presets and return the instance. Success preset disables markup escaping; never pass untrusted HTML. |
| `showAlert(options={})` | `title`, `text`, `icon`, `theme`, `confirmText`, `confirmButtonColor`; `Promise<SweetAlertResult>`. | Informational SweetAlert. |
| `confirmAlert(options={})` | `title`, `text`, `icon`, `theme`, `confirmText`, `cancelText`, button colors, `onConfirm`, `onCancel`; `Promise<boolean>`. | Awaits callbacks. `deleteItem({url,callback,title,text})` builds a confirmation and sends DELETE; `confirmDangerousDelete(...)` asks user to type the Persian deletion phrase; inspect its full signature for that flow. |
| `createInlineAlert({type='info',message,dismissible=false,icon})` | Returns `HTMLElement`. | `message` is inserted as HTML; escape untrusted text with `escapeHtml(value)` first. |

```js
await sendApiRequest({
  url: '/Warehouse/AddWarehouse',
  method: 'POST',
  data: model,
  onSuccess: async () => {
    ModalToggle(warehouseModal, 'hide');
    await GetAllWarehouses();
  }
});
```

## Modal, selection, dropdown

| API | Inputs / output | Behavior / limits |
| --- | --- | --- |
| `ModalToggle(target,action='toggle')` | Element or ID string, `toggle|show|hide`; void. | Calls Bootstrap `getOrCreateInstance`. `isModalOpen(modal)` returns boolean for element or bare ID. `ShowTab(target)` activates Bootstrap tab. |
| `ModalSelector` | `new ModalSelector({modal,title,search,container,submit,cancel})`; `await open({title,selectedIds,load,onSubmit,onCancel,onSelectionChange,clearSearch,showSearch,size})`. `load({search,selectedIds})` returns HTML string or `{data:html}`. | Maintains checkbox/radio selection across search reloads. `getSelection()` returns `{ids,map,numbers}`; `reset()` clears state. Required modal child elements must exist; `open` requires `load`. `submit`/`cancel` callbacks are not awaited. |
| `SelectionManager` | `new SelectionManager({container,selectionBar,toggleBtn,itemSelector,checkboxSelector,getItemKey,isSelectable,itemsProvider,selectedClass,selectionModeClass,enableShiftSelect,onAction,onSelectionModeEnter,onSelectionModeExit,onSelectionChanged})`. | Multi-selection on items. Methods include `getSelectedIds`, `setSelectedIds`, `addSelection`, `removeSelection`, `clearSelection`, `refresh`, `destroy`; `destroy` removes listeners. Actual default `enableShiftSelect` is `false`. |
| `Dropdown` | `new Dropdown({button,content,portal,transitionDuration,zIndex,effect,originalDisplay,placement})`; instance `open/close/toggle/destroy`. Static `setup({toggle,content,onOpen,closeOnOutsideClick,options})` returns a controller or `undefined` if no toggle; `setupDelegate({container,toggleSelector,getContent,onOpen,options})` returns a state controller. | Moves content to `#dropdown-portal`, positions around scrolling. Check portal exists. Prefer delegated setup for dynamic lists; `setup` installs listeners, so do not initialize the same toggle twice. |
| `createOption(select,items,addDefault=true,defaultText='انتخاب',selectTarget=false)` | Select ID/element; items `{text,value,target?}`; void. | Rebuilds native select options; inspect TomSelect integration before using on an enhanced select. |

```js
const menu = Dropdown.setupDelegate({
  container: document.querySelector('#rows'),
  toggleSelector: '.row-menu',
  getContent: button => button.nextElementSibling
});
// menu.close() closes the active delegated dropdown.
```

## Forms, dates, numbers, DOM

### Local wheel inertia

`enableMomentumScroll(element, { signal } = {})` opts one `HTMLElement` into
wheel inertia and returns an idempotent cleanup function. Repeated attachment
returns the existing cleanup. It defaults to the current `PageRuntime` abort
signal when present; cleanup also runs on `pagehide`. Pass a lifecycle signal
for another host, or call the returned cleanup when disposing its component.

The shared tuning matches the approved order preview: 240ms exponential decay,
1.4 wheel-distance multiplier, 220px per-event cap and 420px queued-distance cap.
Pointer/keyboard/drag input, blur, visibility changes and reduced-motion changes
stop pending movement. Reduced motion, small pixel deltas (trackpad heuristic),
zoom/horizontal gestures, nested scroll regions and outer boundaries retain
native behavior. Editable controls and touch scrolling are untouched. Used by
deal columns and the customer directory's local scroll region.

`enableMomentumScrollScope(root, { signal } = {})` installs one non-passive capture
listener on a page/component root. On wheel input it attaches the same helper to
the closest actual vertical scroll region (`overflow-y: auto/scroll/overlay`),
before the event reaches that region. This covers the first wheel event and
future AJAX/DataTables/tab containers without repeatedly scanning descendants.
Keep the scope listener non-passive from initialization: otherwise the browser
can start a native wheel transaction before a region's listener is attached,
leaving the initial events non-cancelable and delaying inertia activation.
It returns an idempotent cleanup, deduplicates per root, prunes replaced regions
on later wheel input and releases its attachments on lifecycle abort/pagehide.
Cartable uses this scope for every tab under `#cartableWorkspace`. Content outside
the chosen scope is unaffected; there is no automatic global activation. Prefer
one scope per region instead of overlapping scopes or mixing direct/scoped owners
for the same scroll element.

| API | Inputs / output | Use |
| --- | --- | --- |
| `MyService` | Instance methods `emptyForm(formId,defaultVals=[],blacklist=[])`, `convertJsonToForm(js,formId,blackListArray=[],flashFilled=false)`, `convertFormToJSON(formId,blackListArray=[],elm=null,selector=null)`, `getJsonFromDiv(divEl,blackListArray=[])`, `CheckValidForm(formId)` async, `RemoveAlertForm(formId)`. | Form reset, hydration, serialization, validation. Check method result in source for the exact workflow. |
| `toggleElementsDisabled(elms,isDisabled=true)` / `toggleElementsRequired(elms,isRequired)` | Element, NodeList, or array; void. | Disabled helper also handles TomSelect. |
| `applyMinMaxValue(el)`, `handlerSeparateDigitNumbers(e)`, `handlerMobileInput(e)`, `handlerIranMobileInput(e)`, `handlerSeparateCurrencyNumbers(el)` | Input/event; void. | Input constraints and formatting. `getNumericValue(value)` returns English digits-only string; `SeparatedNumberRemove(val)` strips grouping separators from strings. |
| `updateCurrencyHint(input)`, `formatAmountHint(rialAmount)` | Void / Persian toman phrase string. | Hint target comes from `data-currency-hint`; amount input is rial. |
| `toEnglishDigits(val)`, `toPersianDigits(val)`, `normalizePersianText(text)` | Return normalized string. | Digit/text normalization. |
| `getTodayDateTime()`, `todayDate()`, `tomorrowDate()`, `GetDate` | Date strings; `GetDate.getCurrentWeek()` and `.getCurrentMonth()` are async instance methods. | Inspect formats and server dependency before assuming ISO dates. `MaskDate()` initializes date mask. |
| `renderMoneyHtml(value,{currency='ريال',empty='-',className=''})`, `renderDateTimeHtml(value,{empty='-',className=''})` | HTML string. | Render existing table values; escape any caller-supplied class/currency. |
| `calculateTotalPrice(weight,unitPrice)` | Number; returns computed price. | Existing price formula. |
| `resolveElement(target)`, `initSearchInput(input,callback)`, `initChangeRadio(name,callback,delegation=false)` | Element resolver; event setup functions return void. | `callback` may be async. Avoid initializing twice because listeners accumulate. |
| `debounce(fn,delay=300)`, `getUrlParams()`, `setUrlParams(paramsObj,{replace:true})`, `flashItem(el,options={})` | Debounced function; URL params; void; void. | Shared timing, URL state, highlight. |
| `updateFilterSwitchState({...})`, `updateTabSwitchState({...})`, `initEyeToggle({...})` | See source for full UI-specific options. | Existing filter/tab/visibility controls. |
| `createRating(containerId,{resultId,count,value,onRate})`, `escapeHtml(value)`, `copyToClipboard(text)`, `toBoolean(str)` | Rating UI; escaped string; clipboard side effect; boolean. | `copyToClipboard` does not return its inner Promise. |
| `goTo(url,blank=false)` | Browser navigation helper. |

`CrmService.getUsers(type)` is an existing async legacy fetch wrapper. For new compatible application HTTP calls, prefer `sendApiRequest`; preserve legacy callers.
