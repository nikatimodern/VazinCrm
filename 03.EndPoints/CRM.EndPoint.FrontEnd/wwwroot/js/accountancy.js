let pfactorID = null;
let factorID = null;
let Invo_id = null;
let Invo_type = null;
let InvoiceType = null;
let invoicePassType = null;
let AccType = null;

let AccParentId = null;
let isEditInvoiceModal = false;
let invoiceItemId;

window.SanadPayList = [];

document.addEventListener("change", (e) => {
    if (e.target.matches("#InvoiceModalId input[type='radio'][name='payType']")) {
        const type = Number(e.target.value);
        if (type === 3) {
            document.getElementById("SanadParId")?.classList.remove("AlertReqRadio");
        }
        PayFormChange("InvoiceModalId", type);
    } else if (e.target.matches("#AttachInvoiceModalId input[type='radio'][name='payTypeAttach']")) {
        PayAttachFormChange(Number(e.target.value));
    }
});

const initPaymentPartial = (type) => {
    const isMobile = window.innerWidth < 767;
    $(`#allPaymentTable${type}`).DataTable({
        paging: false,
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        responsive: true,
        ordering: false,
        searching: false,
        language: languageDatatable,
        columnDefs: [
            {
                targets: "_all",
                render: data => typeof data === "string" ? (data.trim() || "-") : (data ?? "-"),
            },
        ],
        columns: type === 55 || type === 56
            ? [
                { responsivePriority: 4 },
                { responsivePriority: 9 },
                { responsivePriority: 1 },
                { responsivePriority: 3 },
                { responsivePriority: 5 },
                { responsivePriority: 2 },
                { responsivePriority: 7 },
                { responsivePriority: 8 },
                { responsivePriority: isMobile ? 9 : 1 },
                { responsivePriority: isMobile ? 10 : 1 },
            ] :
            [
                { responsivePriority: 7 },
                { responsivePriority: 6 },
                { responsivePriority: 3 },
                { responsivePriority: 4 },
                { responsivePriority: 5 },
                { responsivePriority: 8 },
                { responsivePriority: 2 },
                { responsivePriority: 1 },
            ],
        layout: null,
    });
};

const initWalletPartial = () => {
    $(`#allWalletTable`).DataTable({
        paging: false,
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        responsive: true,
        ordering: false,
        searching: false,
        language: languageDatatable,
        columns:
            [
                { responsivePriority: 6 },
                { responsivePriority: 7 },
                { responsivePriority: 3 },
                { responsivePriority: 2 },
                { responsivePriority: 4 },
                { responsivePriority: 8 },
                { responsivePriority: 1 },
                { responsivePriority: 5 },
            ],
        layout: null,
    });
};


const RefreshDataInvoice = async (id = Invo_id, type = Invo_type) => {

    if (!id) return;
    FLshowLoading();
    
    const [amountData, paymentData] = await Promise.all([
        getAllAmountPartial(id, type),
        getAllPaymentPartial(id, type, invoicePassType),
    ]);
    
    document.getElementById("PayContentId").innerHTML = paymentData.html;
    document.getElementById("PayContentSumId").innerHTML = amountData;
    
    initPaymentPartial(invoicePassType);
    
    if (paymentData.data?.customerId) {
        customerId = paymentData.data.customerId;
    }
    document.getElementById("changeStatusAllAccBtn")?.classList.toggle("d-none", !paymentData.data.showChangeStatusAllBtn);
    document.getElementById("acceptFinancialBtn")?.classList.toggle("d-none", !paymentData.data.showAcceptPfBtn);
    FLshowLoading(false);
};

const getAllAmountPartial = async (id, type) => { //type{1=prefactor 2=Factor 3=deal 4=customer}
    return await sendApiRequest({
        url: "/Accountancy/GetAllAmountPartial",
        params: { type, id },
        showLoading: false,
    });
};

const getAllPaymentPartial = async (id, type, passType, depositsOnly = false, realizedDepositsOnly = false, returnsOnly = false) => {
    return await sendApiRequest({
        url: "/Accountancy/GetAllPaymentPartial",
        params: { type, id, passType, depositsOnly, realizedDepositsOnly, returnsOnly },
        showLoading: false,
    });
};

const ShowModalCustomerListInvoice = async () => {
    await ShowModalListInvoice(customerId, 5, null, 1, false);
};

const ShowModalInvoiceWallet = async () => {
    await ShowModalListInvoice(customerId, 4, null, 1, true);
};


const cancelModalWallet = async () => {
    ModalToggle("#WalletListModalId", "hide");
    await ShowModalInvoice(1);
};


const CheckWallet = async () => {
    return await sendApiRequest({
        url: "/Accountancy/CheckWallet",
        params: { cusId: customerId },
        showLoading: false,
    });
};

