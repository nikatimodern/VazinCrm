let factor = {};
let factorItems = [];
let factorDescriptionItems = [];
let fItemIndex = null;
let typeSubmitFactor;

let supplyData = [];

document.querySelectorAll("#Factor input[type='radio'][name='f_official']")?.forEach(radio => {
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    radio.addEventListener("change", async (e) => {
        factor.officialType = Number(e.target.value);
        currencyTypeElm.toggleAttribute("disabled", factor.officialType === 1);
        await calculateFactorTotals();
    });
});

document.querySelector("#FacItem")?.addEventListener("input", (e) => {
    if (e.target.matches("[name='weightCount']") || e.target.matches("[name='unitAmount']") || e.target.matches("[name='discount']")) {
        calculateItem("FacItem");
    }
});

const AddFactor = async (pfId, type = 1) => {
    if (!await isAllowedToAddFactor(pfId)) return;
    
    typeSubmitFactor = type;
    currentFactorContext.passType = type;
    
    
    await getAllProductOp("FacItem");
    Unit(2);
    
    const baseDesFactor = await getBaseDescription(1);
    const pfData = await getPreFactor(pfId);
    
    FLshowLoading();
    supplyData = (await axios(`/Supply/GetSupplies?pfId=${pfId}`)).data;
    if (!supplyData?.length) {
        toastifyError("بهای تامینی ثبت نشده است!");
        FLshowLoading(false);
        return;
    }
    
    const supplyWeights = new Map();
    
    supplyData.forEach(supply => {
        const weight = Number(supply.weight) || 0;
        
        supplyWeights.set(supply.pfItemId, (supplyWeights.get(supply.pfItemId) || 0) + weight);
    });
    
    factorItems =
        pfData.preFactorItems
              .filter(pfItem => supplyWeights.has(pfItem.id))
              .map(pfItem => ({
                  ...pfItem,
                  pfItemId: pfItem.id,
                  id: null,
                  weightCount: supplyWeights.get(pfItem.id),
                  totalAmount: calculateTotalPrice(supplyWeights.get(pfItem.id), pfItem.unitAmount),
              }));
    
    pfData.descriptions.forEach(item => {
        item.id = null;
    });
    
    factor = {
        ...pfData,
        pfactorID: pfData.id,
        pfactorSerialNumber: pfData.serialNumber,
        serialNumber: null,
        id: null,
        factorItems,
        date: await todayDate(),
        descriptions: [],
    };
    
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    currencyTypeElm.value = pfData.currencyType ?? 0;
    currencyTypeElm.toggleAttribute("disabled", factor.officialType === 1);
    
    factorDescriptionItems = [];
    const official = factor.officialType === 1;
    baseDesFactor
        .filter(item => item.official === official)
        .forEach(item => {
            const description = { ...item, isDeleted: false };
            
            factor.descriptions.push(description);
            factorDescriptionItems.push(description);
        });
    
    document.getElementsByName("f_official").forEach(radio => {
        radio.checked = Number(radio.value) === factor.officialType;
    });
    
    document.getElementsByName("f_export").forEach(radio => {
        radio.checked = Number(radio.value) === (factor.export ? 1 : 0);
    });
    
    document.getElementById("f_serialNumber").value = factor.serialNumber ?? "";
    document.getElementById("f_date").value = factor.date ?? "";
    document.getElementById("f_discount").value = factor.discount?.toLocaleString() ?? "0";
    
    
    
    await calculateFactorTotals();
    showFactorItems();
    showFDescriptionItems();
    
    ModalToggle("Factor", "show");
    FLshowLoading(false);
};


