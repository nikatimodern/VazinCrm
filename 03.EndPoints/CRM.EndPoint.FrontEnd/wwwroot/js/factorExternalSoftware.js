let externalFactorExportState = null;

async function sendFactorToExternalSoftware(factorId) {
    if (!factorId) return;

    await showExternalSoftwareModal(factorId, {
        mapType: 3,
        onConfirm: async ({ entityIds, externalSoftwareIds }) => {
            if (externalSoftwareIds.length !== 1) {
                toastifyError("برای هر انتقال فقط یک نرم‌افزار مقصد انتخاب کنید.");
                return;
            }

            const request = {
                factorId: entityIds[0],
                externalSoftwareId: externalSoftwareIds[0],
            };

            const result = await sendApiRequest({
                url: "/Factor/PrepareExternalFactor",
                method: "POST",
                data: request,
                successMsg: null,
            });

            if (!result?.preview?.items?.length) return;

            externalFactorExportState = { request, processing: false };
            document.getElementById("externalFactorPreviewContent").innerHTML =
                renderExternalFactorPreview(result);
            ModalToggle("chooseExternalSoftwareModal", "hide");
            ModalToggle("sendFactorToExternalSoftwareModal", "show");
        },
    });
}

function renderExternalFactorPreview(data) {
    const preview = data.preview;
    const escape = value => escapeHtml(String(value ?? "-"));
    const number = value => Number(value ?? 0).toLocaleString("fa-IR", {
        maximumFractionDigits: 4,
    });
    const money = value => `${number(value)} ریال`;

    const date = preview.invoiceDateText || preview.invoiceDate?.split("T")[0] || "-";
    document.getElementById("sendFactorToExternalSoftwareTitle").textContent =
        data.softwareTitle || "نرم‌افزار مالی";
    document.getElementById("sendFactorToExternalSoftwareSubtitle").textContent =
        "نرم‌افزار ۱ از ۱";

    return `
        <div class="external-mapping-stage-info">
            <div class="external-mapping-stage-icon">
                <i class="fas fa-clipboard-check" aria-hidden="true"></i>
            </div>
            <div class="external-mapping-stage-content">
                <div class="external-mapping-stage-title">بررسی نهایی فاکتور</div>
                <div class="external-mapping-stage-subtitle">
                    پیش‌فاکتور سپیدار شماره ${escape(preview.quotationNumber)}؛ اطلاعات را بررسی کنید
                </div>
            </div>
            <span class="external-mapping-step">مرحله ۱ از ۱</span>
        </div>
        <div class="external-prefactor-preview">
            <div class="ex-preview-summary">
                <div class="ex-preview-field">
                    <span class="ex-preview-label">نوع فروش:</span>
                    <strong>${escape(preview.saleTypeTitle)}</strong>
                </div>
                <div class="ex-preview-field">
                    <label class="ex-preview-label" for="externalFactorInvoiceNumber">شماره فاکتور:</label>
                    <div class="d-flex flex-column" style="gap: 2px; max-width: 190px;">
                        <input id="externalFactorInvoiceNumber" type="text" inputmode="numeric"
                               autocomplete="off" maxlength="15" dir="ltr"
                               class="form-control form-control-sm" style="height: 30px; padding: 3px 6px;"
                               placeholder="خودکار (شماره بعدی)">
                        <small id="externalFactorInvoiceNumberFeedback" class="text-muted" role="status"
                               style="font-size: 11px;">خالی بماند، شماره بعدی ثبت می‌شود.</small>
                    </div>
                </div>
                <div class="ex-preview-field">
                    <span class="ex-preview-label">تاریخ:</span>
                    <strong>${escape(date)}</strong>
                </div>
                <div class="ex-preview-field ex-preview-customer">
                    <span class="ex-preview-label">مشتری:</span>
                    <strong>${escape(preview.customerName)}</strong>
                </div>
                <div class="ex-preview-field">
                    <span class="ex-preview-label">نرم‌افزار مقصد:</span>
                    <strong class="ex-preview-software">${escape(data.softwareTitle)}</strong>
                </div>
            </div>
            <div class="table-responsive">
                <table class="table table-sm align-middle">
                    <thead>
                        <tr>
                            <th>ردیف</th>
                            <th>محصول CRM</th>
                            <th>کالای مقصد</th>
                            <th>انبار</th>
                            <th>تعداد</th>
                            <th>قیمت واحد</th>
                            <th>مبلغ کالا</th>
                            <th>تخفیف</th>
                            <th>مالیات</th>
                            <th>مبلغ نهایی</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${preview.items.map((item, index) => `
                            <tr>
                                <td>${index + 1}</td>
                                <td>${escape(item.crmProductTitle)}</td>
                                <td>${escape(item.itemTitle)}</td>
                                <td>${escape(item.stockTitle)}</td>
                                <td>${number(item.quantity)}</td>
                                <td>${number(item.fee)}</td>
                                <td>${number(item.price)}</td>
                                <td>${number(item.discount)}</td>
                                <td>${number(item.tax)}</td>
                                <td class="fbold">${number(item.netPrice)}</td>
                            </tr>`).join("")}
                    </tbody>
                </table>
            </div>
            <div class="external-mapping-entity mt-3">
                <div class="d-flex justify-content-between mb-1">
                    <span>جمع مبلغ کالاها</span><strong>${money(preview.price)}</strong>
                </div>
                <div class="d-flex justify-content-between mb-1">
                    <span>تخفیف کل</span><strong>${money(preview.discount)}</strong>
                </div>
                <div class="d-flex justify-content-between mb-1">
                    <span>مالیات کل</span><strong>${money(preview.tax)}</strong>
                </div>
                <hr>
                <div class="d-flex justify-content-between">
                    <strong>مبلغ نهایی</strong><strong>${money(preview.netPrice)}</strong>
                </div>
            </div>
        </div>`;
}


