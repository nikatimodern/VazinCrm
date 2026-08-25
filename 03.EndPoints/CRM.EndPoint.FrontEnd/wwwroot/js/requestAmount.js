////////////////////////////////////////////////////
async function showAddOrderHeadModal() {
    
    await modalSelector.open({
        title: "انتخاب حواله تولید",
        selectedIds: orderHeadsSelectedIds,
        
        load: async ({ search, selectedIds }) => {
            FLshowLoading();
            let filterIds = (await axios("/Order/GetAllOrderHeadsByCurrentTamin")).data;
            
            if (filterIds) {
                filterIds = [...new Set([...filterIds, ...selectedIds])];
            }
            
            const res = await axios.post("/Order/GetAllOrderHeads", { search, passType: 4, selectedIds, filterIds });
            FLshowLoading(false);
            return res.data;
        },
        
        onSubmit: ({ ids, numbers }) => {
            orderHeadsSelectedIds = ids;
            const input = document.getElementById("reqAmountOrderHead");
            input.value = numbers.join(", ");
            
            input.classList.remove("AlertReq");
        },
    });
}

/*--------------------------------------------------------------------------------*/
let supplyFactorsSelectedIds = [];
async function showAddSupplyFactorModal(elm) {
    const targetInput = elm.parentElement.querySelector("input");
    
    await modalSelector.open({
        title: "انتخاب فاکتور تامین",
        selectedIds: supplyFactorsSelectedIds,
        load: async ({ search, selectedIds }) => {
            
            FLshowLoading();
            const res = await axios.post("/Supply/GetAllSupplyFactorsTablePartial", { search, selectedIds });
            FLshowLoading(false);
            return res.data;
        },
        onSubmit: ({ ids, numbers }) => {
            supplyFactorsSelectedIds = ids;
            targetInput.value = numbers.join(", ");
            targetInput.classList.remove("AlertReq");
        },
    });
}
/*--------------------------------------------------------------------------------*/

let paymentData = {};
var reqAmountAttachment = [];
let reqAmountId;
let paymentTable;
let canEditPaymentTable;

let reqAmountAttachModal = document.getElementById("reqAmountAttachModal");

async function changeStatusReqAmount(id, status) {
    await sendApiRequest({
        method: "POST",
        url: "/RequestAmount/ChangeStatusRequestAmount",
        successMsg: null,
        params: { id, status },
        onSuccess: refreshRequestAmountDom,
    });
}

const ShowModalListPayment = async (id) => {
    getAllPaymentsList(id);
    ModalToggle("paymentListModalId", "show");
};
const initPaymentTable = () => {
    paymentTable = $("#paymentListTable").DataTable({
        paging: false,
        processing: true,
        serverSide: true,
        searching: false,
        ordering: true,
        order: [[1, "desc"]],
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        responsive: true,
        ajax: {
            url: ``,
            type: "POST",
            dataSrc: function (json) {
                canEditPaymentTable = json.hasAccess;
                return json.data;
            },
        },
        language: languageDatatable,
        columnDefs: [
            {
                targets: [3, 4, 6, 7],
                orderable: false,
            },
            {
                targets: [1],
                render: data => `<span dir="ltr">${data.slice(0, -3).split(" ").join(" | ")}</span>`,
            },
            {
                targets: [2],
                render: (data, type, row) => {
                    return `
                        <span class="d-inline-flex flex-column fbold">
                            <span dir="ltr">${data ? data.toLocaleString() : "-"}</span>
                           ${row.payment.paymentMode === 1
                        ? `<span style="font-size: 12px;color: #008077">پرداخت ${row.payment.paymentModeTxt} (<span>${row.payment.amount.toLocaleString()}</span>)</span>` : ""}
                        </span>
                    `;
                },
            },
        ],
        columns: [
            { data: "payment.paymentNumber", name: "PaymentNumber", defaultContent: "-" },
            { data: "payment.createDateTxt", name: "CreateDateTxt", defaultContent: "-" },
            { data: "requestPaymentAmount", name: "RequestPaymentAmount", defaultContent: "-" },
            { data: "payment.fromAccountTxt", name: "FromAccountTxt", defaultContent: "-" },
            { data: "payment.toAccountTxt", name: "ToAccountTxt", defaultContent: "-" },
            { data: "payment.paymentDateTxt", name: "PaymentDateTxt", defaultContent: "-" },
            { data: "payment.userCreator", name: "UserCreator", defaultContent: "-" },
            {
                data: null,
                render: function (data, type, row) {
                    return `
                        <div class="inline-flex-center gap-1">
                            <button type="button" style="padding: 6px 7px;box-shadow: none;font-size: 12px" class="buttonGray flex-center" onclick="showAttachments('${row.payment.id}', 9)"><i class="fa fa-paperclip"></i></button>
                          ${canEditPaymentTable
                        ? ` <button type="button" style="padding: 6px 6px;box-shadow: none;font-size: 12px" class="buttonGreen3 flex-center" onclick="showEditPaymentModal('${row.payment.id}')"><i class="fa fa-pencil"></i></button>
                            <button type="button" style="padding: 6px 7px;box-shadow: none;font-size: 12px" class="buttonCancel2 flex-center" onclick="deletePayment('${row.payment.id}')"><i class="fa fa-trash"></i></button>`
                        : ""}
                            
                        </div>
                    `;
                },
            },
        ],
        layout: null,
    });
};

