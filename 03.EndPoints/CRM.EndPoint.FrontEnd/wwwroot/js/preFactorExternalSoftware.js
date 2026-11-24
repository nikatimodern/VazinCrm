/** ERP quotation export to external financial software.
 *  Shares its UI shell with invoice and purchase exports without changing its API steps.
 */

/* ----------------------------------------------------------------------------------------------------------- */

async function sendPreFactorToExternalSoftware(id) {
    const selected = await ExternalFinancialHistory.chooseDestinations({
        ids: id, mapType: 2,
    });
    if (!selected?.length) return;
    await exSoftwarePreFactorStart({
        preFactorId: Array.isArray(id) ? id[0] : id,
        softwareIds: selected,
    });
}

async function deletePreFactorDestination(button, key, historyModal) {
    if (button.disabled) return;
    button.disabled = true;
    try {
        const result = await sendApiRequest({
            url: "/PreFactorDestination/Details", params: key, showLoading: false,
        });
        if (!result?.document || !button.isConnected || !isModalOpen(historyModal)) return;
        const documentState = result.document;
        const text = ExternalFinancialHistory.element("div");
        const blockers = [documentState.restriction, ...documentState.deleteBlockers].filter(Boolean);
        if (blockers.length) {
            text.append(ExternalFinancialHistory.element("p", null, "این پیش‌فاکتور در مقصد قابل حذف نیست."));
            const list = ExternalFinancialHistory.element("ul", "text-start mb-0 small");
            for (const blocker of blockers) list.append(ExternalFinancialHistory.element("li", null, blocker));
            text.append(list);
        } else {
            text.textContent = `پیش‌فاکتور شماره ${documentState.number} و اقلام آن از «${documentState.softwareTitle}» حذف شوند؟`;
        }
        const confirmation = await Swal.fire({
            target: historyModal, title: "حذف پیش‌فاکتور از مقصد", html: text, icon: "warning",
            showCancelButton: !blockers.length, focusCancel: true,
            confirmButtonText: blockers.length ? "بستن" : "حذف پیش‌فاکتور", cancelButtonText: "انصراف",
        });
        if (blockers.length || !confirmation.isConfirmed || !isModalOpen(historyModal)) return;
        try {
            await sendApiRequest({
                url: "/PreFactorDestination/Delete", method: "POST", showLoading: false,
                headers: ExternalFinancialWizard.antiforgeryHeaders(historyModal),
                data: { key, version: documentState.version, lastModificationDate: documentState.lastModificationDate },
                successMsg: "پیش‌فاکتور از نرم‌افزار مقصد حذف شد.",
            });
        } finally {
            if (externalSotwarePfId === key.sourceId) await getPreFactorExternalSotwares(key.sourceId);
        }
    } finally {
        button.disabled = false;
    }
}

async function openPreFactorDestinationEdit(button, key, historyModal) {
    const modal = document.getElementById("preFactorDestinationEditModal");
    if (!modal || modal.dataset.active || button.disabled) return;
    modal.dataset.active = "true";
    button.disabled = true;
    let opened = false;
    try {
        const data = await sendApiRequest({
            url: "/PreFactorDestination/Details", params: { ...key, editing: true }, showLoading: false,
        });
        if (!data?.document || !button.isConnected || !isModalOpen(historyModal)) return;
        if (data.document.restriction) {
            toastifyError(data.document.restriction);
            return;
        }
        renderPreFactorDestinationEdit(modal, key, data);
        opened = true;
    } finally {
        if (!opened) {
            delete modal.dataset.active;
            button.disabled = false;
        } else {
            modal.addEventListener("hidden.bs.modal", () => {
                delete modal.dataset.active;
                button.disabled = false;
                if (button.isConnected)
                    (button.closest(".external-pf-operations")?.querySelector(".external-pf-operations-toggle") ?? button).focus();
            }, { once: true });
        }
    }
}

