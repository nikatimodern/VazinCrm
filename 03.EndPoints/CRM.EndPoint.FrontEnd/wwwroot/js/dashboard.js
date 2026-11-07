let modelDash = { filt: {} };
var systematicTask = 0;
var filterDashboardConfig = { filterVar: {} };

document.addEventListener("DOMContentLoaded", async () => {
    enableMomentumScrollScope(document.getElementById("dashboardWorkspace"));
    enableMomentumScroll(document.scrollingElement);
    const tasks = getAllTask(1, 0, 2);
    try {
        currentMonthArr = await myDate.getCurrentMonth();
        showTaskUsersDropdown();
        showInfoFactors();

        const filConfigCurrentMonth = {
            ContentFilterDiv: "ContentDashboardFilterId",
            filterCallBack: "callbackFilterDashboard",
            filterVar: {
                DateFilterChk: true,
                fromDate: currentMonthArr[0],
                toDate: currentMonthArr[1],
            },
            filterVarConfig: "filterDashboardConfig",
        };
        document.getElementById("DateFilterChk").setAttribute("disabled", "true");
        document.querySelector("#dateFilterBtnId .monthBtn").classList.add("bgGreen2");
        await FilterAccepted(filConfigCurrentMonth);
        await tasks;
    } catch (error) {
        console.error("Dashboard initialization failed:", error);
        document.querySelectorAll('.dashboard-value:empty').forEach(value => { value.textContent = '—'; });
        document.querySelectorAll('.dashboard-skeleton-list').forEach(skeleton => {
            const feedback = document.createElement('p');
            feedback.className = 'dashboard-load-error';
            feedback.setAttribute('role', 'status');
            feedback.textContent = 'آماده‌سازی داشبورد انجام نشد. صفحه را دوباره بارگذاری کنید.';
            skeleton.replaceWith(feedback);
        });
    }


    /*const cpuValueFromServer = Math.random() * 100;
    renderTargetGaugeChart(cpuValueFromServer);*/
});


let dashboardRefresh = Promise.resolve();
function UpdateDash() {
    dashboardRefresh = dashboardRefresh.catch(() => {}).then(refreshDashboardSections);
    return dashboardRefresh;
}

async function refreshDashboardSections() {
    await Promise.allSettled([
        getDashReports(),
        getDashFactors(),
        getOrderCount(),
        getActiveCustomerCount(),
        getInfoFactors(),
        getReportsCount(),
        getLogSystem(),
        getFinancialReport(),
    ]);
}


const showInfoFactors = () => {
    const infoFactorsIcon = document.getElementById("infoFactors-icon");
    if (!infoFactorsIcon) return;
    const infoFactorsDropdown = document.getElementById("infoFactors-dropdown");

    const isTouchDevice = "ontouchstart" in window;

    if (!infoFactorsIcon.dropdownInstance) {
        infoFactorsIcon.dropdownInstance = new Dropdown({
            button: infoFactorsIcon,
            content: infoFactorsDropdown,
            effect: "slide",
            transitionDuration: 200,
            placement: "right",
            zIndex: 1100,
        });
    }

    if (isTouchDevice) {
        infoFactorsIcon.addEventListener("click", (e) => {
            e.stopPropagation();
            infoFactorsIcon.dropdownInstance.toggle();
        });

        document.addEventListener("click", (e) => {
            if (!infoFactorsDropdown.contains(e.target) && !infoFactorsIcon.contains(e.target)) {
                infoFactorsIcon.dropdownInstance.close();
            }
        });
    } else {
        let hoverTimeout;
        const handleMouseEnter = () => {
            clearTimeout(hoverTimeout);
            infoFactorsIcon.dropdownInstance.open();
        };

        const handleMouseLeave = () => {
            hoverTimeout = setTimeout(() => infoFactorsIcon.dropdownInstance.close(), 300);
        };

        infoFactorsIcon.addEventListener("pointerenter", handleMouseEnter);
        infoFactorsDropdown.addEventListener("pointerenter", handleMouseEnter);

        infoFactorsIcon.addEventListener("pointerleave", handleMouseLeave);
        infoFactorsDropdown.addEventListener("pointerleave", handleMouseLeave);
    }
};

const getDashReports = async () => {
    return dashboardLoading.run("allReportsDashboardId", async () => {
        const reportContainer = document.getElementById("allReportsDashboardId");
        try {
            const model = { ...modelDash, count: 50, _type: -1 };
            const res = await axios.post("/Report/GetAllReportPartial", model);
            if (res.status === 200) {
                reportContainer.innerHTML = res.data.html;
            }
        } catch (err) {
            console.error(err);
            throw err;
        }
    });
};