const upFactor = async (id, type = 1) => {
    typeSubmitFactor = type;
    currentFactorContext.passType = type;
    
    Unit(2);
    await getAllProductOp("FacItem");
    
    const data = await getFactor(id);
    
    if (!data) return;
    
    factorID = data.id;
    factor = data;
    factorItems = data.factorItems;
    factorDescriptionItems = data.descriptions;
    
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    currencyTypeElm.value = data.currencyType ?? 0;
    currencyTypeElm.toggleAttribute("disabled", factor.officialType === 1);
    
    document.getElementsByName("f_official").forEach(radio => {
        radio.checked = Number(radio.value) === factor.officialType;
    });
    
    document.getElementsByName("f_export").forEach(radio => {
        radio.checked = Number(radio.value) === (factor.export ? 1 : 0);
    });
    
    document.getElementById("f_SerialFormId").elements.namedItem("f_serialNumber").value = factor.serialNumber;
    document.getElementById("f_dateFormId").elements.namedItem("f_date").value = factor.date;
    document.getElementById("snFinancialFormId").elements.namedItem("snFinancial").value = factor.snFinancial;
    
    showFactorItems();
    showFDescriptionItems();
    
    document.getElementById("f_sumTotalAmount").textContent = SeperatedNumberGenerate(factor.sumTotalAmount);
    document.getElementById("f_discount").value = SeperatedNumberGenerate(factor.discount);
    document.getElementById("f_totalTax").textContent = SeperatedNumberGenerate(factor.totalTax);
    document.getElementById("f_paymentAmount").textContent = SeperatedNumberGenerate(factor.paymentAmount);
    //document.getElementById("f_reciveAmount").value = SeperatedNumberGenerate(factor.reciveAmount);
    
    ModalToggle("Factor", "show");
};


const returnFactor = async (fid, type = 1) => {
    typeSubmitFactor = type;
    currentFactorContext.passType = type;
    
    const modalFactor = document.getElementById("Factor");
    modalFactor.classList.add("returned-factor");
    
    
    FLshowLoading();
    Unit(2);
    await getAllProductOp("FacItem");
    factor = await getFactor(fid);
    factor.id = null;
    factor.serialNumber = null;
    factor.parentReturnedId = fid;
    factor.sepidarId = null;
    factor.snFinancial = null;
    factor.descriptions = factor.descriptions.map((f) => ({ ...f, id: null }));
    factor.factorItems = factor.factorItems.map((f) => ({ ...f, id: null }));
    factor.date = await todayDate();
    
    factorItems = factor.factorItems;
    factorDescriptionItems = factor.descriptions;
    
    
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    currencyTypeElm.value = factor.currencyType ?? 0;
    toggleElementsDisabled(currencyTypeElm, factor.officialType === 1);
    
    
    modalFactor.querySelectorAll("input[name='f_official']").forEach((i) => {
        i.checked = Number(i.value) === factor.officialType;
    });
    
    const exportValue = factor.export ? "1" : "0";
    modalFactor.querySelectorAll("input[name='f_export']").forEach((i) => {
        i.checked = (i.value === exportValue);
    });
    
    
    modalFactor.querySelector("#f_dateFormId input[name='f_date']").value = factor.date;
    modalFactor.querySelector("#snFinancialFormId input[name='snFinancial']").value = factor.snFinancial;
    
    const modalTitle = modalFactor.querySelector("#factorModalTitle");
    modalTitle.textContent = "فاکتور برگشتی";
    modalTitle.style.backgroundColor = "darkred";
    
    modalFactor.querySelectorAll(".modal-body .buttonGreen3").forEach((el) => {
        el.classList.replace("buttonGreen3", "buttonCancel3");
    });
    
    
    showFactorItems();
    showFDescriptionItems();
    modalFactor.querySelector("#f_sumTotalAmount").textContent = factor.sumTotalAmount.toLocaleString();
    modalFactor.querySelector("#f_totalTax").textContent = factor.totalTax.toLocaleString();
    modalFactor.querySelector("#f_paymentAmount").textContent = factor.paymentAmount.toLocaleString();
    modalFactor.querySelector("#f_discount").value = factor.discount.toLocaleString();
    
    ModalToggle(modalFactor, "show");
    FLshowLoading(false);
};

const getFactor = async (id) => {
    return await sendApiRequest({
        url: "/Factor/GetFactor/",
        params: { id },
    });
};

