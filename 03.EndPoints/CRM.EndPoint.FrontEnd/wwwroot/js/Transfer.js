let transferTbl;
let transferModel = { _type: -1, serVal: "" };
let transferData = {};
var transferAttachment = [];

const transferModal = document.getElementById("transferModal");
const transferFields = {
    fromSelect: transferModal?.querySelector("[name='fromAccountId']"),
    fromInput: transferModal?.querySelector("[name='fromAccountTxt']"),
    toSelect: transferModal?.querySelector("[name='toAccountId']"),
    toInput: transferModal?.querySelector("[name='toAccountTxt']"),
    header: transferModal?.querySelector(".modalHeader-invoice"),
};


async function showModalTransfer(type) {
    transferData.transferType = type;
    
    setTransferModalUI(type);
    ModalToggle(transferModal, "show");
    await loadTransferAccounts(type);
}

function setTransferModalUI(type) {
    const { fromSelect, fromInput, toSelect, toInput, header } = transferFields;
    
    if (type === 1) {
        fromSelect.classList.add("d-none");
        fromInput.classList.remove("d-none");
        
        toSelect.classList.remove("d-none");
        toInput.classList.add("d-none");
        toInput.value = "";
        
        fromSelect.required = false;
        fromInput.required = true;
        
        toSelect.required = true;
        toInput.required = false;
        
        header.innerText = "ثبت واریز";
        transferModal.classList.add("deposit-mode");
        transferModal.classList.remove("backward-mode");
    }
    
    if (type === 2) {
        fromSelect.classList.remove("d-none");
        fromInput.classList.add("d-none");
        fromInput.value = "";
        
        toSelect.classList.add("d-none");
        toInput.classList.remove("d-none");
        
        fromSelect.required = true;
        fromInput.required = false;
        
        toSelect.required = false;
        toInput.required = true;
        
        header.innerText = "ثبت برداشت";
        transferModal.classList.remove("deposit-mode");
        transferModal.classList.add("backward-mode");
    }
}

const loadTransferAccounts = async (type) => {
    const { fromSelect, toSelect } = transferFields;
    await getAllBankAccounts([{ element: type === 1 ? toSelect : fromSelect, type: [1] }]);
};

function cancelTransferModal() {
    ModalToggle(transferModal, "hide");
    mySer.emptyForm("transferForm");
    transferData = {};
    transferAttachment = [];
}

async function submitTransferModal() {
    if (!await mySer.CheckValidForm("transferForm")) return;
    if (transferAttachment.every(item => item.isDeleted)) {
        toastifyError("رسید پرداخت اجباری است");
        return;
    }
    const formData = mySer.convertFormToJSON("transferForm");
    const fromAccountElm = transferModal.querySelector("#transferForm select[name='fromAccountId']");
    const toAccountElm = transferModal.querySelector("#transferForm select[name='toAccountId']");
    
    const model = {
        ...transferData,
        ...formData,
        attachments: transferAttachment,
        toAccountId: formData.toAccountId || null,
        fromAccountId: formData.fromAccountId || null,
        toAccountTxt: formData.toAccountTxt || toAccountElm.selectedOptions[0]?.innerText,
        fromAccountTxt: formData.fromAccountTxt || fromAccountElm.selectedOptions[0]?.innerText,
    };
    try {
        FLshowLoading();
        const res = await axios.post(`/Transfer/AddTransfer`, model);
        if (res.status === 200) {
            toastifySuccess("با موفقیت ثبت شد");
            cancelTransferModal();
            transferTbl.ajax.reload();
        }
    } catch (err) {
        console.log(err);
        toastifyError("خطای سرور");
    } finally {
        FLshowLoading(false);
    }
}

