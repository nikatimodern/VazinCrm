var supplyFactorAttachments = [];
let orderHeadsSupFactorSelectedIds = [];
let supplyFactorData = {};

async function showSupplyFactorModal(dealId = null) {
    supplyFactorData.dealId = dealId;
    
    document.getElementById("supplyFactorOrderHeadWrapper").classList.toggle("d-none", dealId !== null);
    toggleElementsRequired(document.getElementById("supplyFactorOrderHead"), dealId === null);
    
    await setupSelectOptions();
    ModalToggle("#supplyFactorModal");
}

function cancelSupplyFactorModal() {
    supplyFactorAttachments = [];
    supplyFactorData = {};
    
    orderHeadsSupFactorSelectedIds = [];
    modalSelector.reset();
    
    mySer.emptyForm("supplyFactorForm");
    
    toggleElementsDisabled(document.getElementById("showAddOrderHeadBtn"), true);
    ModalToggle("#supplyFactorModal", "hide");
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
    