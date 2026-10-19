var cashAttachments = [];
var searchVarCash;
var cashConfigFilter = {};

let cashTbl;
let cashGroupTbl = null;
let cashModalTbl = null;

let currentCashGroupId = null;
let currentCashGroupExported = null;
let cashViewType;

let cashData = {};
let modelCash = {};

let cashOrderHeadsSelectedIds = [];

const cashModal = document.getElementById("cashModal");
const cashGroupDetailModal = document.getElementById("cashGroupDetailModal");

const costCenterElm = cashModal.querySelector("#cashCostCenter");
document.addEventListener("DOMContentLoaded", async () => {
    
    //await initCashierSelectOpt();
    
    const checkedRadio = document.querySelector(`input[name="cashViewType"]:checked`);
    if (checkedRadio) {
        switchCashView(checkedRadio.value);
    }
    
    const radioButtons = document.querySelectorAll(`input[name="cashViewType"]`);
    radioButtons.forEach(radio => radio.addEventListener("change", (e) => switchCashView(e.target.value)));
    
    document.body.addEventListener("change", async (e) => {
        if (e.target.matches("input[name='cash_type']")) {
            modelCash._type = Number(e.target.value);
            cashTbl?.ajax.reload();
        }
    });
});


function refreshCashView() {
    cashTbl?.ajax.reload(null, false);
    
    getBalanceCashier().then(data => initCashierBalanceUI(data));
}

async function getBalanceCashier() {
    return await sendApiRequest({
        url: "/Cash/GetBalanceCashier",
    });
}

cashModal.addEventListener("hidden.bs.modal", () => {
    mySer.emptyForm("cashForm");
    cashData = [];
    cashAttachments = [];
    cashOrderHeadsSelectedIds = [];
});

cashGroupDetailModal.addEventListener("hidden.bs.modal", () => {
    cashModalSelection.hideSelectionMode();
    cashIds = [];
});

async function initCashierSelectOpt() {
    const [baseCashCenter, baseCash] = await Promise.all([
        axios("/Cash/GetAllBaseCashCenter").then(r => r.data),
        axios("/Cash/GetAllBaseCashTitle").then(r => r.data),
    ]);
    createOption(costCenterElm, baseCashCenter);
    createOption("cashCostTitle", baseCash);
}


async function showEditCashModal(id) {
    FLshowLoading();
    ModalToggle(cashModal);
    
    try {
        const res = await axios(`/Cash/GetCash/${id}`);
        if (res.status === 200) {
            cashData = res.data.cash;
            const orderHeadSerials = res.data.orderHeadSerials;
            cashOrderHeadsSelectedIds = cashData.cashOrderHeads?.map(r => r.orderHeadId) ?? [];
            mySer.convertJsonToForm(cashData, "cashForm");
            cashAttachments = res.data.attachments;
            FileUpReload("cashAtt");
            
            const inpElmOrderHead = cashModal.querySelector("#cashOrderHeadInp");
            if (inpElmOrderHead) {
                inpElmOrderHead.value = orderHeadSerials.join(", ");
            }
        }
        
    } catch (err) {
        console.log(err);
    } finally {
        FLshowLoading(false);
    }
}

async function sendCashToExternalSoftwares(id) {
    await showExternalSoftwareModal(id, {
        mapType: 0,
        onConfirm: async ({ entityIds, externalSoftwareIds }) => {
            await sendApiRequest({
                url: "/Cash/SendCashesToExternalSoftware",
                method: "POST",
                data: { ids: entityIds, externalSoftwareIds },
                successMsg: "انتقال تنخواه با موفقیت انجام شد",
                onSuccess: () => cashModalTbl?.ajax.reload(null, false),
            });
        },
    });
}


const deleteCash = async (id) => {
    Swal.fire({
        title: "حذف!",
        text: "آیا مطمئن هستید که میخواهید حذف کنید؟",
        icon: "warning",
        showCancelButton: true,
        cancelButtonColor: "#adadad",
        confirmButtonColor: "#e64942",
        cancelButtonText: "انصراف",
        confirmButtonText: "بله، حذف شود",
    }).then(async (willDelete) => {
        if (willDelete.isConfirmed) {
            FLshowLoading();
            const res = await axios(`/Cash/DeleteCash/${id}`);
            if (res.status === 200) {
                toastifySuccess("با موفقیت حذف شد");
                if (cashViewType === "cashes") {
                    refreshCashView();
                } else {
                    cashModalTbl?.ajax.reload(null, false);
                }
            }
            FLshowLoading(false);
        }
    });
};