function getAllTransfersDt(selector, isMdCartable = false) {
    transferTbl = $(selector).DataTable({
        paging: !isMdCartable,
        lengthMenu: [10, 25, 50, 100, { label: "همه", value: -1 }],
        pageLength: 10,
        processing: true,
        serverSide: true,
        search: {
            return: true,
        },
        ordering: true,
        order: [[1, "desc"]],
        filter: true,
        stateSave: false,
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        responsive: true,
        ajax: {
            url: `/Transfer/GetAllTransfersDt`,
            type: "POST",
            data: (d) => {
                d.model = JSON.stringify(transferModel);
            },
            dataSrc: function (json) {
                if (isMdCartable) {
                    document.getElementById("MDTransferCount").textContent = json.recordsFiltered;
                    
                    updateFilterSwitchState({
                        wrapperSelector: "#MDTransferTab-pane",
                        filterData: json.filterCounts,
                        currentFilterValue: transferModel._type,
                        searchValue: transferModel.serVal,
                        onChange: (type) => {
                            transferModel._type = Number(type);
                            transferTbl.ajax.reload();
                        },
                    });
                }
                return json.data;
            },
        },
        columns: [
            { data: "transferNumber" },
            { data: "createDateTxt", render: data => `<span dir="ltr">${renderDateTimeHtml(data)}</span>` },
            {
                data: "amount",
                render: data => renderMoneyHtml(data, { className: "fbold" }),
            },
            { data: "fromAccountTxt" },
            { data: "toAccountTxt" },
            {
                data: "transferType",
                render: data => {
                    if (data === 1) return `<span class="badge badge-green">واریز</span>`;
                    if (data === 2) return `<span class="badge badge-red">برداشت</span>`;
                },
                orderable: false,
                defaultContent: "-"
            },
            { data: "transferDate" },
            { data: "userCreator", orderable: false },
            {
                data: "status",
                orderable: false,
                render: (data, _, row) => {
                    if (isMdCartable && data === 1) {
                        return `
                            <button class="buttonGreen2" onclick="changeStatusTransfer('${row.id}', 2)">تایید</button>
                            <button class="buttonCancel3" onclick="changeStatusTransfer('${row.id}', 0)">برگشت</button>
                        `;
                    }
                    
                    if (!isMdCartable && data === 0) {
                        return `<button class="buttonGreen2" onclick="changeStatusTransfer('${row.id}', 1)">ارسال به مدیرعامل</button>`;
                    }
                    
                    const STATUS_COLORS = { 0: "#777", 1: "#CC9900", 2: "#006400", 3: "#8b0000" };
                    let label = `<span class="fbold" style="color: ${STATUS_COLORS[data]}">${row.statusTxt}</span>`;
                    if (isMdCartable && data === 2) {
                        label += `
                            <span style="cursor:pointer;color:#333" onclick="changeStatusTransfer('${row.id}', 1)" data-title-right="بازگشت">
                              <svg style="width:16px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="m15 15 6-6m0 0-6-6m6 6H9a6 6 0 0 0 0 12h3"></path></svg>
                            </span>
                        `;
                    }
                    return label;
                },
            },
            {
                data: "id",
                render: (data, _, row) => {
                    if (!isMdCartable && (row.status === 0 || row.status === 3)) return `
                        <div class="d-inline-flex align-items-center justify-content-center gap-1">
                            <button type="button" style="padding: 6px 6px;font-size: 12px" class="buttonGreen3 d-inline-flex shadow-none" onclick="editTransfer('${data}')"><i class="fa fa-pencil"></i></button>
                            <button type="button" style="padding: 6px 7px;font-size: 12px" class="buttonCancel2 d-inline-flex shadow-none" onclick="deleteTransfer('${data}')"><i class="fa fa-trash"></i></button>
                        </div>
                    `;
                    
                    return `
                        <button type="button" style="padding: 6px 7px;font-size: 12px" class="buttonGray d-inline-flex shadow-none" onclick="showAttachments('${data}', 12)">
                            <i class="fa fa-paperclip"></i>
                        </button>
                    `;
                },
                orderable: false,
            },
        ],
        createdRow: (row, data) => {
            if (data.transferType === 1) {
                row.classList.add("tableRow-bgGreen");
            } else if (data.transferType === 2) {
                row.classList.add("tableRow-bgRed");
            }
        },
        language: languageDatatable,
        layout: isMdCartable ? null : layoutDatatable,
        initComplete: () => {
            document.querySelectorAll("#transferTable_wrapper > div.row").forEach((row) => {
                row.classList.add("d-flex", "flex-wrap");
                row.classList.remove("row");
            });
        },
        
    });
}


const editTransfer = async (id) => {
    const data = await getData(`/Transfer/GetTransfer/${id}`);
    if (!data) return;
    
    transferData = data.transfer;
    transferAttachment = data.attachments;
    
    await loadTransferAccounts(transferData.transferType);
    mySer.convertJsonToForm(transferData, "transferForm");
    setTransferModalUI(transferData.transferType);
    
    FileUpReload("transferAttach");
    
    ModalToggle(transferModal);
};

const deleteTransfer = async (id) => {
    await deleteItem({
        url: `/Transfer/DeleteTransfer/${id}`,
        callback: () => {
            transferTbl.ajax.reload(null, false);
        },
    });
};

async function changeStatusTransfer(id, status) {
    try {
        FLshowLoading();
        await axios.patch(`/Transfer/ChangeTransferStatus/${id}`, status);
        
        transferTbl.ajax.reload(null, false);
        
    } catch (err) {
        console.log(err);
        const errorMessage = err.response?.data?.message || err.response?.data || "خطای سرور";
        toastifyError(errorMessage);
    } finally {
        FLshowLoading(false);
    }
}
