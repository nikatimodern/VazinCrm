let submitOrderPfId;
let submitOrderCusId;
let submitOrderDealId;
let submitOrderDealSerialNumber;
let submitOrderType;
let pfItemsGroupedData = [];
let totalStepCreateOrderModal;
let currentStepCreateOrderModal = 1;
let newTabCount = 1;

const showModalCreateOrder = async (type, pfId, cusId, dealId, dealSerialNumber) => {
    submitOrderPfId = pfId;
    submitOrderCusId = cusId;
    submitOrderDealId = dealId;
    submitOrderDealSerialNumber = dealSerialNumber;
    submitOrderType = type;
    
    FLshowLoading();
    const data = (await axios(`/Order/GetHeadOrdersByPfId?pfId=${pfId}`)).data;
    
    pfItemsGroupedData = data.orderHeads;
    pfItemsNotInOrders = data.pfItemsNotInOrders;
    
    totalStepCreateOrderModal = pfItemsGroupedData.length;
    newTabCount = 1;
    generateCreateOrderTabs();
    await goToStepCreateOrderModal(1);
    
    document.querySelectorAll(".addFromPreFactorBtn").forEach(elm => elm.style.display = pfItemsNotInOrders.length ? "flex" : "none");
    FLshowLoading(false);
    
    $("#CreateOrderModal").modal("show");
};

const cancelModalCreateOrder = () => {
    pfItemsGroupedData = [];
    $("#CreateOrderModal").modal("hide");
};

const nextStepCreateOrderModal = async () => {
    if (currentStepCreateOrderModal === totalStepCreateOrderModal) {
        await submitOrder();
        return;
    }
    
    await goToStepCreateOrderModal(currentStepCreateOrderModal + 1);
    /*const nextTab = new bootstrap.Tab(document.getElementById(`CreateOrderTabs${currentStepCreateOrderModal}`));
    nextTab.show();*/
};

const prevStepCreateOrderModal = async () => {
    cancelModalCreateOrder();
    /* if (currentStepCreateOrderModal === 1) {
         cancelModalCreateOrder();
     } else {
         await goToStepCreateOrderModal(currentStepCreateOrderModal - 1);
         const prevTab = new bootstrap.Tab(document.getElementById(`CreateOrderTabs${currentStepCreateOrderModal}`));
         prevTab.show();
     }*/
};

const goToStepCreateOrderModal = async (step) => {
    currentStepCreateOrderModal = step;
    const submitOrderBtn = document.getElementById("submitOrder");
    const cancelModalOrder = document.getElementById("cancelModalOrder");
    
    
    if (currentStepCreateOrderModal === 1) {
        cancelModalOrder.className = "btnGroupBase btnRight btnRightRed";
        cancelModalOrder.textContent = "انصراف";
    } /*else {
        cancelModalOrder.className = "btnGroupBase btnRight";
        cancelModalOrder.textContent = "قبلی";
    }*/
    
    if (totalStepCreateOrderModal === 0) {
        await createOrderNewTab(false);
    }
    
    if (currentStepCreateOrderModal < totalStepCreateOrderModal) {
        submitOrderBtn.className = "btnGroupBase btnLeft";
        submitOrderBtn.textContent = "بعدی";
    } else if (currentStepCreateOrderModal === totalStepCreateOrderModal) {
        submitOrderBtn.className = "btnGroupBase btnLeft btnLeftGreen";
        submitOrderBtn.textContent = "ثبت";
    } else {
        submitOrderBtn.classList.add("d-none");
        cancelModalOrder.className = "buttonCancel2";
    }
    
    const navTabs = document.getElementById("CreateOrderTabs");
    const divider = document.getElementById("dividerCreateOrderModal");
    const currentPane = document.getElementById(`CreateOrderTabs${step}-pane`);
    
    if (step % 2) {
        navTabs.style.borderBottomColor = "#00766e";
        divider.style.backgroundColor = "#00766e";
        currentPane?.classList.remove("scrollbar-purple");
    } else {
        navTabs.style.borderBottomColor = "#7740ab";
        divider.style.backgroundColor = "#7740ab";
        currentPane?.classList.add("scrollbar-purple");
    }
    
    const targetTab = document.getElementById(`CreateOrderTabs${currentStepCreateOrderModal}`);
    if (targetTab) new bootstrap.Tab(targetTab).show();
};


