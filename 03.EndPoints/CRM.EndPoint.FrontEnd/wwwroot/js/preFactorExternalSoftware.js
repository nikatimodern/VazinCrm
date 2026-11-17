/** CRM quotation export to external financial software.
 *  Shares its UI shell with invoice and purchase exports without changing its API steps.
 */

/* ----------------------------------------------------------------------------------------------------------- */

async function sendPreFactorToExternalSoftware(id) {
    await showExternalSoftwareModal(id, {
        mapType: 2,
        onConfirm: async ({ entityIds, externalSoftwareIds }) => {
            ModalToggle("chooseExternalSoftwareModal", "hide");
            
            await exSoftwarePreFactorStart({
                preFactorId: entityIds?.[0],
                softwareIds: externalSoftwareIds,
            });
        },
    });
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
        step: "مرحله ۱ از ۵",
        
        content: `
            <div>
                <label for="externalMappingSaleTypeSelect" class="form-label mb-1">
                    نوع فروش
                </label>

                <select id="externalMappingSaleTypeSelect" class="show-dropdown-arrow">
                    ${(state.software.saleTypes ?? []).map(item => `
                        <option value="${escapeHtml(item.saleTypeId)}" ${String(item.saleTypeId) === String(state.mappings.saleTypeId ?? "2") ? "selected" : ""}>
                            ${escapeHtml(item.title)}
                        </option>
                    `).join("")}
                </select>
            </div>
        `,
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
        subtitle: "اتصال مشتری CRM به نرم‌افزار مالی",
        icon: `<i class="fas fa-users"></i>`,
        step: "مرحله ۲ از ۵",
        
        content: `
            <div class="external-mapping-entity">
                <span class="external-mapping-label">
                    مشتری در CRM
                </span>

                <span class="external-mapping-entity-name fbold">
                    ${escapeHtml(software.crmCustomerTitle ?? "-")}
                </span>
            </div>

            <div>
                <label for="externalMappingCustomerSelect" class="form-label mb-0">
                    مشتری معادل در نرم‌افزار مالی
                </label>

                <select id="externalMappingCustomerSelect" class="show-dropdown-arrow">
                    <option value="">جستجو و انتخاب مشتری</option>

                    ${(software.customers ?? []).map(customer => `
                        <option value="${escapeHtml(customer.id)}" ${String(customer.id) === String(mappedCustomerId ?? "") ? "selected" : ""}>
                            ${customer.dlCode ? `${escapeHtml(customer.dlCode)} - ` : ""}${escapeHtml(customer.fullName)}
                        </option>
                    `).join("")}
                </select>
            </div>
        `,
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
        successMsg: null,
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
        return;
    }
    
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalItemMapping",
        method: "POST",
        successMsg: null,
        data: {
            preFactorId: state.preFactorId,
            externalSoftwareId: state.software.externalSoftwareId,
        },
    });
    
    if (!result?.preFactor.items?.length) {
        toastifyError("ردیفی برای تطبیق کالا پیدا نشد.");
        return;
    }
    
    
    state.preFactor = result.preFactor;
    
    state.software = {
        ...state.software,
        sepidarItems: result.sepidarItems,
    };
    
    if (!reload) state.mappings.items = {};
    state.step = "items";
    
    exSoftwarePreFactorRenderItems();
}