async function submitCashModal() {
    if (!await mySer.CheckValidForm("cashForm")) return;
    if (cashAttachments.every(item => item.isDeleted)) {
        toastifyError("تصویر رسید اجباری است");
        return;
    }
    const costTitleElm = document.getElementById("cashCostTitle");
    const model = {
        ...cashData,
        ...mySer.convertFormToJSON("cashForm"),
        costCenterTxt: costCenterElm.options[costCenterElm.selectedIndex].text,
        costTitleTxt: costTitleElm.options[costTitleElm.selectedIndex].text,
        attachments: cashAttachments,
        cashOrderHeads: cashOrderHeadsSelectedIds.map(id => ({ orderHeadId: id, cashId: cashData.id })),
    };
    
    
    try {
        FLshowLoading();
        const res = await axios.post("/Cash/AddCash", model);
        if (res.status === 200) {
            toastifySuccess("با موفقیت ثبت شد");
            refreshCashView();
            ModalToggle(cashModal);
        }
    } catch (err) {
        console.log(err);
        if (err.status === 404) {
            toastifyError(err.response.data);
        } else {
            toastifyError("خطای سرور");
        }
    } finally {
        FLshowLoading(false);
    }
}

async function changeStatusCash(id, status) {
    await sendApiRequest({
        url: `/Cash/ChangeStatusCash/${id}`,
        params: { status: status },
        successMsg: status === 2 ? "تایید شد" : null,
        onSuccess: () => {
            if (cashViewType === "cashes") {
                refreshCashView();
            } else {
                cashModalTbl?.ajax.reload(null, false);
                cashGroupTbl?.ajax.reload(null, false);
            }
        },
    });
}

async function changeStatusAllCashToPending() {
    await sendApiRequest({
        url: `/Cash/ChangeStatusAllCashToPending`,
        successMsg: "به مالی ارسال شد",
        onSuccess: () => {
            cashTbl?.ajax.reload(null, false);
        },
    });
}

async function changeStatusGroupCashToPending(listIds = []) {
    await sendApiRequest({
        url: `/Cash/ChangeStatusGroupCashToPending`,
        method: "POST",
        data: listIds,
        successMsg: "به مالی ارسال شد",
        onSuccess: () => {
            cashTbl?.ajax.reload(null, false);
        },
    });
}

async function approveCashGroup() {
    if (!currentCashGroupId) return;
    await sendApiRequest({
        url: `/Cash/ApproveCashGroup`,
        params: { groupId: currentCashGroupId },
        successMsg: "کل گروه با موفقیت تایید شد",
        onSuccess: () => {
            cashModalTbl?.ajax.reload(null, false);
            cashGroupTbl?.ajax.reload(null, false);
        },
    });
}

async function rejectCashGroup() {
    if (!currentCashGroupId) return;
    
    await confirmAlert({
        title: "برگشت!",
        text: "آیا از برگشت کل این گروه به تنخواه‌دار اطمینان دارید؟",
        confirmText: "بله، برگشت داده شود",
        onConfirm: async () => {
            await sendApiRequest({
                url: `/Cash/RejectCashGroup`,
                params: { groupId: currentCashGroupId },
                successMsg: "گروه با موفقیت برگشت داده شد",
                onSuccess: () => {
                    ModalToggle(cashGroupDetailModal, "hide");
                    cashGroupTbl?.ajax.reload(null, false);
                },
            });
        },
    });
}