const getAndShowWalletModal = async () => {
    await sendApiRequest({
        url: "/Accountancy/GetWalletPartialIfAny",
        params: { cusId: customerId },
        onSuccess: (res) => {
            document.getElementById("WalletContentId").innerHTML = res.data;
            initWalletPartial();
            
            ModalToggle("#WalletListModalId");
        },
    });
};

const addInvoiceWalletToPf = async (id) => {
    await sendApiRequest({
        url: "/Accountancy/AddInvoiceWalletToPf",
        params: { id, pfId: pfactorID },
        onSuccess: async () => {
            await refreshInvoiceSuccess();
            ModalToggle("#WalletListModalId", "hide");
        },
    });
};

let checkAccId;
const ShowVosolCheckInvoiceModal = async (itemId, isTransfered) => {
    suspendPaymentListModal();
    const acc = await getAccountancy(itemId);
    checkAccId = itemId;
    
    document.querySelector("#InvoiceVosolModalId input[name='reciveAmount']").value = acc.accData.credit.toLocaleString();
    document.querySelector("#InvoiceVosolModalId input[name='vosolDate']").value = acc.accData.vosolDate;
    
    const vosolModal = document.querySelector("#InvoiceVosolModalId");
    const transfer = acc.accData.accountancyTransfer;
    vosolModal.querySelector(".toBankAccountWrapperSelect").classList.toggle("d-none", isTransfered);
    vosolModal.querySelector(".transferDetailsWrapper").classList.toggle("d-none", !isTransfered);
    vosolModal.querySelector("input[name='transferBeneficiary']").value = transfer?.beneficiary ?? "";
    vosolModal.querySelector("input[name='transferToName']").value = transfer?.toName ?? "";
    toggleElementsRequired(vosolModal.querySelector("select[name='bankAccountId']"), !isTransfered);
    
    await getAllBankAccounts([{ element: "#InvoiceVosolModalId select[name='bankAccountId']", type: [1] }]);

    const bankAccountSelect = document.querySelector("#InvoiceVosolModalId select[name='bankAccountId']");
    if (!isTransfered && bankAccountSelect) {
        bankAccountSelect.value = acc.accData.bankAccountId ?? "";
        bankAccountSelect.dispatchEvent(new Event("change", { bubbles: true }));
    }
    
    ModalToggle("InvoiceVosolModalId");
};

async function submitVosolInvoice() {
    if (!await mySer.CheckValidForm("PayFactorVosolFormId")) return;
    const bankAccountSelect = document.querySelector("#InvoiceVosolModalId [name='bankAccountId']");
    const transferDetails = document.querySelector("#InvoiceVosolModalId .transferDetailsWrapper");
    if (transferDetails && !transferDetails.classList.contains("d-none")) {
        const transferResult = await sendApiRequest({
            url: "/Accountancy/AddAccountancyTransfer",
            method: "POST",
            data: {
                accountancyId: checkAccId,
                beneficiary: document.querySelector("#InvoiceVosolModalId [name='transferBeneficiary']").value,
                toName: document.querySelector("#InvoiceVosolModalId [name='transferToName']").value,
            },
            showLoading: false,
        });
        if (transferResult === null) return;
    }
    const model = {
        ...mySer.convertFormToJSON("PayFactorVosolFormId"),
        id: checkAccId,
        bankAccountTxt: bankAccountSelect?.selectedOptions[0]?.innerText ?? null,
    };
    
    model.bankAccountId = model.bankAccountId === "" ? null : model.bankAccountId;
    model.bankAccountTxt = model.bankAccountId === "" ? null : model.bankAccountTxt;
       
    
    await sendApiRequest({
        url: "/Factor/VosolCheckInvoice",
        method: "POST",
        data: model,
        onSuccess: async () => {
            await refreshInvoiceSuccess();
            closeModalVosolInvoice();
        },
    });
}

function closeModalVosolInvoice() {
    mySer.emptyForm("PayFactorVosolFormId");
    ModalToggle("InvoiceVosolModalId", "hide");
    restorePaymentListModal();
}


const ShowVosolEtebariInvoiceModal = async (parentId, pfId, payType) => {
    suspendPaymentListModal();
    AccParentId = parentId;
    InvoiceType = 1;
    pfactorID = pfId;
    
    if (payType === 2 || payType === 6) { // وصول چکی بود یا ویرایش انتقال بود گزینه انتقال نمایش داده میشود
        document.getElementById("TransferRadioWrapper")?.classList.remove("d-none");
    } else {
        document.getElementById("TransferRadioWrapper")?.classList.add("d-none");
    }
    
    document.getElementById("DivEtebariId")?.classList.add("d-none");
    
    await ShowModalInvoice(1);
};

const ShowVosolInvoiceModal = async (itemId, pfId, payType, isTransfered) => {
    if (payType === 2) { // چکی
        await ShowVosolCheckInvoiceModal(itemId, isTransfered);
    } else {
        await ShowVosolEtebariInvoiceModal(itemId, pfId, payType);
    }
};

