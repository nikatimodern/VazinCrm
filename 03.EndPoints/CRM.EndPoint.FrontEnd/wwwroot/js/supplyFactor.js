var supplyFactorAttachments = [];
let orderHeadsSupFactorSelectedIds = [];
let supplyFactorData = {};
let supplyFactorItems = [];
let sfItemIndex = null;


document.querySelector("#supplyFactorItem")?.addEventListener("input", (e) => {
    if (e.target.matches("[name='weightCount']") || e.target.matches("[name='unitAmount']") || e.target.matches("[name='discount']")) {
        calculateItem("supplyFactorItem");
    }
});

async function showSupplyFactorModal(dealId = null) {
    supplyFactorData.dealId = dealId;
    
    document.getElementById("supplyFactorOrderHeadWrapper")?.classList.toggle("d-none", dealId !== null);
    
    /*toggleElementsRequired(document.getElementById("supplyFactorOrderHead"), dealId === null);
    await setupSelectOptions();*/
    
    await getAllProductOp("supplyFactorItem");
    Unit(4, "supplyFactorItem");
    ModalToggle("#supplyFactorModal");
}

function cancelSupplyFactorModal() {
    supplyFactorAttachments = [];
    supplyFactorData = {};
    
    orderHeadsSupFactorSelectedIds = [];
/*    modalSelector.reset();
    
    mySer.emptyForm("supplyFactorForm");
    
    toggleElementsDisabled(document.getElementById("showAddOrderHeadBtn"), true);*/
    ModalToggle("#supplyFactorModal", "hide");
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
    
    supplyFactorItems.push(data);
    supplyFactorData.Items = supplyFactorItems;
    
    calculateSupplyFactorTotals();
    showSupplyFactorItems();
    
    mySer.emptyForm("supplyFactorItem");
    document.getElementById("sf-printableF")?.focus();
    return true;
};

const calculateSupplyFactorTotals = (updateItemsFromDiscount = false) => {
    return calculateInvoiceTotals({
        items: supplyFactorItems,
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
        generateInvoiceItemDom(supplyFactorItems, { onDelete: deleteSfItem, onUpdate: showUpdateSfItem }),
    );
};


const deleteSfItem = (index) => {
    let item = supplyFactorItems[index];
    
    if (item.id) {
        item.isDeleted = true;
    } else {
        supplyFactorItems.splice(index, 1);
    }
    
    calculateSupplyFactorTotals();
    showSupplyFactorItems();
};

const showUpdateSfItem = (index) => {
    /*if (window.innerWidth < 992) {
        ModalToggle("ItemPreFactorModal");
    }*/
    sfItemIndex = index;
    
    mySer.convertJsonToForm(supplyFactorItems[index], "supplyFactorItem");
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
    
    
    supplyFactorItems[sfItemIndex] = {
        ...supplyFactorItems[sfItemIndex],
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
    const addOrderBtn = document.getElementById("showAddOrderHeadBtn");
    const data = (await axios("/Supply/GetSupplyBaseData")).data;
    new TomSelect("#supplyFactorSupplierId", {
        options: data.suppliers,
        placeholder: "انتخاب تامین کننده",
        maxItems: 1,
        selectOnTab: true,
        
        onItemAdd() {
            toggleElementsDisabled(addOrderBtn, false);
            document.getElementById("supplyFactorOrderHead").value = "";
            orderHeadsSupFactorSelectedIds = [];
        },
        
        onDelete() {
            toggleElementsDisabled(addOrderBtn, true);
        },
    });
    
    isSetupSupplyTomSelect = true;
}

async function submitSupplyFactorModal() {
    if (!await mySer.CheckValidForm("supplyFactorForm")) return;
    if (supplyFactorAttachments.every(item => item.isDeleted)) {
        toastifyError("تصویر فاکتور اجباری است");
        return;
    }
    
    const supplierSelect = document.querySelector("#supplyFactorModal #supplyFactorSupplierId");
    const model = {
        ...supplyFactorData,
        ...mySer.convertFormToJSON("supplyFactorForm"),
        supplierTxt: supplierSelect.options[supplierSelect.selectedIndex].text,
        supplyFactorOrderHeads: orderHeadsSupFactorSelectedIds.map(id => ({ orderHeadId: id, supplyFactorId: supplyFactorData.id })),
        attachments: supplyFactorAttachments,
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
    