function parseExternalFactorInvoiceNumber() {
    const input = document.getElementById("externalFactorInvoiceNumber");
    if (!input) return { error: "فیلد شماره فاکتور پیدا نشد." };

    const normalized = input.value.trim()
        .replace(/[۰-۹]/g, char => String(char.charCodeAt(0) - 0x06F0))
        .replace(/[٠-٩]/g, char => String(char.charCodeAt(0) - 0x0660))
        .replace(/[٬,\s]/g, "");

    if (!normalized) return { number: null };
    if (!/^\d+$/.test(normalized)) {
        return { error: "شماره فاکتور باید فقط شامل رقم باشد." };
    }

    const number = Number(normalized);
    if (!Number.isInteger(number) || number <= 0 || number > 2147483647) {
        return { error: "شماره فاکتور باید عدد صحیح بین ۱ تا ۲٬۱۴۷٬۴۸۳٬۶۴۷ باشد." };
    }

    return { number };
}

function setExternalFactorInvoiceNumberFeedback(message, tone = "neutral") {
    const feedback = document.getElementById("externalFactorInvoiceNumberFeedback");
    if (!feedback) return;
    feedback.textContent = message;
    feedback.className = tone === "error" ? "text-danger"
        : tone === "success" ? "text-success" : "text-muted";
}

async function validateExternalFactorInvoiceNumber() {
    const state = externalFactorExportState;
    if (!state) return false;

    const parsed = parseExternalFactorInvoiceNumber();
    if (parsed.error) {
        setExternalFactorInvoiceNumberFeedback(parsed.error, "error");
        return false;
    }

    if (parsed.number == null) {
        setExternalFactorInvoiceNumberFeedback("خالی بماند، شماره بعدی ثبت می‌شود.");
        return true;
    }

    setExternalFactorInvoiceNumberFeedback("در حال بررسی شماره در سال مالی سپیدار...");
    try {
        const result = await sendApiRequest({
            url: "/Factor/CheckExternalFactorInvoiceNumber",
            method: "POST",
            data: { ...state.request, invoiceNumber: parsed.number },
            showLoading: false,
            showError: false,
            successMsg: null,
        });

        // Ignore a response for an old value or a preview modal that was closed.
        if (externalFactorExportState !== state ||
            parseExternalFactorInvoiceNumber().number !== parsed.number) return false;

        if (!result || typeof result.available !== "boolean") {
            setExternalFactorInvoiceNumberFeedback("بررسی شماره انجام نشد؛ دوباره تلاش کنید.", "error");
            return false;
        }

        setExternalFactorInvoiceNumberFeedback(
            result.message || (result.available ? "این شماره آزاد است." : "این شماره تکراری است."),
            result.available ? "success" : "error");
        return result.available;
    } catch {
        if (externalFactorExportState === state)
            setExternalFactorInvoiceNumberFeedback("ارتباط برای بررسی شماره برقرار نشد.", "error");
        return false;
    }
}

document.addEventListener("input", event => {
    if (event.target.id !== "externalFactorInvoiceNumber") return;
    setExternalFactorInvoiceNumberFeedback("برای بررسی شماره از فیلد خارج شوید.");
});

document.addEventListener("change", async event => {
    if (event.target.id !== "externalFactorInvoiceNumber") return;
    await validateExternalFactorInvoiceNumber();
});

document.addEventListener("click", async event => {
    if (!event.target.closest("#confirmSendExternalFactorBtn")) return;

    const state = externalFactorExportState;
    if (!state || state.processing) return;

    const parsed = parseExternalFactorInvoiceNumber();
    if (parsed.error) {
        setExternalFactorInvoiceNumberFeedback(parsed.error, "error");
        toastifyError(parsed.error);
        return;
    }

    state.processing = true;
    const button = document.getElementById("confirmSendExternalFactorBtn");
    toggleElementsDisabled(button, true);

    try {
        // Early check improves UX; the server checks again inside the write transaction.
        if (parsed.number != null && !await validateExternalFactorInvoiceNumber()) return;

        const result = await sendApiRequest({
            url: "/Factor/SendFactorToExternalSoftware",
            method: "POST",
            data: { ...state.request, invoiceNumber: parsed.number },
            timeout: 0,
            successMsg: "فاکتور با موفقیت در سپیدار ثبت شد",
        });
        if (!result?.invoiceId) return;

        ModalToggle("sendFactorToExternalSoftwareModal", "hide");
        if (typeof refreshFactorPartial === "function") await refreshFactorPartial();
    } finally {
        state.processing = false;
        toggleElementsDisabled(button, false);
    }
});

document.addEventListener("hidden.bs.modal", event => {
    if (event.target.id === "sendFactorToExternalSoftwareModal")
        externalFactorExportState = null;
});