const ShowEditVosolInvoiceModal = async (itemId) => {
    const result = await getAccountancy(itemId);
    const acc = result?.accData;
    if (!acc) return;
    if (acc.payType === 2) {
        await ShowVosolCheckInvoiceModal(itemId, Boolean(acc.accountancyTransfer));
    } else {
        await ShowVosolEtebariInvoiceModal(itemId, acc.pfid, acc.payType);
    }
};

/* --------------------- InvoiceTransfer --------------------- */

let invoiceTransferId;
let suspendedPaymentListModal = false;

function suspendPaymentListModal() {
    const modal = document.getElementById("customerDepositsModal");
    if (!modal || !modal.classList.contains("show")) return;
    suspendedPaymentListModal = true;
    ModalToggle(modal, "hide");
}

function restorePaymentListModal() {
    if (!suspendedPaymentListModal) return;
    suspendedPaymentListModal = false;
    ModalToggle("customerDepositsModal", "show");
}

async function ShowTransferInvoiceModal(itemId) {
    suspendPaymentListModal();
    invoiceTransferId = itemId;
    ModalToggle("InvoiceTransferModalId");
}
async function ShowEditTransferInvoiceModal(itemId) {
    suspendPaymentListModal();
    invoiceTransferId = itemId;
    const data = await getAccountancy(itemId);
    console.log(data.accData.accountancyTransfer);
    mySer.convertJsonToForm(data.accData.accountancyTransfer, "InvoiceTransferFormId");
    ModalToggle("InvoiceTransferModalId");
}


async function submitTransferInvoice() {
    if (!await mySer.CheckValidForm("InvoiceTransferFormId")) return;
    const model = {
        ...mySer.convertFormToJSON("InvoiceTransferFormId"),
        accountancyId: invoiceTransferId,
    };
    
    console.log(model);
    
    await sendApiRequest({
        url: "/Accountancy/AddAccountancyTransfer",
        method: "POST",
        data: model,
        onSuccess: async () => {
            await refreshInvoiceSuccess();
            closeModalTransferInvoice();
        },
    });
}

function closeModalTransferInvoice() {
    mySer.emptyForm("InvoiceTransferFormId");
    ModalToggle("InvoiceTransferModalId", "hide");
    restorePaymentListModal();
}


const ShowModalListInvoice = async (id, type, ser, passType, hideAddInvoiceBtns = false) => {
    invoicePassType = passType;
    
    Invo_id = id;
    Invo_type = type;
    
    await RefreshDataInvoice(id, type);
    if (type === 1) { //PreFactor
        pfactorID = id;
        InvoiceType = 1;
        document.getElementById("TitleListInvoiceId").innerText = "صورت‌حساب های پیش‌فاکتور " + ser;
    } else if (type === 2) { //Factor
        factorID = id;
        InvoiceType = 2;
        document.getElementById("TitleListInvoiceId").innerText = "واریزی های فاکتور شماره " + ser;
    } else if (type === 3) { //Deal
        InvoiceType = 1;
    } else if (type === 4) { //Wallet
        InvoiceType = 1;
        document.getElementById("TitleListInvoiceId").innerText = "کیف پول";
    } else if (type === 5) { //Customer
        InvoiceType = 1;
        document.getElementById("TitleListInvoiceId").innerText = "صورت‌حساب های مشتری";
    }
    
    document.getElementById("addInvoiceBtns")?.classList.toggle("d-none", hideAddInvoiceBtns); // نمایش یا عدم نمایش دکمه های واریزی و برگشتی
    /*if (!hideAddInvoiceBtns) {
        document.querySelector("#addInvoiceBtns .deposit-btn")?.classList.toggle("d-none", passType === 5); // کارتابل مالی دکمه واریزی نمایش داده نشود فقط برگشتی نمایش داده شود
    }*/
    
    ModalToggle("#InvoiceListModalId", "show");
};


const ShowModalAddInvoice = async () => {
    const hasWallet = await CheckWallet();
    
    if (hasWallet) {
        await getAndShowWalletModal();
        return;
    }
    
    await ShowModalInvoice(1);
};


const ShowModalInvoice = async (acType = 1) => {
    AccType = acType;
    await getAllBankAccounts([{ element: "#InvoiceModalId select[name='bankAccountId']", type: [1] }]);
    
    const invoiceModalElm = document.getElementById("InvoiceModalId");
    if (AccType === 1) {
        invoiceModalElm.querySelector(".modalHeader-invoice").innerText = InvoiceType === 1 ? "واریز پیش پرداخت" : "ثبت واریز";
        invoiceModalElm.classList.add("deposit-mode");
        invoiceModalElm.classList.remove("backward-mode");
    } else if (AccType === 2) {
        invoiceModalElm.querySelector(".modalHeader-invoice").innerText = "برگشت مبلغ";
        invoiceModalElm.classList.remove("deposit-mode");
        invoiceModalElm.classList.add("backward-mode");
        invoiceModalElm.querySelector("#DivEtebariId")?.classList.add("d-none");
    }
    
    document.querySelector("#InvoiceModalId input[name='reciveDate']").value = await todayDate();
    
    ModalToggle(invoiceModalElm);
};

