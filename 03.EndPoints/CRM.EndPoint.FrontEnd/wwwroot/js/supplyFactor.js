window.supplyFactorAttachments = [];
let orderHeadsSupFactorSelectedIds = [];
let supplyFactorData = { items: [] };
let sfItemIndex = null;
let incompleteSfItemsCount = 0;

const supplyFactorDiscountInp = document.getElementById("sf-discount");


let supplyFactorAttInpId;

if (window.innerWidth >= 992 && window.innerWidth < 1400) {
    supplyFactorAttInpId = "supplyFactorTabletAtt";
} else if (window.innerWidth < 992) {
    supplyFactorAttInpId = "supplyFactorMobileAtt";
} else {
    supplyFactorAttInpId = "supplyFactorAtt";
}


document.querySelector("#supplyFactorItem")?.addEventListener("input", (e) => {
    if (e.target.matches("[name='weightCount']") || e.target.matches("[name='unitAmount']") || e.target.matches("[name='discount']")) {
        calculateItem("supplyFactorItem");
    }
});

document.querySelectorAll("#supplyFactorModal input[type='radio'][name='officialType']")?.forEach(radio => {
    radio.addEventListener("change", async (e) => {
        supplyFactorData.officialType = Number(e.target.value);
        calculateSupplyFactorTotals();
        showSupplyFactorItems();
    });
});

async function showSupplyFactorModal(dealId = null) {
    supplyFactorData.dealId = dealId;
    
    // document.getElementById("supplyFactorOrderHeadWrapper")?.classList.toggle("d-none", dealId !== null);
    // toggleElementsRequired(document.getElementById("supplyFactorOrderHead"), dealId === null);
    
    await initSupplyFactorModal();
    ModalToggle("#supplyFactorModal");
}

async function initSupplyFactorModal() {
    await Promise.all([setupSelectOptions(), getAllProductOp("supplyFactorItem")]);
    Unit(4, "supplyFactorItem");
}

function cancelSupplyFactorModal() {
    window.supplyFactorAttachments = [];
    supplyFactorData = { items: [] };
    
    orderHeadsSupFactorSelectedIds = [];
    
    mySer.emptyForm("supplyFactorItem");
    mySer.emptyForm("supplyFactorHeadFormId");
    
    document.getElementById("sf-discount").value = 0;
    document.getElementById("sf-description").value = "";
    document.getElementById("sf-sumTotalAmount").innerHTML = "";
    document.getElementById("sf-totalTax").innerHTML = "";
    document.getElementById("sf-paymentAmount").innerHTML = "";
    document.getElementById("supplyFactorItemId").innerHTML = "";
    document.getElementById("supplyFactorItemId").classList.remove("AlertReq");
    
    const messageBox = document.getElementById("supplyFactorWarningMessage");
    messageBox.classList.add("d-none");
    messageBox.innerHTML = "";
    // modalSelector.reset();
    // toggleElementsDisabled(document.getElementById("showAddOrderHeadBtn"), true);
    ModalToggle("#supplyFactorModal", "hide");
    clearUpload(supplyFactorAttInpId);
    updateSupplyFactorValidationState();
}


const addSfItem = async () => {
    if (!await mySer.CheckValidForm("#supplyFactorItem")) return false;
    if (!validatePfItemDiscount("supplyFactorItem")) return false;
    
    document.getElementById("supplyFactorItemId").classList.remove("AlertReq");
    
    const productCodeElm = document.querySelector("#supplyFactorItem select[name='productCode']");
    const unitElm = document.querySelector("#supplyFactorItem select[name='unit']");
    
    const data = mySer.convertFormToJSON("#supplyFactorItem");
    
    data.productSmallName = productCodeElm.options[productCodeElm.selectedIndex].text;
    data.unitTxt = unitElm.options[unitElm.selectedIndex].text;
    
    data.totalAmount = Number(SeparatedNumberRemove(data.totalAmount)) || 0;
    data.unitAmount = Number(SeparatedNumberRemove(data.unitAmount)) || 0;
    data.discount = Number(SeparatedNumberRemove(data.discount)) || 0;
    
    supplyFactorData.items.push(data);
    
    calculateSupplyFactorTotals();
    showSupplyFactorItems();
    
    mySer.emptyForm("supplyFactorItem");
    document.getElementById("sf-printableF")?.focus();
    return true;
};

const calculateSupplyFactorTotals = (updateItemsFromDiscount = false) => {
    return calculateInvoiceTotals({
        items: supplyFactorData.items,
        invoice: supplyFactorData,
        discountInputId: "sf-discount",
        sumTotalAmountId: "sf-sumTotalAmount",
        totalTaxId: "sf-totalTax",
        paymentAmountId: "sf-paymentAmount",
        updateItemsFromDiscount,
    });
};


