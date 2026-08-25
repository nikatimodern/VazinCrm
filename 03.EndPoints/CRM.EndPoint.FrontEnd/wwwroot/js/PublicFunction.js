let dealData = {};
var customerId = null;

const getBaseDescription = async (type = null) => {  // 0: preFactor | 1: Factor | 2: termsPrefactor
    try {
        const res = await axios(`/Factor/GetItemsDescription?type=${type}`);
        if (res.status === 200) {
            return res.data;
        }
        return null;
    } catch (err) {
        console.error(err);
        return null;
    }
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


if (window.innerWidth < 992) {
    document.getElementById("PreFacItemContainer")?.remove();
    document.getElementById("FacItemContainer")?.remove();
    document.getElementById("orderFormContainer")?.remove();
    const descriptionOrder = document.getElementById("descriptionOrder");
    if (descriptionOrder) descriptionOrder.rows = 3;
}


document.querySelectorAll("#InvoiceModalId input[type='radio'][name='payType']")?.forEach(radio => {
    radio.addEventListener("change", (e) => {
        const type = Number(e.target?.value);
        if (type === 3) {
            document.getElementById("SanadParId").classList.remove("AlertReqRadio");
        }
        PayFormChange("InvoiceModalId", type);
    });
});
document.querySelectorAll("#AttachInvoiceModalId input[type='radio'][name='payTypeAttach']")?.forEach(radio => {
    radio.addEventListener("change", (e) => {
        const type = Number(e.target.value);
        PayAttachFormChange(type);
    });
});


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


let loadAllProduct = false;
const getAllProductOp = (formId) => {
    if (loadAllProduct) return;
    formId = formId.replace("#", "");
    
    const productCodeElm = document.querySelector(`#${formId} select[name='productCode']`);
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
};


async function acceptAccountancy(id, status, description = null) {
    const result = await sendApiRequest({
        url: `/Accountancy/AcceptAccountancy`,
        method: "POST",
        data: { id, description },
        params: { status },
        successMsg: status === 2 ? "تایید شد" : status === 3 ? "رد شد" : null,
        onSuccess: async () => await refreshInvoiceSuccess(),
    });
    
    return result !== null;
}