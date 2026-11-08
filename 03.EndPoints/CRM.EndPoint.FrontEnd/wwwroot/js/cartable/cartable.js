/** Keep tab toolbars outside the flexible scroll region, including AJAX tables. */
function initCartableViewport() {
    const root = document.getElementById("cartableWorkspace");
    if (!root) return;
    let frame = 0;
    const update = () => {
        frame = 0;
        root.querySelectorAll(".cartable-document-scroll, .salesExperts-list, #FinancialExpertPfContentId, #FinancialRequestContentId, #MDRequestAmountContentId, #TContentId, #SalesAdminPfactorContentId, .dt-scroll-body").forEach(region => {
            region.classList.add("cartable-viewport-scroll");
            for (let parent = region.parentElement; parent && parent !== root; parent = parent.parentElement) {
                parent.classList.add("cartable-viewport-fill");
            }
        });
    };
    update();
    const observer = new MutationObserver(() => {
        if (!frame) frame = requestAnimationFrame(update);
    });
    observer.observe(root, { childList:true, subtree:true });
    window.PageRuntime?.current?.signal?.addEventListener("abort", () => {
        observer.disconnect();
        cancelAnimationFrame(frame);
    }, { once:true });
}

/** Keep Cartable search and status filters in one compact mobile row. */
function initCartableMobileControls() {
    const root = document.getElementById("cartableWorkspace");
    if (!root) return;

    const mobileQuery = window.matchMedia("(max-width: 767px)");
    const closeSearch = group => {
        if (!group) return;
        group.classList.remove("is-search-open");
        group.querySelector(".cartable-mobile-search-toggle")?.setAttribute("aria-label", "باز کردن جستجو");
    };
    const closeStatus = list => {
        if (!list) return;
        list.classList.remove("is-mobile-status-open");
        list.parentElement?.querySelector(".cartable-mobile-filter-toggle")?.setAttribute("aria-expanded", "false");
    };
    const closeAll = () => {
        root.querySelectorAll(".cartable-list-toolbar .search-group.is-search-open").forEach(closeSearch);
        root.querySelectorAll(".cartable-list-toolbar .filter-switch.is-mobile-status-open").forEach(closeStatus);
    };

    root.querySelectorAll(".cartable-list-toolbar").forEach(toolbar => {
        const search = toolbar.querySelector(".search-group");
        const input = search?.querySelector("input[type='text'], input[type='search']");
        if (input && !search.querySelector(".cartable-mobile-search-toggle")) {
            const toggle = document.createElement("button");
            toggle.type = "button";
            toggle.className = "cartable-mobile-search-toggle";
            toggle.setAttribute("aria-label", "باز کردن جستجو");
            toggle.setAttribute("aria-controls", input.id);
            toggle.innerHTML = search.querySelector(".search-icon")?.innerHTML ?? "⌕";
            search.append(toggle);

            const syncQuery = () => toggle.classList.toggle("has-query", input.value.trim().length > 0);
            syncQuery();
            input.addEventListener("input", syncQuery);
            toggle.addEventListener("click", () => {
                if (!mobileQuery.matches) return;
                const shouldOpen = !search.classList.contains("is-search-open");
                closeAll();
                if (shouldOpen) {
                    search.classList.add("is-search-open");
                    toggle.setAttribute("aria-label", "بستن جستجو");
                    input.focus();
                }
            });
            input.addEventListener("keydown", event => {
                if (event.key === "Escape" && mobileQuery.matches) {
                    closeSearch(search);
                    toggle.focus();
                }
            });
        }

        const statusList = toolbar.querySelector(".filter-switch");
        if (statusList && !statusList.parentElement.querySelector(".cartable-mobile-filter-toggle")) {
            const toggle = document.createElement("button");
            toggle.type = "button";
            toggle.className = "cartable-mobile-filter-toggle";
            toggle.setAttribute("aria-expanded", "false");
            toggle.setAttribute("aria-label", "انتخاب وضعیت");
            const label = document.createElement("span");
            label.className = "cartable-mobile-status-label";
            toggle.append(label);
            statusList.parentElement.insertBefore(toggle, statusList);

            const updateLabel = () => {
                const selectedLabel = statusList.querySelector("input[type='radio']:checked")?.nextElementSibling;
                const textNodes = selectedLabel ? Array.from(selectedLabel.childNodes).filter(node => node.nodeType === Node.TEXT_NODE) : [];
                label.textContent = textNodes.map(node => node.textContent).join(" ").trim()
                    || selectedLabel?.textContent.trim() || "وضعیت";
            };
            updateLabel();
            statusList.addEventListener("change", event => {
                if (!event.target.matches("input[type='radio']")) return;
                updateLabel();
                closeStatus(statusList);
            });
            toggle.addEventListener("click", () => {
                if (!mobileQuery.matches) return;
                const shouldOpen = !statusList.classList.contains("is-mobile-status-open");
                closeAll();
                if (shouldOpen) {
                    statusList.classList.add("is-mobile-status-open");
                    toggle.setAttribute("aria-expanded", "true");
                }
            });
        }
    });

    const onPointerDown = event => {
        root.querySelectorAll(".cartable-list-toolbar").forEach(toolbar => {
            const search = toolbar.querySelector(".search-group.is-search-open");
            if (search && !search.contains(event.target)) closeSearch(search);
            const status = toolbar.querySelector(".filter-switch.is-mobile-status-open");
            const toggle = toolbar.querySelector(".cartable-mobile-filter-toggle");
            if (status && !status.contains(event.target) && !toggle?.contains(event.target)) closeStatus(status);
        });
    };
    const onKeyDown = event => {
        if (event.key === "Escape" && mobileQuery.matches) closeAll();
    };
    const onResize = () => {
        if (!mobileQuery.matches) closeAll();
    };
    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    window.addEventListener("resize", onResize);
    window.PageRuntime?.current?.signal?.addEventListener("abort", () => {
        document.removeEventListener("pointerdown", onPointerDown);
        document.removeEventListener("keydown", onKeyDown);
        window.removeEventListener("resize", onResize);
    }, { once: true });
}

