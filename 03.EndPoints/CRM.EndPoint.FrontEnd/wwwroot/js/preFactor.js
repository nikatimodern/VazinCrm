let pFactor = {};
let pFactorItems = [];
let pFactorDescriptionItems = [];
let pfItemIndex = null;
let baseDesPfactor = [];
let typeSubmitPfactor;

let dealData = {};

let prePaymentPercentId;
let descriptionInpId;
let descriptionListId;

if (window.innerWidth >= 992 && window.innerWidth < 1400) {
    prePaymentPercentId = "prePaymentPercentTablet";
    descriptionInpId = "descriptionIdTablet";
    descriptionListId = "PfactorDescIdTablet";
} else if (window.innerWidth < 992) {
    prePaymentPercentId = "prePaymentPercentMobile";
    descriptionInpId = "descriptionIdMobile";
    descriptionListId = "PfactorDescIdMobile";
} else {
    prePaymentPercentId = "prePaymentPercent";
    descriptionInpId = "descriptionId";
    descriptionListId = "PfactorDescId";
}
const prePaymentPercentElm = document.getElementById(prePaymentPercentId);

document.querySelector("#PreFacItem")?.addEventListener("input", (e) => {
    if (e.target.matches("[name='weightCount']") || e.target.matches("[name='unitAmount']") || e.target.matches("[name='discount']")) {
        calculateItem("PreFacItem");
    }
});
prePaymentPercentElm?.addEventListener("change", () => calculatePreFactorText());
document.querySelectorAll("#PreFactor input[type='radio'][name='official']")?.forEach(radio => {
    const currencyTypeElm = document.querySelector("#PfHeadFormId select[name='currencyType']");
    radio.addEventListener("change", (e) => {
        pFactor.officialType = Number(e.target.value);
        currencyTypeElm.disabled = pFactor.officialType === 1;
        currencyTypeElm.value = 0;
        
        calculatePreFactorTotals();
        showPFactorItems();
        generatePFactorDescriptions();
        calculatePreFactorText();
    });
});

const showModalAddPfactor = async (type, cusId = null, deal) => {
    typeSubmitPfactor = type;
    dealData = deal;
    FLshowLoading();
    baseDesPfactor = await getBaseDescription(0);
    pFactor.customerId = cusId;
    
    baseDesPfactor.filter(f => !f.official).map(item => {
        pFactorDescriptionItems.push({ text: item.text, isDeleted: false, official: item.official });
    });
    showPfDescriptionItems();
    toggleDisplayPrefactorModal();
    
    $("#official").checked = true;
    $("#PreFactor").modal("show");
    $("#date").val(await todayDate());
    
    $("#validityDade").val(await todayDate()); // await tomorrowDate()
    await getAllProductOp("PreFacItem");
    Unit(1);
    FLshowLoading(false);
};
const toggleDisplayPrefactorModal = (isHide = true) => {
    if (setting.setAmountByRoleType !== 2) return;
    /*
    document.querySelectorAll("#PreFactor .descriptionPreFactorBox").forEach(elm => elm.classList.toggle("hideDisplay", isHide));
    document.querySelector("#PreFactor .amountsPreFactorModalBox").classList.toggle("hideDisplay", isHide);
    document.querySelector("#PreFactor .bodyPreFactorModal").classList.toggle("col-xl-9", !isHide);
    
    const dlg = document.querySelector("#PreFactor .modal-dialog");
    dlg.classList.toggle("modal-xl", !isHide);
    dlg.classList.toggle("modal-lg", isHide);
    */
};
const upPfactor = async (id, type) => {
    typeSubmitPfactor = type;
    
    await getAllProductOp("PreFacItem");
    Unit(1, "PreFacItem");
    baseDesPfactor = await getBaseDescription(0);
    
    const data = await getPreFactor(id);
    
    if (!data) return;
    
    toggleDisplayPrefactorModal(data.paymentAmount === 0);
    
    pFactor = data;
    pFactorItems = data.preFactorItems;
    pFactorDescriptionItems = data.descriptions;
    dealData.id = data.dealId;
    dealData.serialNumber = data.dealSerialNumber;
    
    document.querySelectorAll("#PreFactor input[name='official']").forEach((i) => {
        i.checked = Number(i.value) === pFactor.officialType;
    });
    
    const exportValue = data.export ? "1" : "0";
    document.querySelectorAll("#PreFactor input[name='export']").forEach((i) => {
        i.checked = (i.value === exportValue);
    });
    
    const currencyTypeElm = document.querySelector("#PfHeadFormId select[name='currencyType']");
    currencyTypeElm.value = data.currencyType ?? 0;
    currencyTypeElm.toggleAttribute("disabled", pFactor.officialType === 1);
    
    document.getElementById("serialNumber").value = pFactor.serialNumber;
    document.getElementById("date").value = pFactor.date;
    document.getElementById("validityDade").value = pFactor.validityDade;
    
    document.getElementById("sumTotalAmount").textContent = pFactor.sumTotalAmount.toLocaleString();
    document.getElementById("discount").value = pFactor.discount.toLocaleString();
    document.getElementById("totalTax").textContent = pFactor.totalTax.toLocaleString();
    document.getElementById("paymentAmount").textContent = pFactor.paymentAmount.toLocaleString();
    
    showPFactorItems();
    showPfDescriptionItems();
    ModalToggle("#PreFactor");
};
const getPreFactor = async (id) => {
    return await sendApiRequest({
        url: "/Factor/GetPFactor",
        params: { id },
    });
};

const cancelPreFactor = () => {
    ModalToggle("#PreFactor", "hide");
    
    mySer.emptyForm("PreFacItem");
    mySer.emptyForm("PfHeadFormId", [{ name: "currencyType", val: "0" }]);
    
    document.getElementById("serialNumber").value = "";
    document.getElementById("date").value = "";
    document.getElementById("validityDade").value = "";
    document.getElementById("discount").value = "";
    document.getElementById("sumTotalAmount").innerHTML = "";
    document.getElementById("totalTax").innerHTML = "";
    document.getElementById("paymentAmount").innerHTML = "";
    document.getElementById("preFactorItemId").innerHTML = "";
    document.getElementById("preFactorItemId").classList.remove("AlertReq");
    
    document.getElementById("descriptionId").value = "";
    document.getElementById("descriptionIdTablet").value = "";
    document.getElementById("descriptionIdMobile").value = "";
    
    prePaymentPercentElm.value = Array.from(prePaymentPercentElm.children).find(i => i.defaultSelected).value;
    
    pFactor = {};
    pFactorItems = [];
    pFactorDescriptionItems = [];
    cancelUpdatePfItem();
};

/*const calculatePreFactorText = () => {
    const model = pFactorItems.filter(f => !f.isDeleted);
    const currencyTypeElm = document.querySelector("#PfHeadFormId select[name='currencyType']");
    const currencyTxt = currencyTypeElm.options[currencyTypeElm.selectedIndex].text.trim();
    const prePaymentPercent = Number(prePaymentPercentElm.value);
    
    let sumTotal = pFactor.paymentAmount;
    
    let valueText;
    const kelisheItems = model.filter(item => item.productSmallName.includes("کلیشه"));
    const cilandrItems = model.filter(item => item.productSmallName.includes("سیلندر"));
    
    let kelisheTotalAmount, cilandrTotalAmount;
    
    if (pFactor.officialType) {
        kelisheTotalAmount = kelisheItems.reduce((sum, item) => {
            let totalAmount = Number(SeparatedNumberRemove(item.totalAmount));
            return sum + totalAmount + (totalAmount * 0.1);
        }, 0);
        
        
        cilandrTotalAmount = cilandrItems.reduce((sum, item) => {
            let totalAmount = Number(SeparatedNumberRemove(item.totalAmount));
            return sum + totalAmount + (totalAmount * 0.1);
        }, 0);
    } else {
        kelisheTotalAmount = kelisheItems.reduce((sum, item) => sum + Number(SeparatedNumberRemove(item.totalAmount)), 0);
        cilandrTotalAmount = cilandrItems.reduce((sum, item) => sum + Number(SeparatedNumberRemove(item.totalAmount)), 0);
    }
    
    
    const totalWithoutBoth = sumTotal - (kelisheTotalAmount + cilandrTotalAmount); //مبلغ کل بدون کلیشه و سیلندر و مالیات
    
    const prePayment = (totalWithoutBoth * (prePaymentPercent / 100)) || 0;
    
    if (kelisheItems.length > 0 && cilandrItems.length > 0) {
        const finalTotalAmount = prePayment + kelisheTotalAmount + cilandrTotalAmount;
        valueText = `${prePaymentPercent} درصد مبلغ کل ${prePayment.toLocaleString()} ${currencyTxt} و تمام مبلغ کلیشه و سیلندر ${(kelisheTotalAmount + cilandrTotalAmount).toLocaleString()} ${currencyTxt} که جمعا معادل ${(finalTotalAmount).toLocaleString()} ${currencyTxt} می‌شود در ابتدای کار دریافت خواهد شد`;
    } else if (kelisheItems.length > 0) {
        const finalTotalAmount = prePayment + kelisheTotalAmount;
        valueText = `${prePaymentPercent} درصد مبلغ کل ${prePayment.toLocaleString()} ${currencyTxt} و تمام مبلغ کلیشه ${(kelisheTotalAmount).toLocaleString()} ${currencyTxt} که جمعا معادل ${(finalTotalAmount).toLocaleString()} ${currencyTxt} می‌شود در ابتدای کار دریافت خواهد شد`;
    } else if (cilandrItems.length > 0) {
        const finalTotalAmount = prePayment + cilandrTotalAmount;
        valueText = `${prePaymentPercent} درصد مبلغ کل ${prePayment.toLocaleString()} ${currencyTxt} و تمام مبلغ سیلندر ${(cilandrTotalAmount).toLocaleString()} ${currencyTxt} که جمعا معادل ${(finalTotalAmount).toLocaleString()} ${currencyTxt} می‌شود در ابتدای کار دریافت خواهد شد`;
    } else {
        valueText = `${prePaymentPercent} درصد از مبلغ کل معادل ${prePayment.toLocaleString()} ${currencyTxt} در ابتدای کار دریافت می‌شود `;
    }
    
    document.getElementById(descriptionInpId).value = valueText;
};*/

