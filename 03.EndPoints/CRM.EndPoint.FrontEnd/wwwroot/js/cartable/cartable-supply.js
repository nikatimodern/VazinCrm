/*function renderProgressTracker() {
    const steps = [
        { type: 1, label: "تعیین مبلغ فروش" },
        { type: 2, label: "تعیین تامین کننده" },
        { type: 3, label: "تایید" },
    ];

    const stepOrder = steps.findIndex(s => s.type === taminPreFac.taminStep);
    const progressValue = (stepOrder - 0.3) / (steps.length - 1);

    const container = setAmountTaminModal.querySelector("#progressiveTrackerTaminModal");
    container.innerHTML = `
        <div class="progressTracker" style="font-size:12px;margin-top: 10px;width: 88%">
            <div class="progressTrackerFilled" style="width:${stepOrder === steps.length - 1 ? "100%" : `calc(100%*${progressValue})`}"></div>
            <div class="d-flex justify-content-between w-100" style="top:-6px;height:18px;">
                ${steps.map((step, i) => {
        const isCompleted = stepOrder > i || taminPreFac.taminStep === 4;
        const isActive = stepOrder === i && taminPreFac.taminStep !== 4;
        return `
            <div class="d-flex align-items-center justify-content-between flex-column position-relative" data-index="${i}">
                <div class="progressTrackerDots ${isCompleted ? "completedTrack" : ""} ${isActive ? "activeTrack" : ""}">
                    <svg class="completed-svg" style="width:100%;height:100%;display:none;" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" clip-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm3.857-9.809a.75.75 0 0 0-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 1 0-1.06 1.061l2.5 2.5a.75.75 0 0 0 1.137-.089l4-5.5Z"/>
                    </svg>
                </div>
                <span class="d-none d-md-inline-block" style="position:absolute;width:max-content;top:100%;${isActive ? "font-size:13px" : ""}">${step.label}</span>
            </div>
        `;
    }).join("")}
        </div>
            <div class="flex-center position-absolute d-md-none" style="top: 0; left: 0; right: 0;margin-top: 10px;margin-right: 6px">
                <label class="fbold" style="font-size: 13px">${taminPreFac.taminStepTxt}</label>
            </div>
        </div>
    `;
}*/


/*------------- SALES MODAL -------------*/
const setAmountSalesModal = document.getElementById("setAmountSalesModal");
let taminPreFac;

let submitSalesModalWithAcceptPf;
const showModalSetAmount = async (pfId, withAcceptPf = true) => {
    submitSalesModalWithAcceptPf = withAcceptPf;
    
    taminPreFac = await getPreFactor(pfId);
    if (!taminPreFac) return;
    
    createSetAmountSalesItem();
    initSalesModal();
    
    toggleSalesModal();
};

function initSalesModal() {
    setAmountSalesModal.querySelector(".modal-title").innerText = "تایید پیش‌فاکتور " + taminPreFac.serialNumber;
    setAmountSalesModal.querySelector(".pf-official-badge").innerText = taminPreFac.officialType ? "رسمی" : "غیر رسمی";
    setAmountSalesModal.querySelector(".pf-official-badge").style.backgroundColor = taminPreFac.officialType ? "#162636" : "#3f5f80";
    setAmountSalesModal.querySelector(".pf-export-badge").style.display = taminPreFac.export ? "flex" : "none";
}