const sfFieldRules = getFieldRules("supplyFactorItem", { weightCount: { notZero: true, min: 1 } });
const showSupplyFactorItems = () => {
    
    document.getElementById("supplyFactorItemId").replaceChildren(
        generateInvoiceItemDom(supplyFactorData.items, {
            onDelete: deleteSfItem,
            onUpdate: showUpdateSfItem,
            fieldRules: sfFieldRules,
        }),
    );
    
    updateSupplyFactorValidationState();
};


const deleteSfItem = (index) => {
    let item = supplyFactorData.items[index];
    
    if (item.id) {
        item.isDeleted = true;
    } else {
        supplyFactorData.items.splice(index, 1);
    }
    
    calculateSupplyFactorTotals();
    showSupplyFactorItems();
};

const showUpdateSfItem = async (index) => {
    if (window.innerWidth < 992) {
        ModalToggle("ItemSupplyFactorModal");
    }
    sfItemIndex = index;
    
    mySer.convertJsonToForm(supplyFactorData.items[index], "supplyFactorItem");
    await mySer.CheckValidForm("#supplyFactorItem");
    
    document.getElementById("sf-CancelBtnId").classList.add("d-none");
    document.getElementById("sf-AddBtnId").classList.add("d-none");
    document.getElementById("sf-UpBtnId").classList.remove("d-none");
};

const updateSfItem = async () => {
    if (!await mySer.CheckValidForm("#supplyFactorItem")) return false;
    if (!validatePfItemDiscount("supplyFactorItem")) return false;
    
    const form = document.getElementById("supplyFactorItem");
    
    const productCode = form.elements.namedItem("productCode");
    const unit = form.elements.namedItem("unit");
    
    const data = mySer.convertFormToJSON("#supplyFactorItem");
    
    data.productSmallName = productCode.options[productCode.selectedIndex].text;
    data.unitTxt = unit.options[unit.selectedIndex].text;
    
    
    supplyFactorData.items[sfItemIndex] = {
        ...supplyFactorData.items[sfItemIndex],
        ...data,
    };
    
    
    calculateSupplyFactorTotals();
    showSupplyFactorItems();
    
    toastifySuccess("با موفقیت ویرایش شد!");
    cancelUpdateSfItem();
};

const cancelUpdateSfItem = () => {
    mySer.emptyForm("supplyFactorItem");
    sfItemIndex = null;
    
    document.getElementById("sf-AddBtnId").classList.remove("d-none");
    document.getElementById("sf-CancelBtnId").classList.remove("d-none");
    document.getElementById("sf-UpBtnId").classList.add("d-none");
    
    if (window.innerWidth < 992) {
        ModalToggle("ItemSupplyFactorModal", "hide");
    }
};

const addItemSupplyFactor = async () => {
    FLshowLoading();
    const isValid = await addSfItem();
    FLshowLoading(false);
    if (isValid) {
        ModalToggle("ItemSupplyFactorModal", "hide");
        toastifySuccess("با موفقیت اضافه شد!");
    }
};


const canselModalItemSupplyFactor = () => {
    mySer.emptyForm("supplyFactorItem");
    ModalToggle("ItemSupplyFactorModal", "hide");
};


supplyFactorDiscountInp?.addEventListener("input", () => {
    calculateSupplyFactorTotals(true);
    showSupplyFactorItems();
});


async function proccessSupplyFactorWithAi(btn) {
    if (window.supplyFactorAttachments.every(item => item.isDeleted)) {
        toastifyError("سند فاکتور باید بارگذاری شود");
        return;
    }
    
    const spinner = btn.querySelector(".spinner-border");
    const aiIcon = btn.querySelector(".ai-svg-icon");
    const elementsToDisable = [
        btn,
        ...Array.from(document.getElementById("supplyFactorHeadFormId").elements),
        ...Array.from(document.getElementById("supplyFactorItem").elements),
        document.getElementById("cancelSupplyFactorBtn"),
        document.getElementById("submitSupplyFactorBtn"),
        supplyFactorDiscountInp,
    ];
    
    toggleElementsDisabled(elementsToDisable);
    spinner.classList.remove("d-none");
    aiIcon.classList.add("d-none");
    
    /*await delay(3000);
    const sfData = {
        "date": "1405/01/26",
        "serialNumber": "2117",
        "officialType": 1,
        "items": [{
            "product": "لفاف ملزومات پر کردن ، دربندی و بسته بندی",
            "weightCount": 966,
            "unit": 2,
            "unitTxt": "کیلوگرم",
            "unitAmount": 3950000,
            "totalAmount": 3815700000,
            "discount": 0,
            "taxAmount": 381570000,
            "productCode": null,
            "productSmallName": null,
            "printable": 0,
        }],
    };*/
    
    const attachFile = window.supplyFactorAttachments.filter(i => !i.isDeleted)[0];
    
    await sendApiRequest({
        url: "/Supply/ProccessSupplyFactorWithAi/",
        method: "POST",
        data: attachFile,
        successMsg: null,
        showLoading: false,
        onSuccess: (res) => {
            supplyFactorData = { ...supplyFactorData, ...res.data };
            
            calculateSupplyFactorTotals();
            showSupplyFactorItems();
            mySer.convertJsonToForm(supplyFactorData, "supplyFactorHeadFormId", [], true);
            
            const messageBox = document.getElementById("supplyFactorWarningMessage");
            const message = createInlineAlert({
                type: "warning",
                message: `اطلاعات این فاکتور توسط هوش مصنوعی استخراج شده است و ممکن است خطا داشته باشد؛ لطفاً پیش از ثبت، از صحت اطلاعات مطمئن شوید.`,
            });
            messageBox.classList.remove("d-none");
            messageBox.replaceChildren(message);
        },
    });
    
    
    toggleElementsDisabled(elementsToDisable, false);
    spinner.classList.add("d-none");
    aiIcon.classList.remove("d-none");
}