const calculatePreFactorText = () => {
    const model = pFactorItems.filter(f => !f.isDeleted);
    const currencyTypeElm = document.querySelector("#PfHeadFormId select[name='currencyType']");
    const currencyTxt = currencyTypeElm.options[currencyTypeElm.selectedIndex].text.trim();
    const prePaymentPercent = Number(prePaymentPercentElm.value) || 0;
    
    const sumTotal = Number(pFactor.paymentAmount) || 0;
    
    const kelisheItems = model.filter(item => item.productSmallName.includes("کلیشه"));
    const cilandrItems = model.filter(item => item.productSmallName.includes("سیلندر"));
    
    const kelisheTotalAmount = Math.round(
        kelisheItems.reduce((sum, item) => sum + (Number(SeparatedNumberRemove(item.totalAmount)) || 0), 0),
    );
    
    const cilandrTotalAmount = Math.round(
        cilandrItems.reduce((sum, item) => sum + (Number(SeparatedNumberRemove(item.totalAmount)) || 0), 0),
    );
    
    // مبلغ کل بدون کلیشه و سیلندر
    const totalWithoutBoth = Math.max(0, sumTotal - kelisheTotalAmount - cilandrTotalAmount);
    
    const prePayment = Math.round(totalWithoutBoth * prePaymentPercent / 100);
    const toolsTotalAmount = kelisheTotalAmount + cilandrTotalAmount;
    const finalTotalAmount = prePayment + toolsTotalAmount;
    
    let valueText;
    
    if (kelisheItems.length > 0 && cilandrItems.length > 0) {
        valueText =
            `${prePaymentPercent} درصد مبلغ کل ${prePayment.toLocaleString()} ${currencyTxt} ` +
            `و تمام مبلغ کلیشه و سیلندر ${toolsTotalAmount.toLocaleString()} ${currencyTxt} ` +
            `که جمعا معادل ${finalTotalAmount.toLocaleString()} ${currencyTxt} می‌شود ` +
            `در ابتدای کار دریافت خواهد شد`;
    } else if (kelisheItems.length > 0) {
        valueText =
            `${prePaymentPercent} درصد مبلغ کل ${prePayment.toLocaleString()} ${currencyTxt} ` +
            `و تمام مبلغ کلیشه ${kelisheTotalAmount.toLocaleString()} ${currencyTxt} ` +
            `که جمعا معادل ${finalTotalAmount.toLocaleString()} ${currencyTxt} می‌شود ` +
            `در ابتدای کار دریافت خواهد شد`;
    } else if (cilandrItems.length > 0) {
        valueText =
            `${prePaymentPercent} درصد مبلغ کل ${prePayment.toLocaleString()} ${currencyTxt} ` +
            `و تمام مبلغ سیلندر ${cilandrTotalAmount.toLocaleString()} ${currencyTxt} ` +
            `که جمعا معادل ${finalTotalAmount.toLocaleString()} ${currencyTxt} می‌شود ` +
            `در ابتدای کار دریافت خواهد شد`;
    } else {
        valueText =
            `${prePaymentPercent} درصد از مبلغ کل معادل ` +
            `${prePayment.toLocaleString()} ${currencyTxt} ` +
            `در ابتدای کار دریافت می‌شود`;
    }
    
    document.getElementById(descriptionInpId).value = valueText;
};


const showPFactorItems = () => {
    document.getElementById("preFactorItemId").replaceChildren(
        generateInvoiceItemDom(pFactorItems, { onDelete: deletePfItem, onUpdate: showUpdatePfItem }),
    );
};

/*
const generateItemPfactorDom = (data, type = "edit-delete") => {
    let ob = `<div class="pf-items-list">`;
    
    data.forEach((item, index) => {
        if (item.isDeleted) return;
        
        item.product ??= "";
        item.discount ??= 0;
        item.row = index + 1;
        const beforeDiscountAmount = calculateTotalPrice(item.weightCount, item.unitAmount);
        
        const beforeDiscountAmountStr = beforeDiscountAmount ? beforeDiscountAmount.toLocaleString() : "-";
        const uAmount = item.unitAmount ? item.unitAmount.toLocaleString() : "-";
        const discountAmount = item.discount ? item.discount.toLocaleString() : "-";
        const tAmount = item.totalAmount ? item.totalAmount.toLocaleString() : "-";
        
        let actionButtons = "";
        
        if (type === "edit-delete") {
            actionButtons = `
                <button type="button" class="pf-item-action" onclick="deletePfItem(${index})">
                    <i class="fa fa-trash"></i>
                </button>
                <button type="button" class="pf-item-action" onclick="showUpdatePfItem(${index})">
                    <i class="fa fa-pencil"></i>
                </button>
            `;
        } else if (type === "select") {
            actionButtons = `
                <button type="button" class="buttonPurple2" onclick="selectPfactorItem('${item.id}')">
                    افزودن
                </button>
            `;
        }
        
        ob += `
            <div class="pf-item-row">
                <div class="InfoDataItemHead">${index + 1}</div>

                <div class="InfoDataItem">
                    <div class="pf-item-printable">
                        ${Number(item.printable) === 1 ? "چـاپـدار" : ""}
                    </div>

                    <div class="pf-item-content">
                        <div class="pf-item-field pf-item-product">
                            <label>شرح کالا</label>
                            <label>
                                <span>${item.productSmallName ?? ""}</span>
                                <span>${item.product ?? ""}</span>
                            </label>
                        </div>
                        
                        <div class="pf-item-field pf-item-quantity">
                            <label>تعداد/مقدار</label>
                            <label>${item.weightCount}</label>
                        </div>
                        
                        <div class="pf-item-field pf-item-unit">
                            <label>واحد</label>
                            <label>${item.unitTxt ?? ""}</label>
                        </div>
                        
                        <div class="pf-item-field pf-item-unit-amount">
                            <label>مبلغ واحد</label>
                            <label>
                                ${uAmount}
                                ${item.currencyTypeTxt ?? ""}
                            </label>
                        </div>
                        
                        <div class="pf-item-field pf-item-beforeDiscount">
                            <label><span class="d-none d-sm-inline">مبلغ </span>قبل تخفیف</label>
                            <label>
                                ${beforeDiscountAmountStr}
                                ${item.currencyTypeTxt ?? ""}
                            </label>
                        </div>
                        
                        <div class="pf-item-field pf-item-discount">
                            <label>تخفیف</label>
                            <label>
                                ${discountAmount}
                                ${item.currencyTypeTxt ?? ""}
                            </label>
                        </div>
                        
                        <div class="pf-item-field pf-item-total">
                            <label>مبلغ نهایی</label>
                            <label>
                                ${tAmount}
                                ${item.currencyTypeTxt ?? ""}
                            </label>
                        </div>
                    </div>

                    <div class="InfoDataItemOp pf-item-actions">
                        ${actionButtons}
                    </div>
                </div>
            </div>
        `;
    });
    
    ob += `</div>`;
    return ob;
};
*/