const generateCreateOrderTabs = (singleEdit = false) => {
    const tabsElm = document.getElementById("CreateOrderTabs");
    const panesElm = document.getElementById("CreateOrderTabPanes");
    tabsElm.innerHTML = "";
    panesElm.innerHTML = "";
    
    pfItemsGroupedData.forEach((item, index) => {
        const { tab, pane } = createSingleOrderTab(item, index);
        tabsElm.appendChild(tab);
        panesElm.appendChild(pane);
    });
    
    if (!singleEdit) {
        tabsElm.insertAdjacentHTML("beforeend", `
            <li class="nav-item" role="presentation" id="AddCreateOrderTab">
                <button class="nav-link" onclick="createOrderNewTab()" type="button" tabindex="-1">
                    <svg style="width: 19px" viewBox="0 0 20 20" fill="currentColor"><path d="M10.75 4.75a.75.75 0 0 0-1.5 0v4.5h-4.5a.75.75 0 0 0 0 1.5h4.5v4.5a.75.75 0 0 0 1.5 0v-4.5h4.5a.75.75 0 0 0 0-1.5h-4.5v-4.5Z"/></svg>
                </button>
            </li>
        `);
    }
};


const createSingleOrderTab = (item, index, isNew = false, uniqueId = null, fromPlusTab = false) => {
    const tabId = `CreateOrderTabs${index + 1}`;
    const tabPaneId = `${tabId}-pane`;
    const customerValue = item.customerTxt.trim() + " - کد مشتری: " + (item.customerCode ?? "-");
    
    
    const tab = document.createElement("li");
    tab.className = "nav-item";
    if (isNew) tab.dataset.tabid = uniqueId;
    tab.innerHTML = `
        <button class="nav-link ${index === 0 ? "active" : ""} d-flex align-items-center gap-3" id="${tabId}" data-step="${index + 1}" data-bs-target="#${tabPaneId}" type="button" role="tab">
            <span>${isNew ? `حواله جدید ${newTabCount++}` : `حواله ${index + 1}`}</span>
            ${fromPlusTab && totalStepCreateOrderModal !== 1 ? `<svg style="width: 16px" viewBox="0 0 16 16" fill="currentColor"><path d="M5.28 4.22a.75.75 0 0 0-1.06 1.06L6.94 8l-2.72 2.72a.75.75 0 1 0 1.06 1.06L8 9.06l2.72 2.72a.75.75 0 1 0 1.06-1.06L9.06 8l2.72-2.72a.75.75 0 0 0-1.06-1.06L8 6.94 5.28 4.22Z" /></svg>` : ""}
        </button>
    `;
    const button = tab.querySelector("button");
    button.addEventListener("click", () => {
        const step = Number(button.dataset.step);
        goToStepCreateOrderModal(step);
    });
    
    button.querySelector("svg")?.addEventListener("click", (e) => {
        e.stopPropagation();
        closeNewTabOrder(uniqueId);
    });
    
    
    const pane = document.createElement("div");
    pane.className = `tab-pane flex-column ${index === 0 ? "active" : ""}`;
    pane.id = tabPaneId;
    pane.role = "tabpanel";
    if (isNew) pane.dataset.tabidPane = uniqueId;
    pane.tabIndex = 0;
    
    pane.innerHTML = `
        <form class="headOrderForm pt-2 pb-3">
            <div class="row flex-column flex-lg-row justify-content-between w-100 g-2 px-xl-2">
                <div class="col-12 col-lg col-xl-4 gap-1 d-grid align-items-center align-content-start" style="grid-template-columns:auto 1fr;">
                    <label style="white-space:nowrap;text-align:center">شماره حواله:</label>
                    <input class="form-control" style="height: 24px;" type="text" name="orderNumber" value="${item.orderNumber ?? "-"}" disabled />
                    <label style="white-space:nowrap;text-align:center">مشتری:</label>
                    <input class="form-control" style="height: 24px;" type="text" name="customerTxt" value="${customerValue}" disabled />
                    ${item.supplierTxt ? `
                    <label style="white-space:nowrap;text-align:center">تامین کننده:</label>
                    <input class="form-control" style="height: 24px;" type="text" name="supplierTxt" value="${item.supplierTxt}" disabled />` : ""}
                </div>
                <div class="col-12 col-lg-4 col-xl-3 gap-1 d-grid align-items-center align-content-start" style="grid-template-columns:auto 1fr;">
                    <label style="white-space:nowrap;text-align:center">کارشناس فروش:</label>
                    <input class="form-control" style="height: 24px;" type="text" name="userCreator" value="${item.userCreator || ""}" disabled />
                    <label style="white-space:nowrap;text-align:center">کارشناس تامین:</label>
                    <input class="form-control" style="height: 24px;" type="text" name="userTaminTxt" value="${item.userTaminTxt || ""}" disabled />
                </div>
                <div class="col-12 col-lg-3 col-xl-2 gap-1 d-grid align-items-center align-content-start" style="grid-template-columns:auto 1fr">
                    <label style="white-space:nowrap;text-align:center">تاریخ:</label>
                    <input class="sdate form-control" style="height: 24px;" type="text" name="orderDate" value="${item.orderDate || ""}" placeholder="yyyy/mm/dd" />
                    <label style="white-space:nowrap;text-align:center">تلفن:</label>
                    <input class="form-control" style="height: 24px;" type="text" name="tel" />
                    ${!fromPlusTab ? `
                    <button type="button" class="addFromPreFactorBtn buttonGreen2 align-items-center justify-content-center gap-2 mt-2" style="display: none;grid-column: 2 / -1;width: 140px;justify-self: end" onclick="AddFromPreFactorModal()" tabindex="0">
                        افزودن از پیش‌فاکتور
                        <i class="mp fa fa-plus"></i>
                    </button>` : ""}
                </div>
            </div>
        </form>
        <div class="mx-auto" style="width:100%; height:2px; background-color:${(index + 1) % 2 ? "#00766e" : "#7740ab"};"></div>
        <div class="orderBody"></div>
    `;
    
    const generatedDom = generateCreateOrderDom(item.orders, item.description, item.supplierId);
    pane.querySelector(".orderBody").appendChild(generatedDom);
    
    return { tab, pane };
};


