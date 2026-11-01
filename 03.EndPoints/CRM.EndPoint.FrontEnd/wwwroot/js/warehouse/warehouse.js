let warehouseData = {};
let warehouseTransactionsData = {};
let warehouseProductsData = {};
let warehouseTransactionTbl = null;
let warehouseProductTbl = null;
let warehouseProductSelection;

const warehouseModal = document.getElementById("warehouseModal");
const warehouseTransactionModal = document.getElementById("warehouseTransactionModal");
const warehouseTransactionHistoryModal = document.getElementById("warehouseTransactionHistoryModal");
const warehouseFormElm = warehouseModal?.querySelector("#warehouseForm");
const warehousesSection = document.getElementById("warehouses-section");
const warehouseProductsSection = document.getElementById("warehouseProducts-section");

window.addEventListener("popstate", syncWarehouseViewWithUrl);

document.addEventListener("DOMContentLoaded", () => {
    syncWarehouseViewWithUrl();
    Unit(3, "warehouseTransactionForm");
    Unit(3, "warehouseProductForm");
});


async function syncWarehouseViewWithUrl() {
    const { warehouse, title } = getUrlParams();
    
    if (!warehouse) {
        warehouseData = {};
        warehousesSection.classList.remove("d-none");
        warehouseProductsSection.classList.add("d-none");
        return;
    }
    
    warehouseData = { id: warehouse, isMain: true };
    setWarehouseManualActions(false);
    await sendApiRequest({
        url: `/Warehouse/GetWarehouse/${encodeURIComponent(warehouse)}`,
        onSuccess: (res) => {
            if (warehouseData.id !== warehouse) return;
            warehouseData.isMain = res.data.isMain === true;
            setWarehouseManualActions(canManuallyManageWarehouse());
        },
    });
    if (warehouseData.id !== warehouse) return;
    
    warehouseProductsSection.querySelector("#selected-warehouse-title").innerText = "محصولات " + decodeURIComponent(title || "انبار");
    warehousesSection.classList.add("d-none");
    warehouseProductsSection.classList.remove("d-none");
    
    if (!warehouseProductTbl) {
        initWarehouseProductsTable(warehouse);
    } else {
        warehouseProductTbl.clear().draw();
        warehouseProductTbl.ajax.url(`/Warehouse/GetWarehouseProductsDt?warehouseId=${warehouse}`).load();
    }
    
    showLastWarehouseVouchers();
}

function canManuallyManageWarehouse() {
    return warehouseData.isMain === false || warehouseProductsSection?.dataset.isSeniorManager === "true";
}

function setWarehouseManualActions(allowed) {
    for (const id of ["warehouseProductBtn", "groupSelectWarehouseProductBtn"]) {
        document.getElementById(id)?.classList.toggle("d-none", !allowed);
    }
    document.getElementById("warehouseSystemNotice")?.classList.toggle("d-none", allowed);
    if (!allowed) document.getElementById("warehouseSelectionToolbar")?.classList.add("d-none");
}

warehouseModal?.addEventListener("hidden.bs.modal", cancelWarehouseModal);
warehouseTransactionModal?.addEventListener("hidden.bs.modal", cancelWarehouseTransactionModal);
warehouseTransactionHistoryModal?.addEventListener("hidden.bs.modal", () => warehouseProductsData = {});

function backToWarehouses() {
    history.back();
}


async function showWarehouseModal() {
    ModalToggle(warehouseModal, "show");
    await initWarehouseUsers();
}
function cancelWarehouseModal() {
    warehouseData = {};
    mySer.emptyForm("warehouseForm");
}


let warehouseUsersPromise = null;
async function initWarehouseUsers() {
    const select = warehouseModal.querySelector("#warehouseUsers");
    if (!warehouseUsersPromise) {
        FLshowLoading();
        warehouseUsersPromise = CrmSer.getUsers(1);
    }
    
    const users = await warehouseUsersPromise;
    FLshowLoading(false);
    
    if (select.tomselect) return;
    
    new TomSelect(select, {
        options: users,
        valueField: "id",
        selectOnTab: true,
        placeholder: "انتخاب کاربران",
    });
}


