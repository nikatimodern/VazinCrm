let externalFactorHistory = { factorId: null, serialNumber: null };

function getFactorFinancialHistory() {
    return new ExternalFinancialHistory({
        modal: "externalSoftwareFactorsModal",
        list: "externalSoftwareFactors",
    });
}

async function showFactorExternalSoftwaresModal(factorId, serialNumber) {
    if (!factorId) return;

    externalFactorHistory = { factorId, serialNumber };
    const modal = document.getElementById("externalSoftwareFactorsModal");
    if (!modal) return;
    getFactorFinancialHistory().show({
        title: `مدیریت ارسال فاکتور ${serialNumber}`,
        loadingText: "در حال دریافت سوابق ثبت...",
    });
    await loadFactorExternalSoftwares(factorId);
}

async function loadFactorExternalSoftwares(factorId) {
    await sendApiRequest({
        url: "/Factor/GetExternalSoftwareFactors",
        params: { factorId },
        onSuccess: result => {
            if (externalFactorHistory.factorId === factorId)
                renderExternalSoftwareFactors(result.data ?? []);
        },
    });
}

function renderExternalSoftwareFactors(items = []) {
    getFactorFinancialHistory().render(items, {
        emptyText: "این فاکتور هنوز در نرم‌افزار مالی ثبت نشده است.",
        fields: item => [
            ["نرم‌افزار", item.externalSoftwareTitle, true],
            ["تاریخ ثبت", item.creationDate],
            ["شماره فاکتور", item.externalFactorNumber],
            ["مشتری", item.customerName],
        ],
        actions: item => {
            if (!item.viewUrl) return [];
            const link = document.createElement("a");
            link.className = "external-pf-action external-pf-view";
            link.href = item.viewUrl;
            link.target = "_blank";
            link.rel = "noopener noreferrer";
            link.setAttribute("aria-label", "مشاهده فاکتور ثبت شده");
            link.innerHTML = '<i class="fa fa-eye" aria-hidden="true"></i> مشاهده';
            return [link];
        },
    });
}

document.addEventListener("click", async event => {
    if (!event.target.closest("#sendExternalFactorBtn")) return;
    if (!externalFactorHistory.factorId) return;

    if (isModalOpen("sendFactorToExternalSoftwareModal") ||
        isModalOpen("chooseExternalSoftwareModal")) return;
    // Keep the history dialog open below the destination chooser and export wizard.
    await sendFactorToExternalSoftware(externalFactorHistory.factorId);
});

let externalFactorExportState = null;
let factorFinancialWizard = null;
function getFactorFinancialWizard() {
    if (!factorFinancialWizard?.modal.isConnected) {
        factorFinancialWizard = new ExternalFinancialWizard({
            modal: "sendFactorToExternalSoftwareModal",
            container: "externalFactorPreviewContent",
            back: "backExternalFactorStepBtn",
            confirm: "confirmSendExternalFactorBtn",
        });
    }
    return factorFinancialWizard;
}

/** Start a single destination without losing the user's remaining selected software IDs. */
async function prepareFactorDestination(state) {
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalFactorModes",
        method: "POST",
        data: state.request,
        successMsg: null,
    });
    if (!result?.softwareTitle || externalFactorExportState !== state) return false;
    state.softwareTitle = result.softwareTitle;
    state.hasQuotation = result.hasQuotation === true;
    factorDirectWizard.showMode(state);
    return true;
}

function resetFactorDestination(state, softwareId) {
    state.request = {
        factorId: state.request.factorId,
        externalSoftwareId: softwareId,
        usePreFactor: true,
        includeBroker: false,
        previewBrokerPartyId: null,
    };
    state.direct = { saleTypeId: "", customerId: "", items: {} };
    state.saleTypes = null;
    state.customerData = null;
    state.items = null;
    state.sepidarItems = null;
    state.stocksByItem = null;
    state.stockCache = null;
    state.stockPending = null;
    state.hasLoadedPreview = false;
    state.step = "mode";
}