function exSoftwarePreFactorRenderItems() {
    const state = externalSoftwareProcess;
    const software = state.software;
    const preFactor = state.preFactor;
    
    
    const { content, add, list } = createExternalItemMatching();
    add.addEventListener("click", () => openSepidarItemCreation({
        softwareId: software.externalSoftwareId,
        softwareTitle: software.externalSoftwareTitle,
        afterCreate: () => exSoftwarePreFactorLoadItems(true),
        canOpen: () => externalSoftwareProcess === state && state.software === software
            && isModalOpen(getExternalMappingModal()),
    }));

    for (const item of preFactor.items ?? []) {
        const target = appendExternalItemMatchRow(list, item.productTitle, item.weightCount, item.unitTxt);
        const select = document.createElement("select");
        select.id = "externalMappingItem_" + item.preFactorItemId;
        select.dataset.crmItemId = item.preFactorItemId;
        select.className = "external-mapping-item-select show-dropdown-arrow";

        const label = document.createElement("label");
        label.htmlFor = select.id;
        label.className = "form-label mb-0";
        label.textContent = "کالای معادل در نرم‌افزار مالی";

        const selectedId = String(state.mappings.items[String(item.preFactorItemId)]?.externalItemId ?? "");
        select.append(new Option("", ""));
        for (const product of software.sepidarItems ?? []) {
            select.append(new Option(product.code + " - " + product.title, product.id, false, String(product.id) === selectedId));
        }
        target.append(label, select);
    }

    exSoftwarePreFactorRenderStep({
        title: "تطبیق کالاها",
        subtitle: "انتخاب کالای معادل برای ردیف‌های پیش‌فاکتور",
        icon: '<i class="fas fa-boxes" aria-hidden="true"></i>',
        step: "مرحله ۳ از ۵", large: true, content,
    });
    
    getExternalMappingList().querySelectorAll(".external-mapping-item-select").forEach(select => {
        new TomSelect(select, {
            placeholder: "جستجو و انتخاب کالا",
            
            onChange: value => {
                const itemId = String(select.dataset.crmItemId);
                
                const mapping = state.mappings.items[itemId] ?? {};
                
                if (value) {
                    mapping.externalItemId = value;
                    state.mappings.items[itemId] = mapping;
                } else if (mapping.externalStockId) {
                    delete mapping.externalItemId;
                } else {
                    delete state.mappings.items[itemId];
                }
                
                exSoftwarePreFactorUpdateConfirmButton();
            },
        });
    });
    
    exSoftwarePreFactorSetConfirmButton("تأیید کالاها و ادامه", !exSoftwarePreFactorAreAllItemsMapped());
}


function exSoftwarePreFactorAreAllItemsMapped() {
    const state = externalSoftwareProcess;
    const items = state.preFactor?.items ?? [];
    
    return items.length > 0 && items.every(item => state.mappings.items[String(item.preFactorItemId)]?.externalItemId);
}


async function exSoftwarePreFactorConfirmItems() {
    if (!exSoftwarePreFactorAreAllItemsMapped()) {
        toastifyError("برای تمام ردیف‌های پیش‌فاکتور کالای معادل انتخاب کنید.");
        return;
    }
    
    await exSoftwarePreFactorLoadStock();
}


/* -------------------------------------------------------
   Stock
------------------------------------------------------- */

async function exSoftwarePreFactorLoadStock() {
    const state = externalSoftwareProcess;
    
    if (state.software?.stocksByItem?.length) {
        state.step = "stock";
        exSoftwarePreFactorRenderStock();
        return;
    }
    
    
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalStockMapping",
        method: "POST",
        successMsg: null,
        data: {
            itemIds: Object.values(state.mappings.items).map(item => item.externalItemId),
            preFactorId: state.preFactorId,
            externalSoftwareId: state.software.externalSoftwareId,
        },
    });
    
    if (!result) return;
    
    state.software = {
        ...state.software,
        ...result,
    };
    
    state.step = "stock";
    
    exSoftwarePreFactorRenderStock();
}


