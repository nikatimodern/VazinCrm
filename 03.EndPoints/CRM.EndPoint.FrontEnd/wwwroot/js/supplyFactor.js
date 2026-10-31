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


document.addEventListener("input", (e) => {
    if (!e.target.closest("#supplyFactorItem")) return;
    if (e.target.matches("[name='weightCount']") || e.target.matches("[name='unitAmount']") || e.target.matches("[name='discount']")) {
        calculateItem("supplyFactorItem");
    }
});

document.addEventListener("change", async (e) => {
    if (e.target.matches("#supplyFactorModal input[type='radio'][name='officialType']")) {
        supplyFactorData.officialType = Number(e.target.value);
        calculateSupplyFactorTotals();
        showSupplyFactorItems();
    }
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


document.addEventListener("input", (e) => {
    if (!e.target.matches("#supplyFactorModal #sf-discount")) return;
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
        document.getElementById("sf-discount"),
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

async function getOrderHeadIdsBySupplier(supplierId) {
    try {
        const res = await axios(`/Supply/GetOrderHeadsBySupplier?supplierId=${supplierId}`);
        if (res.status === 200) {
            return res.data;
        }
        return [];
    } catch (err) {
        console.log(err);
        return [];
    }
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

// Post-registration allocation workspace. Styles live exclusively in supplyFactor.css.
let sfAllocationState = null;
let sfAllocationPerspective = "supply";
const sfAllocationIsSales = () => sfAllocationPerspective === "sales";
let sfAllocationSaving = false;
let sfAllocationChoosing = false;
let sfAllocationSelector = null;
let sfAllocationRequest = 0;
let sfAllocationAbort = null;
let sfAllocationOrigin = null;
let sfAllocationInvoiceId = null;
let sfAllocationFilter = "all";
let sfAllocationCurrentItemId = null;
let sfAllocationOpen = new Set();
let sfAllocationDrafts = new Map();
let sfPickerTab = "sales";
let sfPickerData = null;
let sfPickerTargets = new Map();
let sfPickerForm = null;
let sfPickerAbort = null;
let sfPickerRequest = 0;
let sfPickerTimer = null;
let sfPickerLoading = false;
// Display values are normalized before interpolation, including imported literal null strings.
const sfAllocationText = (value, fallback = "") => {
    const text = value == null ? "" : String(value).trim();
    return !text || /^(null|undefined|nan)$/i.test(text) ? fallback : text;
};
const sfAllocationEscape = value => sfAllocationText(value).replace(/[&<>"']/g, c =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" })[c]);
const sfAllocationNumber = value => Number.isFinite(Number(value))
    ? Number(value).toLocaleString("fa-IR", { maximumFractionDigits: 2 }) : "—";
const sfAllocationRemaining = item => (Math.round(Number(item.weightCount) * 100) - Math.round(Number(item.allocated) * 100)) / 100;
const sfAllocationNormalize = value => sfAllocationText(value).toLocaleLowerCase().replace(/ي/g, "ی").replace(/ك/g, "ک").trim();

function sfAllocationIcon(name) {
    const paths = {
        close: "m6 6 12 12M6 18 18 6", search: "M21 21l-5-5M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0",
        check: "m5 12 4 4L19 6", plus: "M12 5v14M5 12h14", arrow: "m8 5 7 7-7 7",
        chevron: "m6 9 6 6 6-6", edit: "m14 4 6 6M4 20l5-1L21 7l-5-5L4 14v6Z",
        trash: "M4 6h16M9 6V3h6v3M6 6l1 15h10l1-15M10 10v7M14 10v7",
        warehouse: "m3 10 9-7 9 7v11H3V10ZM8 21V11h8v10M8 15h8",
        flow: "M5 5h5v5H5zM14 14h5v5h-5zM8 10v7h6M14 7h5M17 4l3 3-3 3",
        info: "M12 11v6M12 7v1M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0",
        invoice: "M7 3h7l4 4v14H6V3h1ZM14 3v5h5M9 12h6M9 16h6",
    };
    return `<svg class="sf-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="${paths[name] || paths.info}"/></svg>`;
}

function sfAllocationSalesWeight(value) {
    const normalized = String(value ?? "").trim()
                                          .replace(/[۰-۹]/g, d => String(d.charCodeAt(0) - "۰".charCodeAt(0)))
                                          .replace(/[٠-٩]/g, d => String(d.charCodeAt(0) - "٠".charCodeAt(0)))
                                          .replace(/[,٬\s]/g, "").replace(/٫/g, ".");
    if (!/^[+-]?(?:\d+(?:\.\d*)?|\.\d+)$/.test(normalized)) return 0;
    const weight = Number(normalized);
    return Number.isFinite(weight) && weight > 0 ? weight : 0;
}

function sfAllocationDisplayRecord(record) {
    const copy = { ...record };
    for (const key of ["customer", "serialNumber", "date", "product", "productSmallName", "unitTxt",
        "warehouseTitle", "sourceWarehouseTitle", "factorItemWeightCount", "factorItemUnitTxt"]) {
        if (key in copy) copy[key] = sfAllocationText(copy[key]);
    }
    if (copy.sourceWarehouseProductId) copy.sourceWarehouseAvailable = Math.max(
        0,
        Math.min(Number(copy.sourceWarehouseStock) - Number(copy.sourceWarehouseReserved), Number(copy.sourceWarehouseRemainder)));
    return copy;
}
function sfAllocationTargetLabel(a) {
    if (a.warehouseId && !(sfAllocationIsSales() && a.sourceWarehouseProductId)) return sfAllocationText(a.warehouseTitle, "انبار");
    const serial = sfAllocationText(a.serialNumber);
    const quantity = sfAllocationText(a.factorItemWeightCount ?? a.weightCount);
    const unit = sfAllocationText(a.factorItemUnitTxt ?? a.unitTxt);
    return [sfAllocationIsSales() && a.sourceWarehouseProductId ? sfAllocationText(a.warehouseTitle) : "", sfAllocationText(a.customer), serial ? `فاکتور ${sfAllocationIsSales() ? "تامین " : ""}${serial}` : "",
        sfAllocationText(a.productSmallName) || sfAllocationText(a.product),
        quantity ? [quantity, unit].filter(Boolean).join(" ") : ""].filter(Boolean).join(" | ");
}
function sfAllocationSalesCapacity(a) {
    if (sfAllocationIsSales() && a.sourceWarehouseProductId) return Math.max(0, Number(a.sourceWarehouseAvailable) + Number(a.weight));
    return Math.max(0, (Math.round(sfAllocationSalesWeight(a.factorItemWeightCount) * 100)
        - Math.round(Number(a.factorItemAllocated) * 100) + Math.round(a.weight * 100)) / 100);
}
function sfAllocationSkeleton(count = 2) {
    return `<div class="sf-item-list" role="status" aria-label="در حال دریافت اطلاعات">${Array.from({ length: count }, () =>
        `<div class="sf-skeleton" aria-hidden="true"><div class="sf-skeleton-line"></div><div class="sf-skeleton-line"></div><div class="sf-skeleton-line"></div></div>`)
                                                                                              .join("")}</div>`;
}
function sfAllocationEmpty(title, text, icon = "search") {
    return `<div class="sf-empty"><span class="sf-empty-symbol">${sfAllocationIcon(icon)}</span><strong>${sfAllocationEscape(title)}</strong><p>${sfAllocationEscape(text)}</p></div>`;
}

function ensureSupplyAllocationUI() {
    if (document.getElementById("sfAllocationsModal")?.classList.contains("sf-allocation-modal")) return;
    const old = document.getElementById("sfAllocationsModal");
    const token = old?.querySelector(`[name="__RequestVerificationToken"]`)?.value
        || document.querySelector(`[name="__RequestVerificationToken"]`)?.value || "";
    if (old) {
        bootstrap.Modal.getInstance(old)?.dispose();
        old.remove();
    }
    document.body.insertAdjacentHTML("beforeend", `
        <div id="sfAllocationsModal" class="modal fade sf-ui sf-allocation-modal" dir="rtl" tabindex="-1" role="dialog" aria-labelledby="sfAllocationsTitle" aria-hidden="true" data-bs-backdrop="static">
          <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable sf-dialog">
           <div class="modal-content sf-modal-content">
            <header class="modal-header sf-dialog-header"><div class="sf-header-title"><h2 id="sfAllocationsTitle">تخصیص آیتم‌های فاکتور</h2></div><button type="button" class="sf-icon-button" data-sf-close aria-label="بستن پنجره">${sfAllocationIcon("close")}</button></header>
            <div class="sf-workspace-top">
              <div class="sf-workspace-toolbar">
                <div class="sf-toolbar-row"><div class="sf-search">${sfAllocationIcon("search")}<input id="sfAllocationSearch" type="search" autocomplete="off" placeholder="جست‌وجوی محصول…" aria-label="جست‌وجو در آیتم‌های تامین"/><button class="sf-icon-button" type="button" data-sf-clear-search aria-label="پاک کردن جست‌وجو" hidden>${sfAllocationIcon("close")}</button></div>
                  <div class="sf-filter-group" aria-label="فیلتر وضعیت آیتم‌ها"><button type="button" class="sf-filter" data-sf-filter="all" aria-pressed="true">همه <span></span></button><button type="button" class="sf-filter" data-sf-filter="complete" aria-pressed="false">تکمیل‌شده <span></span></button><button type="button" class="sf-filter" data-sf-filter="partial" aria-pressed="false">نیمه‌تکمیل <span></span></button><button type="button" class="sf-filter" data-sf-filter="empty" aria-pressed="false">بدون تخصیص <span></span></button></div>
                </div>
                <nav id="sfAllocationPagination" class="sf-item-pagination" aria-label="پیمایش آیتم‌های تامین" hidden></nav>
              </div>
            </div>
            <div class="modal-body sf-workspace-scroll"><input type="hidden" name="__RequestVerificationToken" value="${sfAllocationEscape(token)}"/>
              <div id="sfAllocationFeedback" role="status" aria-live="polite" hidden></div><div id="sfAllocationItems" class="sf-item-list"></div>
              <div id="sfAllocationEmpty" hidden></div>
            </div>
            <footer class="modal-footer sf-dialog-footer"><div class="sf-footer-info"><div class="sf-color-legend" role="group" aria-label="راهنمای رنگ وضعیت تخصیص"><span><i class="sf-legend-dot sf-legend-complete" aria-hidden="true"></i>تکمیل‌شده</span><span><i class="sf-legend-dot sf-legend-partial" aria-hidden="true"></i>نیمه‌تکمیل</span><span><i class="sf-legend-dot sf-legend-empty" aria-hidden="true"></i>بدون تخصیص</span></div><span class="sf-footer-hint"><span id="sfAllocationVisibleCount"></span></span></div><button type="button" class="sf-button" data-sf-close>بستن</button></footer>
           </div>
          </div>
        </div>
        <div id="sfAllocationPickerModal" class="modal fade sf-ui sf-allocation-modal sf-picker-modal" dir="rtl" tabindex="-1" role="dialog" aria-labelledby="sfPickerTitle" aria-hidden="true" data-bs-backdrop="static">
          <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable sf-dialog">
           <div class="modal-content sf-modal-content">
            <header class="modal-header sf-dialog-header"><span class="sf-header-symbol">${sfAllocationIcon("search")}</span><div class="sf-header-title"><span class="sf-kicker">مقصد تخصیص</span><h2 id="sfPickerTitle">انتخاب آیتم فروش یا انبار</h2><p id="sfPickerContext" class="sf-context"></p></div><button class="sf-icon-button" type="button" data-sf-picker-close aria-label="بازگشت به تخصیص‌ها">${sfAllocationIcon("close")}</button></header>
            <div class="sf-picker-toolbar"><div class="sf-picker-toolbar-top">
              <div class="sf-picker-tabs" role="tablist" aria-label="نوع مقصد"><button class="sf-picker-tab" type="button" role="tab" id="sfPickerSalesTab" data-sf-tab="sales" aria-selected="true" aria-controls="sfPickerContent">فاکتورهای فروش</button><button class="sf-picker-tab" type="button" role="tab" id="sfPickerWarehousesTab" data-sf-tab="warehouses" tabindex="-1" aria-selected="false" aria-controls="sfPickerContent">انبارها</button></div>
              <div class="sf-picker-search"><div class="sf-search">${sfAllocationIcon("search")}<input id="sfPickerSearch" type="search" autocomplete="off" placeholder="مشتری، شماره فاکتور یا محصول…" aria-label="جست‌وجوی آیتم فاکتور فروش"/><button type="button" class="sf-icon-button" data-sf-picker-clear aria-label="پاک کردن جست‌وجو" hidden>${sfAllocationIcon("close")}</button></div><button type="button" class="sf-button" data-sf-picker-search>جست‌وجو</button></div>
            </div><div id="sfPickerInfo" class="sf-picker-info" role="status" aria-live="polite"></div></div>
            <div id="sfPickerContent" class="modal-body sf-picker-content" role="tabpanel" aria-labelledby="sfPickerSalesTab"></div>
            <footer class="modal-footer sf-dialog-footer"><div class="sf-picker-selection">${sfAllocationIcon("check")}<strong id="sfPickerSelection">هنوز مقصدی انتخاب نشده است</strong></div><div class="sf-picker-footer-actions"><button id="sfPickerCancel" type="button" class="sf-button sf-button-quiet">بازگشت</button><button id="sfPickerSubmit" type="button" class="sf-button sf-button-primary" disabled>تایید انتخاب ${sfAllocationIcon("check")}</button></div></footer>
           </div>
          </div>
        </div>`);
    const modal = document.getElementById("sfAllocationsModal");
    modal.addEventListener("hide.bs.modal", event => {
        if (sfAllocationSaving || sfAllocationChoosing) event.preventDefault();
    });
    modal.addEventListener("hidden.bs.modal", () => {
        if (!sfAllocationChoosing) {
            sfAllocationAbort?.abort();
            sfAllocationRequest++;
            sfAllocationOrigin?.focus();
        }
    });
    modal.addEventListener("toggle", event => {
        if (event.target.matches(".sf-item")) {
            const id = event.target.dataset.itemId;
            event.target.open ? sfAllocationOpen.add(id) : sfAllocationOpen.delete(id);
        }
    }, true);
    sfAllocationSelector = new ModalSelector({
        modal: "#sfAllocationPickerModal", title: "#sfPickerTitle",
        search: "#sfPickerSearch", container: "#sfPickerContent", submit: "#sfPickerSubmit", cancel: "#sfPickerCancel",
    });
    // The existing selector supplies selection semantics; all sizing is custom CSS.
    sfAllocationSelector.setModalSize = () => {
    };
    sfAllocationSelector.load = loadSupplyAllocationPicker;
    document.getElementById("sfPickerSearch").addEventListener("keyup", event => {
        if (event.key !== "Enter") return;
        event.stopImmediatePropagation();
        clearTimeout(sfPickerTimer);
        sfAllocationSelector.load(event.target.value.trim());
    }, true);
    document.getElementById("sfAllocationPickerModal").addEventListener("shown.bs.modal", () => {
        modal.removeAttribute("aria-modal");
        modal.setAttribute("aria-hidden", "true");
        if (sfPickerTab === "sales") document.getElementById("sfPickerSearch").focus();
    });
    document.getElementById("sfAllocationPickerModal").addEventListener("hidden.bs.modal", () => {
        sfPickerAbort?.abort();
        sfPickerRequest++;
        clearTimeout(sfPickerTimer);
        sfAllocationChoosing = false;
        if (modal.classList.contains("show")) {
            document.body.classList.add("modal-open");
            // Bootstrap activates only the top focus trap. Restore its parent after the child closes.
            modal.removeAttribute("aria-hidden");
            modal.setAttribute("aria-modal", "true");
            const parentFocusTrap = bootstrap.Modal.getInstance(modal)?._focustrap;
            parentFocusTrap?.deactivate();
            parentFocusTrap?.activate();
            const weightInput = sfPickerForm?.querySelector("[name=\"weight\"]:not(:disabled)");
            (weightInput || sfPickerForm?.querySelector(".sf-allocation-select"))?.focus();
        }
    });
}

let sfAllocationRefreshNeeded = false;
function sfAllocationFeedback(message, type = "success", retry = false) {
    const box = document.getElementById("sfAllocationFeedback");
    const text = sfAllocationText(message);
    if (typeof type === "boolean") type = type ? "error" : "success";
    if (text && type === "success") toastifySuccess(text);
    if (text && type === "error") toastifyError(text);
    if (!box) return;
    const persistent = !!text && (type === "loading" || retry);
    box.hidden = !persistent;
    if (!persistent) {
        box.replaceChildren();
        return;
    }
    box.className = `sf-feedback${type === "error" ? " sf-feedback-error" : " sf-feedback-loading"}`;
    box.setAttribute("role", type === "error" ? "alert" : "status");
    box.innerHTML = `${type === "loading" ? "<span class=\"sf-spinner\" aria-hidden=\"true\"></span>" : sfAllocationIcon("info")}<span class="sf-feedback-text">${sfAllocationEscape(text)}</span>${retry ? "<button type=\"button\" class=\"sf-button\" data-sf-retry>تلاش دوباره</button>" : ""}`;
}
function showSalesFactorAllocations(id) {
    return showSupplyAllocations(id, "sales");
}
async function showSupplyAllocations(id, perspective = "supply") {
    ensureSupplyAllocationUI();
    if (sfAllocationSaving || sfAllocationChoosing) return;
    sfAllocationPerspective = perspective;
    document.getElementById("sfAllocationPagination")
            .setAttribute("aria-label", sfAllocationIsSales() ? "پیمایش آیتم‌های فروش" : "پیمایش آیتم‌های تامین");
    document.getElementById("sfAllocationSearch")
            .setAttribute("aria-label", sfAllocationIsSales() ? "جست‌وجو در آیتم‌های فروش" : "جست‌وجو در آیتم‌های تامین");
    document.querySelector(`[data-sf-tab="sales"]`).textContent = sfAllocationIsSales() ? "فاکتورهای تامین" : "فاکتورهای فروش";
    document.querySelector(`[data-sf-tab="warehouses"]`).textContent = sfAllocationIsSales() ? "موجودی انبارها" : "انبارها";
    document.getElementById("sfPickerTitle").textContent = sfAllocationIsSales() ? "انتخاب آیتم تامین یا انبار" : "انتخاب آیتم فروش یا انبار";
    const pickerSearch = document.getElementById("sfPickerSearch");
    pickerSearch.placeholder = sfAllocationIsSales() ? "تامین‌کننده، شماره فاکتور یا محصول…" : "مشتری، شماره فاکتور یا محصول…";
    pickerSearch.setAttribute("aria-label", sfAllocationIsSales() ? "جست‌وجوی آیتم فاکتور تامین" : "جست‌وجوی آیتم فاکتور فروش");
    sfAllocationOrigin = document.activeElement;
    sfAllocationInvoiceId = id;
    sfAllocationState = null;
    sfAllocationFilter = "all";
    sfAllocationCurrentItemId = null;
    sfAllocationOpen.clear();
    sfAllocationDrafts.clear();
    sfAllocationRefreshNeeded = false;
    document.getElementById("sfAllocationSearch").value = "";
    document.getElementById("sfAllocationPagination").hidden = true;
    document.getElementById("sfAllocationEmpty").hidden = true;
    document.getElementById("sfAllocationItems").innerHTML = sfAllocationSkeleton(1);
    document.getElementById("sfAllocationsTitle").textContent = "تخصیص آیتم‌های فاکتور";
    sfAllocationFeedback("");
    ModalToggle("#sfAllocationsModal", "show");
    try {
        await loadSupplyAllocations(id);
    } catch (error) {
        if (error.code === "ERR_CANCELED") return;
        document.getElementById("sfAllocationItems").innerHTML = sfAllocationEmpty("اطلاعات دریافت نشد", "اتصال را بررسی کنید و دوباره تلاش کنید.", "info");
        sfAllocationFeedback("دریافت آیتم‌های فاکتور انجام نشد.", "error", true);
    }
}
async function loadSupplyAllocations(id) {
    sfAllocationAbort?.abort();
    sfAllocationAbort = new AbortController();
    const request = ++sfAllocationRequest;
    const { data } = await axios.get(sfAllocationIsSales() ? "/Supply/GetSalesFactorAllocations" : "/Supply/GetSupplyFactorAllocations", {
        params: { id },
        signal: sfAllocationAbort.signal,
    });
    if (request !== sfAllocationRequest) return;
    if (sfAllocationIsSales()) {
        // Adapt only the view model: both perspectives still edit the same persisted junction rows.
        data.allocations = data.allocations.map(a => ({
            ...a, supplyFactorItemId: a.factorItemId,
            factorItemId: a.supplyFactorItemId, customer: a.supplier,
            warehouseId: a.sourceWarehouseId || a.warehouseId, warehouseTitle: a.sourceWarehouseTitle || a.warehouseTitle,
            factorItemWeightCount: a.supplyItemWeightCount, factorItemUnitTxt: a.supplyItemUnitTxt,
            factorItemAllocated: a.supplyItemAllocated,
        }));
    }
    data.factor = sfAllocationDisplayRecord(data.factor);
    data.items = data.items.map(sfAllocationDisplayRecord);
    data.allocations = data.allocations.map(sfAllocationDisplayRecord);
    captureSupplyAllocationDrafts();
    sfAllocationState = data;
    sfAllocationRefreshNeeded = false;
    document.querySelectorAll("[data-supply-factor-id]").forEach(card => {
        if (sfAllocationIsSales() || card.dataset.supplyFactorId !== data.factor.id) return;
        card.classList.toggle("sf-supply-card-complete", data.isAllocationComplete);
        const badge = card.querySelector(".sf-supply-complete-badge");
        if (badge) badge.hidden = !data.isAllocationComplete;
    });
    document.getElementById("sfAllocationsTitle").textContent = `تخصیص فاکتور ${sfAllocationIsSales() ? "فروش" : "تامین"} ${data.factor.serialNumber}`;
    if (!sfAllocationOpen.size) {
        // Existing allocations should be discoverable as soon as the workspace opens.
        const allocatedItems = new Set(data.allocations.map(a => a.supplyFactorItemId));
        data.items.filter(i => allocatedItems.has(i.id)).forEach(i => sfAllocationOpen.add(i.id));
        if (!sfAllocationOpen.size) {
            const first = data.items.find(i => sfAllocationRemaining(i) > 0) || data.items[0];
            if (first) sfAllocationOpen.add(first.id);
        }
    }
    renderSupplyAllocations();
}
function sfAllocationCapacity(item, allocation = null) {
    if (!sfAllocationIsSales() && allocation?.sourceWarehouseProductId)
        return Math.max(0, Number(allocation.sourceWarehouseAvailable) + Number(allocation.weight));
    return Math.max(0, (Math.round(sfAllocationRemaining(item) * 100) + Math.round((allocation?.weight ?? 0) * 100)) / 100);
}
function updateSupplyAllocationLimit(form) {
    const item = sfAllocationState.items.find(i => i.id === form.closest("[data-item-id]").dataset.itemId);
    if (!item) return;
    const target = form.elements.factorItemId;
    const previous = sfAllocationState.allocations.find(a => a.id === form.dataset.allocationId);
    const supplyCapacity = sfAllocationCapacity(item, previous);
    const capacity = target.dataset.warehouseId && !sfAllocationIsSales() ? supplyCapacity : Math.min(supplyCapacity, sfAllocationSalesWeight(target.dataset.salesWeight));
    const weight = form.elements.weight;
    weight.max = capacity;
    weight.disabled = !target.value;
    weight.placeholder = target.value ? `حداکثر ${sfAllocationNumber(capacity)}` : "ابتدا مقصد را انتخاب کنید";
    form.querySelector(".sf-limit-note").textContent = target.value ? `حداکثر قابل تخصیص: ${sfAllocationNumber(capacity)} ${item.unitTxt ?? ""}` : "پس از انتخاب مقصد، سقف مجاز نمایش داده می‌شود.";
    form.querySelector(`[type="submit"]`).disabled = !target.value || capacity <= 0;
    form.querySelector(".sf-allocation-select").classList.toggle("sf-has-target", !!target.value);
}
function captureSupplyAllocationDrafts() {
    document.querySelectorAll("#sfAllocationItems .sf-allocation-form").forEach(form => {
        const key = `${form.closest("[data-item-id]").dataset.itemId}:${form.dataset.allocationId || "new"}`;
        if (form.dataset.saved) {
            sfAllocationDrafts.delete(key);
            return;
        }
        const target = form.elements.factorItemId;
        if (form.closest(".sf-allocation-edit-row")?.hidden) return;
        if (!target.value && !form.elements.weight.value) return;
        sfAllocationDrafts.set(key, {
            id: target.value,
            warehouse: target.dataset.warehouseId || "",
            capacity: target.dataset.salesWeight || "",
            sourceProduct: target.dataset.sourceWarehouseProductId || "",
            label: form.querySelector(".sf-target-label").textContent,
            weight: form.elements.weight.value,
            editing: !form.closest(".sf-allocation-edit-row")?.hidden,
        });
    });
}
function sfAllocationForm(item, allocation = null) {
    const escape = sfAllocationEscape;
    const suffix = allocation?.id || `new-${item.id}`;
    const target = sfAllocationIsSales() && allocation?.sourceWarehouseProductId
        ? allocation.sourceWarehouseProductId : allocation?.warehouseId || allocation?.factorItemId || "";
    return `<form class="sf-allocation-form${allocation ? " sf-allocation-edit-form" : ""}" ${allocation ? `data-allocation-id="${escape(allocation.id)}"` : ""} novalidate>
      <div class="sf-field"><label id="sfTargetLabel-${suffix}">${sfAllocationIsSales() ? "آیتم تامین / انبار" : "مقصد تخصیص"}</label>
        <button type="button" class="sf-target-button sf-allocation-select" aria-labelledby="sfTargetLabel-${suffix} sfTargetText-${suffix}"><span id="sfTargetText-${suffix}" class="sf-target-label">${allocation ? escape(sfAllocationTargetLabel(allocation)) : sfAllocationIsSales() ? "انتخاب آیتم تامین یا انبار" : "انتخاب آیتم فروش یا انبار"} </span>${sfAllocationIcon("plus")}</button>
        <input type="hidden" name="factorItemId" value="${escape(target)}" data-warehouse-id="${escape(allocation?.warehouseId)}" data-source-warehouse-product-id="${escape(allocation?.sourceWarehouseProductId)}" data-sales-weight="${allocation && (!allocation.warehouseId || sfAllocationIsSales()) ? sfAllocationSalesCapacity(allocation) : 0}"/>
      </div>
      <div class="sf-field"><label for="sfWeight-${suffix}">مقدار / وزن</label>
        <input id="sfWeight-${suffix}" class="sf-input" name="weight" type="number" min="0" step="1" value="${allocation?.weight ?? ""}" inputmode="decimal" required aria-describedby="sfLimit-${suffix}"/>
        <span id="sfLimit-${suffix}" class="sf-limit-note"></span>
      </div>
      <div class="sf-form-actions"><button type="submit" class="sf-button sf-button-primary">${sfAllocationIcon("check")}${allocation ? "ذخیره ویرایش" : "ثبت تخصیص"}</button>${allocation ? "<button type=\"button\" class=\"sf-button sf-button-quiet sf-allocation-cancel-edit\">انصراف</button>" : ""}</div>
      <p class="sf-form-error" role="alert" hidden></p>
    </form>`;
}
function renderSupplyAllocations() {
    const data = sfAllocationState;
    if (!data) return;
    document.getElementById("sfAllocationItems").innerHTML = data.items.map((item, index) => {
        const remaining = sfAllocationRemaining(item), done = remaining <= 0;
        const records = data.allocations.filter(a => a.supplyFactorItemId === item.id);
        const title = item.productSmallName || item.product || (sfAllocationIsSales() ? "آیتم فروش" : "آیتم تامین");
        const tone = done ? "green" : Number(item.allocated) > 0 ? "violet" : "gray";
        return `<details class="sf-item sf-item-tone-${tone}${done ? " sf-item-complete" : ""}" data-item-id="${sfAllocationEscape(item.id)}" data-complete="${done}" ${sfAllocationOpen.has(item.id) ? "open" : ""}>
          <summary class="sf-item-summary"><div class="sf-item-heading"><span class="sf-item-index">${sfAllocationNumber(index + 1)}</span><div class="sf-item-title"><h3>${sfAllocationEscape(title)}</h3>${item.product && item.product !== title ? `<p>${sfAllocationEscape(item.product)}</p>` : ""}</div><span class="sf-status ${done ? "sf-status-complete" : records.length ? "sf-status-recorded" : "sf-status-pending"}">${done ? sfAllocationIcon("check") + "تخصیص کامل" : records.length ? sfAllocationNumber(records.length) + " تخصیص ثبت‌شده" : "بدون تخصیص"}</span><span class="sf-item-toggle-hint"><span class="sf-item-expand-label">مشاهده جزئیات</span><span class="sf-item-collapse-label">بستن جزئیات</span><span class="sf-item-chevron">${sfAllocationIcon("chevron")}</span></span></div>
          <div class="sf-item-metrics">${[["مقدار کل", item.weightCount], ["تخصیص‌یافته", item.allocated], ["باقی‌مانده", remaining]].map(([label, value]) => `<div class="sf-item-metric"><span>${label}</span><strong>${sfAllocationNumber(value)}</strong><small class="sf-unit">${sfAllocationEscape(item.unitTxt)}</small></div>`)
                                                                                                                                     .join("")}</div>
          <progress max="${Math.max(Number(item.weightCount), .01)}" value="${Math.max(0, Number(item.allocated))}" aria-label="مقدار تخصیص‌یافته این آیتم"></progress></summary>
          <div class="sf-item-body">
          ${done ? `<div class="sf-completion-note">${sfAllocationIcon("check")}تمام مقدار این آیتم تخصیص داده شده است. تخصیص‌های قبلی قابل ویرایش‌اند.</div>`
            : `<section class="sf-new-allocation-panel" aria-label="افزودن تخصیص جدید">${sfAllocationForm(item)}</section>`}
          ${records.length ? `<section class="sf-records-panel" aria-labelledby="sfRecordsTitle-${sfAllocationEscape(item.id)}">
            <header class="sf-records-header"><span class="sf-records-symbol">${sfAllocationIcon("check")}</span>
              <div class="sf-records-title"><h4 id="sfRecordsTitle-${sfAllocationEscape(item.id)}">تخصیص‌های ثبت‌شده</h4><p>این مقدارها قبلاً به مقصدهای زیر اختصاص یافته‌اند.</p></div>
              <span class="sf-records-count">${sfAllocationNumber(records.length)} مورد</span>
            </header>
            ${renderSupplyAllocationRecords(item, records)}
          </section>` : "<div class=\"sf-inline-empty\">هنوز تخصیصی ثبت نشده است؛ آیتم یا انبار و مقدار را در فرم بالا انتخاب کنید.</div>"}
          </div></details>`;
    }).join("");
    document.querySelectorAll("#sfAllocationItems .sf-allocation-form").forEach(form => {
        const key = `${form.closest("[data-item-id]").dataset.itemId}:${form.dataset.allocationId || "new"}`;
        const draft = sfAllocationDrafts.get(key);
        if (draft) {
            const target = form.elements.factorItemId;
            target.value = draft.id;
            target.dataset.warehouseId = draft.warehouse;
            target.dataset.sourceWarehouseProductId = draft.sourceProduct || "";
            const latestSale = !draft.warehouse && data.allocations.find(a => a.factorItemId === draft.id);
            const ownAllocation = data.allocations.find(a => a.id === form.dataset.allocationId);
            const restored = ownAllocation?.factorItemId === draft.id || ownAllocation?.sourceWarehouseProductId === draft.id ? ownAllocation.weight : 0;
            const latestWarehouse = sfAllocationIsSales() && draft.sourceProduct
                ? data.allocations.find(a => a.sourceWarehouseProductId === draft.sourceProduct) : null;
            target.dataset.salesWeight = latestWarehouse ? Number(latestWarehouse.sourceWarehouseAvailable) + Number(restored) : latestSale ? Math.max(0, (Math.round(sfAllocationSalesWeight(latestSale.factorItemWeightCount) * 100)
                - Math.round(Number(latestSale.factorItemAllocated) * 100) + Math.round(restored * 100)) / 100) : draft.capacity;
            form.elements.weight.value = draft.weight;
            form.querySelector(".sf-target-label").textContent = draft.label;
            if (form.dataset.allocationId && draft.editing) form.closest(".sf-allocation-edit-row").hidden = false;
        }
        updateSupplyAllocationLimit(form);
    });
    filterSupplyAllocationItems();
}

function renderSupplyAllocationRecords(item, records) {
    const escape = sfAllocationEscape;
    return `<div class="sf-record-scroll"><table class="sf-table sf-record-table"><thead><tr><th>${sfAllocationIsSales() ? "آیتم تامین / انبار" : "مقصد"}</th><th>محصول / شرح</th><th>فاکتور</th><th>مقدار آیتم ${sfAllocationIsSales() ? "تامین" : "فروش"}</th><th>مقدار تخصیص</th><th>عملیات</th></tr></thead><tbody>
      ${records.map(a => `<tr class="sf-record-row" data-record-id="${escape(a.id)}">
        <td class="sf-record-destination" data-label="مقصد"><strong class="sf-cell-title">${escape(a.warehouseTitle || a.customer || "—")}</strong><span class="sf-destination-type">${sfAllocationIcon(a.warehouseId ? "warehouse" : "invoice")}${a.sourceWarehouseProductId ? `برداشت از ${escape(a.sourceWarehouseTitle || a.warehouseTitle || "انبار")}` : a.warehouseId ? "انتقال به انبار" : sfAllocationIsSales() ? "فاکتور تامین" : "فاکتور فروش"}</span></td>
        <td data-label="محصول"><span class="sf-cell-title">${escape(a.productSmallName || a.product)}</span><small class="sf-cell-description">${escape(a.product)}</small></td>
        <td data-label="شماره فاکتور"><bdi>${escape(a.serialNumber ?? "—")}</bdi></td>
        <td data-label="مقدار آیتم ${sfAllocationIsSales() ? "تامین" : "فروش"}">${a.warehouseId ? "—" : `${escape(a.factorItemWeightCount)} <span class="sf-unit">${escape(a.factorItemUnitTxt)}</span>`}</td>
        <td data-label="تخصیص‌یافته"><span class="sf-record-weight"><strong class="sf-record-amount">${sfAllocationNumber(a.weight)}</strong> <span class="sf-unit">${escape(item.unitTxt)}</span></span></td>
        <td><div class="sf-record-actions"><button type="button" class="sf-icon-button sf-allocation-edit" data-allocation-id="${escape(a.id)}" title="ویرایش تخصیص" aria-label="ویرایش تخصیص ${escape(a.warehouseTitle || a.customer)}">${sfAllocationIcon("edit")}</button><button type="button" class="sf-icon-button sf-action-danger sf-allocation-delete" data-allocation-id="${escape(a.id)}" title="حذف تخصیص" aria-label="حذف تخصیص ${escape(a.warehouseTitle || a.customer)}">${sfAllocationIcon("trash")}</button></div></td>
      </tr>
      <tr class="sf-allocation-edit-row" data-edit-allocation-id="${escape(a.id)}" hidden><td class="sf-editor-cell" colspan="6">${sfAllocationForm(item, a)}</td></tr>`)
               .join("")}
    </tbody></table></div>`;
}
function filterSupplyAllocationItems() {
    if (!sfAllocationState) return;
    const query = sfAllocationNormalize(document.getElementById("sfAllocationSearch").value);
    const items = sfAllocationState.items;
    const matching = items.filter(item => {
        const done = sfAllocationRemaining(item) <= 0;
        return sfAllocationNormalize(`${item.productSmallName} ${item.product}`).includes(query)
            && (sfAllocationFilter === "all"
                || (sfAllocationFilter === "complete" && done)
                || (sfAllocationFilter === "partial" && !done && Number(item.allocated) > 0)
                || (sfAllocationFilter === "empty" && !done && Number(item.allocated) <= 0));
    });
    if (!matching.some(item => item.id === sfAllocationCurrentItemId)) {
        sfAllocationCurrentItemId = matching[0]?.id ?? null;
    }
    document.querySelectorAll("#sfAllocationItems .sf-item").forEach(element => {
        element.hidden = element.dataset.itemId !== sfAllocationCurrentItemId;
        if (!element.hidden) {
            element.open = true;
            sfAllocationOpen.add(element.dataset.itemId);
        }
    });
    const counts = { all: items.length, complete: items.filter(i => sfAllocationRemaining(i) <= 0).length };
    counts.partial = items.filter(i => sfAllocationRemaining(i) > 0 && Number(i.allocated) > 0).length;
    counts.empty = counts.all - counts.complete - counts.partial;
    document.querySelectorAll("[data-sf-filter]").forEach(button => {
        button.setAttribute("aria-pressed", String(button.dataset.sfFilter === sfAllocationFilter));
        button.querySelector("span").textContent = sfAllocationNumber(counts[button.dataset.sfFilter]);
    });
    const current = matching.findIndex(item => item.id === sfAllocationCurrentItemId);
    const originalIndex = items.findIndex(item => item.id === sfAllocationCurrentItemId);
    const pagination = document.getElementById("sfAllocationPagination");
    pagination.hidden = !matching.length;
    pagination.innerHTML = matching.length ? `
      <div class="sf-page-counter" role="status" aria-live="polite">آیتم <strong>${sfAllocationNumber(originalIndex + 1)}</strong> از <strong>${sfAllocationNumber(items.length)}</strong></div>
      <div class="sf-page-numbers">${matching.map(item => {
        const index = items.indexOf(item), done = sfAllocationRemaining(item) <= 0;
        const tone = done ? "green" : Number(item.allocated) > 0 ? "violet" : "gray";
        const status = done ? "تکمیل‌شده" : Number(item.allocated) > 0 ? "نیمه‌تکمیل" : "بدون تخصیص";
        return `<button type="button" class="sf-page-number sf-page-${tone}" data-sf-page-id="${sfAllocationEscape(item.id)}" ${item.id === sfAllocationCurrentItemId ? "aria-current=\"page\"" : ""} aria-label="آیتم ${sfAllocationNumber(index + 1)}؛ ${sfAllocationEscape(item.productSmallName || item.product)}؛ ${status}" title="${status}">${sfAllocationNumber(index + 1)}</button>`;
    }).join("")}</div>
      <div class="sf-page-actions"><button type="button" class="sf-button" data-sf-page-step="-1" ${current === 0 ? "disabled" : ""}>قبلی</button><button type="button" class="sf-button" data-sf-page-step="1" ${current === matching.length - 1 ? "disabled" : ""}>بعدی</button></div>` : "";
    const empty = document.getElementById("sfAllocationEmpty");
    empty.hidden = matching.length > 0;
    empty.innerHTML = sfAllocationEmpty(
        counts.all ? "آیتمی با این فیلتر پیدا نشد" : "این فاکتور آیتمی ندارد",
        counts.all ? "عبارت جست‌وجو یا فیلتر وضعیت را تغییر دهید." : `ابتدا آیتم‌های فاکتور ${sfAllocationIsSales() ? "فروش" : "تامین"} را ثبت کنید.`, "search");
    document.querySelector("[data-sf-clear-search]").hidden = !query;
    document.getElementById("sfAllocationVisibleCount").textContent = matching.length === counts.all
        ? ""
        : `${sfAllocationNumber(matching.length)} از ${sfAllocationNumber(counts.all)} آیتم با جست‌وجو و فیلتر مطابقت دارد`;
}
function navigateSupplyAllocationItem(button) {
    if (sfAllocationSaving || sfAllocationChoosing) return;
    const pages = [...document.querySelectorAll("[data-sf-page-id]")];
    const current = pages.findIndex(page => page.dataset.sfPageId === sfAllocationCurrentItemId);
    const target = button.dataset.sfPageId || pages[current + Number(button.dataset.sfPageStep)]?.dataset.sfPageId;
    if (!target || !pages.some(page => page.dataset.sfPageId === target)) return;
    sfAllocationCurrentItemId = target;
    filterSupplyAllocationItems();
    document.querySelector("#sfAllocationsModal .sf-workspace-scroll").scrollTop = 0;
    // Navigation only hides other pages; unsaved values and open editors stay intact.
    const focusTarget = button.dataset.sfPageId
        ? [...document.querySelectorAll("[data-sf-page-id]")].find(page => page.dataset.sfPageId === target)
        : document.querySelector(`[data-sf-page-step="${button.dataset.sfPageStep}"]:not(:disabled)`)
        || document.querySelector("[data-sf-page-id][aria-current=\"page\"]");
    focusTarget?.focus({ preventScroll: true });
}

async function loadSupplyAllocationPicker(search = "") {
    const selector = sfAllocationSelector;
    if (!selector?.config) return;
    const request = ++sfPickerRequest;
    sfPickerAbort?.abort();
    sfPickerAbort = new AbortController();
    sfPickerLoading = true;
    selector.submitButton.disabled = true;
    selector.container.setAttribute("aria-busy", "true");
    selector.container.innerHTML = sfAllocationSkeleton(2);
    document.getElementById("sfPickerInfo").innerHTML = "<span class=\"sf-spinner\" aria-hidden=\"true\"></span><span>در حال جست‌وجوی مقصدها…</span>";
    try {
        const data = await selector.config.load({ search, selectedIds: [...selector.tempSelectedIds], signal: sfPickerAbort.signal });
        if (request !== sfPickerRequest || data === null) return;
        data.items = data.items.map(sfAllocationDisplayRecord);
        data.warehouseItems = (data.warehouseItems ?? []).map(sfAllocationDisplayRecord);
        data.warehouses = (data.warehouses ?? []).map(w => ({ ...w, title: sfAllocationText(w.title, "انبار") }));
        sfPickerData = data;
        const usedWarehouses = new Set(sfAllocationState.allocations.filter(a =>
            a.supplyFactorItemId === sfPickerForm.closest("[data-item-id]").dataset.itemId
            && a.id !== sfPickerForm.dataset.allocationId && a.warehouseId)
                                                        .map(a => sfAllocationIsSales() ? a.sourceWarehouseProductId : a.warehouseId));
        data.items.forEach(i => sfPickerTargets.set(i.id, {
            id: i.id, label: sfAllocationTargetLabel(i),
            capacity: Number(i.available), warehouse: false, blocked: Number(i.available) <= 0,
        }));
        if (sfAllocationIsSales()) data.warehouseItems.forEach(i => sfPickerTargets.set(i.id, {
            id: i.id, warehouseId: i.warehouseId,
            label: [i.warehouseTitle, sfAllocationTargetLabel({ ...i, warehouseId: null })].filter(Boolean).join(" | "),
            capacity: Number(i.available), warehouse: true, blocked: usedWarehouses.has(i.id) || Number(i.available) <= 0,
        }));
        else (data.warehouses ?? []).forEach(w => sfPickerTargets.set(w.id, {
            id: w.id, label: w.title,
            warehouse: true, blocked: usedWarehouses.has(w.id),
        }));
        renderSupplyAllocationPicker();
    } catch (error) {
        if (request !== sfPickerRequest || error.code === "ERR_CANCELED") return;
        sfPickerData = null;
        toastifyError("جست‌وجوی مقصدها انجام نشد؛ دوباره تلاش کنید.");
        selector.container.innerHTML = `<div class="sf-picker-error">${sfAllocationEmpty("جست‌وجو انجام نشد", "اتصال را بررسی کنید و دوباره تلاش کنید.", "info")}<button type="button" class="sf-button" data-sf-picker-search>تلاش دوباره</button></div>`;
        document.getElementById("sfPickerInfo").textContent = "دریافت مقصدهای تخصیص ناموفق بود.";
    } finally {
        if (request === sfPickerRequest) {
            sfPickerLoading = false;
            selector.container.setAttribute("aria-busy", "false");
            updateSupplyPickerSelection();
        }
    }
}
function renderSupplyAllocationPicker() {
    if (!sfPickerData) return;
    const selector = sfAllocationSelector, escape = sfAllocationEscape, data = sfPickerData;
    document.querySelectorAll("[data-sf-tab]").forEach(button => {
        const selected = button.dataset.sfTab === sfPickerTab;
        button.setAttribute("aria-selected", String(selected));
        button.tabIndex = selected ? 0 : -1;
    });
    selector.container.setAttribute("aria-labelledby", sfPickerTab === "sales" ? "sfPickerSalesTab" : "sfPickerWarehousesTab");
    document.querySelector(".sf-picker-search").hidden = sfPickerTab === "warehouses" && !sfAllocationIsSales();
    document.querySelector("[data-sf-picker-clear]").hidden = !document.getElementById("sfPickerSearch").value;
    const warehouseStock = sfAllocationIsSales() && sfPickerTab === "warehouses";
    const rows = warehouseStock ? data.warehouseItems : data.items;
    if (sfPickerTab === "warehouses" && !sfAllocationIsSales()) {
        selector.container.innerHTML = (data.warehouses ?? []).length ? `<div class="sf-warehouse-grid">${data.warehouses.map(w => {
            const target = sfPickerTargets.get(w.id);
            return `<div class="sf-warehouse-option sf-allocation-sale-row ${target.blocked ? "" : "sf-allocation-sale-selectable"}" ${target.blocked ? "" : "tabindex=\"0\""} aria-label="${escape(w.title)}" aria-disabled="${target.blocked}">
                <input class="sf-radio" type="radio" name="sfAllocationFactorItem" value="${escape(w.id)}" data-number="${escape(w.title)}" aria-label="${escape(w.title)}" ${target.blocked ? "disabled" : ""}/>
                <span class="sf-header-symbol">${sfAllocationIcon("warehouse")}</span><div class="sf-warehouse-copy"><h3>${escape(w.title)}</h3><p>تخصیص بدون اتصال به فاکتور فروش</p></div>
                <p class="sf-warehouse-note">${target.blocked ? "قبلاً برای این آیتم ثبت شده است؛ تخصیص قبلی را ویرایش کنید." : "مقدار انتخاب‌شده از انبار گردشی خارج و به این انبار وارد می‌شود."}</p></div>`;
        }).join("")}</div>` : sfAllocationEmpty("انبار مقصدی موجود نیست", "تعریف انبارهای ضایعات و ذخیره را بررسی کنید.", "warehouse");
        document.getElementById("sfPickerInfo").textContent = "مقدار انتقال به انبار از باقی‌مانده آیتم تامین کم می‌شود.";
    } else {
        selector.container.innerHTML = rows.length ? `<table class="sf-table sf-picker-table"><thead><tr><th>انتخاب</th><th>${warehouseStock ? "انبار / تامین‌کننده" : sfAllocationIsSales() ? "تامین‌کننده" : "مشتری"}</th><th>فاکتور / تاریخ</th><th>محصول / شرح</th><th>${warehouseStock ? "موجودی انبار" : "مقدار کل"}</th><th>${warehouseStock ? "برداشت فروش" : "تخصیص‌یافته"}</th><th>${warehouseStock ? "قابل برداشت" : "باقی‌مانده"}</th></tr></thead><tbody>
            ${rows.map(i => `<tr class="sf-allocation-sale-row ${i.remaining <= 0 ? "sf-allocation-sale-complete" : ""} ${i.available > 0 ? "sf-allocation-sale-selectable" : ""}" ${i.available > 0 ? "tabindex=\"0\"" : ""} aria-disabled="${i.available <= 0}" aria-label="${escape(`انتخاب ${sfAllocationTargetLabel(i)}`)}">
              <td class="sf-picker-radio-cell"><input class="sf-radio" type="radio" name="sfAllocationFactorItem" value="${escape(i.id)}" data-number="${escape(sfPickerTargets.get(i.id).label)}" aria-label="انتخاب ${escape(sfAllocationTargetLabel(i))}" ${i.available > 0 ? "" : "disabled"}/></td>
              <td class="sf-picker-customer"><strong class="sf-cell-title">${escape(warehouseStock ? i.warehouseTitle : i.customer || "—")}</strong>${warehouseStock ? `<small class="sf-cell-description">${escape(i.customer)}</small>` : ""}</td>
              <td class="sf-picker-factor"><bdi class="sf-cell-title">${escape(i.serialNumber)}</bdi><small class="sf-cell-description">${escape(i.date)}</small></td>
              <td class="sf-picker-product"><strong class="sf-cell-title">${escape(i.productSmallName || i.product)}</strong><small class="sf-cell-description">${escape(i.product)}</small></td>
              <td class="sf-picker-total" data-label="کل"><span class="sf-numeric">${escape(i.weightCount)}</span> <small class="sf-unit">${escape(i.unitTxt)}</small></td>
              <td class="sf-picker-allocated" data-label="تخصیص‌یافته">${sfAllocationNumber(i.allocated)} <small class="sf-unit">${escape(i.unitTxt)}</small></td>
              <td class="sf-picker-remaining" data-label="باقی‌مانده"><strong class="sf-available">${sfAllocationNumber(i.remaining)}</strong> <small class="sf-unit">${escape(i.unitTxt)}</small>${i.remaining <= 0 ? `<span class="sf-status sf-status-complete sf-sales-complete-badge">${warehouseStock ? "موجودی قابل برداشت ندارد" : "تخصیص کامل"}</span>` : ""}</td></tr>`)
                  .join("")}</tbody></table>`
            : sfAllocationEmpty(warehouseStock ? "موجودی مرتبط با تامین پیدا نشد" : `آیتم ${sfAllocationIsSales() ? "تامین" : "فروش"} پیدا نشد`, "شماره فاکتور، محصول یا نام طرف حساب را جست‌وجو کنید.");
        document.getElementById("sfPickerInfo").innerHTML = `<span>${sfAllocationNumber(rows.length)} نتیجه <span class="sf-muted">· انتخاب با کلیک روی ردیف</span></span><span class="sf-picker-result-note">${(warehouseStock ? data.warehouseHasMore : data.hasMore) ? "۲۰۰ نتیجه آخر؛ جست‌وجوی دقیق‌تر برای نتایج بیشتر" : warehouseStock ? "برداشت از موجودی انبار؛ ارتباط فروش با آیتم تامین همین موجودی ثبت می‌شود" : "باقی‌مانده بر اساس تمام تخصیص‌ها محاسبه می‌شود"}</span>`;
    }
    selector.restoreSelected();
    const invalid = selector.tempSelectedIds.some(id => sfPickerTargets.get(id)?.blocked);
    if (invalid) {
        selector.tempSelectedIds = [];
        selector.tempSelectedMap.clear();
    }
    updateSupplyPickerSelection();
}
function updateSupplyPickerSelection() {
    const id = sfAllocationSelector?.tempSelectedIds[0], target = sfPickerTargets.get(id);
    document.getElementById("sfPickerSelection").textContent = target?.label || "هنوز مقصدی انتخاب نشده است";
    sfAllocationSelector.submitButton.disabled = sfPickerLoading || !sfPickerData || !target || target.blocked;
}

async function selectSupplyAllocationFactor(form) {
    if (sfAllocationSaving || sfAllocationRefreshNeeded) return;
    sfPickerForm = form;
    sfPickerData = null;
    sfPickerTargets.clear();
    const field = form.elements.factorItemId;
    const item = sfAllocationState.items.find(i => i.id === form.closest("[data-item-id]").dataset.itemId);
    sfPickerTab = field.dataset.warehouseId ? "warehouses" : "sales";
    if (field.value) sfPickerTargets.set(field.value, {
        id: field.value,
        label: form.querySelector(".sf-target-label").textContent, warehouse: !!field.dataset.warehouseId, warehouseId: field.dataset.warehouseId,
        capacity: Number(field.dataset.salesWeight), blocked: false,
    });
    document.getElementById("sfPickerContext").textContent = `${item.productSmallName || item.product} · باقی‌مانده ${sfAllocationIsSales() ? "فروش" : "تامین"}: ${sfAllocationNumber(sfAllocationRemaining(item))} ${item.unitTxt ?? ""}`;
    sfAllocationChoosing = true;
    let firstLoad = true;
    try {
        await sfAllocationSelector.open({
            title: sfAllocationIsSales() ? "انتخاب آیتم تامین یا انبار" : "انتخاب مقصد تخصیص",
            selectedIds: field.value ? [field.value] : [],
            load: async ({ search, signal }) => {
                if (firstLoad) {
                    firstLoad = false;
                    return null;
                }
                const { data } = await axios.get(sfAllocationIsSales() ? "/Supply/SearchAllocationSupplyItems" : "/Supply/SearchAllocationFactorItems", {
                    params: {
                        search, ...(sfAllocationIsSales() ? { factorItemId: item.id } : { supplyFactorItemId: item.id }),
                        allocationId: form.dataset.allocationId,
                    }, signal,
                });
                return data;
            },
            onSelectionChange: updateSupplyPickerSelection,
            onSubmit: ({ ids }) => {
                const target = sfPickerTargets.get(ids[0]);
                if (!target || target.blocked || sfPickerLoading) return;
                field.value = target.id;
                field.dataset.warehouseId = target.warehouse ? target.warehouseId || target.id : "";
                if (sfAllocationIsSales()) field.dataset.sourceWarehouseProductId = target.warehouse ? target.id : "";
                field.dataset.salesWeight = target.capacity ?? 0;
                form.querySelector(".sf-target-label").textContent = target.label;
                clearSupplyAllocationFormError(form);
                updateSupplyAllocationLimit(form);
            },
        });
        await sfAllocationSelector.load("");
        if ((sfPickerTab === "sales" || sfAllocationIsSales()) && sfAllocationSelector.modal.classList.contains("show"))
            document.getElementById("sfPickerSearch").focus();
    } catch (error) {
        sfAllocationChoosing = false;
        ModalToggle("#sfAllocationPickerModal", "hide");
        sfAllocationFeedback("انتخابگر باز نشد؛ دوباره تلاش کنید.", "error");
    }
}
function clearSupplyAllocationFormError(form) {
    form.querySelector(".sf-form-error").hidden = true;
    form.querySelectorAll(".sf-field-error").forEach(el => el.classList.remove("sf-field-error"));
    form.querySelectorAll("[aria-invalid]").forEach(el => el.removeAttribute("aria-invalid"));
}
function supplyAllocationFormError(form, message, field = "weight", notify = true) {
    message = sfAllocationText(message, "مقدار واردشده معتبر نیست.");
    if (notify) toastifyError(message);
    const box = form.querySelector(".sf-form-error");
    box.textContent = message;
    box.hidden = false;
    const target = field === "target" ? form.querySelector(".sf-allocation-select") : form.elements.weight;
    target.closest(".sf-field").classList.add("sf-field-error");
    target.setAttribute("aria-invalid", "true");
    target.focus();
    requestAnimationFrame(() => {
        if (target.isConnected && !target.disabled) target.focus();
    });
}
async function submitSupplyAllocation(form) {
    if (sfAllocationSaving || sfAllocationRefreshNeeded) return;
    clearSupplyAllocationFormError(form);
    const itemId = form.closest("[data-item-id]").dataset.itemId;
    const item = sfAllocationState.items.find(i => i.id === itemId);
    const selectedId = form.elements.factorItemId.value;
    const warehouseId = form.elements.factorItemId.dataset.warehouseId || null;
    const factorItemId = warehouseId ? null : selectedId;
    const allocationId = form.dataset.allocationId;
    const weight = Number(form.elements.weight.value);
    if (!selectedId) {
        supplyAllocationFormError(form, sfAllocationIsSales() ? "ابتدا آیتم تامین یا انبار را انتخاب کنید." : "ابتدا آیتم فروش یا انبار مقصد را انتخاب کنید.", "target");
        return;
    }
    if (!Number.isFinite(weight) || weight <= 0 || Math.abs(weight * 100 - Math.round(weight * 100)) > .000001) {
        supplyAllocationFormError(form, "مقدار باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
        return;
    }
    if (sfAllocationState.allocations.some(a => a.supplyFactorItemId === itemId && a.id !== allocationId
        && (warehouseId ? sfAllocationIsSales() ? a.sourceWarehouseProductId === selectedId : a.warehouseId === warehouseId
            : a.factorItemId === factorItemId && (sfAllocationIsSales() ? !a.sourceWarehouseProductId
            : (a.sourceWarehouseProductId || null) === (form.elements.factorItemId.dataset.sourceWarehouseProductId || null))))) {
        supplyAllocationFormError(form, "این آیتم یا انبار قبلاً ثبت شده است؛ تخصیص قبلی را ویرایش کنید.", "target");
        return;
    }
    const previous = sfAllocationState.allocations.find(a => a.id === allocationId);
    if (weight > sfAllocationCapacity(item, previous)) {
        supplyAllocationFormError(form, `مقدار از باقی‌مانده آیتم ${sfAllocationIsSales() ? "فروش" : "تامین"} بیشتر است.`);
        return;
    }
    const salesCapacity = sfAllocationSalesWeight(form.elements.factorItemId.dataset.salesWeight);
    if ((!warehouseId || sfAllocationIsSales()) && (!salesCapacity || weight > salesCapacity)) {
        supplyAllocationFormError(form, sfAllocationIsSales() && warehouseId ? "مقدار از موجودی قابل برداشت این آیتم در انبار بیشتر است."
            : `مقدار از باقی‌مانده آیتم ${sfAllocationIsSales() ? "تامین" : "فروش"} در مجموع تمام تخصیص‌ها بیشتر است.`);
        return;
    }
    await persistSupplyAllocation({
        url: `/Supply/${allocationId ? "Update" : "Add"}${sfAllocationIsSales() ? "Sales" : "Supply"}FactorAllocation`,
        itemId,
        data: sfAllocationIsSales()
            ? {
                id: allocationId, factorItemId: itemId, supplyFactorItemId: warehouseId ? undefined : selectedId,
                sourceWarehouseProductId: warehouseId ? selectedId : null, warehouseId, weight,
            }
            : { id: allocationId, supplyFactorItemId: itemId, factorItemId, warehouseId, weight },
        form, trigger: form.querySelector("[type=\"submit\"]"),
        success: allocationId ? "تخصیص ویرایش شد؛ مقدارهای باقی‌مانده به‌روز شدند." : "تخصیص ثبت شد. می‌توانید مقصد دیگری انتخاب کنید.",
    });
}
async function persistSupplyAllocation({ url, data, itemId = data.supplyFactorItemId, form, trigger, success }) {
    if (sfAllocationSaving) return;
    sfAllocationSaving = true;
    const modal = document.getElementById("sfAllocationsModal");
    const controls = [...modal.querySelectorAll("button, input")];
    const disabled = controls.map(c => c.disabled);
    const scroll = modal.querySelector(".sf-workspace-scroll"), position = scroll.scrollTop;
    const triggerHtml = trigger.innerHTML;
    controls.forEach(c => c.disabled = true);
    modal.setAttribute("aria-busy", "true");
    trigger.innerHTML = `<span class="sf-spinner" aria-hidden="true"></span> در حال ثبت…`;
    sfAllocationFeedback(url.includes("Delete") ? "در حال حذف و اصلاح موجودی…" : "در حال ثبت تخصیص…", "loading");
    try {
        await axios.post(url, data, {
            headers: {
                RequestVerificationToken: modal.querySelector("[name=\"__RequestVerificationToken\"]").value,
            },
        });
        if (form) form.dataset.saved = "true";
        sfAllocationDrafts.delete(`${itemId}:${data.id || "new"}`);
        const deletedEditor = [...modal.querySelectorAll(".sf-allocation-edit-form")].find(f => f.dataset.allocationId === data.id);
        if (deletedEditor) deletedEditor.dataset.saved = "true";
        sfAllocationOpen.add(itemId);
        try {
            await loadSupplyAllocations(sfAllocationInvoiceId);
            scroll.scrollTop = position;
            sfAllocationFeedback(success);
            const card = [...modal.querySelectorAll(".sf-item")].find(el => el.dataset.itemId === itemId && !el.hidden);
            (card?.querySelector(".sf-allocation-form:not(.sf-allocation-edit-form) .sf-allocation-select") || card?.querySelector("summary"))?.focus();
        } catch (error) {
            sfAllocationRefreshNeeded = true;
            sfAllocationFeedback("تغییر ذخیره شد، اما دریافت وضعیت جدید انجام نشد. برای ادامه، وضعیت را دوباره دریافت کنید.", "error", true);
            document.getElementById("sfAllocationFeedback").scrollIntoView({ block: "nearest" });
        }
    } catch (error) {
        const responseMessage = sfAllocationText(error.response?.data);
        const message = typeof error.response?.data === "string" && responseMessage && !responseMessage.includes("<")
            ? responseMessage : "تغییر ذخیره نشد؛ اتصال را بررسی کنید و دوباره تلاش کنید.";
        if (form) supplyAllocationFormError(form, message, "weight", false);
        sfAllocationFeedback(message, "error");
    } finally {
        trigger.innerHTML = triggerHtml;
        controls.forEach((c, i) => c.disabled = sfAllocationRefreshNeeded && !!c.closest("#sfAllocationItems") ? true : disabled[i]);
        modal.removeAttribute("aria-busy");
        sfAllocationSaving = false;
    }
}
async function deleteSupplyAllocation(button) {
    if (sfAllocationSaving || sfAllocationRefreshNeeded) return;
    const section = button.closest("[data-item-id]");
    const id = button.dataset.allocationId;
    const allocation = sfAllocationState.allocations.find(a => a.id === id);
    if (!allocation) return;
    sfAllocationSaving = true;
    const parentModal = document.getElementById("sfAllocationsModal");
    const parentFocusTrap = bootstrap.Modal.getInstance(parentModal)?._focustrap;
    parentFocusTrap?.deactivate();
    let confirmed = false;
    try {
        confirmed = await confirmAlert({
            title: "حذف تخصیص؟",
            text: `تخصیص ${sfAllocationNumber(allocation.weight)} به ${sfAllocationText(allocation.warehouseTitle || allocation.customer, "این مقصد")} حذف شود؟ مقدار آن به باقی‌مانده آیتم ${sfAllocationIsSales() ? "فروش" : "تامین"} برمی‌گردد.`,
            icon: "warning", confirmText: "حذف تخصیص",
        });
    } catch {
        toastifyError("نمایش تایید حذف انجام نشد؛ دوباره تلاش کنید.");
    } finally {
        sfAllocationSaving = false;
        if (parentModal.classList.contains("show")) {
            parentFocusTrap?.activate();
            if (button.isConnected) button.focus();
        }
    }
    if (!confirmed) return;
    await persistSupplyAllocation({
        url: sfAllocationIsSales() ? "/Supply/DeleteSalesFactorAllocation" : "/Supply/DeleteSupplyFactorAllocation",
        itemId: section.dataset.itemId,
        data: sfAllocationIsSales() ? { id, factorItemId: section.dataset.itemId } : { id, supplyFactorItemId: section.dataset.itemId },
        trigger: button, success: "تخصیص حذف شد؛ باقی‌مانده و وضعیت فاکتور به‌روز شدند.",
    });
}
async function retrySupplyAllocations() {
    if (sfAllocationSaving) return;
    sfAllocationFeedback("در حال دریافت آخرین وضعیت…", "loading");
    try {
        await loadSupplyAllocations(sfAllocationInvoiceId);
        sfAllocationFeedback("");
    } catch (error) {
        if (error.code !== "ERR_CANCELED") sfAllocationFeedback("دریافت اطلاعات انجام نشد؛ دوباره تلاش کنید.", "error", true);
    }
}

document.addEventListener("input", event => {
    if (event.target.id === "sfAllocationSearch") filterSupplyAllocationItems();
    if (event.target.id === "sfPickerSearch") {
        clearTimeout(sfPickerTimer);
        document.querySelector("[data-sf-picker-clear]").hidden = !event.target.value;
        sfPickerTimer = setTimeout(() => sfAllocationSelector.load(event.target.value.trim()), 350);
    }
    const form = event.target.closest(".sf-allocation-form");
    if (form && event.target.name === "weight") clearSupplyAllocationFormError(form);
});
document.addEventListener("submit", event => {
    const form = event.target.closest(".sf-allocation-form");
    if (!form) return;
    event.preventDefault();
    submitSupplyAllocation(form);
});
document.addEventListener("click", event => {
    const open = event.target.closest("[data-sf-open]");
    if (open) {
        showSupplyAllocations(open.dataset.sfOpen);
        return;
    }
    if (event.target.closest("[data-sf-close]")) {
        ModalToggle("#sfAllocationsModal", "hide");
        return;
    }
    if (event.target.closest("[data-sf-picker-close]")) {
        sfAllocationSelector.cancel();
        return;
    }
    if (event.target.closest("[data-sf-retry]")) {
        retrySupplyAllocations();
        return;
    }
    if (event.target.closest("[data-sf-clear-search]")) {
        document.getElementById("sfAllocationSearch").value = "";
        filterSupplyAllocationItems();
        document.getElementById("sfAllocationSearch").focus();
        return;
    }
    const page = event.target.closest("[data-sf-page-id], [data-sf-page-step]");
    if (page) {
        navigateSupplyAllocationItem(page);
        return;
    }
    const filter = event.target.closest("[data-sf-filter]");
    if (filter) {
        sfAllocationFilter = filter.dataset.sfFilter;
        filterSupplyAllocationItems();
        return;
    }
    const tab = event.target.closest("[data-sf-tab]");
    if (tab) {
        sfPickerTab = tab.dataset.sfTab;
        document.querySelectorAll("[data-sf-tab]").forEach(b => b.setAttribute("aria-selected", String(b === tab)));
        renderSupplyAllocationPicker();
        return;
    }
    if (event.target.closest("[data-sf-picker-search]")) {
        clearTimeout(sfPickerTimer);
        sfAllocationSelector.load(document.getElementById("sfPickerSearch").value.trim());
        return;
    }
    if (event.target.closest("[data-sf-picker-clear]")) {
        document.getElementById("sfPickerSearch").value = "";
        clearTimeout(sfPickerTimer);
        sfAllocationSelector.load("");
        document.getElementById("sfPickerSearch").focus();
        return;
    }
    const row = event.target.closest(".sf-allocation-sale-row");
    if (row && !event.target.closest("input, button, a, label")) {
        const radio = row.querySelector("input[name=\"sfAllocationFactorItem\"]");
        if (radio && !radio.disabled) radio.click();
    }
    if (sfAllocationSaving || sfAllocationRefreshNeeded) return;
    const select = event.target.closest(".sf-allocation-select");
    if (select) {
        selectSupplyAllocationFactor(select.closest("form"));
        return;
    }
    const edit = event.target.closest(".sf-allocation-edit");
    if (edit) {
        const section = edit.closest("[data-item-id]");
        const editor = [...section.querySelectorAll(".sf-allocation-edit-row")].find(r => r.dataset.editAllocationId === edit.dataset.allocationId);
        editor.hidden = false;
        updateSupplyAllocationLimit(editor.querySelector("form"));
        editor.querySelector(`[name="weight"]`).focus();
        return;
    }
    const cancelEdit = event.target.closest(".sf-allocation-cancel-edit");
    if (cancelEdit) {
        const form = cancelEdit.closest("form");
        const allocation = sfAllocationState.allocations.find(a => a.id === form.dataset.allocationId);
        const target = form.elements.factorItemId;
        form.reset();
        target.value = allocation.warehouseId || allocation.factorItemId;
        target.dataset.warehouseId = allocation.warehouseId || "";
        target.dataset.salesWeight = allocation.warehouseId ? 0 : sfAllocationSalesCapacity(allocation);
        form.querySelector(".sf-target-label").textContent = sfAllocationTargetLabel(allocation);
        clearSupplyAllocationFormError(form);
        updateSupplyAllocationLimit(form);
        sfAllocationDrafts.delete(`${allocation.supplyFactorItemId}:${allocation.id}`);
        form.closest(".sf-allocation-edit-row").hidden = true;
        [...form.closest("[data-item-id]").querySelectorAll(".sf-allocation-edit")].find(b => b.dataset.allocationId === allocation.id)?.focus();
        return;
    }
    const remove = event.target.closest(".sf-allocation-delete");
    if (remove) deleteSupplyAllocation(remove);
});
document.addEventListener("keydown", event => {
    const row = event.target.closest(".sf-allocation-sale-row");
    if (row && event.target === row && ["Enter", " "].includes(event.key)) {
        event.preventDefault();
        const radio = row.querySelector(`input[name="sfAllocationFactorItem"]`);
        if (radio && !radio.disabled) radio.click();
    }
    const tab = event.target.closest("[data-sf-tab]");
    if (tab && ["ArrowLeft", "ArrowRight"].includes(event.key)) {
        event.preventDefault();
        const next = [...document.querySelectorAll("[data-sf-tab]")].find(b => b !== tab);
        next.click();
        next.focus();
    }
});