async function continueFactorDestinations(state) {
    const wizard = getFactorFinancialWizard();
    if (!wizard.hasNextDestination) return false;
    resetFactorDestination(state, wizard.nextDestination());

    const loaded = await prepareFactorDestination(state);
    if (!loaded && externalFactorExportState === state) {
        state.step = "retryDestination";
        wizard.render({
            destination: "نرم‌افزار مالی",
            title: "دریافت اطلاعات مقصد",
            subtitle: "اطلاعات نرم‌افزار بعدی دریافت نشد",
            icon: '<i class="fas fa-exclamation-circle" aria-hidden="true"></i>',
            step: "ادامه انتقال",
            content: '<p class="mb-0">برای ادامه ارسال فاکتور به نرم‌افزار بعدی، دوباره تلاش کنید.</p>',
        });
        wizard.setActions({
            confirmText: "تلاش دوباره", confirmDisabled: false,
            backHidden: true, backDisabled: true,
        });
    }
    return true;
}

async function loadExternalFactorPreview(state, includeBroker) {
    const button = document.getElementById("confirmSendExternalFactorBtn");
    const previousInvoiceNumber = state.hasLoadedPreview
        ? document.getElementById("externalFactorInvoiceNumber")?.value
        : null;
    state.processing = true;
    toggleElementsDisabled(button, true);

    try {
        const result = await sendApiRequest({
            url: "/Factor/PrepareExternalFactor",
            method: "POST",
            data: { ...state.request, includeBroker, previewBrokerPartyId: null },
            successMsg: null,
        });
        if (!result?.preview?.items?.length || externalFactorExportState !== state) return false;
        if (includeBroker && (!result.broker || !Number.isInteger(Number(result.broker.partyId)))) {
            toastifyError("اطلاعات واسط فروش در پیش‌نمایش معتبر نیست.");
            return false;
        }

        state.request.includeBroker = includeBroker;
        state.request.previewBrokerPartyId = includeBroker ? Number(result.broker.partyId) : null;
        state.request.previewToken = state.request.usePreFactor ? null : result.previewToken;
        state.hasLoadedPreview = true;
        state.step = "preview";
        getFactorFinancialWizard().render({
            destination: result.softwareTitle,
            title: "بررسی نهایی فاکتور",
            subtitle: state.request.usePreFactor
                ? "پیش‌فاکتور سپیدار شماره " + result.preview.quotationNumber + "؛ اطلاعات را بررسی کنید"
                : "صدور مستقیم بدون پیش‌فاکتور سپیدار؛ اطلاعات را بررسی کنید",
            icon: '<i class="fas fa-clipboard-check" aria-hidden="true"></i>',
            step: state.request.usePreFactor ? "پیش‌نمایش" : "مرحله ۴ از ۴",
            content: renderExternalFactorPreview(result, state),
            large: true,
        });
        getFactorFinancialWizard().setActions({
            confirmText: "تأیید و ثبت در نرم‌افزار",
            confirmDisabled: false,
            backHidden: false,
            backDisabled: false,
        });
        if (previousInvoiceNumber != null) {
            document.getElementById("externalFactorInvoiceNumber").value = previousInvoiceNumber;
        }
        return true;
    } finally {
        if (externalFactorExportState === state) {
            state.processing = false;
            toggleElementsDisabled(button, false);
            const checkbox = document.getElementById("externalFactorIncludeBroker");
            if (checkbox) checkbox.disabled = false;
        }
    }
}

async function sendFactorToExternalSoftware(factorId) {
    if (!factorId) return;
    const selected = await ExternalFinancialHistory.chooseDestinations({
        ids: factorId, mapType: 3,
    });
    if (!selected?.length) return;

    const wizard = getFactorFinancialWizard();
    wizard.setDestinations(selected);
    if (!wizard.currentDestination) return;
    const state = {
        request: {
            factorId,
            externalSoftwareId: wizard.currentDestination,
            usePreFactor: true,
            includeBroker: false,
            previewBrokerPartyId: null,
        },
        direct: { saleTypeId: "", customerId: "", items: {} },
        processing: false,
        hasLoadedPreview: false,
        completedCount: 0,
        finished: false,
        step: "mode",
    };
    externalFactorExportState = state;
    if (!await prepareFactorDestination(state)) {
        if (externalFactorExportState === state) externalFactorExportState = null;
        return;
    }
    ModalToggle("sendFactorToExternalSoftwareModal", "show");
}