function renderPreFactorDestinationEdit(modal, key, { document: quote, saleTypes, customers, items, stocks }) {
    const wizard = new ExternalFinancialWizard({
        modal, container: "preFactorDestinationEditBody", back: "preFactorDestinationEditBack", confirm: "preFactorDestinationEditSave",
    });
    const element = ExternalFinancialHistory.element;
    const content = element("div");
    const currentOption = (options, value, label) =>
        options.some(option => option.value === value) ? options : [...options, { value, label }];
    const saleOptions = saleTypes.map(type => ({ value: type.saleTypeId, label: type.title }));
    const customerOptions = customers.map(customer => ({
        value: customer.id, label: [customer.dlCode, customer.fullName].filter(Boolean).join(" - "),
    }));
    const itemOptions = items.map(item => ({ value: item.id, label: item.code + " - " + item.title }));
    const field = (id, label, options, selected, title) => ExternalFinancialWizard.selectField({
        id, label, options: currentOption(options, selected, title), selected,
    });
    content.innerHTML = `<div class="row g-2 mb-3">
        <div class="col-12 col-sm-6">${field("pfDestinationSaleType", "نوع فروش", saleOptions, quote.saleTypeId, quote.saleTypeId)}</div>
        <div class="col-12 col-sm-6">
            ${field("pfDestinationCustomer", "مشتری معادل", customerOptions, quote.customerId, quote.customerName ?? "—")}
            ${quote.canChangeCustomer ? "" : '<small id="pfDestinationCustomerReason" class="d-block mt-1 text-muted">به‌دلیل وجود سند وابسته، مشتری قابل تغییر نیست.</small>'}
        </div>
    </div>`;

    const list = element("div", "external-item-match-list");
    content.append(list);
    const rows = quote.items.map(row => {
        const target = appendExternalItemEditRow(list, row);
        const source = target.previousElementSibling;
        if (row.usedQuantity || row.deliveredQuantity) {
            source.append(element("small", null, `فاکتور شده ${row.usedQuantity} · خروج شده ${row.deliveredQuantity}`));
        }
        if (!row.canChangeItem) {
            target.append(element("small", null, "تغییر کالای معادل برای این ردیف دارای اطلاعات تکمیلی ممکن نیست."));
        } else {
            target.innerHTML = field("pfDestinationItem_" + row.id, "کالای معادل", itemOptions, row.itemId, row.itemTitle);
        }
        if (!row.canChangeStock) target.append(element("small", null, "انبار مقصد " + row.stockTitle));
        const notice = element("small", "text-muted", "با تغییر کالا، مقدار فاکتورشده این ردیف صفر می‌شود.");
        notice.hidden = true;
        target.append(notice);
        return { row, target, notice, itemId: row.itemId, stockId: row.stockId, pending: false };
    });

    wizard.render({
        destination: quote.softwareTitle, position: `پیش‌فاکتور شماره ${quote.number}`,
        title: "ویرایش اطلاعات انتقال", subtitle: "نوع فروش، مشتری معادل، کالا و انبار مقصد را بررسی کنید",
        icon: '<i class="fas fa-pencil-alt" aria-hidden="true"></i>', step: "", content, large: true,
    });
    const sale = content.querySelector("#pfDestinationSaleType");
    const customer = content.querySelector("#pfDestinationCustomer");
    const customerReason = content.querySelector("#pfDestinationCustomerReason");
    customer.disabled = !quote.canChangeCustomer;
    new TomSelect(sale, { create: false });
    const customerControl = new TomSelect(customer, { create: false });
    if (customerReason) {
        customer.setAttribute("aria-describedby", customerReason.id);
        customerControl.control_input.setAttribute("aria-describedby", customerReason.id);
    }
    const getCustomerId = () => quote.canChangeCustomer ? Number(customer.value) : quote.customerId;
    const stockCache = new Map(stocks.map(entry => [String(entry.itemRef), Promise.resolve(entry.stocks)]));
    let saving = false;
    for (const state of rows) {
        const { row, target, notice } = state;
        if (!row.canChangeStock) continue;
        const stock = ExternalFinancialWizard.linkedStockField(target, {
            id: "pfDestinationStock_" + row.id, softwareId: key.externalSoftwareId, cache: stockCache,
            retainedStock: { itemId: row.itemId, stockId: row.stockId, title: row.stockTitle },
            isCurrent: () => content.isConnected,
            onChange: value => { state.stockId = Number(value); updateActions(); },
            onLoading: value => { state.pending = value; updateActions(); },
        });
        const item = target.querySelector("#pfDestinationItem_" + row.id);
        if (item) new TomSelect(item, {
            create: false, onChange: value => {
                state.itemId = Number(value);
                state.stockId = state.itemId === row.itemId ? row.stockId : 0;
                notice.hidden = !row.usedQuantity || !state.itemId || state.itemId === row.itemId;
                void stock.update(value, state.stockId);
                updateActions();
            },
        });
        void stock.update(row.itemId, row.stockId);
    }

    function updateActions() {
        const changed = Number(sale.value) !== quote.saleTypeId ||
            getCustomerId() !== quote.customerId ||
            rows.some(({ row, itemId, stockId }) => itemId !== row.itemId || stockId !== row.stockId);
        wizard.setActions({ confirmText: "ذخیره تغییرات", backText: "انصراف", backDisabled: saving,
            confirmDisabled: saving || !changed || !sale.value || !getCustomerId() ||
                rows.some(row => row.pending || !row.itemId || !row.stockId) });
    }
    sale.addEventListener("change", updateActions);
    if (quote.canChangeCustomer) customer.addEventListener("change", updateActions);
    wizard.back.onclick = () => ModalToggle(modal, "hide");
    const close = modal.querySelector(".btn-close");
    const guardClose = event => { if (saving) event.preventDefault(); };
    modal.addEventListener("hide.bs.modal", guardClose);
    modal.addEventListener("hidden.bs.modal", () => {
        modal.removeEventListener("hide.bs.modal", guardClose);
        wizard.destroy();
        wizard.container.replaceChildren();
        wizard.confirm.onclick = wizard.back.onclick = null;
    }, { once: true });
    wizard.confirm.onclick = async () => {
        if (saving || wizard.confirm.disabled) return;
        saving = close.disabled = true;
        updateActions();
        let result;
        try {
            result = await sendApiRequest({
                url: "/PreFactorDestination/Save", method: "POST", showLoading: false,
                headers: ExternalFinancialWizard.antiforgeryHeaders(modal),
                data: {
                    key, version: quote.version, lastModificationDate: quote.lastModificationDate,
                    saleTypeId: Number(sale.value), customerId: getCustomerId(),
                    items: rows.map(({ row, itemId, stockId }) => ({ id: row.id, itemId, stockId })),
                },
                successMsg: "اطلاعات انتقال پیش‌فاکتور به‌روزرسانی شد.",
            });
        } finally {
            saving = close.disabled = false;
            updateActions();
        }
        if (!result?.saved) return;
        ModalToggle(modal, "hide");
        if (externalSotwarePfId === key.sourceId) await getPreFactorExternalSotwares(key.sourceId);
    };
    updateActions();
    ModalToggle(modal, "show");
}


