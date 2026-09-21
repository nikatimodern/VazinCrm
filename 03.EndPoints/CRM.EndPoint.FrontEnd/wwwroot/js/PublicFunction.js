var customerId = null;

const getBaseDescription = async (type = null) => {  // 0: preFactor | 1: Factor | 2: termsPrefactor
    return await sendApiRequest({
        url: "/Factor/GetItemsDescription",
        params: { type },
    });
};

/*function calculateItem(formId, weightSelector = "[name='weightCount']", unitSelector = "[name='unitAmount']", totalSelector = "[name='totalAmount']", discountSelector = "[name='discount']") {
    const form = document.getElementById(formId);
    const getValue = (selector) => {
        const el = form.querySelector(selector);
        if (!el) return 0;
        
        return el.tagName === "INPUT" || el.tagName === "TEXTAREA"
            ? SeparatedNumberRemove(el.value)
            : SeparatedNumberRemove(el.innerText);
    };
    
    const setValue = (selector, value) => {
        const el = form.querySelector(selector);
        if (!el) return;
        
        if (el.tagName === "INPUT" || el.tagName === "TEXTAREA") {
            el.value = value;
        } else {
            el.innerText = value;
        }
    };
    
    const unitAmount = Number(getValue(unitSelector)) || 0;
    const weightCount = Number(getValue(weightSelector)) || 0;
    const discount = Number(getValue(discountSelector)) || 0;
    
    
    const amountBeforeDiscount = Math.round(unitAmount * weightCount);
    const total = Math.max(0, amountBeforeDiscount - discount);
    
    setValue(totalSelector, total.toLocaleString());
}*/

function calculateItem(
    formId,
    weightSelector = "[name='weightCount']",
    unitSelector = "[name='unitAmount']",
    totalSelector = "[name='totalAmount']",
    discountSelector = "[name='discount']",
) {
    const form = document.getElementById(formId);
    
    const getValue = (selector) => {
        const el = form.querySelector(selector);
        if (!el) return 0;
        
        return el.tagName === "INPUT" || el.tagName === "TEXTAREA"
            ? SeparatedNumberRemove(el.value)
            : SeparatedNumberRemove(el.innerText);
    };
    
    const setValue = (selector, value) => {
        const el = form.querySelector(selector);
        if (!el) return;
        
        if (el.tagName === "INPUT" || el.tagName === "TEXTAREA") {
            el.value = value;
        } else {
            el.innerText = value;
        }
    };
    
    const unitAmount = Number(getValue(unitSelector)) || 0;
    const weightCount = Number(getValue(weightSelector)) || 0;
    let discount = Number(getValue(discountSelector)) || 0;
    
    const amountBeforeDiscount = Math.round(unitAmount * weightCount);
    
    discount = Math.min(Math.max(0, Math.round(discount)), amountBeforeDiscount);
    
    const total = Math.max(0, amountBeforeDiscount - discount);
    
    setValue(discountSelector, discount.toLocaleString());
    setValue(totalSelector, total.toLocaleString());
}


const distributeItemDiscount = (items, totalDiscount) => {
    let remainingDiscount = Math.max(0, Math.round(totalDiscount));
    
    const totalBeforeDiscount = items.reduce((sum, item) => {
        const amount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
        
        return sum + amount;
    }, 0);
    
    if (!totalBeforeDiscount || !remainingDiscount) {
        items.forEach(item => {
            item.discount = 0;
            
            const beforeDiscount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
            
            item.totalAmount = beforeDiscount;
        });
        
        return;
    }
    
    remainingDiscount = Math.min(remainingDiscount, totalBeforeDiscount);
    
    items.forEach((item, index) => {
        const beforeDiscount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
        
        if (index === items.length - 1) {
            item.discount = Math.min(remainingDiscount, beforeDiscount);
        } else {
            const proportionalDiscount = Math.round(remainingDiscount * beforeDiscount / totalBeforeDiscount); // تخفیف نسبی
            
            item.discount = Math.min(proportionalDiscount, beforeDiscount);
            
            remainingDiscount -= item.discount;
        }
        
        item.totalAmount = Math.max(0, beforeDiscount - item.discount);
    });
};

