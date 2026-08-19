
let targetUserId;
var smsSendedPfId;
let f_type = 0;

let filConfigSession = null;

let pfType = 0;//ریدیو باتن پیش فاکتور
let fType = 0;//ریدیو باتن فاکتور

let fromWarning = false;
document.addEventListener("UserLoginReady", () => {
    const rols = JSON.parse(sessionStorage.getItem("Rols")) || [];
    rols.forEach(item => ShowActiveCartable(item.name));
});

document.addEventListener("DOMContentLoaded", async () => {
    showTaminDropdown();
    
    //////////////////////preFactor//////////////////////
    initSearchInput("searchId", async (e, inp) => await GetPreFactorPartial(inp.value, modelPreFactor._type, modelPreFactor.filt));
    initChangeRadio("financeFilter", async (e) => await GetPreFactorPartial(modelPreFactor.serVal, Number(e.target.value), modelPreFactor.filt));
    
    //////////////////////factor//////////////////////
    initSearchInput("f_searchId", async (e, inp) => await GetFactorPartial(inp.value, null, null, modelFactor.filt, modelFactor._type));
    initChangeRadio("f_type", async (e) => await GetFactorPartial(modelFactor.serVal, modelFactor.EntityId, null, modelFactor.filt, Number(e.target.value)));
    
    //////////////////////saleAdmin//////////////////////
    initSearchInput("SalesAdminSearchId", GetPreFactorSalesAdmin);
    initChangeRadio("SalesAdminFilter", GetPreFactorSalesAdmin);
    
    //////////////////////tamin pf//////////////////////
    initSearchInput("TSearchId", GetPreFactorTaminExpert);
    initChangeRadio("TFilter", GetPreFactorTaminExpert);
    
    //////////////////////tamin inv//////////////////////
    initSearchInput("taminInvoiceSearchId", GetInvoicesTaminExpert);
    initChangeRadio("taminInvoiceFilter", GetInvoicesTaminExpert);
    
    //////////////////////financial pf//////////////////////
    initSearchInput("FinancialExpertSearchId", GetPreFactorFinancialExpert);
    initChangeRadio("financialExpertFilter", GetPreFactorFinancialExpert);
    
    //////////////////////financial inv//////////////////////
    initSearchInput("FinancialExpertInvoiceSearchId", GetInvoicesFinancialExpert);
    initChangeRadio("financialExpertInvoiceFilter", GetInvoicesFinancialExpert);
    
    //////////////////////financial req//////////////////////
    initSearchInput("FinancialRequestSearchId", GetRequestFinancialExpert);
    initChangeRadio("FinancialRequestFilter", GetRequestFinancialExpert);
    
    //////////////////////financial deal//////////////////////
    initSearchInput("FinancialDealSearchId", GetDealFinancialExpert);
    initChangeRadio("FinancialDealFilter", GetDealFinancialExpert);
    
    //////////////////////MD req//////////////////////
    initSearchInput("MDRequestAmountSearchId", GetRequestManagingDirector);
    initChangeRadio("MDRequestAmountFilter", GetRequestManagingDirector);
    
    //////////////////////MD transfer//////////////////////
    initSearchInput("MDTransferSearchId", GetTransferManagingDirector);
    initChangeRadio("MDTransferFilter", GetTransferManagingDirector);
    
    //////////////////////////////////////////////////
    
    const fFilterFromDash = JSON.parse(sessionStorage.getItem("fFilterFromDash"));
    if (fFilterFromDash) {
        sessionStorage.removeItem("fFilterFromDash");
        filConfigSession = {
            ContentFilterDiv: "f_ContentFilterId",
            filterCallBack: "CbFFilter",
            txtSercheId: "f_searchId",
            varSearch: "",
            filterVar: {
                ...fFilterFromDash.filt,
                
            },
            filterVarConfig: "varList2",
        };
    }
    
    const warningDetailsObj = JSON.parse(localStorage.getItem("warningDetails"));
    if (warningDetailsObj) {
        fromWarning = true;
        localStorage.removeItem("warningDetails");
        await showPrefac(warningDetailsObj.pFactorSerialNumber.toString());
        await showFac(warningDetailsObj.factorSerialNumber.toString());
    }
    
    
    //const tabId = location.hash?.split("=")[1];
    const { tab = "TPrefactorFactorId", sub } = getUrlParams();
    if (document.getElementById(tab)) {
        GetCartable(tab, sub);
    }
    
});

