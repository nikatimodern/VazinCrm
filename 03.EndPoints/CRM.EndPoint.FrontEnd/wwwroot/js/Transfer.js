let transferTable;
let transferData = {};
var transferAttachment = [];

const transferModal = document.getElementById("transferModal");
const transferFields = {
    fromSelect: transferModal.querySelector("[name='fromAccountId']"),
    fromInput: transferModal.querySelector("[name='fromAccountTxt']"),
    toSelect: transferModal.querySelector("[name='toAccountId']"),
    toInput: transferModal.querySelector("[name='toAccountTxt']"),
    header: transferModal.querySelector(".modalHeader-invoice"),
};


document.addEventListener("DOMContentLoaded", async () => {
    getAllTransfersDt();
});

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
            transferTable.ajax.reload();
        }
    } catch (err) {
        console.log(err);
        toastifyError("خطای سرور");
    } finally {
        FLshowLoading(false);
    }
}

function getAllTransfersDt() {
    transferTable = $("#transferTable").DataTable({
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
        },
        columns: [
            { data: "transferNumber" },
            { data: "createDateTxt", render: data => `<span dir="ltr">${data.slice(0, -3).split(" ").join(" | ")}</span>` },
            {
                data: "amount",
                render: data => `<span class="direction-ltr">${data.toLocaleString()}</span>`,
            },
            { data: "fromAccountTxt" },
            { data: "toAccountTxt" },
            {
                data: "transferType",
                render: data => {
                    if (data === 1) return `<span class="badgeGreen d-inline-flex">واریز</span>`;
                    if (data === 2) return `<span class="badgeRed2">برداشت</span>`;
                    
                },
                orderable: false,
            },
            { data: "transferDate" },
            { data: "userCreator" },
            {
                data: "id",
                render: data => `
                    <div class="d-inline-flex align-items-center justify-content-center gap-1">
                        <button type="button" style="padding: 6px 6px;box-shadow: none;font-size: 12px" class="buttonGreen3 flex-center" onclick="editTransfer('${data}')"><i class="fa fa-pencil"></i></button>
                        <button type="button" style="padding: 6px 7px;box-shadow: none;font-size: 12px" class="buttonCancel2 flex-center" onclick="deleteTransfer('${data}')"><i class="fa fa-trash"></i></button>
                    </div>
                `,
                orderable: false,
            },
        ],
        language: languageDatatable,
        layout: layoutDatatable,
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
            transferTable.ajax.reload();
        },
    });
};