var customerId = null;

const prepareResponsiveForms = () => {
    if (window.innerWidth >= 992) return;
    document.getElementById("PreFacItemContainer")?.remove();
    document.getElementById("FacItemContainer")?.remove();
    document.getElementById("supplyFactorContainer")?.remove();
    document.getElementById("orderFormContainer")?.remove();
    const descriptionOrder = document.getElementById("descriptionOrder");
    if (descriptionOrder) descriptionOrder.rows = 3;
};
prepareResponsiveForms();


const Unit = (type, formId, defaultValueId) => {
    
    let unitItems = [
        { id: 1, text: "گرم" },
        { id: 2, text: "کیلوگرم" },
        { id: 3, text: "عدد" },
        { id: 4, text: "متر مربع" },
        { id: 5, text: "متر طول" },
        { id: 6, text: "شاخه" },
        { id: 7, text: "رول" },
    ];
    
    if (type === 1) {
        const unitElm = document.querySelector(`${window.innerWidth < 992 ? "#ItemPreFactorModal" : "#PreFactor"} #PreFacItem select[name='unit']`);
        if (!unitElm) return;
        $(unitElm).select2({
            data: unitItems,
            dir: "rtl",
            placeholder: "انتخاب",
            minimumResultsForSearch: Infinity,
            selectionCssClass: "form-control",
            dropdownParent: window.innerWidth < 992 ? $("#ItemPreFactorModal") : $("#PreFactor"),
        });
        $(unitElm).val(null).trigger("change");
    } else if (type === 2) {
        const unitElm = document.querySelector(`${window.innerWidth < 992 ? "#ItemFactorModal" : "#Factor"} #FacItem select[name='unit']`);
        if (!unitElm) return;
        $(unitElm).select2({
            data: unitItems,
            dir: "rtl",
            placeholder: "انتخاب",
            selectionCssClass: "form-control",
            dropdownParent: window.innerWidth < 992 ? $("#ItemFactorModal") : $("#Factor"),
        });
        $(unitElm).val(null).trigger("change");
    } else if (type === 3) {
        const unitInput = document.querySelector(`#${formId} select[name='unit']`);
        if (!unitInput) return;
        unitInput.innerHTML = "";
        
        const fragment = document.createDocumentFragment();
        if (!defaultValueId) {
            const opt = document.createElement("option");
            opt.innerText = "انتخاب";
            opt.value = "";
            fragment.appendChild(opt);
        }
        unitItems.forEach(unit => {
            const opt = document.createElement("option");
            opt.innerText = unit.text;
            opt.value = unit.id;
            if (defaultValueId && unit.id === defaultValueId) {
                opt.setAttribute("selected", true);
            }
            fragment.appendChild(opt);
        });
        unitInput.appendChild(fragment);
    } else if (type === 4) {
        const unitElm = document.querySelector(`${window.innerWidth < 992 ? "#ItemSupplyFactorModal" : "#supplyFactorModal"} #supplyFactorItem select[name='unit']`);
        if (!unitElm) return;
        $(unitElm).select2({
            data: unitItems,
            dir: "rtl",
            placeholder: "انتخاب",
            minimumResultsForSearch: Infinity,
            selectionCssClass: "form-control",
            dropdownParent: window.innerWidth < 992 ? $("#ItemSupplyFactorModal") : $("#supplyFactorModal"),
        });
        $(unitElm).val(null).trigger("change");
    }
};


let allProductPromise = null;
let productAccessGeneration = 0;
let productAccessInvalidated = false;
const productSelectors = new Set();

function invalidateProductSelectors() {
    productAccessInvalidated = true;
    productAccessGeneration++;
    allProductPromise = null;
    for (const select of productSelectors) {
        if (!select.isConnected) continue;
        // Retain the selected value as a draft but remove other catalog choices.
        select.tomselect?.clearOptions();
        select.tomselect?.disable();
        select.disabled = true;
    }
    productSelectors.clear();
}
window.addEventListener("erp:access-changed", invalidateProductSelectors);
window.addEventListener("erp:access-reconnected", invalidateProductSelectors);
window.addEventListener("erp:access-state", invalidateProductSelectors);