const updateSupplyFactorValidationState = () => {
    incompleteSfItemsCount = getIncompleteItemsCount(supplyFactorData.items, sfFieldRules);
    
    const messageBox = document.getElementById("supplyFactorValidationMessage");
    
    if (incompleteSfItemsCount > 0) {
        const message = createInlineAlert({
            type: "error",
            message: `${incompleteSfItemsCount} آیتم ناقص است؛ برای ثبت فاکتور، ابتدا آیتم‌های ناقص را ویرایش و تکمیل کنید.`,
        });
        
        messageBox.classList.remove("d-none");
        messageBox.replaceChildren(message);
    } else {
        messageBox.classList.add("d-none");
        messageBox.innerHTML = "";
    }
};

///////////////////
async function showAddOrderHeadSfModal() {
    await modalSelector.open({
        title: "انتخاب حواله تولید",
        selectedIds: orderHeadsSupFactorSelectedIds,
        load: async ({ search, selectedIds }) => {
            FLshowLoading();
            const supplierId = document.getElementById("supplyFactorSupplierId").value;
            
            let filterIds = supplierId
                ? await getOrderHeadIdsBySupplier(supplierId)
                : null;
            
            if (filterIds) {
                filterIds = [...new Set([...filterIds, ...selectedIds])];
            }
            
            const res = await axios.post("/Order/GetAllOrderHeads", { search, passType: 3, selectedIds, filterIds });
            FLshowLoading(false);
            return res.data;
        },
        
        onSubmit: ({ ids, numbers }) => {
            orderHeadsSupFactorSelectedIds = ids;
            const input = document.getElementById("supplyFactorOrderHead");
            
            input.value = numbers.join(", ");
            input.classList.remove("AlertReq");
        },
    });
}


let isSetupSupplyTomSelect = false;
async function setupSelectOptions() {
    if (isSetupSupplyTomSelect) return;
    // const addOrderBtn = document.getElementById("showAddOrderHeadBtn");
    const data = (await axios("/Supply/GetSupplyBaseData")).data;
    new TomSelect("#sf-SupplierId", {
        options: data.suppliers,
        placeholder: "انتخاب تامین کننده",
        maxItems: 1,
        selectOnTab: true,
        
        /*onItemAdd() {
            toggleElementsDisabled(addOrderBtn, false);
            document.getElementById("supplyFactorOrderHead").value = "";
            orderHeadsSupFactorSelectedIds = [];
        },
        
        onDelete() {
            toggleElementsDisabled(addOrderBtn, true);
        },*/
    });
    
    isSetupSupplyTomSelect = true;
}

async function submitSupplyFactorModal() {
    const activeItems = supplyFactorData.items.filter(item => !item.isDeleted);
    if (!activeItems.length) {
        document.getElementById("supplyFactorItemId").classList.add("AlertReq");
        toastifyError("فاکتور خالی است");
        return;
    }
    
    if (incompleteSfItemsCount > 0) {
        toastifyError("برای ثبت فاکتور، ابتدا آیتم‌های ناقص را ویرایش و تکمیل کنید.");
        return;
    }
    
    if (!await mySer.CheckValidForm("#supplyFactorHeadFormId")) return;
    
    if (window.supplyFactorAttachments.every(item => item.isDeleted)) {
        toastifyError("تصویر فاکتور اجباری است");
        return;
    }
    
    const supplierSelect = document.querySelector("#supplyFactorModal select[name='supplierId']");
    const model = {
        ...supplyFactorData,
        ...mySer.convertFormToJSON("supplyFactorHeadFormId"),
        supplierTxt: supplierSelect.options[supplierSelect.selectedIndex].text,
        description: document.getElementById("sf-description")?.value?.trim(),
        attachments: window.supplyFactorAttachments,
    };
    
    await sendApiRequest({
        method: "POST",
        url: model.id ? "/Supply/UpdateSupplyFactor" : "/Supply/AddSupplyFactor",
        data: model,
        successMsg: model.id ? "با موفقیت ویرایش شد" : "با موفقیت ثبت شد",
        onSuccess: () => {
            cancelSupplyFactorModal();
            
            if (typeof getAllSupplyFactors === "function") {
                getAllSupplyFactors();
            }
            
            if (typeof GetDealFinancialExpert === "function") {
                GetDealFinancialExpert();
            }
            
            if (typeof refreshDealSupplyFactors === "function") {
                refreshDealSupplyFactors();
            }
        },
    });
}