const getExternalMappingModal = () => document.getElementById("sendPreFactorToExternalSoftwareModal");
const getExternalMappingList = () => document.querySelector("#sendPreFactorToExternalSoftwareModal #externalMappingList");
const getConfirmExternalMappingBtn = () => document.querySelector("#sendPreFactorToExternalSoftwareModal #confirmExternalMappingBtn");
const getBackExternalMappingBtn = () => document.querySelector("#sendPreFactorToExternalSoftwareModal #backExternalMappingBtn");

let preFactorFinancialWizard = null;
function getPreFactorFinancialWizard() {
    if (!preFactorFinancialWizard?.modal.isConnected) {
        preFactorFinancialWizard = new ExternalFinancialWizard({
            modal: getExternalMappingModal(),
            container: getExternalMappingList(),
            back: getBackExternalMappingBtn(),
            confirm: getConfirmExternalMappingBtn(),
        });
    }
    return preFactorFinancialWizard;
}


/* -------------------------------------------------------
   State
------------------------------------------------------- */

const createExternalSoftwareState = () => ({
    preFactorId: null,
    preFactor: null,
    
    softwareIds: [],
    softwareIndex: 0,
    software: null, // دیتاهای مربوط به هر نرم‌افزار داخل این ابجکت نگه داری می شود
    
    step: "saleType",
    
    mappings: {
        customerId: null,
        saleTypeId: null,
        items: {},
    },
    
    processing: false,
});

let externalSoftwareProcess = createExternalSoftwareState();


/* -------------------------------------------------------
   Reset / Cleanup
------------------------------------------------------- */

function exSoftwarePreFactorReset() {
    if (preFactorFinancialWizard) preFactorFinancialWizard.destroy();
    externalSoftwareProcess = createExternalSoftwareState();
}


function exSoftwarePreFactorDestroySelects() {
    if (preFactorFinancialWizard) preFactorFinancialWizard.clearSelects();
}


document.addEventListener("hidden.bs.modal", event => {
    if (event.target.id === "sendPreFactorToExternalSoftwareModal") exSoftwarePreFactorReset();
});


