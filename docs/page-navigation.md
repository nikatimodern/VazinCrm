# Shared-shell page navigation

The MVC layout retains its navigation, header, shared modals, shared JavaScript, and SignalR connections. Page navigation fetches the next HTML document, prepares its page scripts, disposes the previous page scope, and commits a keyed virtual DOM snapshot under `#mainContent`. It does not create an iframe or replay the layout scripts.

## Layout contract

- `template[data-page-body]` contains the server-rendered body, including page-specific partials. Executable script nodes are extracted before DOM creation.
- `template[data-page-scripts]` contains the page's Scripts section.
- `template[data-page-styles]` contains page-specific style assets. These are removed on leaving the page.
- Pages using other layouts, authentication responses, and unsupported responses use ordinary document navigation. Downloads, modified clicks, fragment links, and form submissions retain their browser behavior.

## Page scripts

Add `data-page-bindings="name1,name2"` to a page script and list its top-level variables/functions that must remain available to inline handlers or shared helpers. External page script tags declare the bindings from their source file. The runtime executes each script in a fresh factory, exposing accessors for the declared bindings and restoring previous globals when the page is disposed. Scripts in AJAX partial responses continue to execute using their existing partial-loading mechanism; the attribute itself does not alter native script behavior.

Factories receive `__page`. The local `document`, `window`, timers, GET fetches, and document/window jQuery listeners belong to that scope. DOMContentLoaded/ready callbacks run for each mount. Use `__page.listen(target, event, handler)` for listeners on shared nodes such as `document.body`. After long asynchronous initialization, check `__page.isActive` before continuing. Register custom resource cleanup with `__page.cleanups.push(() => resource.destroy())`.

Shared helpers that make read requests through `sendApiRequest` inherit the current page's abort signal and ignore results after disposal. Business writes are not automatically aborted. Date pickers, DataTables, TomSelect, Sortable, dropdowns, and Bootstrap instances have scoped disposal; shared upload inputs are initialized idempotently for each inserted root.

## Verification

Check repeated visits, browser history, fast consecutive clicks, page scripts with identical binding names, inline actions, filters, modals, uploads, and session expiry. Shared libraries must execute once per document and a page's document listeners/timers must stop on disposal. Actual database-backed forms and large tables require application-level verification; fixture navigation tests do not prove their business behavior or production latency.