/*
const showFactorItems = () => {
    let ob = `<div class="pf-items-list">`;
    
    factorItems.forEach((item, index) => {
        if (item.isDeleted) return;
        
        item.product ??= "";
        item.discount ??= 0;
        item.row = index + 1;
        const beforeDiscountAmount = calculateTotalPrice(item.weightCount, item.unitAmount);
        
        const beforeDiscountAmountStr = beforeDiscountAmount ? beforeDiscountAmount.toLocaleString() : "-";
        const uAmount = item.unitAmount ? item.unitAmount.toLocaleString() : "-";
        const discountAmount = item.discount ? item.discount.toLocaleString() : "-";
        const tAmount = item.totalAmount ? item.totalAmount.toLocaleString() : "-";
        
        ob += `
            <div class="pf-item-row">
                <div class="InfoDataItemHead InfoDataItemHeadFactor">${index + 1}</div>

                <div class="InfoDataItem InfoDataItemFactor">
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
                            <label>مبلغ قبل تخفیف</label>
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
                        <button type="button" class="pf-item-action" onclick="deleteFItem(${index})">
                            <i class="fa fa-trash"></i>
                        </button>

                        <button type="button" class="pf-item-action" onclick="showUpdateFItem(${index})">
                            <i class="fa fa-pencil"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
    });
    
    ob += `</div>`;
    
    document.getElementById("FactorItemId").innerHTML = ob;
};
*/

const showFactorItems = () => {
    document.getElementById("FactorItemId").replaceChildren(
        generateFactorItemDom(factorItems, { onDelete: deleteFItem, onUpdate: showUpdateFItem }),
    );
};


document.getElementById("f_discount")?.addEventListener("input", async () => {
    await calculateFactorTotals(true);
    
    showFactorItems();
});


const addFItem = async () => {
    if (!await mySer.CheckValidForm("#FacItem")) return false;
    if (!validatePfItemDiscount("FacItem")) return false;
    
    const productCodeElm = document.querySelector("#FacItem select[name='productCode']");
    const unitElm = document.querySelector("#FacItem select[name='unit']");
    
    let data = mySer.convertFormToJSON("#FacItem");
    
    data.productSmallName = productCodeElm.options[productCodeElm.selectedIndex].text;
    data.unitTxt = unitElm.options[unitElm.selectedIndex].text;
    
    factorItems.push(data);
    factor.factorItems = factorItems;
    
    
    await calculateFactorTotals();
    showFactorItems();
    //calculateFactorText();
    mySer.emptyForm("FacItem");
    
    return true;
};


const addItemFactor = async () => {
    FLshowLoading();
    const isValid = await addFItem();
    FLshowLoading(false);
    if (isValid) {
        ModalToggle("ItemFactorModal");
        toastifySuccess("با موفقیت اضافه شد!");
    }
};


const canselModalItemFactor = () => {
    mySer.emptyForm("FacItem");
    ModalToggle("ItemFactorModal", "hide");
};


const showUpdateFItem = (index) => {
    if (window.innerWidth < 992) {
        ModalToggle("ItemFactorModal", "show");
    }
    fItemIndex = index;
    mySer.convertJsonToForm(factorItems[index], "FacItem");
    
    document.getElementById("f_AddBtnId").style.display = "none";
    document.getElementById("f_CancelBtnId").style.display = "none";
    document.getElementById("f_UpBtnId").style.display = "flex";
};


const cancelUpdateFItem = () => {
    mySer.emptyForm("FacItem");
    fItemIndex = null;
    
    document.getElementById("f_AddBtnId").style.display = "flex";
    document.getElementById("f_CancelBtnId").style.display = "flex";
    document.getElementById("f_UpBtnId").style.display = "none";
    
    if (window.innerWidth < 992) {
        canselModalItemFactor();
    }
};


