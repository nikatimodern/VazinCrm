(() => {
    const root = document.getElementById("prefactorTermsWorkspace");
    const list = document.getElementById("prefactorTermList");
    if (!root || !list || root.dataset.bound) return;
    root.dataset.bound = "true";

    const get = id => document.getElementById(id);
    const search = get("prefactorTermSearch");
    const clearSearch = get("termsClearSearch");
    const sort = get("termsSort");
    const size = get("termsPageSize");
    const prev = get("termsPrev");
    const next = get("termsNext");
    const editor = get("prefactorTermEditor");
    const token = editor.querySelector('input[name="__RequestVerificationToken"]').value;
    const normalize = value => toEnglishDigits(value).toLowerCase()
        .replace(/[يى]/g, "ی").replace(/ك/g, "ک")
        .replace(/\u200c/g, " ").trim();
    const number = value => toPersianDigits(String(value));
    const id = row => Number(row.dataset.id);
    const order = row => Number(row.dataset.order);
    const text = row => row.querySelector(".prefactor-term-text").textContent;
    const creator = row => row.querySelector(".terms-creator").textContent;
    const date = row => row.querySelector(".terms-created-date").textContent;
    const compareDate = (a, b, descending) => {
        const first = a.dataset.created, second = b.dataset.created;
        if (!first || !second) return Number(!first) - Number(!second) || id(b) - id(a);
        return (descending ? second.localeCompare(first) : first.localeCompare(second)) || id(b) - id(a);
    };
    let canonical = Array.from(list.rows);
    let visible = [];
    let page = 0;
    let busy = false;
    let sortable = null;

    function canReorder() {
        return !busy && !normalize(search.value) && sort.value === "asc";
    }

    function render() {
        const query = normalize(search.value);
        const rows = canonical.filter(row => normalize(order(row) + " " + text(row) + " " + creator(row) + " " + date(row)).includes(query));
        if (sort.value === "desc") rows.sort((a, b) => order(b) - order(a) || id(b) - id(a));
        if (sort.value === "text") rows.sort((a, b) => normalize(text(a)).localeCompare(normalize(text(b)), "fa"));
        if (sort.value === "new" || sort.value === "old") rows.sort((a, b) => compareDate(a, b, sort.value === "new"));
        const limit = Number(size.value) || rows.length || 1;
        const pages = Math.max(1, Math.ceil(rows.length / limit));
        page = Math.min(page, pages - 1);
        const start = page * limit;
        visible = rows.slice(start, start + limit);
        const shown = new Set(visible);
        for (const row of canonical) {
            row.classList.add("d-none");
            row.querySelector(".terms-handle").disabled = !canReorder();
            list.append(row);
        }
        visible.forEach((row, index) => {
            row.classList.remove("d-none");
            row.querySelector(".terms-row-number").textContent = number(start + index + 1);
            row.querySelector(".terms-priority").textContent = number(order(row));
            list.append(row);
        });
        get("prefactorTermCount").textContent = number(rows.length);
        get("prefactorTermEmpty").hidden = rows.length !== 0;
        get("termsPageInfo").textContent = rows.length
            ? "نمایش " + number(start + 1) + " تا " + number(start + shown.size) + " از " + number(rows.length) + " بند"
            : "بدون نتیجه";
        get("termsPageNumber").textContent = number(page + 1) + " / " + number(pages);
        prev.disabled = busy || page === 0;
        next.disabled = busy || page >= pages - 1;
        for (const control of [search, sort, size, clearSearch, get("termsAdd")]) control.disabled = busy;
        clearSearch.hidden = !search.value;
        get("termsDragHint").textContent = busy ? "در حال ذخیره ترتیب..." :
            canReorder() ? "برای تغییر اولویت، دستگیره را بکشید یا از کلیدهای بالا و پایین استفاده کنید. برای جابه‌جایی بین صفحات، نمایش همه را انتخاب کنید." :
            "برای جابه‌جایی بندها، جستجو را پاک و مرتب‌سازی را روی اولویت صعودی قرار دهید.";
        sortable?.option("disabled", !canReorder());
    }

    async function persist(reordered) {
        if (!canReorder()) { render(); return; }
        const oldIds = canonical.map(id);
        const affected = new Set(visible);
        let index = 0;
        const updated = canonical.map(row => affected.has(row) ? reordered[index++] : row);
        if (updated.every((row, i) => row === canonical[i])) { render(); return; }
        busy = true;
        render();
        try {
            const result = await sendApiRequest({
                url: root.dataset.reorderUrl,
                method: "POST",
                headers: { RequestVerificationToken: token },
                data: { originalIds: oldIds, orderedIds: updated.map(id) },
                successMsg: null,
                showError: false
            });
            if (!root.isConnected) return;
            if (result?.success) {
                canonical = updated;
                canonical.forEach((row, i) => row.dataset.order = String(i + 1));
                const nextOrder = editor.querySelector('[name="Input.Order"]');
                if (!editor.querySelector('[name="Input.Id"]').value) nextOrder.value = String(canonical.length + 1);
                toastifySuccess("ترتیب بندها ذخیره شد.");
            } else {
                toastifyError("ترتیب ذخیره نشد. در صورت تغییر فهرست، صفحه را به‌روزرسانی کنید.");
            }
        } finally {
            busy = false;
            if (root.isConnected) render();
        }
    }

    sortable = Sortable.create(list, {
        handle: ".terms-handle",
        draggable: ".prefactor-term-row:not(.d-none)",
        animation: 120,
        ghostClass: "terms-ghost",
        onEnd: () => persist(Array.from(list.rows).filter(row => !row.classList.contains("d-none")))
    });
    list.addEventListener("keydown", event => {
        const handle = event.target.closest(".terms-handle");
        if (!handle || !canReorder() || !["ArrowUp", "ArrowDown"].includes(event.key)) return;
        event.preventDefault();
        const row = handle.closest("tr");
        const from = visible.indexOf(row);
        const to = from + (event.key === "ArrowUp" ? -1 : 1);
        if (from < 0 || to < 0 || to >= visible.length) return;
        const reordered = [...visible];
        [reordered[from], reordered[to]] = [reordered[to], reordered[from]];
        persist(reordered).then(() => { if (root.isConnected) handle.focus(); });
    });
    for (const [control, event] of [[search, "input"], [sort, "change"], [size, "change"]]) {
        control.addEventListener(event, () => { page = 0; render(); });
    }
    prev.addEventListener("click", () => { page--; render(); });
    next.addEventListener("click", () => { page++; render(); });
    clearSearch.addEventListener("click", () => {
        search.value = "";
        page = 0;
        render();
        search.focus();
    });

    get("termsAdd").addEventListener("click", () => {
        editor.querySelector('[name="Input.Id"]').value = "";
        editor.querySelector('[name="Input.Text"]').value = "";
        const lastOrder = canonical.reduce((max, row) => Math.max(max, order(row)), 0);
        editor.querySelector('[name="Input.Order"]').value = String(Math.min(lastOrder + 1, 2147483647));
        get("termsEditorTitle").textContent = "افزودن بند";
        editor.querySelectorAll("[data-valmsg-for]").forEach(message => { message.textContent = ""; });
        editor.querySelectorAll(".input-validation-error").forEach(field => field.classList.remove("input-validation-error"));
    });

    const modal = bootstrap.Modal.getOrCreateInstance(editor);
    if (editor.dataset.open === "true") modal.show();
    const scope = window.PageRuntime?.current;
    scope?.cleanups?.push(() => { sortable.destroy(); modal.dispose(); });
    enableMomentumScrollScope(root);
    render();
})();