document.addEventListener("change", async event => {
    if (event.target.id !== "externalFactorIncludeBroker") return;
    const state = externalFactorExportState;
    if (!state || state.processing) return;

    const checkbox = event.target;
    checkbox.disabled = true;
    try {
        const loaded = await loadExternalFactorPreview(state, checkbox.checked);
        if (!loaded && externalFactorExportState === state)
            checkbox.checked = state.request.includeBroker;
    } catch {
        if (externalFactorExportState === state) checkbox.checked = state.request.includeBroker;
        toastifyError("دریافت اطلاعات واسط امکان‌پذیر نیست.");
    }
});

function renderExternalFactorPreview(data, state) {
    const preview = data.preview;
    const escape = value => escapeHtml(String(value ?? "-"));
    const number = value => Number(value ?? 0).toLocaleString("fa-IR", {
        maximumFractionDigits: 4,
    });
    const money = value => `${number(value)} ریال`;

    const date = preview.invoiceDateText || preview.invoiceDate?.split("T")[0] || "-";
    return `
        <div class="external-prefactor-preview">
            <div class="ex-preview-summary">
                <div class="ex-preview-field">
                    <span class="ex-preview-label">نوع فروش:</span>
                    <strong>${escape(preview.saleTypeTitle)}</strong>
                </div>
                <div class="ex-preview-field">
                    <label class="ex-preview-label" for="externalFactorInvoiceNumber">شماره فاکتور:</label>
                    <input id="externalFactorInvoiceNumber" type="text" inputmode="numeric"
                           autocomplete="off" maxlength="15" dir="ltr"
                           class="ex-preview-invoice-number digitNumber" placeholder="-">
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
            <section class="external-factor-broker" aria-label="تنظیمات واسط فروش">
                <label for="externalFactorIncludeBroker" class="external-factor-broker-toggle ${state.request.includeBroker ? "is-active" : ""}">
                    <input id="externalFactorIncludeBroker" type="checkbox" role="switch"
                           class="external-factor-broker-input" ${state.request.includeBroker ? "checked" : ""}>
                    <span class="external-factor-broker-switch" aria-hidden="true"></span>
                    <span class="external-factor-broker-toggle-text">
                        <span class="external-factor-broker-title">ثبت واسط برای این فاکتور</span>
                        <span class="external-factor-broker-seller">
                            کارشناس فروش: <strong>${escape(data.salespersonName || "نامشخص")}</strong>
                        </span>
                    </span>
                    <i class="fas fa-user-tie external-factor-broker-icon" aria-hidden="true"></i>
                </label>
                ${state.request.includeBroker && data.broker ? `
                    <div class="external-factor-broker-details">
                        <div class="external-factor-broker-detail">
                            <span>واسط سپیدار</span>
                            <strong>${escape(data.broker.fullName)}</strong>
                        </div>
                        <div class="external-factor-broker-detail">
                            <span>نرخ پورسانت</span>
                            <strong>${number(data.broker.commissionRate)}٪</strong>
                        </div>
                        <div class="external-factor-broker-detail">
                            <span>مبلغ پورسانت</span>
                            <strong>${money(data.broker.commission)}</strong>
                        </div>
                    </div>
                ` : ""}
            </section>
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
        </div>
        <p class="text-muted small mt-3 mb-0">
            با ثبت فاکتور، خروج انبار و سند حسابداری مطابق تنظیمات نرم‌افزار مالی صادر می‌شوند.
        </p>`;
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

function setExternalFactorInvoiceNumberInvalid(invalid) {
    document.getElementById("externalFactorInvoiceNumber")?.classList.toggle("is-invalid", invalid);
}

async function validateExternalFactorInvoiceNumber() {
    const state = externalFactorExportState;
    if (!state) return false;
    
    const parsed = parseExternalFactorInvoiceNumber();
    if (parsed.error) {
        setExternalFactorInvoiceNumberInvalid(true);
        toastifyError(parsed.error);
        return false;
    }
    
    if (parsed.number == null) {
        setExternalFactorInvoiceNumberInvalid(false);
        return true;
    }
    
    try {
        const result = await sendApiRequest({
            url: "/Factor/CheckExternalFactorInvoiceNumber",
            method: "POST",
            data: { ...state.request, invoiceNumber: parsed.number },
            showLoading: false,
            showError: false,
            successMsg: null,
        });
        
        // Do not display a stale error after the number changes or the modal closes.
        if (externalFactorExportState !== state ||
            parseExternalFactorInvoiceNumber().number !== parsed.number) return false;
        
        if (!result || typeof result.available !== "boolean") {
            setExternalFactorInvoiceNumberInvalid(true);
            toastifyError("امکان بررسی شماره فاکتور وجود ندارد.");
            return false;
        }
        
        setExternalFactorInvoiceNumberInvalid(!result.available);
        if (!result.available) {
            toastifyError(result.message ||
                `شماره فاکتور ${parsed.number} قبلاً در این سال مالی ثبت شده است.`);
        }
        return result.available;
    } catch {
        if (externalFactorExportState === state) {
            setExternalFactorInvoiceNumberInvalid(true);
            toastifyError("امکان بررسی شماره فاکتور وجود ندارد.");
        }
        return false;
    }
}

document.addEventListener("input", event => {
    if (event.target.id !== "externalFactorInvoiceNumber") return;
    setExternalFactorInvoiceNumberInvalid(false);
});

document.addEventListener("change", async event => {
    if (event.target.id !== "externalFactorInvoiceNumber") return;
    // A blank input preserves automatic numbering and needs no backend check.
    await validateExternalFactorInvoiceNumber();
});

document.addEventListener("click", async event => {
    if (!event.target.closest("#confirmSendExternalFactorBtn")) return;
    
    const state = externalFactorExportState;
    if (!state || state.processing) return;
    if (state.step !== "preview") {
        await factorDirectWizard.confirm(state);
        return;
    }

    const parsed = parseExternalFactorInvoiceNumber();
    if (parsed.error) {
        setExternalFactorInvoiceNumberInvalid(true);
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
        
        state.completedCount++;
        // Keep the underlying history current even if the remaining destinations are cancelled.
        if (externalFactorHistory.factorId === state.request.factorId) {
            await loadFactorExternalSoftwares(state.request.factorId);
        }
        // A successful financial write must not be repeated when another destination is pending.
        if (await continueFactorDestinations(state)) return;

        state.finished = true;
        ModalToggle("sendFactorToExternalSoftwareModal", "hide");
        if (typeof refreshFactorPartial === "function") await refreshFactorPartial();
    } finally {
        state.processing = false;
        toggleElementsDisabled(button, false);
    }
});

document.addEventListener("hidden.bs.modal", event => {
    if (event.target.id === "sendFactorToExternalSoftwareModal") {
        const state = externalFactorExportState;
        externalFactorExportState = null;
        factorFinancialWizard?.destroy();
        if (state?.completedCount && !state.finished &&
            typeof refreshFactorPartial === "function") {
            void refreshFactorPartial();
        }
        const historyModal = document.getElementById("externalSoftwareFactorsModal");
        if (historyModal && isModalOpen(historyModal))
            historyModal.querySelector("#sendExternalFactorBtn")?.focus({ preventScroll: true });
    }
});

const factorDirectWizard = (function buildFactorDirectWizard() {
    const modal = () => document.getElementById("sendFactorToExternalSoftwareModal");
    const body = () => document.getElementById("externalFactorPreviewContent");
    const button = () => document.getElementById("confirmSendExternalFactorBtn");
    const html = value => escapeHtml(String(value ?? ""));
    const get = (state) => state === externalFactorExportState;
    const setButton = (text, disabled = false) =>
        getFactorFinancialWizard().setActions({ confirmText: text, confirmDisabled: disabled });
    const isReady = state => {
        if (state.step === "saleType") return !!state.direct.saleTypeId;
        if (state.step === "customer") return !!state.direct.customerId;
        if (state.step === "items") return !state.stockPending?.size && state.items?.length > 0 &&
            state.items.every(x => state.direct.items[x.factorItemId]?.externalItemId &&
                state.direct.items[x.factorItemId]?.externalStockId);
        return true;
    };
    const update = state => {
        const confirmText = state.step === "items" ? "تأیید کالاها و انبارها و ادامه"
            : state.step === "retryDestination" ? "تلاش دوباره" : "تأیید و ادامه";
        setButton(confirmText, state.step !== "retryDestination" && !isReady(state));
    };
    const stage = (state, stageId, title, subtitle, icon, count, content, large = false) => {
        if (!get(state)) return;
        state.step = stageId;
        getFactorFinancialWizard().render({
            destination: state.softwareTitle,
            title, subtitle, icon, step: count, content, large,
        });
        getFactorFinancialWizard().setActions({
            backHidden: stageId === "mode",
            backDisabled: stageId === "mode",
        });
        update(state);
    };
    const query = async (action, data) => sendApiRequest({
        url: "/Factor/" + action, method: "POST", data,
        successMsg: null, showLoading: false,
    });

    function showMode(state) {
        state.sourceChoice = state.hasQuotation ? "quotation" : "direct";
        stage(state, "mode", "روش صدور فاکتور", "روش ثبت فاکتور در نرم‌افزار مالی را انتخاب کنید",
            '<i class="fas fa-file-invoice"></i>', "انتخاب روش", `
            <div class="factor-export-source-options">
                <label class="factor-export-source">
                    <input type="radio" name="factorExportSource" value="quotation"
                           ${state.hasQuotation ? "checked" : "disabled"}>
                    <span>
                        <strong>صدور از پیش‌فاکتور نرم‌افزار مالی</strong>
                        <small>${state.hasQuotation
                            ? "استفاده از اطلاعات پیش‌فاکتور ثبت‌شده در نرم‌افزار مالی"
                            : "پیش‌فاکتور این فاکتور در نرم‌افزار مالی ثبت نشده است"}</small>
                    </span>
                </label>
                <label class="factor-export-source">
                    <input type="radio" name="factorExportSource" value="direct"
                           ${state.hasQuotation ? "" : "checked"}>
                    <span>
                        <strong>صدور مستقیم فاکتور</strong>
                        <small>ثبت فاکتور در نرم‌افزار مالی بدون وابستگی به پیش‌فاکتور آن</small>
                    </span>
                </label>
            </div>`);
        body().querySelectorAll('input[name="factorExportSource"]').forEach(input => {
            input.addEventListener("change", () => { if (input.checked) state.sourceChoice = input.value; });
        });
    }

    async function loadSaleType(state) {
        if (!state.saleTypes) {
            const data = await query("PrepareExternalSaleType", {
                externalSoftwareIds: [state.request.externalSoftwareId],
            });
            if (!data || !get(state)) return;
            state.saleTypes = data.saleTypes || [];
        }
        renderSaleType(state);
    }
    function renderSaleType(state) {
        stage(state, "saleType", "انتخاب نوع فروش", "نوع فروش فاکتور را مشخص کنید",
            '<i class="fas fa-file-invoice"></i>', "مرحله ۱ از ۴",
            ExternalFinancialWizard.selectField({
                id: "factorDirectSaleType",
                label: "نوع فروش",
                placeholder: "انتخاب کنید",
                selected: state.direct.saleTypeId,
                options: state.saleTypes.map(x => ({
                    value: x.saleTypeId,
                    label: x.number ? x.number + " - " + x.title : x.title,
                })),
            }));
        const control = body().querySelector("#factorDirectSaleType");
        new TomSelect(control, {
            placeholder: "جستجو و انتخاب نوع فروش",
            onChange: value => { state.direct.saleTypeId = value; update(state); },
        });
        update(state);
    }

    async function loadCustomer(state) {
        if (!state.customerData) {
            const data = await query("PrepareExternalFactorCustomer", state.request);
            if (!data || !get(state)) return;
            state.customerData = data;
            const mapped = data.mappedExternalCustomerId;
            if (!state.direct.customerId && mapped && data.customers?.some(x => String(x.id) === String(mapped)))
                state.direct.customerId = String(mapped);
        }
        renderCustomer(state);
    }
    function renderCustomer(state) {
        const data = state.customerData;
        stage(state, "customer", "تطبیق مشتری", "مشتری CRM را به شخص متناظر در نرم‌افزار مالی متصل کنید",
            '<i class="fas fa-users"></i>', "مرحله ۲ از ۴",
            ExternalFinancialWizard.crmCustomer(data.crmCustomerTitle || "—") +
            ExternalFinancialWizard.selectField({
                id: "factorDirectCustomer",
                label: "مشتری معادل در نرم‌افزار مالی",
                placeholder: "جستجو و انتخاب مشتری",
                selected: state.direct.customerId,
                options: (data.customers || []).map(x => ({
                    value: x.id,
                    label: [x.dlCode, x.fullName].filter(Boolean).join(" - "),
                })),
            }));
        new TomSelect(body().querySelector("#factorDirectCustomer"), {
            placeholder: "جستجو و انتخاب مشتری",
            onChange: value => { state.direct.customerId = value; update(state); },
        });
        update(state);
    }

    async function loadItems(state, refresh = false) {
        if (refresh || !state.items || !state.sepidarItems) {
            const data = await query("PrepareExternalFactorItems", state.request);
            if (!data?.items?.length || !get(state)) return;
            state.items = data.items;
            state.sepidarItems = data.sepidarItems || [];
        }
        renderItems(state);
    }
    function renderItems(state) {
        const { content, add, list } = createExternalItemMatching();
        const rows = [];
        state.stockPending = new Set();
        state.stockCache ??= new Map();
        state.stocksByItem ??= [];

        add.addEventListener("click", () => openSepidarItemCreation({
            softwareId: state.request.externalSoftwareId,
            softwareTitle: state.softwareTitle,
            afterCreate: () => loadItems(state, true),
            canOpen: () => get(state) && state.step === "items" && isModalOpen(modal()),
        }));

        for (const item of state.items) {
            const itemId = String(item.factorItemId);
            const target = appendExternalItemMatchRow(list, item.productTitle, item.weightCount, item.unitTxt);
            const label = document.createElement("label");
            label.htmlFor = "factorDirectItem_" + itemId;
            label.className = "form-label mb-0";
            label.textContent = "کالای معادل در نرم‌افزار مالی";

            const select = document.createElement("select");
            select.id = label.htmlFor;
            select.dataset.itemId = itemId;
            select.className = "factor-direct-item-select show-dropdown-arrow";
            select.append(new Option("", ""));
            const selected = state.direct.items[itemId]?.externalItemId;
            for (const entry of state.sepidarItems) {
                select.append(new Option(
                    entry.code + " - " + entry.title, entry.id, false,
                    String(entry.id) === String(selected ?? ""),
                ));
            }
            target.append(label, select);
            rows.push({ itemId, target, select });
        }

        stage(state, "items", "تطبیق کالاها و انبارها",
            "برای هر ردیف فاکتور، کالای معادل و انبار مرتبط را انتخاب کنید",
            '<i class="fas fa-boxes"></i>', "مرحله ۳ از ۴", content, true);

        for (const { itemId, target, select } of rows) {
            const stock = ExternalFinancialWizard.linkedStockField(target, {
                id: "factorDirectStock_" + itemId,
                softwareId: state.request.externalSoftwareId,
                cache: state.stockCache,
                isCurrent: () => get(state) && state.step === "items",
                onChange: value => {
                    const mapping = state.direct.items[itemId];
                    if (!mapping) return;
                    mapping.externalStockId = value;
                    update(state);
                },
                onLoading: loading => {
                    if (loading) state.stockPending.add(itemId);
                    else state.stockPending.delete(itemId);
                    update(state);
                },
                onLoaded: (externalItemId, stocks) => {
                    const index = state.stocksByItem.findIndex(
                        item => String(item.itemRef) === externalItemId);
                    const entry = { itemRef: Number(externalItemId), stocks };
                    if (index < 0) state.stocksByItem.push(entry);
                    else state.stocksByItem[index] = entry;
                },
            });

            new TomSelect(select, {
                placeholder: "جستجو و انتخاب کالا",
                onChange: value => {
                    const previous = state.direct.items[itemId] || {};
                    if (!value) {
                        delete state.direct.items[itemId];
                    } else {
                        state.direct.items[itemId] = {
                            externalItemId: value,
                            externalStockId: String(previous.externalItemId) === String(value)
                                ? previous.externalStockId ?? "" : "",
                        };
                    }
                    void stock.update(value, state.direct.items[itemId]?.externalStockId ?? "");
                    update(state);
                },
            });

            const mapping = state.direct.items[itemId];
            void stock.update(mapping?.externalItemId ?? "", mapping?.externalStockId ?? "");
        }
        update(state);
    }

    async function preview(state) {
        state.request.usePreFactor = false;
        state.request.saleTypeId = Number(state.direct.saleTypeId);
        state.request.externalCustomerId = Number(state.direct.customerId);
        state.request.itemMappings = state.items.map(x => ({
            factorItemId: x.factorItemId,
            externalItemId: Number(state.direct.items[x.factorItemId].externalItemId),
            externalStockId: Number(state.direct.items[x.factorItemId].externalStockId),
        }));
        await loadExternalFactorPreview(state, false);
    }

    async function confirm(state) {
        if (!get(state) || state.processing || state.busy) return;
        state.busy = true;
        toggleElementsDisabled(button(), true);
        try {
            switch (state.step) {
                case "retryDestination":
                    await prepareFactorDestination(state);
                    break;
                case "mode":
                    state.request.usePreFactor = state.sourceChoice === "quotation";
                    if (state.request.usePreFactor) await loadExternalFactorPreview(state, false);
                    else await loadSaleType(state);
                    break;
                case "saleType":
                    if (!state.direct.saleTypeId) return toastifyError("نوع فروش را انتخاب کنید.");
                    await loadCustomer(state);
                    break;
                case "customer":
                    if (!state.direct.customerId) return toastifyError("مشتری مقصد را انتخاب کنید.");
                    const saved = await query("SaveExternalFactorCustomer", {
                        ...state.request, externalCustomerId: Number(state.direct.customerId),
                    });
                    if (saved?.saved) await loadItems(state);
                    break;
                case "items":
                    if (!isReady(state)) return toastifyError("برای تمام ردیف‌های فاکتور کالا و انبار را انتخاب کنید.");
                    await preview(state);
                    break;
            }
        } finally {
            if (get(state)) {
                state.busy = false;
                if (state.step !== "preview") update(state);
            }
        }
    }

    function back(state) {
        if (!get(state) || state.processing || state.busy) return;
        switch (state.step) {
            case "saleType": showMode(state); break;
            case "customer": renderSaleType(state); break;
            case "items": renderCustomer(state); break;
            case "preview":
                state.request.includeBroker = false;
                state.request.previewBrokerPartyId = null;
                state.hasLoadedPreview = false;
                if (state.request.usePreFactor) showMode(state);
                else renderItems(state);
                break;
        }
    }
    document.addEventListener("click", event => {
        if (event.target.closest("#backExternalFactorStepBtn"))
            back(externalFactorExportState);
    });

    return {showMode, confirm, back};
})();