const CarPfExportExcel = () => {
    $("#FLshowLoading").css("display", "");
    
    const model = {};
    model.serVal = CartablePfSearchVal;
    model._type = Number(document.querySelector("input[type='radio'][name='financeFilter']:checked").value);
    
    if (window[filterVarConfig] != null)
        model.filt = window[filterVarConfig].filterVar;
    else
        model.filt = null;
    
    fetch("/Cartable/PrefactorExportToExcel", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(model),
        
        
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            } else {
                $("#FLshowLoading").css("display", "none");
                return response.blob(); // تبدیل پاسخ به Blob
                
            }
        })
        .then(blob => {
            const url = window.URL.createObjectURL(blob); // ایجاد URL برای Blob
            const a = document.createElement("a");
            a.href = url;
            a.download = "PrefactorsExcel.xlsx"; // نام و فرمت دلخواه فایل
            document.body.appendChild(a);
            a.click();
            a.remove();
            window.URL.revokeObjectURL(url); // پاکسازی URL Blob پس از دانلود
        })
        .catch(error => {
            console.error("Error:", error);
        });
    
};
const CarFExportExcel = () => {
    
    $("#FLshowLoading").css("display", "");
    
    const model = {};
    model.serVal = CartableFSearchVal;
    model._type = Number(document.querySelector("input[type='radio'][name='f_type']:checked").value);
    if (window[filterVarConfig] != null)
        model.filt = window[filterVarConfig].filterVar;
    else
        model.filt = null;
    
    fetch("/Cartable/FactorExportToExcel", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(model),
        
        
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            } else {
                $("#FLshowLoading").css("display", "none");
                return response.blob(); // تبدیل پاسخ به Blob
                
            }
        })
        .then(blob => {
            const url = window.URL.createObjectURL(blob); // ایجاد URL برای Blob
            const a = document.createElement("a");
            a.href = url;
            a.download = "factorsExcel.xlsx"; // نام و فرمت دلخواه فایل
            document.body.appendChild(a);
            a.click();
            a.remove();
            window.URL.revokeObjectURL(url); // پاکسازی URL Blob پس از دانلود
        })
        .catch(error => {
            console.error("Error:", error);
        });
    
};

const CA_AcceptPfactor = async (id) => {
    //$("#BtnSendToId").css("display", "none");
    //$("#loadingSendToId").css("display", "");
    $("#FLshowLoading").css("display", "");
    $.get(baseUrl + "Factor/AcceptPreFactorManual/" + id, (res) => {
        toastifySuccess("پیش فاکتور تایید و به واحد مالی ارسال شد.");
        
        GetSelesExpertPartial();
        $("#FLshowLoading").css("display", "none");
    })
     .fail(() => {
         toastifyError("خطا در انجام عملیات");
         $("#FLshowLoading").css("display", "none");
     });
    //$("#BtnSendToId").css("display", "");
    //$("#loadingSendToId").css("display", "none");
};

const CA_HandAccepting = async (id) => {
    await CA_AcceptPfactor(id);
};


const CA_SendToCustomer = () => {
    
    $("#CA_MobileModal").modal("toggle");
    $("#FLshowLoading").css("display", "");
    var toNumber = $("#mobileid").val();
    var id = smsSendedPfId;
    
    $.get(baseUrl + `Factor/SendToCustomer?id=${id}&toNumber=${toNumber}`, (res) => {
        GetSelesExpertPartial();
        $("#FLshowLoading").css("display", "none");
    })
     .fail(() => {
         setTimeout(() => {
             CA_SendToCustomer();
         }, 2000);
     });
};

const CA_showMobileModal = (id) => {
    
    $("#CA_MobileModal").modal("show");
    smsSendedPfId = id;
    
    $.get(baseUrl + "factor/GetCustomerPhone/" + id, (res) => {
        $("#mobileid").val(res);
    })
     .fail(() => {
         setTimeout(() => CA_showMobileModal(id), 3000);
     });
};


const HideAllCartable = () => {
    document.getElementById("CPrefactorFactorId").style.display = "none";
    document.getElementById("CFinancialAdminId").style.display = "none";
    document.getElementById("CFinancialExpertId").style.display = "none";
    document.getElementById("CSalesExpertId").style.display = "none";
    document.getElementById("CSalesAdminId").style.display = "none";
    document.getElementById("CTaminExpertId").style.display = "none";
    document.getElementById("CManagingDirectorId").style.display = "none";
};


const ShowActiveCartable = (name) => {
    switch (name) {
        case "Admin":
            document.getElementById("TPrefactorFactorId").style.display = "flex";
            //document.getElementById('TFinancialAdminId').style.display = "flex";
            document.getElementById("TFinancialExpertId").style.display = "flex";
            document.getElementById("TSalesExpertId").style.display = "flex";
            document.getElementById("TSalesAdminId").style.display = "flex";
            document.getElementById("TTaminExpertId").style.display = "flex";
            document.getElementById("TManagingDirectorId").style.display = "flex";
            
            break;
        case "FinancialExpert":
            document.getElementById("TPrefactorFactorId").style.display = "flex";
            document.getElementById("TFinancialExpertId").style.display = "flex";
            break;
        
        case "FinancialAdmin":
            //document.getElementById('TFinancialAdminId').style.display = "flex";
            break;
        
        case "SalesExpert":
            document.getElementById("TPrefactorFactorId").style.display = "flex";
            document.getElementById("TSalesExpertId").style.display = "flex";
            break;
        
        case "SalesAdmin":
            document.getElementById("TPrefactorFactorId").style.display = "flex";
            document.getElementById("TSalesAdminId").style.display = "flex";
            break;
        
        case "TaminExpert":
        case "TaminAdmin":
            document.getElementById("TPrefactorFactorId").style.display = "flex";
            document.getElementById("TTaminExpertId").style.display = "flex";
            break;
        
        
        case "ManagingDirector":
            document.getElementById("TPrefactorFactorId").style.display = "flex";
            document.getElementById("TTaminExpertId").style.display = "flex";
            document.getElementById("TManagingDirectorId").style.display = "flex";
            break;
        
    }
};

