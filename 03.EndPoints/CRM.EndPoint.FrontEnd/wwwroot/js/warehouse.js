let warehouseData = {};
let warehouseTransactionsData = {};
let warehouseTransactionTbl = null;

const warehouseModal = document.getElementById("warehouseModal");
const warehouseTransactionModal = document.getElementById("warehouseTransactionModal");
const warehouseFormElm = warehouseModal.querySelector("#warehouseForm");
const warehousesSection = document.getElementById("warehouses-section");
const warehouseTransactionsSection = document.getElementById("warehouseTransactions-section");

window.addEventListener("popstate", syncWarehouseViewWithUrl);

document.addEventListener("DOMContentLoaded", () => {
    syncWarehouseViewWithUrl();
    Unit(3, "warehouseTransactionForm");
});

function syncWarehouseViewWithUrl() {
    const { warehouse, title } = getUrlParams();
    if (warehouse) {
        showWarehouseTransactions(warehouse, decodeURIComponent(title || "انبار"));
    } else {
        warehousesSection.classList.remove("d-none");
        warehouseTransactionsSection.classList.add("d-none");
    }
}

warehouseModal?.addEventListener("hidden.bs.modal", cancelWarehouseModal);
warehouseTransactionModal?.addEventListener("hidden.bs.modal", cancelWarehouseTransactionModal);


function cancelWarehouseModal() {
    warehouseData = {};
    mySer.emptyForm("warehouseForm");
    ModalToggle(warehouseModal, "hide");
}

warehouseFormElm?.addEventListener("submit", async (e) => {
    e.preventDefault();
    if (!await mySer.CheckValidForm("warehouseForm")) return;
    
    const model = {
        ...warehouseData,
        ...mySer.convertFormToJSON("warehouseForm"),
    };
    
    await sendApiRequest({
        url: "/Warehouse/AddWarehouse",
        method: "POST",
        data: model,
        onSuccess: async () => {
            cancelWarehouseModal();
            await GetAllWarehouses();
        },
    });
});

const GetAllWarehouses = async () => {
    await sendApiRequest({
        url: "/Warehouse/GetAllWarehousesPartial",
        onSuccess: (res) => {
            document.getElementById("warehousesContainer").innerHTML = res.data;
        },
    });
};

async function deleteWarehouse(id) {
    warehouseDropdown.close();
    await deleteItem({
        url: `/Warehouse/DeleteWarehouse/${id}`,
        callback: GetAllWarehouses,
    });
}

async function editWarehouse(id) {
    warehouseDropdown.close();
    await sendApiRequest({
        url: `/Warehouse/GetWarehouse/${id}`,
        onSuccess: (res) => {
            warehouseData = res.data;
            mySer.convertJsonToForm(warehouseData, "warehouseForm");
            ModalToggle(warehouseModal, "show");
        },
    });
}


document.addEventListener("click", (e) => {
    const card = e.target.closest(".clickable-card");
    if (!card) return;
    
    if (e.target.closest(".clickable-card__toggle")) return;
    const { id, warehouseTitle: title } = card.dataset;
    if (!id) return;
    
    showWarehouseTransactions(id, title);
});

const warehouseDropdown = Dropdown.setupDelegate({
    container: document,
    toggleSelector: ".clickable-card__toggle",
    options: {
        transitionDuration: 200,
        originalDisplay: "flex",
        effect: "zoom",
        placement: "right",
    },
});


function backToWarehouses() {
    warehouseTransactionsData = {};
    setUrlParams({ warehouse: null, title: null });
    warehouseTransactionsSection.classList.add("d-none");
    warehousesSection.classList.remove("d-none");
}

/* ----------------------------------------------------------- */
function showWarehouseTransactions(id, title) {
    warehousesSection.classList.add("d-none");
    warehouseTransactionsSection.querySelector("#selected-warehouse-title").innerText = "گردش انبار " + title || "";
    warehouseTransactionsSection.classList.remove("d-none");
    
    warehouseTransactionsData.warehouseId = id;
    setUrlParams({ warehouse: id, title: title }, { replace: false });
    
    if (!warehouseTransactionTbl) {
        initWarehouseTransaction(id);
    } else {
        warehouseTransactionTbl.clear().draw();
        warehouseTransactionTbl.ajax.url(`/Warehouse/GetWarehouseTransactionsDt?warehouseId=${id}`).load();
    }
}

