var customerId = null;

const prepareResponsiveForms = () => {
    if (window.innerWidth >= 992) return;
    document.getElementById("PreFacItemContainer")?.remove();
    document.getElementById("FacItemContainer")?.remove();
    document.getElementById("supplyFactorContainer")?.remove();
    document.getElementById("orderFormContainer")?.remove();
    const descriptionOrder = document.getElementById("descriptionOrder");
    if (descriptionOrder) descriptionOrder.rows = 3;
};
prepareResponsiveForms();


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
        const unitElm = document.querySelector(`${window.innerWidth < 992 ? "#ItemPreFactorModal" : "#PreFactor"} #PreFacItem select[name='unit']`);
        if (!unitElm) return;
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
        const unitElm = document.querySelector(`${window.innerWidth < 992 ? "#ItemFactorModal" : "#Factor"} #FacItem select[name='unit']`);
        if (!unitElm) return;
        $(unitElm).select2({
            data: unitItems,
            dir: "rtl",
            placeholder: "انتخاب",
            selectionCssClass: "form-control",
            dropdownParent: window.innerWidth < 992 ? $("#ItemFactorModal") : $("#Factor"),
        });
        $(unitElm).val(null).trigger("change");
    } else if (type === 3) {
        const unitInput = document.querySelector(`#${formId} select[name='unit']`);
        if (!unitInput) return;
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
    } else if (type === 4) {
        const unitElm = document.querySelector(`${window.innerWidth < 992 ? "#ItemSupplyFactorModal" : "#supplyFactorModal"} #supplyFactorItem select[name='unit']`);
        if (!unitElm) return;
        $(unitElm).select2({
            data: unitItems,
            dir: "rtl",
            placeholder: "انتخاب",
            minimumResultsForSearch: Infinity,
            selectionCssClass: "form-control",
            dropdownParent: window.innerWidth < 992 ? $("#ItemSupplyFactorModal") : $("#supplyFactorModal"),
        });
        $(unitElm).val(null).trigger("change");
    }
};


let allProductPromise = null;