/*
const generateItemPfactorDom = (data, type = "edit-delete") => {
    let ob = `<div class="d-flex flex-column w-100" style="gap:5px">`;
    data.forEach((item, index) => {
        if (item.isDeleted) return;
        if (item.product == null) item.product = "";
        
        item.row = index + 1;
        const uAmount = item.unitAmount.toLocaleString();
        const tAmount = item.totalAmount.toLocaleString();
        
        let actionButtons = "";
        
        if (type === "edit-delete") {
            actionButtons = `
                <button class="flex-center" style="color:#3f0e6e">
                    <i class="fa fa-trash" onclick="deletePfItem(${index})"></i>
                </button>
                <button class="flex-center" style="color:#3f0e6e">
                    <i class="fa fa-pencil" onclick="showUpdatePfItem(${index})"></i>
                </button>
            `;
        } else if (type === "select") {
            actionButtons = `
                <button class="buttonPurple2" onclick="selectPfactorItem('${item.id}')">
                    افزودن
                </button>
            `;
        }
        
        ob += `
            <div class="d-flex w-100">
                <div class="InfoDataItemHead">
                    <label class="fbold">${index + 1}</label>
                </div>
                <div class="InfoDataItem">
                    <div class="flex-center flex-column flex-shrink-0 px-2" style="width: 22px">
                        <label class="fbold" style="writing-mode: sideways-lr">${Number(item.printable) === 1 ? "چـاپـدار" : ""}</label>
                    </div>
                    <div class="d-flex justify-content-between flex-wrap w-100">
                        <div class="col-12 col-lg-5 InfoDataItemOp">
                            <label>شرح کالا</label>
                            <label class="px-2 d-flex gap-1 fbold">
                                <span>${item.productSmallName}</span>
                                <span>${item.product}</span>
                            </label>
                        </div>
                        <div class="d-lg-none" style="width:90%;margin-block: 4px;margin-inline: auto; height:1px; background-color:rgba(0,0,0,0.15);"></div>
                        <div class="col-6 col-lg-1 InfoDataItemOp">
                            <label>تعداد/مقدار</label>
                            <label class="fbold">${item.weightCount}</label>
                        </div>
                        <div class="col-6 col-lg-1 InfoDataItemOp">
                            <label>واحد</label>
                            <label class="fbold">${item.unitTxt}</label>
                        </div>
                        <div class="d-lg-none" style="width:90%;margin-block: 4px;margin-inline: auto; height:1px; background-color:rgba(0,0,0,0.15);"></div>
                        <div class="col-6 col-lg-2 InfoDataItemOp">
                            <label>مبلغ واحد</label>
                            <label class="fbold">${uAmount === "0" ? "-" : uAmount} ${item.currencyTypeTxt ?? ""}</label>
                        </div>
                        <div class="col-6 col-lg-2 InfoDataItemOp">
                            <label>مبلغ کل</label>
                            <label class="fbold">${tAmount === "0" ? "-" : tAmount} ${item.currencyTypeTxt ?? ""}</label>
                        </div>
                    </div>

                    <div class="InfoDataItemOp col-auto flex-shrink-0" style="padding-inline: 6px">
                        ${actionButtons}
                    </div>
                </div>
            </div>
        `;
    });
    
    ob += `</div>`;
    return ob;
};
*/


const showPfDescriptionItems = () => {
    let items = "";
    pFactorDescriptionItems.forEach((item, index) => {
        if (!item.isDeleted) {
            items += `
                <li class="d-flex justify-content-between gap-1">
                    <p>${item.text}</p>
                    <span class="cursor-pointer" onclick="deletePfDescriptionItem(${index})">
                        <i class="fa fa-trash"></i>
                    </span>
                </li>
                <div style="width:95%;margin:4px auto;height:1px;background-color:rgba(0,0,0,0.1)"></div>
            `;
        }
    });
    items = `<ul class="mp">${items}</ul>`;
    
    document.getElementById(descriptionListId).innerHTML = items;
};

const addPfDescriptionItem = () => {
    const inputElement = document.getElementById(descriptionInpId);
    const val = inputElement.value;
    inputElement.value = "";
    
    if (!val.length) return;
    
    pFactorDescriptionItems.push({ text: val, isDeleted: false, dir: null });
    showPfDescriptionItems();
    
    setTimeout(() => {
        const listElement = document.getElementById(descriptionListId);
        listElement.scrollTop = listElement.scrollHeight;
    }, 100);
};


const deletePfDescriptionItem = (index) => {
    if (!pFactorDescriptionItems[index].id) {
        pFactorDescriptionItems.splice(index, 1);
    } else {
        pFactorDescriptionItems[index].isDeleted = true;
    }
    showPfDescriptionItems();
};

const addPfItem = async () => {
    document.getElementById("preFactorItemId").classList.remove("AlertReq");
    
    if (!await mySer.CheckValidForm("#PreFacItem")) return false;
    if (!validatePfItemDiscount("PreFacItem")) return false;
    
    const productCodeElm = document.querySelector("#PreFacItem select[name='productCode']");
    const unitElm = document.querySelector("#PreFacItem select[name='unit']");
    
    const data = mySer.convertFormToJSON("#PreFacItem");
    
    data.productSmallName = productCodeElm.options[productCodeElm.selectedIndex].text;
    data.unitTxt = unitElm.options[unitElm.selectedIndex].text;
    
    data.totalAmount = Number(SeparatedNumberRemove(data.totalAmount)) || 0;
    data.unitAmount = Number(SeparatedNumberRemove(data.unitAmount)) || 0;
    data.discount = Number(SeparatedNumberRemove(data.discount)) || 0;
    
    pFactorItems.push(data);
    pFactor.PreFactorItems = pFactorItems;
    
    calculatePreFactorTotals();
    showPFactorItems();
    calculatePreFactorText();
    
    mySer.emptyForm("PreFacItem");
    document.getElementById("printableF")?.focus();
    return true;
};

const updatePfItem = async () => {
    if (!await mySer.CheckValidForm("#PreFacItem")) return false;
    if (!validatePfItemDiscount("PreFacItem")) return false;
    
    const form = document.getElementById("PreFacItem");
    
    const productCode = form.elements.namedItem("productCode");
    const unit = form.elements.namedItem("unit");
    
    const data = mySer.convertFormToJSON("#PreFacItem");
    
    data.productSmallName = productCode.options[productCode.selectedIndex].text;
    data.unitTxt = unit.options[unit.selectedIndex].text;
    
    data.totalAmount = Number(SeparatedNumberRemove(data.totalAmount)) || 0;
    data.discount = Number(SeparatedNumberRemove(data.discount)) || 0;
    data.unit = Number(SeparatedNumberRemove(data.unit)) || 0;
    data.unitAmount = Number(SeparatedNumberRemove(data.unitAmount)) || 0;
    
    
    pFactorItems[pfItemIndex] = {
        ...pFactorItems[pfItemIndex],
        ...data,
    };
    
    
    calculatePreFactorTotals();
    showPFactorItems();
    calculatePreFactorText();
    
    toastifySuccess("با موفقیت ویرایش شد!");
    cancelUpdatePfItem();
};


const deletePfItem = (index) => {
    let item = pFactorItems[index];
    
    if (item.id) {
        item.isDeleted = true;
    } else {
        pFactorItems.splice(index, 1);
    }
    
    calculatePreFactorTotals();
    showPFactorItems();
    calculatePreFactorText();
};

const showUpdatePfItem = (index) => {
    if (window.innerWidth < 992) {
        ModalToggle("ItemPreFactorModal");
    }
    pfItemIndex = index;
    
    mySer.convertJsonToForm(pFactorItems[index], "PreFacItem");
    document.getElementById("AddBtnId").classList.add("d-none");
    document.getElementById("CancelBtnId").classList.add("d-none");
    document.getElementById("UpBtnId").classList.remove("d-none");
};

const cancelUpdatePfItem = () => {
    mySer.emptyForm("PreFacItem");
    pfItemIndex = null;
    
    document.getElementById("AddBtnId").classList.remove("d-none");
    document.getElementById("CancelBtnId").classList.remove("d-none");
    document.getElementById("UpBtnId").classList.add("d-none");
    
    if (window.innerWidth < 992) {
        canselModalItemPreFactor();
    }
};

const addItemPreFactor = async () => {
    FLshowLoading();
    const isValid = await addPfItem();
    FLshowLoading(false);
    if (isValid) {
        ModalToggle("ItemPreFactorModal");
        toastifySuccess("با موفقیت اضافه شد!");
    }
};

const canselModalItemPreFactor = () => {
    mySer.emptyForm("PreFacItem");
    ModalToggle("ItemPreFactorModal", "hide");
};

/*
const calculatePreFactorTotals = () => {
    return calculateInvoiceTotals({
        items: pFactorItems,
        invoice: pFactor,
        discountInputId: "discount",
        sumTotalAmountId: "sumTotalAmount",
        totalTaxId: "totalTax",
        paymentAmountId: "paymentAmount",
    });
};
*/

const calculatePreFactorTotals = (updateItemsFromDiscount = false) => {
    return calculateInvoiceTotals({
        items: pFactorItems,
        invoice: pFactor,
        discountInputId: "discount",
        sumTotalAmountId: "sumTotalAmount",
        totalTaxId: "totalTax",
        paymentAmountId: "paymentAmount",
        updateItemsFromDiscount,
    });
};

document.getElementById("discount")?.addEventListener("input", () => {
    calculatePreFactorTotals(true);
    
    showPFactorItems();
    calculatePreFactorText();
});


const generatePFactorDescriptions = () => {
    if (pFactor.officialType === 0) { //اگر غیر رسمی بود
        pFactorDescriptionItems = pFactorDescriptionItems.filter(item => {
            if (item.official === true) {
                if (item.id == null) {
                    return false;
                    
                } else {
                    item.isDeleted = true;
                }
            }
            return true;
        });
    } else { //اگر رسمی بود
        pFactorDescriptionItems = [
            ...pFactorDescriptionItems.filter(item => !item.official),
            ...baseDesPfactor.filter(item => item.official),
        ];
    }
    showPfDescriptionItems();
    if (pFactor.officialType === 1) {
        setTimeout(() => {
            const listElement = document.getElementById(descriptionListId);
            listElement.scrollTop = listElement.scrollHeight;
        }, 100);
    }
};