const getAllPaymentsList = (id) => {
    reqAmountId = id;
    if (!paymentTable) {
        initPaymentTable();
    }
    paymentTable.clear().draw(); // TODO
    paymentTable.ajax.url(`/RequestAmount/GetAllPaymentsByRequestAmount?id=${id}`).load();
};


const deletePayment = async (id) => {
    await deleteItem({
        url: `/RequestAmount/DeletePayment/${id}`,
        callback: async () => {
            paymentTable.ajax.reload();
            await refreshRequestAmountDom();
        },
    });
};

async function showEditPaymentModal(id) {
    const data = await sendApiRequest({
        url: "/Supply/GetPayment",
        params: { paymentId: id },
    });
    if (!data) return;
    
    await showAttachReqAmount();
    paymentData = data.payment;
    mySer.convertJsonToForm(paymentData, "reqAmountAttachForm");
    if (paymentData.paymentMode === 1) {
        const targetInputAmount = reqAmountAttachModal.querySelector("input[name='amount']");
        toggleElementsDisabled(targetInputAmount, true);
        const amountEditIcon = reqAmountAttachModal.querySelector("#paymentAmountGroupEditIcon");
        amountEditIcon.classList.remove("d-none");
    }
    reqAmountAttachment = data.attachments;
    FileUpReload("reqAmountAttach");
}


////////////////////////////////////////////////////////
let requestAmountListData;
let reqAmountPaymentModal = document.getElementById("requestAmountPaymentModal");

let isGroupPayment = false;
async function showSetAmountPaymentItemModal(filterIds) {
    isGroupPayment = true;
    await sendApiRequest({
        method: "POST",
        url: "/RequestAmount/GetRequestsAmountByFilteredIds",
        data: { filterIds },
        successMsg: null,
        onSuccess: (res) => {
            requestAmountListData = res.data;
            
            createSetAmountPaymentItem();
            ModalToggle(reqAmountPaymentModal);
        },
    });
}

async function showEditAmountPaymentItemModal() {
    isGroupPayment = true;
    const data = await sendApiRequest({
        url: "/RequestAmount/GetRequestAmountsWithPaidAmountByPayment",
        params: { paymentId: paymentData.id },
        onSuccess: refreshRequestAmountDom,
    });
    
    if (!data) return;
    
    requestAmountListData = data;
    
    createSetAmountPaymentItem();
    ModalToggle(reqAmountPaymentModal);
}