const calculateInvoiceTotals = (
    {
        items,
        invoice,
        discountInputId,
        sumTotalAmountId,
        totalTaxId,
        paymentAmountId,
        updateItemsFromDiscount = false,
    }) => {
    const activeItems = items.filter(item => !item.isDeleted);
    
    const taxPercent = setting?.taxPercent || 10;
    const taxAfterDiscount = setting?.taxAfterDiscount ?? true;
    
    const discountInput = document.getElementById(discountInputId);
    
    let sumTotalAmount = 0;
    let totalDiscount = 0;
    let totalTax = 0;
    
    /*
     * مبلغ قبل از تخفیف
     */
    activeItems.forEach(item => {
        const beforeDiscount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
        
        sumTotalAmount += beforeDiscount;
    });
    
    sumTotalAmount = Math.round(sumTotalAmount);
    
    /*
     * اگر تخفیف کلی توسط کاربر تغییر کرده،
     * آن را بین آیتم‌ها تقسیم کن.
     */
    if (updateItemsFromDiscount && discountInput) {
        const requestedDiscount = Math.max(0, Math.round(Number(SeparatedNumberRemove(discountInput.value)) || 0));
        
        distributeItemDiscount(activeItems, requestedDiscount);
    }
    /*
     * محاسبه تخفیف، مبلغ نهایی و مالیات هر آیتم
     */
    activeItems.forEach(item => {
        const beforeDiscount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
        
        const discount = Math.min(Math.max(0, Math.round(Number(item.discount) || 0)), beforeDiscount);
        
        item.discount = discount;
        
        item.totalAmount = Math.max(0, beforeDiscount - discount);
        
        /*
         * مالیات فقط برای فاکتور رسمی
         */
        if (invoice.officialType === 1) {
            const taxBase = taxAfterDiscount
                ? item.totalAmount
                : beforeDiscount;
            
            item.taxAmount = calculateItemTax(taxBase, taxPercent);
        } else {
            item.taxAmount = 0;
        }
        
        totalDiscount += item.discount;
        totalTax += item.taxAmount;
    });
    
    totalDiscount = Math.round(totalDiscount);
    totalTax = Math.round(totalTax);
    
    /*
     * درصد مالیات فاکتور
     */
    invoice.percentTax = invoice.officialType === 1 ? taxPercent : 0;
    
    /*
     * مبلغ قبل از مالیات
     */
    const amountBeforeTax = Math.max(0, sumTotalAmount - totalDiscount);
    
    /*
     * مبلغ نهایی
     */
    const paymentAmount = Math.round(amountBeforeTax + totalTax);
    
    invoice.sumTotalAmount = sumTotalAmount;
    invoice.discount = totalDiscount;
    invoice.totalTax = totalTax;
    invoice.paymentAmount = paymentAmount;
    
    /*
     * UI
     */
    const sumTotalAmountElement = document.getElementById(sumTotalAmountId);
    const totalTaxElement = document.getElementById(totalTaxId);
    const paymentAmountElement = document.getElementById(paymentAmountId);
    
    if (discountInput) {
        discountInput.value = totalDiscount.toLocaleString();
    }
    
    if (sumTotalAmountElement) {
        sumTotalAmountElement.textContent = sumTotalAmount.toLocaleString();
    }
    
    if (totalTaxElement) {
        totalTaxElement.textContent = totalTax.toLocaleString();
    }
    
    if (paymentAmountElement) {
        paymentAmountElement.textContent = paymentAmount.toLocaleString();
    }
    
    return invoice;
};
const calculateItemTax = (amount, taxPercent) => {
    return Math.round(amount * taxPercent / 100);
};