/* -------------------------------------------------------
   Process
------------------------------------------------------- */

async function exSoftwarePreFactorStart({ preFactorId, softwareIds }) {
    exSoftwarePreFactorReset();
    
    externalSoftwareProcess.preFactorId = preFactorId;
    externalSoftwareProcess.softwareIds = [...new Set(softwareIds ?? [])];
    
    await exSoftwarePreFactorNext();
}


async function exSoftwarePreFactorNext() {
    const state = externalSoftwareProcess;
    
    if (state.softwareIndex >= state.softwareIds.length) {
        return;
    }
    
    state.software = {
        externalSoftwareId: state.softwareIds[state.softwareIndex],
    };
    
    await exSoftwarePreFactorLoadSaleTypes();
    
    ModalToggle(getExternalMappingModal(), "show");
}


async function exSoftwarePreFactorFinish() {
    const state = externalSoftwareProcess;
    
    state.softwareIndex++;
    
    if (state.softwareIndex >= state.softwareIds.length) {
        ModalToggle(getExternalMappingModal(), "hide");
        await getPreFactorExternalSotwares(externalSotwarePfId);
        return;
    }
    
    state.software = null;
    state.mappings = {
        customerId: null,
        saleTypeId: null,
        items: {},
    };
    state.step = "saleType";
    
    await exSoftwarePreFactorNext();
}


/* -------------------------------------------------------
   Step Rendering
------------------------------------------------------- */

function exSoftwarePreFactorRenderStep({ title, subtitle, icon, step, content, large = false }) {
    const state = externalSoftwareProcess;
    getPreFactorFinancialWizard().render({
        destination: state.software?.externalSoftwareTitle,
        position: `نرم‌افزار ${state.softwareIndex + 1} از ${state.softwareIds.length}`,
        title, subtitle, icon, step, content, large,
    });
}


function exSoftwarePreFactorSetConfirmButton(text, disabled = false) {
    getPreFactorFinancialWizard().setActions({ confirmText: text, confirmDisabled: disabled });
}


/* -------------------------------------------------------
   Sale Type
------------------------------------------------------- */

async function exSoftwarePreFactorLoadSaleTypes() {
    const state = externalSoftwareProcess;
    
    if (state.software?.saleTypes?.length) {
        state.step = "saleType";
        exSoftwarePreFactorRenderSaleType();
        return;
    }
    
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalSaleType",
        method: "POST",
        successMsg: null,
        showLoading: false,
        data: {
            externalSoftwareIds: [state.software.externalSoftwareId],
        },
    });
    
    if (!result) return;
    
    state.software = {
        ...state.software,
        ...result,
    };
    
    state.step = "saleType";
    
    exSoftwarePreFactorRenderSaleType();
}


function exSoftwarePreFactorRenderSaleType() {
    const state = externalSoftwareProcess;
    
    exSoftwarePreFactorRenderStep({
        title: "انتخاب نوع فروش",
        subtitle: "نوع فروش پیش‌فاکتور را مشخص کنید",
        icon: `<i class="fas fa-file-invoice"></i>`,
        step: "مرحله ۱ از ۴",
        
        content: ExternalFinancialWizard.selectField({
            id: "externalMappingSaleTypeSelect",
            label: "نوع فروش",
            required: true,
            selected: state.mappings.saleTypeId ?? "2",
            options: (state.software.saleTypes ?? []).map(item => ({
                value: item.saleTypeId, label: item.title,
            })),
        }),
    });
    
    const select = getExternalMappingList().querySelector("#externalMappingSaleTypeSelect");
    
    new TomSelect(select, {
        placeholder: "جستجو و انتخاب نوع فروش",
        onChange: exSoftwarePreFactorUpdateConfirmButton,
    });
    
    exSoftwarePreFactorSetConfirmButton("تأیید و ادامه", !select?.value);
}


async function exSoftwarePreFactorConfirmSaleType() {
    const state = externalSoftwareProcess;
    
    const saleTypeId = getExternalMappingList().querySelector("#externalMappingSaleTypeSelect")?.value;
    
    if (!saleTypeId) {
        toastifyError("نوع فروش را انتخاب کنید.");
        return;
    }
    
    state.mappings.saleTypeId = saleTypeId;
    await exSoftwarePreFactorLoadCustomer();
}


/* -------------------------------------------------------
   Customer
------------------------------------------------------- */

