(() => {
    "use strict";

    const root = document.getElementById("accessCatalog");
    if (!root || root.dataset.historyInitialized === "1") return;
    root.dataset.historyInitialized = "1";
    const page = typeof PageRuntime !== "undefined" ? PageRuntime.current : null;
    const lifetime = new AbortController();
    const signal = lifetime.signal;
    const q = selector => root.querySelector(selector);
    const list = q("[data-history-list]");
    const csrf = q("input[name=__RequestVerificationToken]").value;
    const pointModal = q("#acHistoryPointModal");
    const pointForm = q("[data-history-point-form]");
    let entries = [], selected = null, preview = null;
    let pageNo = 1, pages = 1, kind = "", search = "";
    let generation = 0, detailGeneration = 0, busy = false, searchTimer = null;

    const el = (tag, label, className) => {
        const node = document.createElement(tag);
        if (label !== undefined && label !== null) node.textContent = String(label);
        if (className) node.className = className;
        return node;
    };
    const error = (message, target = "[data-history-error]") => {
        const node = q(target);
        node.textContent = message || "";
        node.hidden = !message;
    };
    const empty = (target, message) => {
        const box = el("div", null, "ac-empty");
        box.append(el("i", null, "fas fa-history"), el("p", message));
        target.replaceChildren(box);
    };
    const get = (action, params) => sendApiRequest({
        url: "/AccessCatalogAdmin/" + action, params, showLoading: false
    });
    const post = (action, data, errorTarget = "[data-history-error]") => sendApiRequest({
        url: "/AccessCatalogAdmin/" + action,
        method: "POST", data,
        headers: { RequestVerificationToken: csrf },
        successMsg: null, showLoading: false, showError: false,
        onError: err => {
            const message = err.response?.data?.message || "عملیات انجام نشد؛ دوباره تلاش کنید.";
            error(message, errorTarget);
            toastifyError(message);
        }
    });
    const formatDate = value => {
        if (!value) return "";
        const valueUtc = /(?:Z|[+-]\d\d:\d\d)$/.test(value) ? value : value + "Z";
        const date = new Date(valueUtc);
        return Number.isNaN(date.getTime()) ? value : date.toLocaleString("fa-IR", {
            year: "numeric", month: "short", day: "numeric", hour: "2-digit", minute: "2-digit"
        });
    };
    const kindMeta = {
        roles: ["نقش‌ها", "fa-user-tag"],
        users: ["کاربران", "fa-user-shield"],
        catalog: ["مجوزها", "fa-key"],
        manual: ["نقطه دستی", "fa-camera"],
        restore: ["بازگردانی", "fa-undo"]
    };
    function renderList() {
        list.replaceChildren();
        q("[data-history-page]").textContent = "صفحه " + pageNo + " از " + pages;
        q("[data-action='history-prev']").disabled = busy || pageNo <= 1;
        q("[data-action='history-next']").disabled = busy || pageNo >= pages;
        if (!entries.length) {
            empty(list, "تاریخچه‌ای مطابق فیلتر پیدا نشد.");
            return;
        }
        for (const item of entries) {
            const b = el("button", null, "ac-history-item" +
                (selected?.id === item.id ? " is-selected" : ""));
            b.type = "button";
            b.dataset.historyId = item.id;
            b.setAttribute("aria-pressed", String(selected?.id === item.id));
            const top = el("span", null, "ac-history-item-top");
            top.append(el("i", null, "fas " + (kindMeta[item.kind]?.[1] || "fa-history")),
                el("span", formatDate(item.createdAtUtc)),
                el("small", kindMeta[item.kind]?.[0] || "تغییر"));
            b.append(top, el("strong", item.title),
                el("span", item.kind === "manual" ? "نقطه بازگردانی دستی" :
                    item.changeCount + " تغییر ثبت‌شده", "ac-history-item-detail"),
                el("span", "توسط " + item.actorName, "ac-history-item-actor"));
            list.append(b);
        }
    }
    function renderDiffs(target, changes) {
        target.replaceChildren();
        if (!changes?.length) {
            empty(target, "در این نسخه تغییری برای نمایش وجود ندارد.");
            return;
        }
        for (const change of changes.slice(0, 250)) {
            const row = el("article", null, "ac-history-diff");
            const sides = el("div", null, "ac-history-diff-pair");
            for (const [label, value, side] of [
                ["قبل", change.before, "before"], ["بعد", change.after, "after"]
            ]) {
                const part = el("div", null, "ac-history-diff-side is-" + side);
                part.append(el("small", label), el("span", value));
                sides.append(part);
            }
            row.append(el("strong", change.title, "ac-history-diff-title"), sides);
            target.append(row);
        }
        if (changes.length > 250)
            target.append(el("p", "فقط ۲۵۰ مورد اول نمایش داده شده است.", "ac-muted"));
    }
    function exitPreview() {
        preview = null;
        q("[data-history-preview-pane]").hidden = true;
        q("[data-history-diff-view]").hidden = false;
    }
    async function choose(id) {
        if (busy || !id) return;
        const turn = ++detailGeneration;
        selected = null;
        exitPreview();
        q("[data-history-actions]").hidden = true;
        empty(q("[data-history-diffs]"), "در حال دریافت جزئیات...");
        const result = await get("HistoryDetail", { id });
        if (signal.aborted || turn !== detailGeneration || q("#acHistoryPanel").hidden) return;
        if (!result?.id) {
            empty(q("[data-history-diffs]"), "دریافت جزئیات ناموفق بود.");
            return;
        }
        selected = result;
        q("[data-history-detail-title]").textContent = result.title;
        q("[data-history-detail-meta]").textContent =
            formatDate(result.createdAtUtc) + " · " + result.actorName +
            " · " + result.changeCount + " تغییر";
        renderDiffs(q("[data-history-diffs]"), result.changes);
        q("[data-history-actions]").hidden = false;
        q("[data-action='history-preview-single']").disabled = !result.changeCount;
        renderList();
    }
    async function load(nextPage = 1) {
        const turn = ++generation;
        ++detailGeneration;
        selected = null;
        exitPreview();
        q("[data-history-actions]").hidden = true;
        q("[data-history-detail-title]").textContent = "جزئیات نسخه";
        q("[data-history-detail-meta]").textContent = "یک تغییر را از فهرست انتخاب کنید.";
        empty(q("[data-history-diffs]"), "برای مشاهده جزئیات یک تغییر انتخاب کنید.");
        empty(list, "در حال دریافت تاریخچه...");
        error("");
        const result = await get("History", { page: nextPage, kind: kind || null, search: search || null });
        if (signal.aborted || turn !== generation || q("#acHistoryPanel").hidden) return;
        if (!Array.isArray(result?.items)) {
            empty(list, "دریافت تاریخچه ناموفق بود.");
            return;
        }
        entries = result.items;
        pageNo = result.page;
        pages = Math.max(1, result.totalPages);
        q("[data-history-count]").textContent = String(result.total);
        renderList();
        if (entries.length) await choose(entries[0].id);
    }
    async function showPreview(mode) {
        if (busy || !selected) return;
        const id = selected.id;
        busy = true;
        try {
            error("");
            const result = await get("HistoryPreview", { id, mode });
            if (signal.aborted || !result?.revision || selected?.id !== id) return;
            preview = result;
            q("[data-history-preview-pane]").hidden = false;
            q("[data-history-diff-view]").hidden = true;
            q("[data-history-preview-description]").textContent = mode === "single" ?
                "فقط تغییرات همین عملیات معکوس می‌شوند. تغییرات نامرتبط حفظ خواهند شد." :
                "تمام تنظیمات سطح دسترسی به وضعیت این نقطه بازگردانده می‌شوند.";
            const warnings = q("[data-history-preview-warnings]");
            warnings.replaceChildren();
            for (const warning of result.warnings || [])
                warnings.append(el("p", warning, "ac-history-warning"));
            renderDiffs(q("[data-history-preview-diffs]"), result.changes);
            q("[data-action='history-restore']").disabled =
                result.conflictCount > 0 || !result.changes?.length;
        } finally { busy = false; }
    }
    async function restore() {
        if (busy || !preview || preview.conflictCount || !preview.changes?.length) return;
        const currentPreview = preview;
        const approved = await confirmAlert({
            title: "تأیید بازگردانی",
            text: currentPreview.mode === "single" ?
                "فقط این تغییر برگردانده می‌شود. نسخه فعلی هم برای بازگشت بعدی ثبت خواهد شد." :
                "تمام تنظیمات دسترسی به نقطه انتخابی بازمی‌گردند و ممکن است تغییرات جدیدتر از دست بروند. ادامه می‌دهید؟",
            confirmText: "بازگردانی", cancelText: "انصراف", icon: "warning"
        });
        if (!approved || signal.aborted || busy) return;
        busy = true;
        q("[data-action='history-restore']").disabled = true;
        try {
            error("");
            const result = await post("RestoreHistory", {
                id: currentPreview.id, mode: currentPreview.mode, revision: currentPreview.revision
            });
            if (signal.aborted) return;
            if (result?.success) {
                toastifySuccess(result.message || "بازگردانی انجام شد.");
                await load(1);
            }
        } finally { busy = false; }
    }
    async function createManual(event) {
        event.preventDefault();
        if (busy || !pointForm.reportValidity()) return;
        busy = true;
        const button = pointForm.querySelector("[type=submit]");
        button.disabled = true;
        try {
            error("", "[data-history-point-error]");
            const result = await post("CreateHistoryPoint", {
                title: pointForm.elements.namedItem("title").value.trim()
            }, "[data-history-point-error]");
            if (signal.aborted) return;
            if (result?.success) {
                ModalToggle(pointModal, "hide");
                toastifySuccess("نقطه بازگردانی ثبت شد.");
                await load(1);
            }
        } finally { busy = false; button.disabled = false; }
    }

    root.addEventListener("access-history-open", () => { void load(1); }, { signal });
    root.addEventListener("click", event => {
        const selectedButton = event.target.closest("[data-history-id]");
        if (selectedButton) { void choose(selectedButton.dataset.historyId); return; }
        const b = event.target.closest("[data-action]");
        if (!b || b.disabled || !q("#acHistoryPanel") || q("#acHistoryPanel").hidden) return;
        switch (b.dataset.action) {
            case "history-prev": if (pageNo > 1) void load(pageNo - 1); break;
            case "history-next": if (pageNo < pages) void load(pageNo + 1); break;
            case "history-reload": void load(pageNo); break;
            case "history-new-point":
                pointForm.reset(); error("", "[data-history-point-error]");
                ModalToggle(pointModal, "show"); break;
            case "history-preview-single": void showPreview("single"); break;
            case "history-preview-point": void showPreview("point"); break;
            case "history-preview-cancel": exitPreview(); break;
            case "history-restore": void restore(); break;
        }
    }, { signal });
    q("[data-history-kind]").addEventListener("change", event => {
        kind = event.target.value;
        void load(1);
    }, { signal });
    q("[data-history-search]").addEventListener("input", event => {
        search = event.target.value.trim();
        if (searchTimer !== null) clearTimeout(searchTimer);
        searchTimer = setTimeout(() => {
            searchTimer = null;
            if (!q("#acHistoryPanel").hidden) void load(1);
        }, 300);
    }, { signal });
    pointForm.addEventListener("submit", event => { void createManual(event); }, { signal });
    const cleanup = () => {
        lifetime.abort();
        if (searchTimer !== null) clearTimeout(searchTimer);
        bootstrap.Modal.getInstance(pointModal)?.dispose();
    };
    if (page) page.cleanups.push(cleanup);
    else window.addEventListener("pagehide", cleanup, { once: true });
})();