async function getAccountancy(id, parent) {
    return await sendApiRequest({
        url: "/Accountancy/getAccountancy",
        params: { id, parent },
    });
}

const ShowEditModalInvoice = async (id) => {
    FLshowLoading();
    const result = await getAccountancy(id);
    const data = result.accData;
    
    isEditInvoiceModal = true;
    invoiceItemId = id;
    InvoiceType = data.invoiceType;
    
    if (InvoiceType === 1) {
        pfactorID = data.pfid;
    } else if (InvoiceType === 2) {
        factorID = data.fid;
    }
    
    if (data.parentId) {
        await ShowVosolInvoiceModal(data.parentId, data.pfid, data.payType);
    } else {
        await ShowModalInvoice(data.accType);
    }
    
    data.reciveAmount = data.accType === 2 ? data.backWard : data.credit;
    mySer.convertJsonToForm(data, "PayFactorFormId");
    
    PayFormChange("InvoiceModalId", data.payType);
    
    document.getElementById("DivShowSanadPar").innerHTML = "";
    window.SanadPayList = result.listAttach;
    FileUpReload("upSanadPay");
    FLshowLoading(false);
};


async function removeVosolInvoice(id) {
    await sendApiRequest({
        url: "/Accountancy/RemoveVosolInvoice",
        params: { id: id },
        onSuccess: () => {
            refreshInvoiceSuccess();
        },
    });
}
async function removeTransferInvoice(id) {
    await sendApiRequest({
        url: "/Accountancy/RemoveTransferInvoice",
        params: { id: id },
        onSuccess: () => {
            refreshInvoiceSuccess();
        },
    });
}

async function showPayment(id, parent = false) {
    const data = await getAccountancy(id, parent);
    if (!data) return;
    
    await getAllBankAccounts([{ element: "#ShowPaymentModal select[name='bankAccountId']", type: [1], isDisable: true }]);
    
    let accData = data.accData;
    
    const paymentModalElm = document.getElementById("ShowPaymentModal");
    if (accData.accType === 1) {
        paymentModalElm.classList.add("deposit-mode");
        paymentModalElm.classList.remove("backward-mode");
        paymentModalElm.querySelector(".modalHeader-invoice").innerText = "واریزی " + accData.payTypeTxt;
    } else if (accData.accType === 2) {
        paymentModalElm.classList.remove("deposit-mode");
        paymentModalElm.classList.add("backward-mode");
        paymentModalElm.querySelector(".modalHeader-invoice").innerText = "برگشتی " + accData.payTypeTxt;
    }
    
    PayFormChange("ShowPaymentModal", accData.payType);
    
    accData.reciveAmount = accData.accType === 2 ? accData.backWard : accData.credit;
    mySer.convertJsonToForm(accData, "ShowPaymentFormId");
    
    const attachList = data.listAttach;
    
    const containerUploader = paymentModalElm.querySelector(".sanadPayBox");
    const divShow = containerUploader.querySelector(".image-display");
    divShow.innerHTML = "";
    if (attachList.length) {
        containerUploader.classList.remove("d-none");
        attachList.forEach(fileItem => {
            const imageBox = createImageBox(fileItem);
            divShow.appendChild(imageBox);
        });
    } else {
        containerUploader.classList.add("d-none");
    }
    
    ModalToggle(paymentModalElm);
}

async function checkEtebariWarning() {
    if (isEditInvoiceModal || AccParentId || AccType !== 1) {
        return true;
    }
    
    const hasAnyEtebari = await sendApiRequest({
        url: "/Factor/HasAnyEtebari",
        params: { pfId: pfactorID },
    });
    
    if (hasAnyEtebari === true) {
        return await confirmAlert({
            title: "هشدار!",
            text: "برای این پیش‌فاکتور قبلا یک واریزی اعتباری ثبت شده است. اگر این واریزی مربوط به آن مورد اعتباری نیست، تایید کنید تا ثبت شود.",
            icon: "warning",
            confirmButtonColor: "#0d6efd",
            confirmText: "تایید",
        });
    }
    
    return true;
}


