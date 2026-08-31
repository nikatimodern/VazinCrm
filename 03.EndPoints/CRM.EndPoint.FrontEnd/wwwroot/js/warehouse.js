let warehouseProductsData = {};
let warehouseProductTbl = null;

const warehouseModal = document.getElementById("warehouseModal");
const warehouseProductModal = document.getElementById("warehouseProductModal");
const warehouseFormElm = warehouseModal.querySelector("#warehouseForm");
const warehousesSection = document.getElementById("warehouses-section");
const warehouseProductsSection = document.getElementById("warehouseProducts-section");

window.addEventListener("popstate", syncWarehouseViewWithUrl);

document.addEventListener("DOMContentLoaded", () => {
    syncWarehouseViewWithUrl();
    Unit(3, "warehouseProductForm");
});

function syncWarehouseViewWithUrl() {
    const { warehouse, title } = getUrlParams();
    if (warehouse) {
        showWarehouseProducts(warehouse, decodeURIComponent(title || "انبار"));
    } else {
        warehousesSection.classList.remove("d-none");
        warehouseProductsSection.classList.add("d-none");
    }
}

warehouseModal?.addEventListener("hidden.bs.modal", cancelWarehouseModal);
warehouseProductModal?.addEventListener("hidden.bs.modal", cancelWarehouseProductModal);


function cancelWarehouseModal() {
    mySer.emptyForm("warehouseForm");
    ModalToggle(warehouseModal, "hide");
}

warehouseFormElm?.addEventListener("submit", async (e) => {
    e.preventDefault();
    if (!await mySer.CheckValidForm("warehouseForm")) return;
    
    await sendApiRequest({
        url: "/Warehouse/AddWarehouse",
        method: "POST",
        data: mySer.convertFormToJSON("warehouseForm"),
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


function backToWarehouses() {
    warehouseProductsData = {};
    setUrlParams({ warehouse: null, title: null });
    warehouseProductsSection.classList.add("d-none");
    warehousesSection.classList.remove("d-none");
}


function showWarehouseProducts(id, title) {
    warehousesSection.classList.add("d-none");
    warehouseProductsSection.querySelector("#selected-checkbook-title").innerText = "لیست محصولات " + title || "";
    warehouseProductsSection.classList.remove("d-none");
    
    warehouseProductsData.warehouseId = id;
    setUrlParams({ warehouse: id, title: title }, { replace: false });
    
    if (!warehouseProductTbl) {
        initWarehouseProduct(id);
    } else {
        warehouseProductTbl.clear().draw();
        warehouseProductTbl.ajax.url(`/Warehouse/GetWarehouseProductsDt?warehouseId=${id}`).load();
    }
}

function cancelWarehouseProductModal() {
    ModalToggle(warehouseProductModal, "hide");
    mySer.emptyForm("warehouseProductForm");
}

async function submitWarehouseProductModal() {
    if (!await mySer.CheckValidForm("warehouseProductForm")) return;
    const model = {
        ...warehouseProductsData,
        ...mySer.convertFormToJSON("warehouseProductForm"),
    };
    
    await sendApiRequest({
        url: "/Warehouse/AddWarehouseProduct",
        method: "POST",
        data: model,
        onSuccess: () => {
            cancelWarehouseProductModal();
            warehouseProductTbl.ajax.reload();
            GetAllWarehouses();
        },
    });
}

function initWarehouseProduct(id) {
    warehouseProductTbl = $("#warehouseProductsTable").DataTable({
        paging: false,
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        responsive: true,
        ordering: false,
        searching: false,
        ajax: {
            url: `/Warehouse/GetWarehouseProductsDt?warehouseId=${id}`,
        },
        columns: [
            { data: "productTxt" },
            { data: "weight" },
            { data: "unitTxt" },
            {
                data: "id",
                render: data => `
                    <div class="inline-flex-center gap-1">
                        <button type="button" style="padding: 6px 6px;box-shadow: none;font-size: 12px" class="buttonGreen3 flex-center" onclick="editWarehouseProduct('${data}')"><i class="fa fa-pencil"></i></button>
                        <button type="button" style="padding: 6px 7px;box-shadow: none;font-size: 12px" class="buttonCancel2 flex-center" onclick="deleteWarehouseProduct('${data}')"><i class="fa fa-trash"></i></button>
                    </div>
                `,
            },
        ],
        language: languageDatatable,
        layout: null,
        initComplete: () => {
            document.querySelectorAll("#warehouseProductsTable_wrapper > div.row").forEach((row) => {
                row.classList.add("d-flex", "flex-wrap");
                row.classList.remove("row");
            });
        },
    });
}


const editWarehouseProduct = async (id) => {
    warehouseProductsData = await getWarehouseProduct(id);
    if (!warehouseProductsData) return;
    
    mySer.convertJsonToForm(warehouseProductsData, "warehouseProductForm");
    
    ModalToggle(warehouseProductModal);
};

const getWarehouseProduct = async (id) => {
    return await sendApiRequest({
        url: "/Warehouse/GetWarehouseProduct",
        params: { id },
    });
};

const deleteWarehouseProduct = async (id) => {
    await deleteItem({
        url: `/Warehouse/DeleteWarehouseProduct/${id}`,
        callback: () => {
            warehouseProductTbl.ajax.reload();
            GetAllWarehouses();
        },
    });
};