let transferType;
let transferTable;
var transferAttachment = [];
const transferModal = document.getElementById("transferModal");

async function showModalTransfer(type) {
    transferType = type;
    
    await getAllBankAccounts();
    
    if (transferType === 1) {
        transferModal.querySelector(".modalHeader-invoice").innerText = "ثبت واریز";
        transferModal.classList.add("deposit-mode");
        transferModal.classList.remove("backward-mode");
    } else if (transferType === 2) {
        transferModal.querySelector(".modalHeader-invoice").innerText = "ثبت برداشت";
        transferModal.classList.remove("deposit-mode");
        transferModal.classList.add("backward-mode");
    }
    ModalToggle(transferModal);
}

async function cancelTransferModal() {
    ModalToggle(transferModal, "hide");
    mySer.emptyForm("transferForm");
}

async function submitTransferModal() {
    if (!await mySer.CheckValidForm("transferForm")) return;
    if (transferAttachment.every(item => item.isDeleted)) {
        toastifyError("رسید پرداخت اجباری است");
        return;
    }
    const model = {
        ...mySer.convertFormToJSON("transferForm"),
        transferType,
        attachments: transferAttachment,
    };
    console.log(model);
}

function initTransferTable() {
    transferTable = $("#transferTable").DataTable({
        processing: true,
        serverSide: true,
        filter: true,
        stateSave: false,
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        responsive: true,
        ajax: {
            url: `/Transfer/GetAllTransfersDt`,
            dataSrc: "",
        },
        columns: [
            { data: "transferNumber", orderable: true },
            {
                data: "amount",
                orderable: true,
                render: data => `<span class="direction-ltr">${data.toLocaleString()}</span>`,
            },
            { data: "transferDateTxt" },
            { data: "fromAccountTxt" },
            { data: "toAccountTxt" },
            { data: "description" },
            {
                data: "id",
                render: data => `
                    <div class="d-inline-flex align-items-center justify-content-center gap-1">
                        <button type="button" style="padding: 6px 6px;box-shadow: none;font-size: 12px" class="buttonGreen3 flex-center" onclick="editTransfet('${data}')"><i class="fa fa-pencil"></i></button>
                        <button type="button" style="padding: 6px 7px;box-shadow: none;font-size: 12px" class="buttonCancel2 flex-center" onclick="deleteTransfer('${data}')"><i class="fa fa-trash"></i></button>
                    </div>
                `,
            },
        ],
        language: ,
        layout: null,
        initComplete: () => {
            document.querySelectorAll("#checksTable_wrapper > div.row").forEach((row) => {
                row.classList.add("d-flex", "flex-wrap");
                row.classList.remove("row");
            });
        },
    });
}