const createOrderNewTab = async (fromPlusTab = true) => {
    try {
        const apiUrl = submitOrderType === 1 || fromPlusTab
            ? `/Factor/GetPFactorItems?id=${submitOrderPfId}`
            : `/Factor/GetPFactorItemsGroupBySuppliers?id=${submitOrderPfId}`;
        
        
        FLshowLoading();
        const data = (await axios(apiUrl)).data;
        data.forEach(newData => {
            const uniqueId = `tab-${Date.now()}`;
            newData._tabid = uniqueId;
            const newIndex = totalStepCreateOrderModal++;
            pfItemsGroupedData.push(newData);
            const panesElm = document.getElementById("CreateOrderTabPanes");
            const addTabBtn = document.getElementById("AddCreateOrderTab");
            
            const { tab, pane } = createSingleOrderTab(newData, newIndex, true, uniqueId, fromPlusTab);
            addTabBtn.insertAdjacentElement("beforebegin", tab);
            panesElm.appendChild(pane);
            
            fromPlusTab && tab.firstElementChild.click();
        });
        
        
        FLshowLoading(false);
    } catch (err) {
        console.log(err);
    }
};

const closeNewTabOrder = (uniqueId) => {
    pfItemsGroupedData = pfItemsGroupedData.filter(item => item._tabid !== uniqueId);
    totalStepCreateOrderModal--;
    newTabCount--;
    
    const navTabs = document.getElementById("CreateOrderTabs");
    const tab = navTabs.querySelector(`[data-tabid=${uniqueId}]`);
    const pane = document.querySelector(`[data-tabid-pane=${uniqueId}]`);
    
    const tabIsActive = tab.firstElementChild.classList.contains("active");
    
    tab?.remove();
    pane?.remove();
    
    renumberTabsAndPanes();
    if (tabIsActive) {
        const remainingTabs = navTabs.querySelectorAll(".nav-item:not(#AddCreateOrderTab) .nav-link");
        remainingTabs[remainingTabs.length - 1]?.click();
    } else {
        navTabs.querySelector(".nav-link.active").click();
    }
};