const updateFItem = async () => {
    if (!await mySer.CheckValidForm("#FacItem")) return false;
    if (!validatePfItemDiscount("FacItem")) return false;
    
    const form = document.getElementById("FacItem");
    
    const productCode = form.elements.namedItem("productCode");
    const unit = form.elements.namedItem("unit");
    
    const data = mySer.convertFormToJSON("#FacItem");
    
    if (productCode.value) {
        data.productSmallName = productCode.options[productCode.selectedIndex].text;
    }
    
    //اگر کسب و کار کوچک بود این موارد باید برداشته شود
    data.unitTxt = unit.options[unit.selectedIndex].text;
    //تا اینجا
    
    //اگر کسب و کار کوچک بود این موارد باید فعال شود
    //if (unit.value != "") {
    //    data.unitTxt = unit.options[unit.selectedIndex].text;
    //}
    //تا اینجا
    
    factorItems[fItemIndex] = {
        ...factorItems[fItemIndex],
        ...data,
    };
    
    await calculateFactorTotals();
    showFactorItems();
    //calculateFactorText();
    
    toastifySuccess("با موفقیت ویرایش شد!");
    cancelUpdateFItem();
};


const deleteFItem = async (index) => {
    let item = factorItems[index];
    
    if (item.id != null) {
        item.isDeleted = true;
        // factorItems = factorItems.filter(f => f.isDeleted !== true);
    } else {
        factorItems.splice(index, 1);
    }
    
    await calculateFactorTotals();
    showFactorItems();
    calculateFactorText();
};


const calculateFactorTotals = (updateItemsFromDiscount = false) => {
    return new Promise((resolve) => {
        calculateInvoiceTotals({
            items: factorItems,
            invoice: factor,
            discountInputId: "f_discount",
            sumTotalAmountId: "f_sumTotalAmount",
            totalTaxId: "f_totalTax",
            paymentAmountId: "f_paymentAmount",
            updateItemsFromDiscount
        });
        resolve(true);
    });
};

const calculateFactorText = () => {
    const model = factorItems.filter(f => !f.isDeleted);
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    const currencyTxt = currencyTypeElm.options[currencyTypeElm.selectedIndex].text.trim();
    
    let sumTotal = Number(SeparatedNumberRemove(document.getElementById("f_sumTotalAmount").textContent));
    
    let valueText;
    const kelisheItems = model.filter(item => item.productSmallName.includes("کلیشه"));
    const cilandrItems = model.filter(item => item.productSmallName.includes("سیلندر"));
    
    let kelisheTotalAmount = kelisheItems.reduce((sum, item) => sum + Number(SeparatedNumberRemove(item.totalAmount)), 0);
    let cilandrTotalAmount = cilandrItems.reduce((sum, item) => sum + Number(SeparatedNumberRemove(item.totalAmount)), 0);
    
    const totalWithoutBoth = sumTotal - (kelisheTotalAmount + cilandrTotalAmount); //مبلغ کل بدون کلیشه و سیلندر و مالیات
    
    if (kelisheItems.length > 0 && cilandrItems.length > 0) {
        const finalTotalAmount = (totalWithoutBoth / 2) + kelisheTotalAmount + cilandrTotalAmount;
        valueText = `50 درصد مبلغ کل ${SeperatedNumberGenerate(totalWithoutBoth / 2)} ${currencyTxt} و تمام مبلغ کلیشه و سیلندر ${SeperatedNumberGenerate(kelisheTotalAmount + cilandrTotalAmount)} ${currencyTxt} که جمعا معادل ${SeperatedNumberGenerate(finalTotalAmount)} ${currencyTxt} می‌شود در ابتدای کار دریافت خواهد شد`;
    } else if (kelisheItems.length > 0) {
        const finalTotalAmount = (totalWithoutBoth / 2) + kelisheTotalAmount;
        valueText = `50 درصد مبلغ کل ${SeperatedNumberGenerate(totalWithoutBoth / 2)} ${currencyTxt} و تمام مبلغ کلیشه ${SeperatedNumberGenerate(kelisheTotalAmount)} ${currencyTxt} که جمعا معادل ${SeperatedNumberGenerate(finalTotalAmount)} ${currencyTxt} می‌شود در ابتدای کار دریافت خواهد شد`;
    } else if (cilandrItems.length > 0) {
        const finalTotalAmount = (totalWithoutBoth / 2) + cilandrTotalAmount;
        valueText = `50 درصد مبلغ کل ${SeperatedNumberGenerate(totalWithoutBoth / 2)} ${currencyTxt} و تمام مبلغ سیلندر ${SeperatedNumberGenerate(cilandrTotalAmount)} ${currencyTxt} که جمعا معادل ${SeperatedNumberGenerate(finalTotalAmount)} ${currencyTxt} می‌شود در ابتدای کار دریافت خواهد شد`;
    } else {
        valueText = `50 درصد از مبلغ کل معادل ${SeperatedNumberGenerate(sumTotal / 2)} ${currencyTxt} در ابتدای کار دریافت می‌شود `;
    }
    
    ["f_descriptionId", "f_descriptionIdMobile", "f_descriptionIdTablet"].forEach(id => {
        const element = document.getElementById(id);
        
        if (!element) return;
        element.value = valueText;
    });
};