function createSetAmountPaymentItem() {
    const container = reqAmountPaymentModal.querySelector("#setAmountPaymentForm");
    container.innerHTML = "";
    
    requestAmountListData.forEach((item, index) => {
        const fieldset = document.createElement("fieldset");
        fieldset.id = `setAmountPaymentItem-${index}`;
        fieldset.dataset.id = item.id;
        fieldset.innerHTML = `
            <div class="d-flex flex-wrap align-items-center justify-content-around setAmountPaymentItem bgGreen" style="border: 1px solid;border-radius: 6px;overflow: hidden">
                <div class="d-flex col-12 justify-content-around p-1 px-2" style="background-color:#04857c;color: #fff">
                    <div class="d-flex align-items-center justify-content-center gap-1 pe-2">
                        <span class="flex-shrink-0" style="color: #e1e1e1">شماره درخواست: </span>
                        <span class="fMedium text-center">${item.requestNumber ?? ""}</span>
                    </div>
                    <div class="d-flex align-items-center justify-content-center gap-1">
                        <span class="flex-shrink-0" style="color: #e1e1e1">مبلغ درخواست: </span>
                        <span class="fMedium text-center" style="white-space: nowrap">
                            <span>${item.amount.toLocaleString()}</span>
                            <span style="font-size: 11px">ریال</span>
                        </span>
                    </div>
                </div>

                <div class="d-flex flex-wrap col-12 justify-content-around p-2 row-gap-2">
                    <div class="d-flex align-items-center justify-content-between gap-3 gap-lg-2 col-12 col-lg-auto pe-xl-2">
                        <span class="flex-shrink-0" style="color: #535353">مبلغ تایید شده: </span>

                        <div class="fbold column-gap-1 badge badge-outline-green" style="color: #333">
                            <span style="font-size: 16px;">${item.acceptedAmount.toLocaleString()}</span>
                            <span style="font-size: 11px;">ريال</span>
                            <span style="font-size: 16px; color:#888">${item.acceptedAmountPercent ? item.acceptedAmountPercent + "%" : ""}</span>
                        </div>
                    </div>
                    <div class="d-flex align-items-center justify-content-between gap-3 gap-lg-2 col-12 col-lg-auto">
                        <span class="flex-shrink-0" style="color: #535353">مبلغ پرداختی: </span>
                        <input type="text" inputmode="numeric" class="currencyNumber form-control inpAmount fbold" style="border: 1px solid #b5b3b3;height: 26px;" name="amount"  value="${item.paidAmount ? item.paidAmount.toLocaleString() : ""}" required>
                    </div>
                </div>
            </div>
            ${index !== requestAmountListData.length - 1 ? `<div style="width: 95%;height:1px; margin: 10px auto;background-color:darkviolet"></div>` : ""}
            `;
        container.appendChild(fieldset);
    });
}


let requestAmountPaymentMap = [];
async function goToStepAttachReqAmount() {
    if (!await mySer.CheckValidForm("setAmountPaymentForm")) return;
    
    const allItemsArray = Array.from(reqAmountPaymentModal.querySelectorAll("fieldset"));
    
    requestAmountPaymentMap = allItemsArray.map(item => {
        const amountValue = item.querySelector("input[name='amount']").value;
        return {
            paymentId: paymentData.id,
            requestAmountId: item.dataset.id,
            amount: Number(SeparatedNumberRemove(amountValue)),
        };
    });
    const allInputsAmount = Array.from(reqAmountPaymentModal.querySelectorAll("input[name='amount']"));
    const totalAmount = allInputsAmount.reduce((sum, i) => sum + Number(SeparatedNumberRemove(i.value) || 0), 0);
    const targetInputAmount = reqAmountAttachModal.querySelector("input[name='amount']");
    
    targetInputAmount.value = totalAmount.toLocaleString();
    toggleElementsDisabled(targetInputAmount, true);
    ModalToggle(reqAmountPaymentModal, "hide");
    
    await showAttachReqAmount();
}

function cancelRequestAmountPaymentModal() {
    isGroupPayment = false;
    mySer.emptyForm("setAmountPaymentForm");
    ModalToggle(reqAmountPaymentModal, "hide");
}


let isLoadedBankAccounts = false;
const showAttachReqAmount = async (id) => {
    if (id) {
        reqAmountId = id;
    }
    if (!isLoadedBankAccounts) {
        await getAllBankAccounts([
            { element: "#reqAmountAttachModal select[name='fromAccountId']", type: [1] },
            { element: "#reqAmountAttachModal select[name='toAccountId']", type: [2, 3], isTomSelect: true },
        ]);
        isLoadedBankAccounts = true;
    }
    
    ModalToggle(reqAmountAttachModal, "show");
};


const cancelReqAmountAttach = () => {
    ModalToggle(reqAmountAttachModal, "hide");
    reqAmountAttachment = [];
    paymentData = {};
    document.getElementById("imageDisplayReqAmountAt").innerHTML = "";
    document.getElementById("reqAmountAttach").value = "";
    mySer.emptyForm("reqAmountAttachForm");
    const targetInputAmount = reqAmountAttachModal.querySelector("input[name='amount']");
    const amountEditIcon = reqAmountAttachModal.querySelector("#paymentAmountGroupEditIcon");
    toggleElementsDisabled(targetInputAmount, false);
    amountEditIcon.classList.add("d-none");
};