const submitPreFactor = async () => {
    const dateInput = document.getElementById("date");
    const validityDateInput = document.getElementById("validityDade");
    
    if (!await mySer.CheckValidForm("#PfHeadFormId") || !validityDateInput.value || !dateInput.value) {
        return;
    }
    
    const activeItems = pFactorItems.filter(item => !item.isDeleted);
    
    if (!activeItems.length) {
        document.getElementById("preFactorItemId").classList.add("AlertReq");
        toastifyError("پیش فاکتور خالی است");
        return;
    }
    
    await submitPreFactorOk();
};

const submitPreFactorOk = async () => {
    const dateInput = document.getElementById("date");
    const validityDateInput = document.getElementById("validityDade");
    
    const currencyTypeElement = document.querySelector("#PfHeadFormId select[name='currencyType']");
    
    pFactor.serialNumber = Number(document.getElementById("serialNumber").value);
    pFactor.date = toEnglishDigits(dateInput.value);
    pFactor.validityDade = toEnglishDigits(validityDateInput.value);
    pFactor.descriptions = pFactorDescriptionItems;
    pFactor.export = document.getElementById("export").checked;
    pFactor.dealId = dealData.id;
    pFactor.dealSerialNumber = dealData.serialNumber;
    
    pFactor.currencyType = Number(currencyTypeElement.value);
    pFactor.currencyTypeTxt = currencyTypeElement.options[currencyTypeElement.selectedIndex].text.trim();
    
    await sendApiRequest({
        url: "/Factor/AddPrefactor",
        method: "POST",
        data: pFactor,
        successMsg: "پیش‌فاکتور با موفقیت ثبت شد.",
        
        onSuccess: async () => {
            if (typeSubmitPfactor === 0) { // PreFactor/Factor
                await GetPreFactorPartial();
            } else if (typeSubmitPfactor === 1) { // CustomerProfile
                await getAllPreFactor();
            } else if (typeSubmitPfactor === 2) { // SalesAdmin
                await GetPreFactorSalesAdmin();
            } else if (typeSubmitPfactor === 4) {  // Deal
                await refreshDealPreFactors();
                
                if (typeof getAllDeals === "function") {
                    await getAllDeals();
                }
                
                if (!isModalOpen("dealPreFactorsModal")) {
                    await showModalDealPreFactors(dealData);
                }
            }
            
            cancelPreFactor();
        },
    });
};

let expirePfModalId = null;
let expirePfModalPassType = null;
const expirePFactorSelectElm = document.getElementById("M_ex_Item");
const ShowModalExpireDescription = (PfId, amount, serial, name, _formCode) => {
    expirePfModalPassType = _formCode;
    expirePfModalId = PfId;
    
    document.getElementById("M_ex_Serial").innerHTML = serial;
    document.getElementById("M_ex_Name").innerHTML = name;
    document.getElementById("M_ex_TAmount").innerHTML = amount;
    ModalToggle("ModalExpireDescription", "show");
};

const SubmitDescExpirePreFactor = async () => {
    if (!await mySer.CheckValidForm("#descExpirePfactor")) return;
    
    const data = mySer.convertFormToJSON("#descExpirePfactor");
    
    const description = Number(data.M_ex_Item) === 0
        ? data.des
        : expirePFactorSelectElm.options[expirePFactorSelectElm.selectedIndex].text;
    
    await sendApiRequest({
        method: "POST",
        url: "/Factor/ExpirePreFactor",
        data: { id: expirePfModalId, description },
        successMsg: "پیش‌فاکتور با موفقیت بسته شد",
        onSuccess: () => {
            if (expirePfModalPassType === 3) {
                GetPreFactorSalesAdmin();
            } else if (expirePfModalPassType === 4) {
                GetSelesExpertPartial();
            }
            ModalToggle("ModalExpireDescription", "hide");
        },
    });
};

expirePFactorSelectElm?.addEventListener("change", (e) => {
    const textareaWrapper = document.getElementById("descExpirePfactorID");
    const textarea = document.getElementById("descExpirePfactorIdTxt");
    if (Number(e.target.value) === 0) {
        textareaWrapper.style.display = "";
        textarea.setAttribute("required", "");
    } else {
        textareaWrapper.style.display = "none";
        textarea.removeAttribute("required");
    }
});


const deletePreFactor = async (id, type) => {
    await deleteItem({
        url: `/Factor/DelPreFactor/${id}`,
        callback: async () => {
            if (type === 0) { // PreFactor/Factor
                await GetPreFactorPartial();
            } else if (type === 1) { // CustomerProfile
                await getAllPreFactor();
                getAllDetailAmount();
            } else if (type === 2) { // SalesAdmin
                await GetPreFactorSalesAdmin();
            } else if (type === 4) { //deal
                await refreshDealPreFactors();
            }
        },
    });
};


let pfIdChangeUser;
async function showModalChangeUsersPf(pfId, cusId) {
    pfIdChangeUser = pfId;
    
    const data = await sendApiRequest({
        url: "/Customer/GetUsersForPreFactor",
        params: { cusId, pfId },
    });
    
    if (!data) return;
    
    createOption("selectOptUserPf", data.salesUsers, false, "انتخاب", true);
    createOption("selectOptTaminPf", data.taminUsers, true, "انتخاب", true);
    
    ModalToggle("changeUserPfModal");
}


async function submitChangeUsersPf() {
    const userId = document.getElementById("selectOptUserPf").value;
    const taminUserId = document.getElementById("selectOptTaminPf").value;
    
    await sendApiRequest({
        url: "/Customer/ChangeUsersPreFactor",
        params: { userId, taminUserId, pfId: pfIdChangeUser },
        successMsg: "با موفقیت ثبت شد",
        onSuccess: () => ModalToggle("changeUserPfModal", "hide"),
    });
}

///////////////////////////////////////////////////////////////////////////////////////
const showModalReceiptsPreFactor = async (id) => {
    await getAllReceiptsPreFactor(id);
    $("#AttachmentReceiptModal").modal("show");
};

const getAllReceiptsPreFactor = async (id) => {
    try {
        FLshowLoading();
        const res = await axios(`/Factor/GetAttachmentsInvoiceUploaded/${id}`);
        if (res.status === 200) {
            const { attachments: receipts, accCredits } = res.data;
            
            const container = document.getElementById("ReceiptsContainer");
            container.innerHTML = "";
            if (receipts.length || accCredits.length) {
                receipts.forEach((fileItem) => {
                    const wrapperCard = document.createElement("div");
                    wrapperCard.style = "border:1px solid #ccc; border-radius:8px; padding:10px; text-align:center; box-shadow:0 0 4px rgba(0,0,0,0.1)";
                    
                    const imageBox = createImageBox(fileItem, null, null, false, fileItem._AttachType === 4, "ثبت شد");
                    
                    const footerCard = document.createElement("div");
                    footerCard.style.marginTop = "10px";
                    if (fileItem._AttachType === 5 || fileItem._AttachType === 6) {
                        footerCard.innerHTML = `<button class="buttonGreen2" style="min-width: 72px">ثبت واریزی</button>`;
                        footerCard.querySelector("button").addEventListener("click", () => openPaymentModal(fileItem));
                    } else {
                        footerCard.innerHTML = `<button class="buttonGray" style="min-width: 72px">ویرایش</button>`;
                        footerCard.querySelector("button").addEventListener("click", () => ShowEditModalInvoice(fileItem.entityId));
                    }
                    
                    wrapperCard.append(imageBox, footerCard);
                    container.appendChild(wrapperCard);
                });
                accCredits.forEach((acc) => {
                    const wrapperCard = document.createElement("div");
                    wrapperCard.style = "border:1px solid #ccc; border-radius:8px; padding:10px; text-align:center; box-shadow:0 0 4px rgba(0,0,0,0.1);";
                    
                    wrapperCard.innerHTML = `
                        <div class="boxAttach d-flex flex-column align-items-center justify-content-evenly gap-2" style="pointer-events: none;background-color: #f3f3f3">
                            <span class="fbold" style="font-size: 20px;color: #333">اعتباری</span>
                            <div class="d-flex flex-column align-items-center justify-content-center gap-2">
                                <div class="d-flex align-items-center justify-content-center flex-wrap gap-1">
                                    <span class="text-muted">مبلغ: </span>
                                    <span class="fMedium">${acc.credit.toLocaleString()} <span style="font-size: 12px">ريال</span></span>
                                </div>
                                <div class="d-flex align-items-center justify-content-center flex-wrap gap-1">
                                    <span class="text-muted">تاریخ: </span>
                                    <span class="fMedium">${acc.vosolDate}</span>
                                </div>
                            </div>
                            ${acc.accepted ? `<div class="watermark watermark--success">تایید شد</div>` : ""}
                        </div>
                    `;
                    
                    
                    const footerCard = document.createElement("div");
                    footerCard.style.marginTop = "10px";
                    footerCard.innerHTML = `
                        <button class="${acc.accepted ? "buttonCancel3" : "buttonGreen2"}" style="min-width:72px">
                            ${acc.accepted ? "لغو تایید" : "تایید"}
                        </button>
                    `;
                    footerCard.querySelector("button").addEventListener("click", () => acceptAcc(acc.id, !acc.accepted, acc.pfid));
                    
                    wrapperCard.appendChild(footerCard);
                    container.appendChild(wrapperCard);
                });
            } else {
                container.innerHTML = `
                    <div class="d-flex flex-column align-items-center pb-4">
                        <img src="/Img/no-data.jpg" style="width: clamp(120px, 20vw, 180px)" alt="">
                        <div style="color: #506a77;font-size: clamp(13px, 2.5vw, 15px)">هیچ رسید واریزی برای نمایش وجود ندارد!</div>
                    </div>
                `;
            }
            FLshowLoading(false);
        }
    } catch (err) {
        console.log(err);
    }
};
async function openPaymentModal(fileItem) {
    const divShow = document.getElementById("DivShowSanadPar");
    divShow.innerHTML = "";
    const imageBox = createImageBox(fileItem);
    divShow.appendChild(imageBox);
    
    window.SanadPayList = [fileItem]; // حذف
    //await ShowModalInvoice(1);
}
async function acceptAcc(id, accepted, pfId) {
    await sendApiRequest({
        url: "/Accountancy/ToggleAcceptAcc",
        params: { id, accepted },
        onSuccess: async () => {
            await getAllReceiptsPreFactor(pfId);
            await GetPreFactorFinancialExpert();
        },
    });
}
///////////////////////////////////////////////////////////////////////////////////////