async function exSoftwarePreFactorLoadCustomer() {
    const state = externalSoftwareProcess;
    
    if (state.software?.customers?.length) {
        state.step = "customer";
        exSoftwarePreFactorRenderCustomer();
        return;
    }
    
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalCustomerMapping",
        method: "POST",
        successMsg: null,
        showLoading: false,
        data: {
            ids: [state.preFactorId],
            externalSoftwareIds: [state.software.externalSoftwareId],
        },
    });
    
    if (!result?.length) return;
    
    state.software = {
        ...state.software,
        ...result[0],
    };
    
    
    state.step = "customer";
    exSoftwarePreFactorRenderCustomer();
}


function exSoftwarePreFactorRenderCustomer() {
    const state = externalSoftwareProcess;
    const software = state.software;
    
    const mappedCustomerId = state.mappings.customerId ?? software.mappedExternalCustomerId;
    
    exSoftwarePreFactorRenderStep({
        title: "تطبیق مشتری",
        subtitle: "اتصال مشتری ERP به نرم‌افزار مالی",
        icon: `<i class="fas fa-users"></i>`,
        step: "مرحله ۲ از ۴",
        
        content: ExternalFinancialWizard.crmCustomer(software.crmCustomerTitle ?? "-") +
            ExternalFinancialWizard.selectField({
                id: "externalMappingCustomerSelect",
                label: "مشتری معادل در نرم‌افزار مالی",
                required: true,
                placeholder: "جستجو و انتخاب مشتری",
                selected: mappedCustomerId,
                options: (software.customers ?? []).map(customer => ({
                    value: customer.id,
                    label: [customer.dlCode, customer.fullName].filter(Boolean).join(" - "),
                })),
            }),
    });
    
    const select = getExternalMappingList().querySelector("#externalMappingCustomerSelect");
    
    new TomSelect(select, {
        placeholder: "جستجو و انتخاب مشتری",
        onChange: exSoftwarePreFactorUpdateConfirmButton,
    });
    
    exSoftwarePreFactorSetConfirmButton("تأیید و ادامه", !select?.value);
}


async function exSoftwarePreFactorConfirmCustomer() {
    const state = externalSoftwareProcess;
    const customerId = getExternalMappingList().querySelector("#externalMappingCustomerSelect")?.value;
    
    if (!customerId) {
        toastifyError("مشتری نرم‌افزار مالی را انتخاب کنید.");
        return;
    }
    
    await sendApiRequest({
        url: "/Factor/SaveExternalCustomerMapping",
        method: "POST",
        headers: ExternalFinancialWizard.antiforgeryHeaders(getExternalMappingModal()),
        successMsg: null,
        showLoading: false,
        data: {
            preFactorId: state.preFactorId,
            externalSoftwareId: state.software.externalSoftwareId,
            externalCustomerId: customerId,
        },
        onSuccess: async () => {
            state.mappings.customerId = customerId;
            await exSoftwarePreFactorLoadItems();
        },
    });
    
}


/* -------------------------------------------------------
   Items
------------------------------------------------------- */

async function exSoftwarePreFactorLoadItems(reload = false) {
    const state = externalSoftwareProcess;
    
    if (!reload && state.preFactor?.items?.length && state.software?.sepidarItems?.length) {
        state.step = "items";
        exSoftwarePreFactorRenderItems();
        return true;
    }
    
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalItemMapping",
        method: "POST",
        successMsg: null,
        showLoading: false,
        data: {
            preFactorId: state.preFactorId,
            externalSoftwareId: state.software.externalSoftwareId,
        },
    });
    
    if (!result?.preFactor.items?.length) {
        toastifyError("ردیفی برای تطبیق کالا پیدا نشد.");
        return false;
    }
    
    
    state.preFactor = result.preFactor;
    
    state.software = {
        ...state.software,
        sepidarItems: result.sepidarItems,
    };
    
    if (!reload) state.mappings.items = {};
    state.step = "items";
    
    exSoftwarePreFactorRenderItems();
    return true;
}