const submitReqAmountAttach = async () => {
    if (!await mySer.CheckValidForm("reqAmountAttachForm")) return;
    
    if (reqAmountAttachment.every(item => item.isDeleted)) {
        toastifyError("رسید واریزی اجباری است");
        return;
    }
    
    const fromAccountElm = document.querySelector("#reqAmountAttachForm select[name='fromAccountId']");
    const toAccountElm = document.querySelector("#reqAmountAttachForm select[name='toAccountId']");
    
    const formData = mySer.convertFormToJSON("reqAmountAttachForm");
    
    const requestAmountPayments = isGroupPayment
        ? requestAmountPaymentMap
        : [{ paymentId: paymentData.id, requestAmountId: reqAmountId, amount: formData.amount }];
    
    const model = {
        ...paymentData,
        ...formData,
        requestAmountPayments,
        attachments: reqAmountAttachment,
        toAccountTxt: toAccountElm.selectedOptions[0].innerText,
        fromAccountTxt: fromAccountElm.selectedOptions[0].innerText,
        paymentMode: isGroupPayment && requestAmountPaymentMap?.length > 1 ? 1 : 0,
    };
    
    
    await sendApiRequest({
        method: "POST",
        url: "/RequestAmount/AddPaymentByRequestAmount",
        data: model,
        onSuccess: async (res) => {
            if (res.status === 200) {
                cancelReqAmountAttach();
                if (isGroupPayment) {
                    cancelRequestAmountPaymentModal();
                    requestSelection.hideSelectionMode();
                }
                if (model.id && paymentTable) {
                    paymentTable.ajax.reload();
                }
                await refreshRequestAmountDom();
            }
        },
    });
};


const addBankAccountModal = document.getElementById("addBankAccountModal");
function toggleAddBankAccountModal() {
    mySer.emptyForm("addBankAccountForm");
    ModalToggle(addBankAccountModal);
}

async function submitAddBankAccount() {
    if (!await mySer.CheckValidForm("addBankAccountForm")) return;
    
    const cartNumber = addBankAccountModal.querySelector("#cartNumberId").value.trim();
    const accountNumber = addBankAccountModal.querySelector("#accountNumberId").value.trim();
    const shebaNumber = addBankAccountModal.querySelector("#shebaNumberId").value.trim();
    
    const hasValue = cartNumber || accountNumber || shebaNumber;
    
    if (!hasValue) {
        addBankAccountModal.querySelector("#cartNumberId").focus();
        toastifyError("حداقل یکی از موارد شماره کارت، شماره حساب یا شماره شبا باید وارد شود.");
        return;
    }
    
    
    const model = mySer.convertFormToJSON("addBankAccountForm");
    if (model.shebaNumber?.trim()) {
        model.shebaNumber = "IR " + model.shebaNumber.trim();
    }
    
    await sendApiRequest({
        method: "POST",
        url: "/Accountancy/AddBankAccount",
        data: model,
        onSuccess: async () => {
            toggleAddBankAccountModal();
            await getAllBankAccounts([
                { element: "#reqAmountAttachModal select[name='toAccountId']", type: [2, 3], isTomSelect: true },
            ], true);
        },
    });
}

/*--------------------------------------------------------------------------------*/

const addReqAmountModal = document.getElementById("addRequestAmountModal");
let orderHeadsSelectedIds = [];
let requestAmountData = {};
let isFromOrderHead = false;
let ohReqAmountId;
async function showAddRequestAmountModal(ohId, ohNumber) {
    await setupSelectOptionsReqAmount();
    if (ohId) {
        ohReqAmountId = ohId;
        isFromOrderHead = true;
        requestAmountData.requestAmountType = 1;
        orderHeadsSelectedIds = [ohId];
        addReqAmountModal.querySelector("#reqAmountModalTitleId").textContent = `درخواست پیش پرداخت حواله ${ohNumber}`;
    }
    
    if (ohId || ohReqAmountId) {
        const reqAmountTypeWrapper = addReqAmountModal.querySelector("#requestAmountTypeWrapper");
        reqAmountTypeWrapper?.remove();
        /*reqAmountType.value = "1";
        toggleElementsDisabled(reqAmountType, true);*/
    }
    ModalToggle(addReqAmountModal);
}

