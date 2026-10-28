let oldReport = null;
let dealIdReport = null;
let dealSerialNumberReport = null;
let typeReportSubmit = 0;

var filterReportConfig = {};

const SubmitReportForm = async () => {
    if (await mySer.CheckValidForm("#ReportFormId")) {
        let data = mySer.convertFormToJSON("#ReportFormId");
        const el = document.getElementById("repCustomerTxt");
        
        //data.customerId = data.repCustomerTxt;
        data.customerTxt = el.options[el.selectedIndex].text;
        data.dealId = dealIdReport;
        data.dealSerialNumber = dealSerialNumberReport;
        
        if (oldReport != null) {
            data = { ...oldReport, ...data };
            oldReport = null;
        }
        
        const resultCall = document.getElementById("resultCall");
        const finalResultCall = document.getElementById("finalResultCall");
        const reasonCancel = document.getElementById("reasonCancel");
        data.resultCallTxt = resultCall.value ? resultCall.options[resultCall.selectedIndex].text : "";
        data.finalResultCallTxt = finalResultCall.value ? finalResultCall.options[finalResultCall.selectedIndex].text : "";
        data.reasonCancelTxt = reasonCancel.value ? reasonCancel.options[reasonCancel.selectedIndex].text : "";

        
        FLshowLoading();
        $.post(`/Report/AddReport`, data, async (res) => {
            if (res.status === 200) {
                if (typeReportSubmit === 0) {
                    const currentPath = window.location.pathname.toLowerCase();
                    if (currentPath.includes("report")) {
                        await getAllReports();
                    } else if (currentPath.includes("dashboard")) {
                        await getDashReports();
                    }
                } else if (typeReportSubmit === 4) {
                    await refreshDealReports();
                    if (typeof getAllDeals === "function") {
                        await getAllDeals();
                    }
                }
                
                toastifySuccess(res.message);
            } else if (res.status === 201) {
                toastifyError(res.message);
            }
            FLshowLoading(false);
            closeModalReport();
        }).fail(() => {
            setTimeout(async () => await SubmitReportForm(), 3000);
        });
    }
    
};

const getBaseItemsReport = async () => {
    return await sendApiRequest({
        url: "/Report/GetBaseItemsReport",
    });
};


async function initReportModal() {
    const baseItem = await getBaseItemsReport();
    if (!baseItem) return;
    
    createOption("resultCall", baseItem.resultCall);
    createOption("finalResultCall", baseItem.finalResultCall);
    createOption("reasonCancel", baseItem.reasonCancel);
    
    const container = document.getElementById("ReportFormId");
    
    const resultCallWrapper = container.querySelector(".resultCallWrapper");
    const finalResultCallWrapper = container.querySelector(".finalResultCallWrapper");
    const reasonCancelWrapper = container.querySelector(".reasonCancelWrapper");
    
    const resultCallSelect = resultCallWrapper.querySelector("select");
    const finalResultCallSelect = finalResultCallWrapper.querySelector("select");
    const reasonCancelSelect = reasonCancelWrapper.querySelector("select");
    
    container.querySelectorAll("input[name='reportType']").forEach(elm => elm.addEventListener("change", (e) => {
        const showResultFollowUpContainer = Number(e.target.value) === 1;
        container.querySelector("#resultFollowUpContainer").classList.toggle("d-none", !showResultFollowUpContainer);
        toggleElementsRequired(resultCallSelect, showResultFollowUpContainer);
        toggleElementsRequired([finalResultCallSelect, reasonCancelSelect], false);
        finalResultCallWrapper.classList.toggle("d-none", true);
        reasonCancelWrapper.classList.toggle("d-none", true);
        resultCallSelect.value = "";
        finalResultCallSelect.value = "";
        reasonCancelSelect.value = "";
    }));
    
    resultCallSelect.addEventListener("change", (e) => {
        const value = e.target.value;
        const showFinalResult = value === "ed0feaf4-9264-4028-b8cf-1a268bfc1c9a"
            || value === "310df88c-b65b-4f26-9f1e-ab72f489bc1e"
            || value === "986e7e82-9853-402f-a7f6-36252965ff82";
        const showReasonCancel = value === "986e7e82-9853-402f-a7f6-36252965ff82";
        
        
        finalResultCallWrapper.classList.toggle("d-none", !showFinalResult);
        reasonCancelWrapper.classList.toggle("d-none", !showReasonCancel);
        toggleElementsRequired(finalResultCallSelect, showFinalResult);
        
        finalResultCallSelect.value = showReasonCancel ? "82769710-880f-43aa-bd83-f5c6016ce38c" : "";
        toggleElementsDisabled(finalResultCallSelect, showReasonCancel);
        reasonCancelSelect.value = "";
        toggleElementsRequired(reasonCancelSelect, showReasonCancel);
    });
    
    finalResultCallSelect.addEventListener("change", (e) => {
        const value = e.target.value;
        const showReasonCancel = value === "82769710-880f-43aa-bd83-f5c6016ce38c";
        
        reasonCancelSelect.value = "";
        reasonCancelWrapper.classList.toggle("d-none", !showReasonCancel);
        toggleElementsRequired(reasonCancelSelect, showReasonCancel);
    });
}