async function getInfoFactors() {

    const res = await axios.post("/dashboard/GetInfoFactors", modelDash);
    const data = res.data;

    const infoFactorsDropdown = document.getElementById("infoFactors-dropdown");
    infoFactorsDropdown.innerHTML = "";
    const container = document.createElement("div");
    data.forEach((item, index) => {
        const wrapper = document.createElement("div");
        wrapper.className = "user__item";

        wrapper.innerHTML = `
            <div class="d-flex align-items-center gap-2 ${index === 0 ? "infoFactors-firstItem" : ""}">
                <img class="profile-image" style="width:36px;" src="${item.userImage || "/Img/User.png"}" alt="">
                <span style="font-size:13px">${item.sellerName}</span>
            </div>
            <div style="display:flex;gap:2px;font-size:13px">
                <button class="badge badge-gray" onclick="redirectFromDashboard('Cartable', '${item.sellerId}', '${item.sellerName}')" style="border-end-end-radius:0;border-start-end-radius:0">
                    <span class="fbold" style="font-size:13px;">${item.factorCount}</span> فاکتور
                </button>
                <div class="badge badge-green" style="border-end-start-radius:0;border-start-start-radius:0">
                    <span class="fbold direction-ltr" style="font-size:13px;">${item.totalAmount.toLocaleString()}</span> ريال
                </div>
            </div>
        `;

        container.appendChild(wrapper);
    });
    infoFactorsDropdown.appendChild(container);
}


function redirectFromDashboard(targetPage, sellerId = null, sellerName = null) {
    if (sellerId) {
        modelDash.filt.SelFilterChd = true;
        modelDash.filt.selFilter = sellerId;
        modelDash.filt.selFilterTxt = sellerName;
    }

    sessionStorage.setItem("fFilterFromDash", JSON.stringify(modelDash));
    window.location.href = `/${targetPage}/Index`;
}

document.getElementById("systematicTaskDash").addEventListener("change", async (e) => {
    e.target.setAttribute("disabled", "true");
    systematicTask = e.target.value;
    try {
        await getAllTask(1, e.target.value, 2);
    } finally {
        e.target.removeAttribute("disabled");
    }
});

async function getReportsCount() {
    return dashboardLoading.run("reportsCountDash", async () => {
        const data = await axios.post("/dashboard/GetReportsCount", modelDash);
        document.getElementById("reportsCountDash").innerText = data.data;
    });
}
async function getDashFactors() {
    return dashboardLoading.run("totalAmountId", async () => {
        const data = await axios.post("/dashboard/GetDashFactors", modelDash);
        document.getElementById("totalAmountId").innerText = Number(data.data.sumAmount).toLocaleString();
        document.getElementById("totalFactorsCountId").innerText = data.data.count;
        document.getElementById("exportFactorsCount").innerText = data.data.export;
        document.getElementById("notExportFactorsCount").innerText = data.data.noExport;
    });
}

async function getOrderCount() {
    return dashboardLoading.run("ordersAllCount", async () => {
        const res = await axios.post("/dashboard/GetOrderCount", modelDash);
        document.getElementById("ordersAllCount").innerText = res.data.inOrder + res.data.inProgress + res.data.done + res.data.deliver;
        document.getElementById("inOrderCount").innerText = res.data.inOrder;
        document.getElementById("ordersProgressCount").innerText = res.data.inProgress;
        document.getElementById("ordersDeliverCount").innerText = res.data.deliver;
        document.getElementById("ordersDoneCount").innerText = res.data.done;
    });
}

async function getActiveCustomerCount() {
    return dashboardLoading.run("allCustomerCount", async () => {
        const res = await axios.post("/dashboard/getCustomerSuccess", modelDash);
        document.getElementById("allCustomerCount").innerText = res.data.count;
        document.getElementById("exportCustomerCount").innerText = res.data.export;
        document.getElementById("notExportCustomerCount").innerText = res.data.notExport;
    });
}

async function getLogSystem(isRefresh = false) {
    return dashboardLoading.run("LogSystemId", async () => {
        const container = document.getElementById("LogSystemId");
        const data = (await axios.post("/dashboard/GetLogSystem", modelDash)).data;

        container.innerHTML = "";
        const dom = createDomLogSystem(data);
        container.appendChild(dom);

    });
}