async function showAllRequestAmountModal(ohId, ohNumber) {
    const modal = document.getElementById("allRequestAmountsModal");
    modal.querySelector(".modal-title").innerText = ohNumber ? `درخواست های حواله شماره ${ohNumber}` : "درخواست های وجه";
    
    ohReqAmountId = ohId;
    await getAllRequestAmountByOrderHead(ohId);
    ModalToggle(modal, "show");
}

async function getAllRequestAmountByOrderHead(ohId) {
    const data = await sendApiRequest({
        url: "/RequestAmount/GetAllRequestAmountsPartial",
        params: { orderHeadId: ohId }
    });
    if (!data) return;
    
    document.querySelector("#allRequestAmountsModal #requestAmountsBodyModal").innerHTML = data.html;
}


let isSetupReqAmountModal = false;
async function setupSelectOptionsReqAmount() {
    if (isSetupReqAmountModal) return;
    const totalAmountSupFactorElm = addReqAmountModal.querySelector("#reqAmountTotalAmountSupFactor");
    
    const [resSuppliers, resCashiers] = await Promise.all([axios("/Supply/GetSupplyBaseData"), axios("/Cash/GetAllCashiers")]);
    
    const selectSupplier = new TomSelect("#reqAmountSupplierId", {
        options: resSuppliers.data.suppliers,
        placeholder: "انتخاب تامین کننده",
        maxItems: 1,
        selectOnTab: true,
        
        async onItemAdd(supplierId) {
            selectSupplier.disable();
            const supplierSummary = await getSupplierFinancialSummary(supplierId);
            if (supplierSummary !== null) {
                const debt = supplierSummary.supplierDebt;
                const debtColor = debt < 0 ? "#b42318" : debt > 0 ? "#16803c" : "#666";
                const debtBackground = debt < 0 ? "#fff3f3" : debt > 0 ? "#f0faf3" : "#f5f5f5";
                
                totalAmountSupFactorElm.innerHTML = `
                    <div class="d-flex flex-column align-items-end justify-content-center gap-1" style="font-size: 12px;color: #666">
                        <div class="d-flex flex-column flex-sm-row align-items-end justify-content-end column-gap-3 me-1">
                            <span>
                                جمع فاکتورها:
                                <b dir="ltr" style="color: #333">${supplierSummary.totalSupplyAmount?.toLocaleString()}</b>
                                <span class="fs-small">ریال</span>
                            </span>
                
                            <span class="d-none d-sm-inline">|</span>
                
                            <span>
                                جمع پرداختی‌ها:
                                <b dir="ltr" style="color: #333">${supplierSummary.totalPaidAmount?.toLocaleString()}</b>
                                <span class="fs-small">ریال</span>
                            </span>
                        </div>
                
                        <div class="px-2 py-1 rounded" style="background: ${debtBackground};color: ${debtColor}">
                            مانده بدهی:
                            <b dir="ltr">${debt?.toLocaleString()}</b>
                            <span class="fs-small">ریال</span>
                        </div>
                    </div>
                `;
            }
            selectSupplier.enable();
        },
        
        onDelete() {
            totalAmountSupFactorElm.innerHTML = "";
        },
    });
    
    new TomSelect("#reqAmountCashierId", {
        options: resCashiers.data,
        items: resCashiers.data.find(i => i.target)?.value,
        placeholder: "انتخاب تنخواه دار",
        maxItems: 1,
        selectOnTab: true,
    });
    
    isSetupReqAmountModal = true;
}


async function submitRequestAmountModal() {
    if (!await mySer.CheckValidForm("requestAmountForm")) return;
    const supplierSelect = addReqAmountModal.querySelector("#reqAmountSupplierId");
    const cashierSelect = addReqAmountModal.querySelector("#reqAmountCashierId");
    
    const formData = mySer.convertFormToJSON("requestAmountForm");
    const model = {
        ...requestAmountData,
        ...formData,
        cashierId: formData.cashierId || null,
        supplierId: formData.supplierId || null,
        supplierTxt: supplierSelect.selectedOptions[0].innerText,
        cashierTxt: cashierSelect.selectedOptions[0].innerText,
        requestAmountSupplyFactor: supplyFactorsSelectedIds.map(id => ({ supplyFactorId: id, requestAmountId: requestAmountData.id })),
        requestAmountOrderHeads: orderHeadsSelectedIds.map(id => ({ orderHeadId: id, requestAmountId: requestAmountData.id })),
    };
    
    await sendApiRequest({
        method: "POST",
        url: model.id ? "/RequestAmount/UpdateRequestAmount" : "/RequestAmount/AddRequestAmount",
        data: model,
        onSuccess: async () => {
            cancelRequestAmountModal();
            await refreshRequestAmountDom();
            const currentPath = window.location.pathname.toLowerCase();
            if (currentPath.includes("order") && !model.id) {
                await showAllRequestAmountModal(ohReqAmountId);
            }
        },
    });
}


