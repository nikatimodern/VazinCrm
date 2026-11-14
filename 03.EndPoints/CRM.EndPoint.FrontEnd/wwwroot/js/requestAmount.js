////////////////////////////////////////////////////
async function showAddOrderHeadRaModal() {
    
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

const ShowModalListPayment = async (id, rqNumber) => {
    
    
    getAllPaymentsList(id);
    ModalToggle("paymentListModalId", "show");
    
    document.getElementById("titlePaymentListModalId").textContent = `لیست پرداختی ها مربط به درخواست شماره ${rqNumber}`;
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
                            <button type="button" class="icon-button icon-button--gray" onclick="showAttachments('${row.payment.id}', 9)"><i class="fa fa-paperclip"></i></button>
                          ${canEditPaymentTable
                        ? ` <button type="button" class="icon-button icon-button--green" onclick="showEditPaymentModal('${row.payment.id}')"><i class="fa fa-pencil"></i></button>
                            <button type="button" class="icon-button icon-button--red" onclick="deletePayment('${row.payment.id}')"><i class="fa fa-trash"></i></button>`
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
            
            
            createSetAmountPaymentItem(false);
            ModalToggle(reqAmountPaymentModal);
        },
    });
}

async function showEditAmountPaymentItemModal() {
    isGroupPayment = true;
    const data = await sendApiRequest({
        url: "/RequestAmount/GetRequestAmountsWithPaidAmountByPayment",
        params: { paymentId: paymentData.id },
    });
    
    if (!data) return;
    
    requestAmountListData = data;
    
    createSetAmountPaymentItem(true);
    ModalToggle(reqAmountPaymentModal);
}