const initCustomerOpReport = async () => {
    const customerData = await sendApiRequest({ url: "/Customer/GetAllCustomers" });
    const repTelElm = document.getElementById("repTel");
    
    const cusTomSelect = new TomSelect("#repCustomerTxt", {
        options: customerData.map(c => ({ text: `${c.customerName} (کد مشتری: ${c.customerCode})`, value: c.id })),
        maxItems: 1,
        persist: false,
        openOnFocus: false,
        createOnBlur: true,
        selectOnTab: true,
        placeholder: "نام مشتری ...",
        
        create: (input, callback) => callback({ value: `custom-${input}`, text: input }),
        
        async onChange(value) {
            if (!value) return;
            if (!value.startsWith("custom")) {
                toggleElementsDisabled(repTelElm, true);
                cusTomSelect.disable();
                
                const data = await sendApiRequest({ url: "/Customer/GetCustomer", params: { id: value } });
                // repTelElm.tomselect.setValue(res.data.mobile, true);
                repTelElm.value = data.mobile;
                flashItem(repTelElm);
                // flashItem(repTelElm.tomselect.control);
                cusTomSelect.enable();
                toggleElementsDisabled(repTelElm, false);
            }
        },
        
        onDelete(values) {
            const value = values[0];
            if (value.startsWith("custom")) {
                const opt = cusTomSelect.options[value];
                
                cusTomSelect.removeItem(value, true);
                cusTomSelect.setTextboxValue(opt.text.trim());
                cusTomSelect.refreshOptions(true);
                return false;
            }
            
            // repTelElm.tomselect.clear(true);
            repTelElm.value = "";
            return true;
        },
    });
    /*
        new TomSelect(repTelElm, {
            options: customerRes.data.map(c => ({ text: `${c.customerName} (${c.mobile})`, value: c.mobile })),
            openOnFocus: false,
            searchField: ["value"],
            maxItems: 1,
            persist: false,
            create: true,
            createOnBlur: true,
            selectOnTab: true,
            placeholder: "...شماره موبایل",
            
            render: {
                item(data, escape) {
                    return `<div>${escape(data.value)}</div>`;
                },
            },
            
            onChange(value) {
                if (!value) return;
                const customer = customerRes.data.find(x => x.mobile === value);
                if (!customer) return;
                
                cusTomSelect.setValue(customer.id);
                flashItem(cusTomSelect.control);
            },
        });
    const repTelTsInput = document.getElementById("repTel-ts-control");
    repTelTsInput.setAttribute("inputmode", "numeric");
    repTelTsInput.addEventListener("input", handlerMobileInput);*/
};

