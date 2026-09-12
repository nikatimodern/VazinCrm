const warehouseVoucherState = { page: 1 };
let warehouseVoucherId;


const warehouseVoucherModal = document.getElementById("warehouseVoucherModal");

warehouseVoucherModal?.addEventListener("hidden.bs.modal", cancelWarehouseVoucherModal);


async function loadWarehouseVouchers(page = 1) {
    const search = document.getElementById("WarehouseVoucherSearchId").value.trim();
    const status = Number(document.querySelector(`input[name="WarehouseVoucherFilter"]:checked`).value);
    
    const { warehouseId } = getUrlParams();
    const data = await sendApiRequest({
        url: "/Warehouse/GetWarehouseVouchers",
        method: "POST",
        successMsg: null,
        params: { warehouseId },
        data: {
            page,
            search,
            status: status !== -1 ? status : null,
        },
    });
    
    document.getElementById("voucherGridContainer").innerHTML = data.html;
    document.getElementById("totalCount").innerHTML = data.totalCount;
    document.getElementById("vouchersFrom").innerHTML = data.displayFrom;
    document.getElementById("vouchersTo").innerHTML = data.displayTo;
}


async function refreshWarehouseVouchers() {
    const currentPath = window.location.pathname.toLowerCase();
    if (currentPath.includes("voucherdetail")) {
        location.reload();
    } else if (currentPath.includes("voucher")) {
        await loadWarehouseVouchers(warehouseVoucherState.page);
    } else {
        warehouseProductTbl?.ajax.reload(null, false);
        showLastWarehouseVouchers();
    }
}

let warehouseProductListData = [];
async function showWarehouseVoucherModal(filterIds) {
    await sendApiRequest({
        method: "POST",
        url: "/Warehouse/GetWarehouseProductByFilteredIds",
        data: { filterIds },
        successMsg: null,
        onSuccess: (res) => {
            warehouseProductListData = res.data;
            
            createSetVoucherItem();
            ModalToggle(warehouseVoucherModal, "show");
            warehouseProductSelection.hideSelectionMode();
        },
    });
}


async function editWarehouseVoucher(id) {
    await sendApiRequest({
        url: "/Warehouse/GetWarehouseVoucher",
        params: { id },
        onSuccess: (res) => {
            warehouseVoucherId = id;
            warehouseProductListData = res.data;
            
            createSetVoucherItem();
            
            ModalToggle(warehouseVoucherModal, "show");
        },
    });
}

function cancelWarehouseVoucherModal() {
    mySer.emptyForm("warehouseVoucherForm");
    warehouseVoucherId = undefined;
}

document.getElementById("warehouseVoucherForm").addEventListener("keydown", function (e) {
    if (e.key === "Enter") {
        e.preventDefault();
    }
});