const getAllProductOp = async (form) => {
    const formElement = resolveElement(form);
    if (!formElement) return;
    const productCodeElement = formElement.querySelector("select[name='productCode']");
    if (!productCodeElement) return;
    if (productAccessInvalidated || window.AccessLive?.can("Product", "View") === false) {
        productCodeElement.tomselect?.disable();
        productCodeElement.disabled = true;
        return;
    }
    const generation = productAccessGeneration;
    if (!allProductPromise) {
        allProductPromise = sendApiRequest({ url: "/Product/GetAllProduct" }).then(products => {
            if (!products && generation === productAccessGeneration) allProductPromise = null;
            return products;
        });
    }
    const products = await allProductPromise;
    if (!products || !productCodeElement.isConnected || generation !== productAccessGeneration
        || productAccessInvalidated || window.AccessLive?.can("Product", "View") === false) return;
    for (const select of productSelectors) if (!select.isConnected) productSelectors.delete(select);
    productSelectors.add(productCodeElement);
    if (productCodeElement.tomselect) return productCodeElement.tomselect;
    return new TomSelect(productCodeElement, {
        options: products,
        valueField: "id",
        placeholder: "انتخاب",
        maxItems: 1,
        selectOnTab: true
    });
};

/* ----------------------------------------------------------------------------- */

const getBaseDescription = async (type = null) => {  // 0: preFactor | 1: Factor | 2: termsPrefactor
    return await sendApiRequest({
        url: "/Factor/GetItemsDescription",
        params: { type },
    });
};

function calculateItem(
    formId,
    weightSelector = "[name='weightCount']",
    unitSelector = "[name='unitAmount']",
    totalSelector = "[name='totalAmount']",
    discountSelector = "[name='discount']",
) {
    const form = document.getElementById(formId);
    if (!form) return;
    
    const getValue = (selector) => {
        const el = form.querySelector(selector);
        if (!el) return 0;
        
        return el.tagName === "INPUT" || el.tagName === "TEXTAREA"
            ? SeparatedNumberRemove(el.value)
            : SeparatedNumberRemove(el.innerText);
    };
    
    const setValue = (selector, value) => {
        const el = form.querySelector(selector);
        if (!el) return;
        
        if (el.tagName === "INPUT" || el.tagName === "TEXTAREA") {
            el.value = value;
        } else {
            el.innerText = value;
        }
    };
    
    const unitAmount = Number(getValue(unitSelector)) || 0;
    const weightCount = Number(getValue(weightSelector)) || 0;
    let discount = Number(getValue(discountSelector)) || 0;
    
    /*
     * مبلغ قبل از تخفیف و مالیات
     */
    const amountBeforeDiscount = Math.round(unitAmount * weightCount);
    
    /*
     * تخفیف
     */
    discount = Math.min(Math.max(0, Math.round(discount)), amountBeforeDiscount);
    
    /*
     * مبلغ بعد از تخفیف
     */
    const amountAfterDiscount = Math.max(0, amountBeforeDiscount - discount);
    
    /*
     * مالیات
     */
    let taxAmount = 0;
    
    if (pFactor.officialType === 1) {
        const taxPercent = setting?.taxPercent || 10;
        const taxAfterDiscount = setting?.taxAfterDiscount ?? true;
        
        const taxBase = taxAfterDiscount ? amountAfterDiscount : amountBeforeDiscount;
        
        taxAmount = calculateItemTax(taxBase, taxPercent);
    }
    
    /*
     * مبلغ نهایی:
     * مبلغ قبل تخفیف - تخفیف + مالیات
     */
    const total = Math.max(0, amountAfterDiscount + taxAmount);
    
    setValue(discountSelector, discount.toLocaleString());
    
    setValue(totalSelector, total.toLocaleString());
}


const distributeItemDiscount = (items, totalDiscount) => {
    const discount = Math.max(0, Math.round(totalDiscount));
    
    const totalBeforeDiscount = items.reduce((sum, item) => {
        return sum + Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
    }, 0);
    
    if (!totalBeforeDiscount || !discount) {
        items.forEach(item => {
            item.discount = 0;
        });
        
        return;
    }
    
    const finalDiscount = Math.min(discount, totalBeforeDiscount);
    
    let distributedDiscount = 0;
    
    items.forEach((item, index) => {
        const beforeDiscount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
        
        if (index === items.length - 1) {
            item.discount = Math.min(Math.max(0, finalDiscount - distributedDiscount), beforeDiscount);
        } else {
            item.discount = Math.min(Math.round(finalDiscount * beforeDiscount / totalBeforeDiscount), beforeDiscount);
            
            distributedDiscount += item.discount;
        }
    });
};