const renumberTabsAndPanes = () => {
    const navTabs = document.getElementById("CreateOrderTabs");
    const newBtnsTabs = navTabs.querySelectorAll(".nav-item[data-tabid] .nav-link span");
    const allBtnTabs = navTabs.querySelectorAll(".nav-item:not(#AddCreateOrderTab) .nav-link");
    
    newBtnsTabs.forEach((btn, idx) => btn.textContent = `حواله جدید ${idx + 1}`);
    allBtnTabs.forEach((btn, idx) => {
        const newIndex = idx + 1;
        
        const oldBtnId = btn.id;
        const newBtnId = `CreateOrderTabs${newIndex}`;
        btn.id = newBtnId;
        btn.dataset.step = newIndex.toString();
        btn.setAttribute("data-bs-target", `#${newBtnId}-pane`);
        
        const pane = document.getElementById(`${oldBtnId}-pane`);
        pane.id = `${newBtnId}-pane`;
    });
};


const generateCreateOrderDom = (items, description = "", supplierId) => {
    const container = document.createElement("div");
    container.className = "col-12 d-flex flex-column gap-2 px-2 pt-2";
    container.style.fontSize = "clamp(12px,2vw,14px)";
    container.innerHTML = `
        <div class="py-2 gap-2" style="display:flex;width:100%;overflow: auto;">
            <form class="createOrderForm flex-grow-1">
                <div class="flex-column" style="display:flex;justify-content:space-between;width:100%;">
                    <div class="gap-1 fbold" style="display: grid;grid-template-columns: 20px 123px 1fr 1fr 100px repeat(3, 1fr) repeat(3, 100px) 1fr 20px;margin-bottom:10px;">
                        <label class="text-center">#</label>
                        <label class="text-center">نوع جنس</label>
                        <label class="text-center">شرح کالا</label>
                        <label class="text-center">ابعاد</label>
                        <label class="text-center">نوع دسته</label>
                        <label class="text-center">کاست</label>
                        <label class="text-center">رنگ زمینه</label>
                        <label class="text-center">رنگ چاپ</label>
                        <label class="text-center">تعداد رنگ</label>
                        <label class="text-center">نام طرح</label>
                        <label class="text-center">مقدار</label>
                        <label class="text-center">ضخامت</label>
                        <label class="text-center"></label>
                    </div>
                    <div class="createOrderItems"></div>
                </div>
            </form>
        </div>
        <div class="my-2 w-100" style="background-color: rgba(0, 0, 0, 0.15);height:1px;"></div>
        <div>
            <label>توضیحات:</label>
            <textarea class="form-control" name="Description" style="text-align:start; padding:10px !important; height:auto !important;" rows="4">${description}</textarea>
        </div>
    `;
    
    const fieldsName = [
        "productSmallName",
        "product",
        "dimensions",
        "categoryType",
        "caset",
        "backgroundColor",
        "printColor",
        "countColor",
        "designName",
        "weight",
        "thickness",
        "unit",
        "productCode",
        /* "productGroup",
         "productGroupTxt",*/
    ];
    const containerItems = container.querySelector(".createOrderItems");
    containerItems.innerHTML = "";
    
    const filteredItems = items.filter(i => !i.isDeleted);
    filteredItems.forEach((item, index) => {
        const row = document.createElement("fieldset");
        row.className = "grid-row";
        row.dataset.preFactorItemId = item.preFactorItemId;
        
        const numberCell = document.createElement("div");
        numberCell.className = "divInp";
        numberCell.style.width = "auto";
        numberCell.innerHTML = `<span>${index + 1}</span>`;
        
        row.appendChild(numberCell);
        fieldsName.forEach((fieldName, i) => {
            const cell = document.createElement("div");
            cell.className = "divInp";
            
            const input = document.createElement("input");
            input.type = "text";
            input.name = fieldName;
            input.placeholder = "-";
            input.tabIndex = i + 1;
            input.className = `inpTlab w-100 ${fieldName === "weight" ? "digitNumberString" : ""}`;
            input.value = item[fieldName] ?? "";
            if (fieldName === "productSmallName") {
                input.disabled = true;
                cell.style.width = "123px";
            } else if (fieldName === "weight") {
                const unitInputTxt = document.createElement("input");
                unitInputTxt.classList.add("inputGroupText");
                unitInputTxt.name = "unitTxt";
                unitInputTxt.value = item.unitTxt;
                unitInputTxt.disabled = true;
                
                cell.appendChild(unitInputTxt);
                cell.style.width = "100px";
                
                input.disabled = Boolean(supplierId);
                input.style.borderBottomLeftRadius = "0";
                input.style.borderTopLeftRadius = "0";
                input.style.direction = "ltr";
            } else if (fieldName === "unit" || fieldName === "productCode") {
                input.type = "hidden";
                cell.style.display = "none";
                if (fieldName === "unit") input.classList.add("digitNumber");
            } else if (fieldName === "countColor" || fieldName === "designName" || fieldName === "categoryType") {
                cell.style.width = "100px";
            } else if (fieldName === "thickness") {
                //cell.style.paddingInlineEnd = "2px";
            }
            
            
            cell.appendChild(input);
            row.appendChild(cell);
        });
        
        // if (filteredItems.length > 1) {
        const deleteElm = document.createElement("div");
        deleteElm.className = "divInp";
        deleteElm.style.width = "auto";
        deleteElm.innerHTML = `
            <span class="d-flex align-items-center justify-content-center" style="cursor: pointer"">
                <svg style="width: 18px;color: darkred" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 0 0 6 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 1 0 .23 1.482l.149-.022.841 10.518A2.75 2.75 0 0 0 7.596 19h4.807a2.75 2.75 0 0 0 2.742-2.53l.841-10.52.149.023a.75.75 0 0 0 .23-1.482A41.03 41.03 0 0 0 14 4.193V3.75A2.75 2.75 0 0 0 11.25 1h-2.5ZM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4ZM8.58 7.72a.75.75 0 0 0-1.5.06l.3 7.5a.75.75 0 1 0 1.5-.06l-.3-7.5Zm4.34.06a.75.75 0 1 0-1.5-.06l-.3 7.5a.75.75 0 1 0 1.5.06l.3-7.5Z" clip-rule="evenodd" /></svg>
            </span>`;
        
        deleteElm.addEventListener("click", () => deleteItemOrder(item));
        
        row.appendChild(deleteElm);
        // }
        
        containerItems.appendChild(row);
        
    });
    
    return container;
};

