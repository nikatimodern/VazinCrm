const syncSmsBtn = document.getElementById("syncSmsBtn");

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

syncSmsBtn.addEventListener("click", async () => {
    const spinner = syncSmsBtn.querySelector(".spinner-border");
    const syncIcon = syncSmsBtn.querySelector(".sync-icon");
    
    toggleElementsDisabled(syncSmsBtn);
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
    
    toggleElementsDisabled(syncSmsBtn, false);
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
    
    $("#smsReportsTable").DataTable({
        pageLength: 10,
        lengthMenu: [[5, 10, 50, 100], [5, 10, 50, 100]],
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
            { data: "mobile", render: value => `<span class="sms-mobile">${value ?? "-"}</span>` },
            {
                data: "messageText",
                // className: "wrap-text",
                render: value => `<span class="sms-message" title="${escapeHtml(value ?? "")}">${escapeHtml(value ?? "-")}</span>`,
            },
            { data: "number", render: value => `<span class="sms-line">${value ?? "-"}</span>` },
            { data: "typeTxt" },
            { data: "sendDateTimeTxt", render: renderDateTimeHtml },
            { data: "deliveryState", render: renderSmsStatus },
        ],
        
        language: languageDatatable,
        layout: layoutDatatable,
    });
}


function initSmsReceivesTable() {
    if ($.fn.DataTable.isDataTable("#smsReceivesTable"))
        return;
    
    const table = $("#smsReceivesTable").DataTable({
        pageLength: 10,
        lengthMenu: [[5, 10, 50, 100], [5, 10, 50, 100]],
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
    
    table.on("draw.dt", togglePageLength);
    
    function togglePageLength() {
        const searchValue = table.search();
        const wrapper = table.table().container();
        
        wrapper.querySelector(".dt-length").classList.toggle("d-none", searchValue);
    }
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

function formatSmsDate(value) {
    if (!value)
        return "-";
    
    const number = Number(value);
    
    if (!Number.isFinite(number))
        return "-";
    
    const date = new Date(number * 1000);
    
    if (Number.isNaN(date.getTime()))
        return "-";
    
    return new Intl.DateTimeFormat("fa-IR", {
        dateStyle: "short",
        timeStyle: "short",
    }).format(date);
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#039;");
}