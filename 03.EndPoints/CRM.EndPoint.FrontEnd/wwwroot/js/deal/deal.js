
let modelDeal = { filt: null, serVal: null };
var filterDealConfig = {};
var dealSearchVal = null;
let currentDealData = {};
let isEditMode = false;
let canCreateCustomerTomSelect = true;
let selectTomCustomers;

const dealModalElm = document.getElementById("dealModal");
const toggleExtraPanelBtn = dealModalElm.querySelector("#toggleExtraInfo");
const extraCustomerPanel = document.getElementById("extraCustomerPanel");
document.addEventListener("DOMContentLoaded", async () => {
    /* FLshowLoading(); */
    
    removeDealsTabsDesktop();
    initSortableDeal();
    
    showTaminDropdown();
    await getAllDeals();
    
    /* FLshowLoading(false); */
    
    const searchDealElm = document.getElementById("dealSearchId");
    searchDealElm?.addEventListener("keyup", async (e) => {
        if (e.key === "Enter") {
            modelDeal.serVal = e.target.value.trim();
            await getAllDeals();
        }
    });
    searchDealElm?.nextElementSibling.addEventListener("click", async (e) => {
        modelDeal.serVal = searchDealElm.value.trim();
        await getAllDeals();
    });
});


const dealDropdown = Dropdown.setupDelegate({
    container: document,
    toggleSelector: ".deal-dropdown__toggle",
    options: {
        transitionDuration: 200,
        originalDisplay: "flex",
        effect: "zoom",
    },
});


const deleteDeal = async (id) => {
    dealDropdown.close();
    await confirmAlert({
        title: "حذف!",
        text: "آیا مطمئن هستید که میخواهید حذف کنید؟",
        confirmText: "بله، حذف شود",
        // theme: "dark",
        onConfirm: async () => {
            await sendApiRequest({
                url: "/Deal/DeleteDeal",
                method: "DELETE",
                params: { id },
                onSuccess: getAllDeals,
            });
        },
    });
};


async function callbackFilterDeal() {
    modelDeal.filt = window[filterVarConfig].filterVar;
    modelDeal.serVal = window[filterVarConfig].varSearch;
    
    await getAllDeals();
}

const dealCardSkeletonTemplate = `
    <div class="deal-card" style="border-right-color: #ddd; pointer-events: none;">
        <div class="position-relative w-100 h-100">
            <div class="d-flex justify-content-between align-items-start gap-2 mb-3">
                <div class="skel-base skel-title"></div>
                <div class="d-flex gap-2">
                    <div class="skel-base skel-circle" style="width: 20px; height: 20px;"></div>
                    <div class="skel-base skel-circle" style="width: 20px; height: 20px;"></div>
                </div>
            </div>
            <div class="skel-base skel-line-short"></div>
            <div class="skel-base skel-line mt-2" style="height: 25px;"></div>
            <div class="d-flex justify-content-between align-items-end mt-3 gap-2">
                <div class="w-100">
                    <div class="skel-base skel-line" style="width: 70%;"></div>
                    <div class="skel-base skel-line" style="width: 50%;"></div>
                </div>
                <div class="skel-base skel-circle" style="width: 24px; height: 24px;"></div>
            </div>
        </div>
    </div>
`;
Skeleton.register("deal-card", () => `
    <div class="deal-card">
        <div class="sk sk-line mb-2" style="width:45%;height:16px"></div>
        <div class="sk sk-line mb-3" style="width:70%"></div>
        <div class="sk-gap">
            <div class="sk sk-line"></div>
            <div class="sk sk-line" style="width:80%"></div>
            <div class="d-flex justify-content-between">
                <div class="sk sk-pill" style="width:90px;height:14px"></div>
                <div class="sk sk-pill" style="width:70px;height:14px"></div>
                <div class="sk sk-pill" style="width:110px;height:14px"></div>
            </div>
        </div>
    </div>`,
);
const getAllDeals = async () => {
    const openSkeleton = new DynamicSkeleton({ container: "#openDealsContainerId .deal-column-list", template: dealCardSkeletonTemplate, count: 4 });
    const successSkeleton = new DynamicSkeleton({
        container: "#successDealsContainerId .deal-column-list",
        template: dealCardSkeletonTemplate,
        count: 4,
    });
    const failedSkeleton = new DynamicSkeleton({
        container: "#failedDealsContainerId .deal-column-list",
        template: dealCardSkeletonTemplate,
        count: 4,
    });
    try {
        FLshowLoading();
        //  openSkeleton.render();
        //successSkeleton.render();
        // failedSkeleton.render();
        /*Skeleton.show({
                container:"#openDealsContainerId .deal-column-list",
                count:6,
                template:"deal-card"
            });
            
            Skeleton.show({
                container:"#successDealsContainerId .deal-column-list",
                count:6,
                template:"deal-card"
            });
            
            Skeleton.show({
                container:"#failedDealsContainerId .deal-column-list",
                count:6,
                template:"deal-card"
            });*/
        const res = await axios.post(`/Deal/GetAllDeals`, modelDeal);
        if (res.status === 200) {
            const items = res.data.items;
            if (window.innerWidth < 1200) {
                updateTabSwitchState({
                    wrapperSelector: "#dealTabs",
                    tabData: items,
                    searchValue: modelDeal.serVal,
                    filter: modelDeal.filt,
                });
            }
            createDomAllDeals(items.open, "#openDealsContainerId", res.data.isAdmin);
            createDomAllDeals(items.success, "#successDealsContainerId", res.data.isAdmin);
            createDomAllDeals(items.failed, "#failedDealsContainerId", res.data.isAdmin);
            FLshowLoading(false);
        }
    } catch (err) {
        console.log(err);
    }
};


