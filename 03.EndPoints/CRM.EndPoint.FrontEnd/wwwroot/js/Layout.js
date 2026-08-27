var AccParentId = null;

var showFlag = 0;
var customerIdUp = null;

// var userRol = null;
// var rol;
var currentUser;
var pfactorID = null;
var factorID = null;
var SanadPayList = [];
var attachInvoiceList = [];
var setting = null;


var Invo_id = null;
var Invo_type = null;
var InvoiceType = null;
var invoicePassType = null;
var AccType = null;
var accConfigFilter = {};//موارد فیلترینگ accontancy
//متغییر های فیلترینگ و سرچ کارتابل پیش فاکتور و فاکتور
var varList = {};//موارد فیلترینگ
var varList2 = {};//موارد فیلترینگ

var filterReportConfig = {};


const languageDatatable = {
    emptyTable: "هیچ رکوردی موجود نمی باشد.",
    info: "نمایش _START_ تا _END_ از _TOTAL_ ردیف",
    infoEmpty: "نمایش 0 تا 0 از 0 ردیف",
    infoFiltered: "(فیلتر شده از _MAX_ ردیف)",
    lengthMenu: "نمایش _MENU_ ردیف",
    loadingRecords: "در حال بارگزاری...",
    search: "",
    zeroRecords: "رکوردی با این مشخصات پیدا نشد",
    aria: {
        sortAscending: ": فعال سازی نمایش به صورت صعودی",
        sortDescending: ": فعال سازی نمایش به صورت نزولی",
    },
};
const layoutDatatable = {
    bottomEnd: "info",
    bottomStart: {
        paging: {
            type: "simple_numbers",
        },
    },
    topEnd: {
        pageLength: true,
    },
    topStart: {
        search: {
            placeholder: "جستجو",
        },
    },
};


////////// Set defaults patch in Header Axios ////////////
axios.defaults.headers.patch["Content-Type"] = "application/json";


//
// var mySer = null;
// var CrmSer = null;

var CounterSmsSend = 0;

var currentDateTypeFilter = 2;
var currentMonthArr;
let mySer = new MyService();
let CrmSer = new CrmService();
let myDate = new GetDate();
document.addEventListener("DOMContentLoaded", async () => {
    await GetUserLogin();
    
    // initLightbox();
    
    setting = await GetSetting();
    
    
    AcceptSetting(setting);
    
    
    initTooltipsIfCollapsed();
    dateTimePicker();
    
    getRemind();
    setDateTime();
    //getAllWarnings();
});

const modalSelector = new ModalSelector();

let tooltipList = [];
function initTooltipsIfCollapsed() {
    if (!document.documentElement.classList.contains("sidebar-collapsed")) return;
    
    
    const triggers = document.querySelectorAll("[data-tooltip-left]");
    
    tooltipList = [...triggers]
        .filter(el => !el.closest(".submenu"))
        .map(el => {
            const isSubmenu = el.closest(".has-submenu");
            
            return new bootstrap.Tooltip(el, {
                title: el.getAttribute("data-tooltip-left"),
                delay: { show: 150, hide: 0 },
                customClass: "main-font",
                placement: isSubmenu ? "top" : "left",
            });
        });
    
}

function toggleMenu() {
    const collapsed = document.documentElement.classList.toggle("sidebar-collapsed");
    sessionStorage.setItem("sidebar-collapsed", collapsed);
    
    tooltipList.forEach(t => t.dispose());
    tooltipList = [];
    if (collapsed) initTooltipsIfCollapsed();
}

connection.on("RefreshTasks", async () => {
    if (typeof systematicTask !== "undefined") {
        await Promise.all([getAllTask(0, systematicTask), getAllTask(1, systematicTask), getAllTask(1, systematicTask, 2), getAllTask(2, systematicTask)]);
    }
});


const notifDropdown = Dropdown.setup({
    toggle: document.getElementById("notification-icon"),
    content: document.getElementById("notification-dropdown"),
    
    async onOpen() {
        await getRemind();
    },
    
    options: {
        originalDisplay: "flex",
        effect: "zoom",
        zIndex: 100,
        transitionDuration: 200,
    },
});

function closeNotification() {
    notifDropdown?.close();
}


// function addMinutesToTime(time, minutesToAdd) {
//    let [hour, minute] = time.split(':').map(Number);

//    let totalMinutes = hour * 60 + minute + minutesToAdd;

//    let newHour = Math.floor(totalMinutes / 60) % 24;
//    let newMinute = totalMinutes % 60;

//    let formattedHour = newHour < 10 ? '0' + newHour : newHour;
//    let formattedMinute = newMinute < 10 ? '0' + newMinute : newMinute;

//    return `${formattedHour}:${formattedMinute}`;
// }


// let notifedList = []
// async function notifShow() {
//     notifedList = [];
//     let data =
//     data = data.filter(item => !item.remindDismiss)
//     data.forEach(item => {
//        notifedList.push(item.id);
//        notification(item.id, item.title, item.description,'/carizo/Index', item.dateMiladi)
//     })

//
// }


/////////////////////////////


const getAndSetAllProduct = async (from, searchValue = "") => {
    FLshowLoading();
    const AllProduct = await getAllProduct(searchValue, from);
    const containerId = from === "amountIndex" ? "ListProductId" : "ListAmountBodyId";
    document.getElementById(containerId).innerHTML = AllProduct;
    FLshowLoading(false);
};


const searchProduct = async (e, from) => {
    e.preventDefault();
    const searchValue = e.target.elements.search.value;
    await getAndSetAllProduct(from, searchValue);
};


const showModalAmount = async () => {
    $("#ListAmount").modal("toggle");
    await getAndSetAllProduct("amountModal");
};


const closeModalAmount = () => {
    $("#ListAmount").modal("toggle");
};


let sellersLoaded = false;
const GetSelsOpId = async (Lid, valTriger = null, parentModal = null, type = 2) => {
    if (sellersLoaded) return;
    let sel = await CrmSer.getUsers(type);
    $("#" + Lid).select2({
        data: sel, dir: "rtl", placeholder: "انتخاب", selectionCssClass: "form-control", dropdownParent: $(parentModal),
    });
    $("#" + Lid).val(valTriger).trigger("change");
    sellersLoaded = true;
};


let customersLoaded = false;
const GetCustomersOpId = async (Lid, valTriger = null, parentModal = null) => {
    if (customersLoaded) return;
    
    let roles = JSON.parse(sessionStorage.getItem("Rols"));
    if (roles?.some(r => r.name === "Customer")) return;
    $.get(baseUrl + "order/GetCustomersOpId", async (res) => {
        if (!res) return;
        $("#" + Lid).select2({
            data: res, dir: "rtl", placeholder: "انتخاب", selectionCssClass: "form-control", dropdownParent: $(parentModal),
        });
        $("#" + Lid).val(valTriger).trigger("change");
    });
    
    customersLoaded = true;
};


document.getElementById("CusFilterChd")?.addEventListener("change", () => {
    const flag = document.getElementById("CusFilterChd").checked;
    filterToggleDisable(flag, "CusFilterDivContent");
});


document.getElementById("SelFilterChd")?.addEventListener("change", () => {
    const flag = document.getElementById("SelFilterChd").checked;
    filterToggleDisable(flag, "SelFilterDivContent");
});


document.getElementById("DateFilterChk")?.addEventListener("change", () => {
    const flag = document.getElementById("DateFilterChk").checked;
    filterToggleDisable(flag, "DateFilterDivContent");
    filterToggleDisable(flag, "dateFilterBtnId");
});


let filterVarConfig = null;
const getFilterElement = (id) => document.getElementById(id);

const getFilterInput = (name) => document.querySelector(`#FilterModal [name="${name}"]`);

const ShowFilterPopup = (filterBtn, filterItemBlackList = []) => {
    if (!filterBtn) return;
    
    const filterCallBack = filterBtn.getAttribute("data-filterCallBack");
    const ContentFilterDiv = filterBtn.getAttribute("data-filterContentId");
    const txtSercheId = filterBtn.getAttribute("data-filterInpSearchId");
    const varSearch = filterBtn.getAttribute("data-filterVarSearch");
    
    filterVarConfig = filterBtn.getAttribute("data-filterVarConfig");
    
    if (!filterVarConfig) {
        console.error("Filter config not found.");
        return;
    }
    
    const selLabelElm = getFilterElement("SelFilterLabel");
    const selChkElm = getFilterElement("SelFilterChd");
    const cusChkElm = getFilterElement("CusFilterChd");
    const dateChkElm = getFilterElement("DateFilterChk");
    
    // Customer
    if (cusChkElm) {
        GetCustomersOpId("customerFilter", null, "#FilterFormId");
    }
    
    // Seller
    if (selLabelElm && selChkElm) {
        if (filterVarConfig === "cashConfigFilter") {
            selLabelElm.textContent = "کارشناس";
            GetSelsOpId("selFilter", null, "#FilterFormId", 1);
        } else {
            selLabelElm.textContent = "کارشناس فروش";
            GetSelsOpId("selFilter", null, "#FilterFormId");
        }
    }
    
    // Blacklist
    filterItemBlackList.forEach(id => {
        const elm = getFilterElement(id);
        if (elm) {
            elm.style.display = "none";
        }
    });
    
    // Config
    const currentConfig = window[filterVarConfig];
    
    if (!currentConfig) {
        console.warn(`Filter config "${filterVarConfig}" not found.`);
        return;
    }
    
    window[filterVarConfig] = {
        ...currentConfig,
        filterVar: {
            ...(currentConfig.filterVar || {}),
        },
        filterCallBack,
        ContentFilterDiv,
        txtSercheId,
        varSearch,
        filterVarConfig,
    };
    
    const list = window[filterVarConfig].filterVar;
    
    // Customer filter
    if (cusChkElm) {
        const customerFilter = getFilterElement("customerFilter");
        
        if (list?.CusFilterChd) {
            cusChkElm.checked = true;
            
            filterToggleDisable(true, "CusFilterDivContent");
            
            if (customerFilter) {
                $("#customerFilter").val(list.customerFilter).trigger("change");
            }
        } else {
            cusChkElm.checked = false;
            
            if (customerFilter) {
                $("#customerFilter").val(null).trigger("change");
            }
            
            filterToggleDisable(false, "CusFilterDivContent");
        }
    }
    
    // Seller filter
    if (selChkElm) {
        const selFilter = getFilterElement("selFilter");
        
        if (list?.SelFilterChd) {
            selChkElm.checked = true;
            
            filterToggleDisable(true, "SelFilterDivContent");
            
            if (selFilter) {
                $("#selFilter").val(list.selFilter).trigger("change");
            }
        } else {
            selChkElm.checked = false;
            
            if (selFilter) {
                $("#selFilter").val(null).trigger("change");
            }
            
            filterToggleDisable(false, "SelFilterDivContent");
        }
    }
    
    // Date filter
    if (dateChkElm) {
        const toDate = getFilterInput("toDate");
        const fromDate = getFilterInput("fromDate");
        
        if (list?.DateFilterChk) {
            dateChkElm.checked = true;
            
            filterToggleDisable(true, "DateFilterDivContent");
            filterToggleDisable(true, "dateFilterBtnId");
            
            if (toDate) {
                toDate.value = list.toDate ?? "";
            }
            
            if (fromDate) {
                fromDate.value = list.fromDate ?? "";
            }
        } else {
            dateChkElm.checked = false;
            
            filterToggleDisable(false, "DateFilterDivContent");
            filterToggleDisable(false, "dateFilterBtnId");
            
            if (toDate) {
                toDate.value = "";
            }
            
            if (fromDate) {
                fromDate.value = "";
            }
        }
    }
    
    // Search value
    window[filterVarConfig].varSearch = document.getElementById(window[filterVarConfig].txtSercheId)?.value ?? "";
    
    ModalToggle("FilterModal");
};


const FilterAccepted = async (filConfig = null) => {
    try {
        if (filConfig) {
            filterVarConfig = filConfig.filterVarConfig;
            if (!filterVarConfig) {
                console.warn("Filter config not found.");
                return;
            }
            
            window[filterVarConfig] = filConfig;
            showFilterContent(window[filterVarConfig].filterVar);
            await executeFilterCallback(window[filterVarConfig].filterCallBack);
            return;
        }
        
        if (!await mySer.CheckValidForm("#FilterFormId")) return;
        
        const data = getFilterData();
        
        // Save filter
        if (!filterVarConfig || !window[filterVarConfig]) {
            console.warn("Filter config is not available.");
            return;
        }
        
        window[filterVarConfig].filterVar = data;
        
        showFilterContent(data);
        
        // اگر هیچ فیلتری فعال نیست
        if (!data.SelFilterChd && !data.DateFilterChk && !data.CusFilterChd) {
            window[filterVarConfig].filterVar = null;
        }
        
        // Callback
        await executeFilterCallback(window[filterVarConfig].filterCallBack);
        
        ModalToggle("FilterModal");
        
    } catch (error) {
        console.error("FilterAccepted error:", error);
    }
};