warehouseFormElm?.addEventListener("submit", async (e) => {
    e.preventDefault();
    if (!await mySer.CheckValidForm("warehouseForm")) return;
    
    const model = {
        ...warehouseData,
        ...mySer.convertFormToJSON("warehouseForm"),
        isMain: warehouseFormElm.querySelector("#warehouseIsMain")?.checked ?? false,
    };
    
    await sendApiRequest({
        url: "/Warehouse/AddWarehouse",
        method: "POST",
        data: model,
        onSuccess: async () => {
            ModalToggle(warehouseModal, "hide");
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
    await showWarehouseModal();
    
    await sendApiRequest({
        url: `/Warehouse/GetWarehouse/${id}`,
        onSuccess: (res) => {
            warehouseData = res.data;
            mySer.convertJsonToForm(warehouseData, "warehouseForm");
            const mainCheckbox = warehouseFormElm.querySelector("#warehouseIsMain");
            if (mainCheckbox) mainCheckbox.checked = warehouseData.isMain === true;
        },
    });
}


document.addEventListener("click", (e) => {
    const card = e.target.closest(".clickable-card");
    if (!card) return;
    
    if (e.target.closest(".clickable-card__toggle")) return;
    const { id, warehouseTitle: title } = card.dataset;
    if (!id) return;
    
    showWarehouseProducts(id, title);
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

/* ----------------------------------------------------------- */
function showWarehouseProducts(id, title) {
    setUrlParams({ warehouse: id, title: title }, { replace: false });
    syncWarehouseViewWithUrl();
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

const setWarehouseTransactionModal = (data) => {
    if (!data) return;
    
    mySer.convertJsonToForm(data, "warehouseTransactionForm");
    
    const disableElms = warehouseTransactionModal.querySelectorAll("input[name='productDescription'], select[name='productCode'], select[name='unit']");
    toggleElementsDisabled(disableElms, true);
};

const openWarehouseTransactionModal = async (type) => {
    await getAllProductOp("warehouseTransactionForm");
    configWarehouseTransactionModal(type);
    ModalToggle(warehouseTransactionModal, "show");
};

const showWarehouseTransactionModal = async (type, data) => {
    warehouseTransactionsData.transactionType = type;
    await openWarehouseTransactionModal(type);
    if (data) {
        warehouseTransactionsData.warehouseProductId = data.id;
        setWarehouseTransactionModal(data);
    }
};


const editWarehouseTransaction = async (id) => {
    warehouseTransactionsData = await getWarehouseTransaction(id);
    
    const data = {
        ...warehouseProductsData,
        ...warehouseTransactionsData,
    };
    
    await openWarehouseTransactionModal(data.transactionType);
    setWarehouseTransactionModal(data);
};

const getWarehouseTransaction = async (id) => {
    return await sendApiRequest({
        url: "/Warehouse/GetWarehouseTransaction",
        params: { id },
    });
};

function cancelWarehouseTransactionModal() {
    warehouseTransactionsData = {};
    mySer.emptyForm("warehouseTransactionForm");
    
    const disableElms = warehouseTransactionModal.querySelectorAll("input[name='productDescription'], select[name='productCode'], select[name='unit']");
    toggleElementsDisabled(disableElms, false);
}


async function submitWarehouseTransactionModal() {
    if (!await mySer.CheckValidForm("warehouseTransactionForm")) return;
    
    const model = {
        ...warehouseTransactionsData,
        ...mySer.convertFormToJSON("warehouseTransactionForm"),
        warehouseId: warehouseData.id,
        productSmallName: warehouseTransactionModal.querySelector("select[name='productCode']")?.selectedOptions[0].text,
    };
    
    await sendApiRequest({
        url: model.id ? "/Warehouse/UpdateWarehouseTransaction" : "/Warehouse/AddWarehouseTransaction",
        method: "POST",
        data: model,
        onSuccess: (res) => {
            ModalToggle(warehouseTransactionModal, "hide");
            warehouseTransactionTbl?.ajax.reload();
            warehouseProductTbl?.ajax.reload();
            GetAllWarehouses();
            if (model.id && res.data?.stock !== undefined) {
                warehouseProductsData.stock = res.data?.stock;
                setWarehouseTransactionHistoryHeader();
            }
        },
        onError: async (err) => {
            const status = err.response?.status;
            
            if (status === 409) {
                await showAlert({
                    title: "امکان انجام عملیات نیست",
                    text: err.response.data,
                    icon: "error",
                });
                return;
            }
            await handleApiError(err);
        },
        showError: false,
    });
}

function initWarehouseProductsTable(id) {
    warehouseProductTbl = $("#warehouseProductsTable").DataTable({
        lengthMenu: [5, 10, 20, { label: "همه", value: -1 }],
        pageLength: 5,
        // paging: false,
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
            url: `/Warehouse/GetWarehouseProductsDt?warehouseId=${id}`,
            type: "POST",
            error: handleApiError,
        },
        columnDefs: [
            { targets: "_all", defaultContent: "-" },
        ],
        columns: [
            { data: "productSmallName" },
            { data: "productDescription" },
            { data: "stock", className: "direction-ltr" }, // موجودی
            { data: "unitTxt" },
            { data: "userCreator" },
            {
                data: "id",
                orderable: false,
                render: (data, type, row) => !canManuallyManageWarehouse()
                    ? `<button class="icon-button icon-button--gray" data-title-left="تاریخچه گردش" data-action="warehouse-history">
                        <svg style="width: 18px !important" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"></path><path d="M3 3v5h5"></path><path d="M12 7v5l4 2"></path></svg>
                      </button>`
                    : `
                    <div class="inline-flex-center gap-2">
                        ${row.stock > 0 ? `
                            <button class="icon-button icon-button--blue" style="width: 32px" data-title-right="ثبت حواله انبار" data-action="warehouse-out">
                              <svg style="width: 18px !important" viewBox="0 0 20 20" fill="currentColor"><path d="M9.25 13.25a.75.75 0 0 0 1.5 0V4.636l2.955 3.129a.75.75 0 0 0 1.09-1.03l-4.25-4.5a.75.75 0 0 0-1.09 0l-4.25 4.5a.75.75 0 1 0 1.09 1.03L9.25 4.636v8.614Z" /><path d="M3.5 12.75a.75.75 0 0 0-1.5 0v2.5A2.75 2.75 0 0 0 4.75 18h10.5A2.75 2.75 0 0 0 18 15.25v-2.5a.75.75 0 0 0-1.5 0v2.5c0 .69-.56 1.25-1.25 1.25H4.75c-.69 0-1.25-.56-1.25-1.25v-2.5Z" /></svg>
                            </button>
                        ` : ""}
                        <button class="icon-button icon-button--green" style="width: 32px" data-title-right="ورود به انبار" data-action="warehouse-in">
                            <svg style="width: 18px !important" viewBox="0 0 20 20" fill="currentColor"><path d="M10.75 2.75a.75.75 0 0 0-1.5 0v8.614L6.295 8.235a.75.75 0 1 0-1.09 1.03l4.25 4.5a.75.75 0 0 0 1.09 0l4.25-4.5a.75.75 0 0 0-1.09-1.03l-2.955 3.129V2.75Z" /><path d="M3.5 12.75a.75.75 0 0 0-1.5 0v2.5A2.75 2.75 0 0 0 4.75 18h10.5A2.75 2.75 0 0 0 18 15.25v-2.5a.75.75 0 0 0-1.5 0v2.5c0 .69-.56 1.25-1.25 1.25H4.75c-.69 0-1.25-.56-1.25-1.25v-2.5Z" /></svg>
                        </button>
                        <span class="vr"></span>
                        <button class="icon-button icon-button--gray" style="width: 32px" data-title-left="تاریخچه گردش" data-action="warehouse-history">
                            <svg style="width: 18px !important" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"></path><path d="M3 3v5h5"></path><path d="M12 7v5l4 2"></path></svg>
                        </button>
                    </div>
                `,
            },
        ],
        language: languageDatatable,
        layout: layoutDatatable,
        initComplete: () => {
            document.querySelectorAll("#warehouseProductsTable_wrapper > div.row").forEach((row) => {
                row.classList.add("d-flex", "flex-wrap");
                row.classList.remove("row");
            });
        },
    });
    
    document.querySelector("#warehouseProductsTable")?.addEventListener("click", async (e) => {
        const button = e.target.closest("[data-action]");
        if (!button) return;
        
        const rowElement = button.closest("tr");
        const data = warehouseProductTbl.row(rowElement).data();
        
        if (!data) return;
        switch (button.dataset.action) {
            case "warehouse-in":
                await showWarehouseTransactionModal(1, data);
                break;
            
            case "warehouse-out":
                // await showWarehouseTransactionModal(0, data);
                await showWarehouseVoucherModal([data.id]);
                break;
            
            case "warehouse-history":
                showWarehouseTransactionHistory(data);
                break;
        }
    });
    
    warehouseProductSelection = new SelectionManager({
        container: "#warehouseProductsTable tbody",
        selectionBar: "#warehouseSelectionToolbar",
        toggleBtn: "#groupSelectWarehouseProductBtn",
        itemSelector: "tr:not(:has(td.dt-empty)):not(.child)",
        selectedClass: "row-selected",
        checkboxSelector: null,
        getItemKey: item => warehouseProductTbl.row(item).data()?.id,
        isSelectable: item => {
            const data = warehouseProductTbl.row(item).data();
            return canManuallyManageWarehouse() && data?.stock > 0;
        },
        onAction: async (ids) => {
            await showWarehouseVoucherModal(ids);
        },
        
        onSelectionModeEnter() {
            document.querySelectorAll("#warehouseProducts-section .section-actions button").forEach(el => el.classList.add("d-none"));
            document.querySelector("#warehouseProducts-section .warehouse-vouchers-section").classList.add("d-none");
        },
        
        onSelectionModeExit() {
            document.querySelectorAll("#warehouseProducts-section .section-actions button").forEach(el => el.classList.remove("d-none"));
            document.querySelector("#warehouseProducts-section .warehouse-vouchers-section").classList.remove("d-none");
        },
    });
    
    warehouseProductTbl.on("draw.dt", () => warehouseProductSelection.refresh());
}


function showLastWarehouseVouchers() {
    sendApiRequest({
        url: "/Warehouse/GetLastWarehouseVouchers",
        params: { warehouseId: warehouseData.id },
        showLoading: false,
    }).then(data => {
        document.getElementById("lastWarehouseVouchersGrid").innerHTML = data;
    });
}

function showWarehouseVouchersPage() {
    goTo(`/Warehouse/Voucher?warehouseId=${warehouseData.id}`, true);
}

/* ----------------------------------------------------------- */
const showWarehouseTransactionHistory = (data) => {
    warehouseProductsData = data;
    setWarehouseTransactionHistoryHeader();
    
    ModalToggle(warehouseTransactionHistoryModal, "show");
    
    if (!warehouseTransactionTbl) {
        initWarehouseTransaction(data.id);
    } else {
        warehouseTransactionTbl.ajax.url(`/Warehouse/GetWarehouseTransactionsDt?productId=${data.id}`).load();
    }
};

const editWarehouseProduct = async () => {
    await getAllProductOp("warehouseProductForm");
    await sendApiRequest({
        url: "/Warehouse/GetWarehouseProduct",
        params: { id: warehouseProductsData.id },
        onSuccess: (res) => {
            warehouseProductsData = { ...warehouseProductsData, ...res.data };
            ModalToggle("warehouseProductModal");
            mySer.convertJsonToForm(res.data, "warehouseProductForm");
        },
    });
};


const deleteWarehouseProduct = async () => {
    await deleteItem({
        url: `/Warehouse/DeleteWarehouseProduct/${warehouseProductsData.id}`,
        callback: () => {
            ModalToggle(warehouseTransactionHistoryModal, "hide");
            warehouseProductTbl?.ajax.reload(null, false);
            GetAllWarehouses();
        },
    });
};

async function submitWarehouseProductModal() {
    if (!await mySer.CheckValidForm("warehouseProductForm")) return;
    
    const model = {
        ...warehouseProductsData,
        ...mySer.convertFormToJSON("warehouseProductForm"),
        productSmallName: document.querySelector("#warehouseProductForm select[name='productCode']")?.selectedOptions[0].text,
    };
    
    await sendApiRequest({
        url: "/Warehouse/UpdateWarehouseProduct",
        method: "POST",
        data: model,
        onSuccess: (res) => {
            ModalToggle("warehouseProductModal", "hide");
            mySer.emptyForm("warehouseProductForm");
            warehouseProductsData = { ...warehouseProductsData, ...res.data };
            setWarehouseTransactionHistoryHeader();
            warehouseProductTbl?.ajax.reload();
        },
    });
}


function initWarehouseTransaction(id) {
    warehouseTransactionTbl = $("#warehouseTransactionTable").DataTable({
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
            url: `/Warehouse/GetWarehouseTransactionsDt?productId=${id}`,
            type: "POST",
        },
        columnDefs: [
            { targets: "_all", defaultContent: "-" },
        ],
        columns: [
            { data: "transactionType", render: data => data === 1 ? "ورود" : "خروج" },
            { data: "weight", className: "direction-ltr", render: (data, type, row) => row.transactionType === 1 ? `+${data}` : `-${data}` },
            { data: "createDateTxt", name: "createDateTxt", render: renderDateTimeHtml },
            { data: "userCreator" },
            {
                data: "id",
                orderable: false,
                render: (data, type, row) => `
                    <div class="inline-flex-center gap-1">
                    ${canManuallyManageWarehouse() && row.transactionType === 1 ? `
                        <button class="icon-button icon-button--green" data-action="warehouse-edit" onclick="editWarehouseTransaction('${data}')"><i class="fa fa-pencil"></i></button>
                        <button class="icon-button icon-button--red" data-action="warehouse-delete" onclick="deleteWarehouseTransaction('${data}')"><i class="fa fa-trash"></i></button>
                    ` : ""}
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

const deleteWarehouseTransaction = async (id) => {
    await deleteItem({
        url: `/Warehouse/DeleteWarehouseTransaction/${id}`,
        callback: (res) => {
            warehouseTransactionTbl?.ajax.reload(null, false);
            warehouseProductTbl?.ajax.reload(null, false);
            if (res.data?.stock !== undefined) {
                warehouseProductsData.stock = res.data?.stock;
                setWarehouseTransactionHistoryHeader();
            }
        },
    });
};


const setWarehouseTransactionHistoryHeader = () => {
    document.getElementById("transactionHistoryProductTitle").textContent =
        `${warehouseProductsData.productSmallName || ""} ${warehouseProductsData.productDescription || ""}`.trim() || "-";
    document.getElementById("transactionHistoryStock").textContent =
        (`${warehouseProductsData.stock?.toLocaleString() ?? "0"} ${warehouseProductsData.unitTxt || ""}`);
    
    document.querySelectorAll('#warehouseTransactionHistoryModal [onclick="editWarehouseProduct()"], #deleteWarehouseProductBtn')
        .forEach(button => button.classList.toggle("d-none", !canManuallyManageWarehouse()));
    const deleteBtn = document.getElementById("deleteWarehouseProductBtn");
    deleteBtn.classList.toggle("opacity-25", warehouseProductsData.stock > 0);
    toggleElementsDisabled(deleteBtn, warehouseProductsData.stock > 0);
};