const submitInvoice = async () => {
    if (!await mySer.CheckValidForm("PayFactorFormId")) return;
    
    const accData = mySer.convertFormToJSON("PayFactorFormId");
    
    const isSanadRequired = document.getElementById("upSanadPay").required;
    if (isSanadRequired) {
        if (setting.attPfPayment && (accData.payType !== 3 && accData.payType !== 6) && window.SanadPayList.every(item => item.isDeleted)) {
            toastifyError("سند واریزی اجباریست");
            document.getElementById("SanadParId").classList.add("AlertReqRadio");
            return;
        }
    }
    
    if (Invo_type !== 5 && accData.payType !== 3 && !await checkEtebariWarning()) {
        return;
    }
    
    if (isEditInvoiceModal) accData.id = invoiceItemId;
    
    accData.invoiceType = InvoiceType ?? 1;
    accData.accType = AccType;
    accData.parentId = AccParentId;
    accData.customerId = customerId;
    
    if (Invo_type !== 5) { // مشتری نبود
        if (InvoiceType === 1) {
            accData.pfid = pfactorID;
        } else if (InvoiceType === 2) {
            accData.fid = factorID;
        }
    }
    
    if (accData.payType === 1) {
        accData.bankAccountTxt = document.querySelector("#PayFactorFormId [name='bankAccountId']").selectedOptions[0].innerText;
    } else if (accData.payType === 4) {
        accData.bankAccountId = null;
    } else {
        accData.bankAccountId = null;
        accData.bankAccountTxt = null;
    }
    
    if (accData.payType !== 4 && accData.payType !== 6) {
        accData.beneficiary = null;
        accData.toName = null;
    }
    
    
    await sendApiRequest({
        url: "/Factor/AddPayAttachment",
        method: "POST",
        data: { accData, listAttach: window.SanadPayList },
        successMsg: isEditInvoiceModal ? "با موفقیت ویرایش شد" : "با موفقیت ثبت شد",
        onSuccess: async () => {
            await refreshInvoiceSuccess();
            closeModalInvoice();
            
            const pfListModal = document.getElementById("preFactorListModal");
            if (isModalOpen(pfListModal)) {
                ModalToggle(pfListModal, "hide");
            }
        },
    });
};


async function refreshInvoiceSuccess() {
    await RefreshDataInvoice();

    if (typeof window.refreshSupplyDemandDeposits === "function") {
        await window.refreshSupplyDemandDeposits();
    }
    if (typeof window.refreshSupplyDemandTable === "function") {
        window.refreshSupplyDemandTable();
    }
    
    const currentPath = window.location.pathname.toLowerCase();
    if (currentPath.includes("customer")) { //1 پروفایل مشتری
        await getAllPreFactor();
        getAllDetailAmount();
        
    } else if (currentPath.includes("deal")) { //4 سفارشات
        await refreshDealPreFactors();
        
    } else if (currentPath.includes("check")) { //چک ها
        await getReceivedChecks();
        
    } else if (currentPath.includes("cartable")) { // کارتابل
        const { tab, sub } = getUrlParams();
        switch (tab) {
            case "TPrefactorFactorId": //2 پیش‌فاکتور و فاکتور
                await GetPreFactorPartial();
                break;
            case "TFinancialExpertId": //5 مالی
                switch (sub) {
                    case "FinancialPrefactorTab":
                        await GetPreFactorFinancialExpert();
                        break;
                    case "FinancialInvoiceTab":
                        await GetInvoicesFinancialExpert();
                        break;
                }
                break;
            case "TTaminExpertId": //5 تامین
                await GetInvoicesTaminExpert();
                break;
            case "TSalesExpertId": //3 کارشناس فروش
                await GetSelesExpertPartial();
                break;
        }
    }
}


const resetInvoiceModal = () => {
    const invoiceModalId = document.getElementById("InvoiceModalId");
    if (!invoiceModalId) return;
    AccParentId = null;
    isEditInvoiceModal = false;
    invoiceItemId = null;
    invoiceModalId.classList.remove("deposit-mode", "backward-mode");
    invoiceModalId.querySelector(".modalHeader-invoice").textContent = "";
    mySer.emptyForm("PayFactorFormId");
    PayFormChange("InvoiceModalId", 1);
    window.SanadPayList = [];
    invoiceModalId.querySelector("#SanadParId").classList.remove("AlertReqRadio");
    invoiceModalId.querySelector("#DivEtebariId")?.classList.remove("d-none");
    invoiceModalId.querySelector("#TransferRadioWrapper")?.classList.add("d-none");
    toggleElementsRequired(document.getElementById("upSanadPay"), true);
    invoiceModalId.querySelector("#DivShowSanadPar").innerHTML = "";
    const upSanadPay = invoiceModalId.querySelector("#upSanadPay");
    if (upSanadPay) upSanadPay.value = "";
};

const closeModalInvoice = () => {
    ModalToggle("InvoiceModalId", "hide");
    restorePaymentListModal();
};

document.addEventListener("hidden.bs.modal", (e) => {
    if (e.target.id === "InvoiceModalId") resetInvoiceModal();
});


let accType_pfList;
async function showModalPreFactorList(accType) {
    const preFactorListModal = document.getElementById("preFactorListModal");
    if (!preFactorListModal) return;
    accType_pfList = accType;
    await getAllPreFactorList();
    if (!preFactorListModal.isConnected) return;
    preFactorListModal.querySelector("#preFactorListSearch").value = "";
    ModalToggle(preFactorListModal);
}