const getFilterData = () => {
    const data = mySer.convertFormToJSON("#FilterFormId");
    
    const cusChk = getFilterElement("CusFilterChd");
    const selChk = getFilterElement("SelFilterChd");
    const dateChk = getFilterElement("DateFilterChk");
    
    data.CusFilterChd = cusChk?.checked ?? false;
    data.SelFilterChd = selChk?.checked ?? false;
    data.DateFilterChk = dateChk?.checked ?? false;
    
    const customer = getFilterElement("customerFilter");
    const seller = getFilterElement("selFilter");
    
    if (data.CusFilterChd && customer) {
        data.customerFilterTxt = customer.options[customer.selectedIndex]?.text ?? "";
    }
    
    if (data.SelFilterChd && seller) {
        data.selFilterTxt = seller.options[seller.selectedIndex]?.text ?? "";
    }
    
    return data;
};

const executeFilterCallback = async (callbackName) => {
    if (!callbackName) return;
    
    try {
        const callback = eval(callbackName);
        
        if (typeof callback === "function") {
            await callback();
        }
    } catch (error) {
        console.error(`Filter callback "${callbackName}" could not be executed:`, error);
    }
};

const showFilterContent = (model) => {
    const ContentFilterDivId = window[filterVarConfig].ContentFilterDiv;
    const varConfig = window[filterVarConfig].filterVarConfig;
    
    
    if (ContentFilterDivId !== "ContentDashboardFilterId") {
        let htm = `<div class="d-flex align-items-center flex-wrap gap-1">`;
        if (model.CusFilterChd && model.customerFilterTxt) {
            htm += `<p class="ContentFilterItem" data-type="Cus" ><i class="delFilter fas fa-times" onclick="delFilterContent(this, ${varConfig} )" ></i>  <span>${model.customerFilterTxt}</span></p>`;
        }
        
        if (model.DateFilterChk && (model.fromDate || model.toDate)) {
            htm += `<p class="ContentFilterItem" data-type="Date"><i class="delFilter fas fa-times" onclick="delFilterContent(this, ${varConfig} )" ></i> <span>از ${model.fromDate ? model.fromDate : "ابتدا"} تا ${model.toDate ? model.toDate : "امروز"}</span></p>`;
        }
        
        if (model.SelFilterChd && model.selFilterTxt) {
            htm += `<p class="ContentFilterItem" data-type="Sel"><i class="delFilter fas fa-times" onclick="delFilterContent(this, ${varConfig} )" ></i> <span>${model.selFilterTxt}</span></p>`;
        }
        document.getElementById(ContentFilterDivId).innerHTML = htm + `</div>`;
    } else {
        let htm = `<div class="d-flex align-items-center flex-wrap gap-2">`;
        
        if (model.CusFilterChd && model.customerFilterTxt) {
            htm += `<p class="dashGridItem fbold gap-2" style="padding: 5px 14px;color: #333;border-radius: 4px;" data-type="Cus"><i class="fas fa-times" style="cursor: pointer" onclick="delFilterContent(this, ${varConfig})"></i> <span>${model.customerFilterTxt}</span></p>`;
        }
        if (model.DateFilterChk && (model.fromDate || model.toDate)) {
            if (currentDateTypeFilter === 1) {
                htm += `<p class="dashGridItem fbold gap-2" style="padding: 5px 14px;color: #333;border-radius: 4px;" data-type="Date"><i class="fas fa-times" style="cursor: pointer" onclick="delFilterContent(this, ${varConfig})"></i> <span>سال <span>${currentYear}</span></span></p>`;
            } else if (currentDateTypeFilter === 2 && currentMonthArr[0] === model.fromDate && currentMonthArr[1] === model.toDate) {
                htm += `<p class="dashGridItem fbold gap-2" style="padding: 5px 14px;color: #333;border-radius: 4px;" data-type="Date"><span>${currentMonthArr[2]} ماه ${currentMonthArr[3]}</span></p>`;
            } else if (currentDateTypeFilter === 3) {
                htm += `<p class="dashGridItem fbold gap-2" style="padding: 5px 14px;color: #333;border-radius: 4px;" data-type="Date"><i class="fas fa-times" style="cursor: pointer" onclick="delFilterContent(this, ${varConfig})"></i> <span>هفته جاری</span></p>`;
            } else if (currentDateTypeFilter === 4) {
                htm += `<p class="dashGridItem fbold gap-2" style="padding: 5px 14px;color: #333;border-radius: 4px;" data-type="Date"><i class="fas fa-times" style="cursor: pointer" onclick="delFilterContent(this, ${varConfig})"></i> <span>روز جاری</span></p>`;
            } else {
                htm += `<p class="dashGridItem fbold gap-2" style="padding: 5px 14px;color: #333;border-radius: 4px;" data-type="Date"><i class="fas fa-times" style="cursor: pointer" onclick="delFilterContent(this, ${varConfig})"></i> <span>از ${model.fromDate || "ابتدا"} تا ${model.toDate || "امروز"}</span></p>`;
            }
        }
        
        if (model.SelFilterChd && model.selFilterTxt) {
            htm += `<p class="dashGridItem fbold gap-2" style="padding: 5px 14px;color: #333;border-radius: 4px;" data-type="Sel"><i class="fas fa-times" style="cursor: pointer" onclick="delFilterContent(this, ${varConfig})"></i> <span>${model.selFilterTxt}</span></p>`;
        }
        
        document.getElementById(ContentFilterDivId).innerHTML = htm;
    }
    
};

const delFilterContent = async (event, varConfig) => {
    const type = event.parentElement.getAttribute("data-type");
    filterVarConfig = varConfig.filterVarConfig;
    
    if (type === "Cus") {
        window[filterVarConfig].filterVar.CusFilterChd = false;
        window[filterVarConfig].filterVar.customerFilter = null;
        window[filterVarConfig].filterVar.customerFilterTxt = "";
    }
    if (type === "Date") {
        if (window[filterVarConfig].ContentFilterDiv !== "ContentDashboardFilterId") {
            window[filterVarConfig].filterVar.DateFilterChk = false;
            window[filterVarConfig].filterVar.toDate = "";
            window[filterVarConfig].filterVar.fromDate = "";
            document.querySelectorAll("#dateFilterBtnId button").forEach(el => el.classList.remove("bgGreen2"));
        } else {
            window[filterVarConfig].filterVar.DateFilterChk = true;
            window[filterVarConfig].filterVar.toDate = currentMonthArr[1];
            window[filterVarConfig].filterVar.fromDate = currentMonthArr[0];
            currentDateTypeFilter = 2;
            document.querySelectorAll("#dateFilterBtnId button").forEach(el => el.classList.remove("bgGreen2"));
            document.querySelector("#dateFilterBtnId .monthBtn").classList.add("bgGreen2");
        }
    }
    if (type === "Sel") {
        window[filterVarConfig].filterVar.SelFilterChd = false;
        window[filterVarConfig].filterVar.selFilter = null;
        window[filterVarConfig].filterVar.selFilterTxt = "";
    }
    
    showFilterContent(window[filterVarConfig].filterVar);
    if (!window[filterVarConfig].filterVar.SelFilterChd && !window[filterVarConfig].filterVar.DateFilterChk && !window[filterVarConfig].filterVar.CusFilterChd) {
        window[filterVarConfig].filterVar = null;
    }
    const callBack = eval(window[filterVarConfig].filterCallBack);
    if (callBack) await callBack();
};

const filterToggleDisable = (_val, contentId) => {
    const divCon = document.getElementById(contentId);
    
    if (_val) {
        divCon.classList.remove("disableDiv");
    } else {
        divCon.classList.add("disableDiv");
    }
};

const getDateFilter = async (type, btn) => {
    const filterInputs = document.querySelectorAll("#DateFilterDivContent input");
    toggleElementsDisabled(filterInputs);
    toggleElementsDisabled(btn);
    let currentDate =
        type === 1 ? await myDate.getCurrentYear() :
            type === 2 ? await myDate.getCurrentMonth() :
                type === 3 ? await myDate.getCurrentWeek() :
                    await myDate.getTodayDate();
    
    
    toggleElementsDisabled(filterInputs, false);
    toggleElementsDisabled(btn, false);
    document.querySelectorAll("#dateFilterBtnId button").forEach(el => el.classList.remove("bgGreen2"));
    btn.classList.add("bgGreen2");
    currentDateTypeFilter = type;
    if (type === 1) {
        currentYear = currentDate[2];
    }
    // }else if (type === 2) {
    //     currentMonth = currentDate;
    //     currentYear = '';
    // } else {
    //     currentMonth = '';
    //     currentYear = '';
    // }
    document.querySelector("#DateFilterDivContent input[name='fromDate']").value = currentDate instanceof Array ? currentDate[0] : currentDate;
    document.querySelector("#DateFilterDivContent input[name='toDate']").value = currentDate instanceof Array ? currentDate[1] : currentDate;
};


window.onSelectFilterDate = function () {
    currentDateTypeFilter = null;
    // currentYear = '';
    // currentMonth = '';
};


const PayFormChange = (modalId, type) => {
    const modal = document.getElementById(modalId);
    const selectedType = Number(type);
    
    const elements = {
        transactionTitle: modal.querySelector(".transactionBankBox .sectionTitle-invoice"),
        
        vosolDateDiv: modal.querySelector(".VosolDateDiv"),
        vosolLabel: modal.querySelector(".VosolDateDiv label"),
        vosolInput: modal.querySelector(".VosolDateDiv input"),
        
        amountWrapper: modal.querySelector(".transactionAmountWrapper"),
        amountInput: modal.querySelector("input[name='reciveAmount']"),
        
        receiveDateDiv: modal.querySelector(".ReceiveDateDiv"),
        receiveDateInput: modal.querySelector("input[name='reciveDate']"),
        
        beneficiaryWrapper: modal.querySelector(".beneficiaryWrapper"),
        beneficiaryInput: modal.querySelector(".beneficiaryWrapper input"),
        
        toBankSelectWrapper: modal.querySelector(".toBankAccountWrapperSelect"),
        toBankSelect: modal.querySelector(".toBankAccountWrapperSelect select"),
        toBankTxtWrapper: modal.querySelector(".toBankAccountWrapperTxt"),
        toBankTxtInput: modal.querySelector(".toBankAccountWrapperTxt input"),
        
        toNameWrapper: modal.querySelector(".toNameWrapper"),
        toNameLabel: modal.querySelector(".toNameWrapper label"),
        toNameInput: modal.querySelector(".toNameWrapper input"),
        
        sanadPayBox: modal.querySelector(".sanadPayBox"),
        uploaderSpan: modal.querySelector(".sanadPayBox .container-uploader span"),
    };
    
    const config = {
        1: { // نقدی
            transactionBankTitle: "اطلاعات تراکنش بانکی",
            vosolText: "تاریخ پرداخت",
            vosolDateVisible: true,
            amountVisible: true,
            receiveVisible: false,
            beneficiaryVisible: false,
            
            toSelectVisible: true,
            toTxtVisible: false,
            toNameVisible: false,
            
            sanadPayVisible: true,
            uploaderText: "تصویر رسید را انتخاب کنید یا اینجا رها کنید",
        },
        2: { // چکی
            transactionBankTitle: "اطلاعات تراکنش بانکی",
            vosolText: "تاریخ وصول",
            vosolDateVisible: true,
            amountVisible: true,
            receiveVisible: true,
            beneficiaryVisible: false,
            
            toSelectVisible: false,
            toTxtVisible: false,
            toNameVisible: false,
            
            sanadPayVisible: true,
            uploaderText: "تصویر چک را انتخاب کنید یا اینجا رها کنید",
        },
        3: { // اعتباری
            transactionBankTitle: "اطلاعات تراکنش بانکی",
            vosolText: "تاریخ وصول",
            vosolDateVisible: true,
            amountVisible: true,
            receiveVisible: false,
            beneficiaryVisible: false,
            
            toSelectVisible: false,
            toTxtVisible: false,
            toNameVisible: false,
            
            sanadPayVisible: true,
            uploaderText: "تصویر رسید را انتخاب کنید یا اینجا رها کنید",
        },
        4: { // تهاتری
            transactionBankTitle: "اطلاعات تراکنش بانکی",
            vosolText: "تاریخ پرداخت",
            vosolDateVisible: true,
            amountVisible: true,
            receiveVisible: false,
            beneficiaryVisible: true,
            
            toSelectVisible: false,
            toTxtVisible: true,
            toNameVisible: true,
            toNameLabelVisible: true,
            toNamePlaceholder: "نام صاحب حساب واریز شده",
            
            sanadPayVisible: true,
            uploaderText: "تصویر رسید را انتخاب کنید یا اینجا رها کنید",
        },
        6: { // انتقال
            transactionBankTitle: "انتقال به",
            vosolDateVisible: false,
            amountVisible: false,
            receiveVisible: false,
            beneficiaryVisible: true,
            
            toSelectVisible: false,
            toTxtVisible: false,
            toNameVisible: true,
            toNameLabelVisible: false,
            toNamePlaceholder: "انتقال به",
            
            sanadPayVisible: false,
        },
    };
    
    const current = config[selectedType];
    if (!current) return;
    
    elements.transactionTitle.textContent = current.transactionBankTitle || "";
    
    elements.vosolDateDiv.classList.toggle("d-none", !current.vosolDateVisible);
    elements.vosolInput.toggleAttribute("required", current.vosolDateVisible);
    elements.vosolLabel.textContent = current.vosolText || "";
    elements.vosolInput.placeholder = current.vosolText || "";
    
    elements.amountWrapper.classList.toggle("d-none", !current.amountVisible);
    elements.amountInput.toggleAttribute("required", current.amountVisible);
    
    elements.receiveDateDiv.classList.toggle("d-none", !current.receiveVisible);
    elements.receiveDateInput.toggleAttribute("required", current.receiveVisible);
    
    elements.beneficiaryWrapper.classList.toggle("d-none", !current.beneficiaryVisible);
    elements.beneficiaryInput.toggleAttribute("required", current.beneficiaryVisible);
    
    elements.toBankSelectWrapper.classList.toggle("d-none", !current.toSelectVisible);
    elements.toBankSelect.toggleAttribute("required", current.toSelectVisible);
    
    elements.toBankTxtWrapper.classList.toggle("d-none", !current.toTxtVisible);
    elements.toBankTxtInput.toggleAttribute("required", current.toTxtVisible);
    
    elements.toNameWrapper.classList.toggle("d-none", !current.toNameVisible);
    elements.toNameLabel.classList.toggle("d-none", !current.toNameLabelVisible);
    elements.toNameInput.toggleAttribute("required", current.toNameVisible);
    elements.toNameInput.placeholder = current.toNamePlaceholder || "";
    
    elements.sanadPayBox.classList.toggle("d-none", !current.sanadPayVisible);
    
    if (elements.uploaderSpan)
        elements.uploaderSpan.textContent = current.uploaderText;
};