function createExternalItemMatching() {
    const content = document.createElement("div");
    content.className = "external-item-matching";

    const toolbar = document.createElement("div");
    toolbar.className = "external-item-match-toolbar";
    const add = document.createElement("button");
    add.type = "button";
    add.className = "external-item-match-add";
    add.title = "باز کردن فرم ثبت کالای جدید در نرم‌افزار مقصد";
    const addIcon = document.createElement("i");
    addIcon.className = "fas fa-plus";
    addIcon.setAttribute("aria-hidden", "true");
    const addLabel = document.createElement("span");
    addLabel.textContent = "افزودن کالای جدید";
    add.append(addIcon, addLabel);

    const refresh = document.createElement("button");
    refresh.type = "button";
    refresh.className = "external-item-match-refresh";
    refresh.title = "دریافت دوباره فهرست کالاها از نرم‌افزار مقصد";
    const refreshIcon = document.createElement("i");
    refreshIcon.className = "fas fa-sync-alt";
    refreshIcon.setAttribute("aria-hidden", "true");
    const refreshLabel = document.createElement("span");
    refreshLabel.textContent = "بروزرسانی کالاها";
    refresh.append(refreshIcon, refreshLabel);
    toolbar.append(refresh, add);

    const list = document.createElement("div");
    list.className = "external-item-match-list";
    content.append(toolbar, list);
    return { content, toolbar, add, refresh, list };
}

async function refreshExternalItemMatching(button, reload) {
    if (button.disabled) return;
    button.disabled = true;
    const icon = button.querySelector("i");
    icon?.classList.add("fa-spin");
    try {
        if (await reload()) toastifySuccess("فهرست کالاهای نرم‌افزار مقصد بروزرسانی شد.");
    } finally {
        icon?.classList.remove("fa-spin");
        button.disabled = false;
    }
}

function appendExternalItemMatchRow(list, title, quantity, unit, quantityLabel) {
    const row = document.createElement("div");
    row.className = "external-item-match-row";
    const source = document.createElement("div");
    source.className = "external-item-match-source";
    const name = document.createElement("strong");
    name.textContent = title ?? "—";
    source.append(name);
    if (quantity != null) {
        const amount = document.createElement("small");
        amount.textContent = [quantityLabel, Number(quantity).toLocaleString("fa-IR"), unit].filter(Boolean).join(" ");
        source.append(amount);
    }
    const target = document.createElement("div");
    target.className = "external-item-match-target";
    row.append(source, target);
    list.append(row);
    return target;
}

function appendExternalItemEditRow(list, row) {
    const target = appendExternalItemMatchRow(list, row.itemTitle);
    const caption = document.createElement("small");
    caption.textContent = `ردیف ${row.row} · مقدار ${row.quantity} · قیمت واحد ${Number(row.fee).toLocaleString()} ریال`;
    target.previousElementSibling.append(caption);
    return target;
}