const getAllPreFactorList = async (search = "") => {
    await sendApiRequest({
        url: "/Factor/GetAllPreFactorList",
        params: { search },
        onSuccess: (res) => {
            const container = document.querySelector("#preFactorListModal #preFactorListContainer");
            if (container) container.innerHTML = res.data;
        },
    });
};

document.addEventListener("keyup", async (e) => {
    if (e.key !== "Enter" || !e.target.matches("#preFactorListModal #preFactorListSearch") || e.target.disabled) return;
    const searchInput = e.target;
    toggleElementsDisabled(searchInput);
    try {
        await getAllPreFactorList(searchInput.value.trim());
    } finally {
        if (searchInput.isConnected) {
            toggleElementsDisabled(searchInput, false);
            searchInput.focus();
        }
    }
});


async function selectPfFromPfListModal(pfId) {
    InvoiceType = 1;
    pfactorID = pfId;
    accType_pfList === 1 ? await ShowModalAddInvoice() : await ShowModalInvoice(accType_pfList);
}


async function changeStatusAllAcc() {
    await sendApiRequest({
        url: "/Accountancy/ChangeStatusAllAcc",
        params: { pfId: pfactorID },
        successMsg: "به مالی ارسال شد.",
        onSuccess: async () => {
            await refreshInvoiceSuccess();
            ModalToggle("#InvoiceListModalId");
        },
    });
}

async function changeStatusAcc(id) {
    await sendApiRequest({
        url: "/Accountancy/ChangeStatusAcc",
        params: { id },
        successMsg: "به مالی ارسال شد.",
        onSuccess: refreshInvoiceSuccess,
    });
}

async function acceptFinancialFromModal() {
    await acceptFinancial(pfactorID);
    ModalToggle("#InvoiceListModalId");
}


async function acceptFinancial(pfId) {
    await sendApiRequest({
        url: `/Factor/AcceptFinancial`,
        params: { id: pfId },
        onSuccess: async () => {
            await GetPreFactorFinancialExpert();
            showAlert({ icon: "success", title: "پیش‌فاکتور تایید شد." });
        },
    });
}

async function acceptAccountancy(id, status, description = null) {
    const result = await sendApiRequest({
        url: `/Accountancy/AcceptAccountancy`,
        method: "POST",
        data: { id, description },
        params: { status },
        successMsg: status === 2 ? "تایید شد" : status === 3 ? "رد شد" : null,
        onSuccess: refreshInvoiceSuccess,
    });
    
    return result !== null;
}


