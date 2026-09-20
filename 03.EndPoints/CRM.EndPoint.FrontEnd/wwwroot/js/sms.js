document.addEventListener("DOMContentLoaded", async () => {
    initSmsTabs();
    initSmsReportsTable();
    initSmsReceivesTable();
    await loadSmsDashboard();
    
    document.getElementById("refreshSmsBtn")
            ?.addEventListener("click", async () => {
                await loadSmsDashboard();
                $("#smsReportsTable").DataTable().ajax.reload(null, false);
                $("#smsReceivesTable").DataTable().ajax.reload(null, false);
            });
});

function initSmsTabs() {
    document.querySelectorAll(".sms-tab").forEach(tab => {
        tab.addEventListener("click", () => {
            document.querySelectorAll(".sms-tab").forEach(x =>
                x.classList.remove("active"));
            
            document.querySelectorAll(".sms-tab-content").forEach(x =>
                x.classList.add("d-none"));
            
            tab.classList.add("active");
            
            const target = tab.dataset.tab === "reports"
                ? "reportsTab"
                : "receivesTab";
            
            document.getElementById(target)?.classList.remove("d-none");
        });
    });
}

async function loadSmsDashboard() {
    const response = await fetch("/Sms/Dashboard");
    
    if (!response.ok)
        return;
    
    const data = await response.json();
    
    document.getElementById("smsCredit").textContent =
        data.credit != null
            ? `${Number(data.credit).toLocaleString()} تومان`
            : "-";
    
    document.getElementById("smsProvider").textContent =
        data.provider || "-";
    
    document.getElementById("smsLines").textContent =
        data.lines?.length
            ? data.lines.length.toLocaleString()
            : "۰";
}

function initSmsReportsTable() {
    $("#smsReportsTable").DataTable({
        processing: true,
        serverSide: false,
        ajax: {
            url: "/Sms/Reports",
            dataSrc: function (data) {
                document.getElementById("reportsCount").textContent =
                    (data?.length ?? 0).toLocaleString();
                
                return data;
            },
        },
        columns: [
            {
                data: "mobile",
                render: value =>
                    `<span class="sms-mobile">${value ?? "-"}</span>`,
            },
            {
                data: "messageText",
                render: value =>
                    `<span class="sms-message" title="${escapeHtml(value ?? "")}">
                        ${escapeHtml(value ?? "-")}
                    </span>`,
            },
            {
                data: "deliveryState",
                render: value => renderSmsStatus(value),
            },
            {
                data: "sendDateTime",
                render: value => formatSmsDate(value),
            },
            {
                data: "deliveryDateTime",
                render: value => formatSmsDate(value),
            },
        ],
        pageLength: 20,
        lengthMenu: [
            [20, 50, 100, -1],
            [20, 50, 100, "همه"],
        ],
        order: [[3, "desc"]],
    });
}

function initSmsReceivesTable() {
    $("#smsReceivesTable").DataTable({
        processing: true,
        serverSide: false,
        ajax: {
            url: "/Sms/Receives",
            dataSrc: function (data) {
                document.getElementById("receivesCount").textContent =
                    (data?.length ?? 0).toLocaleString();
                
                return data;
            },
        },
        columns: [
            {
                data: "mobile",
                render: value =>
                    `<span class="sms-mobile">${value ?? "-"}</span>`,
            },
            {
                data: "messageText",
                render: value =>
                    `<span class="sms-message" title="${escapeHtml(value ?? "")}">
                        ${escapeHtml(value ?? "-")}
                    </span>`,
            },
            {
                data: "number",
                render: value =>
                    `<span class="sms-mobile">${value ?? "-"}</span>`,
            },
            {
                data: "receivedDateTime",
                render: value => formatSmsDate(value),
            },
        ],
        pageLength: 20,
        lengthMenu: [[20, 50, 100, -1], [20, 50, 100, "همه"]],
        order: [[3, "desc"]],
    });
}

function renderSmsStatus(state) {
    if (state == null)
        return `<span class="sms-status sms-status-unknown">نامشخص</span>`;
    
    switch (Number(state)) {
        case 1:
            return `
                <span class="sms-status sms-status-success">
                    <i class="fa-solid fa-check"></i>
                    تحویل شده
                </span>`;
        
        case 0:
            return `
                <span class="sms-status sms-status-pending">
                    <i class="fa-solid fa-clock"></i>
                    در انتظار
                </span>`;
        
        default:
            return `
                <span class="sms-status sms-status-error">
                    <i class="fa-solid fa-xmark"></i>
                    ناموفق
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