async function openSepidarItemCreation({ softwareId, softwareTitle, afterCreate, canOpen = () => true }) {
    const modal = document.getElementById("addItemInSepidarModal");
    if (!modal || !softwareId || modal.dataset.active === "true") return;
    modal.dataset.active = "true";

    const form = modal.querySelector("form");
    const unit = form.elements.namedItem("itemUnit");
    const stockInput = modal.querySelector("#sepidarStockId");
    const defaultStock = form.elements.namedItem("defaultStockRef");
    const defaultStockField = modal.querySelector("[data-sepidar-default-stock]");
    const submit = form.querySelector('button[type="submit"]');
    const data = { externalSoftwareIds: [softwareId] };
    const lifecycle = new AbortController();
    const { signal } = lifecycle;
    let stockIds = [], saving = false;

    const cleanup = () => {
        lifecycle.abort();
        unit.tomselect?.destroy();
        unit.replaceChildren(new Option("انتخاب کنید", ""));
        defaultStock.replaceChildren(new Option("بدون انبار پیش‌فرض", ""));
        defaultStockField.hidden = true;
        form.reset();
        delete modal.dataset.active;
    };
    const read = action => sendApiRequest({
        url: "/Factor/" + action, method: "POST", data,
        successMsg: null, showLoading: false,
    });
    let units, lastCode;
    try {
        [units, lastCode] = await Promise.all([read("PrepareExternalUnits"), read("GetLastSepidarItemCode")]);
        if (!units || !canOpen()) {
            cleanup();
            return;
        }

        modal.querySelector("#addSepidarItemTitle").textContent = softwareTitle
            ? "افزودن کالای جدید در " + softwareTitle : "افزودن کالای جدید";
        modal.querySelector("#lastSepidarItemCode").textContent = lastCode ? "کد آخرین کالا: " + lastCode : "";
        new TomSelect(unit, {
            options: units, placeholder: "جستجو و انتخاب واحد", maxItems: 1, selectOnTab: true,
        });
    } catch (error) {
        cleanup();
        throw error;
    }

    modal.querySelector("[data-sepidar-item-stocks]").addEventListener("click", () => modalSelector.open({
        title: "انتخاب انبار", selectedIds: stockIds, size: "sm", showSearch: false,
        load: () => read("GetAllSepidarStocksPartial"),
        onSubmit: ({ ids, numbers, map }) => {
            const previousDefault = defaultStock.value;
            const stockTitles = new Map([...modalSelector.container.querySelectorAll("[data-stock-title]")]
                .map(input => [input.value, input.dataset.stockTitle]));
            stockIds = ids;
            stockInput.value = numbers.join(", ");
            defaultStock.replaceChildren(new Option("بدون انبار پیش‌فرض", ""));
            for (const id of ids) {
                const code = map.get(id) ?? id;
                const title = stockTitles.get(id);
                defaultStock.append(new Option(title ? code + " - " + title : code, id));
            }
            defaultStock.value = ids.includes(previousDefault) ? previousDefault : "";
            defaultStockField.hidden = ids.length === 0;
        },
    }), { signal });

    modal.addEventListener("hide.bs.modal", event => {
        if (saving) event.preventDefault();
    }, { signal });
    modal.addEventListener("hidden.bs.modal", () => {
        cleanup();
        modalSelector.reset();
    }, { once: true, signal });

    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (saving) return;
        saving = true;
        submit.disabled = true;
        let saved;
        try {
            if (!await mySer.CheckValidForm(form.id)) return;
            if (!stockIds.length) return toastifyError("حداقل یک انبار باید انتخاب شود.");

            const model = mySer.convertFormToJSON(form.id);
            const token = form.elements.namedItem("__RequestVerificationToken").value;
            delete model.__RequestVerificationToken;
            saved = await sendApiRequest({
                url: "/Factor/SendItemToExternalSoftware", method: "POST", returnFullResponse: true,
                headers: { RequestVerificationToken: token },
                data: { ...model, stockIds: stockIds.map(Number), externalSoftwareId: softwareId,
                    defaultStockRef: defaultStock.value ? Number(defaultStock.value) : null },
                successMsg: "ثبت کالا با موفقیت انجام شد",
            });
        } finally {
            saving = false;
            submit.disabled = false;
        }
        if (saved) {
            modal.addEventListener("hidden.bs.modal", () => {
                if (canOpen()) afterCreate();
            }, { once: true });
            ModalToggle(modal, "hide");
        }
    }, { signal });

    ModalToggle(modal, "show");
}

function renderExternalFinancialStage(modal, container, { destination, position, title, subtitle, icon, step, content, large = false }) {
    modal.querySelector(".external-mapping-title").textContent = destination ?? "نرم‌افزار مقصد";
    modal.querySelector(".external-mapping-subtitle").textContent = position;
    modal.querySelector(".modal-dialog").classList.toggle("modal-lg", large);
    container.innerHTML = `
        <div class="external-mapping-card required-mark">
            <div class="external-mapping-stage-info">
                <div class="external-mapping-stage-icon">${icon}</div>
                <div class="external-mapping-stage-content">
                    <div class="external-mapping-stage-title">${escapeHtml(title)}</div>
                    <div class="external-mapping-stage-subtitle">${escapeHtml(subtitle)}</div>
                </div>
                <span class="external-mapping-step">${escapeHtml(step)}</span>
            </div>
            ${typeof content === "string" ? content : ""}
        </div>`;
    if (content instanceof Node) container.firstElementChild.append(content);
}