let reportModalLoaded = false;
const ShowModalReport = async (model = null, passType = 0, dealId = null, dealSerialNumber = null) => {
    const repTelElm = document.getElementById("repTel");
    const selectElm = document.getElementById("repCustomerTxt");
    
    if (!reportModalLoaded) {
        await Promise.all([initReportModal(), initCustomerOpReport()]);
        reportModalLoaded = true;
    }
    
    dealIdReport = dealId;
    dealSerialNumberReport = dealSerialNumber;
    typeReportSubmit = passType;
    
    if (model) {
        let { id = null, agentName, companyName, customerCode, mobile } = model;
        repTelElm.value = mobile;
        
        if (id) {
            selectElm.tomselect.setValue(id, true);
        } else {
            const customerCodeText = customerCode ? ` (کد مشتری ${customerCode})` : "";
            const text = `${companyName ?? ""} ${agentName}${customerCodeText}`.trim();
            
            const customId = `custom-${text}`;
            selectElm.tomselect.addOption({ value: customId, text });
            selectElm.tomselect.setValue(customId, true);
        }
        selectElm.tomselect.disable();
        toggleElementsDisabled(repTelElm, true);
    }
    
    /*createRating("reportRate", {
        count: 10,
        value: 0,
        resultId: "reportRateResult",
        onRate: (val) => {
            console.log(val);
        },
    });*/
    
    ModalToggle("#ModalReport");
};
const closeModalReport = () => {
    oldReport = null;
    $("#repCustomerTxt").val(null).trigger("change");
    document.getElementById("resultFollowUpContainer").classList.add("d-none");
    ModalToggle("#ModalReport");
    mySer.emptyForm("ReportFormId");
    const repTelElm = document.getElementById("repTel");
    const selectElm = document.getElementById("repCustomerTxt");
    selectElm.tomselect.enable();
    toggleElementsDisabled(repTelElm, false);
};


async function getAllReports(showLoading = true) {
    const containerReport = document.getElementById("allReportsId");
    if (!containerReport) return;
    
    const data = filterReportConfig || {};
    
    const model = {
        ...data,
        filt: data.filterVar,
        serVal: data.varSearch,
        count: data.count ?? 50,
        _type: data._type ?? -1,
    };
    
    await sendApiRequest({
        url: "/Report/GetAllReportPartial",
        method: "POST",
        successMsg: null,
        data: model,
        showLoading,
        onSuccess: async (res) => {
            containerReport.innerHTML = res.data.html;
            
            const select = document.getElementById("reportCountSelect");
            const lastOption = select?.querySelector("option[value='-1']");
            const totalCount = res.data.count ?? 0;
            
            if (select && lastOption) {
                lastOption.textContent = select.value === "-1" ? `همه (${totalCount})` : "همه";
                
                if (!data.count) {
                    if (totalCount < 50) {
                        select.value = "-1";
                        lastOption.textContent = `همه (${totalCount})`;
                    } else {
                        select.value = "50";
                    }
                }
            }
        },
        onError: (err) => {
            containerReport.innerHTML = err.response.data; // "خطا در دریافت گزارش ها :(";
        },
    });
}

async function getReport(id) {
    return await sendApiRequest({
        url: "/Report/GetReport/",
        params: { id },
    });
}

const ShowUpdateReport = async (id, passType = 0) => {
    if (!reportModalLoaded) {
        await initReportModal();
        await initCustomerOpReport();
        reportModalLoaded = true;
    }
    
    typeReportSubmit = passType;
    
    const data = await getReport(id);
    
    if (!data) return;
    
    oldReport = data;
    dealIdReport = data.dealId;
    dealSerialNumberReport = data.dealSerialNumber;
    
    mySer.convertJsonToForm(data, "ReportFormId");
    
    if (data.customerId === null && data.customerTxt) {
        const selectTomCustomers = document.getElementById("repCustomerTxt");
        const customId = `custom-${data.customerTxt}`;
        selectTomCustomers.tomselect.addOption({ value: customId, text: data.customerTxt });
        selectTomCustomers.tomselect.setValue(customId, true);
    }
    
    ModalToggle("#ModalReport");
};


const DelReport = async (id, passType = 0) => {
    await deleteItem({
        url: `/Report/DelReport/${id}`,
        callback: async () => {
            if (passType === 0) {
                const currentPath = window.location.pathname.toLowerCase();
                if (currentPath.includes("report")) {
                    await getAllReports();
                } else if (currentPath.includes("dashboard")) {
                    await getDashReports();
                }
            } else if (passType === 4) {
                await refreshDealReports();
            }
        },
    });
};