const getAllCashesDt = () => {
    cashTbl = $("#cashTable").DataTable({
        pageLength: 10,
        lengthMenu: [5, 10, 20, 50, 100, { label: "همه", value: -1 }],
        processing: true,
        serverSide: true,
        searching: true,
        search: {
            return: true,
        },
        ordering: true,
        order: [[2, "desc"]],
        scrollCollapse: true,
        scrollY: "70dvh",
        responsive: true,
        ajax: {
            url: `/Cash/GetAllCashesDt`,
            type: "POST",
            data: (d) => {
                d.model = JSON.stringify(modelCash);
            },
            dataSrc: function (json) {
                cashTbl.column(1).visible(window.isAdminOrFinancial);
                cashTbl.settings()[0].aoColumns[1].bSearchable = window.isAdminOrFinancial;
                // initCashierBalanceUI(json.cashierBalance);
                return json.data;
            },
        },
        language: languageDatatable,
        columnDefs: [
            { targets: "_all", defaultContent: "-" },
        ],
        columns: [
            { data: "serial", responsivePriority: 1 },
            { data: "cashGroup.groupNumber" },
            { data: "createDateTxt", render: renderDateTimeHtml },
            { data: "costTitleTxt", responsivePriority: 2 },
            { data: "costCenterTxt", responsivePriority: 3 },
            { data: "costDate" },
            { data: "amount", render: data => renderMoneyHtml(data, { currency: null }) },
            { data: "fee", render: data => renderMoneyHtml(data, { currency: null }) },
            { data: "totalAmount", render: data => renderMoneyHtml(data, { currency: null, className: "fbold" }) },
            { data: "costDescription", className: "wrap-text-minw" },
            { data: "userCreator" },
            {
                data: "status",
                orderable: false,
                className: "nowrap-text",
                render: function (data, type, row) {
                    const mappedInfo = window.isAdminOrFinancial && row.externalSoftwareTitles?.length
                        ? `<div class="cash-map-softwares">
                                ${(row.externalSoftwareTitles).map(title => `
                                <span class="cash-map-software">
                                  <svg style="width: 13px" viewBox="0 0 16 16" fill="currentColor"><path fill-rule="evenodd" d="M12.416 3.376a.75.75 0 0 1 .208 1.04l-5 7.5a.75.75 0 0 1-1.154.114l-3-3a.75.75 0 0 1 1.06-1.06l2.353 2.353 4.493-6.74a.75.75 0 0 1 1.04-.207Z" clip-rule="evenodd" /></svg>
                                  ${title}
                                </span>`).join("")}
                            </div>`
                        : "";
                    
                    
                    let statusContent = "";
                    
                    if (data === 0) { // ثبت موقت
                        //if (isManager) return `<span class="fMedium" style="color: #666">${row.statusTxt}</span>`;
                        statusContent = `<button class="buttonGreen2" onclick="changeStatusCash('${row.id}', 1)">ارسال به مالی</button>`;
                    } else if (data === 1) { // در دست تایید
                        statusContent = window.isAdminOrFinancial
                            ? `<div class="inline-flex-center gap-1">
                                    <button class="buttonGreen2" onclick="changeStatusCash('${row.id}', 2)">تایید</button>
                                    <button class="buttonCancel3" onclick="changeStatusCash('${row.id}', 0)">برگشت</button>
                                </div>`
                            : `<span class="fMedium" style="color:#CC9900">${row.statusTxt}</span>`;
                        
                    } else {
                        // تایید شده
                        
                        statusContent = `
                            <div class="d-inline-flex flex-column align-items-center gap-1">
                                <div class="inline-flex-center gap-1">
                                    <span class="fMedium" style="color:darkgreen">${row.statusTxt}</span>
                                    ${window.isAdminOrFinancial ? `
                                        <button style="color:#333" onclick="changeStatusCash('${row.id}', 1)" data-title-left="بازگشت">
                                            <svg style="width:16px;height:16px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="m15 15 6-6m0 0-6-6m6 6H9a6 6 0 0 0 0 12h3"/></svg>
                                        </button>` : ""}
                                </div>
                            </div>`
                        ;
                    }
                    
                    return `<div class="d-inline-flex flex-column align-items-center gap-1">
                        ${statusContent}
                        ${mappedInfo}
                    </div>`;
                },
            },
            {
                data: null,
                orderable: false,
                render: function (data, type, row) {
                    return `
                        <div class="inline-flex-center gap-1">
                        ${window.isAdminOrFinancial && row.status === 2 ? `
                            <button type="button" class="buttonGray" onclick="sendCashToExternalSoftwares('${row.id}')">ارسال به نرم‌افزار مالی</button>                       
                        ` : ""}
                            <button type="button" class="icon-button icon-button--gray" onclick="showAttachments('${row.id}', 10)"><i class="fa fa-paperclip"></i></button>
                            ${row.status === 0 ? `
                                <button type="button" class="icon-button icon-button--green" onclick="showEditCashModal('${row.id}')"><i class="fa fa-pencil"></i></button>
                                <button type="button" class="icon-button icon-button--red" onclick="deleteCash('${row.id}')"><i class="fa fa-trash"></i></button>
                            ` : ""}
                        </div>
                    `;
                },
            },
        ],
        layout: {
            ...layoutDatatable,
            topEnd: {
                pageLength: true,
                buttons: [
                    {
                        extend: "excel",
                        exportOptions: {
                            columns: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
                        },
                        text: `<svg fill="currentColor" viewBox="0 0 256 256"><path d="M156,208a8,8,0,0,1-8,8H120a8,8,0,0,1-8-8V152a8,8,0,0,1,16,0v48h20A8,8,0,0,1,156,208ZM92.65,145.49a8,8,0,0,0-11.16,1.86L68,166.24,54.51,147.35a8,8,0,1,0-13,9.3L58.17,180,41.49,203.35a8,8,0,0,0,13,9.3L68,193.76l13.49,18.89a8,8,0,0,0,13-9.3L77.83,180l16.68-23.35A8,8,0,0,0,92.65,145.49Zm98.94,25.82c-4-1.16-8.14-2.35-10.45-3.84-1.25-.82-1.23-1-1.12-1.9a4.54,4.54,0,0,1,2-3.67c4.6-3.12,15.34-1.72,19.82-.56a8,8,0,0,0,4.07-15.48c-2.11-.55-21-5.22-32.83,2.76a20.58,20.58,0,0,0-8.95,14.95c-2,15.88,13.65,20.41,23,23.11,12.06,3.49,13.12,4.92,12.78,7.59-.31,2.41-1.26,3.33-2.15,3.93-4.6,3.06-15.16,1.55-19.54.35A8,8,0,0,0,173.93,214a60.63,60.63,0,0,0,15.19,2c5.82,0,12.3-1,17.49-4.46a20.81,20.81,0,0,0,9.18-15.23C218,179,201.48,174.17,191.59,171.31ZM40,112V40A16,16,0,0,1,56,24h96a8,8,0,0,1,5.66,2.34l56,56A8,8,0,0,1,216,88v24a8,8,0,1,1-16,0V96H152a8,8,0,0,1-8-8V40H56v72a8,8,0,0,1-16,0ZM160,80h28.68L160,51.31Z"></path></svg>`,
                        titleAttr: "اکسل",
                        title: "تنخواه",
                        className: "buttonExcel",
                    },
                ],
            },
        },
        createdRow: (row, data) => {
            row.dataset.id = data.id;
        },
        initComplete: () => initFilterCashTable(),
        drawCallback: function () {
            const json = this.api()?.ajax.json();
            const container = document.querySelector(".dt-length");
            
            if (!container) return;
            
            const shouldShow = json?.data?.some(i => i.status === 0);
            
            let groupBtn = container.querySelector(".groupSelectCashBtn");
            let sendAllBtn = container.querySelector(".sendAllBtn");
            
            if (!shouldShow) {
                groupBtn?.remove();
                sendAllBtn?.remove();
                return;
            }
            
            if (!groupBtn) {
                groupBtn = document.createElement("button");
                groupBtn.className = "buttonBlue2 groupSelectCashBtn";
                groupBtn.textContent = "ارسال گروهی";
            }
            
            if (!sendAllBtn) {
                sendAllBtn = document.createElement("button");
                sendAllBtn.className = "buttonGreen2 sendAllBtn";
                sendAllBtn.innerHTML = `ارسال همه <span class="d-none d-sm-inline">به مالی</span>`;
                
                sendAllBtn.onclick = async () => {
                    sendAllBtn.disabled = true;
                    await changeStatusAllCashToPending();
                    sendAllBtn.disabled = false;
                };
            }
            
            container.prepend(sendAllBtn);
            container.prepend(groupBtn);
        },
    });
    
    const cashSelection = new SelectionManager({
        container: "#cashTable tbody",
        selectionBar: "#cashSelectionToolbar",
        toggleBtn: ".groupSelectCashBtn",
        itemSelector: "tr:not(:has(td.dt-empty)):not(.child)",
        selectedClass: "row-selected",
        checkboxSelector: null,
        getItemKey: item => item.dataset.id,
        
        isSelectable: item => {
            const row = cashTbl.row(item);
            return row.any() && row.data().status === 0;
        },
        
        onAction: async (ids, selection) => {
            await changeStatusGroupCashToPending(ids);
            selection.hideSelectionMode();
        },
        
        onSelectionModeEnter() {
            document.querySelector("#cashBalanceContainer").classList.add("d-none");
            document.querySelector("#cashModalBtn").classList.add("d-none");
            document.querySelector(".buttonExcel").classList.add("d-none");
            document.querySelector(".sendAllBtn").classList.add("d-none");
        },
        
        onSelectionModeExit() {
            document.querySelector("#cashBalanceContainer").classList.remove("d-none");
            document.querySelector("#cashModalBtn").classList.remove("d-none");
            document.querySelector(".buttonExcel").classList.remove("d-none");
            document.querySelector(".sendAllBtn").classList.remove("d-none");
        },
    });
    
    cashTbl.on("draw.dt", () => cashSelection.refresh());
    
    
    function initFilterCashTable() {
        const dtLength = document.querySelector(".dt-length label");
        const dtSearch = document.querySelector(".dt-search");
        const searchInput = dtSearch?.querySelector("input");
        if (!searchInput) return;
        const filterButton = `
            <div class="btnFilter pe-md-2" data-filterVarConfig="cashConfigFilter" data-filterCallBack="callBackCashFilter" data-filterContentId="contentCashFilterId" data-filterInpSearchId="dt-search-0" data-filterVarSearch="searchVarCash" onclick="ShowFilterPopup(this, ['cusFilterItem'])">
                <svg style="width:24px;height:24px;color:#333" viewBox="0 0 24 24" fill="currentColor" ><path fill-rule="evenodd" d="M3.792 2.938A49.069 49.069 0 0 1 12 2.25c2.797 0 5.54.236 8.209.688a1.857 1.857 0 0 1 1.541 1.836v1.044a3 3 0 0 1-.879 2.121l-6.182 6.182a1.5 1.5 0 0 0-.439 1.061v2.927a3 3 0 0 1-1.658 2.684l-1.757.878A.75.75 0 0 1 9.75 21v-5.818a1.5 1.5 0 0 0-.44-1.06L3.13 7.938a3 3 0 0 1-.879-2.121V4.774c0-.897.64-1.683 1.542-1.836Z" clip-rule="evenodd" /></svg>
            </div>
        `;
        const radioBtns = `
            <div class="d-flex column-gap-2 align-items-center justify-content-between flex-wrap">
                <ul class="filter-switch mt-2 mt-md-0">
                    <li class="filter-switch-item">
                        <input type="radio" name="cash_type" id="cash_type-all" value="-1" checked>
                        <label for="cash_type-all">همه</label>
                    </li>
                    <li class="filter-switch-item">
                        <input type="radio" name="cash_type" id="cash_type-1" value="1">
                        <label for="cash_type-1">در انتظار تایید</label>
                    </li>
                    <li class="filter-switch-item">
                        <input type="radio" name="cash_type" id="cash_type-2" value="2">
                        <label for="cash_type-2">تایید شده</label>
                    </li>
                </ul>
            </div>
        `;
        const contentFilter = `<div id="contentCashFilterId" style="justify-content: start;" class="ContentFilter mt-2 mt-md-0 ms-md-2"></div>`;
        
        searchInput.insertAdjacentHTML("afterend", filterButton);
        dtSearch.insertAdjacentHTML("afterend", contentFilter);
        dtSearch.insertAdjacentHTML("afterend", radioBtns);
    }
    
    cashTbl.on("search", function () {
        modelCash.serVal = cashTbl.search();
    });
};