function createSetAmountPaymentItem(isEdit) {
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
                        <span class="flex-shrink-0" style="color: #e1e1e1">مبلغ تایید شده: </span>
                        <span class="fMedium text-center" style="white-space: nowrap">
                            <span>${item.acceptedAmount.toLocaleString()}</span>
                            <span style="font-size: 11px">ریال</span>
                            <span style="font-size: 16px;color:#ccc">${item.acceptedAmountPercent ? item.acceptedAmountPercent + "%" : ""}</span>
                        </span>
                    </div>
                </div>

                <div class="d-flex flex-wrap col-12 justify-content-around p-2 row-gap-2">
                    <div class="d-flex align-items-center justify-content-between gap-3 gap-lg-2 col-12 col-lg-auto pe-xl-2">
                        <span class="flex-shrink-0" style="color: #535353">مبلغ پرداخت شده: </span>

                        <div class="fbold column-gap-1 badge badge-outline-green" style="color: #333">
                            <span style="font-size: 16px;">${item.requestAmountPayments.reduce((sum, i) => {
            
            return i.amount + sum;
            
        }, 0).toLocaleString()}</span>
                            <span style="font-size: 11px;">ريال</span>
                            
                        </div>
                    </div>
                    <div class="d-flex align-items-center justify-content-between gap-3 gap-lg-2 col-12 col-lg-auto">
                        <span class="flex-shrink-0" style="color: #535353">مبلغ پرداختی: </span>
                        <input type="text" inputmode="numeric" class="currencyNumber form-control inpAmount fbold" style="border: 1px solid #b5b3b3;height: 26px;" name="amount"  value="  ${isEdit && item.requestAmountPayments[0].amount ? item.requestAmountPayments[0].amount.toLocaleString() : ""}" required>
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
let requestPaymentTargetContext = null;

// A single request is loaded by its ID; grouped payments already have the
// requests returned by GetRequestsAmountByFilteredIds / GetRequestsByPayment.
async function resolveRequestPaymentTarget() {
    const groupedRequests = isGroupPayment && Array.isArray(requestAmountListData)
        ? requestAmountListData
        : null;
    // One destination account cannot represent different recipients in a group payment.
    if (groupedRequests && (!groupedRequests.length || groupedRequests.some(item =>
        String(item.targetPersonId || "").toLowerCase() !== String(groupedRequests[0].targetPersonId || "").toLowerCase()
        || Number(item.requestAmountType === 6 ? 4 : item.requestAmountType === 3 ? 3 : 2)
            !== Number(groupedRequests[0].requestAmountType === 6 ? 4 : groupedRequests[0].requestAmountType === 3 ? 3 : 2)))) {
        return null;
    }
    let request = groupedRequests?.[0] || null;

    if (!request && reqAmountId && !groupedRequests) {
        const details = await sendApiRequest({
            url: "/RequestAmount/GetRequestAmount",
            params: { id: reqAmountId },
            showLoading: false,
            showError: false,
        });
        request = details?.requestAmount;
    }
    if (!request) return null;

    const requestType = Number(request.requestAmountType);
    // 6: customer refund; 3: petty cash; other types: supplier.
    const bankAccountType = requestType === 6 ? 4 : requestType === 3 ? 3 : 2;
    return {
        bankAccountType,
        targetPersonId: request.targetPersonId || null,
        targetPersonTxt: request.targetPersonTxt || "",
    };
}

async function refreshRequestAmountBankAccountOptions() {
    const select = reqAmountAttachModal?.querySelector("select[name='toAccountId']");
    if (!select) return;
    const context = requestPaymentTargetContext;
    const linkedPersonId = context && (context.bankAccountType === 2 || context.bankAccountType === 4)
        ? context.targetPersonId
        : null;

    // Remove the previous request's choices before fetching, so a failed
    // refresh cannot expose accounts from another recipient.
    if (select.tomselect) {
        select.tomselect.clear(true);
        select.tomselect.clearOptions();
    } else {
        select.replaceChildren();
    }

    // Supplier/customer requests show strictly the accounts linked to their
    // target person. Without a person or linked account, TomSelect is empty.
    // Petty-cash requests keep the cashier account category.
    const isPersonTarget = context?.bankAccountType === 2 || context?.bankAccountType === 4;
    await getAllBankAccounts([
        {
            element: select,
            type: context?.bankAccountType === 3 ? [3] : [],
            isTomSelect: true,
            preferredCustomerId: linkedPersonId,
            onlyLinkedToCustomer: isPersonTarget || !context,
            autoSelectLinked: Boolean(linkedPersonId),
        },
    ], true);
}

const openNewRequestAmountBankAccount = () => {
    const context = requestPaymentTargetContext;
    return toggleAddBankAccountModal({
        bankAccountType: context?.bankAccountType ?? 2,
        customerId: context?.bankAccountType === 2 || context?.bankAccountType === 4
            ? context.targetPersonId
            : null,
        customerText: context?.targetPersonTxt || "",
        onSaved: refreshRequestAmountBankAccountOptions,
    });
};

const showAttachReqAmount = async (id) => {
    if (id) reqAmountId = id;

    if (!isLoadedBankAccounts) {
        await getAllBankAccounts([
            { element: "#reqAmountAttachModal select[name='fromAccountId']", type: [1] },
        ]);
        isLoadedBankAccounts = true;
    }

    requestPaymentTargetContext = await resolveRequestPaymentTarget();
    await refreshRequestAmountBankAccountOptions();
    ModalToggle(reqAmountAttachModal, "show");
};


const cancelReqAmountAttach = () => {
    ModalToggle(reqAmountAttachModal, "hide");
    reqAmountAttachment = [];
    paymentData = {};
    requestPaymentTargetContext = null;
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


/*--------------------------------------------------------------------------------*/

const addReqAmountModal = document.getElementById("addRequestAmountModal");
let orderHeadsSelectedIds = [];
let requestAmountData = {};
let isFromOrderHead = false;
let ohReqAmountId;
async function showAddRequestAmountModal(ohId, ohNumber) {
    // Keep the TomSelect dropdown on native scrolling; modal-wide momentum
    // handlers intercept its wheel events and make the list feel sluggish.
    await setupSelectOptionsReqAmount();
    if (ohId) {
        ohReqAmountId = ohId;
        isFromOrderHead = true;
        requestAmountData.requestAmountType = 1;
        orderHeadsSelectedIds = [ohId];
        addReqAmountModal.querySelector("#reqAmountOrderHead").value = String(ohNumber ?? "");
        addReqAmountModal.querySelector("#reqAmountModalTitleId").textContent = `درخواست پیش پرداخت حواله ${ohNumber}`;
    }
    
    if (ohId || ohReqAmountId) {
        const reqAmountTypeWrapper = addReqAmountModal.querySelector("#requestAmountTypeWrapper");
        reqAmountTypeWrapper?.classList.add("d-none");
        const typeSelect = addReqAmountModal.querySelector("#requestAmountTypeId");
        typeSelect.value = "1";
        typeSelect.dispatchEvent(new Event("change"));
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
        params: { orderHeadId: ohId },
    });
    if (!data) return;
    
    document.querySelector("#allRequestAmountsModal #requestAmountsBodyModal").innerHTML = data.html;
}


let isSetupReqAmountModal = false;
let requestTargetKind = "";
let requestSupplierOptions = [];
let requestCashierOptions = [];
let requestSupplierSummaryToken = 0;

function requestTargetSelect() {
    return addReqAmountModal?.querySelector("#reqAmountTargetPersonId")?.tomselect;
}

// The option's target kind is generated from RequestAmountTypeEnum on the server.
function updateRequestAmountTargetField() {
    const kind = addReqAmountModal.querySelector("#requestAmountTypeId")?.selectedOptions[0]?.dataset.targetKind || "";
    const wrapper = addReqAmountModal.querySelector("#reqAmountTargetPersonWrapper");
    const select = addReqAmountModal.querySelector("#reqAmountTargetPersonId");
    const dropdown = requestTargetSelect();
    const labels = { supplier: "تأمین‌کننده", cashier: "تنخواه‌دار", customer: "مشتری" };
    const hasTarget = Boolean(labels[kind]);
    
    wrapper.classList.toggle("d-none", !hasTarget);
    toggleElementsRequired(select, hasTarget);
    addReqAmountModal.querySelector("#reqAmountTargetPersonLabel").textContent = hasTarget ? `${labels[kind]}:` : "شخص مقصد:";
    
    if (requestTargetKind !== kind && dropdown) {
        requestTargetKind = kind;
        requestSupplierSummaryToken++;
        dropdown.clear(true);
        dropdown.clearOptions();
        const choices = kind === "supplier" ? requestSupplierOptions
            : kind === "cashier" ? requestCashierOptions : [];
        dropdown.addOptions(choices);
        dropdown.refreshOptions(false);
        dropdown.settings.placeholder = hasTarget ? `انتخاب ${labels[kind]}` : "انتخاب شخص";
        dropdown.control_input?.setAttribute("placeholder", dropdown.settings.placeholder);
        if (kind === "cashier") {
            const ownCashier = choices.find(x => x.target)?.value;
            if (ownCashier) dropdown.setValue(ownCashier, true);
        }
        addReqAmountModal.querySelector("#reqAmountTotalAmountSupFactor").replaceChildren();
    }
}

async function renderRequestSupplierSummary(supplierId) {
    const container = addReqAmountModal.querySelector("#reqAmountTotalAmountSupFactor");
    const token = ++requestSupplierSummaryToken;
    container.replaceChildren();
    if (requestTargetKind !== "supplier" || !supplierId) return;
    
    const summary = await getSupplierFinancialSummary(supplierId);
    if (token !== requestSupplierSummaryToken || requestTargetKind !== "supplier"
        || requestTargetSelect()?.getValue() !== supplierId || !summary) return;
    
    const debt = summary.supplierDebt ?? 0;
    const debtColor = debt < 0 ? "#b42318" : debt > 0 ? "#16803c" : "#666";
    const debtBackground = debt < 0 ? "#fff3f3" : debt > 0 ? "#f0faf3" : "#f5f5f5";
    container.innerHTML = `
        <div class="d-flex flex-column align-items-end justify-content-center gap-1" style="font-size:12px;color:#666">
            <div class="d-flex flex-column flex-sm-row align-items-end justify-content-end column-gap-3 me-1">
                <span>جمع فاکتورها: <b dir="ltr" style="color:#333">${(summary.totalSupplyAmount ?? 0).toLocaleString()}</b> <span class="fs-small">ریال</span></span>
                <span class="d-none d-sm-inline">|</span>
                <span>جمع پرداختی‌ها: <b dir="ltr" style="color:#333">${(summary.totalPaidAmount ?? 0).toLocaleString()}</b> <span class="fs-small">ریال</span></span>
            </div>
            <div class="px-2 py-1 rounded" style="background:${debtBackground};color:${debtColor}">
                مانده بدهی: <b dir="ltr">${debt.toLocaleString()}</b> <span class="fs-small">ریال</span>
            </div>
        </div>`;
}

async function setupSelectOptionsReqAmount() {
    if (isSetupReqAmountModal) return;
    const [suppliersData, cashiersData] = await Promise.all([getSupplyBaseData(), getAllCashiers()]);
    if (!suppliersData || !cashiersData) return;
    
    requestSupplierOptions = suppliersData.suppliers ?? [];
    requestCashierOptions = cashiersData;
    
    new TomSelect("#reqAmountTargetPersonId", {
        options: [],
        placeholder: "انتخاب شخص",
        maxItems: 1,
        selectOnTab: true,
        loadThrottle: 300,
        shouldLoad: query => requestTargetKind === "customer" && query.trim().length > 0,
        load(query, callback) {
            if (requestTargetKind !== "customer") return callback();
            sendApiRequest({
                url: "/Customer/GetCustomerOp",
                params: { search: query.trim(), roleType: 1 },
                showLoading: false,
            }).then(result => {
                if (requestTargetKind !== "customer") return callback();
                callback(Array.isArray(result)
                    ? result.map(item => ({ value: item.id, text: item.text }))
                    : []);
            }).catch(() => callback());
        },
        onChange(value) {
            void renderRequestSupplierSummary(value);
        },
    });
    isSetupReqAmountModal = true;
    updateRequestAmountTargetField();
}


async function getAllCashiers() {
    return await sendApiRequest({
        url: "/Cash/GetAllCashiers",
        params: { useEntityId: true },
        showLoading: false,
    });
}


async function getSupplyBaseData() {
    return await sendApiRequest({
        url: "/Supply/GetSupplyBaseData",
        showLoading: false,
    });
}

async function getSupplierFinancialSummary(supplierId) {
    return await sendApiRequest({
        url: "/Supply/GetSupplierFinancialSummary",
        params: { supplierId },
        showLoading: false,
    });
}


async function submitRequestAmountModal() {
    if (!await mySer.CheckValidForm("requestAmountForm")) return;
    const formData = mySer.convertFormToJSON("requestAmountForm");
    const targetSelect = requestTargetSelect();
    const targetPersonId = targetSelect?.getValue() || null;
    if (!targetPersonId) {
        targetSelect?.focus();
        toastifyError("شخص مقصد را انتخاب کنید.");
        return;
    }
    const model = {
        ...requestAmountData,
        ...formData,
        targetPersonId,
        targetPersonTxt: targetSelect.options[targetPersonId]?.text || "",
        requestAmountSupplyFactor: supplyFactorsSelectedIds.map(id => ({ supplyFactorId: id, requestAmountId: requestAmountData.id })),
        requestAmountOrderHeads: orderHeadsSelectedIds.map(id => ({ orderHeadId: id, requestAmountId: requestAmountData.id })),
    };
    const relatedOrderHeadId = ohReqAmountId;
    await sendApiRequest({
        method: "POST",
        url: model.id ? "/RequestAmount/UpdateRequestAmount" : "/RequestAmount/AddRequestAmount",
        data: model,
        onSuccess: async () => {
            cancelRequestAmountModal();
            await refreshRequestAmountDom();
            const currentPath = window.location.pathname.toLowerCase();
            if (currentPath.includes("order") && !model.id && relatedOrderHeadId) {
                await showAllRequestAmountModal(relatedOrderHeadId);
            }
        },
    });
}

function cancelRequestAmountModal() {
    ModalToggle(addReqAmountModal, "hide");
    mySer.emptyForm("requestAmountForm");
    supplyFactorsSelectedIds = [];
    orderHeadsSelectedIds = [];
    requestAmountData = {};
    isFromOrderHead = false;
    ohReqAmountId = null;
    const ohWrapper = addReqAmountModal.querySelector("#reqAmountOrderHeadWrapper");
    ohWrapper.classList.add("d-none");
    toggleElementsRequired(addReqAmountModal.querySelector("#reqAmountOrderHead"), false);
    addReqAmountModal.querySelector("#requestAmountTypeWrapper")?.classList.remove("d-none");
    addReqAmountModal.querySelector("#reqAmountModalTitleId").textContent = "درخواست وجه";
    addReqAmountModal.querySelector("#reqAmountTotalAmountSupFactor").replaceChildren();
    updateRequestAmountTargetField();
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
    
    // Set the type first so its target-entity switch cannot discard the selected person.
    const typeSelect = addReqAmountModal.querySelector("#requestAmountTypeId");
    typeSelect.value = String(requestAmountData.requestAmountType);
    typeSelect.dispatchEvent(new Event("change"));
    mySer.convertJsonToForm(requestAmountData, "requestAmountForm", ["requestAmountType", "targetPersonId"]);
    const targetSelect = requestTargetSelect();
    if (requestAmountData.targetPersonId && targetSelect) {
        const personId = String(requestAmountData.targetPersonId);
        if (!targetSelect.options[personId]) {
            targetSelect.addOption({ value: personId, text: requestAmountData.targetPersonTxt || "شخص مقصد" });
        }
        targetSelect.setValue(personId);
    }
    supplyFactorsSelectedIds = (requestAmountData.requestAmountSupplyFactor || []).map(r => r.supplyFactorId);
    orderHeadsSelectedIds = (requestAmountData.requestAmountOrderHeads || []).map(r => r.orderHeadId);
    
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

addReqAmountModal?.querySelector("select#requestAmountTypeId")?.addEventListener("change", event => {
    const type = Number(event.target.value);
    const ohWrapper = addReqAmountModal.querySelector("#reqAmountOrderHeadWrapper");
    const orderHeadInput = ohWrapper.querySelector("input");
    const isAdvance = type === 1;
    
    ohWrapper.classList.toggle("d-none", !isAdvance);
    toggleElementsRequired(orderHeadInput, isAdvance);
    if (!isAdvance) {
        orderHeadsSelectedIds = [];
        orderHeadInput.value = "";
    }
    updateRequestAmountTargetField();
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