function createSetAmountSalesItem() {
    const container = setAmountSalesModal.querySelector("#setAmountSalesForm");
    container.innerHTML = "";
    
    const isOfficial = taminPreFac.officialType === 1;
    
    taminPreFac.preFactorItems.forEach((item, index) => {
        const fieldset = document.createElement("fieldset");
        fieldset.id = `setAmountSalesForm-${index}`;
        const totalLabel = isOfficial && item.discount ? " با مالیات و تخفیف" : isOfficial ? " با مالیات" : item.discount ? " با تخفیف" : "";
        
        fieldset.innerHTML = `
            <div class="d-flex flex-wrap align-items-center justify-content-around setAmountSalesItem ${index % 2 ? "bgPurple" : "bgGreen"}" style="border: 1px solid;border-radius: 6px;overflow: hidden">
                <div class="d-flex col-12 justify-content-around p-1 px-2" style="background-color: ${index % 2 ? "rebeccapurple" : "#04857c"};color: #fff">
                    <div class="d-flex align-items-center justify-content-center gap-1 pe-2">
                        <span class="flex-shrink-0" style="color: #e1e1e1">شرح کالا: </span>
                        <span class="fMedium text-center">${item.productSmallName ?? ""} ${item.product ?? ""} ${item.printable ? "<span class='fbold'>(چاپدار)</span>" : ""}</span>
                    </div>
                    <div class="d-flex align-items-center justify-content-center gap-1">
                        <span class="flex-shrink-0" style="color: #e1e1e1">مقدار: </span>
                        <span class="fMedium text-center" style="white-space: nowrap">
                            <span>${item.weightCount} </span>
                            <span>${item.unitTxt}</span>
                        </span>
                    </div>
                </div>

                <div class="d-flex flex-wrap col-12 justify-content-around p-2">
                    <div class="d-flex align-items-center gap-1 gap-xl-2 col-6 col-xl-auto pe-2">
                        <span class="flex-shrink-0" style="color: #535353">مبلغ واحد: </span>
                        <input type="text" inputmode="numeric" class="currencyNumber form-control inpAmount fbold" style="border: 1px solid #b5b3b3;height: 26px;" name="unitAmount" value="${item.unitAmount ? item.unitAmount.toLocaleString() : ""}" required placeholder="به ریال">
                    </div>
                    <div class="d-flex align-items-center gap-1 gap-xl-2 col-6 col-xl-auto">
<!--                         <span class="flex-shrink-0" style="color: #535353">${`مبلغ کل${totalLabel}: `}</span> -->
                        <span class="flex-shrink-0" style="color: #535353">مبلغ نهایی: </span>
                        <input type="text" inputmode="numeric" class="currencyNumber form-control inpAmount fbold" style="border: 1px solid #b5b3b3;height: 26px;" name="totalAmount" value="${item.totalAmount ? item.totalAmount.toLocaleString() : ""}" disabled>
                    </div>
                </div>
            </div>
            ${index !== taminPreFac.preFactorItems.length - 1 ? `<div style="width: 95%;height:1px; margin: 10px auto;background-color:darkviolet"></div>` : ""}
        `;
        
        fieldset.querySelector("input[name='unitAmount']").addEventListener("input", (e) => {
            const totalInput = fieldset.querySelector("input[name='totalAmount']");
            const totalPrice = calculateTotalPrice(item.weightCount, e.target.value);
            totalInput.value = totalPrice ? totalPrice.toLocaleString() : "";
            setAmountSalesModal.querySelector(".btnLeft").textContent = "ثبت و تایید";
        });
        
        container.appendChild(fieldset);
    });
}

const toggleSalesModal = () => {
    ModalToggle(setAmountSalesModal);
    setAmountSalesModal.querySelector(".btnLeft").textContent = "تایید";
};

const submitSalesModal = async () => {
    if (!await submitAmountSalesModal()) return;
    
    if (submitSalesModalWithAcceptPf) {
        if (!await acceptTaminExpert(taminPreFac.id)) return;
    } else {
        toastifySuccess("با موفقیت ثبت شد");
    }
    toggleSalesModal();
    await GetPreFactorTaminExpert();
};

async function submitAmountSalesModal() {
    if (!await mySer.CheckValidForm("setAmountSalesForm")) return false;
    
    const model = [];
    let hasChange = false;
    for (let i = 0; i < taminPreFac.preFactorItems.length; i++) {
        const json = mySer.convertFormToJSON(`setAmountSalesForm-${i}`);
        const item = { ...taminPreFac.preFactorItems[i], ...json };
        const original = taminPreFac.preFactorItems[i];
        for (const key in json) {
            if (json[key] !== original[key]) {
                hasChange = true;
                break;
            }
        }
        model.push(item);
    }
    
    if (!hasChange) return true;
    
    return await sendApiRequest({
        url: "/Factor/UpdatePrefactorItems",
        method: "POST",
        successMsg: null,
        data: model,
    });
}


/*------------- SUPPLY MODAL -------------*/

const setAmountTaminModal = document.getElementById("setAmountTaminModal");
let suppliesData = [];

const submitSupplyModal = async () => {
    if (!await submitAmountTaminModal()) return;
    
    toggleSupplyModal();
};