const createDomAllDeals = (items, containersId, isAdmin) => {
    const countElm = document.querySelector(`${containersId} .countDeal`);
    const containerElm = document.querySelector(`${containersId} .deal-column-list`);
    if (!containerElm) return;
    
    if (countElm) countElm.textContent = `(${items.length})`;
    containerElm.innerHTML = "";
    
    items.forEach((item, index) => {
        const isFinancialClosed = item.financialStatus === 1;
        
        const card = document.createElement("div");
        card.dataset.id = item.id;
        card.className = "deal-card";
        
        if (!item.counts.preFactor && !item.counts.report) {
            card.classList.add("deal-card--inactive");
        }
        
        if (isFinancialClosed) {
            card.classList.add("deal-card--locked");
        }
        
        card.innerHTML = `
            <div class="position-relative w-100 h-100">
                ${item.suspend ? `
                    <div class="overlay-card">
                        <p class="fbold" style="padding: 10px;border-radius: 50px;background-color: red;color: aliceblue;opacity: 30%;transform: rotate(-10deg);">
                            در انتظار تصمیم مدیر فروش
                        </p>
                    </div>` : ""}
                <div class="d-flex justify-content-between align-items-start gap-2">
                    <div>
                        <span class="fbold" style="font-size:clamp(13px, 2vw, 14px);color: #333">${item.title}</span>
                    </div>
                        ${!isFinancialClosed ? `
                            <div class="d-flex align-items-center gap-2" style="color: #555;">
                                <button data-title-right="ثبت گزارش" class="d-flex addReport">
                                  <svg style="width:16px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="4 7 4 4 20 4 20 7"></polyline><line x1="9" y1="20" x2="15" y2="20"></line><line x1="12" y1="4" x2="12" y2="20"></line></svg>
                                </button>
                                    <button data-title-right="ثبت پیش‌فاکتور" class="d-flex addPreFactor">
                                      <svg style="width: 18px;cursor: pointer" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15"></path></svg>
                                    </button>
                            </div>` : `<span class="deal-card-locked--badge">
                                <svg style="width: 14px" viewBox="0 0 16 16" fill="currentColor"><path fill-rule="evenodd" d="M15 8c0 .982-.472 1.854-1.202 2.402a2.995 2.995 0 0 1-.848 2.547 2.995 2.995 0 0 1-2.548.849A2.996 2.996 0 0 1 8 15a2.996 2.996 0 0 1-2.402-1.202 2.995 2.995 0 0 1-2.547-.848 2.995 2.995 0 0 1-.849-2.548A2.996 2.996 0 0 1 1 8c0-.982.472-1.854 1.202-2.402a2.995 2.995 0 0 1 .848-2.547 2.995 2.995 0 0 1 2.548-.849A2.995 2.995 0 0 1 8 1c.982 0 1.854.472 2.402 1.202a2.995 2.995 0 0 1 2.547.848c.695.695.978 1.645.849 2.548A2.996 2.996 0 0 1 15 8Zm-3.291-2.843a.75.75 0 0 1 .135 1.052l-4.25 5.5a.75.75 0 0 1-1.151.043l-2.25-2.5a.75.75 0 1 1 1.114-1.004l1.65 1.832 3.7-4.789a.75.75 0 0 1 1.052-.134Z" clip-rule="evenodd" /></svg>
                                تایید مالی
                            </span>`}
                </div>
                <div class="d-flex align-items-center gap-2 mb-2 mt-1" style="font-size:clamp(10px, 2vw, 11px);color: #555">
                    <div class="d-flex align-items-center gap-1">
                        <span class="fMedium text-muted">شماره سفارش:</span>
                        <span class="fbold" style="font-size:clamp(11px, 2vw, 12px)">${item.serialNumber}</span>
                    </div>
                    ${isAdmin ? `
                        <span class="d-flex align-items-center gap-1">
                             <span class="fMedium text-muted">دراختیار:</span>
                             <span class="fbold">${item.userCreator}</span>
                        </span>` : ""}
                </div>
                <p style="font-size:clamp(12px, 2vw, 13px);color:gray;min-height:12px;">${item.commentText?.trim()}</p>
                <div class="d-flex w-100 justify-content-between align-items-end mt-2" style="column-gap:10px;row-gap:8px;color: #666">
                    <div class="d-flex gap-2 gap-lg-3 align-items-end justify-content-between flex-grow-1" style="font-size:clamp(10px, 2vw, 11px)">
                        <div class="d-flex gap-2 gap-lg-3 align-items-center flex-grow-1 flex-wrap" style="row-gap:.5rem !important">
                            <span class="d-flex align-items-center gap-1 ">
                                <svg class="flex-shrink-0 ${item.customerId ? "icon-hover cursor-pointer" : ""}" style="width: 16px" 
                                      ${item.customerId ? `onclick="openCustomerProfile('${item.customerId}')"` : ""} viewBox="0 0 16 16" fill="currentColor">
                                  <path fill-rule="evenodd" d="M15 8A7 7 0 1 1 1 8a7 7 0 0 1 14 0Zm-5-2a2 2 0 1 1-4 0 2 2 0 0 1 4 0ZM8 9c-1.825 0-3.422.977-4.295 2.437A5.49 5.49 0 0 0 8 13.5a5.49 5.49 0 0 0 4.294-2.063A4.997 4.997 0 0 0 8 9Z" clip-rule="evenodd"/></svg>
                                <span>${item.customerName ?? ""}${item.customerCode ? " (کد مشتری: " + item.customerCode + ")" : ""}</span>
                            </span>
                             <span class="d-flex align-items-center gap-1 ">
                                <svg style="width: 16px;flex-shrink: 0" viewBox="0 0 16 16" fill="currentColor"><path fill-rule="evenodd" d="m3.855 7.286 1.067-.534a1 1 0 0 0 .542-1.046l-.44-2.858A1 1 0 0 0 4.036 2H3a1 1 0 0 0-1 1v2c0 .709.082 1.4.238 2.062a9.012 9.012 0 0 0 6.7 6.7A9.024 9.024 0 0 0 11 14h2a1 1 0 0 0 1-1v-1.036a1 1 0 0 0-.848-.988l-2.858-.44a1 1 0 0 0-1.046.542l-.534 1.067a7.52 7.52 0 0 1-4.86-4.859Z" clip-rule="evenodd" /></svg>
                                <span style="direction: ltr">${item.mobileNumber}</span>
                            </span>
                             <span class="d-flex align-items-center gap-1 ">
                                <svg style="width: 16px;flex-shrink: 0" viewBox="0 0 16 16" fill="currentColor"><path fill-rule="evenodd" d="M4 1.75a.75.75 0 0 1 1.5 0V3h5V1.75a.75.75 0 0 1 1.5 0V3a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2V1.75ZM4.5 6a1 1 0 0 0-1 1v4.5a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7a1 1 0 0 0-1-1h-7Z" clip-rule="evenodd" /></svg>
                                <span style="direction: ltr">${item.createDateTxt.slice(0, -3).split(" ").join(" | ")} </span>
                            </span>
                        </div>
                        <div class="d-flex align-items-center gap-1 d-xl-none">
                            <button class="${item.status === 1 ? "buttonGreen2" : "buttonBlue2"}" style="font-size: 11px" onclick="changeStatusDeal('${item.id}', ${item.status === 1 ? "3" : "1"})">
                                ${item.status === 1 ? "موفق" : "باز"}
                            </button>
                            <button class="${item.status === 2 ? "buttonGreen2" : "buttonCancel3"}" style="font-size: 11px" onclick="changeStatusDeal('${item.id}', ${item.status === 2 ? "3" : "2"})">
                                ${item.status === 2 ? "موفق" : "ناموفق"}
                            </button>
                        </div>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                        <button class="deal-dropdown__toggle d-flex align-items-center justify-content-center" tabindex="0">
                            <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M17 3.34a10 10 0 1 1 -14.995 8.984l-.005 -.324l.005 -.324a10 10 0 0 1 14.995 -8.336zm-8 4.66a1 1 0 0 0 -1 1v6l.007 .117l.029 .149l.035 .105l.054 .113l.071 .111c.03 .04 .061 .077 .097 .112l.09 .08l.096 .067l.098 .052l.11 .044l.112 .03l.126 .017l6.075 .003l.117 -.007a1 1 0 0 0 .883 -.993l-.007 -.117a1 1 0 0 0 -.993 -.883h-3.586l4.293 -4.293l.083 -.094a1 1 0 0 0 -1.497 -1.32l-4.293 4.291v-3.584l-.007 -.117a1 1 0 0 0 -.993 -.883z"/></svg>
                        </button>
                        <div class="task-dropdown deal-dropdown__list" style="border: 1px solid #e5e5e5;">
                            <div class="dropdown-item justify-content-between itemGreen showPreFactors">
                                <div class="d-flex align-items-center" style="gap: 12px">
                                  <svg style="width:16px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M19 17V5a2 2 0 0 0-2-2H4"/><path d="M8 21h12a2 2 0 0 0 2-2v-1a1 1 0 0 0-1-1H11a1 1 0 0 0-1 1v1a2 2 0 1 1-4 0V5a2 2 0 1 0-4 0v2a1 1 0 0 0 1 1h3"/></svg>
                                  <span>پیش‌فاکتور ها</span>
                                </div>
                                ${item.counts.preFactor ? `<span class="badge-count">${item.counts.preFactor}</span>` : ""}
                            </div>
                            <div class="dropdown-item justify-content-between itemGreen showFactors">
                                <div class="d-flex align-items-center" style="gap: 12px">
                                    <svg style="width:16px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M15 12h-5"/><path d="M15 8h-5"/><path d="M19 17V5a2 2 0 0 0-2-2H4"/><path d="M8 21h12a2 2 0 0 0 2-2v-1a1 1 0 0 0-1-1H11a1 1 0 0 0-1 1v1a2 2 0 1 1-4 0V5a2 2 0 1 0-4 0v2a1 1 0 0 0 1 1h3"/></svg>
                                    <span>فاکتور ها</span>
                                </div>
                                ${item.counts.factor ? `<span class="badge-count">${item.counts.factor}</span>` : ""}
                            </div>
                            <div class="dropdown-item justify-content-between itemGray showReports">
                                <div class="d-flex align-items-center" style="gap: 12px">
                                    <svg style="width:16px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M8 2v4"/><path d="M12 2v4"/><path d="M16 2v4"/><rect width="16" height="18" x="4" y="4" rx="2"/><path d="M8 10h6"/><path d="M8 14h8"/><path d="M8 18h5"/></svg>
                                    <span>گزارش ها</span>
                                </div>
                                ${item.counts.report ? `<span class="badge-count">${item.counts.report}</span>` : ""}
                            </div>
                            <div class="dropdown-item justify-content-between itemBlue showOrders">
                                <div class="d-flex align-items-center" style="gap: 12px">
                                    <svg style="width: 16px" fill="currentColor" viewBox="0 0 256 256"><path d="M72,104a8,8,0,0,1,8-8h96a8,8,0,0,1,0,16H80A8,8,0,0,1,72,104Zm8,40h96a8,8,0,0,0,0-16H80a8,8,0,0,0,0,16ZM232,56V208a8,8,0,0,1-11.58,7.15L192,200.94l-28.42,14.21a8,8,0,0,1-7.16,0L128,200.94,99.58,215.15a8,8,0,0,1-7.16,0L64,200.94,35.58,215.15A8,8,0,0,1,24,208V56A16,16,0,0,1,40,40H216A16,16,0,0,1,232,56Zm-16,0H40V195.06l20.42-10.22a8,8,0,0,1,7.16,0L96,199.06l28.42-14.22a8,8,0,0,1,7.16,0L160,199.06l28.42-14.22a8,8,0,0,1,7.16,0L216,195.06Z"></path></svg>
                                    <span>حواله ها</span>
                                </div>
                                ${item.counts.orderHead ? `<span class="badge-count">${item.counts.orderHead}</span>` : ""}
                            </div>
                            <div class="dropdown-item justify-content-between itemPurple showInvoices">
                                <div class="d-flex align-items-center" style="gap: 12px">
                                    <svg style="width:16px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M12 18H4a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5"/><path d="M18 12h.01"/><path d="M19 22v-6"/><path d="m22 19-3-3-3 3"/><path d="M6 12h.01"/><circle cx="12" cy="12" r="2"/></svg>
                                    <span>واریزی ها</span>
                                </div>
                                ${item.counts.accountancy ? `<span class="badge-count">${item.counts.accountancy}</span>` : ""}
                            </div>
                            <div class="dropdown-item itemGold" onclick="showTaskModal('${item.id}', 'Deal')">
                                <svg style="width:16px;" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 0 0 5.454-1.31A8.967 8.967 0 0 1 18 9.75V9A6 6 0 0 0 6 9v.75a8.967 8.967 0 0 1-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 0 1-5.714 0m5.714 0a3 3 0 1 1-5.714 0"></path></svg>
                                <span>افزودن فعالیت</span>
                            </div>
                            <div class="dropdown-item itemGray" style="border-top: 1px solid #e3e3e3;" onclick="showLogEntity('Deal', '${item.id}')">
                                <svg style="width: 16px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"></path><path d="M3 3v5h5"></path><path d="M12 7v5l4 2"></path></svg>
                                <span>تاریخچه تغییرات</span>
                            </div>
                            ${!isFinancialClosed ? `
                                <div class="dropdown-item itemGray editDeal" style="border-top: 1px solid #e3e3e3"">
                                    <svg style="width:16px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0 1 15.75 21H5.25A2.25 2.25 0 0 1 3 18.75V8.25A2.25 2.25 0 0 1 5.25 6H10"></path></svg>
                                    <span>ویرایش</span>
                                </div>` : ""}
                            ${!isFinancialClosed && isAdmin ? `
                                <div class="dropdown-item itemRed deleteDeal">
                                    <svg style="width:16px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0"></path></svg>
                                    <span>حذف</span>
                                </div>` : ""}
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        
        if (item.suspend) {
            card.classList.add("suspend-deal");
            const text = isAdmin ? "مشتری این سفارش متعلق به کارشناس دیگری است، لطفاً مشخص کنید این سفارش باید به کدام کارشناس اختصاص یابد."
                : "مشتری این سفارش متعلق به کارشناس دیگری است.";
            
            const suspendElm = `
                <div class="mt-3 p-2 text-center" style="background-color:rgb(216 219 231 / 78%);border-radius: 10px;">
                    <p class="fMedium" style="font-size: 12px; color: #333;">${text}</p>
                    ${isAdmin ? `<div class="d-flex align-items-center gap-1 mt-3 position-relative">
                        <label for="assignUserSuspendSelect${index}" class="flex-shrink-0" style="font-size: 11px">اختصاص به کارشناس</label>
                        <select class="w-100 assignUser-suspendSelect show-dropdown-arrow" id="assignUserSuspendSelect${index}" >
                        </select>
                        <button id="assignUserSuspendBtn" class="flex-center">
                            <svg style="width: 24px;color: #198754" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 11 12 14 22 4"></polyline><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"></path></svg>
                        </button>
                    </div>` : ""}
                </div>
            `;
            
            card.insertAdjacentHTML("beforeend", suspendElm);
            
            const suspendSelect = card.querySelector(`select.assignUser-suspendSelect`);
            if (isAdmin && suspendSelect) {
                card.querySelector("#assignUserSuspendBtn")?.addEventListener("click", async () => {
                    const userId = suspendSelect.value;
                    const userFullName = suspendSelect.selectedOptions[0].textContent;
                    await assignUserSuspendDeal(item.id, userId, userFullName);
                });
                
                const options = item.userIdsForSuspend.map(u => ({ value: u.id, text: u.firstName }));
                
                new TomSelect(suspendSelect, {
                    options: options,
                    items: options[0].value,
                    placeholder: "انتخاب تامین کننده",
                    maxItems: 1,
                    selectOnTab: true,
                });
            }
        }
        /*<svg style="width:16px" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path stroke="none" d="M0 0h24v24H0z" fill="none" /><path d="M17 20h-11a3 3 0 0 1 0 -6h11a3 3 0 0 0 0 6h1a3 3 0 0 0 3 -3v-11a2 2 0 0 0 -2 -2h-10a2 2 0 0 0 -2 2v8" /></svg> */
        card.querySelector(".showPreFactors")?.addEventListener("click", () => showModalDealPreFactors(item));
        card.querySelector(".addPreFactor")?.addEventListener("click", () => showModalAddPfactor(4, item.customerId, item));
        card.querySelector(".showFactors")?.addEventListener("click", () => showModalDealFactors(item));
        card.querySelector(".showReports")?.addEventListener("click", () => showModalDealReports(item));
        card.querySelector(".addReport")?.addEventListener("click", () => {
            const model = item.customer ?? { agentName: item.customerName, mobile: item.mobileNumber };
            ShowModalReport(model, 4, item.id, item.serialNumber);
        });
        card.querySelector(".showOrders")?.addEventListener("click", () => showModalDealOrders(item));
        card.querySelector(".showInvoices")?.addEventListener("click", () => showModalDealInvoices(item));
        
        card.querySelector(".editDeal")?.addEventListener("click", () => editDealModal(item.id, item.counts.preFactor));
        card.querySelector(".deleteDeal")?.addEventListener("click", () => deleteDeal(item.id));
        
        containerElm.append(card);
    });
};

const assignUserSuspendDeal = async (dealId, userId, userFullName) => {
    try {
        FLshowLoading();
        const res = await axios(`/Deal/AssignUserSuspendDeal?dealId=${dealId}&userId=${userId}&userFullName=${userFullName}`);
        if (res.status === 200) {
            toastifySuccess("با موفقیت ثبت شد.");
            await getAllDeals();
        }
        
    } catch (err) {
        console.log(err);
        toastifyError("خطای سرور");
    } finally {
        FLshowLoading(false);
    }
};


const editDealModal = async (id, preFactorCount) => {
    FLshowLoading();
    await openDealModal();
    try {
        const res = await axios(`/Deal/GetDeal/${id}`);
        currentDealData = res.data;
        isEditMode = true;
        mySer.convertJsonToForm(currentDealData, "formDeal");
        
        canCreateCustomerTomSelect = currentDealData.customerId === null || preFactorCount === 0;
        
        if (currentDealData.customerId === null) {
            const customId = `custom-${currentDealData.customerName}`;
            selectTomCustomers.addOption({ value: customId, text: currentDealData.customerName });
            selectTomCustomers.setValue(customId, true);
            
            const mobileNumber = currentDealData.mobileNumber;
            dealMobileElm.tomselect.addOption({ value: mobileNumber });
            dealMobileElm.tomselect.setValue(mobileNumber, true);
        }
        
    } catch (err) {
        console.log(err);
    }
    FLshowLoading(false);
};

let selectTomLoaded = false;
const openDealModal = async () => {
    if (!selectTomLoaded) {
        await initSelectTom();
        selectTomLoaded = true;
    }
    
    ModalToggle(dealModalElm, "show");
};

const closeDealModal = () => {
    ModalToggle(dealModalElm, "hide");
    isEditMode = false;
    canCreateCustomerTomSelect = true;
    currentDealData = {};
    mySer.emptyForm("formDeal");
    toggleExtraPanel(false);
};

dealModalElm?.addEventListener("hidden.bs.modal", closeDealModal);

const submitDeal = async () => {
    if (!await mySer.CheckValidForm("mainFormDeal")) return;
    
    if (!await mySer.CheckValidForm("extraCustomerPanel")) {
        toastifyError("مشخصات حقوقی شرکت را کامل وارد کنید");
        toggleExtraPanel(true);
        return;
    }
    try {
        FLshowLoading();
        
        const data = mySer.convertFormToJSON("formDeal");
        const model = { ...currentDealData, ...data };
        if (model.customerId.startsWith("custom")) model.customerId = null;
        if (!model.cusWorkSenf) model.cusWorkSenf = null;
        if (!model.cusAcquaintanceWay) model.cusAcquaintanceWay = null;
        
        const res = await axios.post(`/Deal/AddDeal`, model);
        if (res.status === 200) {
            await getAllDeals();
            toastifySuccess(isEditMode ? "با موفقیت ویرایش شد" : "سفارش جدید ثبت شد");
            FLshowLoading(false);
            
            closeDealModal();
        } else if (res.status === 201) {
            toastifyError(res.data);
            FLshowLoading(false);
        }
    } catch {
        setTimeout(async () => await submitDeal(), 3000);
    }
};

const dealMobileElm = document.getElementById("dealMobile");
const initSelectTom = async () => {
    const [customerRes, workSenfRes, acquaintanceWayRes] = await Promise.all([
        axios("/Customer/GetAllCustomers"),
        axios("/BaseItem/GetItemsOp?typeCode=1"),
        axios("/BaseItem/GetItemsOp?typeCode=2"),
    ]);
    selectTomCustomers = new TomSelect("#dealCustomersTomSelect", {
        options: customerRes.data.map(c => ({ text: `${c.customerName} (کد مشتری: ${c.customerCode})`, value: c.id })),
        maxItems: 1,
        persist: false,
        openOnFocus: false,
        createOnBlur: true,
        selectOnTab: true,
        placeholder: "نام سفارش دهنده ...",
        
        createFilter: () => canCreateCustomerTomSelect,
        create: (input, callback) => callback({ value: `custom-${input}`, text: input }),
        
        async onChange(value) {
            if (!value) return;
            if (!value.startsWith("custom")) {
                toggleElementsDisabled(dealMobileElm, true);
                selectTomCustomers.disable();
                
                const res = await axios(`/Customer/GetCustomer/${value}`);
                setDealDataByCustomer(res.data);
                
                selectTomCustomers.enable();
                toggleElementsDisabled(dealMobileElm, false);
            } else {
                const opt = selectTomCustomers.options[value];
                
                currentDealData.customerCode = null;
                currentDealData.customerName = opt.text.trim();
            }
        },
        
        onDelete(values) {
            const value = values[0];
            if (value.startsWith("custom")) {
                const opt = selectTomCustomers.options[value];
                
                selectTomCustomers.removeItem(value, true);
                selectTomCustomers.setTextboxValue(opt.text.trim());
                selectTomCustomers.refreshOptions(true);
                return false;
            }
            
            
            mySer.emptyForm("extraCustomerPanel");
            currentDealData.cusWorkSenfTxt = "";
            currentDealData.cusAcquaintanceWayTxt = "";
            
            currentDealData.customerCode = null;
            currentDealData.customerId = null;
            currentDealData.customerName = "";
            dealMobileElm.tomselect.clear(true);
            return true;
        },
    });
    
    new TomSelect(dealMobileElm, {
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
            
            selectTomCustomers.setValue(customer.id);
            flashItem(selectTomCustomers.control);
        },
    });
    const dealMobileTsInput = document.getElementById("dealMobile-ts-control");
    dealMobileTsInput.setAttribute("inputmode", "numeric");
    dealMobileTsInput.addEventListener("input", handlerMobileInput);
    
    
    const selectTomWorkSenf = new TomSelect("#workSenfIDDeal", {
        options: workSenfRes.data,
        valueField: "id",
        maxItems: 1,
        selectOnTab: true,
        onItemAdd(value) {
            const opt = selectTomWorkSenf.options[value];
            currentDealData.cusWorkSenfTxt = opt.text.trim();
        },
    });
    
    const selectTomAcquaintanceWay = new TomSelect("#acquaintanceWayDeal", {
        options: acquaintanceWayRes.data,
        valueField: "id",
        maxItems: 1,
        selectOnTab: true,
        onItemAdd(value) {
            const opt = selectTomAcquaintanceWay.options[value];
            currentDealData.cusAcquaintanceWayTxt = opt.text.trim();
        },
    });
};

const setDealDataByCustomer = (data) => {
    const cusDealData = {
        cusNumberRegister: data.numberRegister,
        cusEghtesadiCode: data.eghtesadiCode,
        cusShenaseMeli: data.shenaseMeli,
        cusMelliCode: data.agentMeliCode,
        cusPostalCode: data.postalCode,
        cusJuridical: data.juridical,
        cusCompanyName: data.companyName,
        cusAddress: data.address,
        cusTel: data.tel,
        cusSex: data.sex,
        cusProvince: data.province,
        cusCity: data.city,
        cusWorkSenf: data.workSenf || null,
        cusWorkSenfTxt: data.workSenfTxt,
        cusWorkSenfDescription: data.workSenfDescription,
        cusAcquaintanceWay: data.acquaintanceWay || null,
        cusAcquaintanceWayTxt: data.acquaintanceWayTxt,
        mobileNumber: data.mobile ?? "",
        customerCode: data.customerCode,
        customerName: `${data.companyName ?? ""} ${data.agentName ?? ""}`.trim(),
        //customerName: data.agentName?.trim(),
        customerId: data.id,
    };
    
    //selectTomCustomers.setValue(data.id, true);
    mySer.convertJsonToForm(cusDealData, "formDeal");
    currentDealData = { ...currentDealData, ...cusDealData };
};

/*dealMobileElm.addEventListener("keydown", async (e) => {
    if (e.key === "Enter" && e.target.value.length > 8) {
        const mobile = e.target.value;
        const res = await axios(`/Customer/GetCustomerByMobile?mobile=${mobile}`);
        if (res.status === 200) {
            showExistingCustomerToast(res.data);
        }
    }
});*/

/*dealMobileElm.addEventListener("blur", async (e) => {
    if (e.target.value.length > 8) {
        const mobile = e.target.value;
        const res = await axios(`/Customer/GetCustomerByMobile?mobile=${mobile}`);
        if (res.status === 200) {
            showExistingCustomerToast(res.data);
        }
    }
});*/

function showExistingCustomerToast(cus) {
    const wrapper = document.createElement("div");
    const cusName = `مشتری ${cus.customerCode ?? ""} ${cus.companyName ?? ""} (${(cus.Sex === 1 ? "جناب آقای" : "سرکار خانم")} ${cus.agentName ?? ""})`;
    
    wrapper.innerHTML = `
        <div class="text-center" style="line-height:1.8">
          <div class="fMedium" style="font-size: 16px">مشتری با این شماره موبایل قبلا در سیستم ثبت شده است!</div>
          <div>${cusName}</div>
          <div class="d-flex justify-content-center gap-2 mt-2">
            <button id="toastAddBtn" class="btn btn-sm btn-light">افزودن</button>
            <button id="toastCancelBtn" class="btn btn-sm btn-outline-light">انصراف</button>
          </div>
        </div>
    `;
    
    const toast = toastifyError2("", { node: wrapper, duration: -1, style: { cursor: "auto" } });
    
    wrapper.querySelector("#toastAddBtn").onclick = () => {
        setDealDataByCustomer(cus);
        toast.hideToast();
    };
    
    wrapper.querySelector("#toastCancelBtn").onclick = () => {
        toast.hideToast();
    };
}


function initSortableDeal() {
    let tabSwitchTimer = null;
    let isDragging = false;
    const overlayElm = document.getElementById("overlay");
    
    document.querySelectorAll(".deal-column-list").forEach(list => {
        new Sortable(list, {
            group: "sharedDeal",
            draggable: ".deal-card",
            filter: ".suspend-deal",
            sort: false,
            animation: 150,
            touchStartThreshold: 120,
            swapThreshold: 0.7,
            delay: 500,
            delayOnTouchOnly: true,
            ghostClass: "lightGray-background-class",
            //disabled: window.innerWidth < 1200,
            onAdd: async (evt) => await changeStatusDeal(evt.item.dataset.id, Number(evt.to.dataset.status)),
            onChange: (evt) => {
                document.querySelectorAll(".deal-column-list").forEach(c => c.classList.remove("drop-target"));
                evt.to.classList.add("drop-target");
            },
            onEnd: () => {
                document.querySelectorAll(".deal-column-list").forEach(c => c.classList.remove("drop-target"));
                clearTimeout(tabSwitchTimer);
                tabSwitchTimer = null;
                if (window.innerWidth < 1200) {
                    isDragging = false;
                    overlayElm.classList.remove("show");
                    document.body.classList.remove("drag-active");
                }
            },
            onStart: () => {
                if (window.innerWidth < 1200) {
                    isDragging = true;
                    overlayElm.classList.add("show");
                    document.body.classList.add("drag-active");
                }
            },
        });
    });
    
    
    const tabs = document.querySelectorAll("[data-bs-toggle=\"tab\"]");
    
    tabs.forEach(tab => {
        tab.addEventListener("dragover", (e) => {
            e.preventDefault();
            if (!tabSwitchTimer) {
                tabSwitchTimer = setTimeout(() => {
                    const bsTab = new bootstrap.Tab(tab);
                    bsTab.show();
                    tabSwitchTimer = null;
                }, 500);
            }
        });
        tab.addEventListener("dragleave", () => {
            clearTimeout(tabSwitchTimer);
            tabSwitchTimer = null;
        });
    });
    
    document.addEventListener("touchmove", (e) => {
        if (!isDragging) return;
        const touch = e.touches[0];
        const elem = document.elementFromPoint(touch.clientX, touch.clientY);
        if (!elem) return;
        const tab = elem.closest("[data-bs-toggle=\"tab\"]");
        if (tab) {
            if (!tabSwitchTimer) {
                tabSwitchTimer = setTimeout(() => {
                    const bsTab = new bootstrap.Tab(tab);
                    bsTab.show();
                    tabSwitchTimer = null;
                }, 800);
            }
        } else {
            clearTimeout(tabSwitchTimer);
            tabSwitchTimer = null;
        }
    });
}

async function changeStatusDeal(id, targetStatus) {
    try {
        FLshowLoading();
        const res = await axios(`/Deal/ChangeStatusDeal?id=${id}&status=${targetStatus}`);
        if (res.status === 200) {
            await getAllDeals();
            FLshowLoading(false);
        }
    } catch (err) {
        setTimeout(async () => await changeStatusDeal(evt), 3000);
    }
}

function removeDealsTabsDesktop() {
    if (window.innerWidth >= 1200) {
        document.getElementById("allDealsContainer")?.classList.remove("tab-content");
        document.getElementById("openDealTab-pane")?.classList.remove("tab-pane", "fade");
        document.getElementById("successDealTab-pane")?.classList.remove("tab-pane", "fade");
        document.getElementById("failedDealTab-pane")?.classList.remove("tab-pane", "fade");
    } else {
        document.getElementById("allDealsContainer")?.classList.add("tab-content");
        document.getElementById("openDealTab-pane")?.classList.add("tab-pane", "fade");
        document.getElementById("successDealTab-pane")?.classList.add("tab-pane", "fade");
        document.getElementById("failedDealTab-pane")?.classList.add("tab-pane", "fade");
    }
    window.addEventListener("resize", removeDealsTabsDesktop);
}


toggleExtraPanelBtn?.addEventListener("click", () => {
    toggleExtraPanel(extraCustomerPanel.classList.contains("d-none"));
});

function toggleExtraPanel(show) {
    const modalDialog = dealModalElm.querySelector(".modal-dialog");
    
    extraCustomerPanel.classList.toggle("d-none", !show);
    modalDialog.classList.toggle("modal-xl", show);
}

dealModalElm.querySelectorAll("input[name='cusJuridical']").forEach((elm) => {
    elm.addEventListener("change", (e) => {
        const juridicalInfoElm = document.getElementById("juridicalInfoDeal");
        const showJuridicalInfo = Number(e.target.value) === 1;
        juridicalInfoElm.classList.toggle("d-none", !showJuridicalInfo);
        
        toggleElementsRequired(juridicalInfoElm.querySelectorAll("input"), showJuridicalInfo);
    });
});