const GetCartable = (tabId, subId) => {
    setUrlParams({ tab: tabId, sub: subId });
    HideAllCartable();
    document.querySelectorAll(".TCar").forEach(async (item) => {
        if (item.id === tabId) {
            item.classList.add("TCarActive");
            switch (tabId) {
                case "TPrefactorFactorId":
                    const promises = [];
                    if (filConfigSession?.filterVar) {
                        promises.push(FilterAccepted(filConfigSession));
                        if (window.innerWidth < 1400) {
                            ShowTab("allFactorTab");
                        }
                    } else if (!fromWarning) {
                        promises.push(GetFactorPartial(modelFactor.serVal, null, null, modelFactor.filt, modelFactor._type, null, 0, null, false));
                    }
                    
                    if (!fromWarning) {
                        promises.push(GetPreFactorPartial(modelPreFactor.serVal, modelPreFactor._type, modelPreFactor.filt, false));
                    }
                    
                    FLshowLoading();
                    await Promise.all(promises);
                    FLshowLoading(false);
                    
                    document.getElementById("CPrefactorFactorId").style.display = "flex";
                    removeTabAllFactorsDesktop();
                    break;
                
                case "TFinancialExpertId":
                    document.getElementById("CFinancialExpertId").style.display = "flex";
                    if (subId === "FinancialInvoiceTab") {
                        await GetInvoicesFinancialExpert();
                    } else if (subId === "FinancialRequestTab") {
                        await GetRequestFinancialExpert();
                    } else if (subId === "FinancialDealTab") {
                        await GetDealFinancialExpert();
                    } else {
                        await GetPreFactorFinancialExpert();
                    }
                    break;
                
                
                case "TFinancialAdminId":
                    document.getElementById("CFinancialAdminId").style.display = "flex";
                    break;
                
                case "TSalesExpertId":
                    document.getElementById("CSalesExpertId").style.display = "flex";
                    await GetSelesExpertPartial();
                    break;
                
                case "TSalesAdminId":
                    document.getElementById("CSalesAdminId").style.display = "flex";
                    await GetPreFactorSalesAdmin();
                    break;
                
                case "TTaminExpertId":
                    document.getElementById("CTaminExpertId").style.display = "flex";
                    if (subId === "TaminInvoiceTab") {
                        await GetInvoicesTaminExpert();
                    } else {
                        await GetPreFactorTaminExpert();
                    }
                    break;
                
                case "TManagingDirectorId":
                    document.getElementById("CManagingDirectorId").style.display = "flex";
                    if (subId === "MDTransferTab") {
                        await GetTransferManagingDirector();
                    } else {
                        await GetRequestManagingDirector();
                    }
                    break;
            }
        } else {
            document.getElementById(item.id).classList.remove("TCarActive");
        }
    });
};

const GetPreFactorPartial = async (val = null, type = 0, filt = null, showLoading = true) => {
    modelPreFactor.serVal = val?.trim();
    modelPreFactor._type = type;
    modelPreFactor.filt = filt;
    
    CartablePfSearchVal = val;
    
    try {
        showLoading && FLshowLoading();
        const res = await axios.post(`/Cartable/PreFactorAcceptedPartial`, modelPreFactor);
        if (res.status === 200) {
            document.getElementById("PfAcceptedList").innerHTML = res.data.html;
            
            document.getElementById("CountId").innerText = res.data.count;
            document.getElementById("CountMobileId").innerText = res.data.count;
        }
        
        showLoading && FLshowLoading(false);
    } catch (e) {
        console.log(e);
    }
};

const GetSelesExpertPartial = async () => {
    try {
        FLshowLoading();
        const res = await axios(`/Cartable/GetSelesExpertPartial`);
        if (res.status === 200) {
            document.getElementById("CSalesExpertId").innerHTML = res.data;
            removeSalesExpertsTabsDesktop();
            FLshowLoading(false);
        }
        
    } catch (err) {
        console.log(err);
    }
};

const GetPreFactorSalesAdmin = async () => {
    const val = document.getElementById("SalesAdminSearchId").value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='SalesAdminFilter']:checked").value;
    if (type === undefined) return;
    try {
        FLshowLoading();
        const res = await axios(`/Cartable/GetPreFactorSalesAdmin?serVal=${val}&type=${type}`);
        if (res.status === 200) {
            document.getElementById("SalesAdminPfactorContentId").innerHTML = res.data.html;
            document.getElementById("S_CountId").textContent = res.data.count;
            document.getElementById("SalesAdminFooterId").classList.toggle("d-none", !res.data.count);
            updateFilterSwitchState({
                wrapperSelector: "#CSalesAdminId",
                filterData: res.data.filterCounts,
                currentFilterValue: type,
                searchValue: val,
                onChange: async () => {
                    await GetPreFactorSalesAdmin();
                },
            });
            FLshowLoading(false);
        }
        
    } catch (err) {
        console.log(err);
    }
    
};