function cancelRequestAmountModal() {
    ModalToggle(addReqAmountModal);
    mySer.emptyForm("requestAmountForm");
    supplyFactorsSelectedIds = [];
    orderHeadsSelectedIds = [];
    requestAmountData = {};
    isFromOrderHead = false;
    
    const ohInputWrapper = addReqAmountModal.querySelector("#reqAmountOrderHeadWrapper");
    const ohInput = addReqAmountModal.querySelector("input#reqAmountOrderHead");
    ohInputWrapper.classList.add("d-none");
    toggleElementsRequired(ohInput, false);
    const totalAmountSupFactorElm = addReqAmountModal.querySelector("#reqAmountTotalAmountSupFactor");
    totalAmountSupFactorElm.innerHTML = "";
    /*  const reqAmountType = addReqAmountModal.querySelector("#requestAmountTypeId");
      if (reqAmountType) toggleElementsDisabled(reqAmountType, false);*/
}


async function showEditRequestAmountModal(id) {
    await showAddRequestAmountModal();
    const data = await sendApiRequest({
        url: "/RequestAmount/GetRequestAmount",
        params: { id },
    });
    if (!data) return;
    
    requestAmountData = data.requestAmount;
    const supplyFactorSerials = data.supplyFactorSerials;
    const orderHeadSerials = data.orderHeadSerials;
    
    mySer.convertJsonToForm(requestAmountData, "requestAmountForm");
    supplyFactorsSelectedIds = requestAmountData.requestAmountSupplyFactor.map(r => r.supplyFactorId);
    orderHeadsSelectedIds = requestAmountData.requestAmountOrderHeads.map(r => r.orderHeadId);
    
    const inpElmSupplyFactor = addReqAmountModal.querySelector("#reqAmountSupplyFactor");
    if (inpElmSupplyFactor) {
        inpElmSupplyFactor.value = supplyFactorSerials.join(", ");
    }
    
    const inpElmOrderHead = addReqAmountModal.querySelector("#reqAmountOrderHead");
    if (inpElmOrderHead) {
        inpElmOrderHead.value = orderHeadSerials.join(", ");
    }
}


const deleteRequestAmount = async (id) => {
    await deleteItem({
        url: `/RequestAmount/DeleteRequestAmount/${id}`,
        callback: refreshRequestAmountDom,
    });
};

addReqAmountModal?.querySelector("select#requestAmountTypeId")?.addEventListener("change", async (e) => {
    const type = Number(e.target.value);
    
    const ohWrapper = addReqAmountModal.querySelector("#reqAmountOrderHeadWrapper");
    const cashierWrapper = addReqAmountModal.querySelector("#reqAmountCashierWrapper");
    const supplierWrapper = addReqAmountModal.querySelector("#reqAmountSupplierWrapper");
    
    const totalAmountSupFactorElm = addReqAmountModal.querySelector("#reqAmountTotalAmountSupFactor");
    
    const toggleField = (wrapper, show) => {
        const input = wrapper.querySelector("input, select");
        wrapper.classList.toggle("d-none", !show);
        toggleElementsRequired(input, show);
        
        if (show) {
            if (wrapper.id === "reqAmountCashierWrapper") {
                const target = Object.values(input.tomselect.options).find(o => o.target)?.value;
                if (target) input.tomselect.setValue(target);
            }
        } else {
            if (input?.tomselect) {
                input.tomselect.clear();
            }
            
            if (wrapper.id === "reqAmountOrderHeadWrapper") {
                orderHeadsSelectedIds = [];
                input.value = "";
            }
            
            if (wrapper.id === "reqAmountSupplierWrapper") {
                totalAmountSupFactorElm.innerHTML = "";
            }
        }
    };
    
    toggleField(ohWrapper, type === 1);
    toggleField(supplierWrapper, type === 1 || type === 2 || type === 4 || type === 5);
    toggleField(cashierWrapper, type === 3);
});