const showFDescriptionItems = () => {
    let items = "";
    
    factorDescriptionItems.forEach((item, index) => {
        if (item.isDeleted) return;
        
        items += `
            <li class="d-flex justify-content-between gap-1">
                <p>${item.text}</p>
                <span onclick="deleteFDescriptionItem(${index})">
                    <i style="cursor: pointer;" class="fa fa-trash"></i>
                </span>
            </li>
        `;
    });
    
    const content = `<ul class="mp">${items}</ul>`;
    
    ["factorDescId", "factorDescIdMobile", "factorDescIdTablet"].forEach(id => {
        const element = document.getElementById(id);
        
        if (!element) return;
        element.innerHTML = content;
    });
};


const addFDescriptionItem = () => {
    let val;
    if (window.innerWidth >= 992 && window.innerWidth < 1200) {
        val = $("#f_descriptionIdTablet").val();
        
    } else if (window.innerWidth < 992) {
        val = $("#f_descriptionIdMobile").val();
    } else {
        val = $("#f_descriptionId").val();
    }
    
    factorDescriptionItems.push({ text: val, isDeleted: false, dir: "" });
    
    $("#f_descriptionId").val("");
    $("#f_descriptionIdMobile").val("");
    $("#f_descriptionIdTablet").val("");
    showFDescriptionItems();
    
};

const deleteFDescriptionItem = (index) => {
    if (factorDescriptionItems[index].id == null) {
        factorDescriptionItems.splice(index, 1);
    } else {
        factorDescriptionItems[index].isDeleted = true;
    }
    showFDescriptionItems();
};


const isAllowedToAddFactor = async (pfId) => {
    const data = await sendApiRequest({
        url: "/Factor/IsAllowedToAddFactor",
        params: { pfId },
    });
    
    if (data.status) return true;
    
    const errorModalElm = document.getElementById("ErrorModalId");
    //errorModalElm.querySelector(".modal-title").textContent = "برای صدور فاکتور ابتدا باید موارد زیر را برطرف نمایید:";
    
    const container = document.createElement("div");
    container.classList.add("error-list");
    data.errors.forEach(err => {
        const wrapper = document.createElement("div");
        wrapper.classList.add("error-item");
        
        wrapper.innerHTML = `
            <h6>${err.title}</h6>
            <div class="d-flex align-items-center gap-2">
                <p>${err.message}</p>
                ${err.code === 32 ? `<button class="btn btn-sm btn-secondary">صورتحساب</button>` : ""}
            </div>
        `;
        wrapper.querySelector("button")?.addEventListener("click", async () => {
            if (err.code === 32) {
                ModalToggle(errorModalElm, "hide");
                await ShowModalListInvoice(pfId, 1, "", 5, true);
            }
        });
        
        container.appendChild(wrapper);
    });
    errorModalElm.querySelector("#ErrorContentId").innerHTML = "";
    errorModalElm.querySelector("#ErrorContentId").appendChild(container);
    ModalToggle(errorModalElm);
    
    return false;
};


