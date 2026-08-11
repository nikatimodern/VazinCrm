let currentDealDocumentData = {};

//Show modals
const showModalDealPreFactors = async (deal) => {
    currentDealDocumentData = deal;
    const canAdd =  // TODO
        (deal.userCreatorID === currentUser.id || currentUser.rollNames.some(i => ["Admin", "SalseAdmin"].includes(i.name)))
        && deal.financialStatus !== 1;
    
    document.getElementById("dealPreFactorsAdd").classList.toggle("d-none", !canAdd);
    document.getElementById("dealPreFactorsTitle").textContent = `پیش‌فاکتورهای سفارش ${deal.serialNumber}`;
    
    await getPreFactorFromDeal(deal.id);
    ModalToggle("dealPreFactorsModal");
};

const showModalDealFactors = async (deal, sNumber) => {
    const factorsModalTitle = document.querySelector("#FactorsModal .modal-title");
    factorsModalTitle.textContent = sNumber ? "فاکتورهای سپیداری" : "فاکتورها";
    factorsModalTitle.style.backgroundColor = sNumber ? "#451377" : "#00302d";
    
    document.getElementById("factorsTitleModal").textContent = `${deal.customerName}${deal.customerCode ? ` (کد مشتری: ${deal.customerCode})` : ""}`;
    
    await getAllFactorsModal(deal.id, 4, sNumber);
    $("#FactorsModal").modal("toggle");
};

const showModalDealReports = async (deal) => {
    currentDealDocumentData = deal;
    const canAdd =   // TODO
        deal.userCreatorID === currentUser.id || currentUser.rollNames.some(i => ["Admin", "SalseAdmin"].includes(i.name));
    document.getElementById("dealReportsAdd").classList.toggle("d-none", !canAdd);
    
    document.getElementById("dealReportsTitle").textContent = `${deal.customerName}${deal.customerCode ? ` (کد مشتری: ${deal.customerCode})` : ""}`;
    await getReportFromDeal(deal.id);
    $("#dealReportsModal").modal("toggle");
};

const showModalDealOrders = async (deal) => {
    document.getElementById("dealOrdersTitle").textContent = `${deal.customerName}${deal.customerCode ? ` (کد مشتری: ${deal.customerCode})` : ""}`;
    
    await getOrdersFromDeal(deal.id);
    $("#dealOrdersModal").modal("toggle");
};

const showModalDealInvoices = async (deal) => {
    await getInvoicesFromDeal(deal.id);
};

//get
const getPreFactorFromDeal = async (dealId, isRefresh = false, showDeleted = false) => {
    if (!dealId) return;
    FLshowLoading();
    const dealPreFactorsElm = document.getElementById("dealPreFactors");
    if (isRefresh) dealPreFactorsElm.innerHTML = "";
    const res = await axios(`/Customer/GetAllPreFactorPartial?dealId=${dealId}&showDeleted=${showDeleted}`);
    
    if (res.status === 200) {
        if (res.data.count) {
            dealPreFactorsElm.innerHTML = res.data.html;
        } else {
            dealPreFactorsElm.innerHTML = `
                <div class="d-flex flex-column align-items-center pb-4">
                    <img src="/Img/no-data.jpg" style="width: clamp(120px, 20vw, 180px)" alt="">
                    <div style="color: #506a77;font-size: clamp(13px, 2.5vw, 15px)">هیچ پیش فاکتوری برای نمایش وجود ندارد!</div>
                </div>
            `;
            dealPreFactorsElm.style.background = "#fff";
        }
        FLshowLoading(false);
    }
};


const getReportFromDeal = async (dealId) => {
    FLshowLoading();
    const res = await axios.post(`/Report/GetAllReportPartial?dealId=${dealId}`, {});
    if (res.status === 200) {
        document.getElementById("dealReports").innerHTML = res.data.html;
        FLshowLoading(false);
    }
};

const getOrdersFromDeal = async (dealId) => {
    FLshowLoading();
    const res = await axios(`/Order/GetAllDealOrdersPartial?dealId=${dealId}`);
    if (res.status === 200) {
        for (const [key, data] of Object.entries(res.data)) {
            const containerElm = document.getElementById(`${key}Tab-pane`);
            const CountElm = document.getElementById(`${key}Count`);
            if (data.count) {
                containerElm.innerHTML = data.html;
                CountElm.style.display = "flex";
                CountElm.textContent = data.count;
            } else {
                CountElm.style.display = "none";
                containerElm.innerHTML = `
                    <div class="d-flex flex-column align-items-center justify-content-center h-100 pb-4">
                        <img src="/Img/no-data.jpg" style="width: clamp(120px, 20vw, 180px)" alt="">
                        <div style="color: #506a77;font-size: clamp(13px, 2.5vw, 15px)">هیچ حواله ای برای نمایش وجود ندارد!</div>
                    </div>
                `;
            }
        }
        
        FLshowLoading(false);
    }
};

const getInvoicesFromDeal = async (dealId) => {
    await ShowModalListInvoice(dealId, 3, null, 4, dealId, true);
};

document.getElementById("dealPreFactorsRefresh")?.addEventListener("click", () => getPreFactorFromDeal(currentDealDocumentData.id, true));
document.getElementById("dealPreFactorsAdd")?.addEventListener("click", () =>
    showModalAddPfactor(4, currentDealDocumentData.customerId, currentDealDocumentData));
document.getElementById("dealReportsAdd")?.addEventListener("click", () => {
    const model = currentDealDocumentData.customer ?? {
        agentName: currentDealDocumentData.customerName,
        mobile: currentDealDocumentData.mobileNumber,
    };
    ShowModalReport(model, 4, currentDealDocumentData.id, currentDealDocumentData.serialNumber);
});

const deletedPrefactorToggle = initEyeToggle({
    selector: ".showDeletedPrefactor-icon",
    onShow: async () => await getPreFactorFromDeal(currentDealDocumentData.id, false, true),
    onHide: async () => await getPreFactorFromDeal(currentDealDocumentData.id, false, false),
    showTitle: "نمایش حذف‌شده‌ها",
    hideTitle: "مخفی کردن حذف‌شده‌ها",
    titlePosition: "left",
});

document.getElementById("dealPreFactorsModal")?.addEventListener("hidden.bs.modal", () => {
    deletedPrefactorToggle.isVisible = false;
});
 
