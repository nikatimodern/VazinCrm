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
    function syncSettings(form) {
        const fields = form.elements;
        for (const group of form.querySelectorAll("[data-protocol]")) {
            const current = group.dataset.protocol === fields.protocol.value;
            group.hidden = !current;
            group.disabled = !current;
        }
        const visibleEnvironment = form.querySelector("select[name=environment]");
        const sandbox = visibleEnvironment.value === "1";
        form.querySelector("[data-sandbox-field]").hidden = !sandbox;
        fields.sandboxEnabled.disabled = !sandbox;
        fields.pfxPassword.disabled = !fields.pfx.files.length;
    }

    for (const form of root.querySelectorAll('[data-admin-action="memory-settings"]')) {
        syncSettings(form);
        form.addEventListener("change", () => syncSettings(form), { signal });
    }

    async function readPem(file) {
        if (!file?.size) return null;
        if (file.size > 8192) throw new Error("اندازه فایل PEM بیش از حد مجاز است.");
        return await file.text();
    }

    async function readPfx(file) {
        if (!file?.size) return null;
        if (file.size > 65536) throw new Error("اندازه فایل PFX/P12 بیش از حد مجاز است.");
        const bytes = new Uint8Array(await file.arrayBuffer());
        try { return btoa(Array.from(bytes, byte => String.fromCharCode(byte)).join("")); }
        finally { bytes.fill(0); }
    }

    async function settingsData(fields) {
        if ((fields.privateKey?.size || fields.pfx?.size) && window.location.protocol !== "https:")
            throw new Error("برای بارگذاری کلید خصوصی یا گواهی از HTTPS استفاده کنید.");
        return {
            id: fields.id || null, title: fields.title, fiscalId: fields.fiscalId,
            sellerEconomicCode: fields.sellerEconomicCode, environment: Number(fields.environment),
            protocol: Number(fields.protocol), sandboxEnabled: fields.sandboxEnabled === "on",
            expectedUpdatedUtc: fields.expectedUpdatedUtc || null,
            certificateSha256: fields.certificateSha256 || null,
            privateKeyPem: await readPem(fields.privateKey), publicKeyPem: await readPem(fields.publicKey),
            pfx: await readPfx(fields.pfx), pfxPassword: fields.pfx?.size ? fields.pfxPassword || null : null
        };
    }

    function showCheck(form, result) {
        const list = form.closest("article").querySelector("[data-connection-results]");
        list.replaceChildren();
        const labels = { Passed: "موفق", Failed: "ناموفق", Skipped: "بررسی نشد" };
        for (const step of result) {
            const row = document.createElement("li");
            row.dataset.checkStatus = step.status;
            row.textContent = `${step.name} · ${labels[step.status] || ""} · ${step.message}`;
            list.append(row);
        }
        list.hidden = !list.children.length;
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
            const data = action === "memory-settings" ? await settingsData(fields) : action === "state" || action === "memory-check" ? null : action === "memory"
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
                if (action === "memory-check" && Array.isArray(result)) {
                    showCheck(form, result);
                    status.textContent = "";
                } else window.location.reload();
            }
        } catch (error) {
            if (!signal.aborted) status.textContent = action === "memory-settings" && error.message
                ? error.message : "عملیات انجام نشد.";
        } finally {
            busy = false;
            button.disabled = false;
        }
    }, { signal });
})();