//OfficialRqModal
const submitFactor = async () => {
    if (!await mySer.CheckValidForm("#f_dateFormId") || !await mySer.CheckValidForm("#f_SerialFormId")) return;
        
    const activeItems = factorItems.filter(item => !item.isDeleted);
    if (!activeItems.length) {
        document.getElementById("FactorItemId").classList.add("AlertReq");
        toastifyError("فاکتور خالی است");
        return;
    }
    
    if (!factor.parentReturnedId) { // اگر فاکتور برگشتی نبود
        const sumTotalSupply = supplyData.reduce((sum, cur) => sum + (cur.supplyType === 3 ? -cur.totalAmount : cur.totalAmount), 0);
        if (factor.sumTotalAmount <= sumTotalSupply) {
            const result = await Swal.fire({
                title: "زیان در ثبت فاکتور!",
                text: "مبلغ نهایی فاکتور کمتر از بهای تمام‌شده است و ثبت آن منجر به زیان خواهد شد. آیا مایل به ثبت فاکتور هستید؟",
                icon: "warning",
                showCancelButton: true,
                cancelButtonColor: "#adadad",
                confirmButtonColor: "#0d6efd",
                cancelButtonText: "خیر",
                confirmButtonText: "بله، ثبت شود",
            });
            if (!result.isConfirmed) return;
        }
    }
    
    await submitFactorOK(Number(typeSubmitFactor));
};

const submitFactorOK = async (type, unlinkSepidar = false) => {
    FLshowLoading();
    factor.snFinancial = document.querySelector("#snFinancialFormId input[name='snFinancial']").value || null;
    factor.date = document.querySelector("#f_dateFormId input[name='f_date']").value;
    factor.descriptions = factorDescriptionItems;
    factor.export = document.getElementById("f_export").checked;
    
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    factor.currencyType = Number(currencyTypeElm.value);
    factor.currencyTypeTxt = currencyTypeElm.options[currencyTypeElm.selectedIndex].text.trim();
    
    $.post(`/Factor/AddFactor?unlinkSepidar=${unlinkSepidar}`, factor, async (res) => {
        FLshowLoading(false);
        if (res.status === 200) {
            if (type === 0) {
                await refreshFactorPartial();
                await GetPreFactorPartial();
            } else if (type === 1) {
                await getAllPreFactor();
                await refreshFactorPartial();
                getAllDetailAmount();
            } else if (type === 4) {
                if (isModalOpen("dealPreFactorsModal")) {
                    await refreshDealPreFactors();
                }
                if (isModalOpen("FactorsModal")) {
                    await refreshDealFactors();
                }
            } else if (type === 5) {
                await getAllFactorsModal(factor.pfactorID, 5);
            } else if (type === 6) {
                await getAllFactorsModal(factor.id, 6);
            }
            toastifySuccess("فاکتور با موفقیت ثبت شد.");
            cancelFactor();
            FLshowLoading(false);
        } else if (res.status === 201) {
            FLshowLoading(false);
            const result = await Swal.fire({
                title: "خطا در همگام‌سازی با سپیدار!",
                text: "فاکتور معادل در سیستم سپیدار یافت نشد. برای ثبت تغییرات، لازم است ارتباط این فاکتور با سپیدار قطع شود. آیا تایید می‌کنید؟",
                icon: "error",
                showCancelButton: true,
                cancelButtonColor: "#adadad",
                confirmButtonColor: "#e64942",
                cancelButtonText: "انصراف",
                confirmButtonText: "ارتباط قطع شود",
            });
            
            if (result.isConfirmed) {
                await submitFactorOK(type, true);
            }
        }
        
    }).fail((err) => {
        console.log(err);
        setTimeout(async () => await submitFactorOK(type), 3000);
    });
    
};