function createSetVoucherItem() {
    const container = warehouseVoucherModal.querySelector("#warehouseVoucherForm");
    container.innerHTML = "";
    
    warehouseProductListData.forEach((item, index) => {
        const fieldset = document.createElement("fieldset");
        
        fieldset.id = `setWarehouseVoucherItem-${index}`;
        fieldset.dataset.id = item.warehouseProductId;
        
        fieldset.innerHTML = `
            <div class="bgLightBlue border rounded-2 overflow-hidden">
                <div class="voucher-item-grid p-2">
                    <div class="voucher-item-field">
                        <span class="voucher-item-label">محصول:</span>
                        <span class="fbold voucher-item-value">${item.productSmallName || ""} ${item.productDescription || ""}</span>
                    </div>
                    
                    <div class="voucher-item-field">
                        <span class="voucher-item-label">موجودی:</span>
                        <span class="fbold voucher-item-value">${item.stock ?? "-"} ${item.unitTxt || ""}</span>
                    </div>
                    
                    <div class="voucher-item-field">
                        <span class="voucher-item-label">مقدار:</span>
                        <input type="text" inputmode="numeric" class="currencyNumber form-control inpAmount fbold" style="border: 1px solid #b5b3b3;height: 26px;"                            name="weight"
                            value="${item.weight ? item.weight.toLocaleString() : ""}" required>
                    </div>
                    
                    <div class="voucher-item-action">
                        <button type="button" class="inline-flex-center remove-voucher-item">
                            <svg style="width: 18px; color: darkred" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 0 0 6 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 1 0 .23 1.482l.149-.022.841 10.518A2.75 2.75 0 0 0 7.596 19h4.807a2.75 2.75 0 0 0 2.742-2.53l.841-10.52.149.023a.75.75 0 0 0 .23-1.482A41.03 41.03 0 0 0 14 4.193V3.75A2.75 2.75 0 0 0 11.25 1h-2.5ZM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4ZM8.58 7.72a.75.75 0 0 0-1.5.06l.3 7.5a.75.75 0 1 0 1.5-.06l-.3-7.5Zm4.34.06a.75.75 0 1 0-1.5-.06l-.3 7.5a.75.75 0 1 0 1.5.06l-.3-7.5Z" clip-rule="evenodd"></path></svg>
                        </button>
                    </div>
                </div>
            </div>

            ${index !== warehouseProductListData.length - 1 ? `<div style="width: 95%; height: 1px; margin: 10px auto; background-color: #8989ff"></div>` : ""}
        `;
        
        fieldset.querySelector(".remove-voucher-item")?.addEventListener("click", () => {
            if (warehouseProductListData.length === 1) {
                toastifyError("حداقل یک محصول باید در حواله باشد!");
                return;
            }
            warehouseProductListData = warehouseProductListData.filter(x => x.warehouseProductId !== item.warehouseProductId);
            createSetVoucherItem();
        });
        
        container.appendChild(fieldset);
    });
}
async function submitWarehouseVoucherModal() {
    if (!await mySer.CheckValidForm("warehouseVoucherForm")) return;
    
    const allItemsArray = Array.from(warehouseVoucherModal.querySelectorAll("fieldset"));
    
    const warehouseVoucherListData = allItemsArray.map(item => {
        const weightValue = item.querySelector("input[name='weight']").value;
        return {
            // warehouseVoucherId: paymentData.id,
            warehouseProductId: item.dataset.id,
            weight: Number(SeparatedNumberRemove(weightValue)),
        };
    });
    
    const model = {
        id: warehouseVoucherId,
        warehouseProductVouchers: warehouseVoucherListData,
    };
    
    const isEdit = Boolean(warehouseVoucherId);
    await sendApiRequest({
        url: isEdit ? "/Warehouse/EditWarehouseVoucher" : "/Warehouse/AddWarehouseVoucher",
        method: "POST",
        data: model,
        successMsg: null,
        onSuccess: async () => {
            ModalToggle(warehouseVoucherModal, "hide");
            if (isEdit) {
                toastifySuccess("حواله انبار با موفقیت ویرایش شد.");
            } else {
                toastifySuccess(`
                    حواله انبار با موفقیت ثبت شد.
                    <a href="/Warehouse/Voucher" target="_blank" class="text-decoration-underline ms-2 text-white">
                        صفحه حواله ها
                    </a>
                `, 6000);
            }
            await refreshWarehouseVouchers();
        },
        onError: async (err) => {
            FLshowLoading(false);
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
    });
}


async function confirmWarehouseVoucher(id) {
    await confirmAlert({
        icon: "question",
        confirmText: "بله، تایید شود",
        onConfirm: async () => await sendApiRequest({
            url: "/Warehouse/ConfirmWarehouseVoucher",
            method: "POST",
            params: { id },
            successMsg: "حواله با موفقیت تایید شد.",
            onSuccess: refreshWarehouseVouchers,
        }),
    });
}

async function deliverWarehouseVoucher(id) {
    await confirmAlert({
        icon: "question",
        confirmText: "بله، تحویل شود",
        onConfirm: async () => await sendApiRequest({
            url: "/Warehouse/DeliverWarehouseVoucher",
            method: "POST",
            params: { id },
            successMsg: "حواله با موفقیت تحویل شد.",
            onSuccess: refreshWarehouseVouchers,
        }),
        onError: async (err) => {
            FLshowLoading(false);
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
    });
}

const deleteWarehouseVoucher = async (id) => {
    await deleteItem({
        url: `/Warehouse/DeleteWarehouseVoucher/${id}`,
        callback: refreshWarehouseVouchers,
    });
};