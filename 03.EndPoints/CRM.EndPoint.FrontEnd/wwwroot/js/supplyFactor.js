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
    