function renderExternalFinancialPreviewTable(columns, rows) {
    return `<div class="table-responsive">
            <table class="table table-sm align-middle">
                <thead><tr>${columns.map(label => `<th scope="col">${escapeHtml(label)}</th>`).join("")}</tr></thead>
                <tbody>${rows.map(row => `<tr>${row.map((value, index) =>
                    `<td${index === row.length - 1 ? ' class="fbold"' : ""}>${escapeHtml(value ?? "—")}</td>`).join("")}</tr>`).join("")}</tbody>
            </table>
        </div>`;
}

function renderExternalFinancialPreview({ fields, columns, rows, totals, detailsHtml }) {
    return `<div class="external-prefactor-preview">
        <div class="ex-preview-summary">${fields.map(({ label, value, wide, accent }) => `
            <div class="ex-preview-field${wide ? " ex-preview-customer" : ""}">
                <span class="ex-preview-label">${escapeHtml(label)}:</span>
                <strong${accent ? ' class="ex-preview-software"' : ""}>${escapeHtml(value ?? "—")}</strong>
            </div>`).join("")}</div>
        ${detailsHtml ?? renderExternalFinancialPreviewTable(columns, rows)}
        <div class="external-mapping-entity mt-3">${totals.map(([label, value], index) => {
            const last = index === totals.length - 1;
            return `${last && totals.length > 1 ? "<hr>" : ""}<div class="d-flex justify-content-between${last ? "" : " mb-1"}">
                ${last ? `<strong>${escapeHtml(label)}</strong>` : `<span>${escapeHtml(label)}</span>`}
                <strong>${renderMoneyHtml(value)}</strong>
            </div>`;
        }).join("")}</div>
    </div>`;
}

const calculateInvoiceTotals = (
    {
        items,
        invoice,
        discountInputId,
        sumTotalAmountId,
        totalTaxId,
        paymentAmountId,
        updateItemsFromDiscount = false,
    }) => {
    const activeItems = items.filter(item => !item.isDeleted);
    
    const taxPercent = setting?.taxPercent || 10;
    const taxAfterDiscount = setting?.taxAfterDiscount ?? true;
    
    const discountInput = document.getElementById(discountInputId);
    
    let sumTotalAmount = 0;
    let totalDiscount = 0;
    let totalTax = 0;
    let paymentAmount = 0;
    
    /*
     * مبلغ قبل از تخفیف و مالیات
     */
    activeItems.forEach(item => {
        const beforeDiscount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
        
        sumTotalAmount += beforeDiscount;
    });
    
    sumTotalAmount = Math.round(sumTotalAmount);
    
    /*
     * اگر تخفیف کلی تغییر کرده،
     * تخفیف را بین آیتم‌ها تقسیم کن.
     */
    if (updateItemsFromDiscount && discountInput) {
        const requestedDiscount = Math.max(0, Math.round(Number(SeparatedNumberRemove(discountInput.value)) || 0));
        
        distributeItemDiscount(activeItems, requestedDiscount);
    }
    
    /*
     * محاسبه نهایی هر آیتم
     */
    activeItems.forEach(item => {
        const beforeDiscount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
        
        /*
         * تخفیف آیتم
         */
        const discount = Math.min(Math.max(0, Math.round(Number(item.discount) || 0)), beforeDiscount);
        
        item.discount = discount;
        
        /*
         * مبلغ بعد از تخفیف
         */
        const amountAfterDiscount = Math.max(0, beforeDiscount - discount);
        
        /*
         * مالیات آیتم
         */
        if (invoice.officialType === 1) {
            const taxBase = taxAfterDiscount
                ? amountAfterDiscount
                : beforeDiscount;
            
            item.taxAmount = calculateItemTax(taxBase, taxPercent);
        } else {
            item.taxAmount = 0;
        }
        
        /*
         * مبلغ نهایی آیتم:
         * بعد از تخفیف + مالیات
         */
        item.totalAmount = Math.max(0, amountAfterDiscount + item.taxAmount);
        
        totalDiscount += item.discount;
        totalTax += item.taxAmount;
        paymentAmount += item.totalAmount;
    });
    
    totalDiscount = Math.round(totalDiscount);
    totalTax = Math.round(totalTax);
    paymentAmount = Math.round(paymentAmount);
    
    /*
     * درصد مالیات
     */
    invoice.percentTax = invoice.officialType === 1 ? taxPercent : 0;
    
    /*
     * اطلاعات نهایی فاکتور
     */
    invoice.sumTotalAmount = sumTotalAmount;
    invoice.discount = totalDiscount;
    invoice.totalTax = totalTax;
    invoice.paymentAmount = paymentAmount;
    
    /*
     * UI
     */
    const sumTotalAmountElement = document.getElementById(sumTotalAmountId);
    const totalTaxElement = document.getElementById(totalTaxId);
    const paymentAmountElement = document.getElementById(paymentAmountId);
    
    if (discountInput) {
        discountInput.value = totalDiscount.toLocaleString();
    }
    if (sumTotalAmountElement) {
        sumTotalAmountElement.textContent = sumTotalAmount.toLocaleString();
    }
    
    if (totalTaxElement) {
        totalTaxElement.textContent = totalTax.toLocaleString();
    }
    
    if (paymentAmountElement) {
        paymentAmountElement.textContent = paymentAmount.toLocaleString();
    }
    
    return invoice;
};