const PayAttachFormChange = (type) => {
    switch (type) {
        case 1:
            document.getElementById("PayAttachFormId").classList.add("d-none");
            document.querySelector("#AttachInvoiceModalId .container-uploader").classList.remove("d-none");
            document.querySelector("#AttachInvoiceModalId .container-uploader span").textContent = "تصویر رسید را انتخاب کنید یا اینجا رها کنید";
            break;
        case 2:
            document.getElementById("PayAttachFormId").classList.add("d-none");
            document.querySelector("#AttachInvoiceModalId .container-uploader").classList.remove("d-none");
            document.querySelector("#AttachInvoiceModalId .container-uploader span").textContent = "تصویر چک را انتخاب کنید یا اینجا رها کنید";
            break;
        case 3:
            document.querySelector("#AttachInvoiceModalId .container-uploader").classList.add("d-none");
            document.getElementById("PayAttachFormId").classList.remove("d-none");
            document.getElementById("PayAttachFormId").classList.remove("d-none");
            break;
    }
};

const AcceptSetting = async () => {
    if (setting == null) {
        setting = await GetSetting();
    }
    
    if (setting.smallOffice) {
        //document.getElementById("divReciveAmountId").style.display = "flex";
        document.getElementById("PfPrintableAlertId").innerHTML = "";
    }
    
    if (setting.setAmountByRoleType === 2) {
        document.querySelectorAll(".unitAmountBoxPrefactorModal")?.forEach(elm => elm.style.display = "none");
        document.querySelectorAll(".totalAmountBoxPrefactorModal")?.forEach(elm => elm.style.display = "none");
        document.querySelectorAll(".unitAmountBoxPrefactorModal input[name='unitAmount']")?.forEach(elm => elm.removeAttribute("required"));
        const PreFacItem = document.getElementById("PreFacItem");
        if (PreFacItem) PreFacItem.style.gridTemplateColumns = "2.1fr 3.2fr 1fr 1fr 0.4fr";
    }
};


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
    document.getElementById("AddFromPreFactor").innerHTML = generateItemPfactorDom(pfItemsNotInOrders, "select");
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


/*const CreateOrder = async (PfId, CuId, type) => {
sessionStorage.setItem("CuId", CuId);
sessionStorage.setItem("PfId", PfId);
sessionStorage.setItem("OfficialType", type);
window.open(`/Order/Index`);
};*/


const GetSetting = async () => {
    return new Promise((resolve) => {
        $.get("/ASetting/GetSetting", (res) => {
            setting = res;
            document.dispatchEvent(new CustomEvent("settingReady"));
            resolve(res);
        })
         .fail(async () => {
             setTimeout(async () => await GetSetting(), 3000);
         });
    });
};


const setDateTime = async () => {
    const el = document.getElementById("dateTimeNowId");
    if (!el) return;
    var dt = await getTodayDateTime();
    var time = dt.split(" ");
    // el.innerHTML = `<span>${time[1]}</span> <span>${time[0]}</span> <span>ساعت:</span> <span>${time[2]}</span>`;
    el.innerHTML = `${dt}`;
    
    setInterval(async () => {
        dt = await getTodayDateTime();
        time = dt.split(" ");
        // el.innerHTML = `<span>${time[1]}</span> <span>${time[0]}</span> <span>ساعت:</span> <span>${time[2]}</span>`;
        el.innerHTML = `${dt}`;
        
    }, 30000);
};


const delPayment = (id) => {
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
                FLshowLoading();
                await delPaymentSubmit(id);
                await refreshInvoiceSuccess();
                FLshowLoading(false);
            }
        });
    
};

const delPaymentSubmit = async (id) => {
    return new Promise(async (resolve) => {
        const model = { "Id": id };
        
        $.post(baseUrl + "Accountancy/delAccountancy", model, () => {
            toastifySuccess("با موفقیت حذف شد");
            resolve();
        })
         .fail(() => {
             setTimeout(() => delPaymentSubmit(id), 3000);
         });
    });
};


const initPaymentPartial = (type) => {
    const isMobile = window.innerWidth < 767;
    $(`#allPaymentTable${type}`).DataTable({
        paging: false,
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        responsive: true,
        ordering: false,
        searching: false,
        language: languageDatatable,
        columnDefs: [
            { targets: "_all", render: data => typeof data === "string" ? (data.trim() || "-") : (data ?? "-") },
        ],
        columns: type === 55 || type === 56
            ? [
                { responsivePriority: 4 },
                { responsivePriority: 9 },
                { responsivePriority: 1 },
                { responsivePriority: 3 },
                { responsivePriority: 5 },
                { responsivePriority: 2 },
                { responsivePriority: 7 },
                { responsivePriority: 8 },
                { responsivePriority: isMobile ? 9 : 1 },
                { responsivePriority: isMobile ? 10 : 1 },
            ] :
            [
                { responsivePriority: 7 },
                { responsivePriority: 6 },
                { responsivePriority: 3 },
                { responsivePriority: 4 },
                { responsivePriority: 5 },
                { responsivePriority: 8 },
                { responsivePriority: 2 },
                { responsivePriority: 1 },
            ],
        layout: null,
    });
};

const initWalletPartial = () => {
    $(`#allWalletTable`).DataTable({
        paging: false,
        scrollCollapse: true,
        scrollY: "calc(100dvh - 400px)",
        responsive: true,
        ordering: false,
        searching: false,
        language: languageDatatable,
        columns:
            [
                { responsivePriority: 6 },
                { responsivePriority: 7 },
                { responsivePriority: 3 },
                { responsivePriority: 2 },
                { responsivePriority: 4 },
                { responsivePriority: 8 },
                { responsivePriority: 1 },
                { responsivePriority: 5 },
            ],
        layout: null,
    });
};

const RefreshDataInvoice = async (id = Invo_id, type = Invo_type) => {
    if (!id) return;
    FLshowLoading();
    
    const [amountData, paymentData] = await Promise.all([
        getAllAmountPartial(id, type),
        getAllPaymentPartial(id, type, invoicePassType),
    ]);
    
    document.getElementById("PayContentId").innerHTML = paymentData.html;
    document.getElementById("PayContentSumId").innerHTML = amountData;
    
    initPaymentPartial(invoicePassType);
    
    customerId = paymentData.data.customerId;
    document.getElementById("changeStatusAllAccBtn")?.classList.toggle("d-none", !paymentData.data.showChangeStatusAllBtn);
    document.getElementById("acceptFinancialBtn")?.classList.toggle("d-none", !paymentData.data.showAcceptPfBtn);
    FLshowLoading(false);
};

const getAllAmountPartial = async (id, type) => { //type{1=prefactor 2=Factor 3=deal 4=customer}
    const res = await axios(`/Accountancy/GetAllAmountPartial?type=${type}&id=${id}`);
    return res.data;
};

const getAllPaymentPartial = async (id, type, invoicePassType) => {
    const res = await axios(`/Accountancy/GetAllPaymentPartial?type=${type}&id=${id}&passType=${invoicePassType}`);
    return res.data;
};


const ShowModalInvoiceWallet = async () => {
    await ShowModalListInvoice(customerId, 4, null, 1, true);
};


const cancelModalWallet = async () => {
    ModalToggle("#WalletListModalId", "hide");
    await ShowModalInvoice(1);
};


const getListWalletModal = async () => {
    try {
        FLshowLoading();
        const res = await axios(`/Accountancy/GetWalletPartialIfAny?cusId=${customerId}`);
        if (!res.data) return false;
        
        document.getElementById("WalletContentId").innerHTML = res.data;
        initWalletPartial();
        ModalToggle("#WalletListModalId");
        return true;
    } catch (err) {
        console.log(err);
    } finally {
        FLshowLoading(false);
    }
};

const addInvoiceWalletToPf = async (id) => {
    try {
        FLshowLoading();
        const res = await axios(`/Accountancy/AddInvoiceWalletToPf?id=${id}&pfId=${pfactorID}`);
        if (res.status === 200) {
            await refreshInvoiceSuccess();
            FLshowLoading(false);
            ModalToggle("#WalletListModalId", "hide");
        }
    } catch (err) {
        console.log(err);
    }
};

let isEditInvoiceModal = false;
let invoiceItemId;
const ShowVosolInvoiceModal = async (parentId, pfId, invoiceType) => {
    AccParentId = parentId;
    InvoiceType = invoiceType;
    pfactorID = pfId;
    
    document.getElementById("TransferRadioWrapper")?.classList.remove("d-none");
    document.getElementById("DivEtebariId")?.classList.add("d-none");
    
    await ShowModalInvoice(1);
};

const ShowModalListInvoice = async (id, type, ser, passType, hideAddInvoiceBtns = false) => {
    invoicePassType = passType;
    
    Invo_id = id;
    Invo_type = type;
    
    await RefreshDataInvoice(id, type);
    if (type === 1) { //PreFactor
        pfactorID = id;
        InvoiceType = 1;
        document.getElementById("TitleListInvoiceId").innerText = "صورت‌حساب های پیش‌فاکتور " + ser;
    } else if (type === 2) { //Factor
        factorID = id;
        InvoiceType = 2;
        document.getElementById("TitleListInvoiceId").innerText = "واریزی های فاکتور شماره " + ser;
    } else if (type === 3) { //Deal
        InvoiceType = 1;
    } else if (type === 4) { //Wallet
        InvoiceType = 1;
        document.getElementById("TitleListInvoiceId").innerText = "کیف پول";
    }
    
    document.getElementById("addInvoiceBtns")?.classList.toggle("d-none", hideAddInvoiceBtns); // نمایش یا عدم نمایش دکمه های واریزی و برگشتی
    /*if (!hideAddInvoiceBtns) {
        document.querySelector("#addInvoiceBtns .deposit-btn")?.classList.toggle("d-none", passType === 5); // کارتابل مالی دکمه واریزی نمایش داده نشود فقط برگشتی نمایش داده شود
    }*/
    
    ModalToggle("#InvoiceListModalId", "show");
};


const ShowModalAddInvoice = async () => {
    const hasWallet = await getListWalletModal();
    if (hasWallet) return;
    await ShowModalInvoice(1);
};


const ShowModalInvoice = async (acType = 1) => {
    AccType = acType;
    await getAllBankAccounts([{ element: "#InvoiceModalId select[name='bankAccountId']", type: [1] }]);
    
    const invoiceModalElm = document.getElementById("InvoiceModalId");
    if (AccType === 1) {
        invoiceModalElm.querySelector(".modalHeader-invoice").innerText = InvoiceType === 1 ? "واریز پیش پرداخت" : "ثبت واریز";
        invoiceModalElm.classList.add("deposit-mode");
        invoiceModalElm.classList.remove("backward-mode");
    } else if (AccType === 2) {
        invoiceModalElm.querySelector(".modalHeader-invoice").innerText = "برگشت مبلغ";
        invoiceModalElm.classList.remove("deposit-mode");
        invoiceModalElm.classList.add("backward-mode");
        invoiceModalElm.querySelector("#DivEtebariId")?.classList.add("d-none");
    }
    
    document.querySelector("#InvoiceModalId input[name='reciveDate']").value = await todayDate();
    
    ModalToggle(invoiceModalElm);
};