const HandAccepting = async (id, type) => {
    if (setting.attAcceptHanding === true) {
        if (await checkPreFactorAttachment(id)) {
            await acceptPreFactorManual(id, type);
        } else {
            Swal.fire({
                icon: "error",
                title: "خطا در تایید",
                text: "تایید انجام نشد فایل تایید باید ضمیمه شود",
                confirmButtonText: "باشه",
            });
        }
    } else {
        await acceptPreFactorManual(id);
    }
};

const checkPreFactorAttachment = async (id) => {
    const data = await sendApiRequest({
        url: "/Factor/GetAttachment",
        params: { id, type: 2 },
    });
    
    return data?.length > 0;
};

const acceptPreFactorManual = async (id, type) => {
    await sendApiRequest({
        url: "/Factor/AcceptPreFactorManual",
        params: { id },
        successMsg: "پیش‌فاکتور تایید شد",
        onSuccess: async () => {
            if (type === 1) {
                await getAllPreFactor();
            } else if (type === 3) {
                await GetSelesExpertPartial();
            } else if (type === 4) {
                await refreshDealPreFactors();
                if (typeof getAllDeals === "function") {
                    await getAllDeals();
                }
            }
        },
    });
};

window.AcceptedAttachment = [];
let attachPfId;
const AttachDoc = async (id) => {
    attachPfId = id;
    ModalToggle("Attachments", "show");
    
    await sendApiRequest({
        url: "/Factor/GetAttachment",
        params: { id, type: 2 },
        onSuccess: (res) => {
            window.AcceptedAttachment = res.data;
            FileUpReload("AcAtID");
        },
    });
};

const SubmitAcAttach = async () => {
    const model = { listAttach: window.AcceptedAttachment, id: attachPfId };
    
    await sendApiRequest({
        url: "/Factor/AddAcPrefacAttachment",
        method: "POST",
        data: model,
        onSuccess: cancelAttachment,
    });
};

const cancelAttachment = () => {
    ModalToggle("Attachments", "hide");
    window.AcceptedAttachment = [];
    document.getElementById("imageDisplayAcAt").innerHTML = "";
    document.getElementById("AcAtID").value = "";
};


const regUtamin = async (pfId, type) => {
    if (!await mySer.CheckValidForm(pfId)) return;
    
    try {
        FLshowLoading();
        const el = document.getElementById("S_" + pfId);
        const res = await axios(`/Factor/UtaminReg?pfId=${pfId}&usId=${el.value}`);
        if (res.status === 200) {
            if (type === 1) {
                await getAllPreFactor();
            } else if (type === 4) {
                await refreshDealPreFactors();
            }
            
            FLshowLoading(false);
            // showSmsSendingToast();
            //toastifySuccess("به مسئول تامین واگذار شد");
        }
    } catch (err) {
        console.log(err);
        toastifyError("خطای سرور");
        //setTimeout(async () => await regUtamin(id), 3000);
    } finally {
        FLshowLoading(false);
    }
};

const resendRegUtamin = async (pfId, userTaminId, type) => {
    try {
        FLshowLoading();
        const res = await axios(`/Factor/ResendRegUtamin?pfId=${pfId}&usId=${userTaminId}`);
        if (res.status === 200) {
            if (type === 1) {
                await getAllPreFactor();
            } else if (type === 4) {
                await refreshDealPreFactors();
            }
            //showSmsSendingToast();
            // toastifyInfo("پیامک در صف ارسال قرار گرفت");
            //toastifySuccess("ارسال مجدد انجام شد");
        }
    } catch (err) {
        console.log(err);
        //setTimeout(async () => await resendRegUtamin(pfId, userTaminId), 3000);
    } finally {
        FLshowLoading(false);
    }
};

let smsSendedPfId;
let typeSubmitSmsSendPf;
const showMobileModal = async (id, type) => {
    typeSubmitSmsSendPf = type;
    try {
        ModalToggle("MobileModal");
        smsSendedPfId = id;
        const res = await axios(`/factor/GetCustomerPhone/${id}`);
        if (res.status === 200) {
            document.getElementById("mobileid").value = res.data;
        }
        
    } catch (err) {
        console.log(err);
        setTimeout(async () => await showMobileModal(id, type), 3000);
    }
};

const SendToCustomer = async () => {
    try {
        FLshowLoading();
        const toNumber = document.getElementById("mobileid").value;
        const res = await axios(`/Factor/SendToCustomer?id=${smsSendedPfId}&toNumber=${toNumber}`);
        if (res.status === 200) {
            if (typeSubmitSmsSendPf === 1) {
                await getAllPreFactor();
            } else if (typeSubmitSmsSendPf === 3) {
                await GetSelesExpertPartial();
            } else if (typeSubmitSmsSendPf === 4) {
                await refreshDealPreFactors();
            }
            
            // showSmsSendingToast();
            //toastifySuccess("ارسال انجام شد");
            $("#MobileModal").modal("toggle");
        }
    } catch (err) {
        console.log(err);
        toastifyError("خطای سرور");
        /*CounterSmsSend = CounterSmsSend + 1;
        if (CounterSmsSend < 3) {
            toastifyError("ارسال با خطا روبرو شد");
            setTimeout(async () => await SendToCustomer(), 3000);
        } else {
            CounterSmsSend = 0;
            document.getElementById("FLshowLoading").style.display = "none";
            $("#MobileModal").modal("toggle");
            if (typeSubmitSmsSendPf === 1) {
                await getAllPreFactor();
            } else if (typeSubmitSmsSendPf === 4) {
                await refreshDealPreFactors();
            }
            Swal.fire({
                title: "خطا!", text: "لینک تایید ارسال نشد", icon: "error", confirmButtonText: "بستن",
            });
            
        }*/
    } finally {
        FLshowLoading(false);
    }
};


let deliveredPfId;
let deliveredType;
const getUndeliveredOrderHeads = async () => {
    const res = await axios(`/Order/GetUndeliveredOrderHeads?pfId=${deliveredPfId}`);
    const data = res.data.trim();
    document.getElementById("UndeliveredOrderHeads").innerHTML = data;
    const deliveredOkBtn = document.getElementById("UndeliveredOrderHeadsModalOkBtn");
    if (data.includes("empty-msg")) {
        deliveredOkBtn.textContent = "اتمام کار";
        return false;
    } else {
        deliveredOkBtn.textContent = "تحویل همه و اتمام کار";
        return true;
    }
};

const delivered = async (id, type) => {
    deliveredPfId = id;
    deliveredType = type;
    FLshowLoading();
    const hasUndelivered = await getUndeliveredOrderHeads();
    FLshowLoading(false);
    if (hasUndelivered) {
        ModalToggle("UndeliveredOrderHeadsModal", "show");
    } else {
        await deliveredOk();
    }
};

const deliveredOk = async () => {
    await confirmAlert({
        title: "اتمام کار!",
        icon: "question",
        confirmText: "بله، به مشتری تحویل داده شد",
        onConfirm: async () => {
            if (await ChangePf(deliveredPfId, 7)) {
                if (deliveredType === 1) {
                    await getAllPreFactor();
                } else if (deliveredType === 4) {
                    await refreshDealPreFactors();
                }
                toastifySuccess("پیش فاکتور اتمام کار خورد");
                
                if (isModalOpen("UndeliveredOrderHeadsModal")) {
                    ModalToggle("UndeliveredOrderHeadsModal", "hide");
                }
            }
        },
    });
};

const backDelivered = async (pfId, passType) => {
    if (await ChangePf(pfId, 6)) {
        if (passType === 1) {
            await getAllPreFactor();
        } else if (passType === 4) {
            await refreshDealPreFactors();
        }
    }
};

const ChangePf = async (id, code) => {
    const result = await sendApiRequest({
        url: "/Factor/ChangeStatePf",
        params: { id, code },
    });
    return result !== null;
};

