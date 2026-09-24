var supplyFactorAttachments = [];
let orderHeadsSupFactorSelectedIds = [];
let supplyFactorData = { items: [], descriptions: [] };
let sfItemIndex = null;

const supplyFactorAiBtn = document.getElementById("supplyFactorAiBtn");
const supplyFactorDiscountInp = document.getElementById("sf-discount");

document.querySelector("#supplyFactorItem")?.addEventListener("input", (e) => {
    if (e.target.matches("[name='weightCount']") || e.target.matches("[name='unitAmount']") || e.target.matches("[name='discount']")) {
        calculateItem("supplyFactorItem");
    }
});

document.querySelectorAll("#supplyFactorModal input[type='radio'][name='officialType']")?.forEach(radio => {
    // const currencyTypeElm = document.querySelector("#supplyFactorHeadFormId select[name='currencyType']");
    radio.addEventListener("change", async (e) => {
        supplyFactorData.officialType = Number(e.target.value);
        // currencyTypeElm.toggleAttribute("disabled", supplyFactorData.officialType === 1);
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
    await setupSelectOptions();
    await getAllProductOp("supplyFactorItem");
    Unit(4, "supplyFactorItem");
}

function cancelSupplyFactorModal() {
    supplyFactorAttachments = [];
    let supplyFactorData = { items: [], descriptions: [] };
    
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
    
    
    // modalSelector.reset();
    // toggleElementsDisabled(document.getElementById("showAddOrderHeadBtn"), true);
    ModalToggle("#supplyFactorModal", "hide");
    clearUpload("supplyFactorAtt");
}


const addSfItem = async () => {
    document.getElementById("supplyFactorItemId").classList.remove("AlertReq");
    
    if (!await mySer.CheckValidForm("#supplyFactorItem")) return false;
    if (!validatePfItemDiscount("supplyFactorItem")) return false;
    
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

const showSupplyFactorItems = () => {
    document.getElementById("supplyFactorItemId").replaceChildren(
        generateInvoiceItemDom(supplyFactorData.items, { onDelete: deleteSfItem, onUpdate: showUpdateSfItem }),
    );
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

const showUpdateSfItem = (index) => {
    /*if (window.innerWidth < 992) {
        ModalToggle("ItemPreFactorModal");
    }*/
    sfItemIndex = index;
    
    mySer.convertJsonToForm(supplyFactorData.items[index], "supplyFactorItem");
    document.getElementById("sf-AddBtnId").style.display = "none";
    // document.getElementById("sf-CancelBtnId").style.display = "none";
    document.getElementById("sf-UpBtnId").style.display = "flex";
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
    
    document.getElementById("sf-AddBtnId").style.display = "flex";
    // document.getElementById("sf-CancelBtnId").style.display = "flex";
    document.getElementById("sf-UpBtnId").style.display = "none";
    
    /* if (window.innerWidth < 992) {
         canselModalItemPreFactor();
     }*/
};

supplyFactorDiscountInp?.addEventListener("input", () => {
    calculateSupplyFactorTotals(true);
    showSupplyFactorItems();
});


supplyFactorAiBtn?.addEventListener("click", proccessSupplyFactorWithAi);

async function proccessSupplyFactorWithAi() {
    const spinner = supplyFactorAiBtn.querySelector(".spinner-border");
    const aiIcon = supplyFactorAiBtn.querySelector(".ai-svg-icon");
    
    const elementsToDisable = [
        supplyFactorAiBtn,
        ...Array.from(document.getElementById("supplyFactorHeadFormId").elements),
        ...Array.from(document.getElementById("supplyFactorItem").elements),
        document.getElementById("cancelSupplyFactorBtn"),
        document.getElementById("submitSupplyFactorBtn"),
        supplyFactorDiscountInp,
    ];
    
    toggleElementsDisabled(elementsToDisable);
    spinner.classList.remove("d-none");
    aiIcon.classList.add("d-none");
    
    await delay(300);
    
    toggleElementsDisabled(elementsToDisable, false);
    spinner.classList.add("d-none");
    aiIcon.classList.remove("d-none");
    
    const sfData = {
        /*        "sumTotalAmount": 3815700000,
                "discount": 0,
                "totalTax": 381570000,
                "paymentAmount": 4197270000,*/
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
    };
    
    supplyFactorData = sfData;
    
    calculateSupplyFactorTotals();
    showSupplyFactorItems();
    mySer.convertJsonToForm(supplyFactorData, "supplyFactorHeadFormId");
    
    
    /*    if (supplyFactorAttachments.every(item => item.isDeleted)) {
            toastifyError("سند فاکتور باید بارگذاری شود");
            return;
        }
        
        await sendApiRequest({
            url: "/Supply/ProccessSupplyFactorWithAi/",
            method: "POST",
            data: supplyFactorAttachments[0],
            successMsg: null,
            onSuccess: (res) => {
                console.log(res);
            }
        })*/
}

///////////////////
async function showAddOrderHeadModal() {
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
    if (!await mySer.CheckValidForm("#supplyFactorHeadFormId")) return;
    
    const activeItems = supplyFactorData.items.filter(item => !item.isDeleted);
    if (!activeItems.length) {
        document.getElementById("supplyFactorItemId").classList.add("AlertReq");
        toastifyError("فاکتور خالی است");
        return;
    }
    
    if (supplyFactorAttachments.every(item => item.isDeleted)) {
        toastifyError("تصویر فاکتور اجباری است");
        return;
    }
    
    const supplierSelect = document.querySelector("#supplyFactorModal select[name='supplierId']");
    const model = {
        ...supplyFactorData,
        ...mySer.convertFormToJSON("supplyFactorHeadFormId"),
        supplierTxt: supplierSelect.options[supplierSelect.selectedIndex].text,
        attachments: supplyFactorAttachments,
    };
    console.log(model);
    
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


async function showEditSupplyFactorModal(id) {
    await sendApiRequest({
        url: "/Supply/GetSupplyFactor",
        params: { id },
        onSuccess: async (res) => {
            supplyFactorData = res.data.supplyFactor;
            await showSupplyFactorModal(supplyFactorData.dealId);
            
            mySer.convertJsonToForm(supplyFactorData, "supplyFactorForm");
            
            orderHeadsSupFactorSelectedIds = supplyFactorData.supplyFactorOrderHeads.map(o => o.orderHeadId);
            document.getElementById("supplyFactorOrderHead").value = res.data.orderSerials.join(", ");
            
            supplyFactorAttachments = res.data.attachments;
            FileUpReload("supplyFactorAtt");
        },
    });
}


const upPfactor = async (id) => {
    await initSupplyFactorModal();
    
    const data = await getPreFactor(id);
    
    if (!data) return;
    
    toggleDisplayPrefactorModal(data.paymentAmount === 0);
    
    pFactor = data;
    pFactorItems = data.preFactorItems;
    pFactorDescriptionItems = data.descriptions;
    dealData.id = data.dealId;
    dealData.serialNumber = data.dealSerialNumber;
    
    document.querySelectorAll("#PreFactor input[name='official']").forEach((i) => {
        i.checked = Number(i.value) === pFactor.officialType;
    });
    
    const exportValue = data.export ? "1" : "0";
    document.querySelectorAll("#PreFactor input[name='export']").forEach((i) => {
        i.checked = (i.value === exportValue);
    });
    
    const currencyTypeElm = document.querySelector("#PfHeadFormId select[name='currencyType']");
    currencyTypeElm.value = data.currencyType ?? 0;
    currencyTypeElm.toggleAttribute("disabled", pFactor.officialType === 1);
    
    document.getElementById("serialNumber").value = pFactor.serialNumber;
    document.getElementById("date").value = pFactor.date;
    document.getElementById("validityDade").value = pFactor.validityDade;
    
    document.getElementById("sumTotalAmount").textContent = pFactor.sumTotalAmount.toLocaleString();
    document.getElementById("discount").value = pFactor.discount.toLocaleString();
    document.getElementById("totalTax").textContent = pFactor.totalTax.toLocaleString();
    document.getElementById("paymentAmount").textContent = pFactor.paymentAmount.toLocaleString();
    
    showPFactorItems();
    showPfDescriptionItems();
    ModalToggle("#PreFactor");
};


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
    