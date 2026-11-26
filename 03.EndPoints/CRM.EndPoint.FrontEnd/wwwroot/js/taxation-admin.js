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
        form.querySelector("[data-production-help]").hidden = sandbox;
        fields.sandboxEnabled.disabled = !sandbox;
        const hasPfx = fields.protocol.value === "2" && fields.pfx.files.length > 0;
        form.querySelector("[data-pfx-password]").hidden = !hasPfx;
        fields.pfxPassword.disabled = !hasPfx;

        let hasPendingFile = false;
        for (const box of form.querySelectorAll(".taxation-upload")) {
            const input = box.querySelector("input");
            const file = input.files[0];
            box.dataset.selected = Boolean(file);
            box.querySelector("[data-file-name]").textContent = file ? file.name : "فایلی انتخاب نشده";
            hasPendingFile ||= Boolean(file && !input.matches(":disabled"));
        }
        form.querySelector("[data-file-pending]").hidden = !hasPendingFile;

        const certificate = fields.certificateSha256.selectedOptions[0];
        const detail = form.querySelector("[data-certificate-detail]");
        detail.hidden = !certificate?.value || hasPfx;
        detail.textContent = hasPfx || !certificate?.value ? "" :
            `صادرکننده ${certificate.dataset.issuer} · انقضا ${certificate.dataset.expires}`;
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

    function showDiagnostics(diagnostics) {
        const details = document.createElement("details");
        details.className = "mt-2 taxation-muted";
        const summary = document.createElement("summary");
        summary.textContent = "جزئیات درخواست‌های V1";
        details.append(summary);
        for (const entry of diagnostics) {
            const fields = document.createElement("dl");
            fields.className = "mb-2 ps-2";
            const values = [
                ["آدرس درخواست", entry.url, true],
                ["نام سرآیندهای درخواست", entry.headerNames.join("، "), true],
                ["ساختار JSON درخواست", entry.requestJson, true],
                ["کد HTTP پاسخ", entry.httpStatusCode ?? "پاسخی دریافت نشد"],
                ["Content-Type پاسخ", entry.contentType ?? "در دسترس نیست", true]
            ];
            if (entry.errorCode) values.push(["کد خطای سازمان", entry.errorCode], ["شرح خطای سازمان", entry.errorDescription]);
            for (const [label, value, technical] of values) {
                const term = document.createElement("dt");
                term.textContent = label;
                const description = document.createElement("dd");
                if (technical) {
                    const code = document.createElement("code");
                    code.dir = "ltr";
                    code.className = "taxation-check-value";
                    code.textContent = value;
                    description.append(code);
                } else description.textContent = value;
                fields.append(term, description);
            }
            details.append(fields);
        }
        return details;
    }

    function showCheck(form, result) {
        const list = form.closest("article").querySelector("[data-connection-results]");
        list.replaceChildren();
        const labels = { Passed: "موفق", Failed: "ناموفق", Skipped: "بررسی نشد" };
        for (const step of result) {
            const row = document.createElement("li");
            row.dataset.checkStatus = step.status;
            row.textContent = `${step.name} · ${labels[step.status] || ""} · ${step.message}`;
            if (step.diagnostics?.length) row.append(showDiagnostics(step.diagnostics));
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
        const isCheck = action === "memory-check";
        const card = isCheck ? form.closest("article") : null;
        const inline = card?.querySelector("[data-connection-feedback]") ?? form.querySelector("[data-form-status]");
        function showError(text) {
            const target = inline ?? status;
            target.textContent = text;
            if (inline) {
                status.textContent = "";
                target.hidden = false;
            }
        }
        if (inline) inline.hidden = true;
        busy = true;
        const button = form.querySelector("button[type=submit]");
        button.disabled = true;
        if (isCheck) {
            card.querySelector("[data-connection-results]").hidden = true;
            inline.textContent = "در حال بررسی…";
            inline.hidden = false;
        } else status.textContent = "در حال ثبت…";
        try {
            const data = action === "memory-settings" ? await settingsData(fields) : action === "state" || action === "memory-check" ? null : action === "memory"
                ? { id: fields.id || null, fiscalId: fields.fiscalId, title: fields.title }
                : { productId: fields.productId, unit: Number(fields.unit), goodsId: fields.goodsId, taxUnit: fields.taxUnit };
            const result = await sendApiRequest({
                url: form.dataset.url, method: "POST", data, headers: { RequestVerificationToken: token },
                showLoading: false, showError: false, logError: false, successMsg: null, signal,
                timeout: isCheck ? 90000 : 30000,
                onError: error => {
                    if (signal.aborted) return;
                    const response = error.response?.data;
                    showError(typeof response === "string" && !response.trimStart().startsWith("<")
                        ? response : "عملیات انجام نشد. اطلاعات واردشده را بررسی کنید.");
                }
            });
            if (result && !signal.aborted) {
                if (isCheck) {
                    if (Array.isArray(result) && result.length) {
                        showCheck(form, result);
                        inline.hidden = true;
                    } else showError("نتیجه بررسی دریافت نشد.");
                } else if (action === "memory-settings") {
                    const destination = new URL(window.location.href);
                    destination.searchParams.set("saved", result);
                    destination.hash = "tax-memory-" + result;
                    if (destination.href === window.location.href) window.location.reload();
                    else window.location.replace(destination.href);
                } else window.location.reload();
            }
        } catch (error) {
            if (!signal.aborted) showError(action === "memory-settings" && error.message
                ? error.message : "عملیات انجام نشد.");
        } finally {
            busy = false;
            button.disabled = false;
        }
    }, { signal });
})();
