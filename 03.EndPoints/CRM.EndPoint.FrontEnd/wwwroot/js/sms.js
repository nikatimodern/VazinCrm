const syncReceivesBtn = document.getElementById("syncReceivesBtn");
const syncSentSmsBtn = document.getElementById("syncSentSmsBtn");

document.addEventListener("DOMContentLoaded", async () => {
    initSmsTabs();
    initSmsTabTable("receivesTab");
    await loadSmsDashboard();
    
    /*    document.getElementById("refreshSmsBtn")?.addEventListener("click", async () => {
            await loadSmsDashboard();
            $("#smsReportsTable").DataTable().ajax.reload(null, false);
            $("#smsReceivesTable").DataTable().ajax.reload(null, false);
        });*/
});

syncReceivesBtn.addEventListener("click", async () => {
    const spinner = syncReceivesBtn.querySelector(".spinner-border");
    const syncIcon = syncReceivesBtn.querySelector(".sync-icon");
    
    toggleElementsDisabled(syncReceivesBtn);
    spinner.classList.remove("d-none");
    syncIcon.classList.add("d-none");
    
    await sendApiRequest({
        url: "/Sms/SyncReceives",
        method: "POST",
        showLoading: false,
        successMsg: "پیامک‌های جدید با موفقیت همگام‌سازی شدند",
        onSuccess: () => {
            $("#smsReceivesTable").DataTable().ajax.reload(null, false);
        },
    });
    
    toggleElementsDisabled(syncReceivesBtn, false);
    spinner.classList.add("d-none");
    syncIcon.classList.remove("d-none");
});


syncSentSmsBtn.addEventListener("click", async () => {
    const spinner = syncSentSmsBtn.querySelector(".spinner-border");
    const syncIcon = syncSentSmsBtn.querySelector(".sync-icon");
    
    toggleElementsDisabled(syncSentSmsBtn);
    spinner.classList.remove("d-none");
    syncIcon.classList.add("d-none");
    
    
    await sendApiRequest({
        url: "/Sms/SyncSents",
        method: "POST",
        showLoading: false,
        successMsg: "پیامک‌های جدید با موفقیت همگام‌سازی شدند",
        onSuccess: () => {
            $("#smsReportsTable").DataTable().ajax.reload(null, false);
        },
    });
    
    toggleElementsDisabled(syncSentSmsBtn, false);
    spinner.classList.add("d-none");
    syncIcon.classList.remove("d-none");
});

function initSmsTabs() {
    document.querySelectorAll(".sms-tab").forEach(tab => {
        tab.addEventListener("click", () => {
            document.querySelectorAll(".sms-tab").forEach(x => x.classList.remove("active"));
            document.querySelectorAll(".sms-tab-content").forEach(x => x.classList.add("d-none"));
            
            tab.classList.add("active");
            
            const target = tab.dataset.target;
            
            document.getElementById(target)?.classList.remove("d-none");
            initSmsTabTable(target);
        });
    });
}

async function loadSmsDashboard() {
    await sendApiRequest({
        url: "/Sms/Dashboard",
        showLoading: false,
        onSuccess: async (res) => {
            const data = res.data;
            
            document.getElementById("smsCredit").textContent = data.credit != null ? `${Number(data.credit).toLocaleString()} تومان` : "-";
            document.getElementById("smsProvider").textContent = data.provider || "-";
            document.getElementById("smsLines").textContent = data.lines.map(l => l.trim()).join(", ");
        },
    });
}

function initSmsTabTable(target) {
    switch (target) {
        case "reportsTab":
            initSmsReportsTable();
            break;
        
        case "receivesTab":
            initSmsReceivesTable();
            break;
    }
}


