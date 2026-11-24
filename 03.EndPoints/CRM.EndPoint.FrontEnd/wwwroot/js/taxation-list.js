(() => {
    "use strict";

    const root = document.getElementById("taxationInvoices");
    if (!root) return;

    const lifetime = new AbortController();
    const page = typeof PageRuntime !== "undefined" ? PageRuntime.current : null;
    const scroll = root.querySelector("[data-taxation-scroll]");
    const releaseScroll = scroll && typeof enableMomentumScrollScope === "function"
        ? enableMomentumScrollScope(scroll) : null;
    const dispose = () => {
        lifetime.abort();
        releaseScroll?.();
    };
    if (page) page.cleanups.push(dispose);
    else window.addEventListener("pagehide", dispose, { once: true });

    const batch = root.querySelector("#taxationBatch");
    batch?.addEventListener("submit", async event => {
        event.preventDefault();
        const submit = batch.querySelector("button[type=submit]");
        if (submit.disabled) return;
        const status = batch.querySelector("[data-batch-status]");
        const results = batch.querySelector("[data-batch-results]");
        const selected = new Map(Array.from(batch.elements)
            .filter(control => control.name === "factor" && control.checked)
            .map(control => [control.value, control.dataset.invoiceNumber]));
        if (!selected.size) {
            status.textContent = "حداقل یک فاکتور انتخاب کنید.";
            return;
        }
        submit.disabled = true;
        status.textContent = "در حال بررسی و ثبت فاکتورها…";
        results.replaceChildren();
        results.hidden = true;
        try {
            const response = await sendApiRequest({
                url: batch.dataset.url, method: "POST", data: Array.from(selected.keys()),
                headers: { RequestVerificationToken: root.querySelector("[name=__RequestVerificationToken]").value },
                showLoading: false, showError: false, logError: false, successMsg: null, signal: lifetime.signal
            });
            if (lifetime.signal.aborted) return;
            if (!Array.isArray(response)) {
                status.textContent = "نتیجه دریافت نشد. برای ادامه، همین انتخاب را دوباره ثبت کنید.";
                return;
            }
            for (const row of response) {
                const item = document.createElement("li");
                item.textContent = "فاکتور " + selected.get(row.factorId) + " · ";
                if (row.dispatchId) {
                    const link = document.createElement("a");
                    link.href = batch.dataset.dispatchUrl.replace("00000000-0000-0000-0000-000000000000", row.dispatchId);
                    link.textContent = "درخواست در صف ثبت شده است";
                    item.append(link);
                } else {
                    item.append(row.errors.join(" "));
                }
                results.append(item);
            }
            results.hidden = false;
            status.textContent = "نتیجه ثبت گروهی";
        } finally {
            submit.disabled = false;
        }
    }, { signal: lifetime.signal });

    root.addEventListener("click", async event => {
        const button = event.target.closest("button[data-preview-url]");
        if (!button || button.disabled) return;
        const result = button.closest("[data-taxation-validation]").querySelector("[data-validation-result]");
        button.disabled = true;
        result.textContent = "در حال بررسی…";
        try {
            const preview = await sendApiRequest({
                url: button.dataset.previewUrl,
                showLoading: false,
                showError: false,
                logError: false,
                signal: lifetime.signal
            });
            if (lifetime.signal.aborted) return;
            if (!preview || !Array.isArray(preview.errors)) {
                result.textContent = "اعتبارسنجی انجام نشد. دوباره تلاش کنید.";
                return;
            }
            if (!preview.errors.length) {
                result.textContent = "پیش‌نمایش معتبر است.";
                return;
            }
            const messages = document.createElement("ul");
            for (const error of preview.errors) {
                const item = document.createElement("li");
                item.textContent = error;
                messages.append(item);
            }
            result.replaceChildren(messages);
        } finally {
            button.disabled = false;
        }
    }, { signal: lifetime.signal });
})();