const ShowEditModalInvoice = async (id) => {
    FLshowLoading();
    const res = await axios(`/Accountancy/getAccountancy/${id}`);
    const data = res.data.accData;
    
    isEditInvoiceModal = true;
    invoiceItemId = id;
    InvoiceType = data.invoiceType;
    
    if (InvoiceType === 1) {
        pfactorID = data.pfid;
    } else if (InvoiceType === 2) {
        factorID = data.fid;
    }
    
    if (data.parentId) {
        await ShowVosolInvoiceModal(data.parentId, data.pfid, data.invoiceType);
    } else {
        await ShowModalInvoice(data.accType);
    }
    
    data.reciveAmount = data.accType === 2 ? data.backWard : data.credit;
    mySer.convertJsonToForm(data, "PayFactorFormId");
    
    PayFormChange("InvoiceModalId", data.payType);
    
    document.getElementById("DivShowSanadPar").innerHTML = "";
    SanadPayList = res.data.listAttach;
    FileUpReload("upSanadPay");
    FLshowLoading(false);
};


async function showPayment(id, parent = false) {
    try {
        FLshowLoading();
        await getAllBankAccounts([{ element: "#ShowPaymentModal select[name='bankAccountId']", type: [1], isDisable: true }]);
        const res = (await axios(`/Accountancy/getAccountancy/${id}?parent=${parent}`)).data;
        let data = res.accData;
        
        const paymentModalElm = document.getElementById("ShowPaymentModal");
        if (data.accType === 1) {
            paymentModalElm.classList.add("deposit-mode");
            paymentModalElm.classList.remove("backward-mode");
            paymentModalElm.querySelector(".modalHeader-invoice").innerText = "واریزی " + data.payTypeTxt;
        } else if (data.accType === 2) {
            paymentModalElm.classList.add("backward-mode");
            paymentModalElm.classList.remove("deposit-mode");
            paymentModalElm.querySelector(".modalHeader-invoice").innerText = "برگشتی " + data.payTypeTxt;
        }
        
        PayFormChange("ShowPaymentModal", data.payType);
        
        data.reciveAmount = data.accType === 2 ? data.backWard : data.credit;
        mySer.convertJsonToForm(data, "ShowPaymentFormId");
        
        const attachList = res.listAttach;
        
        const containerUploader = paymentModalElm.querySelector(".sanadPayBox");
        const divShow = containerUploader.querySelector(".image-display");
        divShow.innerHTML = "";
        if (attachList.length) {
            containerUploader.classList.remove("d-none");
            attachList.forEach(fileItem => {
                const imageBox = createImageBox(fileItem);
                divShow.appendChild(imageBox);
            });
        } else {
            containerUploader.classList.add("d-none");
        }
        ModalToggle(paymentModalElm);
        
    } catch (err) {
        if (err.status === 404) {
            toastifyError(`خطا: ${err.response.data}`);
        }
        console.log(err);
    }
    FLshowLoading(false);
}

async function checkEtebariWarning() {
    if (isEditInvoiceModal || AccParentId || AccType !== 1) {
        return true;
    }
    
    const res = await axios(`/Factor/HasAnyEtebari?pfId=${pfactorID}`);
    
    if (res.status !== 201) {
        return true;
    }
    
    const result = await Swal.fire({
        title: "هشدار!",
        text: "برای این پیش‌فاکتور قبلا یک واریزی اعتباری ثبت شده است. اگر این واریزی مربوط به آن مورد اعتباری نیست، تایید کنید تا ثبت شود.",
        icon: "warning",
        showCancelButton: true,
        cancelButtonColor: "#adadad",
        confirmButtonColor: "#0d6efd",
        cancelButtonText: "انصراف",
        confirmButtonText: "تایید",
    });
    
    return result.isConfirmed;
}


const submitInvoice = async () => {
    if (await mySer.CheckValidForm("PayFactorFormId")) {
        const accData = mySer.convertFormToJSON("PayFactorFormId");
        if (setting.attPfPayment && (accData.payType !== 3 && accData.payType !== 6) && SanadPayList.every(item => item.isDeleted)) {
            toastifyError("سند واریزی اجباریست");
            document.getElementById("SanadParId").classList.add("AlertReqRadio");
            return;
        }
        
        if (accData.payType !== 3 && !await checkEtebariWarning()) {
            return;
        }
        
        if (isEditInvoiceModal) accData.id = invoiceItemId;
        
        accData.invoiceType = InvoiceType ?? 1;
        accData.accType = AccType;
        accData.parentId = AccParentId;
        accData.customerId = customerId;
        
        if (InvoiceType === 1) {
            accData.pfid = pfactorID;
        } else if (InvoiceType === 2) {
            accData.fid = factorID;
        }
        
        if (accData.payType === 1) {
            accData.bankAccountTxt = document.querySelector("#PayFactorFormId [name='bankAccountId']").selectedOptions[0].innerText;
        } else if (accData.payType === 4) {
            accData.bankAccountId = null;
        } else {
            accData.bankAccountId = null;
            accData.bankAccountTxt = null;
        }
        
        if (accData.payType !== 4 && accData.payType !== 6) {
            accData.beneficiary = null;
            accData.toName = null;
        }
        
        try {
            FLshowLoading();
            const res = await axios.post(`/Factor/AddPayAttachment`, { accData, listAttach: SanadPayList });
            if (res.status === 200) {
                await refreshInvoiceSuccess();
                toastifySuccess(isEditInvoiceModal ? "ویرایش انجام شد" : "ثبت انجام شد");
                closeModalInvoice();
                
                const pfListModal = document.getElementById("preFactorListModal");
                if (isModalOpen(pfListModal)) {
                    ModalToggle(pfListModal, "hide");
                }
            } else {
                toastifyError(res.error);
            }
            FLshowLoading(false);
        } catch (err) {
            console.log(err);
            const message = err.response.data;
            if (err.status === 409) {
                toastifyError(message);
                FLshowLoading(false);
                return;
            }
            setTimeout(async () => await submitInvoice(), 3000);
        }
    }
};


async function refreshInvoiceSuccess() {
    await RefreshDataInvoice();
    
    const currentPath = window.location.pathname.toLowerCase();
    if (currentPath.includes("customer")) { //1 پروفایل مشتری
        await getAllPreFactor();
        getAllDetailAmount();
        
    } else if (currentPath.includes("deal")) { //4 سفارشات
        await refreshDealPreFactors();
        
    } else if (currentPath.includes("check")) { //چک ها
        await getReceivedChecks();
        
    } else if (currentPath.includes("cartable")) { // کارتابل
        const { tab, sub } = getUrlParams();
        switch (tab) {
            case "TPrefactorFactorId": //2 پیش‌فاکتور و فاکتور
                await GetPreFactorPartial();
                break;
            case "TFinancialExpertId": //5 مالی
                switch (sub) {
                    case "FinancialPrefactorTab":
                        await GetPreFactorFinancialExpert();
                        break;
                    case "FinancialInvoiceTab":
                        await GetInvoicesFinancialExpert();
                        break;
                }
                break;
            case "TTaminExpertId": //5 تامین
                await GetInvoicesTaminExpert();
                break;
            case "TSalesExpertId": //3 کارشناس فروش
                await GetSelesExpertPartial();
                break;
        }
    }
}


async function changeStatusAllAcc() {
    try {
        FLshowLoading();
        const res = await axios(`/Accountancy/ChangeStatusAllAcc?pfId=${pfactorID}`);
        if (res.status === 200) {
            await refreshInvoiceSuccess();
            FLshowLoading(false);
            toastifySuccess("به مالی ارسال شد.");
            $("#InvoiceListModalId").modal("toggle");
        }
    } catch (err) {
        console.log(err);
    }
}

async function changeStatusAcc(id) {
    try {
        FLshowLoading();
        const res = await axios(`/Accountancy/ChangeStatusAcc/${id}`);
        if (res.status === 200) {
            await refreshInvoiceSuccess();
            FLshowLoading(false);
            toastifySuccess("به مالی ارسال شد.");
        }
    } catch (err) {
        console.log(err);
    }
}

document.getElementById("acceptFinancialBtn")?.addEventListener("click", async () => {
    await acceptFinancial(pfactorID);
    $("#InvoiceListModalId").modal("toggle");
});


async function acceptFinancial(pfId) {
    try {
        FLshowLoading();
        const res = await axios(`/Factor/AcceptFinancial/${pfId}`);
        if (res.status === 200) {
            await GetPreFactorFinancialExpert();
            Swal.fire({ icon: "success", title: "پیش‌فاکتور تایید شد.", confirmButtonText: "باشه", confirmButtonColor: "#0d6efd" });
        } else {
            toastifyError(res.data);
        }
        FLshowLoading(false);
    } catch (err) {
        console.log(err);
        setTimeout(async () => await acceptFinancial(pfId), 3000);
    }
}

let idNotAccept;
let typeNotAccept;
const notAcceptCommentElm = document.getElementById("NotAcceptComment");

async function showModalNotAccept(id, type) { // type: acc= 0 / pf= 12 financial | 13 tamin | 14: SaleAdmin
    notAcceptCommentElm.value = "";
    document.getElementById("NotAcceptHeader").textContent = type !== 0 ? "عدم تایید پیش‌فاکتور" : "عدم تایید واریزی";
    idNotAccept = id;
    typeNotAccept = type;
    ModalToggle("#NotAcceptModal");
}

async function submitNotAccept() { // رد تایید و بازگشت | عدم تایید
    try {
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
            FLshowLoading();
            const urlMap = {
                12: `/Factor/NotAcceptFinancial`,
                13: `/Factor/NotAcceptTaminExpert`,
                14: `/Factor/NotAcceptSalesAdmin`,
            };
            
            const res = await axios.post(urlMap[typeNotAccept], model);
            if (res.status === 200) {
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
                toastifySuccess("عدم تایید با موفقیت انجام شد");
            } else {
                console.log(res);
                toastifyError(res.data);
            }
            FLshowLoading(false);
        }
        notAcceptCommentElm.value = "";
        ModalToggle("#NotAcceptModal");
    } catch (err) {
        console.log(err);
        setTimeout(async () => await submitNotAccept(), 3000);
    }
}

async function rejectPreFactor(id, type) { // رد تایید
    try {
        FLshowLoading();
        const urlMap = {
            12: `/Factor/RejectFinancial/${id}`,
            13: `/Factor/RejectTaminExpert/${id}`,
        };
        
        const res = await axios(urlMap[type]);
        if (res.status === 200) {
            switch (type) {
                case 12:
                    await GetPreFactorFinancialExpert();
                    break;
                case 13:
                    await GetPreFactorTaminExpert();
                    break;
            }
            FLshowLoading(false);
            toastifySuccess("پیش‌فاکتور رد تایید شد و در انتظار تایید قرار گرفت", 5000);
        }
        
    } catch (err) {
        console.log(err);
        setTimeout(async () => await rejectPreFactor(id, type), 3000);
    }
}


/*const submitAttachInvoice = async () => {
    const patType = Number(document.querySelector("#AttachInvoiceModalId input[type='radio'][name='payTypeAttach']:checked").value);
    if (patType !== 3 && !attachInvoiceList.length) {
        toastifyError("سند واریزی اجباریست");
        return;
    }
    
    if (patType !== 3) {
        const model = {
            List: attachInvoiceList, pFactorId: pfactorID, type: 6,
        };
        FLshowLoading();
        $.post("/Factor/UploadAttachmentInvoice", model, async (res) => {
            attachInvoiceList = [];
            document.getElementById("AttachInvoice").value = "";
            
            if (invoicePassType === 1) {
                await getAllPreFactor();
                
            } else if (invoicePassType === 2) {
                await GetPreFactorPartial();
                
            } else if (invoicePassType === 4) {
                await refreshDealPreFactors();
            }
            
            toastifySuccess("ثبت با موفقیت انجام شد");
            closeModalAttachInvoice();
            FLshowLoading(false);
        }).fail(() => {
            setTimeout(() => submitAttachInvoice(), 3000);
        });
    } else {
        
        const model = {
            ...mySer.convertFormToJSON("PayAttachFormId"),
            InvoiceType: 1,
            AccType: 1,
            PayType: 3,
            listAtt: attachInvoiceList,
            pFactorId: pfactorID,
        };
        
        FLshowLoading();
        $.post(baseUrl + `factor/AddPayAttachment`, model, async (res) => {
            if (res.status === 200) {
                if (invoicePassType === 1) {
                    await getAllPreFactor();
                    
                } else if (invoicePassType === 2) {
                    await GetPreFactorPartial();
                    
                } else if (invoicePassType === 4) {
                    await refreshDealPreFactors();
                } else if (invoicePassType === 5) {
                    await getAllReceiptsPreFactor(pfactorID);
                    await GetPreFactorFinancialExpert();
                }
                
                toastifySuccess("ثبت با موفقیت انجام شد");
                closeModalAttachInvoice();
            } else {
                toastifyError(res.error);
            }
            FLshowLoading(false);
            
        }).fail(() => {
            setTimeout(async () => await submitAttachInvoice(), 3000);
        });
    }
};

const closeModalAttachInvoice = () => {
    $("#AttachInvoiceModalId").modal("hide");
    mySer.emptyForm("PayAttachFormId");
    attachInvoiceList = [];
    document.getElementById("DivShowAttachInvoice").innerHTML = "";
    const attachInvoice = document.getElementById("AttachInvoice");
    if (attachInvoice) attachInvoice.value = "";
};*/