const callBackCashFilter = () => {
    modelCash.serVal = window[filterVarConfig]?.varSearch;
    modelCash.filt = window[filterVarConfig]?.filterVar;
    
    cashTbl?.ajax.reload();
};


const initCashierBalanceUI = (data) => {
    if (!data) return;
    
    const container = document.getElementById("cashBalanceContainer");
    
    const balance = data.totalCredit - data.totalDebit;
    let statusClass = "";
    if (balance < 0) {
        statusClass = "cash-summary--negative";
    } else if (!data.isAccepted) {
        statusClass = "cash-summary--pending";
    }
    
    const usedPercent =
        data.totalCredit <= 0
            ? 0
            : Math.min(Math.round((data.totalDebit / data.totalCredit) * 100), 100);
    
    container.innerHTML = `
        <div class="cash-summary ${statusClass}">

            <div class="cash-summary__header">

                <div class="cash-summary__meta">
                    <span>
                        موجودی:
                        <b>${data.totalCredit.toLocaleString()}</b>
                    </span>
                    <span>
                        هزینه شده:
                        <b>${data.totalDebit.toLocaleString()}</b>
                    </span>
                </div>
                <div class="cash-summary__balance-row">
                    <span class="cash-balance-badge">
                        مانده:
                        <span class="direction-ltr">${balance.toLocaleString()}</span>
                        <span style="font-size: 11px">ريال</span>
                    </span>
                </div>

            </div>

            <div class="cash-summary__progress">
                <div class="cash-summary__fill" style="width:${usedPercent}%" data-bs-toggle="popover" data-bs-content="${usedPercent}%" data-bs-custom-class="popover-sm" data-bs-placement="right"></div>
            </div>

        </div>
    `;
};