const GetPreFactorTaminExpert = async () => {
    const val = document.getElementById("TSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='TFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "TaminPrefactorTab" });
    ShowTab("#TaminPrefactorTab");
    try {
        FLshowLoading();
        const res = await axios(`/Cartable/GetPreFactorTaminExpert?serVal=${val}&type=${type}`);
        if (res.status === 200) {
            document.getElementById("TContentId").innerHTML = res.data.html;
            document.getElementById("T_CountId").textContent = res.data.count;
            updateFilterSwitchState({
                wrapperSelector: "#TaminPrefactorTab-pane",
                filterData: res.data.filterCounts,
                currentFilterValue: type,
                searchValue: val,
                onChange: async () => {
                    await GetPreFactorTaminExpert();
                },
            });
            FLshowLoading(false);
        }
        
    } catch (err) {
        console.log(err);
    }
};


const GetInvoicesTaminExpert = async () => {
    const val = document.getElementById("taminInvoiceSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='taminInvoiceFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "TaminInvoiceTab" });
    ShowTab("#TaminInvoiceTab");
    try {
        FLshowLoading();
        const res = await axios(`/Accountancy/GetAllPaymentCartablePartial?serVal=${val}&type=${type}&cartable=3`);
        const data = res.data;
        document.getElementById("taminInvoiceContentId").innerHTML = data.html;
        document.getElementById("taminInvoiceCount").textContent = data.count;
        updateFilterSwitchState({
            wrapperSelector: "#TaminInvoiceTab-pane",
            filterData: res.data.filterCounts,
            currentFilterValue: type,
            searchValue: val,
            onChange: async () => {
                await GetInvoicesTaminExpert();
            },
        });
        initPaymentPartial(56);
        FLshowLoading(false);
    } catch (err) {
        console.log(err);
    }
};


const GetRequestManagingDirector = async () => {
    const val = document.getElementById("MDRequestAmountSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='MDRequestAmountFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "MDRequestAmountTab" });
    ShowTab("#MDRequestAmountTab");
    try {
        FLshowLoading();
        const res = await axios(`/RequestAmount/GetAllRequestAmountsPartial?search=${val}&type=${type}&passType=2`);
        if (res.status === 200) {
            const data = res.data;
            document.getElementById("MDRequestAmountContentId").innerHTML = data.html;
            document.getElementById("MDRequestAmountCount").textContent = data.count;
            updateFilterSwitchState({
                wrapperSelector: "#MDRequestAmountTab-pane",
                filterData: res.data.filterCounts,
                currentFilterValue: type,
                searchValue: val,
                onChange: async () => {
                    await GetRequestManagingDirector();
                },
            });
            
        } else if (res.status === 204) {
            toastifyError("شما به این بخش دسترسی ندارید");
        }
    } catch (err) {
        console.log(err);
    } finally {
        FLshowLoading(false);
    }
};


const GetTransferManagingDirector = async () => {
    const val = document.getElementById("MDTransferSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='MDTransferFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "MDTransferTab" });
    ShowTab("#MDTransferTab");
    
    transferModel._type = Number(type);
    transferModel.serVal = val;
    
    if (transferTbl) {
        transferTbl.ajax.reload();
    } else {
        getAllTransfersDt("#mdTransferTable", true);
    }
    
};

function changePercentAcceptedRequestAmount(select, amount, acceptedAmount) {
    const percent = Number(select.value);
    const input = select.parentElement.querySelector("input");
    const acceptAmount = percent * amount / 100;
    input.value = acceptAmount ? acceptAmount.toLocaleString() : "";
    updateCurrencyHint(input);
    
    select.style.backgroundColor = acceptAmount ? "#cef5ce" : "";
    toggleAcceptBtn(select, acceptAmount, acceptedAmount);
}

function changeAcceptedRequestAmount(input, amount, acceptedAmount) {
    setTimeout(() => {
        const inputValue = Number(SeparatedNumberRemove(input.value)) || 0;
        const select = input.parentElement.querySelector("select");
        const percent = Number(select.value);
        if (percent * amount / 100 !== inputValue) {
            select.value = "";
            select.style.backgroundColor = "";
        }
        toggleAcceptBtn(input, inputValue, acceptedAmount);
    });
}

function toggleAcceptBtn(elm, acceptAmount, acceptedAmount) {
    const acceptBtn = elm.closest(".layered-card").querySelector(".buttonGreen2");
    if (acceptAmount === 0) {
        toggleElementsDisabled(acceptBtn, true);
    } else {
        /*if (acceptedAmount === acceptAmount) {
            toggleElementsDisabled(acceptBtn, true);
        } else {*/
        toggleElementsDisabled(acceptBtn, false);
        // }
    }
}

async function submitAcceptedRequestAmount(btn, id) {
    const card = btn.closest(".layered-card");
    const input = card.querySelector("input.acceptedAmount");
    const selOpPercent = card.querySelector("select.acceptedAmountPercent");
    
    const model = {
        id,
        acceptedAmount: Number(SeparatedNumberRemove(input.value)),
        acceptedAmountPercent: selOpPercent.value ? Number(selOpPercent.value) : null,
    };
    
    try {
        FLshowLoading();
        const res = await axios.post(`/RequestAmount/SetAcceptedRequestAmount`, model);
        if (res.status === 200) {
            await GetRequestManagingDirector();
        }
    } catch (err) {
        console.log(err);
        toastifyError("خطای سرور");
    } finally {
        FLshowLoading(false);
    }
}

const GetPreFactorFinancialExpert = async () => {
    const val = document.getElementById("FinancialExpertSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='financialExpertFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "FinancialPrefactorTab" });
    ShowTab("#FinancialPrefactorTab");
    try {
        FLshowLoading();
        const res = await axios(`/Cartable/GetPreFactorFinancialExpert?serVal=${val}&type=${type}`);
        if (res.status === 200) {
            document.getElementById("FinancialExpertPfContentId").innerHTML = res.data.html || "";
            document.getElementById("FinancialExpertCount").textContent = res.data.count;
            updateFilterSwitchState({
                wrapperSelector: "#FinancialPrefactorTab-pane",
                filterData: res.data.filterCounts,
                currentFilterValue: type,
                searchValue: val,
                onChange: async () => {
                    await GetPreFactorFinancialExpert();
                },
            });
            FLshowLoading(false);
        }
    } catch (err) {
        console.log(err);
    }
};


const GetInvoicesFinancialExpert = async () => {
    const val = document.getElementById("FinancialExpertInvoiceSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='financialExpertInvoiceFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "FinancialInvoiceTab" });
    ShowTab("#FinancialInvoiceTab");
    try {
        FLshowLoading();
        
        const res = await axios(`/Accountancy/GetAllPaymentCartablePartial?serVal=${val}&type=${type}&cartable=7`);
        const data = res.data;
        document.getElementById("FinancialExpertInvoiceContentId").innerHTML = data.html;
        document.getElementById("FinancialExpertInvoiceCount").textContent = data.count;
        updateFilterSwitchState({
            wrapperSelector: "#FinancialInvoiceTab-pane",
            filterData: res.data.filterCounts,
            currentFilterValue: type,
            searchValue: val,
            onChange: async () => {
                await GetInvoicesFinancialExpert();
            },
        });
        initPaymentPartial(55);
        FLshowLoading(false);
    } catch (err) {
        console.log(err);
    }
};


const GetRequestFinancialExpert = async () => {
    const val = document.getElementById("FinancialRequestSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='FinancialRequestFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "FinancialRequestTab" });
    ShowTab("#FinancialRequestTab");
    try {
        FLshowLoading();
        const res = await axios(`/RequestAmount/GetAllRequestAmountsPartial?search=${val}&type=${type}&passType=3`);
        const data = res.data;
        document.getElementById("FinancialRequestContentId").innerHTML = data.html;
        document.getElementById("FinancialRequestCount").textContent = data.count;
        updateFilterSwitchState({
            wrapperSelector: "#FinancialRequestTab-pane",
            filterData: res.data.filterCounts,
            currentFilterValue: type,
            searchValue: val,
            onChange: async () => {
                await GetRequestFinancialExpert();
            },
        });
        requestSelection.restoreSelection();
        FLshowLoading(false);
    } catch (err) {
        console.log(err);
    }
};

let dealTbl;
let dealModel = { filt: null, serVal: null };
const GetDealFinancialExpert = async () => {
    const val = document.getElementById("FinancialDealSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='FinancialDealFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "FinancialDealTab" });
    ShowTab("#FinancialDealTab");
    
    dealModel._type = Number(type);
    dealModel.serVal = val;
    
    if (dealTbl) {
        dealTbl.ajax.reload();
    } else {
        getAllDealsDt();
    }
};

function getAllDealsDt() {
    const dealTableElm = document.getElementById("DealTable");
    
    dealTbl = $(dealTableElm).DataTable({
        paging: false,
        lengthMenu: [10, 25, 50, 100, { label: "همه", value: -1 }],
        pageLength: 10,
        processing: true,
        serverSide: true,
        search: {
            return: true,
        },
        ordering: true,
        order: [[3, "desc"]],
        filter: true,
        stateSave: false,
        scrollCollapse: true,
        scrollY: "clamp(150px, calc(100dvh - 280px), 100dvh)",
        responsive: true,
        ajax: {
            url: `/Deal/GetAllDealsDt`,
            type: "POST",
            data: (d) => {
                d.model = JSON.stringify(dealModel);
            },
            dataSrc: function (json) {
                document.getElementById("FinancialDealCount").textContent = json.recordsFiltered;
                
                updateFilterSwitchState({
                    wrapperSelector: "#FinancialDealTab-pane",
                    filterData: json.filterCounts,
                    currentFilterValue: dealModel._type,
                    searchValue: dealModel.serVal,
                    onChange: (type) => {
                        dealModel._type = Number(type);
                        dealTbl.ajax.reload();
                    },
                });
                
                return json.data;
            },
        },
        columnDefs: [
            { targets: "_all", defaultContent: "-" },
        ],
        columns: [
            { data: "serialNumber" },
            
            { data: "title", className: "wrap-text" },
            { data: "commentText", className: "wrap-text", render: data => data || "-" },
            { data: "createDateTxt", render: data => renderDateTimeHtml(data) },
            { data: "userCreator" },
            {
                data: "customerName",
                className: "wrap-text",
                render: function (data, type, row) {
                    return `${data ?? ""}${row.customerCode ? " (کد مشتری: " + row.customerCode + ")" : ""}`;
                },
            },{
                data: "status",
                render: data => {
                    if (data === 1) return `<span class="fMedium" style="color: #3b82f6">باز</span>`;
                    if (data === 2) return `<span class="fMedium" style="color: #b91c1c">ناموفق</span>`;
                    if (data === 3) return `<span class="fMedium" style="color: #16a34a">موفق</span>`;
                    return "-";
                },
            },
            {
                data: "counts.preFactor",
                orderable: false,
                render: function (data) {
                    if (!data) return "-";
                    
                    return `
                                <span class="deal-document-link" data-action="pre-factors" data-title-right="مشاهده">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M19 17V5a2 2 0 0 0-2-2H4"/><path d="M8 21h12a2 2 0 0 0 2-2v-1a1 1 0 0 0-1-1H11a1 1 0 0 0-1 1v1a2 2 0 1 1-4 0V5a2 2 0 1 0-4 0v2a1 1 0 0 0 1 1h3"/></svg>
                                    <span>${data}</span>
                                </span>
                            `;
                },
            },
            {
                data: "counts.factor",
                orderable: false,
                render: function (data) {
                    if (!data) return "-";
                    
                    return `
                                <span class="deal-document-link" data-action="factors" data-title-right="مشاهده">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M15 12h-5"/><path d="M15 8h-5"/><path d="M19 17V5a2 2 0 0 0-2-2H4"/><path d="M8 21h12a2 2 0 0 0 2-2v-1a1 1 0 0 0-1-1H11a1 1 0 0 0-1 1v1a2 2 0 1 1-4 0V5a2 2 0 1 0-4 0v2a1 1 0 0 0 1 1h3"/></svg>
                                    <span>${data}</span>
                                </span>
                            `;
                },
            },
            {
                data: "counts.accountancy",
                orderable: false,
                render: function (data) {
                    if (!data) return "-";
                    
                    return `
                                <span class="deal-document-link" data-action="invoices" data-title-right="مشاهده">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M12 18H4a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5"/><path d="M18 12h.01"/><path d="M19 22v-6"/><path d="m22 19-3-3-3 3"/><path d="M6 12h.01"/><circle cx="12" cy="12" r="2"/></svg>
                                    <span>${data}</span>
                                </span>
                            `;
                },
            },
            {
                data: "counts.orderHead",
                orderable: false,
                render: function (data) {
                    if (!data) return "-";
                    
                    return `
                                <span class="deal-document-link" data-action="orders" data-title-right="مشاهده">
                                    <svg fill="currentColor" viewBox="0 0 256 256"><path d="M72,104a8,8,0,0,1,8-8h96a8,8,0,0,1,0,16H80A8,8,0,0,1,72,104Zm8,40h96a8,8,0,0,0,0-16H80a8,8,0,0,0,0,16ZM232,56V208a8,8,0,0,1-11.58,7.15L192,200.94l-28.42,14.21a8,8,0,0,1-7.16,0L128,200.94,99.58,215.15a8,8,0,0,1-7.16,0L64,200.94,35.58,215.15A8,8,0,0,1,24,208V56A16,16,0,0,1,40,40H216A16,16,0,0,1,232,56Zm-16,0H40V195.06l20.42-10.22a8,8,0,0,1,7.16,0L96,199.06l28.42-14.22a8,8,0,0,1,7.16,0L160,199.06l28.42-14.22a8,8,0,0,1,7.16,0L216,195.06Z"></path></svg>
                                    <span>${data}</span>
                                </span>
                            `;
                },
            },
            {
                data: "counts.report",
                orderable: false,
                render: function (data) {
                    if (!data) return "-";
                    
                    return `
                                <span class="deal-document-link" data-action="reports" data-title-right="مشاهده">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M8 2v4"/><path d="M12 2v4"/><path d="M16 2v4"/><rect width="16" height="18" x="4" y="4" rx="2"/><path d="M8 10h6"/><path d="M8 14h8"/><path d="M8 18h5"/></svg>
                                    <span>${data}</span>
                                </span>
                            `;
                },
            },
            {
                data: "id",
                orderable: false,
                render: function (data, type, row) {
                    if (row.financialStatus === 0) {
                        return `<button class="buttonCancel3 btn-accept" onclick="closeDeal('${data}')">تایید سفارش</button>`;
                    } else if (row.financialStatus === 1) {
                        return `<button class="buttonGreen2 btn-accept" onclick="activeDeal('${data}')">بازگشت</button>`;
                    }
                    
                    return "-";
                },
            },
        ],
        createdRow: (row, data) => {
            if (data.financialStatus === 1) {
                row.classList.add("tableRow-bgGreen");
            }
        },
        language: languageDatatable,
        layout: null,
        initComplete: () => {
            document.querySelectorAll("#DealTable_wrapper > div.row").forEach((row) => {
                row.classList.add("d-flex", "flex-wrap");
                row.classList.remove("row");
            });
        },
    });
    
    
    dealTableElm.addEventListener("click", (e) => {
        const documentLink = e.target.closest(".deal-document-link");
        if (!documentLink) return;
        
        const rowElement = documentLink.closest("tr");
        const deal = dealTbl.row(rowElement).data();
        
        if (!deal) return;
        
        switch (documentLink.dataset.action) {
            case "pre-factors":
                showModalDealPreFactors(deal);
                break;
            case "factors":
                showModalDealFactors(deal);
                break;
            case "reports":
                showModalDealReports(deal);
                break;
            case "orders":
                showModalDealOrders(deal);
                break;
            case "invoices":
                showModalDealInvoices(deal);
                break;
        }
    });
}


const closeDeal = async (id) => {
    await confirmAlert({
        icon: "warning",
        text: "با تایید این سفارش، کارشناسان قادر به انجام هیچ اقدامی روی آن نخواهند بود.",
        confirmText: "بله، تایید شود",
        onConfirm: async () => await toggleFinancialStatusDeal(id, 1),
    });
};

const activeDeal = async (id) => {
    await toggleFinancialStatusDeal(id, 0);
};


const toggleFinancialStatusDeal = async (id, status) => {
    await sendApiRequest({
        url: `/Deal/ToggleFinancialStatusDeal`,
        params: { id, status },
        successMsg: status === 1 ? "سفارش با موفقیت تایید شد" : "سفارش به حالت فعال بازگشت",
        onSuccess: () => {
            dealTbl.ajax.reload();
        },
    });
};


////////////////////////////////////////////////////////
const requestSelection = new SelectionManager({
    container: "#FinancialRequestContentId",
    selectionBar: "#requestSelectionBar",
    toggleBtn: "#requestSelectionModeBtn",
    itemSelector: ".layered-card",
    checkboxSelector: ".card-checkbox",
    onAction: async (selectedIds) => {
        if (selectedIds.length === 0) {
            toastifyError("لطفاً ابتدا درخواست‌های مورد نظر را انتخاب کنید.");
            return;
        }
        await showSetAmountPaymentItemModal(selectedIds);
    },
});

////////////////////////////////////////////////////////


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
    FLshowLoading();
    taminPreFac = (await axios(`/Factor/GetPFactor?id=${pfId}`)).data;
    
    createSetAmountSalesItem();
    initSalesModal();
    
    FLshowLoading(false);
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
    
    taminPreFac.preFactorItems.forEach((item, index) => {
        const fieldset = document.createElement("fieldset");
        fieldset.id = `setAmountSalesForm-${index}`;
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
                                <span class="flex-shrink-0" style="color: #535353">مبلغ کل: </span>
                                <input type="text" inputmode="numeric" class="currencyNumber form-control inpAmount fbold" style="border: 1px solid #b5b3b3;height: 26px;" name="totalAmount" value="${item.totalAmount ? item.totalAmount.toLocaleString() : ""}" disabled>
                            </div>
                        </div>
                    </div>
                    ${index !== taminPreFac.preFactorItems.length - 1 ? `<div style="width: 95%;height:1px; margin: 10px auto;background-color:darkviolet"></div>` : ""}
                `;
        
        fieldset.querySelector("input[name='unitAmount']").addEventListener("input", (e) => {
            const totalInput = fieldset.querySelector("input[name='totalAmount']");
            totalInput.value = calculateTotalPrice(item.weightCount, e.target.value);
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
    try {
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
        
        FLshowLoading();
        const res = await axios.post(`/Factor/UpdatePrefactorItems`, model);
        if (res.status === 200) {
            FLshowLoading(false);
            return true;
        }
    } catch (err) {
        console.log(err);
        setTimeout(async () => await submitAmountSalesModal(), 3000);
    }
}


/*------------- SUPPLY MODAL -------------*/

const setAmountTaminModal = document.getElementById("setAmountTaminModal");
let suppliesData = [];

const submitSupplyModal = async () => {
    if (!await submitAmountTaminModal()) return;
    
    toggleSupplyModal();
};

const submitAndAcceptPreFactor = async () => {
    if (hasSupplySetting) {
        if (!await submitAmountTaminModal()) return;
    }
    if (!await acceptTaminExpert(taminPreFac.id)) return;
    await GetPreFactorTaminExpert();
    toggleSupplyModal();
};


let hasSupplyAmountSetting;
let hasSupplySetting;
async function showSetSupplyModal(pfId, hasSupplyAmount = true, hasSupply = true) {
    hasSupplyAmountSetting = hasSupplyAmount;
    hasSupplySetting = hasSupply;
    const [{ data: pFactorData }, { data: supplyData }] = await Promise.all([
        axios(`/Factor/GetPFactor?id=${pfId}`),
        axios(`/Supply/GetSupplies?pfId=${pfId}`),
    ]);
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
    
    async function setupSelectOptions() {
        const data = (await axios("/Supply/GetSupplyBaseData")).data;
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
    }
    
    function setupEventListener() {
        const totalPriceInput = addItemSupplyModal.querySelector("#totalAmountMobile");
        const unitPriceInput = addItemSupplyModal.querySelector("#unitAmountMobile");
        const addItemSupplyForm = addItemSupplyModal.querySelector("#addItemSupplyForm");
        
        function updateTotal() {
            totalPriceInput.value = calculateTotalPrice(weightInputSupplyItemModal.value, unitPriceInput.value);
            updateCurrencyHint(totalPriceInput);
        }
        
        if (hasSupplyAmountSetting) {
            unitPriceInput.addEventListener("input", () => {
                updateTotal();
            });
        }
        
        weightInputSupplyItemModal.addEventListener("input", (e) => {
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
        
        
        addItemSupplyForm.addEventListener("submit", (e) => {
            e.preventDefault();
            submitTaminItemFromModal();
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
    try {
        FLshowLoading();
        const res = await axios.post(`/Supply/AddSupply`, model);
        if (res.status === 200) {
            toastifySuccess("بهای تامین با موفقیت ثبت شد");
            GetPreFactorTaminExpert();
            FLshowLoading(false);
            return true;
        }
    } catch (err) {
        console.log(err);
        setTimeout(async () => await submitAmountTaminModal(), 3000);
        return false;
    }
    
};

const acceptTaminExpert = async (id) => {
    try {
        FLshowLoading();
        const res = await axios(`/Factor/AcceptTaminExpert/${id}`);
        if (res.status === 200) {
            toastifySuccess("پیش فاکتور تایید شد.");
            FLshowLoading(false);
            return true;
        }
    } catch (e) {
        setTimeout(async () => await acceptTaminExpert(id), 3000);
        return false;
    }
};

const CbPfFilter = () => {
    const varSearch = window[filterVarConfig].varSearch;
    const filterVar = window[filterVarConfig].filterVar;
    GetPreFactorPartial(varSearch, modelPreFactor._type, filterVar);
};
const CbFFilter = () => {
    const varSearch = window[filterVarConfig].varSearch;
    const filterVar = window[filterVarConfig].filterVar;
    GetFactorPartial(varSearch, null, null, filterVar, modelFactor._type);
};

const showPrefac = async (pfNumber) => {
    document.getElementById("f0").checked = true;
    document.getElementById("searchId").value = pfNumber;
    await GetPreFactorPartial(pfNumber, 0, modelPreFactor.filt);
    if (window.innerWidth < 1400) {
        const allPreFactorTab = new bootstrap.Tab(document.getElementById("allPreFactorTab"));
        allPreFactorTab.show();
    }
};
const showFac = async (pfNumber) => {
    document.getElementById("f_all").checked = true;
    document.getElementById("f_searchId").value = pfNumber;
    await GetFactorPartial(pfNumber);
    if (window.innerWidth < 1400) {
        const allFactorTab = new bootstrap.Tab(document.getElementById("allFactorTab"));
        allFactorTab.show();
    }
};


function removeTabAllFactorsDesktop() {
    if (window.innerWidth >= 1400) {
        document.getElementById("tabContentAllFactors")?.classList.remove("tab-content");
        document.getElementById("allPreFactorTab-pane")?.classList.remove("tab-pane", "fade");
        document.getElementById("allFactorTab-pane")?.classList.remove("tab-pane", "fade");
    } else {
        document.getElementById("tabContentAllFactors")?.classList.add("tab-content");
        document.getElementById("allPreFactorTab-pane")?.classList.add("tab-pane", "fade");
        document.getElementById("allFactorTab-pane")?.classList.add("tab-pane", "fade");
    }
}
window.addEventListener("resize", removeTabAllFactorsDesktop);


function removeSalesExpertsTabsDesktop() {
    if (window.innerWidth >= 1400) {
        document.getElementById("tabContentSalesExperts")?.classList.remove("tab-content");
        document.getElementById("notSendedTab-pane")?.classList.remove("tab-pane", "fade");
        document.getElementById("rejectedPfTab-pane")?.classList.remove("tab-pane", "fade");
        document.getElementById("acceptingTab-pane")?.classList.remove("tab-pane", "fade");
        document.getElementById("FinancialSendedTab-pane")?.classList.remove("tab-pane", "fade");
        document.getElementById("notValidTab-pane")?.classList.remove("tab-pane", "fade");
    } else {
        document.getElementById("tabContentSalesExperts")?.classList.add("tab-content");
        document.getElementById("notSendedTab-pane")?.classList.add("tab-pane", "fade");
        document.getElementById("rejectedPfTab-pane")?.classList.add("tab-pane", "fade");
        document.getElementById("acceptingTab-pane")?.classList.add("tab-pane", "fade");
        document.getElementById("FinancialSendedTab-pane")?.classList.add("tab-pane", "fade");
        document.getElementById("notValidTab-pane")?.classList.add("tab-pane", "fade");
    }
};
window.addEventListener("resize", removeSalesExpertsTabsDesktop);


// const dropdownPFactorFilterList =  document.querySelector('.dropdown-pFactor-filterList');
// document.getElementById('dropdown-pFactor-filterList-toggle').addEventListener('click', (e) => {
//     e.stopPropagation();
//     dropdownPFactorFilterList.classList.toggle('show')
// })
// document.addEventListener("click", function (e) {
//     if (!dropdownPFactorFilterList.contains(e.target)) {
//         dropdownPFactorFilterList.classList.remove("show");
//     }
// });