const generateFactorItemDom = (data, { type = "edit-delete", onDelete, onUpdate, onSelect } = {}) => {
    
    const container = document.createElement("div");
    container.className = "pf-items-list";
    
    data.forEach((item, index) => {
        if (item.isDeleted) return;
        
        item.product ??= "";
        item.discount ??= 0;
        item.row = index + 1;
        
        const beforeDiscountAmount = calculateTotalPrice(item.weightCount, item.unitAmount);
        
        const beforeDiscountAmountStr = beforeDiscountAmount ? beforeDiscountAmount.toLocaleString() : "-";
        const uAmount = item.unitAmount ? item.unitAmount.toLocaleString() : "-";
        const discountAmount = item.discount ? item.discount.toLocaleString() : "-";
        const taxAmount = item.taxAmount ? item.taxAmount.toLocaleString() : "-";
        const tAmount = item.totalAmount ? item.totalAmount.toLocaleString() : "-";
        
        const element = document.createElement("div");
        element.className = "pf-item";
        
        let actionButtons = "";
        
        if (type === "edit-delete") {
            actionButtons = `
                <button type="button" class="pf-item-action pf-item-delete">
                    <i class="fa fa-trash"></i>
                </button>

                <button type="button" class="pf-item-action pf-item-update">
                    <i class="fa fa-pencil"></i>
                </button>
            `;
        } else if (type === "select") {
            actionButtons = `
                <button type="button" class="buttonPurple2 pf-item-select">
                    افزودن
                </button>
            `;
        }
        
        element.innerHTML = `
            <div class="pf-item-row">
                <div class="InfoDataItemHead">
                    ${index + 1}
                </div>

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
                        <div class="pf-item-field pf-item-tax">
                            <label>مالیات</label>
                            <label>
                                ${taxAmount}
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
        
        if (type === "edit-delete") {
            element.querySelector(".pf-item-delete")?.addEventListener("click", async () => {
                await onDelete?.(index, item.id);
            });
            
            element.querySelector(".pf-item-update")?.addEventListener("click", async () => {
                await onUpdate?.(index, item.id);
            });
        } else if (type === "select") {
            element.querySelector(".pf-item-select")?.addEventListener("click", async () => {
                await onSelect?.(index, item.id);
            });
        }
        
        container.appendChild(element);
        
    });
    
    return container;
};


const validatePfItemDiscount = (formId) => {
    const form = document.getElementById(formId);
    
    const weightCount = Number(form.elements.namedItem("weightCount").value) || 0;
    const unitAmount = Number(SeparatedNumberRemove(form.elements.namedItem("unitAmount").value)) || 0;
    const discount = Number(SeparatedNumberRemove(form.elements.namedItem("discount").value)) || 0;
    
    const amountBeforeDiscount = Math.round(weightCount * unitAmount);
    
    if (discount > amountBeforeDiscount) {
        const discountInput = form.elements.namedItem("discount");
        
        discountInput.classList.add("AlertReq");
        discountInput.focus();
        
        return false;
    }
    
    form.elements.namedItem("discount").classList.remove("AlertReq");
    
    return true;
};


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


let allProductPromise = null;

const getAllProductOp = async (form) => {
    const formElement = resolveElement(form);
    if (!formElement) return;
    
    const productCodeElement = formElement.querySelector("select[name='productCode']");
    if (!productCodeElement) return;
    
    if (!allProductPromise) {
        allProductPromise = axios("/Product/GetAllProduct").then(res => res.data);
    }
    
    const products = await allProductPromise;
    
    if (productCodeElement.tomselect) {
        return productCodeElement.tomselect;
    }
    
    return new TomSelect(productCodeElement, {
        options: products,
        valueField: "id",
        placeholder: "انتخاب",
        maxItems: 1,
        selectOnTab: true,
        // dropdownParent
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