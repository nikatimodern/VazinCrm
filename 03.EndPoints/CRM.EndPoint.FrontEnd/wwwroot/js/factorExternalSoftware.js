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
    const money = value => Number(value ?? 0).toLocaleString("en-US");
    const date = preview.invoiceDate?.split("T")[0] ?? "-";

    return `
        <div class="ex-preview-summary">
            <div class="ex-preview-field"><span class="ex-preview-label">نرم‌افزار مقصد:</span><strong>${escape(data.softwareTitle)}</strong></div>
            <div class="ex-preview-field"><span class="ex-preview-label">شماره فاکتور:</span><strong>${escape(preview.invoiceNumber)}</strong></div>
            <div class="ex-preview-field"><span class="ex-preview-label">پیش‌فاکتور سپیدار:</span><strong>${escape(preview.quotationNumber)}</strong></div>
            <div class="ex-preview-field"><span class="ex-preview-label">مشتری:</span><strong>${escape(preview.customerName)}</strong></div>
            <div class="ex-preview-field"><span class="ex-preview-label">تاریخ:</span><strong>${escape(date)}</strong></div>
            <div class="ex-preview-field"><span class="ex-preview-label">نوع فروش:</span><strong>${escape(preview.saleTypeRef)}</strong></div>
        </div>
        <div class="table-responsive mt-3">
            <table class="table table-sm align-middle">
                <thead>
                    <tr>
                        <th>ردیف</th><th>کالا</th><th>شناسه کالا</th><th>انبار</th>
                        <th>تعداد</th><th>قیمت واحد</th><th>مبلغ</th><th>تخفیف</th><th>مالیات</th><th>خالص</th>
                    </tr>
                </thead>
                <tbody>
                    ${preview.items.map(item => `
                        <tr>
                            <td>${escape(item.rowId)}</td>
                            <td>${escape(item.itemTitle)}</td>
                            <td>${escape(item.itemRef)}</td>
                            <td>${escape(item.stockTitle)}</td>
                            <td>${money(item.quantity)}</td>
                            <td>${money(item.fee)}</td>
                            <td>${money(item.price)}</td>
                            <td>${money(item.discount)}</td>
                            <td>${money(item.tax)}</td>
                            <td class="fbold">${money(item.netPrice)}</td>
                        </tr>`).join("")}
                </tbody>
            </table>
        </div>
        <div class="external-mapping-entity mt-3">
            <div class="d-flex justify-content-between"><span>جمع کالاها</span><strong>${money(preview.price)}</strong></div>
            <div class="d-flex justify-content-between"><span>تخفیف</span><strong>${money(preview.discount)}</strong></div>
            <div class="d-flex justify-content-between"><span>مالیات</span><strong>${money(preview.tax)}</strong></div>
            <hr>
            <div class="d-flex justify-content-between"><strong>مبلغ نهایی (ریال)</strong><strong>${money(preview.netPrice)}</strong></div>
        </div>`;
}

document.addEventListener("click", async event => {
    if (!event.target.closest("#confirmSendExternalFactorBtn")) return;

    const state = externalFactorExportState;
    if (!state || state.processing) return;
    state.processing = true;
    const button = document.getElementById("confirmSendExternalFactorBtn");
    toggleElementsDisabled(button, true);

    try {
        const result = await sendApiRequest({
            url: "/Factor/SendFactorToExternalSoftware",
            method: "POST",
            data: state.request,
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