/*const submitAndAcceptPreFactor = async () => {
    if (hasSupplySetting) {
        if (!await submitAmountTaminModal()) return;
    }
    if (!await acceptTaminExpert(taminPreFac.id)) return;
    await GetPreFactorTaminExpert();
    toggleSupplyModal();
};*/

async function getSuppliesByPfId(pfId) {
    return await sendApiRequest({
        url: "/Supply/GetSupplies",
        params: { pfId },
    });
}

let hasSupplyAmountSetting;
let hasSupplySetting;
async function showSetSupplyModal(pfId, hasSupplyAmount = true, hasSupply = true) {
    hasSupplyAmountSetting = hasSupplyAmount;
    hasSupplySetting = hasSupply;
    const [pFactorData, supplyData] = await Promise.all([getPreFactor(pfId), getSuppliesByPfId(pfId)]);
    taminPreFac = pFactorData;
    
    const normalItems = supplyData.filter(x => x.supplyType === 1);
    const extraItems = supplyData.filter(x => x.supplyType === 2);
    const wasteItems = supplyData.filter(x => x.supplyType === 3);
    
    toggleSupplyModal();
    initSupplyModal();
    setupSupplyItemModal();
    createSetAmountTaminItems(normalItems);
    if (hasSupplyAmountSetting) {
        createSetAmountTaminExtras(extraItems);
        createSetAmountTaminWaste(wasteItems);
    } else {
        addItemSupplyModal.querySelector("#supplierAmountInputsWrapper")?.remove();
        setAmountTaminModal.querySelector("#showExtraTaminWrapperBtn").classList.add("d-none");
        setAmountTaminModal.querySelector("#showWasteTaminWrapperBtn").classList.add("d-none");
    }
}

function initSupplyModal() {
    setAmountTaminModal.querySelector(".modal-title").innerText = "بهای تامین پیش‌فاکتور " + taminPreFac.serialNumber;
    setAmountTaminModal.querySelector(".pf-official-badge").innerText = taminPreFac.officialType ? "رسمی" : "غیر رسمی";
    setAmountTaminModal.querySelector(".pf-official-badge").style.backgroundColor = taminPreFac.officialType ? "#162636" : "#3f5f80";
    setAmountTaminModal.querySelector(".pf-export-badge").style.display = taminPreFac.export ? "flex" : "none";
}

function toggleSupplyModal() {
    suppliesData = [];
    supplyItemsExtra = [];
    supplyItemsWaste = [];
    ModalToggle(setAmountTaminModal);
}


const addItemSupplyModal = document.getElementById("addItemSupplyModal");
const weightInputSupplyItemModal = addItemSupplyModal.querySelector("#weightMobile");

let isSetupSupplyModal = false;
function setupSupplyItemModal() {
    if (isSetupSupplyModal) return;
    
    function setupSelectOptions() {
        sendApiRequest({ url: "/Supply/GetSupplyBaseData" }).then((data) => {
            ["#supplierIdMobile", "#addExtraItemSupplierId"].forEach(id => {
                new TomSelect(id, {
                    options: data.suppliers,
                    placeholder: "انتخاب تامین کننده",
                    maxItems: 1,
                    selectOnTab: true,
                    // dropdownParent
                });
            });
            
            createOption("#addExtraItemSupplyTitle", data.extraBaseItems);
        });
    }
    
    function setupEventListener() {
        const totalPriceInput = addItemSupplyModal.querySelector("#totalAmountMobile");
        const unitPriceInput = addItemSupplyModal.querySelector("#unitAmountMobile");
        const addItemSupplyForm = addItemSupplyModal.querySelector("#addItemSupplyForm");
        
        function updateTotal() {
            const totalPrice = calculateTotalPrice(weightInputSupplyItemModal.value, unitPriceInput.value);
            totalPriceInput.value = totalPrice ? totalPrice.toLocaleString() : "";
            updateCurrencyHint(totalPriceInput);
        }
        
        if (hasSupplyAmountSetting) {
            unitPriceInput.addEventListener("input", () => {
                updateTotal();
            });
        }
        
        weightInputSupplyItemModal.addEventListener("input", () => {
            /*const remain = parseFloat(e.target.max) || 0;
            if (parseFloat(e.target.value) > remain) {
                e.target.value = remain;
            } else if (parseFloat(e.target.value) < 0) {
                e.target.value = 0;
            }*/
            
            if (hasSupplyAmountSetting) {
                updateTotal();
            }
        });
        
        
        addItemSupplyForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            await submitTaminItemFromModal();
        });
    }
    setupSelectOptions();
    setupEventListener();
    
    isSetupSupplyModal = true;
}