const calculateItemTax = (amount, taxPercent) => {
    return Math.floor(amount * taxPercent / 100); // تقریب رو به پایین
};

const generateInvoiceItemDom = (data, { type = "edit-delete", onDelete, onUpdate, onSelect, fieldRules = {} } = {}) => {
    const container = document.createElement("div");
    container.className = "pf-items-list";
    
    data.forEach((item, index) => {
        if (item.isDeleted) return;
        
        item.product ??= "";
        item.discount ??= 0;
        item.row = index + 1;
        
        const incompleteFields = getIncompleteFields(item, fieldRules);
        const isIncomplete = incompleteFields.length > 0;
        
        const weight = Number(SeparatedNumberRemove(item.weightCount ?? item.weight)) || 0;
        const beforeDiscountAmount = calculateTotalPrice(weight, item.unitAmount);
        
        const beforeDiscountAmountStr = beforeDiscountAmount ? beforeDiscountAmount.toLocaleString() : "-";
        const uAmount = item.unitAmount ? item.unitAmount.toLocaleString() : "-";
        const discountAmount = item.discount ? item.discount.toLocaleString() : "0";
        const taxAmount = item.taxAmount ? item.taxAmount.toLocaleString() : "-";
        const tAmount = item.totalAmount ? item.totalAmount.toLocaleString() : "-";
        
        const element = document.createElement("div");
        // element.className = "pf-item";
        element.className = `pf-item ${isIncomplete ? "pf-item-incomplete" : ""}`;
        
        let actionButtons = "";
        
        if (type === "edit-delete") {
            actionButtons = `
                <button type="button" class="pf-item-action pf-item-delete">
                    <i class="fa fa-trash"></i>
                </button>

                <button type="button" class="pf-item-action pf-item-update">
                    <i class="fa fa-pencil"></i>
                </button>
            `;
        } else if (type === "select") {
            actionButtons = `
                <button type="button" class="buttonPurple2 pf-item-select">
                    افزودن
                </button>
            `;
        }
        
        element.innerHTML = `
            <div class="pf-item-row">
                <div class="InfoDataItemHead">
                    ${index + 1}
                </div>

                <div class="InfoDataItem">
                    <div class="pf-item-printable">
                        ${Number(item.printable) === 1 ? "چـاپـدار" : ""}
                    </div>

                    <div class="pf-item-content">

                        <div class="pf-item-field pf-item-product">
                            <label>شرح کالا</label>
                            <label>
                                <span class="pf-item-productSmallName">${item.productSmallName ?? ""}</span>
                                <span>${item.product ?? ""}</span>
                                ${isIncomplete ? `
                                    <p class="pf-item-incomplete-field">
                                      <i class="fa fa-exclamation-circle"></i>
                                      نیاز به ویرایش
                                    </p>` : ""}
                            </label>
                        </div>

                        <div class="pf-item-field pf-item-quantity">
                            <label>تعداد/مقدار</label>
                            <label>${weight.toLocaleString()}</label>
                        </div>

                        <div class="pf-item-field pf-item-unit">
                            <label>واحد</label>
                            <label>${item.unitTxt ?? ""}</label>
                        </div>

                        <div class="pf-item-field pf-item-unit-amount">
                            <label>مبلغ واحد</label>
                            <label>
                                ${uAmount}
                                <span class="fs-small">${item.currencyTypeTxt ?? ""}</span>
                            </label>
                        </div>

                        <div class="pf-item-field pf-item-beforeDiscount">
                            <label><span class="d-none d-sm-inline">مبلغ </span>قبل تخفیف</label>
                            <label>
                                ${beforeDiscountAmountStr}
                                <span class="fs-small">${item.currencyTypeTxt ?? ""}</span>
                            </label>
                        </div>

                        <div class="pf-item-field pf-item-discount">
                            <label>تخفیف</label>
                            <label>
                                ${discountAmount}
                                <span class="fs-small">${item.currencyTypeTxt ?? ""}</span>
                            </label>
                        </div>
                        <div class="pf-item-field pf-item-tax">
                            <label>مالیات</label>
                            <label>
                                ${taxAmount}
                                <span class="fs-small">${item.currencyTypeTxt ?? ""}</span>
                            </label>
                        </div>

                        <div class="pf-item-field pf-item-total">
                            <label>مبلغ نهایی</label>
                            <label>
                                ${tAmount}
                                <span class="fs-small">${item.currencyTypeTxt ?? ""}</span>
                            </label>
                        </div>

                    </div>

                    <div class="pf-item-actions">
                        ${actionButtons}
                    </div>
                </div>
            </div>
        `;
        
        if (type === "edit-delete") {
            element.querySelector(".pf-item-delete")?.addEventListener("click", async () => {
                await onDelete?.(index, item.id);
            });
            
            element.querySelector(".pf-item-update")?.addEventListener("click", async () => {
                await onUpdate?.(index, item.id);
            });
        } else if (type === "select") {
            element.querySelector(".pf-item-select")?.addEventListener("click", async () => {
                await onSelect?.(index, item.id);
            });
        }
        
        container.appendChild(element);
        
    });
    
    return container;
};