async function getFinancialReport() {
    return dashboardLoading.run("financialReportContainer", async () => {
        const container = document.getElementById("financialReportContainer");
        if (!container) return;

        const res = await axios.post("/Dashboard/GetFinancialReport", modelDash);
        if (res.status !== 200) throw new Error("Unexpected dashboard response");

        const data = res.data;

        container.querySelector("#bankBalanceId").textContent = data.bankBalance?.toLocaleString() || "-";
        container.querySelector("#totalDemandId").textContent = data.demands?.toLocaleString() || "-";
        container.querySelector("#totalDebitId").textContent = data.debit?.toLocaleString() || "-";

        container.querySelector("#demandInfo").dataset.bsContent = `<div class="popover-financial">
            <div>جمع کل فاکتور فروش: <b>${(data.factorAmount || 0).toLocaleString()}</b></div>
            <div>جمع واریزها به بانک: <b>${(data.bankBalance || 0).toLocaleString()}</b></div>
            <div>جمع واریز های تهاتری: <b>${(data.tahatori || 0).toLocaleString()}</b></div>
        </div>`;

        container.querySelector("#debitInfo").dataset.bsContent = `<div class="popover-financial">
            <div>جمع کل فاکتورهای خرید: <b>${(data.supplyFactorAmount || 0).toLocaleString()}</b></div>
            <div>جمع پرداختی ها: <b>${(data.paymentAmount || 0).toLocaleString()}</b></div>
            <div>جمع واریزی های تهاتری: <b>${(data.tahatori || 0).toLocaleString()}</b></div>
        </div>`;

        ["#demandInfo", "#debitInfo"].forEach(selector => {
            const el = container.querySelector(selector);
            const pop = bootstrap.Popover.getInstance(el);

            if (pop) {
                pop.dispose();
            }
            });
    });
}


const callbackFilterDashboard = async () => {
    modelDash.filt = window[filterVarConfig].filterVar;
    await UpdateDash();
};


/*function renderTargetGaugeChart(value) {
    const COLORS = ["rgb(140, 214, 16)", "rgb(239, 198, 0)", "rgb(231, 24, 49)"];

    const index = (perc) => perc < 20 ? 2 : perc < 60 ? 1 : 0;

    const annotationConfig = {
        type: "doughnutLabel",
        content: ({ chart }) => [
            chart.data.datasets[0].data[0].toFixed(1) + " %",
            "پیشرفت فروش",
        ],
        drawTime: "beforeDraw",
        position: { y: "-50%" },
        font: [
            { size: 50, family: "IRANSansBold" },
            { size: 18, family: "IRANSans" },
        ],
        color: ({ chart }) => [COLORS[index(chart.data.datasets[0].data[0])], "grey"],
    };

    const data = {
        datasets: [{
            data: [value, 100 - value],
            backgroundColor(ctx) {
                if (ctx.type !== "data") return;
                return ctx.index === 1 ? "rgb(234, 234, 234)" : COLORS[index(ctx.raw)];
            },
        }],
    };

    const ctx = document.getElementById("gaugeChart").getContext("2d");

    new Chart(ctx, {
        type: "doughnut",
        data,
        options: {
            aspectRatio: 2,
            circumference: 180,
            rotation: -90,
            cutout: "70%",
            hoverOffset: 8,

            plugins: {
                tooltip: {
                    backgroundColor: "#333",
                    titleColor: "#fff",
                    bodyColor: "#eee",
                    titleFont: { size: 14, family: "IRANSansBold" },
                    bodyFont: { size: 13, family: "IRANSans" },
                },
                annotation: {
                    annotations: { annotationConfig },
                },
            },
        },
    });
}
// renderOrdersBarChart();

function renderOrdersBarChart() {
    // const labels = ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"];
    const labels = ["فروردین", "اردیبهشت", "خرداد","شهریور", "مهر", "آبان"];

    const getRandomData = () => {
        const randomData = Array.from({ length: labels.length }, () =>
            Math.floor(Math.random() * 200),
        );
        return randomData;
    };

    const data = {
        labels: labels,
        datasets: [
            {
                label: "ثبت اولیه",
                data: getRandomData(),
                backgroundColor: "rgb(0 103 139)",
            },
            {
                label: "مرحله تولید",
                data: getRandomData(),
                backgroundColor: "rgb(137 139 0)",
            },
            {
                label: "آماده تحویل",
                data: getRandomData(),
                backgroundColor: "rgb(0 131 17)",
            },
            {
                label: "تحویل شده",
                data: getRandomData(),
                backgroundColor: "rgb(120 120 120)",
            },
        ],
    };

    const ctx = document.getElementById("ordersChart");
    new Chart(ctx, {
        type: "bar",
        data: data,
        options: {
            plugins: {
                title: {
                    display: false,
                    text: "Chart.js Bar Chart - Stacked",
                },
            },
            responsive: true,
            scales: {
                x: {
                    stacked: true,
                },
                y: {
                    stacked: true,
                },
            },
        },
    });
}*/