function initAddItemSupplyModal(supplyIndex, excludeIdx = null) {
    const pfItem = taminPreFac.preFactorItems[supplyIndex];
    
    addItemSupplyModal.querySelector("#previewItemTitle").innerText =
        `${pfItem.productSmallName ?? ""} ${pfItem.product ?? ""} ${pfItem.printable ? "(چاپدار)" : ""}`;
    
    addItemSupplyModal.querySelector("#previewItemWeight").innerText =
        `${Number(pfItem.weightCount)?.toLocaleString() ?? ""} ${pfItem.unitTxt ?? ""}`;
    
    addItemSupplyModal.querySelector("#previewItemUnitAmount").innerText = pfItem.unitAmount.toLocaleString();
    
    addItemSupplyModal.querySelector("#weightTxtMobile").textContent = pfItem.unitTxt;
    
    /*   const supplyItems = suppliesData[supplyIndex] || [];
       const total = supplyItems.filter((i, idx) => !i.isDeleted && idx !== excludeIdx).reduce((sum, x) => sum + (parseFloat(x.weight) || 0), 0);

       const remain = pfItem.weightCount - total;
       weightInputSupplyItemModal.max = remain > 0 ? remain : 0;
       weightInputSupplyItemModal.placeholder = `حداکثر ${remain}`;
       //weightInputSupplyItemModal.value = remain;

       if (parseFloat(weightInputSupplyItemModal.value) > remain) {
           weightInputSupplyItemModal.value = remain;
       }*/
}


function toggleSupplyItemModal(index = null) {
    mySer.emptyForm("addItemSupplyForm");
    //addItemSupplyModal.querySelector("#addItemSupplyForm").reset();
    
    addItemSupplyModal.dataset.editIndex = "";
    
    if (index !== null) {
        addItemSupplyModal.dataset.index = index;
        initAddItemSupplyModal(index);
    } else {
        addItemSupplyModal.dataset.index = "";
        weightInputSupplyItemModal.removeAttribute("max");
        weightInputSupplyItemModal.placeholder = "";
    }
    
    ModalToggle(addItemSupplyModal);
}

function handleDeleteSupplyItem(divPfItemId, supplyItemIndex) {
    const divPfItem = setAmountTaminModal.querySelector(`#${divPfItemId}`);
    const supplyIndex = Number(divPfItemId.split("-")[1]);
    const supplyItems = suppliesData[supplyIndex];
    
    const item = supplyItems[supplyItemIndex];
    if (item.id) {
        item.isDeleted = true;
    } else {
        supplyItems.splice(supplyItemIndex, 1);
    }
    
    refreshInfoItemsSupplies(divPfItem, supplyItems);
}

function handleEditSupplyItem(divPfItemId, supplyItemIndex) {
    const supplyIndex = Number(divPfItemId.split("-")[1]);
    const item = suppliesData[supplyIndex][supplyItemIndex];
    
    mySer.convertJsonToForm(item, "addItemSupplyForm");
    
    addItemSupplyModal.dataset.index = supplyIndex;
    addItemSupplyModal.dataset.editIndex = supplyItemIndex;
    
    initAddItemSupplyModal(supplyIndex, supplyItemIndex);
    ModalToggle(addItemSupplyModal, "show");
}