const closeModalInvoice = () => {
    const invoiceModalId = document.getElementById("InvoiceModalId");
    ModalToggle(invoiceModalId, "hide");
    AccParentId = null;
    isEditInvoiceModal = false;
    mySer.emptyForm("PayFactorFormId");
    SanadPayList = [];
    invoiceModalId.querySelector("#SanadParId").classList.remove("AlertReqRadio");
    invoiceModalId.querySelector("#DivEtebariId")?.classList.remove("d-none");
    invoiceModalId.querySelector("#TransferRadioWrapper")?.classList.add("d-none");
    invoiceModalId.querySelector("#DivShowSanadPar").innerHTML = "";
    const upSanadPay = invoiceModalId.querySelector("#upSanadPay");
    if (upSanadPay) upSanadPay.value = "";
};


var delImgPerStatus = 0;


const openCustomerProfile = async (id, pfSerialNum) => {
    if (!id) return;
    const res = await axios(`/Customer/CustomerProfile?Id=${id}`);
    if (res.status === 200) {
        sessionStorage.setItem("targetPf", pfSerialNum);
        window.open("/Customer/CustomerProfile?Id=" + id);
    } else if (res.status === 204) {
        toastifyError("شما به این صفحه دسترسی ندارید!");
    }
};


const removeReqUp = () => {
    var items = document.getElementsByClassName("Uhog");
    
    $.each(items, (index, item) => {
        item.removeAttribute("required");
    });
};

const addReqUp = () => {
    var items = document.getElementsByClassName("Uhog");
    $.each(items, (index, item) => {
        item.setAttribute("required", "");
    });
};


const changeJur = (val) => {
    
    if (val == 0) {
        $("#UpJuridicalA").prop("checked", true);
        $("#UpJuridicalB").prop("checked", false);
        $("#JuridicalUpDiv").css("display", "none");
        removeReqUp();
    } else {
        $("#UpJuridicalA").prop("checked", false);
        $("#UpJuridicalB").prop("checked", true);
        $("#JuridicalUpDiv").css("display", "flex");
        addReqUp();
    }
};


const cancelUpCustomer = () => {
    mySer.emptyForm("CuInfoUpFormId");
    changeJur(0);
    
    
};


const OpenCloseMenu = () => {
    if (showFlag == 0) {
        $("#left").css("display", "none");
        $("#right").css("display", "");
        OpenMenu();
        showFlag = 1;
        
    } else {
        $("#left").css("display", "");
        $("#right").css("display", "none");
        CloseMenu();
        showFlag = 0;
    }
};

const OpenMenu = () => {
    $("#desktopSidebar").removeClass("closeMenu");
    $("#desktopSidebar").addClass("openMenu");
    
    $("#profileImage").removeClass("personImageCloseMenu");
    $("#profileImage").addClass("personImageOpenMenu");
    
    $(".ToggleClossMenu").css("display", "");
    $("#VContent").css("margin-right", "220px");
    
};
const CloseMenu = () => {
    $("#desktopSidebar").removeClass("openMenu");
    $("#desktopSidebar").addClass("closeMenu");
    
    $("#profileImage").removeClass("personImageOpenMenu");
    $("#profileImage").addClass("personImageCloseMenu");
    
    $(".ToggleClossMenu").css("display", "none");
    $("#VContent").css("margin-right", "60px");
};

/*document.querySelectorAll(".sdate").forEach(elm => {
    elm.addEventListener("blur", (e) => {
        e.target.classList.toggle("AlertReq", !isValidDate(e.target.value));
    });
});*/


// $('.Icsdate').on('click', (e) => {
//     var arr = Array.from(e.delegateTarget.parentElement.children);
//     arr.forEach((item, index) => {
//         if (item.type == 'text') {
//             if (item.classList.contains('sdate') || item.classList.contains('stime')) {
//                 item.focus();
//             }
//         }
//     })
// })


let cropper;
let personImageFile = null;
const profileModal = document.getElementById("UserSetting");
const cropModal = document.getElementById("cropModal");
const inpPersonImg = profileModal.querySelector("#InpPersonImg");
const personImgPreview = profileModal.querySelector(".profileModal-image");
const personImgFileName = profileModal.querySelector(".profile-file-name");
const deletePersonImgBtn = profileModal.querySelector(".profile-delete-image");
inpPersonImg.addEventListener("change", (e) => {
    const file = e.target.files[0];
    if (!file) return;
    
    document.getElementById("cropImage").src = URL.createObjectURL(file);
    cropModal.style.display = "flex";
    
    if (cropper) cropper.destroy();
    cropper = new Cropper(document.getElementById("cropImage"), {
        aspectRatio: 1, viewMode: 2, dragMode: "move", background: false, autoCropArea: 1, autoCrop: true, //cropBoxResizable: false,
    });
});

document.getElementById("cropOkBtn").addEventListener("click", () => {
    if (!cropper) return;
    const canvas = cropper.getCroppedCanvas();
    if (!canvas) return;
    
    canvas.toBlob((blob) => {
        if (!blob) return;
        
        personImageFile = new File([blob], "profile.jpg", { type: "image/jpeg" });
        personImgPreview.src = URL.createObjectURL(personImageFile);
        
        const originalFile = inpPersonImg.files?.[0];
        if (originalFile) {
            personImgFileName.value = originalFile.name;
        }
        
        inpPersonImg.value = "";
        deletePersonImgBtn.classList.remove("d-none");
        delImgPerStatus = 2;
        
        cropper.destroy();
        cropper = null;
        
        cropModal.style.display = "none";
    }, "image/jpeg");
});

document.getElementById("cropCloseBtn").addEventListener("click", () => {
    cropModal.style.display = "none";
    inpPersonImg.value = "";
});

function delPersonImage() {
    personImgPreview.src = "/Img/User.png";
    
    inpPersonImg.value = "";
    personImgFileName.value = "";
    
    personImageFile = null;
    
    delImgPerStatus = 1;
    deletePersonImgBtn.classList.add("d-none");
}

async function SavePersonSetting() {
    const formData = new FormData();
    const firstName = document.getElementById("PFirstName").value;
    const lastName = document.getElementById("PLastName").value;
    const phoneNumber = document.getElementById("PPhoneNumber").value;
    formData.append("FirstName", firstName);
    formData.append("LastName", lastName);
    formData.append("PhoneNumber", phoneNumber);
    formData.append("ImageStatus", delImgPerStatus);
    formData.append("PersonImage", personImageFile);
    
    await sendApiRequest({
        url: `/Account/UpPersonSetting`,
        method: "POST",
        data: formData,
        successMsg: "تغییرات با موفقیت انجام شد.",
        onSuccess: async () => {
            document.getElementById("profileName").textContent = firstName + " " + lastName;
            document.getElementById("profileImage").src = personImgPreview.src;
            delImgPerStatus = 0;
            ModalToggle(profileModal);
        },
    });
    
}
function ShowModalUserSetting() {
    ModalToggle(profileModal);
}


const GetUserLogin = async () => {
    return new Promise(resolve => {
        axios.post(`/Account/GetUserLogin`).then(res => {
            currentUser = res.data;
            sessionStorage.setItem("Rols", JSON.stringify(currentUser.rollNames));
            
            // const imgSrc = currentUser.personImage ? `data:image/jpg;base64,${currentUser.personImage}` : "/Img/User.png";
            // document.getElementById("profileImage").src = imgSrc;
            // document.getElementById("PersonImgPreview").src = imgSrc;
            // document.getElementById("btnDelPersonImg").style.display = currentUser.personImage ? "" : "none";
            
            // document.getElementById("profileName").textContent = `${currentUser.firstName || ""} ${currentUser.lastName || ""}`.trim();
            // document.getElementById("PFirstName").value = currentUser.firstName || "";
            // document.getElementById("PLastName").value = currentUser.lastName || "";
            // document.getElementById("PPhoneNumber").value = currentUser.phoneNumber || "";
            
            checkRoles(currentUser.rollNames);
            
            document.dispatchEvent(new CustomEvent("UserLoginReady"));
            resolve(currentUser.rollNames);
        });
    });
};

let locationTabIdEnvelope;
const checkRoles = (roles) => {
    if (!roles) return;
    roles.forEach(role => {
        const elements = document.querySelectorAll(`.${role.name}`);
        elements.forEach(el => {
            const dataAttr = el.getAttribute("data-class");
            if (dataAttr) {
                el.classList.add(...dataAttr.split(" "));
            } else {
                el.style.display = "";
            }
        });
    });
    
    const roleNames = roles.map(r => r.name);
    
    if (roleNames.includes("TaminAdmin") || roleNames.includes("TaminExpert"))
        locationTabIdEnvelope = "TTaminExpertId";
    else if (roleNames.includes("Admin") || roleNames.includes("SalesAdmin"))
        locationTabIdEnvelope = "TSalesAdminId";
    else if (roleNames.includes("FinancialExpert"))
        locationTabIdEnvelope = "TFinancialExpertId";
    else if (roleNames.includes("SalesExpert"))
        locationTabIdEnvelope = "TSalesExpertId";
    
    if (locationTabIdEnvelope) {
        refreshCounterCartable();
    }
};


function refreshCounterCartable() {
    axios("/Cartable/GetCountEnvelope").then(res => {
        setCountEnvelope(res.data.countObj);
        setCountEmail(res.data.emailsCount);
        setCountChat(res.data.chatsCount);
    });
}


const setCountChat = (chatsCount) => {
    const countChatElm = document.getElementById("CountChatId");
    if (!countChatElm) return;
    
    countChatElm?.classList.toggle("d-none", !Boolean(chatsCount));
    countChatElm.textContent = chatsCount || "";
};
const setCountEmail = (emailsCount) => {
    const countEmailElm = document.getElementById("CountEmailId");
    if (!countEmailElm) return;
    
    countEmailElm?.classList.toggle("d-none", !Boolean(emailsCount));
    countEmailElm.textContent = emailsCount || "";
    
    if (emailsCount) {
        const text = `شما <b>${emailsCount}</b> پیام دریافتی مشاهده نشده دارید.`;
        setTimeout(() => notificationChange(text, { link: "/Email/Index", isBlank: false }, "default"), 3000);
    }
};


let oldSumCount = null;
const setCountEnvelope = (countObj) => {
    const countResult = {
        salesAdmin: {
            count: countObj.salesAdmin || 0,
            id: "TSalesAdminId",
        },
        tamin: {
            count: (countObj.tamin?.invoiceCount || 0) + (countObj.tamin?.preFactorCount || 0),
            id: "TTaminExpertId",
        },
        financial: {
            count: (countObj.financial?.invoiceCount || 0) + (countObj.financial?.preFactorCount || 0) + (countObj.financial?.requestCount || 0),
            id: "TFinancialExpertId",
        },
        managingDirector: {
            count: (countObj.managingDirector?.requestCount || 0) + (countObj.managingDirector?.transferCount || 0),
            id: "TManagingDirectorId",
        },
    };
    let sumCount = Object.values(countResult).reduce((sum, c) => sum + (c.count || 0), 0);
    const countEnvelopeElm = document.getElementById("CountEnvelopeId");
    setBadge(countEnvelopeElm, sumCount);
    
    if (sumCount && sumCount > oldSumCount) {
        const text = `شما در کارتابل خود <b>${sumCount}</b> مورد بررسی نشده دارید`;
        setTimeout(() => notificationChange(text, { link: `/cartable/Index?tab=${locationTabIdEnvelope}`, isBlank: false }, "default"), 3000);
    }
    oldSumCount = sumCount;
    
    const cartableTabsElm = document.getElementById("CartableTabs");
    if (!cartableTabsElm) return;
    Object.values(countResult).forEach(({ id, count }) => {
        const el = cartableTabsElm.querySelector(`#${id} .badge-count`);
        setBadge(el, count);
    });
    
    // sub tabs
    setBadge("#MDRequestAmountTab .badge-count", countObj.managingDirector?.requestCount || 0);
    setBadge("#MDTransferTab .badge-count", countObj.managingDirector?.transferCount || 0);
    setBadge("#FinancialPrefactorTab .badge-count", countObj.financial?.preFactorCount || 0);
    setBadge("#FinancialInvoiceTab .badge-count", countObj.financial?.invoiceCount || 0);
    setBadge("#FinancialRequestTab .badge-count", countObj.financial?.requestCount || 0);
    setBadge("#TaminPrefactorTab .badge-count", countObj.tamin?.preFactorCount || 0);
    setBadge("#TaminInvoiceTab .badge-count", countObj.tamin?.invoiceCount || 0);
};

const setBadge = (target, count) => {
    const el = typeof target === "string" ? document.querySelector(target) : target;
    if (!el) return;
    
    el.textContent = count || "";
    el.classList.toggle("d-none", !count);
};

const GoToCarEnvelope = () => {
    window.location.href = `/cartable/Index?tab=${locationTabIdEnvelope}`;
};


const bankAccountsCache = {};
const getAllBankAccountsJson = async (rerender, type) => {
    const key = type ?? "all";
    
    try {
        if (rerender || !bankAccountsCache[key]) {
            bankAccountsCache[key] = axios(`/Accountancy/GetAllBankAccountJson?type=${type}`).then(res => res.data);
        }
        return await bankAccountsCache[key];
    } catch (err) {
        delete bankAccountsCache[key];
        console.log(err);
    }
};

