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
    const connection = root.querySelector('[data-admin-action="connection"]');
    function syncConnectionFields() {
        for (const label of connection.querySelectorAll("[data-protocol]")) {
            const visible = label.dataset.protocol === connection.elements.protocol.value;
            const input = label.querySelector("input");
            label.hidden = !visible;
            input.disabled = !visible;
            input.required = visible && label.dataset.protocol === "2";
        }
    }
    if (connection) {
        syncConnectionFields();
        connection.elements.protocol.addEventListener("change", syncConnectionFields, { signal });
    }

    async function connectionData(fields) {
        const file = fields.v1PrivateKeyFile;
        if (file?.size > 8192) throw new Error("اندازه فایل کلید خصوصی بیش از حد مجاز است.");
        if (file?.size && !window.location.href.startsWith("https:"))
            throw new Error("برای ثبت کلید خصوصی از اتصال HTTPS استفاده کنید.");
        return {
            memoryId: fields.memoryId, protocol: Number(fields.protocol), sellerEconomicCode: fields.sellerEconomicCode,
            certificateSha256: fields.certificateSha256 || null, v1PrivateKey: file?.size ? await file.text() : null
        };
    }

    let busy = false;
    root.addEventListener("submit", async event => {
        const form = event.target;
        const action = form.dataset.adminAction;
        if (!action) return;
        event.preventDefault();
        if (busy) return;

        const fields = Object.fromEntries(new FormData(form));
        busy = true;
        const button = form.querySelector("button[type=submit]");
        button.disabled = true;
        status.textContent = "در حال ثبت…";
        try {
            const data = action === "connection" ? await connectionData(fields) : action === "state" || action === "connection-probe" ? null : action === "memory"
                ? { id: fields.id || null, fiscalId: fields.fiscalId, title: fields.title }
                : { productId: fields.productId, unit: Number(fields.unit), goodsId: fields.goodsId, taxUnit: fields.taxUnit };
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
            if (result && !signal.aborted) {
                if (action === "connection-probe") status.textContent = "احراز هویت و تطبیق حافظهٔ آزمایشی موفق بود.";
                else window.location.reload();
            }
        } catch (error) {
            if (!signal.aborted) status.textContent = action === "connection" && error.message
                ? error.message : "عملیات انجام نشد.";
        } finally {
            busy = false;
            button.disabled = false;
        }
    }, { signal });
})();