function switchCashView(viewType) {
    const itemsContainer = document.getElementById("cashItemsContainer");
    const groupsContainer = document.getElementById("cashGroupsContainer");
    cashViewType = viewType;
    if (viewType === "cashes") {
        itemsContainer.classList.remove("d-none");
        groupsContainer.classList.add("d-none");
        document.querySelector("#cashModalBtn").classList.remove("d-none");
        if (!cashTbl) {
            getAllCashesDt();
            getBalanceCashier().then(data => initCashierBalanceUI(data));
        } else {
            refreshCashView();
        }
        
    } else { // viewType === "groups"
        groupsContainer.classList.remove("d-none");
        itemsContainer.classList.add("d-none");
        document.querySelector("#cashModalBtn").classList.add("d-none");
        if (!cashGroupTbl) {
            getAllCashesGroupDt();
        } else {
            cashGroupTbl?.ajax.reload(null, false);
        }
    }
}

function updateCashModalActionButtons(items) {
    const btnApprove = document.getElementById("btnApproveGroup");
    const btnReject = document.getElementById("btnRejectGroup");
    
    if (!window.isAdminOrFinancial) {
        btnApprove?.remove();
        btnReject?.remove();
        return;
    }
    
    if (!items || items.length === 0) {
        btnApprove?.classList.add("d-none");
        btnReject?.classList.add("d-none");
        return;
    }
    
    const isAllApproved = items.every(item => item.status === 2);
    
    btnReject?.classList.remove("d-none");
    btnApprove?.classList.toggle("d-none", isAllApproved);
}

