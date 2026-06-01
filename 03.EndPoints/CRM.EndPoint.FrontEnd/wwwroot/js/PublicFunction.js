var _officialType = 0;
var f_officialType = 1;

var pfactor = {};
var factor = {};


var _pfactorItems = [];
var _factorItems = [];


let dealData = {};
var PreFactorDescItems;
var FactorDescItems;

var PfItemUpIndex = null;
var fItemUpIndex = null;

var customerId = null;
var typeSubmitPfactor = null;
var typeSubmitfactor = null;
var pfAbort = null;

//document.addEventListener("DOMContentLoaded", function (event) {
//    Unit();
//});


const addDes = (e) => {


};


const f_getAllProductOp = async () => {
    
    return new Promise(async (resolve) => {
        const el = document.querySelector("#FacItem select[name='productCode']");
        $.get(baseUrl + "Product/GetAllProduct", (res) => {
            
            $(el).select2({
                data: res,
                dir: "rtl",
                placeholder: "انتخاب",
                selectionCssClass: "form-control",
                dropdownParent: window.innerWidth < 992 ? $("#ItemFactorModal") : $("#Factor"),
            });
            $(el).val(null).trigger("change");
            
            resolve();
            
        }).fail(() => {
            setTimeout(() => {
                f_getAllProductOp();
            }, 3000);
        });
        
        
    });
    
    
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


const calculateTotal = () => {
    let data = _pfactorItems.filter(f => f.isDeleted !== true);
    let _SumTotalAmount = 0;
    let _TotalTax = 0;
    
    let _PaymentAmount;
    let _Discount = Number(SeparatedNumberRemove($("#discount").val()));
    
    data.forEach(item => {
        _SumTotalAmount = Number(_SumTotalAmount) + Number(SeparatedNumberRemove(item.totalAmount));
    });
    
    if (_officialType === 1) {
        _TotalTax = Math.round(((_SumTotalAmount - _Discount) * 10) / 100);
    }
    _PaymentAmount = (Number(_SumTotalAmount) - _Discount + Number(_TotalTax));
    
    pfactor.sumTotalAmount = Math.round(_SumTotalAmount);
    pfactor.discount = Math.round(_Discount);
    pfactor.totalTax = Math.round(_TotalTax);
    pfactor.paymentAmount = Math.round(_PaymentAmount);
    
    $("#sumTotalAmount").text(pfactor.sumTotalAmount.toLocaleString());
    $("#discount").val(pfactor.discount.toLocaleString());
    $("#totalTax").text(pfactor.totalTax.toLocaleString());
    $("#paymentAmount").text(pfactor.paymentAmount.toLocaleString());
};
function calculateItem(formId, weightSelector = "[name='weightCount']", unitSelector = "[name='unitAmount']", totalSelector = "[name='totalAmount']") {
    const form = document.getElementById(formId);
    const getValue = (selector) => {
        const el = form.querySelector(selector);
        if (el.tagName === "INPUT" || el.tagName === "TEXTAREA") return el.value;
        return el.innerText;
    };
    const setValue = (selector, val) => {
        const el = form.querySelector(selector);
        if (el.tagName === "INPUT" || el.tagName === "TEXTAREA") {
            el.value = val;
        } else {
            el.innerText = val;
        }
    };
    
    let unitAmount = Number(SeparatedNumberRemove(getValue(unitSelector)));
    let weightCount = Number(getValue(weightSelector));
    let total = Math.round(unitAmount * weightCount);
    
    if (total.toString().includes(".")) {
        total = Number(total.toString().split(".")[0]);
    }
    
    setValue(totalSelector, total.toLocaleString());
}
const calcTotalAmount = (weight, unitValue) => {
    const w = parseFloat(weight);
    const u = parseFloat(unitValue);
    
    if (isNaN(w) || isNaN(u)) return 0;
    
    return w * u;
};


if (window.innerWidth < 992) {
    document.getElementById("PreFacItemContainer")?.remove();
    document.getElementById("FacItemContainer")?.remove();
    document.getElementById("orderFormContainer")?.remove();
    const descriptionOrder = document.getElementById("descriptionOrder");
    if (descriptionOrder) descriptionOrder.rows = 3;
}


el_pf_weightCount = Array.from(document.getElementById("PreFacItem")?.elements ?? []).find(f => f.name === "weightCount");
el_pf_unitAmount = Array.from(document.getElementById("PreFacItem")?.elements ?? []).find(f => f.name === "unitAmount");
el_pf_weightCount?.addEventListener("input", () => calculateItem("PreFacItem"));
el_pf_unitAmount?.addEventListener("input", () => calculateItem("PreFacItem"));


el_f_weightCount = Array.from(document.getElementById("FacItem")?.elements ?? []).find(f => f.name === "weightCount");
el_f_unitAmount = Array.from(document.getElementById("FacItem")?.elements ?? []).find(f => f.name === "unitAmount");
el_f_weightCount?.addEventListener("input", () => calculateItem("FacItem"));
el_f_unitAmount?.addEventListener("input", () => calculateItem("FacItem"));


document.querySelectorAll("#InvoiceModalId input[type='radio'][name='PayType']").forEach(radio => {
    radio.addEventListener("change", (e) => {
        const type = Number(e.target?.value);
        if (type === 3) {
            document.getElementById("SanadParId").classList.remove("AlertReqRadio");
        }
        PayFormChange("InvoiceModalId", type);
    });
});
document.querySelectorAll("#AttachInvoiceModalId input[type='radio'][name='payTypeAttach']").forEach(radio => {
    radio.addEventListener("change", (e) => {
        const type = Number(e.target.value);
        PayAttachFormChange(type);
    });
});

document.querySelectorAll("#PreFactor input[type='radio'][name='official']")?.forEach(radio => {
    const currencyTypeElm = document.querySelector("#PfHeadFormId select[name='currencyType']");
    radio.addEventListener("change", (e) => {
        _officialType = Number(e.target.value);
        currencyTypeElm.toggleAttribute("disabled", _officialType === 1);
        currencyTypeElm.value = 0;
        calculateTotal();
        descGenerate();
        calculateText();
    });
});

/*document.querySelector("#PfHeadFormId select[name='currencyType']").addEventListener("change", (e) => {

});*/

document.querySelectorAll("#Factor input[type='radio'][name='f_official']")?.forEach(radio => {
    const currencyTypeElm = document.querySelector("#FHeadFormId select[name='currencyType']");
    radio.addEventListener("change", async (e) => {
        f_officialType = Number(e.target.value);
        currencyTypeElm.toggleAttribute("disabled", f_officialType === 1);
        await calculateTotalFactor();
    });
});


const descGenerate = () => {
    if (_officialType === 0) { //اگر غیر رسمی بود
        PreFactorDescItems = PreFactorDescItems.filter(item => {
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
        /*PreFactorDescItems.forEach((item, index) => {
            if (item.official === true) {
                PreFactorDescItems.splice(index, 1);
            }
        });
        baseDesPfactor.filter(f => f.type === 0).forEach((item, index) => {
            if (item.official === true) {
                PreFactorDescItems.push({ text: item.text, official: item.official, isDeleted: false });
            }
        });*/
        PreFactorDescItems = [
            ...PreFactorDescItems.filter(item => !item.official),
            ...baseDesPfactor.filter(item => item.type === 0 && item.official),
        ];
    }
    showPreFactorDescItems();
    if (_officialType === 1) {
        setTimeout(() => {
            const listElement = document.getElementById(descriptionListId);
            listElement.scrollTop = listElement.scrollHeight;
        }, 100);
    }
};


const Unit = (type, formId, defaultValueId) => {
    
    let unitItems = [
        { id: 1, text: "گرم" },
        { id: 2, text: "کیلوگرم" },
        { id: 3, text: "عدد" },
        { id: 4, text: "متر مربع" },
        { id: 5, text: "متر طول" },
        { id: 6, text: "شاخه" },
        { id: 7, text: "رول" },
    ];
    
    if (type === 1) {
        const unitElm = document.querySelector("#PreFacItem select[name='unit']");
        $(unitElm).select2({
            data: unitItems,
            dir: "rtl",
            placeholder: "انتخاب",
            minimumResultsForSearch: Infinity,
            selectionCssClass: "form-control",
            dropdownParent: window.innerWidth < 992 ? $("#ItemPreFactorModal") : $("#PreFactor"),
        });
        $(unitElm).val(null).trigger("change");
    } else if (type === 2) {
        const fun = Array.from(document.getElementById("FacItem").elements).filter(f => f.name == "unit")[0];
        $(fun).select2({
            data: unitItems,
            dir: "rtl",
            placeholder: "انتخاب",
            selectionCssClass: "form-control",
            dropdownParent: window.innerWidth < 992 ? $("#ItemFactorModal") : $("#Factor"),
        });
        $(fun).val(null).trigger("change");
    } else if (type === 3) {
        const unitInput = document.querySelector(`#${formId} select[name='unit']`);
        unitInput.innerHTML = "";
        
        const fragment = document.createDocumentFragment();
        if (!defaultValueId) {
            const opt = document.createElement("option");
            opt.innerText = "انتخاب";
            opt.value = "";
            fragment.appendChild(opt);
        }
        unitItems.forEach(unit => {
            const opt = document.createElement("option");
            opt.innerText = unit.text;
            opt.value = unit.id;
            if (defaultValueId && unit.id === defaultValueId) {
                opt.setAttribute("selected", true);
            }
            fragment.appendChild(opt);
        });
        unitInput.appendChild(fragment);
    }
};


const SubmitdescExpirePfactor = async () => {
    
    if (await mySer.CheckValidForm("#descExpirePfactor")) {
        
        var data = mySer.convertFormToJSON("#descExpirePfactor");
        
        
        if (data.M_ex_Item == 0) {
            await changeTypePfactor(pfAbort, 8, FormCode, data.des);
        } else {
            var ss = document.getElementById("M_ex_Item");
            await changeTypePfactor(pfAbort, 8, FormCode, ss.options[ss.selectedIndex].text);
        }
        
        $("#ModalExpireDescription").modal("toggle");
        
    }
};

const M_ex_cheItem = () => {
    if (document.getElementById("M_ex_Item").value == 0) {
        document.getElementById("descExpirePfactorID").style.display = "";
        document.getElementById("descExpirePfactorIDTXT").setAttribute("required", "");
    } else {
        document.getElementById("descExpirePfactorID").style.display = "none";
        document.getElementById("descExpirePfactorIDTXT").removeAttribute("required");
    }
};


const ShowModalExpireDescription = (PfId, amount, serial, name, _formCode) => {
    
    FormCode = _formCode;
    pfAbort = PfId;
    document.getElementById("M_ex_Serial").innerHTML = serial;
    document.getElementById("M_ex_Name").innerHTML = name;
    document.getElementById("M_ex_TAmount").innerHTML = amount;
    $("#ModalExpireDescription").modal("toggle");
    
    
};


let notAcceptSelAdminPfId;
const cancelModalDes = (pfId) => {
    notAcceptSelAdminPfId = null;
    document.getElementById("NotAcceptSelAdminDescTextId").value = "";
    $("#NotAcceptDescModal").modal("toggle");
};
const ShowModalDes = (pfId) => {
    notAcceptSelAdminPfId = pfId;
    $("#NotAcceptDescModal").modal("toggle");
};
const submitNotAcceptSelAdmin = () => {
    $("#FLshowLoading").css("display", "");
    
    var ss = document.getElementById("NotAcceptSelAdminDescTextId").value;
    var model = {
        desc: ss, pfid: notAcceptSelAdminPfId,
    };
    
    $.post(baseUrl + "factor/notAcceptPfactor", model, async (res) => {
        
        $("#FLshowLoading").css("display", "none");
        
        if (res.ok) {
            $("#NotAcceptDescModal").modal("toggle");
            toastifySuccess("پیش فاکتور عدم تایید خورد");
            document.getElementById("NotAcceptSelAdminDescTextId").value = "";
            await GetPreFactorSalesAdmin();
        } else {
            toastifyError("خطای سرور");
        }
        
        
    }).fail(() => {
        setTimeout(() => submitNotAcceptSelAdmin(), 1000);
    });
    
};


const changeTypePfactor = async (_PfId, _type, FormCode, des = null) => {
    
    await changeTypePfactorOk(_PfId, _type, des);
    switch (FormCode) {
        case 1: // کارتابل کارشناس مالی
            toastifySuccess("تکمیل وجه در سیستم ثبت شد.");
            GetPreFactorPartial($("#searchId").val());
            break;
        case 2: //customerProfile
            await getAllPreFactor();
            break;
        case 3:
            toastifySuccess("پیش فاکتور با موفقیت بسته شد");
            GetPreFactorSalesAdmin();
            break;
        case 4:
            toastifySuccess("پیش فاکتور با موفقیت بسته شد");
            GetSelesExpertPartial();
            break;
        case 5: //سفارشات
            toastifySuccess("تغییر انجام شد");
            getPreFactorFromDeal(currentDealData.id, true);
            break;
        case 6: //کارتابل مالی
            toastifySuccess("تغییر انجام شد");
            await GetPreFactorFinancialExpert();
            break;
    }
    
};


const changeTypePfactorOk = async (_PfId, type, des) => {
    
    const model = { id: _PfId, type, des };
    
    FLshowLoading();
    return new Promise(resolve => {
        $.post(baseUrl + "factor/changeTypePfactor", model, () => {
            FLshowLoading(false);
            resolve();
        });
        
    });
};


let loadAllProduct = false;
const getAllProductOp = () => {
    if (loadAllProduct) return;
    
    const productCodeElm = document.querySelector("#PreFacItem select[name='productCode']");
    axios(`/Product/GetAllProduct`).then((res) => {
        new TomSelect(productCodeElm, {
            options: res.data,
            valueField: "id",
            placeholder: "انتخاب",
            maxItems: 1,
            selectOnTab: true,
            // dropdownParent
        });
        
        loadAllProduct = true;
    });
    
    /* $.get(baseUrl + "Product/GetAllProduct", (res) => {
         $(el).select2({
             data: res,
             dir: "rtl",
             placeholder: "انتخاب",
             selectionCssClass: "form-control",
             dropdownParent: window.innerWidth < 992 ? $("#ItemPreFactorModal") : $("#PreFactor"),
         });
         $(el).val(null).trigger("change");
         
         $("#FLshowLoading").css("display", "none");
         
     }).fail(() => {
         setTimeout(() => {
             getAllProductOp();
         }, 3000);
     });*/
};

const upfactor = (fid, cid, type = 1) => {
    typeSubmitfactor = type;
    
    
    customerId = cid;
    
    document.getElementById("FLshowLoading").style.display = "";
    Unit(2);
    f_getAllProductOp();
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
        
        
        ShowItemfactor();
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
    typeSubmitfactor = type;
    customerId = cid;
    const modalFactor = document.getElementById("Factor");
    modalFactor.classList.add("returned-factor");
    
    
    FLshowLoading();
    Unit(2);
    f_getAllProductOp();
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
    
    
    ShowItemfactor();
    showFactorDescItems();
    modalFactor.querySelector("#f_sumTotalAmount").textContent = factor.sumTotalAmount.toLocaleString();
    modalFactor.querySelector("#f_totalTax").textContent = factor.totalTax.toLocaleString();
    modalFactor.querySelector("#f_paymentAmount").textContent = factor.paymentAmount.toLocaleString();
    modalFactor.querySelector("#f_discount").value = factor.discount.toLocaleString();
    
    ModalToggle(modalFactor, "show");
    FLshowLoading(false);
};


const upPfactor = async (id, type) => {
    FLshowLoading();
    typeSubmitPfactor = type;
    
    getAllProductOp();
    Unit(1, "PreFacItem");
    baseDesPfactor = (await getbaseDescription()).filter(f => f.type === 0);
    
    $.get(baseUrl + "Factor/GetPFactor/" + id, (res) => {
        toggleDisplayPrefactorModal(res.paymentAmount === 0);
        
        pfactor = res;
        _pfactorItems = res.preFactorItems;
        _officialType = res.officialType;
        PreFactorDescItems = res.descriptions;
        dealData.id = res.dealId;
        dealData.serialNumber = res.dealSerialNumber;
        customerId = res.customerId;
        
        document.querySelectorAll("#PreFactor input[name='official']").forEach((i) => {
            i.checked = Number(i.value) === _officialType;
        });
        
        const exportValue = res.export ? "1" : "0";
        document.querySelectorAll("#PreFactor input[name='export']").forEach((i) => {
            i.checked = (i.value === exportValue);
        });
        
        const currencyTypeElm = document.querySelector("#PfHeadFormId select[name='currencyType']");
        currencyTypeElm.value = res.currencyType ?? 0;
        currencyTypeElm.toggleAttribute("disabled", _officialType === 1);
        
        $("#serialNumber").val(pfactor.serialNumber);
        $("#date").val(pfactor.date);
        $("#validityDade").val(pfactor.validityDade);
        
        $("#sumTotalAmount").text(pfactor.sumTotalAmount.toLocaleString());
        $("#discount").val(pfactor.discount.toLocaleString());
        $("#totalTax").text(pfactor.totalTax.toLocaleString());
        $("#paymentAmount").text(pfactor.paymentAmount.toLocaleString());
        
        ModalToggle("#PreFactor");
        ShowItemPfactor();
        showPreFactorDescItems();
        FLshowLoading(false);
    })
     .fail(() => {
         setTimeout(async () => await upPfactor(id, type), 3000);
     });
};

const ShowItemfactor = () => {
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
                       <button class="flex-center"><i class="mp fa fa-trash" onclick="delfactorItem(${index})"></i></button>
                       <button class="flex-center"><i class="mp fa fa-pencil" onclick="upfactorItem(${index})"></i></button>
                    </div>
                </div>
            </div>
        `;
    });
    
    ob += `</div>`;
    
    $("#FactorItemId").children().remove();
    $("#FactorItemId").append(ob);
};


const ShowItemPfactor = () => {
    //const model = _pfactorItems.filter(f => !f.isDeleted);
    document.getElementById("preFactorItemId").innerHTML = generateItemPfactorDom(_pfactorItems);
};

const generateItemPfactorDom = (data, type = "edit-delete") => {
    let ob = `<div style="display:flex; flex-direction:column;width:100%;gap:5px">`;
    data.forEach((item, index) => {
        if (item.isDeleted) return;
        if (item.product == null) item.product = "";
        
        item.row = index + 1;
        const uAmount = item.unitAmount.toLocaleString();
        const tAmount = item.totalAmount.toLocaleString();
        item.printableTxt = Number(item.printable) === 1 ? "چـاپـدار" : "";
        
        let actionButtons = "";
        
        if (type === "edit-delete") {
            actionButtons = `
                <button class="flex-center" style="color:#3f0e6e">
                    <i class="mp fa fa-trash" onclick="delPfactorItem(${index})"></i>
                </button>
                <button class="flex-center" style="color:#3f0e6e">
                    <i class="mp fa fa-pencil" onclick="upPfactorItem(${index})"></i>
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
            <div style="width:100%;display:flex">
                <div class="InfoDataItemHead" style="display:flex;width:24px;flex-shrink: 0">
                    <label style="font-family:'IRANSansBold'">${index + 1}</label>
                </div>
                <div class="InfoDataItem py-1" style="display:flex;justify-content:space-between; width:100%; direction:rtl;">
                    <div class="px-2" style="display:flex;flex-direction:column;align-items:center;justify-content:center;width: 22px;flex-shrink: 0;">
                        <label style="writing-mode: sideways-lr;font-family:'IRANSansBold'">${item.printableTxt}</label>
                    </div>
                    <div style="display:flex;justify-content:space-between; width:100%;flex-wrap:wrap;">
                        <div class="col-12 col-lg-5" style="display:flex;flex-direction:column;align-items:center;justify-content:space-around">
                            <label>شرح کالا</label>
                            <label class="px-2 d-flex gap-1 fbold">
                                <span>${item.productSmallName}</span>
                                <span>${item.product}</span>
                            </label>
                        </div>
                        <div class="d-lg-none" style="width:90%;margin-block: 4px;margin-inline: auto; height:1px; background-color:rgba(0,0,0,0.15);"></div>
                        <div class="col-6 col-lg-1" style="white-space: nowrap;display:flex;flex-direction:column;align-items:center;justify-content:space-around">
                            <label style="display:flex">تعداد/مقدار</label>
                            <label style="font-family:'IRANSansBold'">${item.weightCount}</label>
                        </div>
                        <div class="col-6 col-lg-1" style="white-space: nowrap; display:flex;flex-direction:column;align-items:center;justify-content:space-around">
                            <label>واحد</label>
                            <label style="font-family:'IRANSansBold'">${item.unitTxt}</label>
                        </div>
                        <div class="d-lg-none" style="width:90%;margin-block: 4px;margin-inline: auto; height:1px; background-color:rgba(0,0,0,0.15);"></div>
                        <div class="col-6 col-lg-2" style="white-space: nowrap;display:flex;flex-direction:column;align-items:center;justify-content:space-around">
                            <label>مبلغ واحد</label>
                            <label style="font-family:'IRANSansBold'">${uAmount === "0" ? "-" : uAmount} ${item.currencyTypeTxt ?? ""}</label>
                        </div>
                        <div class="col-6 col-lg-2" style="white-space: nowrap;display:flex;flex-direction:column;align-items:center;justify-content:space-around">
                            <label>مبلغ کل</label>
                            <label style="font-family:'IRANSansBold'">${tAmount === "0" ? "-" : tAmount} ${item.currencyTypeTxt ?? ""}</label>
                        </div>
                    </div>

                    <div class="InfoDataItemOp col-auto" style="padding-inline: 6px;flex-shrink: 0">
                        ${actionButtons}
                    </div>

                </div>
            </div>
        `;
    });
    
    ob += `</div>`;
    return ob;
};


const AddPfItem = async () => {
    document.getElementById("preFactorItemId").classList.remove("AlertReq");
    
    if (await mySer.CheckValidForm("#PreFacItem")) {
        const productCodeElm = document.querySelector("#PreFacItem select[name='productCode']");
        const unitElm = document.querySelector("#PreFacItem select[name='unit']");
        
        const data = mySer.convertFormToJSON("#PreFacItem");
        
        data.productSmallName = productCodeElm.options[productCodeElm.selectedIndex].text;
        data.unitTxt = unitElm.options[unitElm.selectedIndex].text;
        data.totalAmount = SeparatedNumberRemove(data.totalAmount);
        data.unitAmount = SeparatedNumberRemove(data.unitAmount);
        data.id = null;
        _pfactorItems.push(data);
        pfactor.PreFactorItems = _pfactorItems;
        
        ShowItemPfactor();
        mySer.emptyForm("PreFacItem");
        
        calculateTotal();
        calculateText();
        
        document.getElementById("printableF")?.focus();
        return true;
        
    } else {
        return false;
    }
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

let descriptionInpId;
let descriptionListId;
let prePaymentPercentId;
if (window.innerWidth >= 992 && window.innerWidth < 1200) {
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
prePaymentPercentElm?.addEventListener("change", () => calculateText());

const calculateText = () => {
    const model = _pfactorItems.filter(f => !f.isDeleted);
    const currencyTypeElm = document.querySelector("#PfHeadFormId select[name='currencyType']");
    const currencyTxt = currencyTypeElm.options[currencyTypeElm.selectedIndex].text.trim();
    const prePaymentPercent = Number(prePaymentPercentElm.value);
    
    /*let sumTotal = document.getElementById("sumTotalAmount").innerText;
    sumTotal = Number(SeparatedNumberRemove(sumTotal));*/
    
    let sumTotal = pfactor.paymentAmount;
    //let totalTax = pfactor.totalTax;
    //let sumTotalTax = pfactor.paymentAmount;
    
    let valueText;
    const kelisheItems = model.filter(item => item.productSmallName.includes("کلیشه"));
    const cilandrItems = model.filter(item => item.productSmallName.includes("سیلندر"));
    
    let kelisheTotalAmount, cilandrTotalAmount;
    
    if (_officialType) {
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


const showPreFactorDescItems = () => {
    let items = "";
    PreFactorDescItems.forEach((item, index) => {
        if (!item.isDeleted) {
            items += `
                <li class="d-flex gap-1">
                    <p>${item.text}</p>
                    <span class="cursor-pointer" onclick="DelPreFactorDescItem(${index})">
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

const addPreFactorDescItem = () => {
    const inputElement = document.getElementById(descriptionInpId);
    const val = inputElement.value;
    inputElement.value = "";
    
    if (val.length) {
        PreFactorDescItems.push({ text: val, isDeleted: false, dir: null });
        showPreFactorDescItems();
        
        setTimeout(() => {
            const listElement = document.getElementById(descriptionListId);
            listElement.scrollTop = listElement.scrollHeight;
        }, 100);
    }
};


const DelPreFactorDescItem = (index) => {
    if (!PreFactorDescItems[index].id) {
        PreFactorDescItems.splice(index, 1);
    } else {
        PreFactorDescItems[index].isDeleted = true;
    }
    showPreFactorDescItems();
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
                    await ShowModalListInvoice(pfId, 1, "", 5, null, true);
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

let supplyData = [];
const AddFactor = async (pfId = null, type = 1) => {
    
    if (!await isAllowedToAddFactor(pfId)) return;
    typeSubmitfactor = type;
    
    FLshowLoading();
    await f_getAllProductOp();
    Unit(2);
    
    baseDesFactor = await getbaseDescription();
    const pfData = (await axios(`/Factor/GetPFactor/${pfId}`)).data;
    supplyData = (await axios(`/Supply/GetSupplies?pfId=${pfId}`)).data;
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
        baseDesFactor.filter(f => f.official === false && f.type === 1).map((item) => {
            item = { ...item, isDeleted: false };
            factor.descriptions.push(item);
            FactorDescItems.push(item);
        });
    } else {
        baseDesFactor.filter(f => f.official === true && f.type === 1).map((item) => {
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
    
    ShowItemfactor();
    f_showDescItem();
    FLshowLoading(false);
    
    
};

const showModalAddPfactor = async (type, cusId = null, deal) => {
    typeSubmitPfactor = type;
    dealData = deal;
    customerId = cusId;
    FLshowLoading();
    baseDesPfactor = await getbaseDescription();
    PreFactorDescItems = [];
    
    baseDesPfactor.filter(f => !f.official).filter(f => f.type === 0).map(item => {
        PreFactorDescItems.push({ text: item.text, isDeleted: false, official: item.official });
    });
    showPreFactorDescItems();
    toggleDisplayPrefactorModal();
    //_officialType = 1;//رسمی
    $("#official").checked = true;
    $("#PreFactor").modal("show");
    $("#date").val(await todayDate());
    //$("#validityDade").val(await tomorrowDate());
    $("#validityDade").val(await todayDate());
    getAllProductOp();
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

const submitPrefactor = async () => {
    await submitPrefactorOK(Number(typeSubmitPfactor));
};

const submitPrefactorOK = async (type) => {
    if (await mySer.CheckValidForm("#PfHeadFormId") & await mySer.CheckValidForm("#validityDadeFormId") & await mySer.CheckValidForm("#dateFormId")) {
        
        if (_pfactorItems.filter(f => f.isDeleted !== true).length > 0) {
            
            FLshowLoading();
            pfactor.serialNumber = $("#serialNumber").val();
            pfactor.date = toEnglishDigits($("#date").val());
            pfactor.validityDade = toEnglishDigits($("#validityDade").val());
            pfactor.officialType = _officialType;
            pfactor.customerId = customerId;
            pfactor.descriptions = PreFactorDescItems;
            pfactor.export = document.getElementById("export").checked;
            pfactor.dealId = dealData.id;
            pfactor.dealSerialNumber = dealData.serialNumber;
            
            const currencyTypeElm = document.querySelector("#PfHeadFormId select[name='currencyType']");
            pfactor.currencyType = currencyTypeElm.value;
            pfactor.currencyTypeTxt = currencyTypeElm.options[currencyTypeElm.selectedIndex].text.trim();
            
            $.post(baseUrl + "Factor/AddPrefactor", pfactor, async (res) => {
                if (type === 1) { // CustomerProfile
                    await getAllPreFactor();
                } else if (type === 2) { // SalesAdmin
                    await GetPreFactorSalesAdmin();
                } else if (type === 3) { // PreFactor/Factor
                    await GetPreFactorPartial(modelPreFactor.serVal, modelPreFactor._type, modelPreFactor.filt);
                } else if (type === 4) { //Deal
                    await getPreFactorFromDeal(dealData.id);
                    await getAllDeals();
                    if (!document.getElementById("dealPreFactorsModal").classList.contains("show")) {
                        await showModalDealPreFactors(dealData);
                    }
                }
                
                toastifySuccess("پیش فاکتور با موفقیت ثبت شد.");
                cancelPreFactor();
                $("#PreFactor").modal("toggle");
                FLshowLoading(false);
            })
             .fail(() => {
                 setTimeout(async () => {
                     await submitPrefactorOK(type);
                 }, 2000);
             });
        } else {
            document.getElementById("preFactorItemId").classList.add("AlertReq");
            FLshowLoading(false);
            toastifyError("پیش فاکتور خالی است");
        }
    }
    
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
    await submitFactorOK(Number(typeSubmitfactor));
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
                    await GetFactorPartial(modelFactor.serVal, modelFactor.EntityId, null, modelFactor.filt, modelFactor._type, factor.pfactorSerialNumber, type);
                    await GetPreFactorPartial(modelPreFactor.serVal, modelPreFactor._type, modelPreFactor.filt);
                } else if (type === 1) {
                    await getAllPreFactor();
                    
                    await GetFactorPartial(null, factor.pfactorID, null, null, factor.officialType, factor.pfactorSerialNumber, type);
                    getFacAmount(factor.pfactorID, factor.pfactorSerialNumber, null);
                    getAllDetailAmount();
                    //getAllFactor();
                } else if (type === 4) {
                    if (!factor.id) {
                        await getPreFactorFromDeal(dealData.id);
                    } else {
                        await getAllFactorsModal(dealData.id, 4);
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
    
    document.getElementById("descriptionId").value = "";
    document.getElementById("descriptionIdTablet").value = "";
    document.getElementById("descriptionIdMobile").value = "";
    
    prePaymentPercentElm.value = Array.from(prePaymentPercentElm.children).find(i => i.defaultSelected).value;
    
    pfactor = {};
    _pfactorItems = [];
    _officialType = null;
    UpPfItemCancel();
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
    _officialType = null;
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
const delPfactor = (id, type, dealId) => {
    Swal.fire({
        title: "حذف!",
        text: "آیا مطمئن هستید که میخواهید حذف کنید؟",
        icon: "warning",
        showCancelButton: true,
        cancelButtonColor: "#adadad",
        confirmButtonColor: "#e64942",
        cancelButtonText: "انصراف",
        confirmButtonText: "بله، حذف شود",
    }).then((willDelete) => {
        if (willDelete.isConfirmed)
            delPfactorOk(id, type, dealId);
    });
};
const delPfactorOk = (id, type, dealId) => {
    if (!id) return;
    FLshowLoading();
    
    axios(`/Factor/DelPreFactor/${id}`).then(async () => {
        if (type === 1) { // CustomerProfile
            await getAllPreFactor();
            getAllDetailAmount();
        } else if (type === 2) { // SalesAdmin
            await GetPreFactorSalesAdmin();
        } else if (type === 3) { // PreFactor/Factor
            await GetPreFactorPartial(modelPreFactor.serVal, modelPreFactor._type, modelPreFactor.filt);
        } else if (type === 4) { //deal
            await getPreFactorFromDeal(dealId);
        }
        
        FLshowLoading(false);
        toastifySuccess("پیش‌فاکتور حذف شد");
    }).catch((err) => {
        console.log(err);
        setTimeout(() => delPfactorOk(id), 3000);
    });
    
};


const delFactor = (id, type, dealId) => {
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
            
            if (willDelete.isConfirmed) delFactorOk(id, type, dealId);
        });
};
const delFactorOk = (id, type, dealId) => {
    if (!id) return;
    
    FLshowLoading();
    $.get(baseUrl + "Factor/DelFactor/" + id, async () => {
        if (type === 0) {
            await GetFactorPartial(modelFactor.serVal, modelFactor.EntityId, null, modelFactor.filt, modelFactor._type, pFactorSerialNumber, type);
            //await GetPreFactorPartial(modelPreFactor.serVal, modelPreFactor._type, modelPreFactor.filt);
            
        } else if (type === 1) {
            await getAllPreFactor();
            await GetFactorPartial(modelFactor.serVal, modelFactor.EntityId, null, modelFactor.filt, modelFactor._type, pFactorSerialNumber, type);
            
            getAllDetailAmount();
        } else if (type === 4) {
            await getAllFactorsModal(dealId, 4);
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
const getFacSepidar = async (pfId, serialNumber = null, _this, PassType, dealId) => {
    if (PassType === 4) {
        const dealRes = await axios(`/Deal/GetDeal/${dealId}`);
        await showModalDealFactors(dealRes.data, serialNumber);
        return;
    }
    FLshowLoading();
    document.querySelector(".SelectedPf")?.classList.remove("SelectedPf");
    _this.closest(".InfoDataItem_Prefactor").classList.add("SelectedPf");
    
    // PfSelForFactor = pfId;
    const res = await getAllFactorSepidar(pfId, serialNumber);
    
    if (PassType === 1) {
        document.getElementById("DetailDivList").innerHTML = res;
        document.getElementById("DivFacListId").style.display = "flex";
        document.getElementById("FactorDivTitleId").innerHTML = pfId
            ? `<span class="mp buttonGreen hide-factor__btn" style="border-radius:5px; font-size:20px; height:20px;width:20px !important; cursor:pointer" onclick="hideFact()"><i class="fas fa-times" style="font-size: 14px;"></i></span><span>تمام فاکتورهای مربوط به پیش فاکتور  ${serialNumber} </span>`
            : "تمام فاکتور ها";
        
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
const showSepidarFactorsModal = async (pfSerialNumber, fId, type, dealId) => {
    dealData.id = dealId;
    typeSubmitfactor = type;
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
            if (typeSubmitfactor === 0) {
                await GetFactorPartial(modelFactor.serVal, modelFactor.EntityId, null, modelFactor.filt, modelFactor._type, pFactorSerialNumber, typeSubmitfactor);
            } else if (typeSubmitfactor === 1) {
                getAllDetailAmount();
                await GetFactorPartial(modelFactor.serVal, modelFactor.EntityId, null, modelFactor.filt, modelFactor._type, pFactorSerialNumber, typeSubmitfactor);
            } else if (typeSubmitfactor === 4) {
                await getAllFactorsModal(dealData.id, 4);
            } else if (typeSubmitfactor === 6) {
                await getAllFactorsModal(model.crmFactorId, 6);
            }
            FLshowLoading(false);
            toastifySuccess("با موفقیت ثبت شد");
            $("#sepidarFactorsModal").modal("hide");
        }
    } catch (err) {
        setTimeout(async () => await updateFactorFinancial(sepidarId), 3000);
        console.log(err);
    }
};


const GetFactorPartial = async (val = null, id = null, _this = null, filt = null, officialType = 0, serialNumber, PassType = 0, dealId, showLoading = true) => {
    if (PassType === 4) {
        const dealRes = await axios(`/Deal/GetDeal/${dealId}`);
        await showModalDealFactors(dealRes.data);
        return;
    }
    
    if (PassType === 0 && _this) document.getElementById("f_all").checked = true;
    modelFactor.serVal = val;
    modelFactor._type = officialType;
    modelFactor.filt = filt;
    modelFactor.EntityId = id;
    
    
    CartableFSearchVal = val;
    PfSelForFactorThis = _this;
    pFactorSerialNumber = serialNumber;
    
    document.querySelector(".SelectedPf")?.classList.remove("SelectedPf");
    
    if (_this != null) {
        _this.closest(".InfoDataItem_Prefactor").classList.add("SelectedPf");
        if (PassType === 0) {
            document.getElementById("HideFactorPartialId").style.display = "flex";
            document.getElementById("f_searchId").value = "";
        }
        
        if (window.innerWidth < 1400) {
            const allFactorTab = new bootstrap.Tab(document.getElementById(PassType === 0 ? "allFactorTab" : "factorTab"));
            allFactorTab.show();
        }
    } else if (PassType === 0) {
        document.getElementById("HideFactorPartialId").style.display = "none";
    }
    
    if (val === "") {
        document.getElementById("f_searchId").value = "";
        $("#f_searchId").val("");
        document.getElementById("HideFactorPartialId").style.display = "none";
    }
    
    
    showLoading && FLshowLoading();
    PfSelForFactor = id;
    const _model = { fmodel: modelFactor, PassType };
    try {
        const res = await axios.post(baseUrl + `Cartable/FactorPartial`, _model);
        if (res.status === 200) {
            if (PassType === 0) {
                document.getElementById("fAcceptedList").innerHTML = res.data.html;
                document.getElementById("fCountId").innerHTML = res.data.count;
                document.getElementById("fCountMobileId").innerText = res.data.count;
            } else {
                
                if (id != null) {
                    document.getElementById("FactorDivTitleId").innerHTML = `<span class="mp buttonGreen hide-factor__btn" style="border-radius:5px; font-size:20px; height:20px;width:20px !important; cursor:pointer" onclick="hideFact()"> <i class="fas fa-times" style="font-size: 14px;"></i></span> <span>تمام فاکتورهای مربوط به پیش فاکتور ${serialNumber}</span> `;
                } else {
                    document.getElementById("FactorDivTitleId").innerHTML = "تمام فاکتور ها";
                }
                
                document.getElementById("DivFacListId").style.display = "flex";
                document.getElementById("DetailDivList").innerHTML = res.data.html;
            }
        }
        
        
        showLoading && FLshowLoading(false);
    } catch (e) {
        console.log(e);
    }
    
};


const getAllFactorsModal = async (id, type, sNumber) => {
    FLshowLoading();
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


const acceptFactor = async (id, type, passType, PfactorSerialNumber, dealId) => {
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
                await GetFactorPartial(modelFactor.serVal, modelFactor.EntityId, null, modelFactor.filt, modelFactor._type, PfactorSerialNumber, passType);
            } else if (passType === 1) {
                getAllDetailAmount();
                await GetFactorPartial(modelFactor.serVal, modelFactor.EntityId, null, modelFactor.filt, modelFactor._type, PfactorSerialNumber, passType);
            } else if (passType === 4) {
                await getAllFactorsModal(dealId, 4);
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
        setTimeout(async () => await acceptFactor(id, type, passType, PfactorSerialNumber, dealId), 3000);
    }
};


let pfIdChangeUser;
async function showModalChangeUserPf(pfId, cusId) {
    pfIdChangeUser = pfId;
    $("#changeUserPfModal").modal("toggle");
    document.getElementById("FLshowLoading").style.display = "flex";
    try {
        const res = await fetch(`/Customer/GetUsersForCustomer?cusId=${cusId}&pfId=${pfId}`);
        const data = await res.json();
        
        
        const wrapper = document.getElementById("selectOptUserPf");
        wrapper.innerHTML = "";
        if (data.length) {
            const fragment = document.createDocumentFragment();
            data.forEach(item => {
                const opt = document.createElement("option");
                opt.innerText = item.text;
                opt.value = item.id;
                opt.selected = item.target;
                opt.style.backgroundColor = item.target ? "#1967d24a" : "#fff";
                fragment.appendChild(opt);
            });
            wrapper.appendChild(fragment);
        }
        
        document.getElementById("FLshowLoading").style.display = "none";
    } catch (err) {
        console.log(err);
    }
}
async function submitChangeUserPf() {
    const userId = document.getElementById("selectOptUserPf").value;
    const userFullName = document.getElementById("selectOptUserPf").selectedOptions[0].innerText;
    
    try {
        document.getElementById("FLshowLoading").style.display = "";
        const res = await fetch(`/Customer/ChangeUserPf?userId=${userId}&pfId=${pfIdChangeUser}&userFullName=${userFullName}`);
        if (res.ok) {
            $("#changeUserPfModal").modal("toggle");
            toastifySuccess("با موفقیت ویرایش شد");
            
            document.getElementById("FLshowLoading").style.display = "none";
        }
    } catch (err) {
        console.log(err);
        setTimeout(async () => await submitChangeUserPf(), 2000);
    }
}

const upfactorItem = (index) => {
    if (window.innerWidth < 992) {
        showModalItemFactor();
    }
    fItemUpIndex = index;
    mySer.convertJsonToForm(_factorItems[index], "FacItem");
    document.getElementById("f_AddBtnId").style.display = "none";
    document.getElementById("f_CancelBtnId").style.display = "none";
    document.getElementById("f_UpBtnId").style.display = "flex";
};


const upPfactorItem = (index) => {
    if (window.innerWidth < 992) {
        showModalItemPreFactor();
    }
    PfItemUpIndex = index;
    console.log(index, _pfactorItems);
    mySer.convertJsonToForm(_pfactorItems[index], "PreFacItem");
    document.getElementById("AddBtnId").style.display = "none";
    document.getElementById("CancelBtnId").style.display = "none";
    document.getElementById("UpBtnId").style.display = "flex";
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


const UpPfItemCancel = () => {
    mySer.emptyForm("PreFacItem");
    PfItemUpIndex = null;
    
    document.getElementById("AddBtnId").style.display = "flex";
    document.getElementById("CancelBtnId").style.display = "flex";
    document.getElementById("UpBtnId").style.display = "none";
    
    if (window.innerWidth < 992) {
        canselModalItemPreFactor();
    }
};


const UpfItem = async () => {
    
    let el_procode = Array.from(document.getElementById("FacItem").elements).filter(f => f.name == "productCode")[0];
    let data = mySer.convertFormToJSON("#FacItem");
    
    
    if (el_procode.value != "") {
        data.productSmallName = el_procode.options[el_procode.selectedIndex].text;
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
    
    data.officialType = _officialType;
    _factorItems[fItemUpIndex] = data;
    
    data.unit = Number(data.unit);
    
    
    $("#f_AddBtnId").css("display", "flex");
    $("#f_UpBtnId").css("display", "none");
    ShowItemfactor();
    fItemUpIndex = null;
    mySer.emptyForm("FacItem");
    let calcStatus = await calculateTotalFactor();
    //calculateTextFactor();
    
    
    toastifySuccess("با موفقیت ویرایش شد!");
    
    if (window.innerWidth < 992) {
        canselModalItemFactor();
    }
};

const UpPfItem = () => {
    
    //ایندکس فیلتر شده 
    
    
    var el_procode = Array.from(document.getElementById("PreFacItem").elements).filter(f => f.name == "productCode")[0];
    
    var data = mySer.convertFormToJSON("#PreFacItem");
    
    if (el_procode.value != "") {
        data.productSmallName = el_procode.options[el_procode.selectedIndex].text;
    }
    
    
    data.id = _pfactorItems[PfItemUpIndex].id;
    data.prefactorId = _pfactorItems[PfItemUpIndex].prefactorId;
    
    var el_Unit = Array.from(document.getElementById("PreFacItem").elements).filter(f => f.name == "unit")[0];
    data.unitTxt = el_Unit.options[el_Unit.selectedIndex].text;
    
    
    data.officialType = _officialType;
    data.isDeleted = false;
    
    data.totalAmount = Number(SeparatedNumberRemove(data.totalAmount));
    data.unit = Number(SeparatedNumberRemove(data.unit));
    data.unitAmount = Number(SeparatedNumberRemove(data.unitAmount));
    
    
    _pfactorItems[PfItemUpIndex].printable = data.printable;
    _pfactorItems[PfItemUpIndex].printableTxt = data.printableTxt;
    _pfactorItems[PfItemUpIndex].product = data.product;
    _pfactorItems[PfItemUpIndex].productCode = data.productCode;
    _pfactorItems[PfItemUpIndex].productSmallName = data.productSmallName;
    _pfactorItems[PfItemUpIndex].weightCount = data.weightCount;
    _pfactorItems[PfItemUpIndex].unit = data.unit;
    _pfactorItems[PfItemUpIndex].unitTxt = data.unitTxt;
    _pfactorItems[PfItemUpIndex].unitAmount = data.unitAmount;
    _pfactorItems[PfItemUpIndex].totalAmount = data.totalAmount;
    _pfactorItems[PfItemUpIndex].productGroup = data.productGroup;
    _pfactorItems[PfItemUpIndex].productGroupTxt = data.productGroupTxt;
    
    
    $("#AddBtnId").css("display", "flex");
    $("#CancelBtnId").css("display", "flex");
    $("#UpBtnId").css("display", "none");
    
    
    ShowItemPfactor();
    PfItemUpIndex = null;
    mySer.emptyForm("PreFacItem");
    calculateTotal();
    calculateText();
    
    toastifySuccess("با موفقیت ویرایش شد!");
    
    if (window.innerWidth < 992) {
        canselModalItemPreFactor();
    }
    
};


const delPfactorItem = (index) => {
    let item = _pfactorItems[index];
    
    if (item.id != null) {
        item.isDeleted = true;
        //_pfactorItems = _pfactorItems.filter(f => f.isDeleted != true);
        
        ShowItemPfactor();
        calculateTotal();
        calculateText();
    } else {
        _pfactorItems.splice(index, 1);
        ShowItemPfactor();
        calculateTotal();
        calculateText();
    }
    console.log(index, _pfactorItems);
    
};

const delfactorItem = async (index) => {
    
    
    let item = _factorItems[index];
    
    if (item.id != null) {
        item.isDeleted = true;
        
        _factorItems = _factorItems.filter(f => f.isDeleted !== true);
        ShowItemfactor();
        await calculateTotalFactor();
        calculateTextFactor();
        
    } else {
        
        _factorItems.splice(index, 1);
        ShowItemfactor();
        await calculateTotalFactor();
        calculateTextFactor();
        
    }
    
};


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
    
    SanadPayList = [fileItem]; // حذف
    //await ShowModalInvoice(1);
}
async function acceptAcc(id, accepted, pfId) {
    try {
        FLshowLoading();
        const res = await axios(`/Accountancy/ToggleAcceptAcc/${id}?accepted=${accepted}`);
        if (res.status === 200) {
            await getAllReceiptsPreFactor(pfId);
            
            await GetPreFactorFinancialExpert();
            
            FLshowLoading(false);
        }
    } catch (err) {
        console.log(err);
    }
}


async function acceptAccountancy(id, status, description = null) {
    try {
        FLshowLoading();
        const model = { id, description };
        const res = await axios.post(`/Accountancy/AcceptAccountancy?status=${status}`, model);
        if (res.status === 200) {
            await refreshInvoiceSuccess();
            if (status === 2 || status === 3) {
                toastifySuccess(`${status === 2 ? "تایید شد" : "رد شد"}`);
            }
            FLshowLoading(false);
            return true;
        }
    } catch (err) {
        console.log(err);
        return false;
    }
}