const validatePfItemDiscount = (formId) => {
    const form = document.getElementById(formId);
    
    const weightCount = Number(form.elements.namedItem("weightCount").value) || 0;
    const unitAmount = Number(SeparatedNumberRemove(form.elements.namedItem("unitAmount").value)) || 0;
    const discount = Number(SeparatedNumberRemove(form.elements.namedItem("discount").value)) || 0;
    
    const amountBeforeDiscount = Math.round(weightCount * unitAmount);
    
    if (discount > amountBeforeDiscount) {
        const discountInput = form.elements.namedItem("discount");
        
        discountInput.classList.add("AlertReq");
        discountInput.focus();
        
        return false;
    }
    
    form.elements.namedItem("discount").classList.remove("AlertReq");
    
    return true;
};


function getIncompleteItemsCount(items, fieldRules) {
    return items.filter(item => {
        if (item.isDeleted) return false;
        return getIncompleteFields(item, fieldRules).length > 0;
    }).length;
}

function getIncompleteFields(item, fieldRules = {}) {
    return Object.entries(fieldRules).filter(([fieldName, rules]) => {
        const value = item[fieldName];
        
        if (rules.required && (value === null || value === undefined || String(value).trim() === "")) {
            return true;
        }
        
        if (rules.numeric && (value === null || value === undefined || isNaN(Number(value)))) {
            return true;
        }
        
        if (rules.notZero && Number(value) === 0) {
            return true;
        }
        
        if (rules.min !== undefined && Number(value) < rules.min) {
            return true;
        }
        
        if (rules.max !== undefined && Number(value) > rules.max) {
            return true;
        }
        
        if (rules.validate && !rules.validate(value, item)) {
            return true;
        }
        
        return false;
    }).map(([fieldName]) => fieldName);
}


