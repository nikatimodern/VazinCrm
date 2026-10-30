// Local loading for read-only page content. Mutations keep their existing loading behavior.
const PageSkeleton = (() => {
    const pending = new WeakMap();
    function card() {
        return '<div data-page-skeleton="card" aria-hidden="true"><div class="page-skeleton-heading"><i></i><span></span></div><span></span><span></span><div class="page-skeleton-footer"><span></span><span></span></div></div>';
    }
    function begin(options) {
        const settings = typeof options === "string" || options instanceof Element ? { target: options } : options;
        const nodes = typeof settings.target === "string" ? [...document.querySelectorAll(settings.target)] : [settings.target].filter(Boolean);
        if (!nodes.length) return null;
        nodes.forEach(node => pending.get(node)?.cancel());
        const controller = new AbortController();
        const originals = new Map();
        const ticket = {
            signal: controller.signal,
            isCurrent: () => nodes.every(node => pending.get(node) === ticket),
            finish() {
                for (const node of nodes) {
                    if (pending.get(node) !== ticket) continue;
                    if (node.querySelector('[data-page-skeleton="card"]')) node.replaceChildren(originals.get(node));
                    node.setAttribute("aria-busy", "false");
                    pending.delete(node);
                }
            },
            cancel() { controller.abort(); ticket.finish(); },
            fail(retry) {
                if (!ticket.isCurrent()) return;
                ticket.finish();
                for (const node of nodes) {
                    node.querySelectorAll(":scope > .page-skeleton-error").forEach(error => error.remove());
                    const feedback = document.createElement("div"); feedback.className = "page-skeleton-error";
                    const text = document.createElement("p"); text.setAttribute("role", "alert"); text.textContent = "دریافت اطلاعات انجام نشد.";
                    const button = document.createElement("button"); button.type = "button"; button.textContent = "تلاش دوباره";
                    button.addEventListener("click", retry); feedback.append(text, button); node.append(feedback);
                }
            },
        };
        for (const node of nodes) {
            node.querySelectorAll(":scope > .page-skeleton-error").forEach(error => error.remove());
            node.querySelectorAll(":scope > [data-page-skeleton]").forEach(placeholder => placeholder.remove());
            const original = document.createDocumentFragment(); while (node.firstChild) original.append(node.firstChild);
            originals.set(node, original); pending.set(node, ticket); node.setAttribute("aria-busy", "true");
            node.innerHTML = Array.from({ length: settings.count ?? 3 }, card).join("");
        }
        return ticket;
    }
    const tableRoutes = {
        "/accountancy/index": ["AcTable"], "/supply/profit": ["supplyProfitTable"],
        "/supply/demand": ["MotalebatTable", "supplyDemandTable"], "/check/index": ["checksTable"],
        "/transfer/index": ["transferTable"], "/cash/cashier": ["cashierTable"],
        "/cash/index": ["cashTable", "cashGroupTable"],
    };
    const tableIds = tableRoutes[location.pathname.replace(/\/+$/, "").toLowerCase()] ?? [];
    if (tableIds.length && window.jQuery) {
        const selector = tableIds.map(id => `#${id}`).join(",");
        function finishTable(table) { table.querySelector("caption[data-page-skeleton]")?.remove(); table.removeAttribute("data-page-loading"); table.setAttribute("aria-busy", "false"); }
        function startTable(table) {
            finishTable(table); table.setAttribute("data-page-loading", "true"); table.setAttribute("aria-busy", "true");
            const columns = Math.max(1, table.querySelector("thead tr")?.children.length ?? 5);
            const caption = document.createElement("caption"); caption.dataset.pageSkeleton = "table"; caption.setAttribute("aria-label", "در حال دریافت اطلاعات");
            caption.style.setProperty("--skeleton-columns", String(columns));
            caption.innerHTML = Array.from({ length: 6 }, () => `<div>${'<span></span>'.repeat(columns)}</div>`).join("");
            table.append(caption); window.jQuery(table).closest(".dataTables_wrapper,.dt-container").addClass("page-skeleton-table-host");
        }
        window.jQuery(document).on("preXhr.dt.pageSkeleton", selector, function () { startTable(this); })
            .on("xhr.dt.pageSkeleton", selector, function (event, settings, json) { if (json) finishTable(this); })
            .on("error.dt.pageSkeleton dt-error.dt.pageSkeleton", selector, function () {
                finishTable(this);
                const table = this; const caption = document.createElement("caption"); caption.dataset.pageSkeleton = "error";
                const button = document.createElement("button"); button.type = "button"; button.textContent = "دریافت اطلاعات انجام نشد؛ تلاش دوباره";
                button.addEventListener("click", () => window.jQuery(table).DataTable().ajax.reload(null, false)); caption.append(button); table.append(caption);
            });
    }
    window.addEventListener("pagehide", () => document.querySelectorAll('[aria-busy="true"]').forEach(node => pending.get(node)?.cancel()));
    return { begin };
})();