function showWarehouseTransactionModal(type) {
    warehouseTransactionsData.transactionType = type;
    const titleElm = warehouseTransactionModal.querySelector(".modal-title");
    const contentElm = warehouseTransactionModal.querySelector(".modal-content");
    
    titleElm.textContent = type === 1 ? "ورود به انبار" : "خروج از انبار";
    titleElm.style.backgroundColor = type === 1 ? "var(--color8)" : "#851b1d";
    contentElm.style.borderColor = type === 1 ? "var(--color8)" : "#851b1d";
    
    ModalToggle(warehouseTransactionModal, "show");
}
function cancelWarehouseTransactionModal() {
    warehouseTransactionsData = {};
    ModalToggle(warehouseTransactionModal, "hide");
    mySer.emptyForm("warehouseTransactionForm");
}

async function submitWarehouseTransactionModal() {
    if (!await mySer.CheckValidForm("warehouseTransactionForm")) return;
    const model = {
        ...warehouseTransactionsData,
        ...mySer.convertFormToJSON("warehouseTransactionForm"),
    };
    
    await sendApiRequest({
        url: "/Warehouse/AddWarehouseTransaction",
        method: "POST",
        data: model,
        onSuccess: () => {
            cancelWarehouseTransactionModal();
            warehouseTransactionTbl.ajax.reload();
            GetAllWarehouses();
        },
    });
}

function initWarehouseTransaction(id) {
    warehouseTransactionTbl = $("#warehouseTransactionsTable").DataTable({
        // lengthMenu: [10, 25, 50, 100, { label: "همه", value: -1 }],
        // pageLength: 10,
        paging: false,
        processing: true,
        serverSide: true,
        search: {
            return: true,
        },
        ordering: true,
        order: { name: "createDateTxt", dir: "desc" },
        responsive: true,
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        ajax: {
            url: `/Warehouse/GetWarehouseTransactionsDt?warehouseId=${id}`,
            type: "POST",
        },
        columnDefs: [
            { targets: "_all", defaultContent: "-" },
        ],
        columns: [
            { data: "productTxt" },
            { data: "weight", className: "direction-ltr", render: (data, type, row) => row.transactionType === 1 ? `+${data}` : `-${data}` },
            { data: "unitTxt" },
            { data: "createDateTxt", name: "createDateTxt", render: renderDateTimeHtml },
            { data: "transactionType", render: data => data === 1 ? "ورود" : "خروج" },
            {
                data: "id",
                render: data => `
                    <div class="inline-flex-center gap-1">
                        <button type="button" style="width: 25px;font-size: 12px" class="buttonGreen3 flex-center shadow-none" onclick="editWarehouseTransaction('${data}')"><i class="fa fa-pencil"></i></button>
                        <button type="button" style="width: 25px;font-size: 12px" class="buttonCancel2 flex-center shadow-none" onclick="deleteWarehouseTransaction('${data}')"><i class="fa fa-trash"></i></button>
                    </div>
                `,
            },
        ],
        language: languageDatatable,
        layout: null,
        createdRow: (row, data) => {
            if (data.transactionType === 1) {
                row.classList.add("tableRow-bgGreen");
            } else if (data.transactionType === 0) {
                row.classList.add("tableRow-bgRed");
            }
        },
        initComplete: () => {
            document.querySelectorAll("#warehouseTransactionsTable_wrapper > div.row").forEach((row) => {
                row.classList.add("d-flex", "flex-wrap");
                row.classList.remove("row");
            });
        },
    });
}


const editWarehouseTransaction = async (id) => {
    warehouseTransactionsData = await getWarehouseTransaction(id);
    if (!warehouseTransactionsData) return;
    
    mySer.convertJsonToForm(warehouseTransactionsData, "warehouseTransactionForm");
    
    ModalToggle(warehouseTransactionModal);
};

const getWarehouseTransaction = async (id) => {
    return await sendApiRequest({
        url: "/Warehouse/GetWarehouseTransaction",
        params: { id },
    });
};

const deleteWarehouseTransaction = async (id) => {
    await deleteItem({
        url: `/Warehouse/DeleteWarehouseTransaction/${id}`,
        callback: () => {
            warehouseTransactionTbl.ajax.reload();
            GetAllWarehouses();
        },
    });
};