function initSmsReportsTable() {
    if ($.fn.DataTable.isDataTable("#smsReportsTable"))
        return;
    
    const table = $("#smsReportsTable").DataTable({
        pageLength: 10,
        lengthMenu: [5, 10, 50, 100],
        processing: true,
        serverSide: true,
        search: {
            return: true,
        },
        ordering: false,
        scrollCollapse: true,
        responsive: true,
        ajax: {
            url: "/Sms/GetReportsDt",
            type: "POST",
            dataSrc: function (json) {
                const reportCountElm = document.getElementById("reportsCount");
                reportCountElm.textContent = (json.recordsFiltered ?? 0).toLocaleString();
                reportCountElm.classList.remove("d-none");
                return json.data || [];
            },
        },
        
        columns: [
            { data: "mobile", render: value => `<span class="sms-mobile">${value ?? "-"}</span>`, responsivePriority: 1 },
            {
                data: "messageText",
                // className: "wrap-text",
                render: value => `<span class="sms-message">${escapeHtml(value ?? "-")}</span>`,
            },
            { data: "number", render: value => `<span class="sms-line">${value ?? "-"}</span>` },
            { data: "typeTxt" },
            { data: "sendDateTimeTxt", render: renderDateTimeHtml, responsivePriority: 2 },
            { data: "deliveryState", render: renderSmsStatus, responsivePriority: 3 },
        ],
        
        language: languageDatatable,
        layout: layoutDatatable,
        
/*        initComplete: () => {
            const wrapper = table.table().container();
            const dtLength = wrapper.querySelector(".dt-length");
            
            const refreshButton = document.createElement("button");
            refreshButton.classList.add("inline-flex-center");
            refreshButton.innerHTML = `<svg style="width: 18px;color: #333" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0 3.181 3.183a8.25 8.25 0 0 0 13.803-3.7M4.031 9.865a8.25 8.25 0 0 1 13.803-3.7l3.181 3.182m0-4.991v4.99" /></svg>`;
            
            refreshButton.addEventListener("click", () => $("#smsReportsTable").DataTable().ajax.reload(null, false));
            dtLength.before(refreshButton);
        },*/
    });
    
}


function initSmsReceivesTable() {
    if ($.fn.DataTable.isDataTable("#smsReceivesTable"))
        return;
    
    $("#smsReceivesTable").DataTable({
        pageLength: 10,
        lengthMenu: [5, 10, 20, 50, 100, { label: "همه", value: -1 }],
        processing: true,
        serverSide: true,
        search: {
            return: true,
        },
        ordering: false,
        scrollCollapse: true,
        responsive: true,
        ajax: {
            url: "/Sms/GetReceivesDt",
            type: "POST",
            dataSrc: function (json) {
                const receivesCountElm = document.getElementById("receivesCount");
                receivesCountElm.textContent = (json.recordsFiltered ?? 0).toLocaleString();
                receivesCountElm.classList.remove("d-none");
                return json.data;
            },
        },
        columns: [
            { data: "mobile", render: value => `<span class="sms-mobile">${value ?? "-"}</span>` },
            { data: "messageText", render: value => `<span class="sms-message">${escapeHtml(value ?? "-")}</span>` },
            { data: "receivedDateTimeTxt", render: renderDateTimeHtml },
            { data: "number", render: value => `<span class="sms-mobile">${value ?? "-"}</span>` },
        ],
        language: languageDatatable,
        layout: layoutDatatable,
    });
}

function renderSmsStatus(state) {
    switch (state) {
        case "sent":
            return `
                <span class="sms-status sms-status-success">
                    <i class="fa-solid fa-check"></i>
                    ارسال شده
                </span>`;
        
        case "in-queue":
            return `
                <span class="sms-status sms-status-pending">
                    <i class="fa-solid fa-clock"></i>
                    در صف ارسال
                </span>`;
        
        case "init":
            return `
                <span class="sms-status sms-status-pending">
                    <i class="fa-solid fa-spinner"></i>
                    در حال آماده‌سازی
                </span>`;
        
        case "pending-approval":
            return `
                <span class="sms-status sms-status-pending">
                    <i class="fa-solid fa-hourglass-half"></i>
                    در انتظار تأیید
                </span>`;
        
        case "insufficient-balance":
            return `
                <span class="sms-status sms-status-error">
                    <i class="fa-solid fa-wallet"></i>
                    موجودی ناکافی
                </span>`;
        
        case "cancelled":
            return `
                <span class="sms-status sms-status-error">
                    <i class="fa-solid fa-ban"></i>
                    لغو شده
                </span>`;
        
        case "rejected":
            return `
                <span class="sms-status sms-status-error">
                    <i class="fa-solid fa-xmark"></i>
                    رد شده
                </span>`;
        
        default:
            return `
                <span class="sms-status sms-status-unknown">
                    <i class="fa-solid fa-question"></i>
                    نامشخص
                </span>`;
    }
}


function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#039;");
}