const Backward = async (id, type) => {
    await confirmAlert({
        title: "بازگشت!",
        text: "با این اقدام پیش فاکتور به مرحله اول فرایند منتقل می شود. آیا مطمئن هستید؟",
        onConfirm: async () => {
            await sendApiRequest({
                url: "/Factor/Backward",
                params: { id },
                onSuccess: async () => {
                    if (type === 0) {
                        await GetPreFactorPartial();
                    } else if (type === 1) {
                        await getAllPreFactor();
                    } else if (type === 4) {
                        await refreshDealPreFactors();
                    } else if (type === 5) {
                        await GetPreFactorTaminExpert();
                    }
                },
            });
        },
    });
};


const showModalDeliverItem = async (pfId, pfItemId) => {
    await sendApiRequest({
        url: "/Factor/GetPartialAllItemFactorDetail",
        params: { pfId, pfItemId },
        onSuccess: (res) => {
            document.getElementById("DeliverDetailBodyId").innerHTML = res.data;
            ModalToggle("DeliverDetailModal", "show");
            initDeliverDataTable("AllItemsFactorDetailTable");
        },
    });
};


const showModalDeliver = async (pfId) => {
    await sendApiRequest({
        url: "/Factor/GetPartialAllItemFactor",
        params: { pfId },
        onSuccess: (res) => {
            document.getElementById("DeliverBodyId").innerHTML = res.data;
            ModalToggle("DeliverModal", "show");
            initDeliverDataTable("AllItemsFactorTable");
        },
    });
};

const initDeliverDataTable = (tableId) => {
    const tableElement = document.getElementById(tableId);
    if (!tableElement) return;
    
    $(tableElement).DataTable({
        paging: false,
        scrollCollapse: true,
        responsive: true,
        //scrollY: "calc(100dvh - 400px)",
        ordering: false,
        searching: false,
        language: languageDatatable,
        layout: null,
    });
};

/* ----------------------------------------------- sendPreFactorToExternalSoftware ----------------------------------------------- */

async function sendPreFactorToExternalSoftware(id) {
    await showExternalSoftwareModal(id, {
        mapType: 2,
        onConfirm: async ({ entityIds, externalSoftwareIds }) => {
            ModalToggle("chooseExternalSoftwareModal", "hide");
            
            await exSoftwarePreFactorStart({
                preFactorId: entityIds?.[0],
                softwareIds: externalSoftwareIds,
            });
        },
    });
}


const externalMappingModal = document.getElementById("externalMappingModal");
const externalMappingList = externalMappingModal?.querySelector("#externalMappingList");
const confirmExternalMappingBtn = externalMappingModal?.querySelector("#confirmExternalMappingBtn");


/* -------------------------------------------------------
   State
------------------------------------------------------- */

const createExternalSoftwareState = () => ({
    preFactorId: null,
    preFactor: null,
    
    softwareIds: [],
    softwareIndex: 0,
    software: null,
    
    step: "saleType",
    
    mappings: {
        customerId: null,
        saleTypeId: null,
        items: {},
    },
    
    processing: false,
});

let externalSoftwareProcess = createExternalSoftwareState();


/* -------------------------------------------------------
   Reset / Cleanup
------------------------------------------------------- */

function exSoftwarePreFactorReset() {
    exSoftwarePreFactorDestroySelects();
    
    externalSoftwareProcess = createExternalSoftwareState();
    externalMappingModal?.querySelector(".modal-dialog")?.classList.remove("modal-lg");
}


function exSoftwarePreFactorDestroySelects() {
    externalMappingList?.querySelectorAll("select").forEach(select => {
        select.tomselect?.destroy();
    });
}


externalMappingModal?.addEventListener("hidden.bs.modal", exSoftwarePreFactorReset);


/* -------------------------------------------------------
   Process
------------------------------------------------------- */

async function exSoftwarePreFactorStart({ preFactorId, softwareIds }) {
    exSoftwarePreFactorReset();
    
    externalSoftwareProcess.preFactorId = preFactorId;
    externalSoftwareProcess.softwareIds = [...new Set(softwareIds ?? [])];
    
    await exSoftwarePreFactorNext();
}


async function exSoftwarePreFactorNext() {
    const state = externalSoftwareProcess;
    
    if (state.softwareIndex >= state.softwareIds.length) {
        return;
    }
    
    state.software = {
        externalSoftwareId: state.softwareIds[state.softwareIndex],
    };
    
    await exSoftwarePreFactorLoadSaleTypes();
    
    ModalToggle(externalMappingModal, "show");
}


async function exSoftwarePreFactorFinish() {
    const state = externalSoftwareProcess;
    
    state.softwareIndex++;
    
    if (state.softwareIndex >= state.softwareIds.length) {
        ModalToggle(externalMappingModal, "hide");
        return;
    }
    
    state.software = null;
    state.mappings = {
        customerId: null,
        saleTypeId: null,
        items: {},
    };
    state.step = "saleType";
    
    await exSoftwarePreFactorNext();
}


/* -------------------------------------------------------
   Step Rendering
------------------------------------------------------- */

function exSoftwarePreFactorRenderStep({ title, subtitle, icon, step, content, large = false }) {
    exSoftwarePreFactorDestroySelects();
    
    document.getElementById("externalMappingTitle").textContent = title;
    document.getElementById("externalMappingSubtitle").textContent = subtitle;
    document.getElementById("externalMappingIcon").innerHTML = icon;
    
    externalMappingModal.querySelector(".modal-dialog").classList.toggle("modal-lg", large);
    
    externalMappingList.innerHTML = `
        <div class="external-mapping-card">
            <div class="external-mapping-software-info">
                <span class="external-mapping-software-name">
                    <i class="fas fa-desktop ms-1"></i>
                    نرم‌افزار ${escapeHtml(externalSoftwareProcess.software?.externalSoftwareTitle ?? "")}
                </span>

                <span class="external-mapping-step">
                    ${step}
                </span>
            </div>

            ${content}
        </div>
    `;
}


function exSoftwarePreFactorSetConfirmButton(text, disabled = false) {
    confirmExternalMappingBtn.textContent = text;
    toggleElementsDisabled(confirmExternalMappingBtn, disabled);
}


/* -------------------------------------------------------
   Sale Type
------------------------------------------------------- */

async function exSoftwarePreFactorLoadSaleTypes() {
    const state = externalSoftwareProcess;
    
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalSaleType",
        method: "POST",
        successMsg: null,
        data: {
            externalSoftwareIds: [state.software.externalSoftwareId],
        },
    });
    
    if (!result) return;
    
    state.software = {
        ...state.software,
        ...result,
    };
    
    state.step = "saleType";
    
    exSoftwarePreFactorRenderSaleType();
}


function exSoftwarePreFactorRenderSaleType() {
    const state = externalSoftwareProcess;
    
    exSoftwarePreFactorRenderStep({
        title: "انتخاب نوع فروش",
        subtitle: "نوع فروش پیش‌فاکتور را مشخص کنید",
        icon: `<i class="fas fa-file-invoice"></i>`,
        step: "مرحله 1 از 5",
        
        content: `
            <div>
                <label for="externalMappingSaleTypeSelect" class="form-label mb-1">
                    نوع فروش
                </label>

                <select id="externalMappingSaleTypeSelect">
                    ${(state.software.saleTypes ?? []).map(item => `
                        <option value="${escapeHtml(item.saleTypeId)}" ${item.saleTypeId === 2 ? "selected" : ""}>
                            ${escapeHtml(item.title)}
                        </option>
                    `).join("")}
                </select>
            </div>
        `,
    });
    
    const select = externalMappingList.querySelector("#externalMappingSaleTypeSelect");
    
    new TomSelect(select, {
        placeholder: "جستجو و انتخاب نوع فروش",
        onChange: exSoftwarePreFactorUpdateConfirmButton,
    });
    
    exSoftwarePreFactorSetConfirmButton("تأیید و ادامه", !select?.value);
}


async function exSoftwarePreFactorConfirmSaleType() {
    const state = externalSoftwareProcess;
    
    const saleTypeId = externalMappingList.querySelector("#externalMappingSaleTypeSelect")?.value;
    
    if (!saleTypeId) {
        toastifyError("نوع فروش را انتخاب کنید.");
        return;
    }
    
    state.mappings.saleTypeId = saleTypeId;
    await exSoftwarePreFactorLoadCustomer();
}


/* -------------------------------------------------------
   Customer
------------------------------------------------------- */

async function exSoftwarePreFactorLoadCustomer() {
    const state = externalSoftwareProcess;
    
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalCustomerMapping",
        method: "POST",
        successMsg: null,
        data: {
            ids: [state.preFactorId],
            externalSoftwareIds: [state.software.externalSoftwareId],
        },
    });
    
    if (!result?.length) return;
    
    state.software = {
        ...state.software,
        ...result[0],
    };
    
    state.step = "customer";
    
    exSoftwarePreFactorRenderCustomer();
}