const closeDocTypeSelect = (select) => {
    select.classList.add("d-none");
    const wrapper = select.closest(".doc-type-wrapper");
    wrapper.querySelector(".doc-type-text").classList.remove("d-none");
};


function changePercentAcceptedRequestAmount(select, amount) {
    const percent = Number(select.value);
    const input = select.parentElement.querySelector("input");
    const acceptAmount = percent * amount / 100;
    input.value = acceptAmount ? acceptAmount.toLocaleString() : "";
    updateCurrencyHint(input);
    
    select.style.backgroundColor = acceptAmount ? "#cef5ce" : "";
    toggleAcceptBtn(select, acceptAmount);
}

function changeAcceptedRequestAmount(input, amount) {
    setTimeout(() => {
        const inputValue = Number(SeparatedNumberRemove(input.value)) || 0;
        const select = input.parentElement.querySelector("select");
        const percent = Number(select.value);
        if (percent * amount / 100 !== inputValue) {
            select.value = "";
            select.style.backgroundColor = "";
        }
        toggleAcceptBtn(input, inputValue);
    });
}

function toggleAcceptBtn(elm, acceptAmount) {
    const acceptBtn = elm.closest(".layered-card").querySelector(".buttonGreen2");
    if (acceptAmount === 0) {
        toggleElementsDisabled(acceptBtn, true);
    } else {
        toggleElementsDisabled(acceptBtn, false);
    }
}

async function submitAcceptedRequestAmount(btn, id) {
    const card = btn.closest(".layered-card");
    const input = card.querySelector("input.acceptedAmount");
    const selOpPercent = card.querySelector("select.acceptedAmountPercent");
    
    const model = {
        id,
        acceptedAmount: Number(SeparatedNumberRemove(input.value)),
        acceptedAmountPercent: selOpPercent.value ? Number(selOpPercent.value) : null,
    };
    
    await sendApiRequest({
        method: "POST",
        url: "/RequestAmount/SetAcceptedRequestAmount",
        data: model,
        successMsg: null,
        onSuccess: refreshRequestAmountDom,
    });
}

async function refreshRequestAmountDom() {
    const currentPath = window.location.pathname.toLowerCase();
    if (currentPath.includes("cartable")) {
        const { tab } = getUrlParams();
        switch (tab) {
            case "TManagingDirectorId":
                await GetRequestManagingDirector();
                break;
            case "TFinancialExpertId":
                await GetRequestFinancialExpert();
                break;
        }
    } else if (currentPath.includes("order")) {
        await getAllRequestAmountByOrderHead(ohReqAmountId);
        refreshOrders();
        
    } else if (currentPath.includes("requestamount")) {
        await getAllRequestAmount();
    }
}

document.addEventListener("click", e => {
    const editBtn = e.target.closest(".doc-type-edit-btn");
    
    if (editBtn) {
        document.querySelectorAll("select.doc-type-select:not(.d-none)").forEach(closeDocTypeSelect);
        
        const card = editBtn.closest(".layered-card");
        const select = card.querySelector(".doc-type-select");
        
        card.querySelector(".doc-type-text").classList.add("d-none");
        
        select.classList.remove("d-none");
        select.focus();
        
        return;
    }
    
    document.querySelectorAll("select.doc-type-select:not(.d-none)").forEach(select => {
        if (select.closest(".doc-type-wrapper").contains(e.target)) return;
        
        closeDocTypeSelect(select);
    });
});

document.addEventListener("change", async (e) => {
    if (!e.target.classList.contains("doc-type-select")) return;
    
    const select = e.target;
    const card = select.closest(".layered-card");
    
    select.disabled = true;
    await sendApiRequest({
        method: "POST",
        url: "/RequestAmount/ChangeDocTypeRequestAmount",
        data: { id: card.dataset.id, docType: Number(select.value) },
        successMsg: null,
        onSuccess: () => {
            const selectedText = select.options[select.selectedIndex].text;
            const docTypeText = card.querySelector(".doc-type-text");
            docTypeText.textContent = `(${selectedText})`;
            
            select.classList.add("d-none");
            docTypeText.classList.remove("d-none");
        },
    });
    select.disabled = false;
});