const getAllProductOp = async (form) => {
    const formElement = resolveElement(form);
    if (!formElement) return;
    
    const productCodeElement = formElement.querySelector("select[name='productCode']");
    if (!productCodeElement) return;
    
    if (!allProductPromise) {
        allProductPromise = sendApiRequest({ url: "/Product/GetAllProduct" }).then(products => {
            if (!products) allProductPromise = null;
            return products;
        });
    }
    
    const products = await allProductPromise;
    if (!products || !productCodeElement.isConnected) return;
    
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

/* ----------------------------------------------------------------------------- */

const getBaseDescription = async (type = null) => {  // 0: preFactor | 1: Factor | 2: termsPrefactor
    return await sendApiRequest({
        url: "/Factor/GetItemsDescription",
        params: { type },
    });
};

function calculateItem(
    formId,
    weightSelector = "[name='weightCount']",
    unitSelector = "[name='unitAmount']",
    totalSelector = "[name='totalAmount']",
    discountSelector = "[name='discount']",
) {
    const form = document.getElementById(formId);
    if (!form) return;
    
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
    
    /*
     * مبلغ قبل از تخفیف و مالیات
     */
    const amountBeforeDiscount = Math.round(unitAmount * weightCount);
    
    /*
     * تخفیف
     */
    discount = Math.min(Math.max(0, Math.round(discount)), amountBeforeDiscount);
    
    /*
     * مبلغ بعد از تخفیف
     */
    const amountAfterDiscount = Math.max(0, amountBeforeDiscount - discount);
    
    /*
     * مالیات
     */
    let taxAmount = 0;
    
    if (pFactor.officialType === 1) {
        const taxPercent = setting?.taxPercent || 10;
        const taxAfterDiscount = setting?.taxAfterDiscount ?? true;
        
        const taxBase = taxAfterDiscount ? amountAfterDiscount : amountBeforeDiscount;
        
        taxAmount = calculateItemTax(taxBase, taxPercent);
    }
    
    /*
     * مبلغ نهایی:
     * مبلغ قبل تخفیف - تخفیف + مالیات
     */
    const total = Math.max(0, amountAfterDiscount + taxAmount);
    
    setValue(discountSelector, discount.toLocaleString());
    
    setValue(totalSelector, total.toLocaleString());
}


const distributeItemDiscount = (items, totalDiscount) => {
    const discount = Math.max(0, Math.round(totalDiscount));
    
    const totalBeforeDiscount = items.reduce((sum, item) => {
        return sum + Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
    }, 0);
    
    if (!totalBeforeDiscount || !discount) {
        items.forEach(item => {
            item.discount = 0;
        });
        
        return;
    }
    
    const finalDiscount = Math.min(discount, totalBeforeDiscount);
    
    let distributedDiscount = 0;
    
    items.forEach((item, index) => {
        const beforeDiscount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
        
        if (index === items.length - 1) {
            item.discount = Math.min(Math.max(0, finalDiscount - distributedDiscount), beforeDiscount);
        } else {
            item.discount = Math.min(Math.round(finalDiscount * beforeDiscount / totalBeforeDiscount), beforeDiscount);
            
            distributedDiscount += item.discount;
        }
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
    let paymentAmount = 0;
    
    /*
     * مبلغ قبل از تخفیف و مالیات
     */
    activeItems.forEach(item => {
        const beforeDiscount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
        
        sumTotalAmount += beforeDiscount;
    });
    
    sumTotalAmount = Math.round(sumTotalAmount);
    
    /*
     * اگر تخفیف کلی تغییر کرده،
     * تخفیف را بین آیتم‌ها تقسیم کن.
     */
    if (updateItemsFromDiscount && discountInput) {
        const requestedDiscount = Math.max(0, Math.round(Number(SeparatedNumberRemove(discountInput.value)) || 0));
        
        distributeItemDiscount(activeItems, requestedDiscount);
    }
    
    /*
     * محاسبه نهایی هر آیتم
     */
    activeItems.forEach(item => {
        const beforeDiscount = Math.round((Number(item.weightCount) || 0) * (Number(item.unitAmount) || 0));
        
        /*
         * تخفیف آیتم
         */
        const discount = Math.min(Math.max(0, Math.round(Number(item.discount) || 0)), beforeDiscount);
        
        item.discount = discount;
        
        /*
         * مبلغ بعد از تخفیف
         */
        const amountAfterDiscount = Math.max(0, beforeDiscount - discount);
        
        /*
         * مالیات آیتم
         */
        if (invoice.officialType === 1) {
            const taxBase = taxAfterDiscount
                ? amountAfterDiscount
                : beforeDiscount;
            
            item.taxAmount = calculateItemTax(taxBase, taxPercent);
        } else {
            item.taxAmount = 0;
        }
        
        /*
         * مبلغ نهایی آیتم:
         * بعد از تخفیف + مالیات
         */
        item.totalAmount = Math.max(0, amountAfterDiscount + item.taxAmount);
        
        totalDiscount += item.discount;
        totalTax += item.taxAmount;
        paymentAmount += item.totalAmount;
    });
    
    totalDiscount = Math.round(totalDiscount);
    totalTax = Math.round(totalTax);
    paymentAmount = Math.round(paymentAmount);
    
    /*
     * درصد مالیات
     */
    invoice.percentTax = invoice.officialType === 1 ? taxPercent : 0;
    
    /*
     * اطلاعات نهایی فاکتور
     */
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

const generateInvoiceItemDom = (data, { type = "edit-delete", onDelete, onUpdate, onSelect, fieldRules = {} } = {}) => {
    const container = document.createElement("div");
    container.className = "pf-items-list";
    
    data.forEach((item, index) => {
        if (item.isDeleted) return;
        
        item.product ??= "";
        item.discount ??= 0;
        item.row = index + 1;
        
        const incompleteFields = getIncompleteFields(item, fieldRules);
        const isIncomplete = incompleteFields.length > 0;
        
        const weight = Number(SeparatedNumberRemove(item.weightCount ?? item.weight)) || 0;
        const beforeDiscountAmount = calculateTotalPrice(weight, item.unitAmount);
        
        const beforeDiscountAmountStr = beforeDiscountAmount ? beforeDiscountAmount.toLocaleString() : "-";
        const uAmount = item.unitAmount ? item.unitAmount.toLocaleString() : "-";
        const discountAmount = item.discount ? item.discount.toLocaleString() : "0";
        const taxAmount = item.taxAmount ? item.taxAmount.toLocaleString() : "-";
        const tAmount = item.totalAmount ? item.totalAmount.toLocaleString() : "-";
        
        const element = document.createElement("div");
        // element.className = "pf-item";
        element.className = `pf-item ${isIncomplete ? "pf-item-incomplete" : ""}`;
        
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
                                <span class="pf-item-productSmallName">${item.productSmallName ?? ""}</span>
                                <span>${item.product ?? ""}</span>
                                ${isIncomplete ? `
                                    <p class="pf-item-incomplete-field">
                                      <i class="fa fa-exclamation-circle"></i>
                                      نیاز به ویرایش
                                    </p>` : ""}
                            </label>
                        </div>

                        <div class="pf-item-field pf-item-quantity">
                            <label>تعداد/مقدار</label>
                            <label>${weight.toLocaleString()}</label>
                        </div>

                        <div class="pf-item-field pf-item-unit">
                            <label>واحد</label>
                            <label>${item.unitTxt ?? ""}</label>
                        </div>

                        <div class="pf-item-field pf-item-unit-amount">
                            <label>مبلغ واحد</label>
                            <label>
                                ${uAmount}
                                <span class="fs-small">${item.currencyTypeTxt ?? ""}</span>
                            </label>
                        </div>

                        <div class="pf-item-field pf-item-beforeDiscount">
                            <label><span class="d-none d-sm-inline">مبلغ </span>قبل تخفیف</label>
                            <label>
                                ${beforeDiscountAmountStr}
                                <span class="fs-small">${item.currencyTypeTxt ?? ""}</span>
                            </label>
                        </div>

                        <div class="pf-item-field pf-item-discount">
                            <label>تخفیف</label>
                            <label>
                                ${discountAmount}
                                <span class="fs-small">${item.currencyTypeTxt ?? ""}</span>
                            </label>
                        </div>
                        <div class="pf-item-field pf-item-tax">
                            <label>مالیات</label>
                            <label>
                                ${taxAmount}
                                <span class="fs-small">${item.currencyTypeTxt ?? ""}</span>
                            </label>
                        </div>

                        <div class="pf-item-field pf-item-total">
                            <label>مبلغ نهایی</label>
                            <label>
                                ${tAmount}
                                <span class="fs-small">${item.currencyTypeTxt ?? ""}</span>
                            </label>
                        </div>

                    </div>

                    <div class="pf-item-actions">
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


function getIncompleteItemsCount(items, fieldRules) {
    return items.filter(item => {
        if (item.isDeleted) return false;
        return getIncompleteFields(item, fieldRules).length > 0;
    }).length;
}

function getIncompleteFields(item, fieldRules = {}) {
    return Object.entries(fieldRules).filter(([fieldName, rules]) => {
        const value = item[fieldName];
        
        if (rules.required && (value === null || value === undefined || String(value).trim() === "")) {
            return true;
        }
        
        if (rules.numeric && (value === null || value === undefined || isNaN(Number(value)))) {
            return true;
        }
        
        if (rules.notZero && Number(value) === 0) {
            return true;
        }
        
        if (rules.min !== undefined && Number(value) < rules.min) {
            return true;
        }
        
        if (rules.max !== undefined && Number(value) > rules.max) {
            return true;
        }
        
        if (rules.validate && !rules.validate(value, item)) {
            return true;
        }
        
        return false;
    }).map(([fieldName]) => fieldName);
}


/**
 * @param {string} formId
 * @param {Object<string, {
 *     required?: boolean,
 *     numeric?: boolean,
 *     notZero?: boolean,
 *     min?: number,
 *     max?: number,
 *     pattern?: RegExp,
 *     validate?: (value: any, item: Object) => boolean
 * }>} customRules
 */
function getFieldRules(formId, customRules = {}) {
    const form = document.getElementById(formId);
    if (!form) return {};
    
    const rules = {};
    
    form.querySelectorAll("[name]").forEach(element => {
        const name = element.name;
        if (!name) return;
        
        const fieldRules = {};
        
        if (element.required) {
            fieldRules.required = true;
        }
        
        if (element.type === "number" || element.classList.contains("valueNumber") || element.classList.contains("currencyNumber")) {
            fieldRules.numeric = true;
        }
        
        if (element.classList.contains("currencyNumber")) {
            fieldRules.min = 0;
        }
        
        rules[name] = {
            ...fieldRules,
            ...customRules[name],
        };
    });
    
    Object.entries(customRules).forEach(([name, fieldRules]) => {
        rules[name] = {
            ...(rules[name] ?? {}),
            ...fieldRules,
        };
    });
    
    return rules;
}

/* ----------------------------------------------------------------------------- */


let idNotAccept;
let typeNotAccept;
async function showModalNotAccept(id, type) { // type: acc= 0 / pf= 12 financial | 13 tamin | 14: SaleAdmin
    const notAcceptCommentElm = document.getElementById("NotAcceptComment");
    if (!notAcceptCommentElm) return;
    notAcceptCommentElm.value = "";
    document.getElementById("NotAcceptHeader").textContent = type !== 0 ? "عدم تایید پیش‌فاکتور" : "عدم تایید واریزی";
    idNotAccept = id;
    typeNotAccept = type;
    ModalToggle("#NotAcceptModal");
}

async function submitNotAccept() { // رد تایید و بازگشت | عدم تایید
    const notAcceptCommentElm = document.getElementById("NotAcceptComment");
    if (!notAcceptCommentElm) return;
    
    if (!notAcceptCommentElm.value.trim().length) {
        toastifyError("دلیل عدم تایید اجباری است!");
        return;
    }
    
    const userFullName = document.getElementById("profileName")?.textContent || "";
    const model = {
        id: idNotAccept,
        description: `${notAcceptCommentElm.value.trim()} (${userFullName})`,
    };
    
    if (typeNotAccept === 0) { // acc
        const result = await acceptAccountancy(model.id, 3, model.description);
        if (!result) return;
        
    } else { // pf
        const urlMap = {
            12: `/Factor/NotAcceptFinancial`,
            13: `/Factor/NotAcceptTaminExpert`,
            14: `/Factor/NotAcceptSalesAdmin`,
        };
        
        await sendApiRequest({
            url: urlMap[typeNotAccept],
            method: "POST",
            data: model,
            successMsg: "عدم تایید با موفقیت ثبت شد",
            onSuccess: async () => {
                switch (typeNotAccept) {
                    case 12:
                        await GetPreFactorFinancialExpert();
                        ModalToggle("#InvoiceListModalId", "hide");
                        break;
                    case 13:
                        await GetPreFactorTaminExpert();
                        break;
                    case 14:
                        await GetPreFactorSalesAdmin();
                        break;
                }
            },
        });
    }
    notAcceptCommentElm.value = "";
    ModalToggle("#NotAcceptModal");
}

async function getSuppliesByPfId(pfId) {
    return await sendApiRequest({
        url: "/Supply/GetSupplies",
        params: { pfId },
    });
}

async function getBaseItemOp(typeCode, showLoading = false) {
    return await sendApiRequest({
        url: "/BaseItem/GetItemsOp",
        params: { typeCode },
        showLoading,
    });
}

async function getCustomersList(showLoading = false) {
    return await sendApiRequest({
        url: "/Customer/GetCustomersList",
        showLoading,
    });
}

async function getCustomer(id, showLoading = false) {
    return await sendApiRequest({
        url: "/Customer/GetCustomer",
        params: { id },
        showLoading,
    });
}