function exSoftwarePreFactorRenderCustomer() {
    const state = externalSoftwareProcess;
    const software = state.software;
    
    const mappedCustomerId = software.mappedExternalCustomerId;
    
    exSoftwarePreFactorRenderStep({
        title: "تطبیق مشتری",
        subtitle: "اتصال مشتری CRM به نرم‌افزار مالی",
        icon: `<i class="fas fa-users"></i>`,
        step: "مرحله 2 از 5",
        
        content: `
            <div class="external-mapping-entity">
                <span class="external-mapping-label fbold">
                    مشتری در CRM
                </span>

                <span class="external-mapping-entity-name">
                    ${escapeHtml(software.crmCustomerTitle ?? "-")}
                </span>
            </div>

            <div>
                <label for="externalMappingCustomerSelect" class="form-label mb-0">
                    مشتری معادل در نرم‌افزار مالی
                </label>

                <select id="externalMappingCustomerSelect">
                    <option value="">جستجو و انتخاب مشتری</option>

                    ${(software.customers ?? []).map(customer => `
                        <option value="${escapeHtml(customer.id)}" ${String(customer.id) === String(mappedCustomerId ?? "") ? "selected" : ""}>
                            ${customer.dlCode ? `${escapeHtml(customer.dlCode)} - ` : ""}${escapeHtml(customer.fullName)}
                        </option>
                    `).join("")}
                </select>
            </div>
        `,
    });
    
    const select = externalMappingList.querySelector("#externalMappingCustomerSelect");
    
    new TomSelect(select, {
        placeholder: "جستجو و انتخاب مشتری",
        onChange: exSoftwarePreFactorUpdateConfirmButton,
    });
    
    exSoftwarePreFactorSetConfirmButton("تأیید و ادامه", !select?.value);
}


async function exSoftwarePreFactorConfirmCustomer() {
    const state = externalSoftwareProcess;
    const customerId = externalMappingList.querySelector("#externalMappingCustomerSelect")?.value;
    
    if (!customerId) {
        toastifyError("مشتری نرم‌افزار مالی را انتخاب کنید.");
        return;
    }
    
    await sendApiRequest({
        url: "/Factor/SaveExternalCustomerMapping",
        method: "POST",
        successMsg: null,
        data: {
            preFactorId: state.preFactorId,
            externalSoftwareId: state.software.externalSoftwareId,
            externalCustomerId: customerId,
        },
        onSuccess: async () => {
            state.mappings.customerId = customerId;
            await exSoftwarePreFactorLoadItems();
        },
    });
    
}


/* -------------------------------------------------------
   Items
------------------------------------------------------- */

async function exSoftwarePreFactorLoadItems() {
    const state = externalSoftwareProcess;
    
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalItemMapping",
        method: "POST",
        successMsg: null,
        data: {
            preFactorId: state.preFactorId,
            externalSoftwareId: state.software.externalSoftwareId,
        },
    });
    
    if (!result?.preFactor.items?.length) {
        toastifyError("ردیفی برای تطبیق کالا پیدا نشد.");
        return;
    }
    
    
    state.preFactor = result.preFactor;
    
    state.software = {
        ...state.software,
        sepidarItems: result.sepidarItems,
    };
    
    state.mappings.items = {};
    state.step = "items";
    
    exSoftwarePreFactorRenderItems();
}


function exSoftwarePreFactorRenderItems() {
    const state = externalSoftwareProcess;
    const software = state.software;
    const preFactor = state.preFactor;
    
    exSoftwarePreFactorRenderStep({
        title: "تطبیق کالاها",
        subtitle: "انتخاب کالای معادل برای ردیف‌های پیش‌فاکتور",
        icon: `<i class="fas fa-boxes"></i>`,
        step: "مرحله 3 از 5",
        
        content: `
            <div class="external-item-mapping-list">
                ${(preFactor.items ?? []).map(item => `
                    <div class="external-mapping-entity external-item-mapping-row">

                        <span class="external-mapping-label">محصول پیش‌فاکتور CRM</span>
                        <span class="external-mapping-entity-name">${escapeHtml(item.productTitle)}</span>

                        <label class="form-label mt-2 mb-0" for="externalMappingItem_${item.preFactorItemId}">کالای معادل در نرم‌افزار مالی</label>

                        <select class="external-mapping-item-select" id="externalMappingItem_${item.preFactorItemId}" data-crm-item-id="${escapeHtml(item.preFactorItemId)}">
                            <option value="">جستجو و انتخاب کالا</option>

                            ${(software.sepidarItems ?? []).map(product => `
                                <option value="${escapeHtml(product.id)}">
                                    ${escapeHtml(product.code)} - ${escapeHtml(product.title)}
                                </option>
                            `).join("")}
                        </select>
                    </div>
                `).join("")}
            </div>
        `,
    });
    
    externalMappingList.querySelectorAll(".external-mapping-item-select").forEach(select => {
        new TomSelect(select, {
            placeholder: "جستجو و انتخاب کالا",
            
            onChange: value => {
                const itemId = String(select.dataset.crmItemId);
                
                const mapping = state.mappings.items[itemId] ?? {};
                
                if (value) {
                    mapping.externalItemId = value;
                    state.mappings.items[itemId] = mapping;
                } else if (mapping.externalStockId) {
                    delete mapping.externalItemId;
                } else {
                    delete state.mappings.items[itemId];
                }
                
                exSoftwarePreFactorUpdateConfirmButton();
            },
        });
    });
    
    exSoftwarePreFactorSetConfirmButton("تأیید کالاها و ادامه", !exSoftwarePreFactorAreAllItemsMapped());
}


function exSoftwarePreFactorAreAllItemsMapped() {
    const state = externalSoftwareProcess;
    const items = state.preFactor?.items ?? [];
    
    return items.length > 0 && items.every(item => state.mappings.items[String(item.preFactorItemId)]?.externalItemId);
}


async function exSoftwarePreFactorConfirmItems() {
    if (!exSoftwarePreFactorAreAllItemsMapped()) {
        toastifyError("برای تمام ردیف‌های پیش‌فاکتور کالای معادل انتخاب کنید.");
        return;
    }
    
    await exSoftwarePreFactorLoadStock();
}


/* -------------------------------------------------------
   Stock
------------------------------------------------------- */

async function exSoftwarePreFactorLoadStock() {
    const state = externalSoftwareProcess;
    
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalStockMapping",
        method: "POST",
        successMsg: null,
        data: {
            preFactorId: state.preFactorId,
            externalSoftwareId: state.software.externalSoftwareId,
        },
    });
    
    if (!result) return;
    
    state.software = {
        ...state.software,
        ...result,
    };
    
    state.step = "stock";
    
    exSoftwarePreFactorRenderStock();
}


function exSoftwarePreFactorRenderStock() {
    const state = externalSoftwareProcess;
    const software = state.software;
    const preFactor = state.preFactor;
    
    exSoftwarePreFactorRenderStep({
        title: "انتخاب انبار کالاها",
        subtitle: "برای هر ردیف پیش‌فاکتور انبار مقصد را مشخص کنید",
        icon: `<i class="fas fa-warehouse"></i>`,
        step: "مرحله 4 از 5",
        
        content: `
            <div class="external-item-mapping-list">
                ${(preFactor.items ?? []).map(item => {
            
            const mapping = state.mappings.items[String(item.preFactorItemId)];
            const externalProduct = (software.sepidarItems ?? []).find(product => String(product.id) === String(mapping?.externalItemId));
            
            return `
                    <div class="external-mapping-entity external-item-mapping-row">
                        <div class="d-flex gap-2 align-items-baseline">
                            <span class="external-mapping-label">
                                محصول پیش‌فاکتور CRM:
                            </span>
    
                            <span class="external-mapping-entity-name">
                                ${escapeHtml(item.productTitle)}
                            </span>
                        </div>
    
                        <div class="d-flex gap-2 align-items-baseline">
                            <span class="external-mapping-label">
                                محصول معادل سپیدار:
                            </span>
    
                            <span class="external-mapping-entity-name">
                                ${escapeHtml(externalProduct?.title ?? "-")}
                            </span>
                        </div>
    
                        <label class="form-label mt-2 mb-0" for="externalMappingStock_${item.preFactorItemId}">
                            انبار
                        </label>
    
                        <select class="external-mapping-stock-select" id="externalMappingStock_${item.preFactorItemId}" data-crm-item-id="${escapeHtml(item.preFactorItemId)}">
                            <option value="">جستجو و انتخاب انبار</option>
                            ${(software.stocks ?? []).map(stock => `
                                <option value="${escapeHtml(stock.id)}">
                                    ${escapeHtml(stock.code)} - ${escapeHtml(stock.title)}
                                </option>
                            `).join("")}
                        </select>
                    </div>`;
        }).join("")}
            </div>
        `,
    });
    
    externalMappingList.querySelectorAll(".external-mapping-stock-select").forEach(select => {
        new TomSelect(select, {
            placeholder: "جستجو و انتخاب انبار",
            
            onChange: value => {
                const itemId = String(select.dataset.crmItemId);
                const mapping = state.mappings.items[itemId];
                
                if (!mapping) return;
                
                if (value) {
                    mapping.externalStockId = value;
                } else {
                    delete mapping.externalStockId;
                }
                
                exSoftwarePreFactorUpdateConfirmButton();
            },
        });
    });
    
    exSoftwarePreFactorSetConfirmButton("تأیید انبارها و ادامه", !exSoftwarePreFactorAreAllItemsMappedWithStock());
}