function getAllCashesGroupDt() {
    cashGroupTbl = $("#cashGroupTable").DataTable({
        pageLength: 10,
        lengthMenu: [5, 10, 20, 50, 100, { label: "همه", value: -1 }],
        processing: true,
        serverSide: true,
        searching: true,
        search: {
            return: true,
        },
        ordering: true,
        order: [[1, "desc"]],
        scrollCollapse: true,
        scrollY: "70dvh",
        responsive: true,
        ajax: {
            url: `/Cash/GetCashGroupsDt`,
            type: "POST",
        },
        language: languageDatatable,
        columnDefs: [
            { targets: "_all", defaultContent: "-" },
        ],
        columns: [
            { data: "groupNumber", responsivePriority: 1 },
            { data: "createDateTxt", responsivePriority: 2, render: renderDateTimeHtml },
            { data: "userCreator" },
            { data: "itemsCount" },
            { data: "totalAmount", render: data => renderMoneyHtml(data, { currency: null, className: "fbold" }) },
            {
                data: "status",
                orderable: false,
                render: (data, type, row) => {
                    if (data === 2) return `<span class="fbold" style="color: darkgreen">${row.statusTxt}</span>`;
                    return `<span class="fbold" style="color: #CC9900">${row.statusTxt}</span>`;
                },
            },
            {
                data: null,
                orderable: false,
                responsivePriority: 3,
                render: function (data, type, row) {
                    return `
                        <button class="buttonGray" onclick="cashGroupModal('${row.cashGroupId}', '${row.groupNumber}', '${row.userCreator ?? "-"}', '${row.createDateTxt}', ${row.itemsCount}, ${row.totalAmount})">مشاهده</button>
                    `;
                },
            },
        ],
        layout: {
            ...layoutDatatable,
            topEnd: {
                pageLength: true,
                buttons: [
                    {
                        extend: "excel",
                        exportOptions: {
                            columns: [0, 1, 2, 3, 4, 5],
                        },
                        text: `<svg fill="currentColor" viewBox="0 0 256 256"><path d="M156,208a8,8,0,0,1-8,8H120a8,8,0,0,1-8-8V152a8,8,0,0,1,16,0v48h20A8,8,0,0,1,156,208ZM92.65,145.49a8,8,0,0,0-11.16,1.86L68,166.24,54.51,147.35a8,8,0,1,0-13,9.3L58.17,180,41.49,203.35a8,8,0,0,0,13,9.3L68,193.76l13.49,18.89a8,8,0,0,0,13-9.3L77.83,180l16.68-23.35A8,8,0,0,0,92.65,145.49Zm98.94,25.82c-4-1.16-8.14-2.35-10.45-3.84-1.25-.82-1.23-1-1.12-1.9a4.54,4.54,0,0,1,2-3.67c4.6-3.12,15.34-1.72,19.82-.56a8,8,0,0,0,4.07-15.48c-2.11-.55-21-5.22-32.83,2.76a20.58,20.58,0,0,0-8.95,14.95c-2,15.88,13.65,20.41,23,23.11,12.06,3.49,13.12,4.92,12.78,7.59-.31,2.41-1.26,3.33-2.15,3.93-4.6,3.06-15.16,1.55-19.54.35A8,8,0,0,0,173.93,214a60.63,60.63,0,0,0,15.19,2c5.82,0,12.3-1,17.49-4.46a20.81,20.81,0,0,0,9.18-15.23C218,179,201.48,174.17,191.59,171.31ZM40,112V40A16,16,0,0,1,56,24h96a8,8,0,0,1,5.66,2.34l56,56A8,8,0,0,1,216,88v24a8,8,0,1,1-16,0V96H152a8,8,0,0,1-8-8V40H56v72a8,8,0,0,1-16,0ZM160,80h28.68L160,51.31Z"></path></svg>`,
                        titleAttr: "اکسل",
                        title: "تنخواه",
                        className: "buttonExcel",
                    },
                ],
            },
        },
    });
}


function cashGroupModal(groupId, groupNumber, userCreator, sendDate, itemsCount, totalAmount) {
    currentCashGroupId = groupId;
    
    cashGroupDetailModal.querySelector("#modalGroupNumber").textContent = groupNumber || "-";
    cashGroupDetailModal.querySelector("#modalGroupUser").textContent = userCreator || "-";
    cashGroupDetailModal.querySelector("#modalGroupDate").textContent = sendDate ? sendDate.split(" ")[0] : "-";
    cashGroupDetailModal.querySelector("#modalGroupCount").textContent = itemsCount || "-";
    cashGroupDetailModal.querySelector("#modalGroupTotal").textContent = totalAmount ? totalAmount.toLocaleString() : "0";
    
    ModalToggle(cashGroupDetailModal);
    
    if (!cashModalTbl) {
        getAllCashesModalDt();
    } else {
        cashModalTbl.clear().draw();
        cashModalTbl.ajax.reload(null, false);
    }
}

