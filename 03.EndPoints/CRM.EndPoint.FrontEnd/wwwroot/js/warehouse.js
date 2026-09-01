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
const configWarehouseTransactionModal = (type) => {
    const isOut = type === 0;
    const color = isOut ? "#851b1d" : "var(--color8)";
    
    const titleElm = warehouseTransactionModal.querySelector(".modal-title");
    const contentElm = warehouseTransactionModal.querySelector(".modal-content");
    
    titleElm.textContent = isOut ? "خروج از انبار" : "ورود به انبار";
    titleElm.style.backgroundColor = color;
    contentElm.style.borderColor = color;
};
const showWarehouseInModal = () => {
    warehouseTransactionsData.transactionType = 1;
    
    configWarehouseTransactionModal(1);
    ModalToggle(warehouseTransactionModal, "show");
};
const showWarehouseOutModal = (data) => {
    warehouseTransactionsData.transactionType = 0;
    warehouseTransactionsData.parentId = data.id;
    
    const productElm = warehouseTransactionModal.querySelector("input[name='productTxt']");
    const unitElm = warehouseTransactionModal.querySelector("select[name='unit']");
    
    productElm.value = data.productTxt;
    unitElm.value = data.unit;
    
    toggleElementsDisabled(productElm, true);
    toggleElementsDisabled(unitElm, true);
    
    configWarehouseTransactionModal(0);
    ModalToggle(warehouseTransactionModal, "show");
};

function cancelWarehouseTransactionModal() {
    ModalToggle(warehouseTransactionModal, "hide");
    warehouseTransactionsData = { warehouseId: warehouseTransactionsData.warehouseId };
    mySer.emptyForm("warehouseTransactionForm");
    
    const productElm = warehouseTransactionModal.querySelector("input[name='productTxt']");
    const unitElm = warehouseTransactionModal.querySelector("select[name='unit']");
    toggleElementsDisabled(productElm, false);
    toggleElementsDisabled(unitElm, false);
}

const editWarehouseTransaction = async (data) => {
    warehouseTransactionsData = data;
    
    configWarehouseTransactionModal(data.transactionType);
    mySer.convertJsonToForm(data, "warehouseTransactionForm");
    ModalToggle(warehouseTransactionModal, "show");
};

const getWarehouseTransaction = async (id) => {
    return await sendApiRequest({
        url: "/Warehouse/GetWarehouseTransaction",
        params: { id },
    });
};


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
                orderable: false,
                render: (data, type, row) => `
                    <div class="inline-flex-center gap-1">
                        ${true || row.transactionType === 1 ? `
                            <button class="icon-button icon-button--orange" style="width: 32px" data-title-right="ثبت خروج از انبار" data-action="warehouse-out"><i class="fa fa-sign-out"></i>                            </button>
                        ` : ""}
                        <button class="icon-button icon-button--green" data-action="warehouse-edit"><i class="fa fa-pencil"></i></button>
                        <button class="icon-button icon-button--red" data-action="warehouse-delete"><i class="fa fa-trash"></i></button>
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
    
    document.querySelector("#warehouseTransactionsTable")?.addEventListener("click", async (e) => {
        const button = e.target.closest("[data-action]");
        if (!button) return;
        
        const rowElement = button.closest("tr");
        const data = warehouseTransactionTbl.row(rowElement).data();
        
        if (!data) return;
        
        switch (button.dataset.action) {
            case "warehouse-out":
                showWarehouseOutModal(data);
                break;
            
            case "warehouse-edit":
                await editWarehouseTransaction(data);
                break;
            
            case "warehouse-delete":
                await deleteWarehouseTransaction(data.id);
                break;
        }
    });
}

const deleteWarehouseTransaction = async (id) => {
    await deleteItem({
        url: `/Warehouse/DeleteWarehouseTransaction/${id}`,
        callback: () => {
            warehouseTransactionTbl.ajax.reload();
            GetAllWarehouses();
        },
    });
};