/*const PayFormChange = (modalId, type) => {
    const modal = document.getElementById(modalId);
    const selectedType = Number(type);
    
    const elements = {
        transactionTitle: modal.querySelector(".transactionBankBox .sectionTitle-invoice"),
        
        vosolDateDiv: modal.querySelector(".VosolDateDiv"),
        vosolLabel: modal.querySelector(".VosolDateDiv label"),
        vosolInput: modal.querySelector(".VosolDateDiv input"),
        
        amountWrapper: modal.querySelector(".transactionAmountWrapper"),
        amountInput: modal.querySelector("input[name='reciveAmount']"),
        
        receiveDateDiv: modal.querySelector(".ReceiveDateDiv"),
        receiveDateInput: modal.querySelector("input[name='reciveDate']"),
        
        beneficiaryWrapper: modal.querySelector(".beneficiaryWrapper"),
        beneficiaryInput: modal.querySelector(".beneficiaryWrapper input"),
        
        toBankSelectWrapper: modal.querySelector(".toBankAccountWrapperSelect"),
        toBankSelect: modal.querySelector(".toBankAccountWrapperSelect select"),
        toBankTxtWrapper: modal.querySelector(".toBankAccountWrapperTxt"),
        toBankTxtInput: modal.querySelector(".toBankAccountWrapperTxt input"),
        
        toNameWrapper: modal.querySelector(".toNameWrapper"),
        toNameLabel: modal.querySelector(".toNameWrapper label"),
        toNameInput: modal.querySelector(".toNameWrapper input"),
        
        sanadPayBox: modal.querySelector(".sanadPayBox"),
        uploaderSpan: modal.querySelector(".sanadPayBox .container-uploader span"),
    };
    
    const config = {
        1: { // نقدی
            transactionBankTitle: "اطلاعات تراکنش بانکی",
            vosolText: "تاریخ پرداخت",
            vosolDateVisible: true,
            amountVisible: true,
            receiveVisible: false,
            beneficiaryVisible: false,
            
            toSelectVisible: true,
            toTxtVisible: false,
            toNameVisible: false,
            
            sanadPayVisible: true,
            uploaderText: "تصویر رسید را انتخاب کنید یا اینجا رها کنید",
        },
        2: { // چکی
            transactionBankTitle: "اطلاعات تراکنش بانکی",
            vosolText: "تاریخ وصول",
            vosolDateVisible: true,
            amountVisible: true,
            receiveVisible: true,
            beneficiaryVisible: false,
            
            toSelectVisible: false,
            toTxtVisible: false,
            toNameVisible: false,
            
            sanadPayVisible: true,
            uploaderText: "تصویر چک را انتخاب کنید یا اینجا رها کنید",
        },
        3: { // اعتباری
            transactionBankTitle: "اطلاعات تراکنش بانکی",
            vosolText: "تاریخ وصول",
            vosolDateVisible: true,
            amountVisible: true,
            receiveVisible: false,
            beneficiaryVisible: false,
            
            toSelectVisible: false,
            toTxtVisible: false,
            toNameVisible: false,
            
            sanadPayVisible: true,
            uploaderText: "تصویر رسید را انتخاب کنید یا اینجا رها کنید",
        },
        4: { // تهاتری
            transactionBankTitle: "اطلاعات تراکنش بانکی",
            vosolText: "تاریخ پرداخت",
            vosolDateVisible: true,
            amountVisible: true,
            receiveVisible: false,
            beneficiaryVisible: true,
            
            toSelectVisible: false,
            toTxtVisible: true,
            toNameVisible: true,
            toNameLabelVisible: true,
            toNamePlaceholder: "نام صاحب حساب واریز شده",
            
            sanadPayVisible: true,
            uploaderText: "تصویر رسید را انتخاب کنید یا اینجا رها کنید",
        },
        6: { // انتقال
            transactionBankTitle: "انتقال به",
            vosolDateVisible: false,
            amountVisible: false,
            receiveVisible: false,
            beneficiaryVisible: true,
            
            toSelectVisible: false,
            toTxtVisible: false,
            toNameVisible: true,
            toNameLabelVisible: false,
            toNamePlaceholder: "انتقال به",
            
            sanadPayVisible: false,
        },
    };
    
    const current = config[selectedType];
    if (!current) return;
    
    elements.transactionTitle.textContent = current.transactionBankTitle || "";
    
    elements.vosolDateDiv.classList.toggle("d-none", !current.vosolDateVisible);
    elements.vosolInput.toggleAttribute("required", current.vosolDateVisible);
    elements.vosolLabel.textContent = current.vosolText || "";
    elements.vosolInput.placeholder = current.vosolText || "";
    
    elements.amountWrapper.classList.toggle("d-none", !current.amountVisible);
    elements.amountInput.toggleAttribute("required", current.amountVisible);
    
    elements.receiveDateDiv.classList.toggle("d-none", !current.receiveVisible);
    elements.receiveDateInput.toggleAttribute("required", current.receiveVisible);
    
    elements.beneficiaryWrapper.classList.toggle("d-none", !current.beneficiaryVisible);
    elements.beneficiaryInput.toggleAttribute("required", current.beneficiaryVisible);
    
    elements.toBankSelectWrapper.classList.toggle("d-none", !current.toSelectVisible);
    elements.toBankSelect.toggleAttribute("required", current.toSelectVisible);
    
    elements.toBankTxtWrapper.classList.toggle("d-none", !current.toTxtVisible);
    elements.toBankTxtInput.toggleAttribute("required", current.toTxtVisible);
    
    elements.toNameWrapper.classList.toggle("d-none", !current.toNameVisible);
    elements.toNameLabel.classList.toggle("d-none", !current.toNameLabelVisible);
    elements.toNameInput.toggleAttribute("required", current.toNameVisible);
    elements.toNameInput.placeholder = current.toNamePlaceholder || "";
    
    elements.sanadPayBox.classList.toggle("d-none", !current.sanadPayVisible);
    
    if (elements.uploaderSpan)
        elements.uploaderSpan.textContent = current.uploaderText;
};*/