let cashModalSelection;
function getAllCashesModalDt() {
    cashModalTbl = $("#cashTableModal").DataTable({
        pageLength: 10,
        lengthMenu: [5, 10, 20, 50, 100, { label: "همه", value: -1 }],
        processing: true,
        serverSide: true,
        searching: true,
        search: {
            return: true,
        },
        ordering: true,
        order: [[1, "desc"]],
        scrollCollapse: true,
        scrollY: "calc(100dvh - 430px)",
        responsive: true,
        ajax: {
            url: `/Cash/GetAllCashesDt`,
            type: "POST",
            data: (d) => {
                d.model = JSON.stringify({ entityId: currentCashGroupId });
            },
            dataSrc: function (json) {
                currentCashGroupExported = json.isExported;
                return json.data;
            },
        },
        language: languageDatatable,
        columnDefs: [
            { targets: "_all", defaultContent: "-" },
        ],
        columns: [
            { data: "serial", responsivePriority: 1 },
            { data: "createDateTxt", className: "nowrap-text", render: renderDateTimeHtml },
            { data: "costTitleTxt", responsivePriority: 2 },
            { data: "costCenterTxt", responsivePriority: 3 },
            { data: "costDate" },
            { data: "amount", render: data => renderMoneyHtml(data, { currency: null }) },
            { data: "fee", render: data => renderMoneyHtml(data, { currency: null }) },
            { data: "totalAmount", render: data => renderMoneyHtml(data, { currency: null, className: "fbold" }) },
            { data: "costDescription" },
            {
                data: "status",
                orderable: false,
                className: "nowrap-text",
                render: function (data, type, row) {
                    const mappedInfo = window.isAdminOrFinancial && row.externalSoftwareTitles?.length
                        ? `<div class="cash-map-softwares">
                                ${(row.externalSoftwareTitles).map(title => `
                                <span class="cash-map-software">
                                  <svg style="width: 13px" viewBox="0 0 16 16" fill="currentColor"><path fill-rule="evenodd" d="M12.416 3.376a.75.75 0 0 1 .208 1.04l-5 7.5a.75.75 0 0 1-1.154.114l-3-3a.75.75 0 0 1 1.06-1.06l2.353 2.353 4.493-6.74a.75.75 0 0 1 1.04-.207Z" clip-rule="evenodd" /></svg>
                                  ${title}
                                </span>`).join("")}
                            </div>`
                        : "";
                    
                    
                    let statusContent = "";
                    
                    if (data === 0) {
                        // ثبت موقت
                        //if (isManager) return `<span class="fMedium" style="color: #666">${row.statusTxt}</span>`;
                        statusContent = `<button class="buttonGreen2" onclick="changeStatusCash('${row.id}', 1)">ارسال به مالی</button>`;
                    } else if (data === 1) {
                        // در دست تایید
                        statusContent = window.isAdminOrFinancial
                            ? `<div class="inline-flex-center gap-1">
                                    <button class="buttonGreen2" onclick="changeStatusCash('${row.id}', 2)">تایید</button>
                                    <button class="buttonCancel3" onclick="changeStatusCash('${row.id}', 0)">برگشت</button>
                                </div>`
                            : `<span class="fMedium" style="color:#CC9900">${row.statusTxt}</span>`;
                        
                    } else {
                        // تایید شده
                        statusContent = `
                            <div class="d-inline-flex flex-column align-items-center gap-1">
                                <div class="inline-flex-center gap-1">
                                    <span class="fMedium" style="color:darkgreen">${row.statusTxt}</span>
                                    ${window.isAdminOrFinancial ? `
                                        <button style="color:#333" onclick="changeStatusCash('${row.id}', 1)" data-title-left="بازگشت">
                                            <svg style="width:16px;height:16px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="m15 15 6-6m0 0-6-6m6 6H9a6 6 0 0 0 0 12h3"/></svg>
                                        </button>` : ""}
                                </div>
                            </div>`
                        ;
                    }
                    
                    return `<div class="d-inline-flex flex-column align-items-center gap-1">
                        ${statusContent}
                        ${mappedInfo}
                    </div>`;
                },
            },
            {
                data: null,
                orderable: false,
                className: "nowrap-text",
                render: function (data, type, row) {
                    return `
                        <div class="inline-flex-center gap-1">
                        ${window.isAdminOrFinancial && row.status === 2 ? `
                            <button type="button" class="buttonGray" onclick="sendCashToExternalSoftwares('${row.id}')">ارسال به نرم‌افزار مالی</button>                       
                        ` : ""}
                            <button type="button" class="icon-button icon-button--gray" onclick="showAttachments('${row.id}', 10)"><i class="fa fa-paperclip"></i></button>
                            ${row.status === 0 ? `
                                <button type="button" class="icon-button icon-button--green" onclick="showEditCashModal('${row.id}')"><i class="fa fa-pencil"></i></button>
                                <button type="button" class="icon-button icon-button--red" onclick="deleteCash('${row.id}')"><i class="fa fa-trash"></i></button>
                            ` : ""}
                        </div>
                    `;
                },
            },
        ],
        layout: {
            ...layoutDatatable,
            /*topEnd: {
                pageLength: true,
                buttons: window.isAdminOrFinancial ? [
                    {
                        extend: "excel",
                        className: "buttonExcel px-2",
                        text: `خروجی اکسل سپیدار <svg fill="currentColor" viewBox="0 0 256 256"><path d="M156,208a8,8,0,0,1-8,8H120a8,8,0,0,1-8-8V152a8,8,0,0,1,16,0v48h20A8,8,0,0,1,156,208ZM92.65,145.49a8,8,0,0,0-11.16,1.86L68,166.24,54.51,147.35a8,8,0,1,0-13,9.3L58.17,180,41.49,203.35a8,8,0,0,0,13,9.3L68,193.76l13.49,18.89a8,8,0,0,0,13-9.3L77.83,180l16.68-23.35A8,8,0,0,0,92.65,145.49Zm98.94,25.82c-4-1.16-8.14-2.35-10.45-3.84-1.25-.82-1.23-1-1.12-1.9a4.54,4.54,0,0,1,2-3.67c4.6-3.12,15.34-1.72,19.82-.56a8,8,0,0,0,4.07-15.48c-2.11-.55-21-5.22-32.83,2.76a20.58,20.58,0,0,0-8.95,14.95c-2,15.88,13.65,20.41,23,23.11,12.06,3.49,13.12,4.92,12.78,7.59-.31,2.41-1.26,3.33-2.15,3.93-4.6,3.06-15.16,1.55-19.54.35A8,8,0,0,0,173.93,214a60.63,60.63,0,0,0,15.19,2c5.82,0,12.3-1,17.49-4.46a20.81,20.81,0,0,0,9.18-15.23C218,179,201.48,174.17,191.59,171.31ZM40,112V40A16,16,0,0,1,56,24h96a8,8,0,0,1,5.66,2.34l56,56A8,8,0,0,1,216,88v24a8,8,0,1,1-16,0V96H152a8,8,0,0,1-8-8V40H56v72a8,8,0,0,1-16,0ZM160,80h28.68L160,51.31Z"></path></svg>`,
                        action: async function (e, dt, node, config, callback) {
                            try {
                                node[0].disabled = true;
                                await exportGroupCashToExcel();
                            } finally {
                                node[0].disabled = false;
                                callback();
                            }
                        },
                    },
                ] : [],
            },*/
        },
        drawCallback: function () {
            const json = this.api()?.ajax.json();
            updateCashModalActionButtons(json?.data);
        },
    });
    
    
    cashModalSelection = new SelectionManager({
        container: "#cashTableModal tbody",
        selectionBar: "#cashGroupDetailSelectionToolbar",
        toggleBtn: "#sendGroupCashToExtrnalSoftwareBtn",
        itemSelector: "tr:not(:has(td.dt-empty)):not(.child)",
        selectedClass: "row-selected",
        checkboxSelector: null,
        
        getItemKey: item => cashModalTbl.row(item).data()?.id,
        
        isSelectable: item => {
            const data = cashModalTbl.row(item).data();
            return data.status === 2;
        },
        
        onAction: async (ids, selection) => {
            await sendCashToExternalSoftwares(ids);
            selection.hideSelectionMode();
        },
        
        onSelectionModeEnter() {
            cashGroupDetailModal.querySelector("#cashGroupDetailCard").classList.add("d-none");
            /*cashGroupDetailModal.querySelector("#btnRejectGroup").classList.add("d-none");
            cashGroupDetailModal.querySelector("#btnApproveGroup").classList.add("d-none");*/
        },
        
        onSelectionModeExit() {
            cashGroupDetailModal.querySelector("#cashGroupDetailCard").classList.remove("d-none");
            /*cashGroupDetailModal.querySelector("#btnRejectGroup").classList.remove("d-none");
            cashGroupDetailModal.querySelector("#btnApproveGroup").classList.remove("d-none");*/
        },
    });
    
    
    cashModalTbl.on("draw.dt", () => cashModalSelection.refresh());
}