function exSoftwarePreFactorRenderItems() {
    const state = externalSoftwareProcess;
    const software = state.software;
    const { content, add, refresh, list } = createExternalItemMatching();
    refresh.addEventListener("click", () =>
        refreshExternalItemMatching(refresh, () => exSoftwarePreFactorLoadItems(true)));
    const rows = [];
    state.stockPending = new Set();
    software.stockCache ??= new Map();
    software.stocksByItem ??= [];

    add.addEventListener("click", async () => {
        add.disabled = true;
        try {
            await openSepidarItemCreation({
                softwareId: software.externalSoftwareId,
                softwareTitle: software.externalSoftwareTitle,
                afterCreate: () => exSoftwarePreFactorLoadItems(true),
                canOpen: () => externalSoftwareProcess === state && state.software === software
                    && isModalOpen(getExternalMappingModal()),
            });
        } finally {
            add.disabled = false;
        }
    });

    for (const item of state.preFactor.items ?? []) {
        const itemId = String(item.preFactorItemId);
        const target = appendExternalItemMatchRow(list, item.productTitle, item.weightCount, item.unitTxt);
        const label = document.createElement("label");
        label.className = "form-label mb-0";
        label.htmlFor = "externalMappingItem_" + itemId;
        label.textContent = "کالای معادل در نرم‌افزار مالی";

        const select = document.createElement("select");
        select.id = label.htmlFor;
        select.dataset.crmItemId = itemId;
        select.className = "external-mapping-item-select show-dropdown-arrow";
        select.required = true;
        const selected = String(state.mappings.items[itemId]?.externalItemId ?? "");
        select.append(new Option("", ""));
        for (const product of software.sepidarItems ?? []) {
            select.append(new Option(
                product.code + " - " + product.title, product.id, false,
                String(product.id) === selected,
            ));
        }
        target.append(label, select);
        rows.push({ itemId, target, select });
    }

    exSoftwarePreFactorRenderStep({
        title: "تطبیق کالاها و انبارها",
        subtitle: "برای هر ردیف پیش‌فاکتور، کالای معادل و انبار مرتبط را انتخاب کنید",
        icon: '<i class="fas fa-boxes" aria-hidden="true"></i>',
        step: "مرحله ۳ از ۴", large: true, content,
    });

    for (const { itemId, target, select } of rows) {
        const linkedStock = ExternalFinancialWizard.linkedStockField(target, {
            id: "externalMappingStock_" + itemId,
            softwareId: software.externalSoftwareId,
            required: true,
            cache: software.stockCache,
            isCurrent: () => externalSoftwareProcess === state && state.software === software
                && state.step === "items",
            onChange: value => {
                const mapping = state.mappings.items[itemId];
                if (!mapping) return;
                if (value) mapping.externalStockId = value;
                else delete mapping.externalStockId;
                exSoftwarePreFactorUpdateConfirmButton();
            },
            onLoading: loading => {
                if (loading) state.stockPending.add(itemId);
                else state.stockPending.delete(itemId);
                exSoftwarePreFactorUpdateConfirmButton();
            },
            onLoaded: (externalItemId, stocks) => {
                const existing = software.stocksByItem.findIndex(
                    entry => String(entry.itemRef) === externalItemId);
                const entry = { itemRef: Number(externalItemId), stocks };
                if (existing < 0) software.stocksByItem.push(entry);
                else software.stocksByItem[existing] = entry;
            },
        });

        new TomSelect(select, {
            placeholder: "جستجو و انتخاب کالا",
            onChange: value => {
                const mapping = state.mappings.items[itemId] ?? {};
                const changed = String(mapping.externalItemId ?? "") !== String(value);
                if (!value) {
                    delete state.mappings.items[itemId];
                } else {
                    mapping.externalItemId = value;
                    if (changed) delete mapping.externalStockId;
                    state.mappings.items[itemId] = mapping;
                }
                void linkedStock.update(value, mapping.externalStockId ?? "");
                exSoftwarePreFactorUpdateConfirmButton();
            },
        });

        const mapping = state.mappings.items[itemId];
        void linkedStock.update(mapping?.externalItemId ?? "", mapping?.externalStockId ?? "");
    }

    exSoftwarePreFactorSetConfirmButton(
        "تأیید کالاها و انبارها و ادامه", !exSoftwarePreFactorAreAllItemsMappedWithStock());
}


function exSoftwarePreFactorAreAllItemsMappedWithStock() {
    const state = externalSoftwareProcess;
    const items = state.preFactor?.items ?? [];
    return !state.stockPending?.size && items.length > 0 && items.every(item => {
        const mapping = state.mappings.items[String(item.preFactorItemId)];
        return mapping?.externalItemId && mapping?.externalStockId;
    });
}