const cancelFactor = () => {
    const modalFactor = document.getElementById("Factor");
    ModalToggle(modalFactor, "hide");
    mySer.emptyForm("FacItem");
    $("#snFinancial").val("");
    $("#serialNumber").val("");
    $("#date").val("");
    $("#validityDade").val("");
    $("#sumTotalAmount").text("");
    $("#discount").val("");
    $("#totalTax").text("");
    $("#paymentAmount").text("");
    $("#f_descriptionId").val("");
    $("#F_currencyTypeSelect").val("0");
    
    factor = {};
    factorItems = [];
    $("#FactorItemId").children().remove();
    cancelUpdateFItem();
    document.getElementById("FactorItemId").classList.remove("AlertReq");
    
    const modalTitle = document.getElementById("factorModalTitle");
    modalTitle.textContent = "صدور فاکتور";
    modalTitle.style.backgroundColor = "#00302d";
    modalFactor.classList.remove("returned-factor");
    modalFactor.querySelectorAll(".modal-body .buttonCancel3").forEach((el) => {
        el.classList.replace("buttonCancel3", "buttonGreen3");
    });
};


const deleteFactor = async (id, type) => {
    await deleteItem({
        url: `/Factor/DelFactor/${id}`,
        callback: async () => {
            if (type === 0) {
                await refreshFactorPartial();
                //await GetPreFactorPartial();
                
            } else if (type === 1) {
                await getAllPreFactor();
                await refreshFactorPartial();
                
                getAllDetailAmount();
            } else if (type === 4) {
                await refreshDealFactors();
            } else if (type === 5 || type === 6) {
                await getAllFactorsModal(id, type);
            }
        },
    });
};


let currentFactorContext = {
    id: null,
    serialNumber: null,
    passType: 0,
    customerId: null,
};

const GetFactorPartial = async ({ id, element, serialNumber, passType = 0, showLoading = true, customerId = null } = {}) => {
    if (passType === 4) {
        return await showModalDealFactors(currentDealDocumentData);
    }
    
    currentFactorContext = { id, serialNumber, passType, customerId };
    
    updateFactorUI(element, passType, serialNumber);
    
    const model = {
        passType,
        customerId,
        fModel: {
            serVal: document.getElementById("f_searchId")?.value.trim() ?? "",
            _type: Number(document.querySelector("input[type='radio'][name='f_type']:checked")?.value ?? 0),
            filt: varList2?.filterVar || null,
            EntityId: id,
        },
    };
    
    await sendApiRequest({
        url: "/Cartable/FactorPartial",
        method: "POST",
        showLoading,
        data: model,
        successMsg: null,
        onSuccess: (res) => renderFactorPartial(res.data, passType),
    });
};


const refreshFactorPartial = async (showLoading = true) => {
    await GetFactorPartial({ ...currentFactorContext, showLoading });
};


const updateFactorUI = (element, passType, serialNumber) => {
    if (!element) return;
    
    document.querySelector(".SelectedPf")?.classList.remove("SelectedPf");
    element.closest(".InfoDataItem_Prefactor")?.classList.add("SelectedPf");
    
    if (passType === 0) {
        document.getElementById("f_all").checked = true;
        document.getElementById("HideFactorPartialId").classList.remove("d-none");
        document.getElementById("f_searchId").value = "";
    } else {
        toggleFactorTitle(serialNumber);
    }
    
    if (window.innerWidth < 1400) {
        ShowTab(passType === 0 ? "allFactorTab" : "factorTab");
    }
};

const renderFactorPartial = (data, passType) => {
    if (passType === 0) {
        document.getElementById("fAcceptedList").innerHTML = data.html;
        document.getElementById("fCountId").innerHTML = data.count;
        document.getElementById("fCountMobileId").innerText = data.count;
        
    } else {
        document.getElementById("DetailDivList").innerHTML = data.html;
        document.getElementById("factorCount").innerHTML = data.count;
        document.getElementById("DivFacListId").classList.remove("d-none");
    }
};

const toggleFactorTitle = (serialNumber) => {
    const allFactorTitle = document.getElementById("allFactorTitle");
    const relatedFactorTitle = document.getElementById("relatedFactorTitle");
    
    if (serialNumber) {
        allFactorTitle.classList.add("d-none");
        relatedFactorTitle.classList.remove("d-none");
        document.getElementById("relatedPfSerialNumber").textContent = serialNumber;
    } else {
        allFactorTitle.classList.remove("d-none");
        relatedFactorTitle.classList.add("d-none");
    }
};