async function exportGroupCashToExcel() {
    if (!currentCashGroupId) return;
    
    let startVoucherNumber = null;
    
    if (!currentCashGroupExported) {
        
        const result = await Swal.fire({
            title: "شماره اولین اعلامیه پرداخت",
            input: "number",
            inputLabel: "شماره اولین رکورد را وارد کنید",
            inputPlaceholder: "مثلاً 423",
            showCancelButton: true,
            confirmButtonText: "دریافت اکسل",
            cancelButtonText: "انصراف",
            inputValidator: value => {
                if (!value) return "شماره را وارد کنید";
                if (+value <= 0) return "شماره معتبر نیست";
            },
        });
        
        if (!result.isConfirmed) return;
        startVoucherNumber = Number(result.value);
    }
    
    await sendApiRequest({
        url: `/Cash/ExportCashGroupToExcel`,
        params: { groupId: currentCashGroupId, startVoucherNumber },
        responseType: "blob",
        showLoading: false,
        onSuccess: (response) => {
            const blob = response.data;
            
            const url = URL.createObjectURL(blob);
            triggerDownload(url, "تنخواه.xlsx");
            URL.revokeObjectURL(url);
            cashModalTbl.ajax.reload(null, false);
        },
    });
}

async function showAddCashOrderHeadModal() {
    await modalSelector.open({
        title: "انتخاب حواله تولید",
        selectedIds: cashOrderHeadsSelectedIds,
        
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
            cashOrderHeadsSelectedIds = ids;
            const input = document.getElementById("cashOrderHeadInp");
            input.value = numbers.join(", ");
            
            input.classList.remove("AlertReq");
        },
    });
}