/**
 * @typedef {Object} BankAccountSelectOptions
 * @property {Element|string} element
 * @property {number[]} type
 * @property {boolean} [isDisable]
 * @property {boolean} [isTomSelect]
 */
/**
 * @param {BankAccountSelectOptions[]} selects
 * @param {boolean} [rerender=false]
 */
const getAllBankAccounts = async (selects, rerender = false) => {
    
    /*const selects = [
        { element: document.querySelector("#InvoiceModalId select[name='bankAccountId']"), type: [1] },
        { element: document.querySelector("#InvoiceModalId select[name='bankAccountId']"), type: [1] },
        { element: document.querySelector("#ShowPaymentModal select[name='bankAccountId']"), type: [1], isDisable: true },
        { element: document.querySelector("#reqAmountAttachModal select[name='fromAccountId']"), type: [1] },
        { element: document.querySelector("#reqAmountAttachModal select[name='toAccountId']"), type: [2, 3], isTomSelect: true },
        { element: document.querySelector("#transferModal select[name='fromAccountId']"), type: [1] },
        { element: document.querySelector("#transferModal select[name='toAccountId']"), type: [2, 3], isTomSelect: true },
    ].filter(e => e.element);*/
    const resolvedSelects = selects.map(x => ({
        ...x,
        element: typeof x.element === "string" ? document.querySelector(x.element) : x.element,
    })).filter(x => x.element);
    
    const elementsToToggleDisable = resolvedSelects.filter(s => !s.isDisable).map(s => s.element);
    toggleElementsDisabled(elementsToToggleDisable);
    
    try {
        const data = await getAllBankAccountsJson(rerender);
        
        resolvedSelects.forEach(x => {
            const filteredData = data.filter(d => x.type.includes(d.type));
            if (x.isTomSelect) {
                createSupplierTomSelect(x.element, filteredData);
                return;
            }
            
            createOption(
                x.element,
                filteredData.reverse().map(item => {
                    const ui = buildBankAccountText(item);
                    
                    return {
                        value: item.value,
                        text: `${ui.title}${ui.details ? ` (${ui.details})` : ""}`,
                    };
                }),
                true,
                "انتخاب کنید",
            );
        });
        
    } catch (err) {
        console.log(err);
    } finally {
        toggleElementsDisabled(elementsToToggleDisable, false);
    }
};

const buildBankAccountText = (item) => {
    const parts = [];
    
    if (item.bankName)
        parts.push(item.bankName);
    
    const number = item.accountNumber || item.shebaNumber || item.cartNumber;
    
    if (number)
        parts.push(number);
    
    return {
        title: item.accountName,
        details: parts.join(" "),
        officialText: item.officialType === 1 ? "رسمی" : "غیررسمی",
    };
};

const buildBankAccountUi = (item) => {
    const number = item.accountNumber || item.cartNumber || item.shebaNumber;
    
    const details = [];
    
    if (item.bankName)
        details.push(item.bankName);
    
    if (number)
        details.push(number);
    
    const detailsText = details.join(" - ");
    
    const displayText = detailsText
        ? `${item.accountName} (${detailsText})`
        : item.accountName;
    
    return {
        title: item.accountName,
        
        details: [
            item.bankName ? `بانک: ${item.bankName}` : null,
            item.accountNumber ? `حساب: ${item.accountNumber}` : null,
            item.cartNumber ? `کارت: ${item.cartNumber}` : null,
            item.shebaNumber ? `شبا: ${item.shebaNumber}` : null,
        ].filter(Boolean).join(" | "),
        
        displayText,
        
        officialText: item.officialType === 1 ? "رسمی" : "غیررسمی",
        
        isOfficial: item.officialType === 1,
    };
};


const createSupplierTomSelect = (element, items) => {
    
    if (element.tomselect) {
        element.tomselect.destroy();
    }
    
    new TomSelect(element, {
        options: items.map(item => {
            const ui = buildBankAccountUi(item);
            return {
                value: item.value,
                
                title: ui.title,
                details: ui.details,
                text: ui.displayText,
                
                officialText: ui.officialText,
                isOfficial: ui.isOfficial,
                
                search: `
                    ${ui.title}
                    ${ui.details}
                    ${ui.officialText}
                `,
            };
        }),
        searchField: ["search"],
        //labelField: ["title"],
        placeholder: "جستجو و انتخاب حساب مقصد",
        
        maxItems: 1,
        persist: false,
        create: false,
        
        render: {
            option: (data, escape) => `
                <div class="bank-option">
                    <div class="top">
                        <div class="title">
                            ${escape(data.title)}
                        </div>

                        <div class="bank-badge ${!data.isOfficial ? "unofficial" : ""}">
                            ${escape(data.officialText)}
                        </div>
                    </div>

                    ${data.details ? `<div class="details">${escape(data.details)}</div>` : ""}
                </div>
            `,
            
            item: (data, escape) => `
                <div class="bank-item">
                    <div class="item-title">
                        ${escape(data.text)}
                    </div>

                    <div class="bank-badge ${!data.isOfficial ? "unofficial" : ""}">
                        ${escape(data.officialText)}
                    </div>
                </div>
            `,
        },
    });
};

const hideFact = async () => {
    //document.getElementById("DivFacListId").style.display = "none";
    await GetFactorPartial({ passType: 1, customerId });
    
    document.querySelector(".SelectedPf").classList.remove("SelectedPf");
    document.getElementById("FactorDivTitleId").innerHTML = `<span class="d-none d-xxl-inline-block">فاکتورها</span>`;
    if (window.innerWidth < 1400) {
        ShowTab("preFactorTab");
    }
};


function showTaminDropdown() {
    Dropdown.setupDelegate({
        container: document,
        toggleSelector: ".taminDropdownToggle",
        
        options: {
            transitionDuration: 200,
            originalDisplay: "flex",
            effect: "zoom",
            zIndex: 1070,
        },
        
        async onOpen({ toggle, content }) {
            const pfId = toggle.dataset.pfid;
            const res = await axios(`/order/GetOrderHead?pfId=${pfId}`);
            content.innerHTML = res.data;
        },
    });
}
/////////

const notificationDropdown = document.getElementById("notification-dropdown");
const notificationIconBadge = document.getElementById("notificationIconBadge");
async function getRemind() {
    if (!notificationDropdown || !notificationIconBadge) return;
    axios.get(`/Carizo/GetRemind`).then(res => {
        if (res.status === 200) {
            return res.data;
        } else {
            throw new Error();
        }
        
    }).then((tasks) => {
        notificationDropdown.innerHTML = tasks.html;
        notificationIconBadge.classList.toggle("d-none", !tasks.count);
        
    }).catch((err) => {
        console.log(err);
    });
}

async function getTask(id) {
    
    try {
        const res = await axios.get(`/Carizo/getTask/${id}`);
        return res.data;
    } catch (err) {
        console.log(err);
    }
}


async function handleNotifAction(id, isRemindOff) {
    const idx = taskList.findIndex(f => f === id);
    if (idx !== -1) taskList.splice(idx, 1);
    
    const url = isRemindOff ? `Carizo/ShowTask?id=${id}` : `Carizo/DismissTask?id=${id}`;
    try {
        const res = await fetch(baseUrl + url);
        
        if (res.ok) {
            await getRemind();
        } else {
            throw new Error();
        }
    } catch (error) {
        console.log(error);
    }
    
}

async function goToLink(link, btn, blank = true) {
    if (link) goTo(link, blank);
    if (btn) {
        //const closeBtn = btn.parentElement.parentElement.parentElement.parentElement.querySelector(".toast-close");
        const closeBtn = btn.closest(".toastify").querySelector(".toast-close");
        closeBtn.click();
    }
}

async function readNotifAction(id, btn, entityType = null, entityId = null) {
    await handleNotifAction(id, true);
    
    if (entityType === "Acc") {
        await showPayment(entityId);
    } else {
        window.open("/Carizo/Index", "_blank");
    }
    
    if (btn) {
        //const closeBtn = btn.parentElement.parentElement.parentElement.parentElement.querySelector(".toast-close");
        const closeBtn = btn.closest(".toastify").querySelector(".toast-close");
        closeBtn.click();
    }
    
}

async function dismissNotif(id = null, btn) {
    if (id) {
        await handleNotifAction(id, false);
    }
    if (btn) {
        const closeBtn = btn.closest(".toastify").querySelector(".toast-close");
        closeBtn.click();
    }
    
}

async function snoozeNotif(id, btn) {
    const indexid = taskList.findIndex(f => f === id);
    if (indexid !== -1) taskList.splice(indexid, 1);
    try {
        const res = await fetch(baseUrl + `Carizo/AddSnooze?id=${id}`);
        
        if (res.ok) {
            await getRemind();
            
            // await getAllTask(0)
            // await getAllTask(1)
            // await getAllTask(2)
        } else {
            throw new Error();
        }
    } catch (error) {
        console.log(error);
    }
    if (btn) {
        //const closeBtn = btn.parentElement.parentElement.parentElement.parentElement.parentElement.querySelector(".toast-close");
        const closeBtn = btn.closest(".toastify").querySelector(".toast-close");
        closeBtn.click();
    }
}


function showTaskUsersDropdown() {
    Dropdown.setupDelegate({
        container: document,
        toggleSelector: ".taskUsers-dropdown__toggle",
        options: {
            originalDisplay: "flex",
            effect: "slide",
            placement: "right",
        },
    });
}


//

let targetEntityTaskType = null;
let targetEntityTaskId = null;
let dataTask;
let editSelectedCustomRepeat = [];

const showTaskModal = async (entityId, entityType) => {
    document.getElementById("FLshowLoading").style.display = "";
    const apiUrl = entityType === "Report" ? `/Report/GetReport/${entityId}` : `/Deal/GetDeal/${entityId}`;
    const data = (await axios(apiUrl)).data;
    
    targetEntityTaskType = entityType;
    targetEntityTaskId = entityId;
    let descText;
    if (entityType === "Report") {
        descText = `بابت مشتری: "${data.customerTxt || ""} - ${data.repTel || ""}"\nگزارش: "${data.reportText}"`;
    } else if (entityType === "Deal") {
        descText = `بابت سفارش ${data.serialNumber}: "${data.title}"\nمشتری: "${data.customerName} - ${data.mobileNumber ?? ""}"`;
        if (data.commentText) descText += `\nتوضیحات: "${data.commentText}"`;
    }
    document.getElementById("editTaskDescription").value = descText;
    
    document.getElementById("editTimeRemind").value = "";
    const assignWrapper = document.getElementById("editTaskAssignWrapper");
    if (assignWrapper) assignWrapper.style.display = "none";
    document.getElementById("submitEditTaskModal").innerText = "ثبت فعالیت";
    document.getElementById("editTaskModalTitle").innerText = "فعالیت";
    
    const customOptionsEl = document.getElementById("customRepeatOptions");
    const toggleCustomOptions = () => {
        const editRepeatTypeVal = document.getElementById("editRepeatType").value;
        
        customOptionsEl.style.display = editRepeatTypeVal === "custom" ? "flex" : "none";
        document.getElementById("divEditUntilRemind").style.display = editRepeatTypeVal !== "none" ? "flex" : "none";
    };
    document.getElementById("editRepeatType").removeEventListener("change", toggleCustomOptions);
    document.getElementById("editRepeatType").addEventListener("change", toggleCustomOptions);
    
    
    customOptionsEl.querySelectorAll("input").forEach(ch => {
        ch.addEventListener("change", (e) => {
            if (e.target.checked) {
                if (!editSelectedCustomRepeat.includes(e.target.value)) {
                    editSelectedCustomRepeat.push(e.target.value);
                }
            } else {
                const index = editSelectedCustomRepeat.indexOf(e.target.value);
                if (index !== -1) {
                    editSelectedCustomRepeat.splice(index, 1);
                }
            }
        });
    });
    document.getElementById("FLshowLoading").style.display = "none";
    $("#editTaskModal").modal("toggle");
    
};