async function submitTaminItemFromModal() {
    if (!await mySer.CheckValidForm("addItemSupplyForm")) return;
    
    const index = addItemSupplyModal.dataset.index;
    const editIndex = addItemSupplyModal.dataset.editIndex;
    
    const supplierSelect = addItemSupplyModal.querySelector("#supplierIdMobile");
    const divPfItem = setAmountTaminModal.querySelector(`#setAmountTaminItems-${index}`);
    
    const supplyItems = suppliesData[index];
    const pfItem = taminPreFac.preFactorItems[index];
    
    const dataObj = {
        ...mySer.convertFormToJSON("addItemSupplyForm"),
        supplierTxt: supplierSelect.options[supplierSelect.selectedIndex].text,
        pfId: taminPreFac.id,
        pfSerial: taminPreFac.serialNumber.toString(),
        pfItemId: pfItem.id,
        productCode: pfItem.productCode,
        itemTitle: `${pfItem.productSmallName ?? ""} ${pfItem.product ?? ""}${pfItem.printable ? " (چاپدار)" : ""}`,
        supplyType: 1,
        supplyTypeTxt: "هزینه تولید",
    };
    
    if (editIndex !== "") {
        supplyItems[Number(editIndex)] = {
            ...supplyItems[Number(editIndex)],
            ...dataObj,
        };
    } else {
        supplyItems.push(dataObj);
    }
    refreshInfoItemsSupplies(divPfItem, supplyItems);
    toggleSupplyItemModal();
}


function refreshInfoItemsSupplies(divPfItem, supplyItems) {
    const infoContainer = divPfItem.querySelector(".infoContainerSupply");
    infoContainer.innerHTML = supplyItems.some(i => !i.isDeleted)
        ? `<table class="table table-bordered supplyItemsTable">
                <thead>
                    <tr>
                        <th>تامین کننده</th>
                        <th>مقدار</th>
                        ${hasSupplyAmountSetting ? `
                        <th>بهای تامین واحد</th>
                        <th>بهای تامین کل</th>` : ""}
                        <th>عملیات</th>
                    </tr>
                </thead>
                <tbody>
                </tbody>
            </table>`
        : `<span class="d-flex align-items-center justify-content-center py-1 text-center">هیچ تامین کننده ای ثبت نشده است!</span>`;
    
    supplyItems.forEach((supplyItem, index) => {
        if (!supplyItem.isDeleted) {
            infoContainer.querySelector("tbody").appendChild(createInfoItem(divPfItem.id, supplyItem, index));
        }
    });
}

function createInfoItem(divPfItemId, supplyItem, supplyItemIndex) {
    const infoItem = document.createElement("tr");
    infoItem.dataset.index = supplyItemIndex;
    
    infoItem.innerHTML = `
        <td>${supplyItem.supplierTxt ?? ""}</td>
        <td>${Number(supplyItem.weight)?.toLocaleString() ?? ""}</td>
        ${hasSupplyAmountSetting ? `
            <td>${supplyItem.unitAmount?.toLocaleString() ?? "-"}</td>
            <td>${supplyItem.totalAmount?.toLocaleString() ?? "-"}</td>` : ""}
            <td>
                <span class="d-flex gap-2 justify-content-center align-items-center" style="font-size: 13px">
                    <button class="mp" type="button" onclick="handleDeleteSupplyItem('${divPfItemId}', ${supplyItemIndex})" style="display:flex;justify-content:center;align-items:center;color:#333;border:none;background-color:transparent">
                      <i class="mp fa fa-trash"></i>
                    </button>
                    <button class="mp" type="button" onclick="handleEditSupplyItem('${divPfItemId}', ${supplyItemIndex})" style="display:flex;justify-content:center;align-items:center;color:#333;border:none;background-color:transparent">
                      <i class="mp fa fa-pencil"></i>
                    </button>
                </span>
            </td>
        `;
    return infoItem;
}


