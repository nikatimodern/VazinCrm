let f_officialType = 1;
let factor = {};
let typeSubmitFactor = null;
let fItemUpIndex = null;
let _factorItems = [];
let FactorDescItems;
let supplyData = [];

document.querySelectorAll("#Factor input[type='radio'][name='f_official']")?.forEach(radio => {
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    radio.addEventListener("change", async (e) => {
        f_officialType = Number(e.target.value);
        currencyTypeElm.toggleAttribute("disabled", f_officialType === 1);
        await calculateTotalFactor();
    });
});

document.querySelector("#FacItem input[name='weightCount']")?.addEventListener("input", () => calculateItem("FacItem"));
document.querySelector("#FacItem input[name='unitAmount']")?.addEventListener("input", () => calculateItem("FacItem"));

const AddFactor = async (pfId = null, type = 1) => {
    
    if (!await isAllowedToAddFactor(pfId)) return;
    typeSubmitFactor = type;
    currentFactorContext = { passType: type };
    
    FLshowLoading();
    getAllProductOp("FacItem");
    Unit(2);
    
    const baseDesFactor = await getBaseDescription(1);
    const pfData = (await axios(`/Factor/GetPFactor/${pfId}`)).data;
    supplyData = (await axios(`/Supply/GetSupplies?pfId=${pfId}`)).data;
    
    dealData.id = pfData.dealId;
    
    if (!supplyData?.length) {
        toastifyError("بهای تامینی ثبت نشده است!");
        FLshowLoading(false);
        return;
    }
    
    _factorItems = pfData.preFactorItems;
    
    _factorItems = _factorItems.filter(pfItem => supplyData.some(sup => sup.pfItemId === pfItem.id));
    _factorItems.forEach(pfItem => {
        pfItem.weightCount = supplyData.filter(sup => sup.pfItemId === pfItem.id).reduce((sum, cur) => sum + Number(cur.weight), 0);
        pfItem.totalAmount = calcTotalAmount(pfItem.weightCount, pfItem.unitAmount);
    });
    
    
    Array.from(pfData.descriptions).forEach((item) => {
        item.id = null;
    });
    
    Array.from(_factorItems).forEach((item) => {
        item.pfItemId = item.id;
        item.id = null;
    });
    
    customerId = pfData.customerId;
    factor = pfData;
    factor.pfactorID = pfData.id;
    factor.pfactorSerialNumber = pfData.serialNumber;
    factor.serialNumber = null;
    factor.id = null;
    factor.factorItems = _factorItems;
    factor.date = await todayDate();
    
    factor.officialType = pfData.officialType;
    f_officialType = pfData.officialType;
    
    factor.descriptions = [];
    FactorDescItems = [];
    
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    currencyTypeElm.value = pfData.currencyType ?? 0;
    currencyTypeElm.toggleAttribute("disabled", f_officialType === 1);
    
    if (factor.officialType === 0) {
        baseDesFactor.filter(f => f.official === false).map((item) => {
            item = { ...item, isDeleted: false };
            factor.descriptions.push(item);
            FactorDescItems.push(item);
        });
    } else {
        baseDesFactor.filter(f => f.official === true).map((item) => {
            item = { ...item, isDeleted: false };
            factor.descriptions.push(item);
            FactorDescItems.push(item);
        });
    }
    
    document.getElementsByName("f_official").forEach((o) => {
        if (o.type === "radio" && Number(o.value) === f_officialType) {
            o.checked = true;
        }
    });
    
    const exportValue = pfData.export ? "1" : "0";
    document.getElementsByName("f_export").forEach(r => {
        r.checked = (r.value === exportValue);
    });
    
    $("#f_serialNumber").val(factor.serialNumber);
    $("#f_date").val(factor.date);
    //$("#validityDade").val(factor.validityDade);
    
    /*    $("#f_sumTotalAmount").text(SeperatedNumberGenerate(factor.sumTotalAmount));
        $("#f_totalTax").text(SeperatedNumberGenerate(factor.totalTax));
        $("#f_paymentAmount").text(SeperatedNumberGenerate(factor.paymentAmount));*/
    $("#f_discount").val(factor.discount.toLocaleString());
    await calculateTotalFactor();
    $("#Factor").modal("show");
    
    ShowItemFactor();
    showFactorDescItem();
    FLshowLoading(false);
};