function exSoftwarePreFactorConfirmItems() {
    if (!exSoftwarePreFactorAreAllItemsMappedWithStock()) {
        toastifyError("برای تمام ردیف‌های پیش‌فاکتور کالا و انبار را انتخاب کنید.");
        return;
    }
    exSoftwarePreFactorPreview();
}


/* -------------------------------------------------------
   Preview
------------------------------------------------------- */

function exSoftwarePreFactorPreview() {
    const state = externalSoftwareProcess;
    
    if (!exSoftwarePreFactorAreAllItemsMappedWithStock()) {
        toastifyError("برای تمام ردیف‌های پیش‌فاکتور کالا و انبار را انتخاب کنید.");
        return;
    }
    
    state.step = "preview";
    
    exSoftwarePreFactorRenderPreview();
}


function exSoftwarePreFactorBuildRequest() {
    const state = externalSoftwareProcess;
    
    return {
        preFactorId: state.preFactorId,
        externalSoftwareId: state.software.externalSoftwareId,
        externalCustomerId: Number(state.mappings.customerId ?? state.software.mappedExternalCustomerId),
        saleTypeId: state.mappings.saleTypeId,
        
        itemMappings: Object.entries(state.mappings.items).map(
            ([preFactorItemId, mapping]) => ({
                preFactorItemId: preFactorItemId,
                externalItemId: Number(mapping.externalItemId),
                externalStockId: Number(mapping.externalStockId),
            }),
        ),
    };
}


function exSoftwarePreFactorRenderPreview(data) {
    const state = externalSoftwareProcess;
    const preFactor = state.preFactor;
    const software = state.software;
    const mappings = state.mappings;
    
    const money = value => Number(value ?? 0).toLocaleString();
    
    const items = (preFactor.items ?? []).map(item => {
        const mapping = mappings.items[String(item.preFactorItemId)];
        const externalItem = (software.sepidarItems ?? []).find(x => String(x.id) === String(mapping?.externalItemId));
        
        const itemStocks = (software.stocksByItem ?? []).find(x => String(x.itemRef) === String(mapping?.externalItemId))?.stocks ?? [];
        const externalStock = itemStocks.find(x => String(x.id) === String(mapping?.externalStockId));
        
        
        return {
            ...item,
            productTitle: item.productTitle,
            externalItemTitle: externalItem ? `${externalItem.code} - ${externalItem.title}` : "-",
            externalStockTitle: externalStock ? `${externalStock.code} - ${externalStock.title}` : "-",
        };
    });
    
    const saleType = (software.saleTypes ?? []).find(x => String(x.saleTypeId) === String(mappings.saleTypeId));
    const saleTypeTitle = saleType ? `${saleType.number} - ${saleType.title}` : "-";
    
    const customer = (software.customers ?? []).find(x => String(x.id) === String(mappings.customerId));
    const customerTitle = customer ? `${customer.dlCode} - ${customer.fullName}` : "-";
    
    exSoftwarePreFactorRenderStep({
        title: "بررسی نهایی پیش‌فاکتور",
        subtitle: "قبل از ثبت در نرم‌افزار مالی، اطلاعات را بررسی کنید",
        icon: `<i class="fas fa-clipboard-check"></i>`,
        step: "مرحله ۴ از ۴",
        large: true,
        
        content: renderExternalFinancialPreview({
            fields: [
                { label: "نوع فروش", value: saleTypeTitle },
                { label: "شماره پیش‌فاکتور", value: preFactor?.serialNumber },
                { label: "تاریخ", value: preFactor?.date },
                { label: "مشتری", value: customerTitle, wide: true },
                { label: "نرم‌افزار مقصد", value: software.externalSoftwareTitle, accent: true },
            ],
            columns: ["ردیف", "محصول ERP", "کالای مقصد", "انبار", "تعداد", "قیمت واحد", "تخفیف", "مالیات", "مبلغ نهایی"],
            rows: items.map((item, index) => [
                index + 1, item.productTitle, item.externalItemTitle, item.externalStockTitle,
                money(item.weightCount), money(item.unitAmount), money(item.discount),
                money(item.taxAmount), money(item.totalAmount),
            ]),
            totals: [
                ["جمع مبلغ کالاها", preFactor?.sumTotalAmount], ["تخفیف کل", preFactor?.discount],
                ["مالیات کل", preFactor?.totalTax], ["مبلغ نهایی", preFactor?.paymentAmount],
            ],
        }),
    });
    
    exSoftwarePreFactorSetConfirmButton("تأیید و ثبت در نرم‌افزار", items.length === 0);
}