function createSetAmountTaminItems(normalItems) {
    const container = setAmountTaminModal.querySelector("#setAmountTaminWrapper");
    container.innerHTML = "";
    suppliesData.length = 0;
    
    taminPreFac.preFactorItems.forEach((item, index) => {
        const divPfItem = document.createElement("div");
        divPfItem.id = `setAmountTaminItems-${index}`;
        divPfItem.className = "flex-grow-1";
        
        divPfItem.innerHTML = `
            <div class="d-flex flex-column align-items-center justify-content-around setAmountSalesItem ${index % 2 ? "bgPurple" : "bgGreen"}" style="border: 1px solid;border-radius: 6px;overflow: hidden">
                <div class="d-flex flex-column col-12 flex-grow-1">
                    <div class="d-flex col-12 justify-content-around p-1 px-2" style="background-color: ${index % 2 ? "rebeccapurple" : "#04857c"};color: #fff">
                        <div class="flex-center gap-1 pe-2">
                            <span class="flex-shrink-0" style="color: #e1e1e1">شرح کالا: </span>
                            <span class="fMedium text-center">${item.productSmallName ?? ""} ${item.product ?? ""} ${item.printable ? "<span class='fbold'>(چاپدار)</span>" : ""}</span>
                        </div>
                        <div class="flex-center gap-1">
                            <span class="flex-shrink-0" style="color: #e1e1e1">مقدار: </span>
                            <span class="fMedium text-center" style="white-space: nowrap">
                                <span>${Number(item.weightCount)?.toLocaleString() ?? ""}</span>
                                <span>${item.unitTxt ?? ""}</span>
                            </span>
                        </div>
                    </div>
                </div>
                <div class="d-flex flex-column gap-2 align-items-center justify-content-center w-100 p-2 px-1 px-sm-2 px-lg-3">
                    <div class="infoContainerSupply col-12" style="font-size:clamp(11px,2.5vw,13px)">
                    </div>
                    <button type="button" onclick="toggleSupplyItemModal(${index})" class="flex-center gap-1 ${index % 2 ? "buttonPurple2" : "buttonGreen2"}" style="height: 26px;font-size: 12px;padding-inline: 10px !important;" >
                        افزودن تامین
                        <svg style="width: 16px;height: 16px" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15"></path></svg>
                    </button>
                </div>
            </div>
            ${index !== taminPreFac.preFactorItems.length - 1 ? `<div style="width: 95%;height:1px; margin: 10px auto;background-color:darkviolet"></div>` : ""}
        `;
        
        const supplyItems = normalItems.filter(x => x.pfItemId === item.id);
        suppliesData.push(supplyItems);
        
        refreshInfoItemsSupplies(divPfItem, supplyItems);
        
        container.appendChild(divPfItem);
    });
}


function showExtraTaminWrapper(show = true, wrapperSelector, wrapperBtnSelector) {
    setAmountTaminModal.querySelector(wrapperSelector).classList.toggle("d-none", !show);
    setAmountTaminModal.querySelector(wrapperBtnSelector).classList.toggle("d-none", show);
}


const addExtraItemSupplyModal = document.getElementById("addExtraItemSupplyModal");
function toggleSupplyExtraItemModal() {
    mySer.emptyForm("addExtraItemSupplyForm");
    addExtraItemSupplyModal.dataset.index = "";
    $(addExtraItemSupplyModal).modal("toggle");
}
const addWasteItemSupplyModal = document.getElementById("addWasteItemSupplyModal");
function toggleSupplyWasteItemModal() {
    mySer.emptyForm("addWasteItemSupplyForm");
    addWasteItemSupplyModal.dataset.index = "";
    $(addWasteItemSupplyModal).modal("toggle");
}

