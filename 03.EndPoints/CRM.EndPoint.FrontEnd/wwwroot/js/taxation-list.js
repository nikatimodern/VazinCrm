(() => {
    "use strict";

    const root = document.getElementById("taxationInvoices");
    if (!root) return;

    const lifetime = new AbortController();
    const page = typeof PageRuntime !== "undefined" ? PageRuntime.current : null;
    const dispose = () => lifetime.abort();
    if (page) page.cleanups.push(dispose);
    else window.addEventListener("pagehide", dispose, { once: true });

    const batch = root.querySelector("#taxationBatch");
    const submit = batch?.querySelector("button[type=submit]");
    if (submit) {
        const boxes = Array.from(batch.elements).filter(control => control.name === "factor");
        const selectPage = batch.querySelector("[data-select-page]");
        const count = batch.querySelector("[data-selection-count]");
        const status = batch.querySelector("[data-batch-status]");
        const results = batch.querySelector("[data-batch-results]");
        const number = new Intl.NumberFormat("fa-IR");
        let submitting = false;

        function updateSelection() {
            const selected = boxes.filter(box => box.checked).length;
            count.textContent = number.format(selected) + " انتخاب";
            selectPage.checked = selected === boxes.length;
            selectPage.indeterminate = selected > 0 && selected < boxes.length;
            selectPage.disabled = submitting;
            submit.disabled = submitting || selected === 0;
        }

        selectPage.addEventListener("change", () => {
            for (const box of boxes) box.checked = selectPage.checked;
            updateSelection();
        }, { signal: lifetime.signal });
        root.addEventListener("change", event => {
            if (event.target.form === batch && event.target.name === "factor")
                updateSelection();
        }, { signal: lifetime.signal });

        batch.addEventListener("submit", async event => {
            event.preventDefault();
            if (submit.disabled) return;
            const selected = new Map(boxes.filter(box => box.checked).map(box => [box.value, box]));
            submitting = true;
            for (const box of boxes) box.disabled = true;
            updateSelection();
            status.textContent = "در حال بررسی و ثبت فاکتورها…";
            results.replaceChildren();
            results.hidden = true;

            try {
                const response = await sendApiRequest({
                    url: batch.dataset.url, method: "POST", data: Array.from(selected.keys()),
                    headers: { RequestVerificationToken: batch.querySelector("[name=__RequestVerificationToken]").value },
                    showLoading: false, showError: false, logError: false, successMsg: null, signal: lifetime.signal
                });
                if (lifetime.signal.aborted) return;
                if (!Array.isArray(response)) {
                    status.textContent = "نتیجه دریافت نشد. برای ادامه، همین انتخاب را دوباره ثبت کنید.";
                    return;
                }

                let queued = 0;
                for (const row of response) {
                    const box = selected.get(row.factorId);
                    if (!box) continue;
                    const item = document.createElement("li");
                    item.textContent = "فاکتور " + box.dataset.invoiceNumber + " · ";
                    if (row.dispatchId) {
                        queued++;
                        box.checked = false;
                        const link = document.createElement("a");
                        link.href = batch.dataset.dispatchUrl.replace("00000000-0000-0000-0000-000000000000", row.dispatchId);
                        link.textContent = "مشاهده درخواست ثبت‌شده";
                        item.append(link);
                    } else {
                        item.dataset.failed = "true";
                        item.append(row.errors.join(" "));
                    }
                    results.append(item);
                }

                results.hidden = false;
                const remaining = selected.size - queued;
                status.textContent = number.format(queued) + " ثبت‌شده" +
                    (remaining ? "، " + number.format(remaining) + " نیازمند بررسی" : "");
            } finally {
                submitting = false;
                for (const box of boxes) box.disabled = false;
                updateSelection();
            }
        }, { signal: lifetime.signal });
        updateSelection();
    }

    root.addEventListener("click", async event => {
        const button = event.target.closest("button[data-preview-url]");
        if (!button || button.disabled) return;
        const result = button.closest("[data-taxation-validation]").querySelector("[data-validation-result]");
        button.disabled = true;
        result.dataset.state = "loading";
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
                result.dataset.state = "error";
                result.textContent = "اعتبارسنجی انجام نشد. دوباره تلاش کنید.";
                return;
            }
            if (!preview.errors.length) {
                result.dataset.state = "valid";
                result.textContent = "پیش‌نمایش معتبر است.";
                return;
            }
            const messages = document.createElement("ul");
            for (const error of preview.errors) {
                const item = document.createElement("li");
                item.textContent = error;
                messages.append(item);
            }
            result.dataset.state = "error";
            result.replaceChildren(messages);
        } finally {
            button.disabled = false;
        }
    }, { signal: lifetime.signal });
})();