const upFactor = (fid, cid, type = 1) => {
    typeSubmitFactor = type;
    currentFactorContext.passType = type;
    
    customerId = cid;
    
    document.getElementById("FLshowLoading").style.display = "";
    Unit(2);
    getAllProductOp("FacItem");
    $.get(baseUrl + `factor/getFactor/${fid}`, (res) => {
        factorID = res.id;
        factor = res;
        _factorItems = res.factorItems;
        f_officialType = res.officialType;
        FactorDescItems = res.descriptions;
        
        const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
        currencyTypeElm.value = res.currencyType ?? 0;
        currencyTypeElm.toggleAttribute("disabled", f_officialType === 1);
        
        document.getElementsByName("f_official").forEach((o, v) => {
            if (o.type === "radio" && o.value == factor.officialType) {
                o.checked = true;
            }
        });
        
        document.getElementsByName("f_export").forEach((o, v) => {
            if (o.type === "radio" && o.value == res.export) {
                o.checked = true;
            }
        });
        
        
        Array.from(document.getElementById("f_SerialFormId").elements)
             .filter(f => f.name === "f_serialNumber")[0].value = factor.serialNumber;
        Array.from(document.getElementById("f_dateFormId").elements)
             .filter(f => f.name === "f_date")[0].value = factor.date;
        Array.from(document.getElementById("snFinancialFormId").elements)
             .filter(f => f.name === "snFinancial")[0].value = factor.snFinancial;
        
        
        ShowItemFactor();
        showFactorDescItems();
        
        $("#f_sumTotalAmount").text(SeperatedNumberGenerate(factor.sumTotalAmount));
        $("#f_discount").val(SeperatedNumberGenerate(factor.discount));
        $("#f_totalTax").text(SeperatedNumberGenerate(factor.totalTax));
        
        $("#f_reciveAmount").val(SeperatedNumberGenerate(factor.reciveAmount));
        $("#f_paymentAmount").text(SeperatedNumberGenerate(factor.paymentAmount));
        
        $("#Factor").modal("show");
        document.getElementById("FLshowLoading").style.display = "none";
        
    });
};