let supplyItemsExtra = [];
function createSetAmountTaminExtras(extraItems) {
    supplyItemsExtra = [...extraItems];
    
    const extraSelect = addExtraItemSupplyModal.querySelector("#addExtraItemSupplyTitle");
    const supplierSelect = addExtraItemSupplyModal.querySelector("#addExtraItemSupplierId");
    const infoContainerExtras = setAmountTaminModal.querySelector(".infoContainerExtras");
    const submitBtnExtra = addExtraItemSupplyModal.querySelector("#addExtraItemSupplyForm .addExtraItemSupplySubmitBtn");
    
    function handleEditExtra(idx) {
        toggleSupplyExtraItemModal();
        addExtraItemSupplyModal.dataset.index = idx;
        const item = supplyItemsExtra[idx];
        mySer.convertJsonToForm(item, "addExtraItemSupplyForm");
    }
    
    function handleDeleteExtra(idx) {
        const item = supplyItemsExtra[idx];
        if (item.id) {
            item.isDeleted = true;
        } else {
            supplyItemsExtra.splice(idx, 1);
        }
        refreshExtraItems();
    }
    
    function createInfoItem(supplyItemExtra, supplyItemExtraIndex) {
        const infoItem = document.createElement("tr");
        infoItem.dataset.index = supplyItemExtraIndex;
        
        infoItem.innerHTML = `
            <td>${supplyItemExtra.itemTitle ?? "-"}</td>
            <td>${supplyItemExtra.supplierTxt ?? "-"}</td>
            <td>${supplyItemExtra.totalAmount?.toLocaleString() ?? 0}</td>
            <td>
                <span class="d-flex gap-2 justify-content-center align-items-center" style="font-size: 13px">
                    <button class="mp deleteBtnExtra flex-center" type="button" style="color:#333;border:none;background-color:transparent">
                      <i class="mp fa fa-trash"></i>
                    </button>
                    <button class="mp editBtnExtra flex-center" type="button" style="color:#333;border:none;background-color:transparent">
                      <i class="mp fa fa-pencil"></i>
                    </button>
                </span>
            </td>
        `;
        
        infoItem.querySelector(".deleteBtnExtra").addEventListener("click", () => handleDeleteExtra(supplyItemExtraIndex));
        infoItem.querySelector(".editBtnExtra").addEventListener("click", () => handleEditExtra(supplyItemExtraIndex));
        
        return infoItem;
    }
    
    function refreshExtraItems() {
        const hasExtra = supplyItemsExtra.some(x => !x.isDeleted);
        showExtraTaminWrapper(hasExtra, "#extraTaminWrapper", "#showExtraTaminWrapperBtn");
        infoContainerExtras.innerHTML = hasExtra ?
            `<table class="table table-bordered supplyItemsTable">
                <thead>
                    <tr>
                        <th>عنوان هزینه</th>
                        <th>تامین کننده</th>
                        <th>بهای کل</th>
                        <th>عملیات</th>
                    </tr>
                </thead>
                <tbody>
                </tbody>
            </table>` : "";
        
        supplyItemsExtra.forEach((item, idx) => {
            if (!item.isDeleted) {
                infoContainerExtras.querySelector("tbody").appendChild(createInfoItem(item, idx));
            }
        });
    }
    
    async function handleSubmitExtra() {
        if (!await mySer.CheckValidForm("addExtraItemSupplyForm")) return;
        const editIndex = addExtraItemSupplyModal.dataset.index;
        
        const selectedOptionSupTxt = supplierSelect.options[supplierSelect.selectedIndex];
        const selectedOptionExtraTxt = extraSelect.options[extraSelect.selectedIndex];
        const data = {
            ...mySer.convertFormToJSON("addExtraItemSupplyForm"),
            itemTitle: selectedOptionExtraTxt.text,
            supplierTxt: selectedOptionSupTxt.value ? selectedOptionSupTxt.text : null,
            pfId: taminPreFac.id,
            pfSerial: taminPreFac.serialNumber.toString(),
            supplyType: 2,
            supplyTypeTxt: "هزینه اضافی",
        };
        if (!data.supplierId)
            data.supplierId = null;
        
        
        delete data.pfItemId;
        
        if (editIndex !== "") {
            supplyItemsExtra[editIndex] = {
                ...supplyItemsExtra[editIndex],
                ...data,
            };
        } else {
            supplyItemsExtra.push(data);
        }
        
        toggleSupplyExtraItemModal();
        refreshExtraItems();
        
        const amountTaminContainer = setAmountTaminModal.querySelector("#amountTaminContainer");
        amountTaminContainer.scrollTo({ top: amountTaminContainer.scrollHeight, behavior: "smooth" });
    }
    
    if (!submitBtnExtra.hasAttribute("data-hasListener")) {
        submitBtnExtra.addEventListener("click", handleSubmitExtra);
        submitBtnExtra.setAttribute("data-hasListener", "true");
    }
    
    refreshExtraItems();
}