async function getSupplyFactor(id) {
    return await sendApiRequest({
        url: "/Supply/GetSupplyFactor",
        params: { id },
    });
}

async function showEditSupplyFactorModal(id) {
    await initSupplyFactorModal();
    
    const data = await getSupplyFactor(id);
    if (!data) return;
    
    supplyFactorData = data.supplyFactor;
    window.supplyFactorAttachments = data.attachments;
    
    FileUpReload(supplyFactorAttInpId);
    mySer.convertJsonToForm(supplyFactorData, "supplyFactorHeadFormId");
    
    document.getElementById("sf-sumTotalAmount").textContent = supplyFactorData.sumTotalAmount.toLocaleString();
    document.getElementById("sf-totalTax").textContent = supplyFactorData.totalTax.toLocaleString();
    document.getElementById("sf-paymentAmount").textContent = supplyFactorData.paymentAmount.toLocaleString();
    document.getElementById("sf-discount").value = supplyFactorData.discount.toLocaleString();
    document.getElementById("sf-description").value = supplyFactorData.description;
    
    /*orderHeadsSupFactorSelectedIds = supplyFactorData.supplyFactorOrderHeads.map(o => o.orderHeadId);
    document.getElementById("supplyFactorOrderHead").value = res.data.orderSerials.join(", ");*/
    
    showSupplyFactorItems();
    ModalToggle("#supplyFactorModal");
}


const deleteSupplyFactor = async (id) => {
    await deleteItem({
        url: `/Supply/DeleteSupplyFactor/${id}`,
        callback: () => {
            if (typeof getAllSupplyFactors === "function") {
                getAllSupplyFactors();
            }
            
            if (typeof GetDealFinancialExpert === "function") {
                GetDealFinancialExpert();
            }
            
            if (typeof refreshDealSupplyFactors === "function") {
                refreshDealSupplyFactors();
            }
        },
    });
};