const returnFactor = async (fid, cid, type = 1) => {
    typeSubmitFactor = type;
    currentFactorContext.passType = type;
    
    customerId = cid;
    const modalFactor = document.getElementById("Factor");
    modalFactor.classList.add("returned-factor");
    
    
    FLshowLoading();
    Unit(2);
    getAllProductOp("FacItem");
    factor = (await axios(`/factor/getFactor/${fid}`)).data;
    factor.id = null;
    factor.serialNumber = null;
    factor.parentReturnedId = fid;
    factor.sepidarId = null;
    factor.snFinancial = null;
    factor.descriptions = factor.descriptions.map((f) => ({ ...f, id: null }));
    factor.factorItems = factor.factorItems.map((f) => ({ ...f, id: null }));
    factor.date = await todayDate();
    
    _factorItems = factor.factorItems;
    f_officialType = factor.officialType;
    FactorDescItems = factor.descriptions;
    
    
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    currencyTypeElm.value = factor.currencyType ?? 0;
    toggleElementsDisabled(currencyTypeElm, f_officialType === 1);
    
    
    modalFactor.querySelectorAll("input[name='f_official']").forEach((i) => {
        i.checked = Number(i.value) === f_officialType;
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
    
    
    ShowItemFactor();
    showFactorDescItems();
    modalFactor.querySelector("#f_sumTotalAmount").textContent = factor.sumTotalAmount.toLocaleString();
    modalFactor.querySelector("#f_totalTax").textContent = factor.totalTax.toLocaleString();
    modalFactor.querySelector("#f_paymentAmount").textContent = factor.paymentAmount.toLocaleString();
    modalFactor.querySelector("#f_discount").value = factor.discount.toLocaleString();
    
    ModalToggle(modalFactor, "show");
    FLshowLoading(false);
};
const calcTotalAmount = (weight, unitValue) => {
    const w = parseFloat(weight);
    const u = parseFloat(unitValue);
    
    if (isNaN(w) || isNaN(u)) return 0;
    
    return w * u;
};


const calculateTotalFactor = async () => {
    return new Promise((resolve, reject) => {
        try {
            let data = _factorItems.filter(f => f.isDeleted !== true);
            let _SumTotalAmount = 0;
            let _TotalTax = 0;
            let _PaymentAmount;
            
            let _Discount = Number(SeparatedNumberRemove(document.getElementById("f_discount").value)) || 0;
            
            data.forEach(item => {
                let itemAmount = item.totalAmount;
                _SumTotalAmount += itemAmount;
            });
            
            _SumTotalAmount = Math.round(_SumTotalAmount);
            _Discount = Math.round(_Discount);
            
            if (f_officialType === 1) {
                _TotalTax = Math.round(((_SumTotalAmount - _Discount) * 10) / 100);
            }
            
            _PaymentAmount = _SumTotalAmount - _Discount + _TotalTax;
            factor.sumTotalAmount = _SumTotalAmount;
            factor.discount = _Discount;
            factor.totalTax = _TotalTax;
            factor.paymentAmount = Math.round(_PaymentAmount);
            
            
            factor.officialType = f_officialType;
            
            document.getElementById("f_sumTotalAmount").innerText = factor.sumTotalAmount.toLocaleString();
            document.getElementById("f_totalTax").innerText = factor.totalTax.toLocaleString();
            document.getElementById("f_paymentAmount").innerText = factor.paymentAmount.toLocaleString();
            return resolve(true);
        } catch {
            return reject(false);
        }
    });
    
};
const showFactorDescItem = () => {
    let items = "";
    
    FactorDescItems.forEach((item, index) => {
        items += `
            <li class="mp d-flex">
                <p>${item.text}</p>
                <span onclick="DelFactorDescItem(${index})">
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

const ShowItemFactor = () => {
    let ob = `<div class="d-flex flex-column w-100" style="gap:5px">`;
    _factorItems.forEach((item, index) => {
        if (item.product == null) item.product = "";
        _factorItems[index].row = index + 1;
        const uAmount = item.unitAmount.toLocaleString();
        const tAmount = item.totalAmount.toLocaleString();
        
        
        if (item.productSmallName == null) {
            item.productSmallName = "";
        }
        
        ob += `
            <div class="d-flex w-100">
                <div class="InfoDataItemHeadFactor" style="display:flex;width:24px;flex-shrink: 0">
                   <label class="fbold">${index + 1}</label>
                </div>
                <div class="InfoDataItemFactor">
                    <div style="display:flex;justify-content:space-between; width:100%;flex-wrap:wrap;">
                       <div class="col-12 col-lg-5 d-flex flex-column align-items-center justify-content-around">
                           <label>شرح کالا</label>
                           <label class="px-2 d-flex gap-1 fbold">
                                <span>${item.productSmallName}</span>
                                <span>${item.product}</span>
                           </label>
                       </div>
                       <div class="d-lg-none" style="width:90%;margin-block: 4px;margin-inline: auto; height:1px; background-color:rgba(0,0,0,0.15);">
                        </div>
                       <div class="col-6 col-lg-1 d-flex flex-column align-items-center justify-content-around" style="white-space: nowrap">
                           <label style="display:flex">تعداد/مقدار</label>
                           <label class="fbold">${item.weightCount}</label>
                       </div>
                       <div class="col-6 col-lg-1 d-flex flex-column align-items-center justify-content-around" style="white-space: nowrap">
                           <label>واحد</label>
                           <label class="fbold">${item.unitTxt}</label>
                       </div>
                        <div class="d-lg-none" style="width:90%;margin-block: 4px;margin-inline: auto; height:1px; background-color:rgba(0,0,0,0.15);">
                        </div>
                       <div class="col-6 col-lg-2 d-flex flex-column align-items-center justify-content-around" style="white-space: nowrap">
                           <label>مبلغ واحد</label>
                           <label class="fbold">${uAmount}</label>
                       </div>
                       <div class="col-6 col-lg-2 d-flex flex-column align-items-center justify-content-around" style="white-space: nowrap">
                           <label>مبلغ کل</label>
                           <label class="fbold">${tAmount}</label>
                       </div>
                    </div>
                    
                    <div class="InfoDataItemOp col-auto" style="padding-inline: 6px;flex-shrink: 0">
                       <button class="flex-center"><i class="mp fa fa-trash" onclick="delFactorItem(${index})"></i></button>
                       <button class="flex-center"><i class="mp fa fa-pencil" onclick="upFactorItem(${index})"></i></button>
                    </div>
                </div>
            </div>
        `;
    });
    
    ob += `</div>`;
    
    $("#FactorItemId").children().remove();
    $("#FactorItemId").append(ob);
};


const calculateTextFactor = () => {
    const model = _factorItems.filter(f => !f.isDeleted);
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    const currencyTxt = currencyTypeElm.options[currencyTypeElm.selectedIndex].text.trim();
    
    let sumTotal = $("#f_sumTotalAmount").text();
    let totalTax = $("#f_totalTax").text();
    let sumTotalTax = $("#f_paymentAmount").text();
    
    sumTotal = Number(SeparatedNumberRemove(sumTotal));
    totalTax = Number(SeparatedNumberRemove(totalTax));
    sumTotalTax = Number(SeparatedNumberRemove(sumTotalTax));
    
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
    
    $("#f_descriptionId").val(valueText);
    $("#f_descriptionIdMobile").val(valueText);
    $("#f_descriptionIdTablet").val(valueText);
};


const showFactorDescItems = () => {
    let items = "";
    FactorDescItems.forEach((item, index) => {
        if (item.isDeleted === false) {
            items = items + `<li style="display:flex;gap:2px"><p class="mp"> ${item.text}</p> <span onclick="DelFactorDescItem(${index})"> <i style="cursor:pointer;" class="fa fa-trash"></i> </span>  </li>`;
            
        }
    });
    items = "<ul class='mp'>" + items + "</ul>";
    $("#factorDescId").children().remove();
    $("#factorDescIdTablet").children().remove();
    $("#factorDescIdMobile").children().remove();
    
    $("#factorDescId").append(items);
    $("#factorDescIdTablet").append(items);
    $("#factorDescIdMobile").append(items);
};

const addFactorDescItem = () => {
    let val;
    if (window.innerWidth >= 992 && window.innerWidth < 1200) {
        val = $("#f_descriptionIdTablet").val();
        
    } else if (window.innerWidth < 992) {
        val = $("#f_descriptionIdMobile").val();
    } else {
        val = $("#f_descriptionId").val();
    }
    
    FactorDescItems.push({ text: val, isDeleted: false, dir: "" });
    
    $("#f_descriptionId").val("");
    $("#f_descriptionIdMobile").val("");
    $("#f_descriptionIdTablet").val("");
    showFactorDescItems();
    
};

const DelFactorDescItem = (index) => {
    if (FactorDescItems[index].id == null) {
        FactorDescItems.splice(index, 1);
        showFactorDescItems();
    } else {
        FactorDescItems[index].isDeleted = true;
        showFactorDescItems();
    }
};

const addFactorItem = async () => {
    
    if (await mySer.CheckValidForm("#FacItem")) {
        
        let data = mySer.convertFormToJSON("#FacItem");
        let el_procode = Array.from(document.getElementById("FacItem").elements)
                              .filter(f => f.name === "productCode")[0];
        
        if (el_procode.value !== "") {
            
            data.productSmallName = el_procode.options[el_procode.selectedIndex].text;
        }
        
        let el_Unit = Array.from(document.getElementById("FacItem").elements).filter(f => f.name === "unit")[0];
        data.unitTxt = el_Unit.options[el_Unit.selectedIndex].text;
        data.unit = Number(data.unit);
        data.id = null;
        
        _factorItems.push(data);
        factor.factorItems = _factorItems;
        ShowItemFactor();
        mySer.emptyForm("FacItem");
        
        let calStatus = await calculateTotalFactor();
        
        //calculateTextFactor();
        
        el_procode.focus();
        
        return true;
    } else {
        return false;
    }
};

const isAllowedToAddFactor = async (pfId) => {
    const res = await axios(`/Factor/IsAllowedToAddFactor?pfId=${pfId}`);
    if (res.data?.status === false) {
        const errorModalElm = document.getElementById("ErrorModalId");
        //errorModalElm.querySelector(".modal-title").textContent = "برای صدور فاکتور ابتدا باید موارد زیر را برطرف نمایید:";
        
        const container = document.createElement("div");
        container.classList.add("error-list");
        res.data.errors.forEach(err => {
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
    }
    return true;
};


//OfficialRqModal
const submitFactor = async () => {
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
    if (await mySer.CheckValidForm("#f_dateFormId") && await mySer.CheckValidForm("#f_SerialFormId")) {
        if (_factorItems.length <= 0) {
            $("#preFactorItemId").parent().css("border", "2px solid #ff6d00");
            toastifyError("فاکتور خالی");
            return;
        }
        
        FLshowLoading();
        factor.snFinancial = document.querySelector("#snFinancialFormId input[name='snFinancial']").value || null;
        factor.date = document.querySelector("#f_dateFormId input[name='f_date']").value;
        factor.officialType = f_officialType;
        factor.customerId = customerId;
        factor.descriptions = FactorDescItems;
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
                    if (factor.id) await refreshFactorPartial();
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
                ModalToggle("#Factor");
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
        
        
    }
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
    _factorItems = [];
    $("#FactorItemId").children().remove();
    UpfItemCancel();
    
    const modalTitle = document.getElementById("factorModalTitle");
    modalTitle.textContent = "صدور فاکتور";
    modalTitle.style.backgroundColor = "#00302d";
    modalFactor.classList.remove("returned-factor");
    modalFactor.querySelectorAll(".modal-body .buttonCancel3").forEach((el) => {
        el.classList.replace("buttonCancel3", "buttonGreen3");
    });
};


const delFactor = (id, type) => {
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
        .then((willDelete) => {
            
            if (willDelete.isConfirmed) delFactorOk(id, type);
        });
};
const delFactorOk = (id, type) => {
    if (!id) return;
    
    FLshowLoading();
    $.get(baseUrl + "Factor/DelFactor/" + id, async () => {
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
        
        FLshowLoading(false);
        toastifySuccess("عملیات انجام شد");
    })
     .fail(() => {
         setTimeout(() => delFactorOk(id, type), 3000);
     });
    
};


//PasType==1 => customerFactorList , PasType==0 => CartableFactorList
const getFacSepidar = async (pfId, serialNumber = null, _this, PassType, dealId) => { // TODO
    if (PassType === 4) {
        const dealRes = await axios(`/Deal/GetDeal/${dealId}`);
        await showModalDealFactors(dealRes.data, serialNumber);
        return;
    }
    FLshowLoading();
    document.querySelector(".SelectedPf")?.classList.remove("SelectedPf");
    _this.closest(".InfoDataItem_Prefactor").classList.add("SelectedPf");
    
    const res = await getAllFactorSepidar(pfId, serialNumber);
    
    if (PassType === 1) {
        document.getElementById("DetailDivList").innerHTML = res;
        document.getElementById("DivFacListId").style.display = "flex";
        document.getElementById("FactorDivTitleId").innerHTML = pfId
            ? `
                <button class="buttonGreen hide-factor__btn" onclick="hideFact()">
                    <i class="fas fa-times"></i>
                </button>
                <span style="font-size: 20px">تمام فاکتورهای مربوط به پیش فاکتور  ${serialNumber} </span>`
            : `<span style="font-size:20px">تمام فاکتور ها</span>`;
        
    } else if (PassType === 0) {
        document.getElementById("fAcceptedList").innerHTML = res;
        document.getElementById("f_all").checked = true;
        document.getElementById("HideFactorPartialId").style.display = "flex";
    }
    
    if (window.innerWidth < 1400) {
        const allFactorTab = new bootstrap.Tab(document.getElementById(PassType === 0 ? "allFactorTab" : "factorTab"));
        allFactorTab.show();
    }
    FLshowLoading(false);
};

const getAllFactorSepidar = async (pfId = null, ser = null) => {
    return new Promise(resolve => {
        $.get(baseUrl + `Customer/GetFactorsSepidar?customerId=${customerId}&pfId=${pfId}&sNumber=${ser}`, (res) => {
            resolve(res.html);
        })
         .fail(() => {
             setTimeout(() => getAllFactorSepidar(), 3000);
         });
        
    });
};

let crmFactorId;
const showSepidarFactorsModal = async (pfSerialNumber, fId, type) => {
    typeSubmitFactor = type;
    crmFactorId = fId;
    try {
        FLshowLoading();
        const res = await axios(`/Factor/GetSepidarFactors?pfSerialNumber=${pfSerialNumber}`);
        if (res.status === 200) {
            document.getElementById("sepidarFactors").innerHTML = res.data;
            $("#sepidarFactorsModal").modal("toggle");
        }
        FLshowLoading(false);
    } catch (err) {
        console.log(err);
    }
};
const updateFactorFinancial = async (sepidarId) => {
    const model = { crmFactorId, sepidarId };
    try {
        FLshowLoading();
        const res = await axios.post(`/Factor/UpdateFactorFinancial`, model);
        if (res.status === 200) {
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
            toastifySuccess("با موفقیت ثبت شد");
            $("#sepidarFactorsModal").modal("hide");
        }
    } catch (err) {
        toastifyError(err.response.data || "خطای سرور");
        console.log(err);
    } finally {
        FLshowLoading(false);
    }
};


let currentFactorContext = {
    id: null,
    serialNumber: null,
    passType: 0,
};

const GetFactorPartial = async ({ id, element, serialNumber, passType = 0, dealId, showLoading = true } = {}) => {
    if (passType === 4 && dealId) {
        const dealRes = await axios(`/Deal/GetDeal/${dealId}`);
        await showModalDealFactors(dealRes.data);
        return;
    }
    
    currentFactorContext = { id, serialNumber, passType };
    
    updateFactorUI(element, passType);
    
    const model = {
        passType,
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
        onSuccess: (res) => renderFactorPartial(res.data, { id, serialNumber, passType }),
    });
};


const updateFactorUI = (element, passType) => {
    if (!element) return;
    
    document.querySelector(".SelectedPf")?.classList.remove("SelectedPf");
    element.closest(".InfoDataItem_Prefactor")?.classList.add("SelectedPf");
    
    if (passType === 0) {
        document.getElementById("f_all").checked = true;
        document.getElementById("HideFactorPartialId").style.display = "flex";
        document.getElementById("f_searchId").value = "";
    }
    
    if (window.innerWidth < 1400) {
        ShowTab(passType === 0 ? "allFactorTab" : "factorTab");
    }
    
};

const renderFactorPartial = (data, { id, serialNumber, passType }) => {
    if (passType === 0) {
        document.getElementById("fAcceptedList").innerHTML = data.html;
        document.getElementById("fCountId").innerHTML = data.count;
        document.getElementById("fCountMobileId").innerText = data.count;
        return;
    }
    
    document.getElementById("FactorDivTitleId").innerHTML = id != null ? `
        <button class="buttonGreen hide-factor__btn" onclick="hideFact()">
            <i class="fas fa-times"></i>
        </button>
        <span style="font-size:20px">تمام فاکتورهای مربوط به پیش فاکتور ${serialNumber || ""}</span>`
        : `<span style="font-size:20px">تمام فاکتور ها</span>`;
    document.getElementById("DivFacListId").style.display = "flex";
    document.getElementById("DetailDivList").innerHTML = data.html;
};

const refreshFactorPartial = async (showLoading = true) => {
    await GetFactorPartial({ ...currentFactorContext, showLoading });
};

const hideSelectedFactor = async () => {
    document.getElementById("HideFactorPartialId").style.display = "none";
    document.querySelector(".SelectedPf")?.classList.remove("SelectedPf");
    
    await GetFactorPartial();
};

const getAllFactorsModal = async (id, type, sNumber) => {
    if (!id) return;
    
    FLshowLoading();
    if (type === 4) dealData.id = id;
    const factorsWrapper = document.getElementById("factorsBodyModal");
    const apiUrl = sNumber
        ? `/Customer/GetFactorsSepidar?sNumber=${sNumber}`
        : `/Cartable/GetAllFactorsPartialModal?id=${id}&type=${type}`;
    
    const res = await axios(apiUrl);
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
        FLshowLoading(false);
    }
};


const acceptFactor = async (id, type, passType, PfactorSerialNumber) => {
    try {
        FLshowLoading();
        const res = await axios(`${baseUrl}Factor/AcceptFactor?id=${id}&type=${type}`);
        
        if (res.status === 200) {
            if (type === 2) {
                toastifySuccess("با موفقیت ثبت نهایی شد.");
            } else if (type === 3) {
                toastifySuccess("ابطال انجام شد.");
            } else if (type === 1) {
                toastifySuccess("برگشت انجام شد.");
            }
            
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
            
        } else if (res.status === 204) {
            toastifyError("شماره فاکتور مالی وارد نشده است!");
        } else {
            toastifyError("خطا در ارسال اطلاعات!");
        }
        FLshowLoading(false);
    } catch (err) {
        console.log(err);
        setTimeout(async () => await acceptFactor(id, type, passType, PfactorSerialNumber), 3000);
    }
};
const addItemFactor = async () => {
    FLshowLoading();
    const isValid = await addFactorItem();
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


const upFactorItem = (index) => {
    if (window.innerWidth < 992) {
        ModalToggle("ItemFactorModal", "show");
    }
    fItemUpIndex = index;
    mySer.convertJsonToForm(_factorItems[index], "FacItem");
    
    document.getElementById("f_AddBtnId").style.display = "none";
    document.getElementById("f_CancelBtnId").style.display = "none";
    document.getElementById("f_UpBtnId").style.display = "flex";
};


const UpfItemCancel = () => {
    mySer.emptyForm("FacItem");
    fItemUpIndex = null;
    
    document.getElementById("f_AddBtnId").style.display = "flex";
    document.getElementById("f_CancelBtnId").style.display = "flex";
    document.getElementById("f_UpBtnId").style.display = "none";
    
    if (window.innerWidth < 992) {
        canselModalItemFactor();
    }
};


const UpfItem = async () => {
    
    const productCodeElm = document.querySelector("#FacItem select[name='productCode']");
    
    let data = mySer.convertFormToJSON("#FacItem");
    
    
    if (productCodeElm.value) {
        data.productSmallName = productCodeElm.options[productCodeElm.selectedIndex].text;
    }
    
    data.id = _factorItems[fItemUpIndex].id;
    data.factorId = _factorItems[fItemUpIndex].factorId;
    data.pfItemId = _factorItems[fItemUpIndex].pfItemId;
    
    
    //اگر کسب و کار کوچک بود این موارد باید برداشته شود
    data.unit = _factorItems[fItemUpIndex].unit;
    data.unitTxt = _factorItems[fItemUpIndex].unitTxt;
    //تا اینجا
    
    //اگر کسب و کار کوچک بود این موارد باید فعال شود
    //var el_Unit = Array.from(document.getElementById("FacItem").elements).filter(f => f.name == "unit")[0]
    //if (el_Unit.value != "") {
    //    data.unitTxt = el_Unit.options[el_Unit.selectedIndex].text;
    //}
    //تا اینجا
    
    _factorItems[fItemUpIndex] = data;
    
    data.unit = Number(data.unit);
    
    
    $("#f_AddBtnId").css("display", "flex");
    $("#f_UpBtnId").css("display", "none");
    ShowItemFactor();
    fItemUpIndex = null;
    mySer.emptyForm("FacItem");
    let calcStatus = await calculateTotalFactor();
    //calculateTextFactor();
    
    
    toastifySuccess("با موفقیت ویرایش شد!");
    
    if (window.innerWidth < 992) {
        canselModalItemFactor();
    }
};


const delFactorItem = async (index) => {
    let item = _factorItems[index];
    
    if (item.id != null) {
        item.isDeleted = true;
        
        _factorItems = _factorItems.filter(f => f.isDeleted !== true);
        ShowItemFactor();
        await calculateTotalFactor();
        calculateTextFactor();
    } else {
        _factorItems.splice(index, 1);
        ShowItemFactor();
        await calculateTotalFactor();
        calculateTextFactor();
    }
};