(() => {
    "use strict";
    const root = document.getElementById("taxationAdmin");
    if (!root) return;

    const lifetime = new AbortController();
    const { signal } = lifetime;
    const page = typeof PageRuntime !== "undefined" ? PageRuntime.current : null;
    const dispose = () => lifetime.abort();
    if (page) page.cleanups.push(dispose);
    else window.addEventListener("pagehide", dispose, { once: true });

    const token = root.querySelector("[name=__RequestVerificationToken]").value;
    const status = root.querySelector("[data-admin-status]");
    let busy = false;
    root.addEventListener("submit", async event => {
        const form = event.target;
        const action = form.dataset.adminAction;
        if (!action) return;
        event.preventDefault();
        if (busy) return;

        const fields = Object.fromEntries(new FormData(form));
        const data = action === "state" ? null : action === "memory"
            ? { id: fields.id || null, fiscalId: fields.fiscalId, title: fields.title }
            : { productId: fields.productId, unit: Number(fields.unit), goodsId: fields.goodsId, taxUnit: fields.taxUnit };
        busy = true;
        const button = form.querySelector("button[type=submit]");
        button.disabled = true;
        status.textContent = "در حال ثبت…";
        try {
            const result = await sendApiRequest({
                url: form.dataset.url, method: "POST", data, headers: { RequestVerificationToken: token },
                showLoading: false, showError: false, logError: false, successMsg: null, signal,
                onError: error => {
                    if (signal.aborted) return;
                    const response = error.response?.data;
                    status.textContent = typeof response === "string" && !response.trimStart().startsWith("<")
                        ? response : "ذخیره انجام نشد. اطلاعات واردشده را بررسی کنید.";
                }
            });
            if (result && !signal.aborted) window.location.reload();
        } finally {
            busy = false;
            button.disabled = false;
        }
    }, { signal });
})();