async function updateTask() {
    if (!await mySer.CheckValidForm("formEditTask")) return;
    
    const model = mySer.convertFormToJSON("formEditTask");
    model.description = document.getElementById("editTaskDescription").value;
    model.title = document.getElementById("editTaskTitle").value;
    model.taskType = 1;
    
    if (dataTask) {
        model.id = dataTask.id;
        model.taskUsers = editSelectedUsers;
        
        model.entityType = dataTask.entityType;
        model.entityId = dataTask.entityId;
        model.systematic = dataTask.systematic;
        model.shareTaskId = dataTask.shareTaskId;
        
        const indexId = taskList.findIndex(f => f === dataTask.id);
        if (indexId !== -1) taskList.splice(indexId, 1);
        
        if (model.taskUsers.length > 0) {
            model.taskType = Number(document.querySelector("#editAssignList input[name=\"taskType\"]:checked").value);
        }
    } else {
        model.entityType = targetEntityTaskType;
        model.entityId = targetEntityTaskId;
    }
    
    model.remind = false;
    model.remindDismiss = true;
    
    if (model.remindTime) {
        model.remind = true;
        model.remindDismiss = false;
    }
    if (model.repeatType === "none") {
        model.repeatType = "";
    }
    if (model.repeatType) {
        model.repeat = true;
    }
    
    
    model.repeatDays = "";
    if (editSelectedCustomRepeat.length > 0 && model.repeatType === "custom") {
        model.repeatDays = editSelectedCustomRepeat.join(",");
    }
    
    
    if (model.repeatType === "custom" && !model.repeatDays) {
        document.getElementById("customRepeatOptions").style.border = "1px solid red";
        return;
    } else {
        document.getElementById("customRepeatOptions").style.border = "none";
    }
    
    document.getElementById("FLshowLoading").style.display = "";
    try {
        const res = await fetch(baseUrl + "Carizo/AddTask", {
            method: "POST", headers: {
                "Content-Type": "application/json",
            }, body: JSON.stringify(model),
        });
        
        if (res.ok) {
            
            $("#editTaskModal").modal("toggle");
            document.getElementById("FLshowLoading").style.display = "none";
            
            if (!dataTask) {
                document.getElementById("formEditTask").reset();
                document.getElementById("divEditUntilRemind").style.display = "none";
                document.getElementById("customRepeatOptions").style.display = "none";
                editSelectedCustomRepeat.length = 0;
                toastifySuccess("تسک با موفقیت ثبت شد.");
            } else {
                toastifySuccess("تسک با موفقیت ویرایش شد.");
            }
            dataTask = null;
            
        } else {
            throw new Error();
        }
    } catch (error) {
        if (!dataTask) {
            toastifyError("خطا در ثبت تسک جدید");
        } else {
            toastifyError("خطا در ویرایش تسک");
        }
        console.log(error);
        document.getElementById("FLshowLoading").style.display = "none";
        dataTask = null;
        setTimeout(async () => {
            await updateTask();
        }, 3000);
    }
    
}

function cancelEditTaskModal() {
    $("#editTaskModal").modal("toggle");
    dataTask = null;
}

function removeSchedulerItem(from) {
    if (from === "taskDate") {
        document.getElementById("taskDate").parentElement.style.display = "none";
        document.getElementById("taskDate").value = "";
    } else if (from === "timeRemind") {
        document.getElementById("timeRemind").parentElement.style.display = "none";
        document.getElementById("timeRemind").value = "";
    } else if (from === "repeatUntilRemind") {
        document.querySelectorAll(".repeatTask-dropdown-items").forEach((op) => {
            op.classList.remove("selected");
            op.querySelector(".checkIcon").classList.remove("selected");
        });
        document.querySelectorAll(".subDropdown-customRepeat__container input").forEach((cb) => (cb.checked = false));
        
        document.getElementById("untilRemind").value = "همیشه";
        document.getElementById("untilRemindSpan").innerText = "همیشه";
        document.querySelector("#untilRemindSpan ~ div").style.display = "none";
        
        document.getElementById("repeatType-input").value = "";
        document.getElementById("repeatType-span").innerText = "";
        document.getElementById("repeatType-span").parentElement.style.display = "none";
        
        document.getElementById("untilRemind-outerInp").value = "";
        document.getElementById("untilRemind-outerDiv").style.display = "none";
    } else if (from === "untilRemind") {
        document.getElementById("untilRemind").value = "همیشه";
        document.getElementById("untilRemindSpan").innerText = "همیشه";
        document.querySelector("#untilRemindSpan ~ div").style.display = "none";
        
        document.getElementById("untilRemind-outerInp").value = "";
        document.getElementById("untilRemind-outerDiv").style.display = "none";
    } else if (from === "editTaskDate") {
        document.getElementById("editTaskDate").value = "";
        document.getElementById("editTaskDate").nextElementSibling.style.display = "none";
    } else if (from === "editTimeRemind") {
        document.getElementById("editTimeRemind").value = "";
        document.getElementById("editTimeRemind").nextElementSibling.style.display = "none";
    } else if (from === "editUntilRemind") {
        document.getElementById("editUntilRemind").value = "همیشه";
        document.getElementById("editUntilRemind").nextElementSibling.style.display = "none";
    } else {
        document.getElementById("taskDate").value = "";
        document.getElementById("taskDate").parentElement.style.display = "none";
        
        document.getElementById("timeRemind").parentElement.style.display = "none";
        document.getElementById("timeRemind").value = "";
        
        document.querySelectorAll(".repeatTask-dropdown-items").forEach((op) => {
            op.classList.remove("selected");
            op.querySelector(".checkIcon").classList.remove("selected");
        });
        document.querySelectorAll(".subDropdown-customRepeat__container input").forEach((cb) => (cb.checked = false));
        document.querySelectorAll(".assign-users-container input").forEach((cb) => (cb.checked = false));
        const taskTypeRadio = document.querySelector("#assignList input[name='taskType'][value='2']");
        if (taskTypeRadio) taskTypeRadio.checked = true;
        
        document.getElementById("untilRemind").value = "همیشه";
        document.getElementById("untilRemindSpan").innerText = "همیشه";
        document.querySelector("#untilRemindSpan ~ div").style.display = "none";
        
        document.getElementById("repeatType-input").value = "";
        document.getElementById("repeatType-span").innerText = "";
        document.getElementById("repeatType-span").parentElement.style.display = "none";
        
        document.getElementById("untilRemind-outerInp").value = "";
        document.getElementById("untilRemind-outerDiv").style.display = "none";
    }
}

async function getAllTask(type, systematic = 0, loc = 1) {
    const containerName = type === 0 ? "todoTask" : type === 1 ? (loc === 1 ? "todayTask" : "todayTaskDash") : "finishedTask";
    const containerElm = document.getElementById(containerName + "Id");
    const countElm = document.getElementById(containerName + "Count");
    const noTaskImgDash = document.getElementById("noTaskImg");
    try {
        const res = await axios.get(`/Carizo/GetAllTask?type=${type}&loc=${loc}&systematic=${systematic}`);
        if (res.status === 200) {
            containerElm && (containerElm.innerHTML = res.data.html);
            countElm && (countElm.innerHTML = res.data.count);
            noTaskImgDash && loc === 2 && (noTaskImgDash.style.display = res.data.count ? "none" : "flex");
        } else {
            throw new Error();
        }
    } catch (err) {
        console.error(err);
        containerElm && (containerElm.innerHTML = "خطا در بارگذاری فعالیت ها :(!");
        // toastifyError("خطا در بارگذاری اطلاعات!");
    }
}

async function finishTask(id, type, thisBtn, loc = 1) {
    const buttons = loc == 1 ? "#allCarizoContainer button" : "#todayTaskDashId button";
    document.querySelectorAll(buttons).forEach(btn => btn.setAttribute("disabled", "true"));
    thisBtn.querySelector(".spinner-border").style.display = "inline-block";
    
    try {
        const res = await axios(`/Carizo/finishTask/${id}`);
        if (res.status === 200) {
            res.data.status === 201 ? toastifyError(res.data.error) : toastifySuccess("تسک انجام شد!");
            
            document.querySelectorAll(buttons).forEach(btn => btn.removeAttribute("disabled"));
            thisBtn.querySelector(".spinner-border").style.display = "none";
        }
        
        if (Number(type) === 1) {
            await getRemind();
        }
        
    } catch (err) {
        console.log(err);
        document.querySelectorAll(buttons).forEach(btn => btn.removeAttribute("disabled"));
        thisBtn.querySelector(".spinner-border").style.display = "none";
        toastifyError("خطای سرور!");
    }
}


const overlay = document.getElementById("overlay");
const mobileMoreMenuBtn = document.getElementById("mobileMoreMenuBtn");
const moreMenuDropdown = document.getElementById("moreMenuDropdown");
/*
mobileMoreMenuBtn?.addEventListener("click", () => {
    if (!moreMenuDropdown || !mobileMoreMenuBtn || !overlay) return;
    const isVisible = moreMenuDropdown.classList.toggle("show");
    overlay.classList.toggle("show", isVisible);
});

document.addEventListener("click", (e) => {
    if (!moreMenuDropdown || !mobileMoreMenuBtn || !overlay) return;
    if (!mobileMoreMenuBtn.contains(e.target) && !moreMenuDropdown.contains(e.target)) {
        moreMenuDropdown.classList.remove("show");
        overlay.classList.remove("show");
    }
});
*/


const mobileDrawer = document.getElementById("mobileDrawer");
const mobileDrawerOverlay = document.getElementById("mobileDrawerOverlay");

function toggleMobileDrawer(state) {
    
    const isOpen = state ?? !mobileDrawer.classList.contains("show");
    
    mobileDrawer.classList.toggle("show", isOpen);
    mobileDrawerOverlay.classList.toggle("show", isOpen);
    
    document.body.style.overflow = isOpen ? "hidden" : "";
}

mobileMoreMenuBtn?.addEventListener("click", () => {
    toggleMobileDrawer(true);
});


mobileDrawerOverlay?.addEventListener("click", () => {
    toggleMobileDrawer(false);
});

/*document.querySelectorAll("#mobileDrawer .has-submenu > .menu-item").forEach(el => {
    el.addEventListener("click", () => {
        const parent = el.closest(".has-submenu");
        console.log(parent);
        parent.classList.add("open");
    });
});*/


function sortableMobileMenu() {
    const navMenu = document.getElementById("navMenuBar");
    const moreMenu = document.getElementById("moreMenuDropdown");
    if (!navMenu || !moreMenu) return;
    
    function handleMoveSortable(e) {
        if (e.to === navMenu && e.from === moreMenu) {
            const navItems = Array.from(navMenu.querySelectorAll(".menu-item"))
                                  .filter(el => getComputedStyle(el).display !== "none");
            
            if (navItems.length >= 6) {
                navMenu.classList.add("nav-deny-drop");
                return false;
            }
        }
        
        navMenu.classList.remove("nav-deny-drop");
        return true;
    }
    
    function handleEndSortable() {
        navMenu.classList.remove("nav-deny-drop");
        
        const updatedNavMenu = Array.from(navMenu.querySelectorAll(".menu-item")).map(el => ({
            key: el.getAttribute("href") || el.id, location: "nav",
        }));
        const updatedMoreMenu = Array.from(moreMenu.querySelectorAll(".menu-item")).map(el => ({
            key: el.getAttribute("href") || el.id, location: "more",
        }));
        const updatedOrder = [...updatedNavMenu, ...updatedMoreMenu];
        
        localStorage.setItem("mobileMenuOrder", JSON.stringify(updatedOrder));
    }
    
    function restoreMenuOrder() {
        const order = JSON.parse(localStorage.getItem("mobileMenuOrder")) || [];
        if (!order.length) return;
        
        const allItems = document.querySelectorAll("#mobileNavbar .menu-item");
        const allMap = {};
        allItems.forEach(item => {
            const key = item.getAttribute("href") || item.id;
            if (key) allMap[key] = item;
        });
        
        const allKeys = Object.keys(allMap);
        
        const orderedKeys = order.map(o => o.key);
        const newKeys = allKeys.filter(k => !orderedKeys.includes(k));
        
        newKeys.forEach(k => {
            order.push({ key: k, location: "more" });
        });
        
        navMenu.innerHTML = "";
        moreMenu.innerHTML = "";
        
        order.forEach(({ key, location }) => {
            const el = allMap[key];
            if (!el) return;
            
            if (location === "nav") {
                navMenu.appendChild(el);
            } else {
                moreMenu.appendChild(el);
            }
        });
        
        if (newKeys.length > 0) {
            localStorage.setItem("mobileMenuOrder", JSON.stringify(order));
        }
    }
    
    [navMenu, moreMenu].forEach(menu => {
        new Sortable(menu, {
            group: "mobile-menus",
            animation: 150,
            delay: 120,
            delayOnTouchOnly: true,
            swapThreshold: 0.7,
            invertSwap: true,
            filter: "#mobileMoreMenuBtn",
            ghostClass: "lightGreen-background-class",
            chosenClass: "lightGreen-background-class",
            dragClass: "dragging",
            onMove: handleMoveSortable,
            onEnd: handleEndSortable,
        });
    });
    
    restoreMenuOrder();
}

sortableMobileMenu();

let warningData = [];
async function showModalWarnings() {
    await getAllWarnings();
    toggleMobileDrawer(false);
    ModalToggle("#warningModal");
}


const warningContainer = document.getElementById("warningContainer");
async function getAllWarnings(isRefresh = false) {
    const warningElmBtns = document.querySelectorAll(".menu-item.warning");
    if (!warningElmBtns.length) return;
    
    if (!warningContainer) return;
    warningContainer.innerHTML = "";
    try {
        isRefresh && FLshowLoading();
        const url = isRefresh ? `/Home/WarningsSepidarCrmFactors` : `/Home/GetAllSystemWarnings`;
        const res = await axios(url);
        
        warningData = res.data;
        
        warningElmBtns.forEach(elm => {
            elm.classList.toggle("d-none", warningData.length === 0);
            const badgeWarningCount = elm.querySelector(".badge-count");
            badgeWarningCount.innerText = warningData.length || "";
        });
        
        initDomWarningsModal();
        
        isRefresh && FLshowLoading(false);
    } catch (err) {
        console.log(err);
    }
}

