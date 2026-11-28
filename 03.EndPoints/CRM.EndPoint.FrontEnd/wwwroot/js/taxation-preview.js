(() => {
    "use strict";

    const root = document.getElementById("taxationPreview");
    if (!root) return;
    const lifetime = new AbortController();
    const { signal } = lifetime;
    const page = typeof PageRuntime !== "undefined" ? PageRuntime.current : null;
    const dispose = () => lifetime.abort();
    if (page) page.cleanups.push(dispose);
    else window.addEventListener("pagehide", dispose, { once: true });

    const message = root.querySelector("[data-action-message]");
    if (!message) return;
    const token = root.querySelector("input[name='__RequestVerificationToken']").value;
    const errors = root.querySelector("[data-validation-errors]");
    const errorRows = new Map(Array.from(root.querySelectorAll("[data-error-row]"), row => [row.dataset.errorRow, row]));
    const localForm = root.querySelector("[data-local-preview]");
    const exportForm = localForm?.querySelector("[data-export]");
    const filing = root.querySelector("[data-filing]");
    let busy = false;
    let version = exportForm ? null : localForm?.dataset.version;
    let valid = Boolean(version) && errors.children.length === 0;

    function renderErrors(issues) {
        errors.replaceChildren();
        for (const row of errorRows.values()) {
            row.hidden = true;
            row.querySelector("ul").replaceChildren();
        }
        for (const issue of issues) {
            const row = errorRows.get(/^ردیف\s+(\d+)\s/.exec(issue)?.[1]);
            const item = document.createElement("li");
            item.textContent = issue;
            if (row) {
                row.querySelector("ul").append(item);
                row.hidden = false;
            } else {
                errors.append(item);
            }
        }
        errors.hidden = errors.children.length === 0;
    }

    function refreshButtons() {
        for (const button of root.querySelectorAll("button[type='submit']")) {
            const action = button.dataset.localAction;
            button.disabled = busy || Boolean(action && action !== "validate" && !valid);
        }
    }

    const dates = window.TaxationDates;
    const valueOf = (scope, name) => scope.querySelector(`[name="${name}"]`).value.trim();
    const atIranTime = (date, time) => `${date}T${time}:00+03:30`;
    const issuedAt = scope => atIranTime(dates.toIso(valueOf(scope, "issuedDate")), valueOf(scope, "issuedTime"));
    dates.clearValidation(root, signal);

    function exportDetails() {
        if (!exportForm) return null;
        const date = dates.toIso(valueOf(exportForm, "customsDate"));
        if (date === null) return null;
        return {
            customsDeclarationNumber: valueOf(exportForm, "customsNumber") || null,
            customsDeclarationDate: date ? atIranTime(date, "00:00") : null,
            customsOffice: valueOf(exportForm, "customsOffice") || null,
            noOtherTaxesOrDuties: exportForm.querySelector("[name=noOtherDuties]").checked,
            lines: Array.from(exportForm.querySelectorAll("[data-export-row]"), row => ({
                factorItemId: row.dataset.exportRow,
                currencyCode: valueOf(row, "currency"),
                exchangeRate: valueOf(row, "rate"),
                rialValue: valueOf(row, "rial"),
                currencyValue: valueOf(row, "value"),
                netWeightKg: valueOf(row, "weight")
            }))
        };
    }

    function filingDetails() {
        if (!filing) return null;
        const settlement = Number(valueOf(filing, "settlement"));
        return {
            serial: valueOf(filing, "serial"),
            issuedAt: issuedAt(filing),
            buyerType: Number(valueOf(filing, "buyerType")),
            settlement,
            shares: settlement === 3 ? { cash: valueOf(filing, "cash"), credit: valueOf(filing, "credit") } : null
        };
    }

    function post(url, data = null) {
        return sendApiRequest({
            url, data, method: "POST", signal,
            headers: { RequestVerificationToken: token },
            showLoading: false, showError: false, logError: false, successMsg: null,
            onError: error => {
                if (signal.aborted) return;
                const response = error.response?.data;
                message.textContent = typeof response === "string" && !response.trimStart().startsWith("<")
                    ? response : "عملیات انجام نشد. وضعیت درخواست را پیش از تلاش دوباره بررسی کنید.";
            }
        });
    }

    function invalidateExport() {
        valid = false;
        version = null;
        refreshButtons();
    }

    exportForm?.addEventListener("input", invalidateExport, { signal });
    exportForm?.addEventListener("change", invalidateExport, { signal });

    if (filing) {
        filing.querySelector("[name=settlement]").addEventListener("change", event => {
            const mixed = event.target.value === "3";
            for (const label of filing.querySelectorAll("[data-share]")) {
                label.hidden = !mixed;
                const input = label.querySelector("input");
                input.disabled = !mixed;
                input.required = mixed;
            }
        }, { signal });
    }

    renderErrors(Array.from(errors.children, item => item.textContent));
    refreshButtons();

    root.addEventListener("submit", async event => {
        const form = event.target;
        const action = event.submitter?.dataset.localAction;
        if (form !== localForm && !form.dataset.dispatchAction) return;
        event.preventDefault();
        if (busy || (form === localForm && !action) || !dates.validate(form)) return;
        busy = true;
        refreshButtons();
        message.textContent = "در حال انجام…";
        try {
            if (action === "validate") {
                const submittedDetails = exportDetails();
                const preview = await post(exportForm.dataset.previewUrl, submittedDetails);
                if (!preview || signal.aborted) return;
                renderErrors(preview.errors);
                valid = preview.errors.length === 0 && JSON.stringify(submittedDetails) === JSON.stringify(exportDetails());
                version = valid ? preview.version : null;
                message.textContent = valid ? "پیش‌نمایش معتبر است." : "پیش‌نمایش و اطلاعات ردیف‌ها را بررسی کنید.";
            } else if (action === "snapshot" || action === "queue") {
                if (!valid) return;
                const url = action === "queue" ? form.dataset.queueUrl : form.dataset.snapshotUrl;
                const id = await post(url, { version, export: exportDetails() });
                if (!id || signal.aborted) return;
                if (action === "queue") {
                    window.location.assign(form.dataset.dispatchUrl.replace("00000000-0000-0000-0000-000000000000", id));
                } else {
                    message.textContent = "Snapshot ذخیره شد.";
                }
            } else if (form.dataset.dispatchAction === "cancel-full-return") {
                const id = await post(form.dataset.url, {
                    kind: Number(form.dataset.cancellationKind),
                    serial: valueOf(form, "serial"),
                    issuedAt: issuedAt(form),
                    cancellationReason: Number(form.dataset.cancellationReason)
                });
                if (id && !signal.aborted)
                    window.location.assign(form.dataset.dispatchUrl.replace("00000000-0000-0000-0000-000000000000", id));
            } else {
                const result = await post(form.dataset.url, form.dataset.dispatchAction === "submit" ? filingDetails() : null);
                if (result && !signal.aborted) window.location.reload();
            }
        } finally {
            busy = false;
            if (!signal.aborted) refreshButtons();
        }
    }, { signal });
})();