/**
 * @param {string} formId
 * @param {Object<string, {
 *     required?: boolean,
 *     numeric?: boolean,
 *     notZero?: boolean,
 *     min?: number,
 *     max?: number,
 *     pattern?: RegExp,
 *     validate?: (value: any, item: Object) => boolean
 * }>} customRules
 */
function getFieldRules(formId, customRules = {}) {
    const form = document.getElementById(formId);
    if (!form) return {};
    
    const rules = {};
    
    form.querySelectorAll("[name]").forEach(element => {
        const name = element.name;
        if (!name) return;
        
        const fieldRules = {};
        
        if (element.required) {
            fieldRules.required = true;
        }
        
        if (element.type === "number" || element.classList.contains("valueNumber") || element.classList.contains("currencyNumber")) {
            fieldRules.numeric = true;
        }
        
        if (element.classList.contains("currencyNumber")) {
            fieldRules.min = 0;
        }
        
        rules[name] = {
            ...fieldRules,
            ...customRules[name],
        };
    });
    
    Object.entries(customRules).forEach(([name, fieldRules]) => {
        rules[name] = {
            ...(rules[name] ?? {}),
            ...fieldRules,
        };
    });
    
    return rules;
}

/* ----------------------------------------------------------------------------- */


let idNotAccept;
let typeNotAccept;
async function showModalNotAccept(id, type) { // type: acc= 0 / pf= 12 financial | 13 tamin | 14: SaleAdmin
    const notAcceptCommentElm = document.getElementById("NotAcceptComment");
    if (!notAcceptCommentElm) return;
    notAcceptCommentElm.value = "";
    document.getElementById("NotAcceptHeader").textContent = type !== 0 ? "عدم تایید پیش‌فاکتور" : "عدم تایید واریزی";
    idNotAccept = id;
    typeNotAccept = type;
    ModalToggle("#NotAcceptModal");
}

async function submitNotAccept() { // رد تایید و بازگشت | عدم تایید
    const notAcceptCommentElm = document.getElementById("NotAcceptComment");
    if (!notAcceptCommentElm) return;
    
    if (!notAcceptCommentElm.value.trim().length) {
        toastifyError("دلیل عدم تایید اجباری است!");
        return;
    }
    
    const userFullName = document.getElementById("profileName")?.textContent || "";
    const model = {
        id: idNotAccept,
        description: `${notAcceptCommentElm.value.trim()} (${userFullName})`,
    };
    
    if (typeNotAccept === 0) { // acc
        const result = await acceptAccountancy(model.id, 3, model.description);
        if (!result) return;
        
    } else { // pf
        const urlMap = {
            12: `/Factor/NotAcceptFinancial`,
            13: `/Factor/NotAcceptTaminExpert`,
            14: `/Factor/NotAcceptSalesAdmin`,
        };
        
        await sendApiRequest({
            url: urlMap[typeNotAccept],
            method: "POST",
            data: model,
            successMsg: "عدم تایید با موفقیت ثبت شد",
            onSuccess: async () => {
                switch (typeNotAccept) {
                    case 12:
                        await GetPreFactorFinancialExpert();
                        ModalToggle("#InvoiceListModalId", "hide");
                        break;
                    case 13:
                        await GetPreFactorTaminExpert();
                        break;
                    case 14:
                        await GetPreFactorSalesAdmin();
                        break;
                }
            },
        });
    }
    notAcceptCommentElm.value = "";
    ModalToggle("#NotAcceptModal");
}

async function getSuppliesByPfId(pfId) {
    return await sendApiRequest({
        url: "/Supply/GetSupplies",
        params: { pfId },
    });
}

async function getBaseItemOp(typeCode, showLoading = false) {
    return await sendApiRequest({
        url: "/BaseItem/GetItemsOp",
        params: { typeCode },
        showLoading,
    });
}

async function getCustomersList(showLoading = false) {
    return await sendApiRequest({
        url: "/Customer/GetCustomersList",
        showLoading,
    });
}

async function getCustomer(id, showLoading = false) {
    return await sendApiRequest({
        url: "/Customer/GetCustomer",
        params: { id },
        showLoading,
    });
}