function initDomWarningsModal() {
    if (warningData.length > 0) {
        warningData.forEach(item => {
            const div = document.createElement("div");
            div.className = "warning-box";
            div.innerHTML = `
                <div class="d-flex align-items-center gap-2">
                    <svg style="width: 18px;flex-shrink: 0; color: #EF5350;" viewBox="0 0 16 16" fill="currentColor"><path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/></svg>
                    <span style="color: #444;">${item.message}</span>
                </div>
                <div class="d-flex align-items-center gap-2">
                    <span class="d-flex align-items-center justify-content-center showDetails" style="cursor: pointer;">
                        <svg style="width: 18px;color: #ed2e2a;" viewBox="0 0 24 24" fill="currentColor"><path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z" /><path fill-rule="evenodd" d="M1.323 11.447C2.811 6.976 7.028 3.75 12.001 3.75c4.97 0 9.185 3.223 10.675 7.69.12.362.12.752 0 1.113-1.487 4.471-5.705 7.697-10.677 7.697-4.97 0-9.186-3.223-10.675-7.69a1.762 1.762 0 0 1 0-1.113ZM17.25 12a5.25 5.25 0 1 1-10.5 0 5.25 5.25 0 0 1 10.5 0Z" clip-rule="evenodd" /></svg>
                    </span>
                    <span class="d-flex align-items-center justify-content-center deleteWarning" style="cursor: pointer;">
                        <svg style="width: 18px;color: #EF5350;" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" /></svg>
                    </span>
                </div>
            `;
            div.querySelector(".deleteWarning").addEventListener("click", () => removeWarning(item.id));
            div.querySelector(".showDetails").addEventListener("click", () => showDetailsWarning(item.entityId));
            
            warningContainer.appendChild(div);
        });
    } else {
        warningContainer.innerHTML = `
            <div class="d-flex flex-column align-items-center pb-4">
                <img src="/Img/no-data.jpg" style="width: clamp(120px, 20vw, 180px)" alt="">
                <div style="color: #506a77;font-size: clamp(13px, 2.5vw, 15px)">هیچ هشداری برای نمایش وجود ندارد!</div>
            </div>
        `;
    }
}
async function removeWarning(id) {
    try {
        const res = await axios(`/Home/DeleteSystemWarning?id=${id}`);
        if (res.status === 200) {
            toastifySuccess("با موفقیت حذف شد!");
            await getAllWarnings();
        }
    } catch (err) {
        toastifyError("یافت نشد!");
        console.log(err);
    }
}
async function showDetailsWarning(id) {
    const res = await axios(`/Factor/GetFactor?id=${id}`);
    const data = res.data;
    const object = {
        factorSerialNumber: data.serialNumber,
        pFactorSerialNumber: data.pfactorSerialNumber,
        pFactorId: data.pfactorID,
    };
    if (!window.location.href.toLowerCase().includes("cartable/index?tab=tprefactorfactorid")) {
        document.getElementById("FLshowLoading").style.display = "";
        localStorage.setItem("warningDetails", JSON.stringify(object));
        window.location.href = `/Cartable/Index`;
    } else {
        $("#warningModal").modal("hide");
        await showPrefac(object.pFactorSerialNumber.toString());
        await showFac(object.factorSerialNumber.toString());
    }
    
}

async function showModalPhonebook() {
    $("#phoneBookModal").modal("show");
    async function loadPhonebook(search = "") {
        try {
            document.getElementById("FLshowLoading").style.display = "";
            const res = await axios(`/Customer/GetPhonebook?search=${search}`);
            
            const phonebookList = document.getElementById("phonebookList");
            phonebookList.innerHTML = "";
            if (res.data.length > 0) {
                res.data.forEach(item => {
                    const div = document.createElement("div");
                    div.className = "box-phoneBook";
                    div.innerHTML = `
                        <div class="d-flex flex-column gap-2 py-1">
                            <div class="fbold" style="color: #333">
                              <svg style="width: 18px" fill="currentColor" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.501 20.118a7.5 7.5 0 0 1 14.998 0A17.933 17.933 0 0 1 12 21.75c-2.676 0-5.216-.584-7.499-1.632Z" /></svg>
                              <span class="hoverLink" onclick="openCustomerProfile('${item.id}')">
                                ${item.name} (${item.customerCode})
                              </span>
                            </div>
                            <div class="text-muted small d-flex align-items-center">
                                ${item.mobile ? `
                                <span class="d-flex align-items-center gap-1">
                                    <svg style="width: 18px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M10.5 1.5H8.25A2.25 2.25 0 0 0 6 3.75v16.5a2.25 2.25 0 0 0 2.25 2.25h7.5A2.25 2.25 0 0 0 18 20.25V3.75a2.25 2.25 0 0 0-2.25-2.25H13.5m-3 0V3h3V1.5m-3 0h3m-3 18.75h3" /></svg>
                                    <a href="tel:${item.mobile}" class="text-muted" target="_self">${item.mobile}</a>
                                </span>
                              ` : ""}
                                ${item.phone ? `
                                <span class="d-flex align-items-center gap-1 ms-2">
                                    <svg style="width: 18px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M2.25 6.75c0 8.284 6.716 15 15 15h2.25a2.25 2.25 0 0 0 2.25-2.25v-1.372c0-.516-.351-.966-.852-1.091l-4.423-1.106c-.44-.11-.902.055-1.173.417l-.97 1.293c-.282.376-.769.542-1.21.38a12.035 12.035 0 0 1-7.143-7.143c-.162-.441.004-.928.38-1.21l1.293-.97c.363-.271.527-.734.417-1.173L6.963 3.102a1.125 1.125 0 0 0-1.091-.852H4.5A2.25 2.25 0 0 0 2.25 4.5v2.25Z" /></svg>
                                    <a href="tel:${item.phone}" class="text-muted" target="_self">${item.phone}</a>
                                </span>
                              ` : ""}
                            </div>
                        </div>
                    `;
                    phonebookList.appendChild(div);
                });
                
            } else {
                phonebookList.innerHTML = `<div class="p-3 text-muted text-center">هیچ نتیجه‌ای یافت نشد.</div>`;
            }
            document.getElementById("FLshowLoading").style.display = "none";
        } catch (err) {
            console.log(err);
        }
    }
    document.getElementById("PhoneBookSearchInp").addEventListener("keyup", async (e) => {
        if (e.key === "Enter") {
            await loadPhonebook(e.target.value);
        }
    });
    
    await loadPhonebook();
}

const showAttachments = async (id, typeAtt) => {
    const showAttModal = document.getElementById("showAttachmentsModal");
    const containerPreview = showAttModal.querySelector(".image-display");
    if (!containerPreview) return;
    containerPreview.innerHTML = "";
    try {
        FLshowLoading();
        const files = (await axios(`/Factor/GetAttachment/${id}?type=${typeAtt}`)).data;
        
        if (files.length) {
            files.forEach(fileItem => {
                const imageBox = createImageBox(fileItem);
                containerPreview.appendChild(imageBox);
            });
            containerPreview.parentElement.style.backgroundColor = "#fafafa";
        } else {
            containerPreview.innerHTML = `
                <div class="d-flex flex-column align-items-center pb-4">
                    <img src="/Img/no-data.jpg" style="width: clamp(120px, 20vw, 180px)" alt="">
                    <div style="color: #506a77;font-size: clamp(13px, 2.5vw, 15px)">هیچ پیوستی برای نمایش وجود ندارد!</div>
                </div>
            `;
            containerPreview.parentElement.style.backgroundColor = "#fff";
        }
        
    } catch (err) {
        containerPreview.innerHTML = "خطای دریافت اطلاعات!";
        console.log(err);
    } finally {
        FLshowLoading(false);
        ModalToggle(showAttModal);
    }
};

const preFactorListModal = document.getElementById("preFactorListModal");
let accType_pfList;
async function showModalPreFactorList(accType) {
    accType_pfList = accType;
    await getAllPreFactorList();
    preFactorListModal.querySelector("#preFactorListSearch").value = "";
    ModalToggle(preFactorListModal);
}

const getAllPreFactorList = async (search = "") => {
    const res = await axios(`/Factor/GetAllPreFactorList?search=${search}`);
    
    preFactorListModal.querySelector("#preFactorListContainer").innerHTML = res.data;
};

preFactorListModal?.querySelector("#preFactorListSearch")?.addEventListener("keyup", async (e) => {
    if (e.key === "Enter") {
        toggleElementsDisabled(e.target);
        await getAllPreFactorList(e.target.value.trim());
        toggleElementsDisabled(e.target, false);
        e.target.focus();
    }
});


async function selectPfFromPfListModal(pfId) {
    InvoiceType = 1;
    pfactorID = pfId;
    accType_pfList === 1 ? await ShowModalAddInvoice() : await ShowModalInvoice(accType_pfList);
}


/*--------------------------------------------------------------------------------*/

async function getOrderHeadIdsBySupplier(supplierId) {
    try {
        const res = await axios(`/Supply/GetOrderHeadsBySupplier?supplierId=${supplierId}`);
        if (res.status === 200) {
            return res.data;
        }
        return [];
    } catch (err) {
        console.log(err);
        return [];
    }
}

async function getSupplierFinancialSummary(supplierId) {
    try {
        const res = await axios(`/Supply/GetSupplierFinancialSummary?supplierId=${supplierId}`);
        if (res.status === 200) {
            return res.data;
        }
        return null;
    } catch (err) {
        console.log(err);
        return null;
    }
}

/*--------------------------------------------------------------------------------*/

/*document.addEventListener("input", (e) => {
    if (e.target.matches("input, textarea")) {
        e.target.value = toEnglishDigits(e.target.value);
    }
});*/

document.addEventListener("input", (e) => {
    const el = e.target;
    
    if (!(el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement))
        return;
    
    const allowedTypes = ["text", "search", "tel", "url", "password", "email"];
    
    if (el instanceof HTMLInputElement && !allowedTypes.includes(el.type))
        return;
    
    const newValue = toEnglishDigits(el.value);
    
    if (el.value !== newValue) {
        const start = el.selectionStart;
        const end = el.selectionEnd;
        
        el.value = newValue;
        
        /* if (start !== null && end !== null) {
             el.setSelectionRange(start, end);
         }*/
    }
});


const audioModal = document.getElementById("audioModal");
const playerAudioModal = audioModal?.querySelector("audio");
const sourceAudioModal = audioModal?.querySelector("source");

function playAudio(src) {
    sourceAudioModal.src = src;
    playerAudioModal.load();
    
    ModalToggle(audioModal);
}
audioModal?.addEventListener("hidden.bs.modal", () => {
    playerAudioModal.pause();
    playerAudioModal.currentTime = 0;
    sourceAudioModal.src = "";
});

function showModalSetting() {
    toggleMobileDrawer(false);
    ModalToggle("BaseSetting");
}

function showDetailsLog(changeDetails, commentTxt) {
    document.getElementById("ShowDetailsLog").innerHTML = changeDetails || commentTxt;
    ModalToggle("#ShowDetailsLogModal");
}

async function showLogEntity(entityName, entityId) {
    const data = await getLogEntity(entityName, entityId);
    if (!data) return;
    
    const container = document.getElementById("logContainer");
    container.innerHTML = "";
    const dom = createDomLogSystem(data);
    container.appendChild(dom);
    
    ModalToggle("logModal");
}

async function getLogEntity(entityName, entityId) {
    try {
        FLshowLoading();
        const res = await axios(`/Home/GetEntityLogSystem?entityName=${entityName}&entityId=${entityId}`);
        if (res.status === 200) {
            return res.data;
        }
        return false;
    } catch (err) {
        console.log(err);
        toastifyError("خطای سرور");
        return false;
    } finally {
        FLshowLoading(false);
    }
}

function createDomLogSystem(data) {
    if (!data) return;
    
    const container = document.createElement("div");
    container.className = "logSystem-container";
    
    const ul = document.createElement("ul");
    data.forEach(item => {
        const li = document.createElement("li");
        li.classList.add(`logAction-${item.actionType}`);
        li.innerHTML = `
            <span class="logSystem-time">${item.changeTimeTxt.split(" ").join(" | ")}</span>
            <p class="logSystem-desc">
                <span>${item.description || ""}</span>
                ${(item.changeDetails || item.commentTxt) ? `
                    <span class="showDetailLog-icon">
                        <svg style="width: 14px" viewBox="0 0 16 16" fill="currentColor"><path fill-rule="evenodd" d="M15 8A7 7 0 1 1 1 8a7 7 0 0 1 14 0ZM9 5a1 1 0 1 1-2 0 1 1 0 0 1 2 0ZM6.75 8a.75.75 0 0 0 0 1.5h.75v1.75a.75.75 0 0 0 1.5 0v-2.5A.75.75 0 0 0 8.25 8h-1.5Z" clip-rule="evenodd" /></svg>
                    </span>
                ` : ""}
            </p>
        `;
        li.querySelector(".showDetailLog-icon")?.addEventListener("click", () => showDetailsLog(item.changeDetails, item.commentTxt));
        ul.appendChild(li);
    });
    
    container.appendChild(ul);
    return container;
}

function showAgentReviewText(btn) {
    btn.closest(".agent-review-box")?.classList.toggle("open");
}