const deleteItemOrder = (item) => {
    const dataCurrentTab = pfItemsGroupedData[currentStepCreateOrderModal - 1];
    const idx = dataCurrentTab.orders.findIndex(o => o.preFactorItemId === item.preFactorItemId);
    if (idx === -1) return;
    
    if (item.id) {
        dataCurrentTab.orders[idx].isDeleted = true;
    } else {
        dataCurrentTab.orders.splice(idx, 1);
    }
    
    const currentOrderBody = document.querySelector("#CreateOrderTabPanes .tab-pane.active .orderBody");
    const generatedDom = generateCreateOrderDom(dataCurrentTab.orders, dataCurrentTab.description, dataCurrentTab.supplierId);
    currentOrderBody.innerHTML = "";
    currentOrderBody.appendChild(generatedDom);
};


const submitOrder = async () => {
    FLshowLoading();
    let model = [];
    
    const getJsonOrdersFrom = (paneSelector, oldOrders) => {
        const orders = [];
        const fieldsets = Array.from(document.querySelectorAll(`${paneSelector} .createOrderItems > fieldset`));
        
        oldOrders.forEach(oldItem => {
            const fieldset = fieldsets.find(el => el.dataset.preFactorItemId === oldItem.preFactorItemId);
            if (fieldset) {
                const json = mySer.convertFormToJSON(null, [], fieldset);
                orders.push({ ...oldItem, ...json });
            } else { //isDeleted = true
                orders.push({ ...oldItem });
            }
        });
        
        return {
            orders: orders,
            prefactor: submitOrderPfId,
            dealId: submitOrderDealId || null, // TODO
            dealSerialNumber: submitOrderDealSerialNumber || null,
            customer: submitOrderCusId || null,
            description: document.querySelector(`${paneSelector} textarea`)?.value || "",
            tel: document.querySelector(`${paneSelector} input[name='tel']`)?.value || "",
            orderDate: document.querySelector(`${paneSelector} input[name='orderDate']`)?.value || "",
        };
    };
    
    pfItemsGroupedData.forEach((item, index) => {
        const data = { ...item, ...getJsonOrdersFrom(`#CreateOrderTabs${index + 1}-pane`, item.orders) };
        model.push(data);
    });
    
    axios.post(`/Order/AddHeadOrder`, model).then((res) => {
        if (res.status === 200) {
            toastifySuccess("حواله تولید با موفقیت ثبت شد");
            cancelModalCreateOrder();
            FLshowLoading(false);
        }
    }).catch((err) => {
        console.log(err);
        setTimeout(async () => await submitOrder(), 3000);
    });
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////
let pfItemsNotInOrders;
const ShowUpdateHeadOrder = async (id, passType, dealId) => {
    submitOrderDealId = dealId;
    FLshowLoading();
    const data = (await axios(`/Order/GetHeadOrder/${id}`)).data;
    
    pfItemsGroupedData[0] = data.orderHead;
    pfItemsNotInOrders = data.pfItemsNotInOrders;
    submitOrderPfId = pfItemsGroupedData[0].prefactor;
    submitOrderDealSerialNumber = pfItemsGroupedData[0].dealSerialNumber;
    submitOrderCusId = pfItemsGroupedData[0].customer;
    
    totalStepCreateOrderModal = pfItemsGroupedData.length;
    
    generateCreateOrderTabs(true);
    await goToStepCreateOrderModal(1);
    document.querySelector(".addFromPreFactorBtn").style.display = pfItemsNotInOrders.length ? "flex" : "none";
    
    FLshowLoading(false);
    $("#CreateOrderModal").modal("show");
};


const AddFromPreFactorModal = () => {
    document.querySelector("#AddFromPreFactorModal .modal-title").textContent = `آیتم های اضافه نشده از پیش‌فاکتور ${pfItemsGroupedData[0].prefactorSer}`;
    
    document.getElementById("AddFromPreFactor").replaceChildren(
        generateInvoiceItemDom(pfItemsNotInOrders, { type: "select", onSelect: (index, id) => selectPfactorItem(id) }),
    );
    
    $("#AddFromPreFactorModal").modal("toggle");
};

let tempPfItemsNotInOrders = [];
const selectPfactorItem = (id) => {
    if (!id) return;
    const index = pfItemsNotInOrders.findIndex(i => i.id === id);
    let selectedItem = pfItemsNotInOrders.splice(index, 1)[0];
    tempPfItemsNotInOrders.push(selectedItem);
    
    const newOrder = {
        dimensions: "", categoryType: "", caset: "", backgroundColor: "", printColor: "", countColor: "", designName: "", thickness: "",
        productSmallName: selectedItem.productSmallName,
        product: selectedItem.product,
        weight: selectedItem.weightCount,
        unit: selectedItem.unit,
        unitTxt: selectedItem.unitTxt,
        preFactorItemId: selectedItem.id,
        productCode: selectedItem.productCode,
        OrderHeadId: pfItemsGroupedData[currentStepCreateOrderModal - 1].id,
    };
    
    
    pfItemsGroupedData[currentStepCreateOrderModal - 1].orders.push(newOrder);
    const currentOrderBody = document.querySelector("#CreateOrderTabPanes .tab-pane.active .orderBody");
    const generatedDom = generateCreateOrderDom(pfItemsGroupedData[currentStepCreateOrderModal - 1].orders, pfItemsGroupedData[currentStepCreateOrderModal - 1].description, pfItemsGroupedData[currentStepCreateOrderModal - 1].supplierId);
    currentOrderBody.innerHTML = "";
    currentOrderBody.appendChild(generatedDom);
    
    document.querySelectorAll(".addFromPreFactorBtn").forEach(i => i.style.display = pfItemsNotInOrders.length ? "flex" : "none");
    $("#AddFromPreFactorModal").modal("toggle");
};


async function isSMSNotificationConfirmed() {
    const result = await Swal.fire({
        title: "اطلاع‌رسانی تحویل جنس به مشتری",
        text: "آیا به مشتری از طریق پیامک اطلاع رسانی شود؟",
        icon: "question",
        showDenyButton: true,
        denyButtonColor: "#dd6b55",
        confirmButtonColor: "#3085d6",
        denyButtonText: "خیر",
        confirmButtonText: "بله، ارسال شود",
        allowOutsideClick: false,
    });
    return result.isConfirmed;
}

async function changeStatusHd(id, status, sourceStatus, passType, fromDrop = false, draggedItem = null, sourceColumn = null) {
    let sendSms = false;
    if (status === 3 && sourceStatus !== 4) {
        sendSms = await isSMSNotificationConfirmed();
    }
    
    FLshowLoading();
    axios(`/order/ChangeStatusHd?id=${id}&status=${status}&sendSms=${sendSms}`).then(async (res) => {
        if (res.status === 200) {
            if (status === 3) {
                if (fromDrop) {
                    if (finishedOrderToggleIcon) {
                        finishedOrderToggleIcon.isVisible = false;
                    }
                    /*openIcon.style.display = "none";
                    closedIcon.style.display = "block";
                    document.getElementById("showFinishedOrderIcon").setAttribute("data-title", "نمایش تحویل شده ها");*/
                    //showHidden = false;
                }
                if (sendSms) {
                    toastifySuccess("اطلاع رسانی از طریق پیامک انجام شد");
                }
            }
            if (passType === 1) {
                await refreshOrders();
            } else if (passType === 4) {
                await refreshDealOrders();
            } else if (passType === 2) {
                await getUndeliveredOrderHeads();
            }
            FLshowLoading(false);
        } else if (res.status === 204) {
            FLshowLoading(false);
            toastifyError("شما مجاز به انجام این کار نیستید!");
            if (fromDrop && draggedItem && sourceColumn) {
                sourceColumn.appendChild(draggedItem);
            }
        }
        
    }).catch((err) => {
        console.log(err);
        setTimeout(async () => await changeStatusHd(id, status, sourceStatus, passType, fromDrop, draggedItem, sourceColumn), 3000);
    });
    
}


const DelHeadOrderItem = (id, passType) => {
    Swal.fire({
        title: "حذف!",
        text: "آیا مطمئن هستید که میخواهید حذف کنید؟",
        icon: "warning",
        showCancelButton: true,
        cancelButtonColor: "#adadad",
        confirmButtonColor: "#e64942",
        cancelButtonText: "انصراف",
        confirmButtonText: "بله، حذف شود",
    })
        .then(async (willDelete) => {
            if (willDelete.isConfirmed) {
                await DelHeadOrderItemOk(id, passType);
            }
        });
};

const DelHeadOrderItemOk = async (id, passType) => {
    $("#FLshowLoading").css("display", "");
    axios(`/Order/DelOrderHead/${id}`).then(async (res) => {
        if (res.status === 200) {
            if (passType === 1) {
                await refreshOrders();
            } else if (passType === 4) {
                await refreshDealOrders();
            }
            
            toastifySuccess("با موفقیت حذف شد!");
        } else if (res.status === 204) {
            toastifyError("شما مجاز به انجام این کار نیستید!");
        }
        $("#FLshowLoading").css("display", "none");
        
    }).catch(() => {
        setTimeout(async () => await DelHeadOrderItemOk(id, passType), 3000);
    });
};

const PreviewOrderPrint = (id) => {
    window.open("/order/PreviewPrint/" + id);
};