let supplyItemsWaste = [];
function createSetAmountTaminWaste(wasteItems) {
    supplyItemsWaste = [...wasteItems];
    
    const infoContainerWaste = setAmountTaminModal.querySelector(".infoContainerWaste");
    const submitBtnWaste = addWasteItemSupplyModal.querySelector("#addWasteItemSupplyForm .addWasteItemSupplySubmitBtn");
    
    function handleEditWaste(idx) {
        toggleSupplyWasteItemModal();
        addWasteItemSupplyModal.dataset.index = idx;
        const item = supplyItemsWaste[idx];
        mySer.convertJsonToForm(item, "addWasteItemSupplyForm");
    }
    
    function handleDeleteWaste(idx) {
        const item = supplyItemsWaste[idx];
        if (item.id) {
            item.isDeleted = true;
        } else {
            supplyItemsWaste.splice(idx, 1);
        }
        refreshWasteItems();
    }
    
    function createInfoItem(supplyItem, index) {
        const infoItem = document.createElement("tr");
        infoItem.dataset.index = index;
        
        infoItem.innerHTML = `
            <td>${supplyItem.itemTitle ?? ""}</td>
            <td>${Number(supplyItem.weight)?.toLocaleString() ?? ""}</td>
            <td>${supplyItem.totalAmount?.toLocaleString() ?? 0}</td>
            <td>
                <span class="d-flex gap-2 justify-content-center align-items-center" style="font-size: 13px;color:#333">
                    <span class="deleteBtnExtra d-inline-flex cursor-pointer">
                      <i class="mp fa fa-trash"></i>
                    </span>
                    <span class="editBtnExtra d-inline-flex cursor-pointer">
                      <i class="mp fa fa-pencil"></i>
                    </span>
                </span>
            </td>
        `;
        
        infoItem.querySelector(".deleteBtnExtra").addEventListener("click", () => handleDeleteWaste(index));
        infoItem.querySelector(".editBtnExtra").addEventListener("click", () => handleEditWaste(index));
        
        return infoItem;
    }
    
    function refreshWasteItems() {
        const hasWaste = supplyItemsWaste.some(x => !x.isDeleted);
        showExtraTaminWrapper(hasWaste, "#wasteTaminWrapper", "#showWasteTaminWrapperBtn");
        
        infoContainerWaste.innerHTML = hasWaste ? `
            <table class="table table-bordered supplyItemsTable">
                <thead>
                    <tr>
                        <th>عنوان ضایعات</th>
                        <th>وزن</th>
                        <th>بهای کل</th>
                        <th>عملیات</th>
                    </tr>
                </thead>
                <tbody></tbody>
            </table>` : "";
        
        supplyItemsWaste.forEach((item, idx) => {
            if (!item.isDeleted) {
                infoContainerWaste.querySelector("tbody").appendChild(createInfoItem(item, idx));
            }
        });
    }
    
    async function handleSubmitWaste() {
        if (!await mySer.CheckValidForm("addWasteItemSupplyForm")) return;
        const editIndex = addWasteItemSupplyModal.dataset.index;
        
        const data = {
            ...mySer.convertFormToJSON("addWasteItemSupplyForm"),
            pfId: taminPreFac.id,
            pfSerial: taminPreFac.serialNumber.toString(),
            supplyType: 3,
            supplyTypeTxt: "ضایعات",
        };
        
        if (editIndex !== "") {
            supplyItemsWaste[editIndex] = {
                ...supplyItemsWaste[editIndex],
                ...data,
            };
        } else {
            supplyItemsWaste.push(data);
        }
        
        toggleSupplyWasteItemModal();
        refreshWasteItems();
        
        const amountTaminContainer = setAmountTaminModal.querySelector("#amountTaminContainer");
        amountTaminContainer.scrollTo({ top: amountTaminContainer.scrollHeight, behavior: "smooth" });
    }
    
    if (!submitBtnWaste.hasAttribute("data-hasListener")) {
        submitBtnWaste.addEventListener("click", handleSubmitWaste);
        submitBtnWaste.setAttribute("data-hasListener", "true");
    }
    
    refreshWasteItems();
}

const submitAmountTaminModal = async () => {
    const model = [...suppliesData.flat(), ...supplyItemsExtra, ...supplyItemsWaste];
    /*if (!suppliesData.every(arr => arr.some(x => !x.isDeleted))) {
        toastifyError("لطفا اطلاعات همه آیتم‌ها را وارد کنید.");
        return false;
    }*/
    
    return await sendApiRequest({
        url: "/Supply/AddSupply",
        method: "POST",
        data: model,
        successMsg: "بهای تامین با موفقیت ثبت شد",
        onSuccess: GetPreFactorTaminExpert,
    });
};

const acceptTaminExpert = async (id) => {
    return await sendApiRequest({
        url: "/Factor/AcceptTaminExpert",
        params: { id },
        successMsg: "پیش فاکتور تایید شد.",
        onSuccess: GetPreFactorTaminExpert,
    });
};