function exSoftwarePreFactorAreAllItemsMappedWithStock() {
    const items = externalSoftwareProcess.preFactor?.items ?? [];
    
    return items.length > 0 && items.every(item => {
        const mapping = externalSoftwareProcess.mappings.items[String(item.preFactorItemId)];
        return mapping?.externalItemId && mapping?.externalStockId;
    });
}


/* -------------------------------------------------------
   Preview
------------------------------------------------------- */

function exSoftwarePreFactorPreview() {
    const state = externalSoftwareProcess;
    
    if (!exSoftwarePreFactorAreAllItemsMappedWithStock()) {
        toastifyError("برای تمام ردیف‌های پیش‌فاکتور کالا و انبار را انتخاب کنید.");
        return;
    }
    
    state.step = "preview";
    
    exSoftwarePreFactorRenderPreview();
}


function exSoftwarePreFactorBuildRequest() {
    const state = externalSoftwareProcess;
    
    return {
        preFactorId: state.preFactorId,
        externalSoftwareId: state.software.externalSoftwareId,
        externalCustomerId: Number(state.mappings.customerId ?? state.software.mappedExternalCustomerId),
        saleTypeId: state.mappings.saleTypeId,
        
        itemMappings: Object.entries(state.mappings.items).map(
            ([preFactorItemId, mapping]) => ({
                preFactorItemId: preFactorItemId,
                externalItemId: Number(mapping.externalItemId),
                externalStockId: Number(mapping.externalStockId),
            }),
        ),
    };
}


function exSoftwarePreFactorRenderPreview(data) {
    const state = externalSoftwareProcess;
    const preFactor = state.preFactor;
    const software = state.software;
    const mappings = state.mappings;
    
    const money = value => Number(value ?? 0).toLocaleString();
    
    const items = (preFactor.items ?? []).map(item => {
        const mapping = mappings.items[String(item.preFactorItemId)];
        const externalItem = (software.sepidarItems ?? []).find(x => String(x.id) === String(mapping?.externalItemId));
        const externalStock = (software.stocks ?? []).find(x => String(x.id) === String(mapping?.externalStockId));
        
        return {
            ...item,
            productTitle: item.productTitle,
            externalItemTitle: externalItem ? `${externalItem.code} - ${externalItem.title}` : "-",
            externalStockTitle: externalStock ? `${externalStock.code} - ${externalStock.title}` : "-",
        };
    });
    
    const saleType = (software.saleTypes ?? []).find(x => String(x.saleTypeId) === String(mappings.saleTypeId));
    const saleTypeTitle = saleType ? `${saleType.number} - ${saleType.title}` : "-";
    
    const customer = (software.customers ?? []).find(x => String(x.id) === String(mappings.customerId));
    const customerTitle = customer ? `${customer.dlCode} - ${customer.fullName}` : "-";
    
    exSoftwarePreFactorRenderStep({
        title: "بررسی نهایی پیش‌فاکتور",
        subtitle: "قبل از ثبت در نرم‌افزار مالی، اطلاعات را بررسی کنید",
        icon: `<i class="fas fa-clipboard-check"></i>`,
        step: "مرحله 5 از 5",
        large: true,
        
        content: `
            <div class="external-prefactor-preview">

                <div class="ex-preview-summary">

                    <div class="ex-preview-field">
                        <span class="ex-preview-label">نوع فروش:</span>
                        <strong>${escapeHtml(saleTypeTitle)}</strong>
                    </div>

                    <div class="ex-preview-field">
                        <span class="ex-preview-label">شماره پیش‌فاکتور:</span>
                        <strong>${escapeHtml(preFactor?.serialNumber ?? "-")}</strong>
                    </div>

                    <div class="ex-preview-field">
                        <span class="ex-preview-label">تاریخ:</span>
                        <strong>${escapeHtml(preFactor?.date ?? "-")}</strong>
                    </div>

                    <div class="ex-preview-field ex-preview-customer">
                        <span class="ex-preview-label">مشتری:</span>
                        <strong>${escapeHtml(customerTitle)}</strong>
                    </div>

                    <div class="ex-preview-field">
                        <span class="ex-preview-label">نرم‌افزار مقصد:</span>
                        <strong class="ex-preview-software">${escapeHtml(software.externalSoftwareTitle ?? "-")}</strong>
                    </div>

                </div>

                <div class="table-responsive">
                    <table class="table table-sm align-middle">
                        <thead>
                            <tr>
                                <th>ردیف</th>
                                <th>محصول CRM</th>
                                <th>کالای مقصد</th>
                                <th>انبار</th>
                                <th>تعداد</th>
                                <th>قیمت واحد</th>
                                <th>تخفیف</th>
                                <th>مالیات</th>
                                <th>مبلغ نهایی</th>
                            </tr>
                        </thead>

                        <tbody>
                            ${items.map((item, index) => `
                                <tr>
                                    <td>${index + 1}</td>
                                    <td>${escapeHtml(item.productTitle ?? "-")}</td>
                                    <td>${escapeHtml(item.externalItemTitle)}</td>
                                    <td>${escapeHtml(item.externalStockTitle)}</td>
                                    <td>${money(item.weightCount)}</td>
                                    <td>${money(item.unitAmount)}</td>
                                    <td>${money(item.discount)}</td>
                                    <td>${money(item.taxAmount)}</td>
                                    <td class="fbold">${money(item.totalAmount)}</td>
                                </tr>
                            `).join("")}
                        </tbody>
                    </table>
                </div>

                <div class="external-mapping-entity mt-3">
                     <div class="d-flex justify-content-between mb-1">
                        <span>جمع مبلغ کالاها</span>
                        <strong>${renderMoneyHtml(preFactor?.sumTotalAmount)}</strong>
                    </div>
                    <div class="d-flex justify-content-between mb-1">
                        <span>تخفیف کل</span>
                        <strong>${renderMoneyHtml(preFactor?.discount)}</strong>
                    </div>
                    <div class="d-flex justify-content-between mb-1">
                        <span>مالیات کل</span>
                        <strong>${renderMoneyHtml(preFactor?.totalTax)}</strong>
                    </div>
                    <hr>
                    <div class="d-flex justify-content-between">
                        <strong>مبلغ نهایی</strong>
                        <strong>${renderMoneyHtml(preFactor?.paymentAmount)}</strong>
                    </div>
                </div>
            </div>
        `,
    });
    
    exSoftwarePreFactorSetConfirmButton("تأیید و ثبت در نرم‌افزار", items.length === 0);
}


/* -------------------------------------------------------
   Submit
------------------------------------------------------- */

async function exSoftwarePreFactorSubmit() {
    const request = exSoftwarePreFactorBuildRequest();
    
    await sendApiRequest({
        url: "/Factor/SendPreFactorsToExternalSoftware",
        method: "POST",
        successMsg: "انتقال پیش‌فاکتور با موفقیت انجام شد",
        data: request,
        onSuccess: exSoftwarePreFactorFinish,
    });
}


/* -------------------------------------------------------
   Button State
------------------------------------------------------- */

function exSoftwarePreFactorUpdateConfirmButton() {
    const state = externalSoftwareProcess;
    
    if (state.processing) return;
    
    switch (state.step) {
        case "saleType": {
            const selected = externalMappingList?.querySelector("#externalMappingSaleTypeSelect")?.value;
            toggleElementsDisabled(confirmExternalMappingBtn, !selected);
            break;
        }
        
        case "customer": {
            const selected = externalMappingList?.querySelector("#externalMappingCustomerSelect")?.value;
            toggleElementsDisabled(confirmExternalMappingBtn, !selected);
            break;
        }
        
        case "items":
            toggleElementsDisabled(confirmExternalMappingBtn, !exSoftwarePreFactorAreAllItemsMapped());
            break;
        
        case "stock":
            toggleElementsDisabled(confirmExternalMappingBtn, !exSoftwarePreFactorAreAllItemsMappedWithStock());
            break;
        
        case "preview":
            toggleElementsDisabled(confirmExternalMappingBtn, false);
            break;
        
        default:
            toggleElementsDisabled(confirmExternalMappingBtn, true);
    }
}


/* -------------------------------------------------------
   Main Step Handler
------------------------------------------------------- */

const externalSoftwareStepHandlers = {
    saleType: exSoftwarePreFactorConfirmSaleType,
    customer: exSoftwarePreFactorConfirmCustomer,
    items: exSoftwarePreFactorConfirmItems,
    stock: exSoftwarePreFactorPreview,
    preview: exSoftwarePreFactorSubmit,
};


confirmExternalMappingBtn?.addEventListener("click", async () => {
        const state = externalSoftwareProcess;
        
        if (state.processing) return;
        
        const handler = externalSoftwareStepHandlers[state.step];
        
        if (!handler) {
            toastifyError("مرحله فعلی معتبر نیست.");
            return;
        }
        
        state.processing = true;
        
        toggleElementsDisabled(confirmExternalMappingBtn, true);
        
        try {
            await handler();
        } catch (error) {
            console.error(error);
            toastifyError("در اجرای این مرحله خطایی رخ داد.");
        } finally {
            state.processing = false;
            
            if (isModalOpen(externalMappingModal)) {
                exSoftwarePreFactorUpdateConfirmButton();
            }
        }
    },
);