// Post-registration allocation of supply items to sales invoice items.
let sfAllocationState = null;
let sfAllocationSaving = false;
const sfAllocationEscape = value => String(value ?? "").replace(/[&<>"']/g, c =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
const sfAllocationNumber = value => Number(value).toLocaleString("fa-IR", { maximumFractionDigits: 2 });
// Calculate in hundredths to match decimal(18,2) and avoid floating-point input limits.
const sfAllocationRemaining = item =>
    (Math.round(Number(item.weightCount) * 100) - Math.round(Number(item.allocated) * 100)) / 100;

function sfAllocationSalesWeight(value) {
    const normalized = String(value ?? "").trim()
        .replace(/[۰-۹]/g, digit => String(digit.charCodeAt(0) - "۰".charCodeAt(0)))
        .replace(/[٠-٩]/g, digit => String(digit.charCodeAt(0) - "٠".charCodeAt(0)))
        .replace(/[,٬\s]/g, "").replace(/٫/g, ".");
    if (!/^[+-]?(?:\d+(?:\.\d*)?|\.\d+)$/.test(normalized)) return 0;
    const weight = Number(normalized);
    return Number.isFinite(weight) && weight > 0 ? weight : 0;
}

function sfAllocationTargetLabel(allocation) {
    return allocation.warehouseId ? allocation.warehouseTitle
        : `${allocation.customer} | فاکتور ${allocation.serialNumber} | ${allocation.productSmallName || allocation.product} | ${allocation.factorItemWeightCount} ${allocation.factorItemUnitTxt ?? ""}`;
}

function sfAllocationSalesCapacity(allocation) {
    return Math.max(0, (Math.round(sfAllocationSalesWeight(allocation.factorItemWeightCount) * 100)
        - Math.round(Number(allocation.factorItemAllocated) * 100) + Math.round(allocation.weight * 100)) / 100);
}

function updateSupplyAllocationLimit(form) {
    const item = sfAllocationState.items.find(i => i.id === form.closest("[data-item-id]").dataset.itemId);
    const previous = sfAllocationState.allocations.find(a => a.id === form.dataset.allocationId);
    const supplyCapacity = (Math.round(sfAllocationRemaining(item) * 100) + Math.round((previous?.weight ?? 0) * 100)) / 100;
    const salesWeight = sfAllocationSalesWeight(form.elements.factorItemId.dataset.salesWeight);
    const capacity = form.elements.factorItemId.dataset.warehouseId ? supplyCapacity : Math.min(supplyCapacity, salesWeight);
    form.elements.weight.max = capacity;
    form.elements.weight.placeholder = `حداکثر ${sfAllocationNumber(capacity)}`;
}

function sfAllocationFeedback(message, error = false) {
    const box = document.getElementById("sfAllocationFeedback");
    box.className = message ? `alert ${error ? "alert-danger" : "alert-info"}` : "";
    box.textContent = message;
}

async function showSupplyAllocations(id) {
    ModalToggle("#sfAllocationsModal", "show");
    sfAllocationState = null;
    document.getElementById("sfAllocationSearch").value = "";
    document.getElementById("sfAllocationItems").replaceChildren();
    sfAllocationFeedback("در حال دریافت آیتم‌ها…");
    try {
        await loadSupplyAllocations(id);
        sfAllocationFeedback("");
    } catch (error) {
        sfAllocationFeedback("دریافت تخصیص‌ها انجام نشد؛ پنجره را ببندید و دوباره تلاش کنید.", true);
    }
}

async function loadSupplyAllocations(id) {
    const { data } = await axios.get("/Supply/GetSupplyFactorAllocations", { params: { id } });
    sfAllocationState = data;
    document.querySelectorAll("[data-supply-factor-id]").forEach(card => {
        if (card.dataset.supplyFactorId !== data.factor.id) return;
        card.classList.toggle("sf-supply-card-complete", data.isAllocationComplete);
        card.querySelector(".sf-supply-complete-badge")?.classList.toggle("d-none", !data.isAllocationComplete);
    });
    document.getElementById("sfAllocationsTitle").textContent =
        `تخصیص آیتم‌های فاکتور تامین ${data.factor.serialNumber} به فروش یا انبار`;
    renderSupplyAllocations();
}

function renderSupplyAllocations() {
    const query = document.getElementById("sfAllocationSearch").value.trim().toLocaleLowerCase();
    const items = sfAllocationState?.items.filter(i =>
        `${i.productSmallName ?? ""} ${i.product ?? ""}`.toLocaleLowerCase().includes(query)) ?? [];
    const escape = sfAllocationEscape;
    document.getElementById("sfAllocationItems").innerHTML = items.map(item => {
        const remaining = sfAllocationRemaining(item);
        const allocations = sfAllocationState.allocations.filter(a => a.supplyFactorItemId === item.id);
        return `<section class="sf-allocation-item" data-item-id="${escape(item.id)}">
            <div class="d-flex flex-wrap justify-content-between align-items-start gap-2">
                <div><strong>${escape(item.productSmallName || item.product)}</strong>
                    <div class="text-muted small">${escape(item.product)}</div></div>
                <div class="sf-allocation-totals">
                    <span>کل: <b>${sfAllocationNumber(item.weightCount)}</b></span>
                    <span>تخصیص‌یافته: <b>${sfAllocationNumber(item.allocated)}</b></span>
                    <span class="${remaining > 0 ? "text-primary" : "text-success"}">باقی‌مانده:
                        <b>${sfAllocationNumber(remaining)}</b> ${escape(item.unitTxt)}</span>
                </div>
            </div>
            ${allocations.length ? `<div class="table-responsive mt-2"><table class="table table-sm table-striped mb-2">
                <thead><tr><th>مشتری / مقصد</th><th>فاکتور فروش</th><th>محصول / شرح</th><th>مقدار آیتم فروش</th><th>مقدار تخصیص</th><th>عملیات</th></tr></thead>
                <tbody>${allocations.map(a => `<tr><td>${escape(a.customer)}</td><td>${escape(a.serialNumber ?? "—")}</td>
                    <td>${escape(a.productSmallName)}<div class="small text-muted">${escape(a.product)}</div></td>
                    <td>${a.warehouseId ? "—" : `${escape(a.factorItemWeightCount)} ${escape(a.factorItemUnitTxt)}`}</td>
                    <td>${sfAllocationNumber(a.weight)}</td>
                    <td><div class="d-flex gap-2">
                        <button type="button" class="btn btn-sm btn-outline-primary sf-allocation-edit" data-allocation-id="${escape(a.id)}">ویرایش</button>
                        <button type="button" class="btn btn-sm btn-outline-danger sf-allocation-delete" data-allocation-id="${escape(a.id)}">حذف</button>
                    </div></td></tr>
                    <tr class="sf-allocation-edit-row d-none" data-edit-allocation-id="${escape(a.id)}"><td colspan="6">
                        <form class="sf-allocation-form sf-allocation-edit-form d-flex flex-wrap align-items-end gap-2" data-allocation-id="${escape(a.id)}">
                            <div class="sf-allocation-target">
                                <label class="small d-block">مقصد تخصیص</label>
                                <button type="button" class="btn btn-outline-primary w-100 text-start sf-allocation-select">${escape(sfAllocationTargetLabel(a))}</button>
                                <input type="hidden" name="factorItemId" value="${escape(a.warehouseId || a.factorItemId)}" data-warehouse-id="${escape(a.warehouseId)}" data-sales-weight="${a.warehouseId ? 0 : sfAllocationSalesCapacity(a)}" />
                            </div>
                            <label class="small mb-0">مقدار / وزن (${escape(item.unitTxt)})
                                <input name="weight" class="form-control" type="number" min="0.01" step="0.01"
                                    max="${a.warehouseId ? (Math.round(remaining * 100) + Math.round(a.weight * 100)) / 100 : Math.min((Math.round(remaining * 100) + Math.round(a.weight * 100)) / 100, sfAllocationSalesCapacity(a))}"
                                    value="${a.weight}" required inputmode="decimal" /></label>
                            <button type="submit" class="btn btn-primary">ذخیره ویرایش</button>
                            <button type="button" class="btn btn-outline-secondary sf-allocation-cancel-edit">انصراف</button>
                        </form>
                    </td></tr>`).join("")}</tbody></table></div>`
                : '<div class="text-muted small my-2">هنوز تخصیصی ثبت نشده است.</div>'}
            ${remaining > 0 ? `<form class="sf-allocation-form d-flex flex-wrap align-items-end gap-2">
                <div class="sf-allocation-target">
                    <label class="small d-block">مقصد تخصیص</label>
                    <button type="button" class="btn btn-outline-primary w-100 text-start sf-allocation-select">انتخاب آیتم فروش یا انبار…</button>
                    <input type="hidden" name="factorItemId" />
                </div>
                <label class="small mb-0">مقدار / وزن (${escape(item.unitTxt)})
                    <input name="weight" class="form-control" type="number" min="0.01" step="0.01"
                        max="${remaining}" required inputmode="decimal" placeholder="ابتدا آیتم فروش را انتخاب کنید" /></label>
                <button type="submit" class="btn btn-primary">ثبت تخصیص</button>
            </form>` : '<span class="badge bg-success">تخصیص کامل شد</span>'}
        </section>`;
    }).join("") || '<div class="text-center text-muted p-4">آیتمی یافت نشد.</div>';
}

async function selectSupplyAllocationFactor(section) {
    const button = section.querySelector(".sf-allocation-select");
    button.disabled = true;
    const target = section.elements.factorItemId;
    const salesWeights = new Map([[target.value, target.dataset.salesWeight]]);
    const warehouseIds = new Set(target.dataset.warehouseId ? [target.dataset.warehouseId] : []);
    const supplyFactorItemId = section.closest("[data-item-id]").dataset.itemId;
    const allocationId = section.dataset.allocationId;
    try {
        await modalSelector.open({
            title: "انتخاب آیتم فروش یا انبار — جست‌وجو و Enter",
            size: "xl",
            selectedIds: section.querySelector('[name="factorItemId"]').value
                ? [section.querySelector('[name="factorItemId"]').value] : [],
            load: async ({ search }) => {
                FLshowLoading();
                try {
                    const { data } = await axios.get("/Supply/SearchAllocationFactorItems", { params: { search, supplyFactorItemId, allocationId } });
                    const escape = sfAllocationEscape;
                    data.items.forEach(i => salesWeights.set(i.id, Number(i.available)));
                    (data.warehouses ?? []).forEach(w => warehouseIds.add(w.id));
                    const usedWarehouses = new Set(sfAllocationState.allocations.filter(a =>
                        a.supplyFactorItemId === supplyFactorItemId && a.id !== allocationId && a.warehouseId).map(a => a.warehouseId));
                    const warehousesHtml = `<div class="sf-allocation-warehouse-options p-3">
                        <div class="small text-muted mb-2">انتقال به انبار (بدون اتصال به فاکتور فروش)</div>
                        <table class="table table-sm mb-0"><tbody>${(data.warehouses ?? []).map(w => `
                            <tr class="sf-allocation-sale-row ${usedWarehouses.has(w.id) ? "" : "sf-allocation-sale-selectable"}">
                                <td class="sf-allocation-radio-cell"><input type="radio" name="sfAllocationFactorItem"
                                    value="${escape(w.id)}" data-number="${escape(w.title)}" aria-label="${escape(w.title)}"
                                    ${usedWarehouses.has(w.id) ? "disabled" : ""} /></td>
                                <td>${escape(w.title)} ${usedWarehouses.has(w.id) ? '<span class="small text-muted">— قبلاً تخصیص داده شده؛ تخصیص قبلی را ویرایش کنید</span>' : ""}</td>
                            </tr>`).join("") || '<tr><td>انبار مقصد تعریف نشده است.</td></tr>'}</tbody></table>
                    </div>`;
                    return `${warehousesHtml}<div class="alert alert-light small" role="status">۲۰۰ آیتم آخر؛ برای جست‌وجو در تمام آیتم‌ها نام مشتری، شماره فاکتور یا محصول را وارد کنید و Enter بزنید.
                        ${data.hasMore ? "نتایج بیشتری وجود دارد؛ جست‌وجو را دقیق‌تر کنید." : ""}</div>
                        <div class="table-responsive sf-allocation-picker"><table class="table table-striped table-hover">
                        <thead><tr><th>انتخاب</th><th>مشتری</th><th>شماره فاکتور</th><th>تاریخ</th><th>محصول</th><th>شرح محصول</th><th>مقدار کل</th><th>تخصیص‌یافته</th><th>باقی‌مانده</th></tr></thead>
                        <tbody>${data.items.map(i => `<tr class="sf-allocation-sale-row ${i.remaining <= 0 ? "sf-allocation-sale-complete" : ""} ${i.available > 0 ? "sf-allocation-sale-selectable" : ""}">
                            <td><input type="radio" name="sfAllocationFactorItem" value="${escape(i.id)}" ${i.available > 0 ? "" : "disabled"}
                                aria-label="${escape(i.customer)} — ${escape(i.serialNumber)} — ${escape(i.product)}"
                                data-number="${escape(`${i.customer} | فاکتور ${i.serialNumber} | ${i.productSmallName || i.product} | ${i.weightCount} ${i.unitTxt ?? ""}`)}" /></td>
                            <td>${escape(i.customer)}</td><td>${escape(i.serialNumber)}</td><td>${escape(i.date)}</td>
                            <td>${escape(i.productSmallName)}</td><td>${escape(i.product)}</td>
                            <td>${escape(i.weightCount)} ${escape(i.unitTxt)}</td>
                            <td>${sfAllocationNumber(i.allocated)} ${escape(i.unitTxt)}</td>
                            <td>${sfAllocationNumber(i.remaining)} ${escape(i.unitTxt)}${i.available <= 0 ? '<div class="small text-success">تخصیص کامل</div>' : ""}</td></tr>`).join("")
                            || '<tr><td colspan="9" class="text-center">نتیجه‌ای پیدا نشد؛ عبارت دیگری جست‌وجو کنید.</td></tr>'}
                        </tbody></table></div>`;
                } catch (error) {
                    toastifyError("دریافت آیتم‌های فروش انجام نشد؛ دوباره جست‌وجو کنید.");
                    return '<div class="alert alert-danger">دریافت اطلاعات انجام نشد؛ برای تلاش مجدد Enter بزنید.</div>';
                } finally {
                    FLshowLoading(false);
                }
            },
            onSubmit: ({ ids, numbers }) => {
                target.value = ids[0];
                target.dataset.salesWeight = salesWeights.get(ids[0]) ?? 0;
                target.dataset.warehouseId = warehouseIds.has(ids[0]) ? ids[0] : "";
                updateSupplyAllocationLimit(section);
                button.textContent = numbers[0] || "آیتم انتخاب شد";
                document.getElementById("sfAllocationsModal").addEventListener("shown.bs.modal", () => {
                    section.querySelector('[name="weight"]').focus();
                }, { once: true });
            }
        });
        ModalToggle("#sfAllocationsModal", "hide");
        modalSelector.modal.addEventListener("hidden.bs.modal", () => {
            ModalToggle("#sfAllocationsModal", "show");
        }, { once: true });
    } catch (error) {
        toastifyError("باز کردن انتخاب آیتم انجام نشد.");
    } finally {
        button.disabled = false;
    }
}

document.addEventListener("input", event => {
    if (event.target.id === "sfAllocationSearch") renderSupplyAllocations();
});
document.addEventListener("click", event => {
    const salesRow = event.target.closest(".sf-allocation-sale-row");
    if (salesRow && !event.target.closest("input, button, a, label")) {
        const radio = salesRow.querySelector('input[name="sfAllocationFactorItem"]');
        if (radio && !radio.disabled) radio.click();
    }
    const button = event.target.closest(".sf-allocation-select");
    if (button && !sfAllocationSaving) selectSupplyAllocationFactor(button.closest(".sf-allocation-form"));
    const edit = event.target.closest(".sf-allocation-edit");
    if (edit && !sfAllocationSaving) {
        const section = edit.closest("[data-item-id]");
        const row = [...section.querySelectorAll("[data-edit-allocation-id]")]
            .find(r => r.dataset.editAllocationId === edit.dataset.allocationId);
        row.classList.remove("d-none");
        updateSupplyAllocationLimit(row.querySelector("form"));
        row.querySelector('[name="weight"]').focus();
    }
    const cancel = event.target.closest(".sf-allocation-cancel-edit");
    if (cancel && !sfAllocationSaving) {
        const form = cancel.closest("form");
        form.reset();
        const allocation = sfAllocationState.allocations.find(a => a.id === form.dataset.allocationId);
        form.elements.factorItemId.dataset.warehouseId = allocation.warehouseId || "";
        form.elements.factorItemId.dataset.salesWeight = allocation.warehouseId ? 0 : sfAllocationSalesCapacity(allocation);
        updateSupplyAllocationLimit(form);
        form.querySelector(".sf-allocation-select").textContent =
            sfAllocationTargetLabel(allocation);
        cancel.closest(".sf-allocation-edit-row").classList.add("d-none");
    }
    const remove = event.target.closest(".sf-allocation-delete");
    if (remove) deleteSupplyAllocation(remove);
});
document.addEventListener("submit", async event => {
    const form = event.target.closest(".sf-allocation-form");
    if (!form) return;
    event.preventDefault();
    if (sfAllocationSaving || !form.reportValidity()) return;
    const section = form.closest("[data-item-id]");
    const selectedId = form.elements.factorItemId.value;
    const warehouseId = form.elements.factorItemId.dataset.warehouseId || null;
    const factorItemId = warehouseId ? null : selectedId;
    const weight = Number(form.elements.weight.value);
    const item = sfAllocationState.items.find(i => i.id === section.dataset.itemId);
    if (!selectedId) { toastifyError("ابتدا آیتم فروش یا انبار مقصد را انتخاب کنید."); return; }
    const allocationId = form.dataset.allocationId;
    if (sfAllocationState.allocations.some(a => a.supplyFactorItemId === item.id
        && (warehouseId ? a.warehouseId === warehouseId : a.factorItemId === factorItemId) && a.id !== allocationId)) {
        toastifyError("این مقصد قبلاً تخصیص داده شده است؛ تخصیص قبلی را ویرایش کنید."); return;
    }
    const previous = sfAllocationState.allocations.find(a => a.id === allocationId);
    const capacity = (Math.round(sfAllocationRemaining(item) * 100) + Math.round((previous?.weight ?? 0) * 100)) / 100;
    if (!Number.isFinite(weight) || weight <= 0 || weight > capacity) {
        toastifyError("مقدار تخصیص باید مثبت و حداکثر برابر باقی‌مانده باشد."); return;
    }
    const salesWeight = sfAllocationSalesWeight(form.elements.factorItemId.dataset.salesWeight);
    if (!warehouseId && (!salesWeight || weight > salesWeight)) {
        toastifyError("مقدار تخصیص نمی‌تواند بیشتر از باقی‌مانده آیتم فروش در تمام فاکتورهای تامین باشد."); return;
    }
    sfAllocationSaving = true;
    const modal = document.getElementById("sfAllocationsModal");
    const controls = [...modal.querySelectorAll("button, input")];
    const previousDisabled = controls.map(c => c.disabled);
    controls.forEach(c => c.disabled = true);
    sfAllocationFeedback("در حال ثبت تخصیص…");
    const supplyFactorId = sfAllocationState.factor.id;
    try {
        await axios.post(allocationId ? "/Supply/UpdateSupplyFactorAllocation" : "/Supply/AddSupplyFactorAllocation", {
            id: allocationId, supplyFactorItemId: item.id, factorItemId, warehouseId, weight
        }, { headers: { RequestVerificationToken: modal.querySelector('[name="__RequestVerificationToken"]').value } });
        try {
            await loadSupplyAllocations(supplyFactorId);
            sfAllocationFeedback("تخصیص ثبت شد. می‌توانید مقصد دیگری انتخاب کنید.");
        } catch (error) {
            // Prevent resubmitting an already committed allocation if the subsequent read fails.
            form.remove();
            sfAllocationFeedback("تخصیص ثبت شد، اما دریافت وضعیت جدید انجام نشد. پنجره را دوباره باز کنید.", true);
        }
    } catch (error) {
        sfAllocationFeedback(typeof error.response?.data === "string" ? error.response.data
            : "ثبت تخصیص انجام نشد؛ دوباره تلاش کنید.", true);
    } finally {
        controls.forEach((c, index) => c.disabled = previousDisabled[index]);
        sfAllocationSaving = false;
    }
});

async function deleteSupplyAllocation(button) {
    if (sfAllocationSaving) return;
    // Reserve the operation while the existing confirmation dialog is open.
    sfAllocationSaving = true;
    const modal = document.getElementById("sfAllocationsModal");
    let controls = [], previousDisabled = [];
    try {
        if (!await confirmAlert({ title: "حذف تخصیص؟", text: "مقدار این تخصیص به باقی‌مانده آیتم تامین برمی‌گردد.", confirmText: "حذف" })) return;
        const supplyFactorId = sfAllocationState.factor.id;
        const supplyFactorItemId = button.closest("[data-item-id]").dataset.itemId;
        controls = [...modal.querySelectorAll("button, input")];
        previousDisabled = controls.map(c => c.disabled);
        controls.forEach(c => c.disabled = true);
        sfAllocationFeedback("در حال حذف تخصیص…");
        await axios.post("/Supply/DeleteSupplyFactorAllocation", {
            id: button.dataset.allocationId, supplyFactorItemId
        }, { headers: { RequestVerificationToken: modal.querySelector('[name="__RequestVerificationToken"]').value } });
        try {
            await loadSupplyAllocations(supplyFactorId);
            sfAllocationFeedback("تخصیص حذف شد و مقدار باقی‌مانده به‌روز شد.");
        } catch (error) {
            button.closest("tr").remove();
            sfAllocationFeedback("تخصیص حذف شد؛ برای دریافت وضعیت جدید پنجره را دوباره باز کنید.", true);
        }
    } catch (error) {
        sfAllocationFeedback(typeof error.response?.data === "string" ? error.response.data
            : "حذف تخصیص انجام نشد؛ دوباره تلاش کنید.", true);
    } finally {
        controls.forEach((c, i) => c.disabled = previousDisabled[i]);
        sfAllocationSaving = false;
    }
}