let filConfigSession = null;
let fromWarning = false;


document.addEventListener("DOMContentLoaded", async () => {
    enableMomentumScrollScope(document.getElementById("cartableWorkspace"));
    initCartableViewport();
    initCartableMobileControls();
    showTaminDropdown();
    
    //////////////////////preFactor//////////////////////
    initSearchInput("searchId", GetPreFactorPartial);
    initChangeRadio("financeFilter", GetPreFactorPartial);
    
    //////////////////////factor//////////////////////
    initSearchInput("f_searchId", refreshFactorPartial);
    initChangeRadio("f_type", refreshFactorPartial);
    
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
    initChangeRadio("FinancialDealFilter", GetDealFinancialExpert, true);
    
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
            filterCallBack: "refreshFactorPartial",
            txtSercheId: "f_searchId",
            varSearch: "",
            filterVar: { ...fFilterFromDash.filt },
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

const exportExcelPreFactors = async () => {
    const model = {
        serVal: document.getElementById("searchId")?.value.trim() ?? "",
        _type: Number(document.querySelector("input[type='radio'][name='financeFilter']:checked")?.value ?? 0),
        filt: window[filterVarConfig]?.filterVar || null,
    };
    
    await sendApiRequest({
        url: "/Cartable/PrefactorExportToExcel",
        method: "POST",
        data: model,
        responseType: "blob",
        successMsg: null,
        onSuccess: (response) => {
            const blob = response.data;
            const url = URL.createObjectURL(blob);
            triggerDownload(url, "PrefactorsExcel.xlsx");
            URL.revokeObjectURL(url);
        },
    });
};

const exportExcelFactors = async () => {
    const model = {
        fModel: {
            serVal: document.getElementById("f_searchId")?.value.trim() ?? "",
            _type: Number(document.querySelector("input[type='radio'][name='f_type']:checked")?.value ?? 0),
            filt: window[filterVarConfig] != null ? window[filterVarConfig].filterVar : null,
        },
    };
    await sendApiRequest({
        url: "/Cartable/FactorExportToExcel",
        method: "POST",
        data: model,
        responseType: "blob",
        successMsg: null,
        onSuccess: (response) => {
            const blob = response.data;
            const url = URL.createObjectURL(blob);
            triggerDownload(url, "FactorsExcel.xlsx");
            URL.revokeObjectURL(url);
        },
    });
};


/*const ShowActiveCartable = (name) => {
    switch (name) {
        case "Admin":
            TPrefactorFactorElm?.classList.remove("d-none");
            TFinancialAdminElm?.classList.remove("d-none");
            TFinancialExpertElm?.classList.remove("d-none");
            TSalesExpertElm?.classList.remove("d-none");
            TSalesAdminElm?.classList.remove("d-none");
            TTaminExpertElm?.classList.remove("d-none");
            TManagingDirectorElm?.classList.remove("d-none");
            
            break;
        case "FinancialExpert":
            TPrefactorFactorElm?.classList.remove("d-none");
            TFinancialExpertElm?.classList.remove("d-none");
            break;
        
        case "FinancialAdmin":
            TFinancialAdminElm?.classList.remove("d-none");
            break;
        
        case "SalesExpert":
            TPrefactorFactorElm?.classList.remove("d-none");
            TSalesExpertElm?.classList.remove("d-none");
            break;
        
        case "SalesAdmin":
            TPrefactorFactorElm?.classList.remove("d-none");
            TSalesAdminElm?.classList.remove("d-none");
            break;
        
        case "TaminExpert":
        case "TaminAdmin":
            TPrefactorFactorElm?.classList.remove("d-none");
            TTaminExpertElm?.classList.remove("d-none");
            break;
        
        
        case "ManagingDirector":
            TPrefactorFactorElm?.classList.remove("d-none");
            TTaminExpertElm?.classList.remove("d-none");
            TManagingDirectorElm?.classList.remove("d-none");
            break;
        
    }
};*/

const GetCartable = (tabId, subId) => {
    setUrlParams({ tab: tabId, sub: subId });
    document.querySelectorAll(".cartable-pane").forEach(el => el.classList.add("d-none"));
    document.querySelectorAll(".TCar").forEach(async (item) => {
        if (item.id === tabId) {
            item.classList.add("TCarActive");
            switch (tabId) {
                case "TPrefactorFactorId":
                    document.getElementById("CPrefactorFactorId")?.classList.remove("d-none");
                    removeTabAllFactorsDesktop();
                    const promises = [];
                    if (filConfigSession?.filterVar) {
                        promises.push(FilterAccepted(filConfigSession));
                        if (window.innerWidth < 1400) {
                            ShowTab("allFactorTab");
                        }
                    } else if (!fromWarning) {
                        promises.push(GetFactorPartial({ showLoading: false }));
                    }
                    
                    if (!fromWarning) {
                        promises.push(GetPreFactorPartial(false));
                    }
                    

                    await Promise.all(promises);
                    break;
                
                case "TFinancialExpertId":
                    document.getElementById("CFinancialExpertId")?.classList.remove("d-none");
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
                    document.getElementById("CFinancialAdminId")?.classList.remove("d-none");
                    break;
                
                case "TSalesExpertId":
                    document.getElementById("CSalesExpertId")?.classList.remove("d-none");
                    await GetSelesExpertPartial();
                    break;
                
                case "TSalesAdminId":
                    document.getElementById("CSalesAdminId")?.classList.remove("d-none");
                    await GetPreFactorSalesAdmin();
                    break;
                
                case "TTaminExpertId":
                    document.getElementById("CTaminExpertId")?.classList.remove("d-none");
                    if (subId === "TaminInvoiceTab") {
                        await GetInvoicesTaminExpert();
                    } else {
                        await GetPreFactorTaminExpert();
                    }
                    break;
                
                case "TManagingDirectorId":
                    document.getElementById("CManagingDirectorId")?.classList.remove("d-none");
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

const GetPreFactorPartial = async (showLoading = true) => {
    const model = {
        serVal: document.getElementById("searchId")?.value.trim() ?? "",
        _type: Number(document.querySelector("input[type='radio'][name='financeFilter']:checked")?.value ?? 0),
        filt: varList?.filterVar || null,
    };
    
    await sendApiRequest({
        url: "/Cartable/PreFactorAcceptedPartial",
        skeleton: { target: "#PfAcceptedList" },
        method: "POST",
        data: model,
        successMsg: null,
        showLoading,
        onSuccess: (res) => {
            document.getElementById("PfAcceptedList").innerHTML = res.data.html;
            document.getElementById("CountId").innerText = res.data.count;
            document.getElementById("CountMobileId").innerText = res.data.count;
        },
    });
};

const GetSelesExpertPartial = async () => {
    await sendApiRequest({
        url: "/Cartable/GetSelesExpertPartial",
        skeleton: { target: "#CSalesExpertId" },
        onSuccess: (res) => {
            document.getElementById("CSalesExpertId").innerHTML = res.data;
            removeSalesExpertsTabsDesktop();
        },
    });
};

const GetPreFactorSalesAdmin = async () => {
    const val = document.getElementById("SalesAdminSearchId").value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='SalesAdminFilter']:checked").value;
    if (type === undefined) return;
    
    await sendApiRequest({
        url: "/Cartable/GetPreFactorSalesAdmin",
        skeleton: { target: "#SalesAdminPfactorContentId" },
        params: { serVal: val, type },
        onSuccess: (res) => {
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
        },
    });
};


const GetPreFactorTaminExpert = async () => {
    const val = document.getElementById("TSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='TFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "TaminPrefactorTab" });
    ShowTab("#TaminPrefactorTab");
    
    await sendApiRequest({
        url: "/Cartable/GetPreFactorTaminExpert",
        skeleton: { target: "#TContentId" },
        params: { serVal: val, type },
        onSuccess: (res) => {
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
        },
    });
};


const GetInvoicesTaminExpert = async () => {
    const val = document.getElementById("taminInvoiceSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='taminInvoiceFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "TaminInvoiceTab" });
    ShowTab("#TaminInvoiceTab");
    
    
    await sendApiRequest({
        skeleton: { target: "#taminInvoiceContentId" },
        url: "/Accountancy/GetAllPaymentCartablePartial",
        params: { serVal: val, type, cartable: 3 },
        onSuccess: (res) => {
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
        },
    });
};


const GetRequestManagingDirector = async () => {
    const val = document.getElementById("MDRequestAmountSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='MDRequestAmountFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "MDRequestAmountTab" });
    ShowTab("#MDRequestAmountTab");
    
    await sendApiRequest({
        skeleton: { target: "#MDRequestAmountContentId" },
        url: "/RequestAmount/GetAllRequestAmountsPartial",
        params: { serVal: val, type },
        onSuccess: (res) => {
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
        },
    });
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


const GetPreFactorFinancialExpert = async () => {
    const val = document.getElementById("FinancialExpertSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='financialExpertFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "FinancialPrefactorTab" });
    ShowTab("#FinancialPrefactorTab");
    
    await sendApiRequest({
        url: "/Cartable/GetPreFactorFinancialExpert",
        skeleton: { target: "#FinancialExpertPfContentId" },
        params: { serVal: val, type },
        onSuccess: (res) => {
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
        },
    });
};


const GetInvoicesFinancialExpert = async () => {
    const val = document.getElementById("FinancialExpertInvoiceSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='financialExpertInvoiceFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "FinancialInvoiceTab" });
    ShowTab("#FinancialInvoiceTab");
    
    await sendApiRequest({
        skeleton: { target: "#FinancialExpertInvoiceContentId" },
        url: "/Accountancy/GetAllPaymentCartablePartial",
        params: { serVal: val, type, cartable: 7 },
        onSuccess: (res) => {
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
        },
    });
};

const financialRequestContentElm = document.getElementById("FinancialRequestContentId");
let requestSelection;
const GetRequestFinancialExpert = async () => {
    const val = document.getElementById("FinancialRequestSearchId")?.value.trim() ?? "";
    const type = document.querySelector("input[type='radio'][name='FinancialRequestFilter']:checked")?.value;
    if (type === undefined) return;
    
    setUrlParams({ sub: "FinancialRequestTab" });
    ShowTab("#FinancialRequestTab");
    
    await sendApiRequest({
        skeleton: { target: "#FinancialRequestContentId" },
        url: "/RequestAmount/GetAllRequestAmountsPartial",
        params: { search: val, type },
        onSuccess: (res) => {
            const data = res.data;
            financialRequestContentElm.innerHTML = data.html;
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
            requestSelection?.restoreSelection();
        },
    });
};

let dealTbl;
let dealModel = { filt: null, serVal: null };
const GetDealFinancialExpert = async () => {
    setUrlParams({ sub: "FinancialDealTab" });
    ShowTab("#FinancialDealTab");
    
    const type = document.querySelector("input[type='radio'][name='FinancialDealFilter']:checked")?.value ?? 0;
    dealModel._type = Number(type);
    dealModel.filt = window.dealConfigFilter?.filterVar;
    
    if (dealTbl) {
        dealTbl.ajax.reload();
    } else {
        getAllDealsDt();
    }
};

window.dealConfigFilter = {};
function getAllDealsDt() {
    const dealTableElm = document.getElementById("DealTable");
    
    dealTbl = $(dealTableElm).DataTable({
        lengthMenu: [10, 25, 50, 100, { label: "همه", value: -1 }],
        pageLength: 10,
        processing: true,
        serverSide: true,
        search: {
            return: true,
        },
        ordering: true,
        order: { name: "createDateTxt", dir: "desc" },
        scrollCollapse: true,
        scrollY: "clamp(150px, calc(100dvh - 315px), 100dvh)",
        responsive: true,
        ajax: {
            url: `/Deal/GetAllDealsDt`,
            type: "POST",
            data: (d) => {
                d.model = JSON.stringify(dealModel);
            },
            dataSrc: function (json) {
                updateFilterSwitchState({
                    wrapperSelector: "#FinancialDealTab-pane",
                    filterData: json.filterCounts,
                    currentFilterValue: dealModel._type,
                    searchValue: dealTbl.search(),
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
            { data: "serialNumber", responsivePriority: 1 },
            { data: "title", className: "wrap-text-minw" },
            { data: "createDateTxt", name: "createDateTxt", render: data => renderDateTimeHtml(data) },
            { data: "userCreator" },
            {
                data: "customerName",
                className: "wrap-text-minw",
                render: function (data, type, row) {
                    return `${data ?? ""}${row.customerCode ? " (کد مشتری: " + row.customerCode + ")" : ""}`;
                },
            },
            {
                data: "status",
                render: data => {
                    if (data === 1) return `<span class="fMedium financial-deal-state--open">باز</span>`;
                    if (data === 2) return `<span class="fMedium financial-deal-state--failed">ناموفق</span>`;
                    if (data === 3) return `<span class="fMedium financial-deal-state--success">موفق</span>`;
                    return "-";
                },
            },
            {
                data: "counts.preFactor",
                responsivePriority: 2,
                orderable: false,
                render: function (data) {
                    if (!data) return "-";
                    
                    return `
                        <span class="deal-document-action" data-action="pre-factors" data-title-right="مشاهده">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M19 17V5a2 2 0 0 0-2-2H4"/><path d="M8 21h12a2 2 0 0 0 2-2v-1a1 1 0 0 0-1-1H11a1 1 0 0 0-1 1v1a2 2 0 1 1-4 0V5a2 2 0 1 0-4 0v2a1 1 0 0 0 1 1h3"/></svg>
                            <span>${data}</span>
                        </span>
                    `;
                },
            },
            {
                data: "counts.factor",
                responsivePriority: 3,
                orderable: false,
                render: function (data) {
                    if (!data) return "-";
                    
                    return `
                        <span class="deal-document-action" data-action="factors" data-title-right="مشاهده">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M15 12h-5"/><path d="M15 8h-5"/><path d="M19 17V5a2 2 0 0 0-2-2H4"/><path d="M8 21h12a2 2 0 0 0 2-2v-1a1 1 0 0 0-1-1H11a1 1 0 0 0-1 1v1a2 2 0 1 1-4 0V5a2 2 0 1 0-4 0v2a1 1 0 0 0 1 1h3"/></svg>
                            <span>${data}</span>
                        </span>
                    `;
                },
            },
            {
                data: "counts.accountancy",
                responsivePriority: 4,
                orderable: false,
                render: function (data) {
                    if (!data) return "-";
                    
                    return `
                        <span class="deal-document-action" data-action="invoices" data-title-right="مشاهده">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M12 18H4a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5"/><path d="M18 12h.01"/><path d="M19 22v-6"/><path d="m22 19-3-3-3 3"/><path d="M6 12h.01"/><circle cx="12" cy="12" r="2"/></svg>
                            <span>${data}</span>
                        </span>
                    `;
                },
            },
            {
                data: "counts.orderHead",
                responsivePriority: 5,
                orderable: false,
                render: function (data) {
                    if (!data) return "-";
                    
                    return `
                        <span class="deal-document-action" data-action="orders" data-title-right="مشاهده">
                            <svg fill="currentColor" viewBox="0 0 256 256"><path d="M72,104a8,8,0,0,1,8-8h96a8,8,0,0,1,0,16H80A8,8,0,0,1,72,104Zm8,40h96a8,8,0,0,0,0-16H80a8,8,0,0,0,0,16ZM232,56V208a8,8,0,0,1-11.58,7.15L192,200.94l-28.42,14.21a8,8,0,0,1-7.16,0L128,200.94,99.58,215.15a8,8,0,0,1-7.16,0L64,200.94,35.58,215.15A8,8,0,0,1,24,208V56A16,16,0,0,1,40,40H216A16,16,0,0,1,232,56Zm-16,0H40V195.06l20.42-10.22a8,8,0,0,1,7.16,0L96,199.06l28.42-14.22a8,8,0,0,1,7.16,0L160,199.06l28.42-14.22a8,8,0,0,1,7.16,0L216,195.06Z"></path></svg>
                            <span>${data}</span>
                        </span>
                    `;
                },
            },
            /*{
                data: "counts.report",
                orderable: false,
                render: function (data) {
                    if (!data) return "-";
                    
                    return `
                        <span class="deal-document-action" data-action="reports" data-title-right="مشاهده">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M8 2v4"/><path d="M12 2v4"/><path d="M16 2v4"/><rect width="16" height="18" x="4" y="4" rx="2"/><path d="M8 10h6"/><path d="M8 14h8"/><path d="M8 18h5"/></svg>
                            <span>${data}</span>
                        </span>
                    `;
                },
            },*/
            /*{
                data: "counts.supplyFactor",
                responsivePriority: 6,
                orderable: false,
                render: function (data) {
                    if (!data) {
                        return `
                            <button class="deal-document-add" data-action="supplyFactor-add">
                                <svg viewBox="0 0 16 16" fill="currentColor"><path d="M8.75 3.75a.75.75 0 0 0-1.5 0v3.5h-3.5a.75.75 0 0 0 0 1.5h3.5v3.5a.75.75 0 0 0 1.5 0v-3.5h3.5a.75.75 0 0 0 0-1.5h-3.5v-3.5Z" /></svg>
                                <span>ثبت</span>
                            </button>
                        `;
                    }
                    
                    return `
                        <span class="deal-document-action" data-action="supplyFactor" data-title-right="مشاهده">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path stroke="none" d="M0 0h24v24H0z" fill="none" /><path d="M5 21v-16a2 2 0 0 1 2 -2h10a2 2 0 0 1 2 2v16l-3 -2l-2 2l-2 -2l-2 2l-2 -2l-3 2" /><path d="M14.8 8a2 2 0 0 0 -1.8 -1h-2a2 2 0 1 0 0 4h2a2 2 0 1 1 0 4h-2a2 2 0 0 1 -1.8 -1" /><path d="M12 6v10" /></svg>
                            <span>${data}</span>
                        </span>
                    `;
                },
            },*/
            {
                data: "id",
                responsivePriority: 7,
                orderable: false,
                render: function (data, type, row) {
                    if (row.financialStatus === 0) {
                        return `<button type="button" class="financial-deal-approve btn-accept" onclick="closeDeal('${data}')">تایید سفارش</button>`;
                    } else if (row.financialStatus === 1) {
                        return `<button type="button" class="financial-deal-reopen btn-accept" onclick="activeDeal('${data}')">بازگشت</button>`;
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
        layout: layoutDatatable,
        initComplete: initFilterDealTable,
    });
    function initFilterDealTable() {
        const dtSearch = document.querySelector("#FinancialDealContentId .dt-search");
        const searchInput = dtSearch?.querySelector("input");
        if (!searchInput) return;
        
        const filterButton = `
            <div class="btnFilter pe-md-2" data-filterVarConfig="dealConfigFilter" data-filterCallBack="GetDealFinancialExpert" data-filterContentId="contentDealFilterId" onclick="ShowFilterPopup(this)">
                <svg style="width:24px;height:24px;color:#333" viewBox="0 0 24 24" fill="currentColor" ><path fill-rule="evenodd" d="M3.792 2.938A49.069 49.069 0 0 1 12 2.25c2.797 0 5.54.236 8.209.688a1.857 1.857 0 0 1 1.541 1.836v1.044a3 3 0 0 1-.879 2.121l-6.182 6.182a1.5 1.5 0 0 0-.439 1.061v2.927a3 3 0 0 1-1.658 2.684l-1.757.878A.75.75 0 0 1 9.75 21v-5.818a1.5 1.5 0 0 0-.44-1.06L3.13 7.938a3 3 0 0 1-.879-2.121V4.774c0-.897.64-1.683 1.542-1.836Z" clip-rule="evenodd" /></svg>
            </div>
        `;
        const radioBtns = `
            <ul class="filter-switch mt-1 mt-md-0">
                <li class="filter-switch-item">
                    <input type="radio" name="FinancialDealFilter" id="FinancialDealFilterAll" value="-1" />
                    <label for="FinancialDealFilterAll">همه</label>
                </li>
                <li class="filter-switch-item">
                    <input type="radio" name="FinancialDealFilter" id="FinancialDealFilter1" value="0" checked />
                    <label for="FinancialDealFilter1">
                        تایید نشده
                        <span class="badge-count filter-count-badge"></span>
                    </label>
                </li>
                <li class="filter-switch-item">
                    <input type="radio" name="FinancialDealFilter" id="FinancialDealFilter2" value="1" />
                    <label for="FinancialDealFilter2">
                        تایید شده
                        <span class="badge-count filter-count-badge"></span>
                    </label>
                </li>
            </ul>
        `;
        const contentFilter = `<div id="contentDealFilterId" class="ContentFilter mt-2 mt-md-0 ms-md-2 justify-content-start"></div>`;
        
        searchInput.insertAdjacentHTML("afterend", filterButton);
        dtSearch.insertAdjacentHTML("afterend", contentFilter);
        dtSearch.insertAdjacentHTML("afterend", radioBtns);
    }
    
    
    dealTableElm.addEventListener("click", async (e) => {
        const documentAction = e.target.closest(".deal-document-action, .deal-document-add");
        if (!documentAction) return;
        
        const rowElement = documentAction.closest("tr");
        const deal = dealTbl.row(rowElement).data();
        
        if (!deal) return;
        
        switch (documentAction.dataset.action) {
            case "pre-factors":
                await showModalDealPreFactors(deal);
                break;
            case "factors":
                await showModalDealFactors(deal);
                break;
            case "invoices":
                await showModalDealInvoices(deal);
                break;
            case "orders":
                await showModalDealOrders(deal);
                break;
            /*case "reports":
                await showModalDealReports(deal);
                break;*/
            case "supplyFactor":
                await showModalDealSupplyFactors(deal);
                break;
            
            case "supplyFactor-add":
                await showSupplyFactorModal(deal.id);
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

if (financialRequestContentElm) {
    requestSelection = new SelectionManager({
        container: financialRequestContentElm,
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
}

////////////////////////////////////////////////////////


const acceptSalesAdmin = async (id) => {
    await sendApiRequest({
        url: "/Factor/AcceptSalesAdmin",
        params: { id },
        successMsg: "پیش فاکتور تایید شد",
        onSuccess: GetPreFactorSalesAdmin,
    });
};

const showPrefac = async (pfNumber) => {
    document.getElementById("f0").checked = true;
    document.getElementById("searchId").value = pfNumber;
    await GetPreFactorPartial();
    if (window.innerWidth < 1400) {
        const allPreFactorTab = new bootstrap.Tab(document.getElementById("allPreFactorTab"));
        allPreFactorTab.show();
    }
};

const showFac = async (pfNumber) => {
    document.getElementById("f_all").checked = true;
    document.getElementById("f_searchId").value = pfNumber;
    await refreshFactorPartial();
    if (window.innerWidth < 1400) {
        ShowTab("allFactorTab");
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
}
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
// if(document.querySelector('.dropdown-pFactor-filterList #pfFilterAll').checked)