/* -------------------------------------------------------
   Submit
------------------------------------------------------- */

async function exSoftwarePreFactorSubmit() {
    const request = exSoftwarePreFactorBuildRequest();
    
    await sendApiRequest({
        url: "/Factor/SendPreFactorsToExternalSoftware",
        method: "POST",
        headers: ExternalFinancialWizard.antiforgeryHeaders(getExternalMappingModal()),
        successMsg: "انتقال پیش‌فاکتور با موفقیت انجام شد",
        data: request,
        onSuccess: exSoftwarePreFactorFinish,
    });
}


/* -------------------------------------------------------
   Button State
------------------------------------------------------- */

function exSoftwarePreFactorUpdateButtons() {
    exSoftwarePreFactorUpdateConfirmButton();
    exSoftwarePreFactorUpdateBackButton();
}
function exSoftwarePreFactorUpdateBackButton() {
    const state = externalSoftwareProcess;
    
    const canGoBack = [
        "customer",
        "items",
        "preview",
    ].includes(state.step);
    
    getPreFactorFinancialWizard().setActions({ backDisabled: !canGoBack || state.processing });
}


function exSoftwarePreFactorUpdateConfirmButton() {
    const state = externalSoftwareProcess;
    
    if (state.processing) {
        toggleElementsDisabled(getConfirmExternalMappingBtn(), true);
        return;
    }
    
    switch (state.step) {
        case "saleType": {
            const selected = getExternalMappingList()?.querySelector("#externalMappingSaleTypeSelect")?.value;
            toggleElementsDisabled(getConfirmExternalMappingBtn(), !selected);
            break;
        }
        
        case "customer": {
            const selected = getExternalMappingList()?.querySelector("#externalMappingCustomerSelect")?.value;
            toggleElementsDisabled(getConfirmExternalMappingBtn(), !selected);
            break;
        }
        
        case "items":
            toggleElementsDisabled(getConfirmExternalMappingBtn(), !exSoftwarePreFactorAreAllItemsMappedWithStock());
            break;
        
        case "preview":
            toggleElementsDisabled(getConfirmExternalMappingBtn(), false);
            break;
        
        default:
            toggleElementsDisabled(getConfirmExternalMappingBtn(), true);
    }
}


function exSoftwarePreFactorGoBack() {
    const state = externalSoftwareProcess;
    
    switch (state.step) {
        case "customer":
            state.step = "saleType";
            exSoftwarePreFactorRenderSaleType();
            break;
        
        case "items":
            state.step = "customer";
            exSoftwarePreFactorRenderCustomer();
            break;
        
        case "preview":
            state.step = "items";
            exSoftwarePreFactorRenderItems();
            break;
        
        default:
            return;
    }
    
    return true;
}

/* -------------------------------------------------------
   Main Step Handler
------------------------------------------------------- */

const externalSoftwareStepHandlers = {
    saleType: exSoftwarePreFactorConfirmSaleType,
    customer: exSoftwarePreFactorConfirmCustomer,
    items: exSoftwarePreFactorConfirmItems,
    preview: exSoftwarePreFactorSubmit,
};


document.addEventListener("click", async event => {
    if (!event.target.closest("#confirmExternalMappingBtn")) return;
        const state = externalSoftwareProcess;
        
        if (state.processing) return;
        
        const handler = externalSoftwareStepHandlers[state.step];
        
        if (!handler) {
            toastifyError("مرحله فعلی معتبر نیست.");
            return;
        }
        
        state.processing = true;
        
        toggleElementsDisabled(getConfirmExternalMappingBtn(), true);
        
        try {
            await handler();
        } catch (error) {
            console.error(error);
            toastifyError("در اجرای این مرحله خطایی رخ داد.");
        } finally {
            state.processing = false;
            
            if (isModalOpen(getExternalMappingModal())) {
                exSoftwarePreFactorUpdateButtons();
            }
        }
    },
);

document.addEventListener("click", event => {
    if (!event.target.closest("#backExternalMappingBtn")) return;
    const state = externalSoftwareProcess;
    if (state.processing) return;
    
    state.processing = true;
    
    toggleElementsDisabled(getBackExternalMappingBtn(), true);
    
    exSoftwarePreFactorGoBack();
    state.processing = false;
    
    if (isModalOpen(getExternalMappingModal())) {
        exSoftwarePreFactorUpdateButtons();
    }
});