function exSoftwarePreFactorRenderStock() {
    const state = externalSoftwareProcess;
    const software = state.software;
    const preFactor = state.preFactor;
    
    exSoftwarePreFactorRenderStep({
        title: "انتخاب انبار کالاها",
        subtitle: "برای هر ردیف پیش‌فاکتور انبار مقصد را مشخص کنید",
        icon: `<i class="fas fa-warehouse"></i>`,
        step: "مرحله ۴ از ۵",
        
        content: `
            <div class="external-item-mapping-list">
                ${(preFactor.items ?? []).map(item => {
            
            const mapping = state.mappings.items[String(item.preFactorItemId)];
            const externalProduct = (software.sepidarItems ?? []).find(product => String(product.id) === String(mapping?.externalItemId));
            
            const itemStocks = (software.stocksByItem ?? []).find(x => String(x.itemRef) === String(mapping?.externalItemId))?.stocks ?? [];
            
            return `
                    <div class="external-mapping-entity external-item-mapping-row">
                        <div class="d-flex gap-2 align-items-baseline">
                            <span class="external-mapping-label">
                                محصول پیش‌فاکتور CRM:
                            </span>
    
                            <span class="external-mapping-entity-name">
                                ${escapeHtml(item.productTitle)}
                            </span>
                        </div>
    
                        <div class="d-flex gap-2 align-items-baseline">
                            <span class="external-mapping-label">
                                محصول معادل سپیدار:
                            </span>
    
                            <span class="external-mapping-entity-name">
                                ${escapeHtml(externalProduct?.title ?? "-")}
                            </span>
                        </div>
    
                        <label class="form-label mt-2 mb-0" for="externalMappingStock_${item.preFactorItemId}">
                            انبار
                        </label>
    
                        <select class="external-mapping-stock-select show-dropdown-arrow" id="externalMappingStock_${item.preFactorItemId}" data-crm-item-id="${escapeHtml(item.preFactorItemId)}">
                            <option value="">جستجو و انتخاب انبار</option>
                            ${itemStocks.map(stock => `
                                <option value="${escapeHtml(stock.id)}" ${String(stock.id) === String(mapping?.externalStockId ?? "") ? "selected" : ""}>
                                    ${escapeHtml(stock.code)} - ${escapeHtml(stock.title)}
                                </option>
                            `).join("")}
                        </select>
                    </div>`;
        }).join("")}
            </div>
        `,
    });
    
    getExternalMappingList().querySelectorAll(".external-mapping-stock-select").forEach(select => {
        new TomSelect(select, {
            placeholder: "جستجو و انتخاب انبار",
            
            onChange: value => {
                const itemId = String(select.dataset.crmItemId);
                const mapping = state.mappings.items[itemId];
                
                if (!mapping) return;
                
                if (value) {
                    mapping.externalStockId = value;
                } else {
                    delete mapping.externalStockId;
                }
                
                exSoftwarePreFactorUpdateConfirmButton();
            },
        });
    });
    
    exSoftwarePreFactorSetConfirmButton("تأیید انبارها و ادامه", !exSoftwarePreFactorAreAllItemsMappedWithStock());
}


function exSoftwarePreFactorAreAllItemsMappedWithStock() {
    const items = externalSoftwareProcess.preFactor?.items ?? [];
    
    return items.length > 0 && items.every(item => {
        const mapping = externalSoftwareProcess.mappings.items[String(item.preFactorItemId)];
        return mapping?.externalItemId && mapping?.externalStockId;
    });
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
        step: "مرحله 5 از 5",
        large: true,
        
        content: renderExternalFinancialPreview({
            fields: [
                { label: "نوع فروش", value: saleTypeTitle },
                { label: "شماره پیش‌فاکتور", value: preFactor?.serialNumber },
                { label: "تاریخ", value: preFactor?.date },
                { label: "مشتری", value: customerTitle, wide: true },
                { label: "نرم‌افزار مقصد", value: software.externalSoftwareTitle, accent: true },
            ],
            columns: ["ردیف", "محصول CRM", "کالای مقصد", "انبار", "تعداد", "قیمت واحد", "تخفیف", "مالیات", "مبلغ نهایی"],
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
        "stock",
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
            toggleElementsDisabled(getConfirmExternalMappingBtn(), !exSoftwarePreFactorAreAllItemsMapped());
            break;
        
        case "stock":
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
        
        case "stock":
            state.step = "items";
            exSoftwarePreFactorRenderItems();
            break;
        
        case "preview":
            state.step = "stock";
            exSoftwarePreFactorRenderStock();
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
    stock: exSoftwarePreFactorPreview,
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