function PayFormChange(modalId, type) {
    const modal = document.getElementById(modalId);
    if (!modal) return;
    
    // تنظیمات نمایش فیلدها برای هر نوع پرداخت
    // گزینه‌هایی که تعریف نشده‌اند، به‌صورت پیش‌فرض false در نظر گرفته می‌شوند.
    const config = {
        1: { // نقدی
            transactionTitle: "اطلاعات تراکنش بانکی",
            vosolDateText: "تاریخ پرداخت",
            uploaderText: "تصویر رسید را انتخاب کنید یا اینجا رها کنید",
            showVosolDate: true,
            showAmount: true,
            showToBankSelect: true,
            showSanadPay: true,
            showInvoiceTypes: true,
        },
        2: { // چکی
            transactionTitle: "اطلاعات تراکنش بانکی",
            vosolDateText: "تاریخ وصول",
            uploaderText: "تصویر چک را انتخاب کنید یا اینجا رها کنید",
            showVosolDate: true,
            showAmount: true,
            showReceiveDate: true,
            showSanadPay: true,
            showInvoiceTypes: true,
        },
        3: { // اعتباری
            transactionTitle: "اطلاعات تراکنش بانکی",
            vosolDateText: "تاریخ وصول",
            uploaderText: "تصویر رسید را انتخاب کنید یا اینجا رها کنید",
            showVosolDate: true,
            showAmount: true,
            showSanadPay: true,
            showInvoiceTypes: true,
        },
        4: { // تهاتری
            transactionTitle: "اطلاعات تراکنش بانکی",
            vosolDateText: "تاریخ پرداخت",
            uploaderText: "تصویر رسید را انتخاب کنید یا اینجا رها کنید",
            toNamePlaceholder: "نام صاحب حساب واریز شده",
            showVosolDate: true,
            showAmount: true,
            showBeneficiary: true,
            showToBankText: true,
            showToName: true,
            showToNameLabel: true,
            showSanadPay: true,
            showInvoiceTypes: true,
        },
        6: { // انتقال
            transactionTitle: "انتقال به",
            toNamePlaceholder: "انتقال به",
            showBeneficiary: true,
            showToName: true,
            showInvoiceTypes: true,
        },
    };
    
    const current = config[Number(type)];
    if (!current) return;
    
    // نمایش یا مخفی کردن فیلد و تنظیم اجباری بودن آن
    const toggleField = (wrapperSelector, inputSelector, visible) => {
        const wrapper = modal.querySelector(wrapperSelector);
        wrapper?.classList.toggle("d-none", !visible);
        
        if (inputSelector) {
            const input = modal.querySelector(inputSelector);
            input.toggleAttribute("required", !!visible);
        }
    };
    
    // نمایش فیلدها بر اساس نوع پرداخت انتخاب‌شده
    toggleField(".VosolDateDiv", ".VosolDateDiv input", current.showVosolDate);
    toggleField(".transactionAmountWrapper", "input[name='reciveAmount']", current.showAmount);
    toggleField(".ReceiveDateDiv", "input[name='reciveDate']", current.showReceiveDate);
    toggleField(".beneficiaryWrapper", ".beneficiaryWrapper input", current.showBeneficiary);
    toggleField(".toBankAccountWrapperSelect", ".toBankAccountWrapperSelect select", current.showToBankSelect);
    toggleField(".toBankAccountWrapperTxt", ".toBankAccountWrapperTxt input", current.showToBankText);
    toggleField(".toNameWrapper", ".toNameWrapper input", current.showToName);
    toggleField(".sanadPayBox", null, current.showSanadPay);
    toggleField("#invoiceTypeBoxId", null, current.showInvoiceTypes);
    
    // تغییر عنوان بخش اطلاعات تراکنش
    modal.querySelector(".transactionBankBox .sectionTitle-invoice").textContent = current.transactionTitle || "";
    
    // تغییر متن تاریخ پرداخت یا وصول
    const vosolDate = modal.querySelector(".VosolDateDiv");
    vosolDate.querySelector("label").textContent = current.vosolDateText || "";
    vosolDate.querySelector("input").placeholder = current.vosolDateText || "";
    
    // تنظیم فیلد نام صاحب حساب
    const toName = modal.querySelector(".toNameWrapper");
    toName.querySelector("label").classList.toggle("d-none", !current.showToNameLabel);
    toName.querySelector("input").placeholder = current.toNamePlaceholder || "";
    
    // تغییر متن انتخاب تصویر رسید یا چک
    const uploaderSpan = modal.querySelector(".sanadPayBox .container-uploader span");
    if (uploaderSpan) {
        uploaderSpan.textContent = current.uploaderText;
    }
}

const PayAttachFormChange = (type) => {
    switch (type) {
        case 1:
            document.getElementById("PayAttachFormId").classList.add("d-none");
            document.querySelector("#AttachInvoiceModalId .container-uploader").classList.remove("d-none");
            document.querySelector("#AttachInvoiceModalId .container-uploader span").textContent = "تصویر رسید را انتخاب کنید یا اینجا رها کنید";
            break;
        case 2:
            document.getElementById("PayAttachFormId").classList.add("d-none");
            document.querySelector("#AttachInvoiceModalId .container-uploader").classList.remove("d-none");
            document.querySelector("#AttachInvoiceModalId .container-uploader span").textContent = "تصویر چک را انتخاب کنید یا اینجا رها کنید";
            break;
        case 3:
            document.querySelector("#AttachInvoiceModalId .container-uploader").classList.add("d-none");
            document.getElementById("PayAttachFormId").classList.remove("d-none");
            document.getElementById("PayAttachFormId").classList.remove("d-none");
            break;
    }
};


const delPayment = async (id) => {
    await deleteItem({
        url: `/Accountancy/DelAccountancy/${id}`,
        callback: refreshInvoiceSuccess,
    });
};