const showSepidarFactors = async (pfSerialNumber, element, passType) => { // PassType: 1 = customerProfile | 0 = cartable
    if (passType === 4) {
        return await showModalDealFactors(currentDealDocumentData, pfSerialNumber);
    }
    updateFactorUI(element, passType, pfSerialNumber);
    
    await sendApiRequest({
        url: "/Customer/GetSepidarFactors",
        params: { pfSerialNumber },
        onSuccess: (res) => renderFactorPartial(res.data, passType),
    });
};


let crmFactorId;
const showSepidarFactorsModal = async (pfSerialNumber, fId, type) => {
    await sendApiRequest({
        url: "/Customer/GetSepidarFactors",
        params: { pfSerialNumber, passType: 2 },
        onSuccess: (res) => {
            document.getElementById("sepidarFactors").innerHTML = res.data.html;
            ModalToggle("sepidarFactorsModal");
            
            typeSubmitFactor = type;
            crmFactorId = fId;
        },
    });
};

const updateFactorFinancial = async (sepidarId) => {
    await sendApiRequest({
        url: "/Factor/UpdateFactorFinancial",
        method: "POST",
        data: { crmFactorId, sepidarId },
        onSuccess: async () => {
            if (typeSubmitFactor === 0) {
                await refreshFactorPartial();
            } else if (typeSubmitFactor === 1) {
                getAllDetailAmount();
                await refreshFactorPartial();
            } else if (typeSubmitFactor === 4) {
                await refreshDealFactors();
            } else if (typeSubmitFactor === 6) {
                await getAllFactorsModal(model.crmFactorId, 6);
            }
            ModalToggle("sepidarFactorsModal", "hide");
        },
    });
};


const hideSelectedFactor = async () => {
    document.getElementById("HideFactorPartialId").classList.add("d-none");
    document.querySelector(".SelectedPf")?.classList.remove("SelectedPf");
    
    await GetFactorPartial();
};


const hideFact = async () => {
    // document.getElementById("DivFacListId").classList.add("d-none");
    await GetFactorPartial({ passType: 1, customerId });
    
    document.querySelector(".SelectedPf").classList.remove("SelectedPf");
    
    toggleFactorTitle();
    
    if (window.innerWidth < 1400) {
        ShowTab("preFactorTab");
    }
};

const getAllFactorsModal = async (id, type, pfSerialNumber) => {
    if (!id) return;
    
    const apiUrl = pfSerialNumber
        ? `/Customer/GetSepidarFactors?pfSerialNumber=${pfSerialNumber}`
        : `/Cartable/GetAllFactorsPartialModal?id=${id}&type=${type}`;
    
    await sendApiRequest({
        url: apiUrl,
        onSuccess: async (res) => {
            const factorsWrapper = document.getElementById("factorsBodyModal");
            if (res.status === 200) {
                if (res.data.count) {
                    factorsWrapper.innerHTML = res.data.html;
                } else {
                    factorsWrapper.innerHTML = `
                        <div class="d-flex flex-column align-items-center pb-4">
                            <img src="/Img/no-data.jpg" style="width: clamp(120px, 20vw, 180px)" alt="">
                            <div style="color: #506a77;font-size: clamp(13px, 2.5vw, 15px)">هیچ فاکتوری برای نمایش وجود ندارد!</div>
                        </div>
                    `;
                    factorsWrapper.style.background = "#fff";
                }
            }
        },
    });
};


const acceptFactor = async (id, type, passType) => {
    await sendApiRequest({
        url: "/Factor/AcceptFactor",
        params: { id, type },
        successMsg: type === 2 ? "با موفقیت تایید نهایی شد" : type === 3 ? "ابطال شد" : null,
        onSuccess: async () => {
            if (passType === 0) {
                await refreshFactorPartial();
            } else if (passType === 1) {
                getAllDetailAmount();
                await refreshFactorPartial();
            } else if (passType === 4) {
                await refreshDealFactors();
            } else if (passType === 6) {
                await getAllFactorsModal(id, 6);
            }
        },
    });
};