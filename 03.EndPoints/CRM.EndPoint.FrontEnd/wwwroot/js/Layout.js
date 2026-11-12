var showFlag = 0;


// var userRol = null;
// var rol;
var currentUser;


var attachInvoiceList = [];
var setting = null;

//متغییر های فیلترینگ و سرچ کارتابل پیش فاکتور و فاکتور
var varList = {};//موارد فیلترینگ
var varList2 = {};//موارد فیلترینگ


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


var currentDateTypeFilter = 2;
var currentMonthArr;
let mySer = new MyService();
let CrmSer = new CrmService();
let myDate = new GetDate();
document.addEventListener("DOMContentLoaded", async () => {
    await GetUserLogin();
    
    setting = await GetSetting();
    
    
    AcceptSetting(setting);
    
    
    initTooltipsIfCollapsed();
    dateTimePicker();
    
    getRemind();
    setDateTime();
    
    Mask();
    MaskDate();
    
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

connection.on("TaskChanged", id => {
    if (document.getElementById("allCarizoContainer") || document.getElementById("todayTaskDashId")) {
        refreshTaskCards(id);
    }
});

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


const checkAccess = async (elId) => {
    let rols = JSON.parse(sessionStorage.getItem("Rols"));
    let hasAccess = false;
    
    rols.forEach((rol) => {
        if (document.getElementById(elId) && document.getElementById(elId).classList.contains(rol?.name)) {
            hasAccess = true;
            return hasAccess;
        }
        
    });
    return hasAccess;
};


(function () {
    const baseZ = 1060;
    const step = 10;
    const stack = [];
    let preventBackHandler = null;
    
    document.addEventListener("show.bs.modal", (e) => {
        const modal = e.target;
        
        if (!stack.includes(modal)) {
            stack.push(modal);
        }
        
        if (window.innerWidth <= 768 && stack.length === 1) {// وقتی اولین مودال باز میشه، بک رو ببند
            history.pushState(null, "", location.href);
            
            preventBackHandler = () => history.go(1);
            window.addEventListener("popstate", preventBackHandler);
        }
        
        setTimeout(() => updateZIndex(), 200);
    });
    
    document.addEventListener("hidden.bs.modal", (e) => {
        const modal = e.target;
        
        const idx = stack.indexOf(modal);
        if (idx > -1) stack.splice(idx, 1);
        
        if (stack.length === 0 && preventBackHandler) { // وقتی آخرین مودال بسته شد، بک رو آزاد کن
            window.removeEventListener("popstate", preventBackHandler);
            preventBackHandler = null;
        }
        
        updateZIndex();
    });
    
    function updateZIndex() {
        stack.forEach((m, i) => {
            const z = baseZ + i * step;
            m.style.zIndex = z;
            
            const backdrop = document.querySelectorAll(".modal-backdrop")[i];
            if (backdrop) backdrop.style.zIndex = z - 5;
        });
        
        document.body.classList.toggle("modal-open", stack.length > 0);
    }
})();

//////////////notification///////////////

function notification(id, title, description, dateMiladi, entityType, entityId) {
    
    const notifDiv = document.createElement("div");
    notifDiv.className = "d-flex flex-column p-0 justify-content-evenly h-100 w-100";
    notifDiv.style.position = "relative";
    notifDiv.id = id;
    
    notifDiv.innerHTML = `
        <div style="display:flex;align-items:flex-start;gap:6px;width:100%;padding:8px 12px;">
            <div style="width: clamp(32px, 4vw, 44px); color: #6a1b9a; margin-top: -3px">
                <img src="/Img/toDo.gif" style="width: 100%" alt="">
            </div>
            <div style="flex: 1; display: flex; flex-direction: column; gap: clamp(5px, 0.6vw, 6px);">
                <div style="display: flex; justify-content: space-between; align-items: start;">
                    <span class="fbold" style="font-size: clamp(12px, 1.2vw, 14px); color: #3a005d">${title}</span>
                    <div class="d-flex align-items-center gap-1" style="color: #6a1b9a;">
                        <span id="counterNotif" style="line-height: 1; font-size: 12px;"></span>
                        <span class="d-flex align-items-center justify-content-center" style="cursor:pointer;" onclick="dismissNotif('${id}', this)">
                            <svg style="width: 16px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
                            </svg>
                        </span>
                    </div>
                </div>
                <div style="font-size: clamp(11px, 1.1vw, 13px); color: #212529; white-space: pre-line;">${description}</div>
                <div class="d-flex align-items-start justify-content-between gap-2 w-100">
                    <div class="d-flex align-items-center" style="gap: 4px">
                        <span style="font-size:clamp(9px, 2vw, 10px);color:#8f8f8f" id="relativeTime"></span>
                        <span class="d-flex align-items-end gap-1 snooze-notification" style="font-size:clamp(9px, 2vw, 10px);cursor:pointer;color:#000" onclick="snoozeNotif('${id}', this)">
                            <svg style="width:19px;height:19px" fill="none" viewBox="0 0 24 24"><g fill="currentColor"><path d="m19.6916 7.34849c-.25-.33-.72-.4-1.05-.15s-.4.72-.15 1.05c1.08 1.44 1.65 3.12001 1.65 4.86001 0 4.49-3.65 8.14-8.14 8.14-4.49004 0-8.14004-3.65-8.14004-8.14 0-4.49001 3.65-8.13001 8.14004-8.13001.58 0 1.17.07 1.81.22.03.01.06 0 .1 0 .02 0 .05.02.07.02.03 0 .05-.01.08-.01.04 0 .07-.01.1-.02.05-.01.1-.03.15-.06.03-.02.07-.03.1-.05.01-.01.03-.01.04-.02.03-.02.04-.05.06-.07.04-.04.07-.07.1-.12.03-.04.04-.09.06-.13.01-.03.03-.06.04-.09 0-.02 0-.03 0-.05.01-.05.01-.1 0-.15 0-.05 0-.09-.01-.14-.01-.04-.03-.08-.05-.13s-.04-.1-.07-.14c-.01-.02-.01-.03-.02-.04l-1.98-2.47c-.26-.32-.73-.37-1.05-.12-.32.26-.37.73-.12 1.05l.82 1.02c-.08 0-.16-.01-.24-.01-5.31004 0-9.64004 4.32-9.64004 9.64001 0 5.32 4.32 9.64 9.64004 9.64 5.32 0 9.64-4.32 9.64-9.64.01-2.07-.67-4.06001-1.94-5.76001z" /><path d="m9.5415 16.6708c-.41 0-.75-.34-.75-.75v-3.39l-.19.22c-.28.31-.75.33-1.06.06-.31-.28-.33-.75-.06-1.06l1.5-1.67c.21-.22999.54-.30999.83-.19999s.48.38999.48.69999v5.35c0 .41-.33.74-.75.74z" /><path d="m14 16.6703c-1.52 0-2.75-1.23-2.75-2.75v-1.35c0-1.52 1.23-2.74999 2.75-2.74999s2.75 1.22999 2.75 2.74999v1.35c0 1.52-1.23 2.75-2.75 2.75zm0-5.34c-.69 0-1.25.56-1.25 1.25v1.35c0 .69.56 1.25 1.25 1.25s1.25-.56 1.25-1.25v-1.35c0-.69-.56-1.25-1.25-1.25z" /></g></svg>
                            <span>تعویق</span>
                        </span>
                    </div>
                    <div class="d-flex align-items-center justify-content-center" style="cursor:pointer">
                        <button onclick="readNotifAction('${id}', this, '${entityType}', '${entityId}')" style="font-size: clamp(10px, 1vw, 12px); padding: clamp(3px, 0.5vw, 4px) clamp(6px, 1vw, 8px);background: #6a1b9a; color: #fff; border: none; border-radius: 4px; cursor: pointer;">
                          مشاهده
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    notifDiv.querySelector("#relativeTime").innerHTML = getRelativeTime(dateMiladi);
    /*const updateTimeId = setInterval(() => {
        notifDiv.querySelector("#relativeTime").innerHTML = getRelativeTime(dateMiladi);
    }, 60000);*/
    
    
    let i = 7;
    let paused = false;
    notifDiv.querySelector("#counterNotif").innerText = i;
    const counterNotifId = setInterval(() => {
        if (!paused) {
            i--;
            notifDiv.querySelector("#counterNotif").innerText = i;
            if (i <= 0) {
                //clearAllInterval();
                toastInstance.hideToast();
            }
        }
    }, 1000);
    
    notifDiv.addEventListener("mouseenter", () => paused = true);
    notifDiv.addEventListener("mouseleave", () => paused = false);
    
    const clearAllInterval = () => {
        clearInterval(counterNotifId);
        handleNotifAction(id, false);
        //clearInterval(updateTimeId);
    };
    
    const isMobile = window.innerWidth <= 768;
    
    const toastInstance = Toastify({
        node: notifDiv,
        duration: i * 1000,
        close: true,
        gravity: isMobile ? "top" : "bottom",
        position: isMobile ? "center" : "left",
        style: {
            background: "#f3e5f5",
            //border: "1px solid #6a1b9a1a",
            borderLeft: `5px solid #6a1b9a`,
            boxShadow: `0 6px 18px rgba(0, 0, 0, 0.2)`,
            color: "#212529",
            padding: "0",
            cursor: "auto",
            borderRadius: "8px",
            width: "450px",
            maxWidth: isMobile ? "90%" : "450px",
        },
        callback: () => clearAllInterval(),
    });
    
    toastInstance.showToast();
}

const activeNotifCall = {};
function notificationIncomingCall(callerNumber, uniqueid, destNumber) {
    if (!callerNumber || !uniqueid || !destNumber) return false;
    if (activeNotifCall[uniqueid]) return false;
    
    const callToast = Toastify({
        text: `
            <div style="display: flex; align-items: center; gap: 14px;">
                <img id="incomingCallIcon" class="notifCallImg" src="/Img/Calling.gif" style="width: 42px;" alt=""/>
                <img id="endedCallIcon" class="notifCallImg" src="/Img/Call_ended.gif" style="width: 42px;display: none" alt=""/>
                <img id="answeredCallIcon" class="notifCallImg" src="/Img/Calling_answergif.gif" style="width: 42px;display: none" alt=""/>
                
                <div class="d-flex align-items-start justify-content-between w-100">
                    <div class="d-flex flex-grow-1 notifCallText" style="gap:6px;flex-direction: column">
                        <div class="d-flex align-items-center justify-content-between">
                            <div class="fbold notifCallTitle" style="font-size: 16px">تماس ورودی</div>
                        </div>
                        
                        <div class="notifCallDetail d-flex align-items-center gap-1" style="font-size: 14px;">
                             <span>${callerNumber}</span>
                        </div>
                    </div>
                    <div class="notifCallTimer" style="font-size: 13px; color: #f1f1f1;">
                        <span id="callTimer"></span>
                    </div>
                </div>
                <div class="d-flex align-items-center" style="align-self: start;position: absolute;bottom: -2px;left: 4px;gap:2px">
                    <button class="minimize-btn" onclick="minimizeCallNotif('${uniqueid}')">
                          <svg style="width: 12px;height: 12px" viewBox="0 0 16 16" fill="currentColor"><path d="M3.75 7.25a.75.75 0 0 0 0 1.5h8.5a.75.75 0 0 0 0-1.5h-8.5Z" /></svg>
                    </button>
                    <button class="dismissNotif-btn" onclick="dismissCallNotif(this,'${uniqueid}')">
                          <svg style="width: 12px;height: 12px" viewBox="0 0 16 16" fill="currentColor"><path d="M5.28 4.22a.75.75 0 0 0-1.06 1.06L6.94 8l-2.72 2.72a.75.75 0 1 0 1.06 1.06L8 9.06l2.72 2.72a.75.75 0 1 0 1.06-1.06L9.06 8l2.72-2.72a.75.75 0 0 0-1.06-1.06L8 6.94 5.28 4.22Z" /></svg>
                    </button>
                </div>
            </div>
        `,
        duration: -1,
        gravity: "top",
        position: "center",
        escapeMarkup: false,
        stopOnFocus: false,
        close: true,
        style: {
            background: "#2f3e46",
            color: "#fff",
            borderRadius: "10px",
            padding: "14px 16px",
            boxShadow: "0 8px 24px rgba(0,0,0,0.3)",
            width: "310px",
            maxWidth: "90%",
            cursor: "auto",
        },
    });
    callToast.showToast();
    activeNotifCall[uniqueid] = callToast;
    
    return true;
}

function closeCallNotif(uniqueid) {
    const notif = activeNotifCall[uniqueid];
    if (!notif) return false;
    
    if (notif._interval) {
        clearInterval(notif._interval);
        delete notif._interval;
    }
    
    notif.toastElement.style.background = "darkred";
    notif.toastElement.querySelector("#incomingCallIcon").style.display = "none";
    notif.toastElement.querySelector("#answeredCallIcon").style.display = "none";
    notif.toastElement.querySelector("#endedCallIcon").style.display = "";
    
    setTimeout(() => {
        notif.hideToast();
        delete activeNotifCall[uniqueid];
    }, 2000);
    
    return true;
}

function answerCallNotif(uniqueid) {
    const notif = activeNotifCall[uniqueid];
    if (!notif) return false;
    
    if (!notif._interval) {
        notif.toastElement.style.background = "#1b4332";
        notif.toastElement.querySelector("#incomingCallIcon").style.display = "none";
        notif.toastElement.querySelector("#endedCallIcon").style.display = "none";
        notif.toastElement.querySelector("#answeredCallIcon").style.display = "";
        
        let seconds = 0;
        const timerSpan = notif.toastElement.querySelector(`#callTimer`);
        if (!timerSpan) return;
        
        timerSpan.textContent = "00:00";
        notif._interval = setInterval(() => {
            seconds++;
            const min = Math.floor(seconds / 60);
            const sec = seconds % 60;
            timerSpan.textContent = `${min.toString().padStart(2, "0")}:${sec.toString().padStart(2, "0")}`;
        }, 1000);
    }
    
    return true;
}

function minimizeCallNotif(uniqueid) {
    const notif = activeNotifCall[uniqueid];
    if (!notif) return;
    
    notif.toastElement.classList.toggle("minimized-bar");
}

function dismissCallNotif(btn, uniqueid) {
    /*  const notif = activeNotifCall[uniqueid];
      if (!notif) return;*/
    
    // notif.hideToast();
    // delete activeNotifCall[uniqueid];
    
    if (btn) {
        const closeBtn = btn.closest(".toastify").querySelector(".toast-close");
        closeBtn.click();
    }
}


function getRelativeTime(dateTimeStr) {
    const inputDate = new Date(dateTimeStr);
    const now = new Date();
    let diffMs = now - inputDate;
    
    const diffMinutes = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffMinutes < 1) {
        return "لحظاتی پیش";
    }
    
    if (diffHours < 24) {
        const hours = Math.floor(diffMinutes / 60);
        const minutes = diffMinutes % 60;
        if (hours > 0 && minutes > 0) {
            return `${hours} ساعت و ${minutes} دقیقه پیش`;
        } else if (hours > 0) {
            return `${hours} ساعت پیش`;
        } else {
            return `${minutes} دقیقه پیش`;
        }
    } else {
        const days = diffDays;
        const remainingHours = diffHours - days * 24;
        if (remainingHours > 0) {
            return `${days} روز و ${remainingHours} ساعت پیش`;
        } else {
            return `${days} روز پیش`;
        }
    }
}

function notificationChange(text, { link, isBlank = true }, category) {
    const notifStyles = {
        actionRequired: { bg: "#e3f2fd", color: "#1565c0", iconSrc: "activate.gif", title: "نیاز به تایید" },
        info: { bg: "#e8f5e9", color: "#2e7d32", iconSrc: "information.gif", title: "اطلاع رسانی" },
        task: { bg: "#f3e5f5", color: "#6a1b9a", iconSrc: "toDo.gif", title: "کاربان" },
        chat: { bg: "#d9eceb", color: "#008077", iconSrc: "notification.gif", title: "پیام رسان" },
        default: { bg: "#eceff1", color: "#37474f", iconSrc: "notification.gif", title: "اعلان سیستم" },
    };
    
    const { bg, color, iconSrc, title } = notifStyles[category] || notifStyles.default;
    
    const notifDiv = document.createElement("div");
    notifDiv.style.display = "flex";
    notifDiv.style.alignItems = "flex-start";
    notifDiv.style.gap = "6px";
    notifDiv.style.width = "100%";
    
    notifDiv.innerHTML = `
        <div style="width: clamp(32px, 4vw, 44px); color: ${color}; margin-top: -3px">
          <img src="/Img/${iconSrc}" style="width: 100%" alt="">
        </div>
        <div style="flex: 1; display: flex; flex-direction: column; gap: clamp(5px, 0.6vw, 6px);">
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <span class="fbold" style="font-size: clamp(12px, 1.2vw, 14px); color: ${color}">${title}</span>
               
                <div class="d-flex align-items-center gap-1" style="color: ${color};">
                  <span id="counterNotif" style="line-height: 1; font-size: 12px;"></span>
                  <span class="d-flex align-items-center justify-content-center" style="cursor:pointer;" onclick="dismissNotif(null, this)">
                     <svg style="width: 16px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                         <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
                     </svg>
                  </span>
                </div>
            </div>
            <div  style="font-size: clamp(11px, 1.1vw, 13px); color: #212529;">${text}</div>
            ${link ? `
                <div class="text-end">
                    <button onclick="goToLink('${link}', this, ${isBlank})" style="font-size: clamp(10px, 1vw, 12px); padding: clamp(3px, 0.5vw, 4px) clamp(6px, 1vw, 8px);background: ${color}; color: #fff; border: none; border-radius: 4px; cursor: pointer;">
                        مشاهده
                    </button>
                </div>
            ` : ""}
        </div>
    `;
    
    
    let i = 7;
    let paused = false;
    notifDiv.querySelector("#counterNotif").innerText = i;
    const counterNotifId = setInterval(() => {
        if (!paused) {
            i--;
            notifDiv.querySelector("#counterNotif").innerText = i;
            if (i <= 0) {
                clearInterval(counterNotifId);
                toastInstance.hideToast();
            }
        }
    }, 1000);
    
    notifDiv.addEventListener("mouseenter", () => paused = true);
    notifDiv.addEventListener("mouseleave", () => paused = false);
    
    const isMobile = window.innerWidth <= 768;
    
    const toastInstance = Toastify({
        node: notifDiv,
        duration: i * 1000,
        close: true,
        gravity: isMobile ? "top" : "bottom",
        position: isMobile ? "center" : "left",
        style: {
            background: bg,
            borderLeft: `5px solid ${color}`,
            boxShadow: `0 6px 18px rgba(0, 0, 0, 0.2)`,
            color: "#212529",
            padding: "clamp(8px, 1.2vw, 12px) clamp(10px, 1.5vw, 14px)",
            cursor: "auto",
            borderRadius: "8px",
            width: "450px",
            minWidth: "280px",
            maxWidth: isMobile ? "90%" : "450px",
        },
        callback: () => clearInterval(counterNotifId),
    });
    
    toastInstance.showToast();
    
    /*if (Notification.permission === "granted") {
        sendBrowserNotification(title, text, `/Img/${iconSrc}`, link);
    }*/
    
}


/////////////////////////////

function dateTimePicker(container = document) {
    container.querySelectorAll(".sdate").forEach((datePicker) => {
        
        if (!$(datePicker).data("myPersianDatepicker")) {
            const positionX = datePicker.getAttribute("data-x") || "28";
            const positionY = datePicker.getAttribute("data-y") || "0";
            const instance = $(datePicker).persianDatepicker({
                onSelect: function (dateText) {
                    const $input = $(this.model.inputElement);
                    $input.val(toEnglishDigits($input.val()));
                    
                    const onSelectCallback = datePicker.getAttribute("data-onSelect");
                    const onSelectId = datePicker.getAttribute("data-id");
                    
                    if ($input.val().length > 0) {
                        $input.removeClass("AlertReq");
                    }
                    
                    if (onSelectCallback && typeof window[onSelectCallback] === "function") {
                        if (onSelectId) {
                            window[onSelectCallback](this, onSelectId);
                        } else {
                            window[onSelectCallback](this);
                        }
                    }
                },
                initialValueType: "persian",
                position: [Number(positionX), Number(positionY)],
                initialValue: false,
                responsive: true,
                observer: false,
                format: "YYYY/MM/DD",
                autoClose: true,
                calendar: {
                    persian: {
                        locale: "fa",
                        showHint: false,
                        leapYearMode: "astronomical",
                    },
                    gregorian: {
                        locale: "en",
                        showHint: false,
                    },
                },
                toolbox: {
                    enabled: false,
                    calendarSwitch: {
                        enabled: false,
                    },
                },
            });
            
            $(datePicker).data("myPersianDatepicker", instance);
        }
    });
    
    container.querySelectorAll(".sdateTime").forEach((dateTimePickerElem) => {
        if (!$(dateTimePickerElem).data("myPersianDatepicker")) {
            const instance = $(dateTimePickerElem).persianDatepicker({
                onSelect: function (dateText) {
                    if ($(this.model.inputElement).val().length > 0) {
                        $(this.model.inputElement).removeClass("AlertReq");
                    }
                },
                initialValueType: "persian",
                initialValue: false,
                responsive: true,
                observer: false,
                format: "YYYY/MM/DD, HH:mm",
                autoClose: true,
                calendar: {
                    persian: {
                        locale: "fa",
                        showHint: false,
                        leapYearMode: "algorithmic",
                    },
                    gregorian: {
                        locale: "en",
                        showHint: false,
                    },
                },
                toolbox: {
                    enabled: false,
                    calendarSwitch: {
                        enabled: false,
                    },
                },
                timePicker: {
                    enabled: true,
                    minute: {
                        step: 5,
                    },
                    second: {
                        enabled: false,
                    },
                    meridiem: {
                        enabled: true,
                    },
                },
            });
            $(dateTimePickerElem).data("myPersianDatepicker", instance);
        }
    });
    
    container.querySelectorAll(".stime").forEach((timePickerElem) => {
        if (!$(timePickerElem).data("myPersianDatepicker")) {
            const positionX = timePickerElem.getAttribute("data-x") || "28";
            const positionY = timePickerElem.getAttribute("data-y") || "0";
            const isTaskTime = timePickerElem.hasAttribute("data-task-time");
            let timeConfirmed = false;
            const confirmTaskTime = () => {
                if (!isTaskTime) return;
                const value = toEnglishDigits(timePickerElem.value).trim();
                if (!/^(?:[01]\d|2[0-3]):[0-5]\d$/.test(value)) return;
                timeConfirmed = true;
                const callback = timePickerElem.getAttribute("data-on-confirm");
                if (typeof window[callback] === "function") window[callback](timePickerElem);
            };
            const instance = $(timePickerElem).persianDatepicker({
                onShow: function () {
                    if (!isTaskTime) return;
                    timeConfirmed = false;
                    timePickerElem.dataset.taskTimePickerOpen = "true";
                    const value = toEnglishDigits(timePickerElem.value).trim();
                    if (/^(?:[01]\d|2[0-3]):[0-5]\d$/.test(value)) {
                        const [hours, minutes] = value.split(":").map(Number);
                        instance.setDate(new Date().setHours(hours, minutes, 0, 0));
                        timePickerElem.value = value;
                    }
                },
                onHide: function () {
                    if (!isTaskTime || timePickerElem.dataset.taskTimePickerOpen !== "true") return;
                    delete timePickerElem.dataset.taskTimePickerOpen;
                    if (!timeConfirmed) timePickerElem.value = timePickerElem.dataset.originalText ?? "";
                },
                ...(isTaskTime ? {
                    toolbox: {
                        enabled: true,
                        todayButton: { enabled: false },
                        calendarSwitch: { enabled: false },
                        submitButton: { enabled: true, text: { fa: "تأیید", en: "Confirm" }, onSubmit: confirmTaskTime },
                    },
                } : {}),
                onSelect: function () {
                    if (isTaskTime) timePickerElem.value = toEnglishDigits(timePickerElem.value);
                    const onSelectCallback = timePickerElem.getAttribute("data-onSelect");
                    if (onSelectCallback) {
                        window[onSelectCallback](this);
                    }
                },
                onlyTimePicker: true,
                initialValue: !isTaskTime,
                initialValueType: "persian",
                autoClose: !isTaskTime,
                observer: !isTaskTime,
                position: [Number(positionX), Number(positionY)],
                format: "HH:mm",
                timePicker: {
                    enabled: true,
                    hour: { enabled: true },
                    minute: { enabled: true },
                    second: { enabled: false },
                    meridian: { enabled: false },
                },
            });
            $(timePickerElem).data("myPersianDatepicker", instance);
            if (isTaskTime) timePickerElem.addEventListener("keydown", event => {
                if (event.key !== "Enter" || event.isComposing) return;
                event.preventDefault();
                confirmTaskTime();
                instance.hide();
                timePickerElem.blur();
            });
        }
    });
}


/////////////////////////////

// بهینه شده

/*const Mask = () => {
    for (const el of document.querySelectorAll("[placeholder][data-slots]")) {

        el.addEventListener('focusout', () => {
            el.value = el.value.replace(/_/g, '').replace(/y/g, '').replace(/m/g, '').replace(/d/g, '');
            el.value = el.value.replace(/\/?mm\/?dd/g, '').replace(/\/?mm/g, '').replace(/\/?dd/g, '');
        });

        const pattern = el.getAttribute("placeholder"),
            slots = new Set(el.dataset.slots || "_"),
            prev = (j => Array.from(pattern, (c, i) => slots.has(c) ? j = i + 1 : j))(0),
            first = [...pattern].findIndex(c => slots.has(c)),
            accept = new RegExp(el.dataset.accept || "\\d", "g"),
            clean = input => {
                input = input.match(accept) || [];
                return Array.from(pattern, c =>
                    input[0] === c || slots.has(c) ? input.shift() || c : c
                );
            },
            format = () => {
                const [i, j] = [el.selectionStart, el.selectionEnd].map(i => {
                    i = clean(el.value.slice(0, i)).findIndex(c => slots.has(c));
                    return i < 0 ? prev[prev.length - 1] : back ? prev[i - 1] || first : i;
                });
                el.value = clean(el.value).join``;
                el.setSelectionRange(i, j);
                back = false;
            };

        let back = false;

        el.addEventListener("keydown", (e) => {
            back = e.key === "Backspace";
        });

        el.addEventListener("keyup", () => {
            back = false;
        });

        el.addEventListener("input", format);
        // el.addEventListener("focus", format);

        el.addEventListener("blur", () => {
            if (el.value.replace(/_/g, '') === '') {
                el.value = "";
            }
        });
    }
};*/
const Mask = () => {
    for (const el of document.querySelectorAll("[placeholder][data-slots]")) {
        el.addEventListener("focusout", () => {
            
            if (el.value.includes("_")) {
                el.value = el.value.replaceAll("_", "");
            }
            
            
            if (el.value.includes("y")) {
                el.value = el.value.replaceAll("y", "");
            }
            
            if (el.value.includes("mm") && el.value.includes("dd")) {
                el.value = el.value.replace("/mm/dd", "");
            }
            if (el.value.includes("mm") || el.value.includes("m")) {
                el.value = el.value.replace("/mm", "");
                el.value = el.value.replace("m", "");
            }
            if (el.value.includes("dd") || el.value.includes("d")) {
                el.value = el.value.replace("/dd", "");
                el.value = el.value.replace("d", "");
            }
            
            
        });
        
        const pattern = el.getAttribute("placeholder"),
            slots = new Set(el.dataset.slots || "_"),
            prev = (j => Array.from(pattern, (c, i) => slots.has(c) ? j = i + 1 : j))(0),
            first = [...pattern].findIndex(c => slots.has(c)),
            accept = new RegExp(el.dataset.accept || "\\d", "g"),
            clean = input => {
                
                input = input.match(accept) || [];
                
                
                return Array.from(pattern, c =>
                    input[0] === c || slots.has(c) ? input.shift() || c : c,
                );
            },
            format = () => {
                
                const [i, j] = [el.selectionStart, el.selectionEnd].map(i => {
                    
                    i = clean(el.value.slice(0, i)).findIndex(c => slots.has(c));
                    return i < 0 ? prev[prev.length - 1] : back ? prev[i - 1] || first : i;
                });
                el.value = clean(el.value).join``;
                el.setSelectionRange(i, j);
                back = false;
            };
        let back = false;
        
        el.addEventListener("keydown", (e) => {
            back = e.key === "Backspace";
        });
        el.addEventListener("input", format);
        //el.addEventListener("focus", format);
        el.addEventListener("blur", () => {
            el.value === pattern && (el.value = "");
        });
    }
};


/////////////////////////////

const getAllProduct = async (searchValue = "", from) => {
    return await sendApiRequest({
        url: "/Amount/ListProduct",
        skeleton: from === "amountIndex" ? { target: "#ListProductId", retry: () => getAndSetAllProduct(from, searchValue) } : null,
        params: { searchValue, from },
    });
};

const getAndSetAllProduct = async (from, searchValue = "") => {
    const AllProduct = await getAllProduct(searchValue, from);
    if (AllProduct == null) return;
    const containerId = from === "amountIndex" ? "ListProductId" : "ListAmountBodyId";
    document.getElementById(containerId).innerHTML = AllProduct;
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
    
    enableMomentumScrollScope(document.getElementById("FilterModal"));
    const selLabelElm = document.getElementById("SelFilterLabel");
    const selChkElm = document.getElementById("SelFilterChd");
    const cusChkElm = document.getElementById("CusFilterChd");
    const dateChkElm = document.getElementById("DateFilterChk");
    
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
    ["cusFilterItem", "selFilterItem", "dateFilterItem"].forEach(id => {
        getFilterElement(id)?.style.setProperty("display", "flex");
    });
    filterItemBlackList.forEach(id => {
        const elm = document.getElementById(id);
        if (elm) {
            elm.style.setProperty("display", "none", "important");
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
        const customerFilter = document.getElementById("customerFilter");
        
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
        const selFilter = document.getElementById("selFilter");
        
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
        // The shared modal survives page navigation; only the dashboard requires an active date filter.
        dateChkElm.disabled = filterVarConfig === "filterDashboardConfig";
        const toDate = document.querySelector(`#FilterModal [name="toDate"]`);
        const fromDate = document.querySelector(`#FilterModal [name="fromDate"]`);
        
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
    
    const cusChk = document.getElementById("CusFilterChd");
    const selChk = document.getElementById("SelFilterChd");
    const dateChk = document.getElementById("DateFilterChk");
    
    data.CusFilterChd = cusChk?.checked ?? false;
    data.SelFilterChd = selChk?.checked ?? false;
    data.DateFilterChk = dateChk?.checked ?? false;
    
    const customer = document.getElementById("customerFilter");
    const seller = document.getElementById("selFilter");
    
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
    document.querySelectorAll("#dateFilterBtnId button").forEach(el => el.classList.remove("bgGreen2"));
};

document.getElementById("DateFilterDivContent")?.addEventListener("input", window.onSelectFilterDate);
document.getElementById("DateFilterDivContent")?.addEventListener("change", window.onSelectFilterDate);


const AcceptSetting = async () => {
    if (setting == null) {
        setting = await GetSetting();
    }
    
    if (setting.smallOffice) {
        //document.getElementById("divReciveAmountId").style.display = "flex";
        document.getElementById("PfPrintableAlertId").innerHTML = "";
    }
    
    if (setting.setAmountByRoleType === 2) {
        document.querySelectorAll(".unitAmountBoxPrefactorModal")?.forEach(elm => elm.classList.add("d-none"));
        document.querySelectorAll(".totalAmountBoxPrefactorModal")?.forEach(elm => elm.classList.add("d-none"));
        document.querySelectorAll(".discountBoxPrefactorModal")?.forEach(elm => elm.classList.add("d-none"));
        document.querySelectorAll(".unitAmountBoxPrefactorModal input[name='unitAmount']")?.forEach(elm => elm.removeAttribute("required"));
        const form = document.getElementById("PreFacItem");
        if (form) form.classList.add("hide-amount-columns");
    }
};


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
    
    let dt = await getTodayDateTime();
    let time = dt.split(" ");
    el.innerHTML = `${dt}`;
    
    setInterval(async () => {
        dt = await getTodayDateTime();
        time = dt.split(" ");
        el.innerHTML = `${dt}`;
    }, 30000);
};


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


const openCustomerProfile = (id, pfSerialNum) => {
    if (!id) return;
    
    sessionStorage.setItem("targetPf", pfSerialNum);
    
    window.open(`/Customer/CustomerProfile?Id=${encodeURIComponent(id)}`, "_blank");
};


const ShowPreviewPfactor = (id) => {
    window.open("/factor/PreviewPfactorPrint/" + id, "_blank", "noopener,noreferrer");
};
const ShowPreviewFactor = (id) => {
    window.open("/factor/PreviewFactorPrint/" + id, "_blank", "noopener,noreferrer");
};
const ShowPreviewFactorSepidar = (id) => {
    window.open("/factor/PreviewFactorSepidarPrint/" + id, "_blank", "noopener,noreferrer");
};


/*document.querySelectorAll(".chip").forEach(chip => {
    chip.addEventListener("click", () => {
        //const group = chip.dataset.group;
        const value = chip.dataset.value;
        
        /!*document.querySelectorAll(`.chip[data-group='${group}']`)
                .forEach(c => c.classList.remove("active"));*!/
        
        chip.classList.toggle("active");
    });
});*/

const changeJur = (val) => {
    const isJuridical = val !== 0;
    
    document.getElementById("UpJuridicalA").checked = !isJuridical;
    document.getElementById("UpJuridicalB").checked = isJuridical;
    document.getElementById("JuridicalUpDiv").style.display = isJuridical ? "flex" : "none";
    
    document.querySelectorAll(".Uhog").forEach(item => item.required = isJuridical);
};



const cancelUpCustomer = () => {
    mySer.emptyForm("CuInfoUpFormId");
    changeJur(0);
};


let cropper;
let personImageFile = null;
let delImgPerStatus = 0;
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
    sendApiRequest({
        url: `/Cartable/GetCountEnvelope`,
        showLoading: false,
    }).then((data) => {
        setCountEnvelope(data.countObj);
        setCountEmail(data.emailsCount);
        setCountChat(data.chatsCount);
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
            bankAccountsCache[key] = sendApiRequest({
                url: `/Accountancy/GetAllBankAccountJson`,
                params: { type },
                showLoading: false,
            });
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


// The bank-account modal is hosted once by _Layout and can be opened by any page.
// Page-specific refreshes are supplied through onSaved, not coupled to requestAmount.js.
const bankAccountModalState = {
    original: null,
    onSaved: null,
    personRoleType: null,
    saving: false,
};

function getBankAccountModal() {
    return document.getElementById("addBankAccountModal");
}

function selectedBankAccountPersonRole() {
    const typeSelect = getBankAccountModal()?.querySelector("#bankAccountTypeModalId");
    return Number(typeSelect?.selectedOptions[0]?.dataset.personRole || 0);
}

function updateBankAccountPersonField() {
    const modal = getBankAccountModal();
    if (!modal) return;
    const person = modal.querySelector("#bankAccountCustomerId");
    const wrapper = modal.querySelector("#bankAccountPersonWrapper");
    const roleType = selectedBankAccountPersonRole();
    const isPersonAccount = roleType > 0;

    wrapper.classList.toggle("d-none", !isPersonAccount);
    person.required = isPersonAccount;
    if (roleType !== bankAccountModalState.personRoleType) {
        // Recreate the search control when switching supplier/customer so stale
        // TomSelect search results can never be shown for the wrong role.
        person.tomselect?.destroy();
        person.replaceChildren();
        person.value = "";
        bankAccountModalState.personRoleType = roleType;
        if (isPersonAccount) {
            person.disabled = false;
            new TomSelect(person, {
                valueField: "value",
                labelField: "text",
                searchField: ["text"],
                placeholder: roleType === 2 ? "جستجوی تأمین‌کننده" : "جستجوی مشتری",
                maxItems: 1,
                create: false,
                loadThrottle: 300,
                shouldLoad: query => query.trim().length > 0,
                dropdownParent: modal,
                load(query, callback) {
                    const currentRole = selectedBankAccountPersonRole();
                    sendApiRequest({
                        url: "/Customer/GetCustomerOp",
                        params: { search: query.trim(), roleType: currentRole },
                        showLoading: false,
                        showError: false,
                    }).then(items => callback(
                        Array.isArray(items)
                            ? items.map(person => ({ value: person.id, text: person.text }))
                            : []
                    )).catch(() => callback());
                },
            });
        } else {
            person.disabled = true;
        }
    }
    modal.querySelector("label[for='bankAccountCustomerId']").textContent =
        roleType === 2 ? "تأمین‌کننده:" : "مشتری:";
}

function initializeBankAccountModal() {
    const modal = getBankAccountModal();
    if (!modal || modal.dataset.bankAccountInitialized === "true") return modal;
    modal.dataset.bankAccountInitialized = "true";
    modal.querySelector("#bankAccountTypeModalId").addEventListener("change", updateBankAccountPersonField);
    modal.querySelector("#addBankAccountForm").addEventListener("submit", event => {
        event.preventDefault();
        void submitAddBankAccount();
    });
    modal.addEventListener("hidden.bs.modal", resetBankAccountModal);
    return modal;
}

function resetBankAccountModal() {
    const modal = getBankAccountModal();
    if (!modal) return;
    bankAccountModalState.original = null;
    bankAccountModalState.onSaved = null;
    bankAccountModalState.saving = false;
    mySer.emptyForm("addBankAccountForm");
    modal.querySelector("#bankAccountTypeModalId").value = "";
    updateBankAccountPersonField();
    const submitButton = modal.querySelector("#bankAccountModalSubmitBtn");
    submitButton.disabled = false;
    submitButton.textContent = "ثبت";
    modal.querySelector("#bankAccountModalTitle").textContent = "افزودن حساب بانکی";
}

/**
 * Shared create/edit bank-account modal.
 * @param {{id?: string, bankAccountType?: number, onSaved?: (account: object) => (void | Promise<void>)}} options
 */
async function toggleAddBankAccountModal(options = {}) {
    const modal = initializeBankAccountModal();
    if (!modal) return;
    if (isModalOpen(modal)) {
        ModalToggle(modal, "hide");
        return;
    }
    const { id = null, bankAccountType = null, onSaved = null } = options || {};
    const original = id ? await sendApiRequest({
        url: "/Accountancy/GetBankAccount",
        params: { id },
    }) : null;
    if (id && !original) return;

    resetBankAccountModal();
    bankAccountModalState.original = original;
    bankAccountModalState.onSaved = typeof onSaved === "function" ? onSaved : null;

    if (original) {
        mySer.convertJsonToForm(original, "addBankAccountForm", ["customerId", "shebaNumber"]);
        const sheba = modal.querySelector("#shebaNumberId");
        sheba.value = (original.shebaNumber || "").replace(/^IR[\s-]*/i, "");
        modal.querySelector("#bankAccountModalTitle").textContent = "ویرایش حساب بانکی";
        modal.querySelector("#bankAccountModalSubmitBtn").textContent = "ذخیره تغییرات";

        if (original.customerId && selectedBankAccountPersonRole() > 0) {
            const customer = await sendApiRequest({
                url: "/Customer/GetCustomer",
                params: { id: original.customerId },
            });
            const selector = modal.querySelector("#bankAccountCustomerId").tomselect;
            if (customer && selector) {
                const name = [customer.companyName, customer.agentName].filter(Boolean).join(" ").trim();
                selector.addOption({
                    value: customer.id,
                    text: `${name || "شخص"} (کد: ${customer.customerCode ?? "-"})`,
                });
                selector.setValue(customer.id, true);
            }
        }
    } else if (bankAccountType != null) {
        const typeSelect = modal.querySelector("#bankAccountTypeModalId");
        typeSelect.value = String(bankAccountType);
        typeSelect.dispatchEvent(new Event("change"));
    }
    ModalToggle(modal, "show");
}

async function submitAddBankAccount() {
    const modal = getBankAccountModal();
    if (!modal || bankAccountModalState.saving) return;
    if (!await mySer.CheckValidForm("addBankAccountForm")) return;

    const cartNumber = modal.querySelector("#cartNumberId").value.trim();
    const accountNumber = modal.querySelector("#accountNumberId").value.trim();
    const shebaNumber = modal.querySelector("#shebaNumberId").value.trim().replace(/^IR[\s-]*/i, "");
    if (!cartNumber && !accountNumber && !shebaNumber) {
        modal.querySelector("#cartNumberId").focus();
        toastifyError("حداقل یکی از موارد شماره کارت، شماره حساب یا شماره شبا باید وارد شود.");
        return;
    }

    const personRole = selectedBankAccountPersonRole();
    const personSelect = modal.querySelector("#bankAccountCustomerId");
    const customerId = personRole ? (personSelect.tomselect?.getValue() || personSelect.value) : null;
    if (personRole && !customerId) {
        personSelect.tomselect?.focus();
        return;
    }

    const model = {
        ...(bankAccountModalState.original || {}),
        ...mySer.convertFormToJSON("addBankAccountForm"),
        customerId: customerId || null,
        shebaNumber: shebaNumber ? `IR ${shebaNumber}` : "",
    };
    const isEdit = !!bankAccountModalState.original;
    const submitButton = modal.querySelector("#bankAccountModalSubmitBtn");
    bankAccountModalState.saving = true;
    submitButton.disabled = true;
    try {
        const response = await sendApiRequest({
            url: isEdit ? "/Accountancy/UpBankAccount" : "/Accountancy/AddBankAccount",
            method: "POST",
            data: model,
            returnFullResponse: true,
            successMsg: isEdit ? "با موفقیت ویرایش شد" : "اطلاعات حساب با موفقیت ثبت شد",
        });
        if (!response || response.status !== 200) return;

        const onSaved = bankAccountModalState.onSaved;
        ModalToggle(modal, "hide");
        if (onSaved) {
            try {
                await onSaved(model);
            } catch (error) {
                console.error("Unable to refresh bank account options:", error);
            }
        }
    } finally {
        bankAccountModalState.saving = false;
        submitButton.disabled = false;
    }
}


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
            await sendApiRequest({
                url: `/order/GetOrderHead`,
                params: { pfId: toggle.dataset.pfid },
                showLoading: false,
                onSuccess: (res) => content.innerHTML = res.data,
            });
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

const taskCardProgress = new WeakMap();

function beginTaskCardUpdate(id) {
    const cards = [...document.querySelectorAll("[data-task-id]")].filter(card => card.dataset.taskId === String(id) || card.dataset.taskGroup === String(id));
    const active = cards.map(card => {
        let state = taskCardProgress.get(card);
        if (!state) {
            const feedback = document.createElement("div");
            feedback.className = "task-card-progress";
            feedback.setAttribute("role", "status");
            const spinner = document.createElement("span");
            spinner.className = "task-card-progress__spinner";
            spinner.setAttribute("aria-hidden", "true");
            const label = document.createElement("span");
            label.textContent = "در حال به‌روزرسانی…";
            feedback.append(spinner, label);
            state = { count: 0, feedback, busy: card.getAttribute("aria-busy") };
            taskCardProgress.set(card, state);
            card.classList.add("task-card-updating");
            card.setAttribute("aria-busy", "true");
            card.append(feedback);
        }
        state.count++;
        return { card, state };
    });
    let released = false;
    return () => {
        if (released) return;
        released = true;
        active.forEach(({ card, state }) => {
            if (--state.count > 0) return;
            state.feedback.remove();
            card.classList.remove("task-card-updating");
            if (state.busy === null) card.removeAttribute("aria-busy");
            else card.setAttribute("aria-busy", state.busy);
            taskCardProgress.delete(card);
        });
    };
}

const carizoListReads = new Map();
const carizoCardReads = new Map();
const carizoAppliedGroups = new Map();
let carizoCardVersion = 0;

function disposeTaskCardPickers(container) {
    container.querySelectorAll(".sdate,.sdateTime,.stime").forEach(input => {
        $(input).data("myPersianDatepicker")?.destroy();
        $(input).removeData("myPersianDatepicker");
    });
}

function taskCardComesBefore(left, right) {
    const date = card => card.dataset.taskDate ? Date.parse(card.dataset.taskDate) : -Infinity;
    const difference = date(left) - date(right);
    return difference > 0 || (date(left) === date(right) && Date.parse(left.dataset.taskCreated) > Date.parse(right.dataset.taskCreated));
}

async function refreshTaskCards(id) {
    if (!id) return;
    carizoCardReads.get(id)?.abort();
    const request = new AbortController();
    const version = ++carizoCardVersion;
    carizoCardReads.set(id, request);
    const finishProgress = beginTaskCardUpdate(id);
    try {
        await Promise.allSettled([...carizoListReads.values()]);
        if (request.signal.aborted) return;
        const systematic = typeof systematicTask === "undefined" ? 0 : systematicTask;
        const targets = [
            { name: "todoTask", column: 0, loc: 1 },
            { name: "todayTask", column: 1, loc: 1 },
            { name: "finishedTask", column: 2, loc: 1 },
            { name: "todayTaskDash", column: 1, loc: 2 },
        ].filter(target => document.getElementById(target.name + "Id"));
        const locations = [...new Set(targets.map(target => target.loc))];
        const updates = await Promise.all(locations.map(loc => sendApiRequest({
            url: "/Carizo/GetTaskCards", params: { id, systematic, loc },
            signal: request.signal, showLoading: false,
        })));
        await Promise.allSettled([...carizoListReads.values()]);
        if (carizoCardReads.get(id) !== request || request.signal.aborted || systematic !== (typeof systematicTask === "undefined" ? 0 : systematicTask)) return;
        locations.forEach((loc, index) => {
            const update = updates[index];
            if (!update) return;
            const groupKey = `${loc}:${update.groupId}`;
            if ((carizoAppliedGroups.get(groupKey) ?? 0) > version) return;
            carizoAppliedGroups.set(groupKey, version);
            targets.filter(target => target.loc === loc).forEach(target => {
                const container = document.getElementById(target.name + "Id");
                const column = Number(container.dataset.taskColumn ?? target.column);
                [...container.children].filter(card => card.dataset.taskId === update.id || card.dataset.taskGroup === update.groupId)
                                       .forEach(card => {
                                           disposeTaskCardPickers(card);
                                           card.remove();
                                       });
                update.cards.filter(card => card.column === column).forEach(card => {
                    const template = document.createElement("template");
                    template.innerHTML = card.html.trim();
                    const node = template.content.firstElementChild;
                    if (!node) return;
                    const next = [...container.children].find(existing => existing.dataset.taskId && taskCardComesBefore(node, existing));
                    container.insertBefore(node, next ?? null);
                    dateTimePicker(node);
                });
                const count = container.querySelectorAll(":scope > [data-task-id]").length;
                const counter = document.getElementById(target.name + "Count");
                if (counter) counter.textContent = count;
                if (loc === 2) {
                    const empty = document.getElementById("noTaskImg");
                    if (empty) empty.style.display = count ? "none" : "flex";
                }
            });
        });
    } catch (error) {
        if (!request.signal.aborted) {
            console.error("Task card refresh failed:", error);
            toastifyError("به‌روزرسانی فعالیت انجام نشد؛ دوباره تلاش کنید.");
        }
    } finally {
        if (carizoCardReads.get(id) === request) carizoCardReads.delete(id);
        finishProgress();
    }
}

window.addEventListener("pagehide", () => carizoCardReads.forEach(request => request.abort()));

async function getAllTask(type, systematic = 0, loc = 1) {
    const containerName = type === 0 ? "todoTask" : type === 1 ? (loc === 1 ? "todayTask" : "todayTaskDash") : "finishedTask";
    const containerElm = document.getElementById(containerName + "Id");
    const countElm = document.getElementById(containerName + "Count");
    const noTaskImgDash = document.getElementById("noTaskImg");
    const load = async () => {
        try {
            const res = await sendApiRequest({
                url: "/Carizo/GetAllTask",
                params: { type, loc, systematic },
                returnFullResponse: true,
                skeleton: loc === 1 && containerElm ? { target: containerElm, retry: () => getAllTask(type, systematic, loc) } : null,
                showLoading: false,
            });
            if (!res) {
                if (loc === 2) throw new Error("Task read failed");
                return;
            }
            if (res.status === 200) {
                if (containerElm) {
                    disposeTaskCardPickers(containerElm);
                    containerElm.innerHTML = res.data.html;
                    containerElm.dataset.taskColumn = String(type);
                    dateTimePicker(containerElm);
                }
                countElm && (countElm.innerHTML = res.data.count);
                noTaskImgDash && loc === 2 && (noTaskImgDash.style.display = res.data.count ? "none" : "flex");
            } else {
                throw new Error();
            }
        } catch (err) {
            console.error(err);
            containerElm && (containerElm.innerHTML = "خطا در بارگذاری فعالیت ها :(!");
            if (loc === 2 && typeof dashboardLoading !== "undefined") throw err;
        }
    };
    const read = loc === 2 && typeof dashboardLoading !== "undefined" ? dashboardLoading.run("todayTaskDashId", load) : load();
    carizoListReads.set(containerName, read);
    try {
        return await read;
    } finally {
        if (carizoListReads.get(containerName) === read) carizoListReads.delete(containerName);
    }
}

async function finishTask(id, type, thisBtn, loc = 1) {
    if (thisBtn.disabled) return;
    thisBtn.disabled = true;
    const finishProgress = beginTaskCardUpdate(id);
    const spinner = thisBtn.querySelector(".spinner-border");
    if (spinner) spinner.style.display = "inline-block";
    try {
        const res = await axios(`/Carizo/finishTask/${id}`);
        if (res.status !== 200) throw new Error("Task completion failed.");
        if (res.data?.status === 201) {
            toastifyError(res.data.error);
            return;
        }
        toastifySuccess("تسک انجام شد!");
        if (connection.state !== "Connected") await refreshTaskCards(id);
        if (Number(type) === 1) await getRemind();
    } catch (error) {
        console.error(error);
        toastifyError("خطای سرور!");
    } finally {
        finishProgress();
        thisBtn.disabled = false;
        if (spinner) spinner.style.display = "none";
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
let knownMenuWarningIds = null;
function updateWarningMenu(warnings) {
    const ids = new Set(warnings.map(warning => warning.id));
    const hasNewWarning = knownMenuWarningIds !== null && warnings.some(warning => !knownMenuWarningIds.has(warning.id));
    document.querySelectorAll(".menu-item.warning").forEach(button => {
        const badge = button.querySelector(".badge-count");
        const previousCount = Number(badge?.textContent) || 0;
        button.classList.toggle("d-none", warnings.length === 0);
        if (badge) badge.textContent = warnings.length || "";
        if (hasNewWarning || (knownMenuWarningIds === null && warnings.length > previousCount)) {
            button.classList.remove("menu-warning-new");
            void button.offsetWidth;
            button.classList.add("menu-warning-new");
        } else if (!warnings.length) {
            button.classList.remove("menu-warning-new");
        }
    });
    knownMenuWarningIds = ids;
}
async function getAllWarnings(isRefresh = false) {
    const warningElmBtns = document.querySelectorAll(".menu-item.warning");
    if (!warningElmBtns.length) return;
    
    if (!warningContainer) return;
    warningContainer.innerHTML = "";
    
    await sendApiRequest({
        url: "/Home/GetAllSystemWarnings",
        params: { isRefresh },
        showLoading: isRefresh,
        onSuccess: (res) => {
            warningData = res.data;
            
            updateWarningMenu(warningData);
            
            initDomWarningsModal();
        },
    });
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
                    <button class="flex-center showDetails">
                        <svg style="width: 18px;color: #ed2e2a;" viewBox="0 0 24 24" fill="currentColor"><path d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z" /><path fill-rule="evenodd" d="M1.323 11.447C2.811 6.976 7.028 3.75 12.001 3.75c4.97 0 9.185 3.223 10.675 7.69.12.362.12.752 0 1.113-1.487 4.471-5.705 7.697-10.677 7.697-4.97 0-9.186-3.223-10.675-7.69a1.762 1.762 0 0 1 0-1.113ZM17.25 12a5.25 5.25 0 1 1-10.5 0 5.25 5.25 0 0 1 10.5 0Z" clip-rule="evenodd" /></svg>
                    </button>
                    <button class="flex-center deleteWarning">
                        <svg style="width: 18px;color: #EF5350;" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" /></svg>
                    </button>
                </div>
            `;
            div.querySelector(".deleteWarning").addEventListener("click", () => removeWarning(item.id));
            div.querySelector(".showDetails").addEventListener("click", () => showDetailsWarning(item.entityId, item.entityType));
            
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
    await sendApiRequest({
        url: "/Home/DeleteSystemWarning",
        params: { id },
        successMsg: "با موفقیت حذف شد!",
        onSuccess: getAllWarnings,
    });
}
async function showDetailsWarning(id, type) {
    if (type === 3) { // factor
        const data = await getFactor(id);
        if (!data) return;
        
        const object = {
            factorSerialNumber: data.serialNumber,
            pFactorSerialNumber: data.pfactorSerialNumber,
            pFactorId: data.pfactorID,
        };
        
        if (!window.location.href.toLowerCase().includes("cartable/index?tab=tprefactorfactorid")) {
            FLshowLoading();
            localStorage.setItem("warningDetails", JSON.stringify(object));
            goTo("/Cartable/Index");
        } else {
            ModalToggle("warningModal", "hide");
            await showPrefac(object.pFactorSerialNumber.toString());
            await showFac(object.factorSerialNumber.toString());
        }
    }
}

async function loadPhonebook(search = "") {
    const data = await sendApiRequest({
        url: "/Customer/GetPhonebook",
        params: { search },
    });
    
    if (!data) return;
    
    const phonebookList = document.getElementById("phonebookList");
    phonebookList.innerHTML = "";
    if (data.length > 0) {
        data.forEach(item => {
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
}


async function showModalPhonebook() {
    enableMomentumScrollScope(document.getElementById("phoneBookModal"));
    ModalToggle("phoneBookModal");
    
    await loadPhonebook();
}

document.getElementById("PhoneBookSearchInp")?.addEventListener("keyup", async (e) => {
    if (e.key === "Enter") {
        await loadPhonebook(e.target.value);
    }
});

const showAttachments = async (id, typeAtt) => {
    const showAttModal = document.getElementById("showAttachmentsModal");
    const containerPreview = showAttModal.querySelector(".image-display");
    if (!containerPreview) return;
    containerPreview.innerHTML = "";
    
    await sendApiRequest({
        url: "/Factor/GetAttachment",
        params: { id, type: typeAtt },
        onSuccess: (res) => {
            const files = res.data;
            
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
        },
        onError: () => {
            containerPreview.innerHTML = "خطای دریافت اطلاعات!";
        },
        showError: false
    });
    
    
    ModalToggle(showAttModal);
};


/*--------------------------------------------------------------------------------*/


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
    enableMomentumScrollScope(document.getElementById("BaseSetting"));
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
    return await sendApiRequest({
        url: "/Home/GetEntityLogSystem",
        params: { entityName, entityId },
    });
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


/*--------------------------------------------------------------------*/


class DynamicSkeleton {
    /**
     * @param {Object} options
     * @param {string|HTMLElement} options.container - سلکتور یا المنت والد که اسکلتون ها در آن قرار میگیرند
     * @param {string} options.template - ساختار HTML یک آیتم اسکلتون
     * @param {number} options.count - تعداد اسکلتون‌هایی که باید ساخته شود (پیش‌فرض 3)
     */
    constructor(options) {
        this.container = typeof options.container === "string"
            ? document.querySelector(options.container)
            : options.container;
        this.template = options.template;
        this.count = options.count || 3;
    }
    
    // متد نمایش لودینگ
    render() {
        if (!this.container) return;
        
        let skeletonsHTML = "";
        for (let i = 0; i < this.count; i++) {
            skeletonsHTML += this.template;
        }
        
        this.container.innerHTML = skeletonsHTML;
    }
    
    // متد پاک کردن لودینگ (البته وقتی دیتای واقعی رو innerHTML کنید خود به خود پاک میشه)
    clear() {
        if (this.container) {
            this.container.innerHTML = "";
        }
    }
}

const Skeleton = (() => {
    
    const templates = {};
    
    function register(name, builder) {
        templates[name] = builder;
    }
    
    function show({
                      container,
                      count = 1,
                      template,
                  }) {
        
        const el = typeof container === "string"
            ? document.querySelector(container)
            : container;
        
        if (!el) return;
        
        el.dataset.skeleton = "true";
        
        const html = [];
        
        const builder =
            typeof template === "function"
                ? template
                : templates[template];
        
        for (let i = 0; i < count; i++) {
            
            html.push(builder(i));
            
        }
        
        el.innerHTML = html.join("");
        
    }
    
    function hide(container) {
        
        const el = typeof container === "string"
            ? document.querySelector(container)
            : container;
        
        if (el)
            el.innerHTML = "";
        
    }
    
    return {
        show,
        hide,
        register,
    };
    
})();


/*--------------------------------------------------------------------*/


const externalSoftwareModalElm = document.getElementById("chooseExternalSoftwareModal");
const externalSoftwareList = externalSoftwareModalElm?.querySelector("#externalSoftwareList");
const sendToSoftwareBtn = externalSoftwareModalElm?.querySelector("#sendToSoftwareBtn");

let externalSoftwareConfig = null;

async function showExternalSoftwareModal(ids, config) {
    const entityIds = [...new Set((Array.isArray(ids) ? ids : [ids]))];
    
    if (!entityIds.length || entityIds.some(id => !id))
        return toastifyError("هیچ موردی انتخاب نشده است.");
    
    externalSoftwareConfig = {
        ...config,
        entityIds,
    };
    
    
    externalSoftwareList.innerHTML = "";
    toggleElementsDisabled(sendToSoftwareBtn);
    
    await sendApiRequest({
        url: "/Home/GetAllExternalSoftware",
        method: "POST",
        successMsg: null,
        data: { entityIds, mapType: config.mapType },
        onSuccess: (res) => {
            const softwares = res.data;
            ModalToggle(externalSoftwareModalElm);
            
            if (!softwares?.length) {
                externalSoftwareList.innerHTML = `
                    <div class="text-muted text-center py-4" style="grid-column: 1 / -1">
                        نرم‌افزاری برای انتخاب وجود ندارد.
                    </div>
                `;
                return;
            }
            
            renderExternalSoftwares(softwares);
        },
    });
}

function renderExternalSoftwares(softwares) {
    externalSoftwareList.innerHTML = softwares.map(software => `
        <label for="ex-software-${software.id}" class="external-software-card ${software.isDisabled ? "is-disabled is-selected" : ""}" data-software-id="${software.id}">
            <input id="ex-software-${software.id}" type="checkbox" value="${software.id}" class="d-none" ${software.isDisabled ? "disabled checked" : ""}>

            <span class="external-software-icon">
                <i class="fas fa-check software-check-icon"></i>
            </span>

            <span class="external-software-info">
                <span class="external-software-title">${software.title}</span>
                ${software.isDisabled ? `<small class="text-muted">(قبلاً ارسال شده)</small>` : ""}
            </span>

            <i class="fa-solid fa-circle-check external-software-check"></i>
        </label>
    `).join("");
    
    externalSoftwareList.querySelectorAll(".external-software-card").forEach(card => {
        const checkbox = card.querySelector("input[type='checkbox']");
        
        checkbox.addEventListener("change", () => {
            card.classList.toggle("is-selected", checkbox.checked);
            
            const hasSelection = externalSoftwareList.querySelector("input[type='checkbox']:checked:not(:disabled)");
            
            toggleElementsDisabled(sendToSoftwareBtn, !hasSelection);
        });
    });
    
    toggleElementsDisabled(sendToSoftwareBtn, !externalSoftwareList.querySelector("input[type='checkbox']:checked:not(:disabled)"));
}


sendToSoftwareBtn?.addEventListener("click", async () => {
    const config = externalSoftwareConfig;
    if (!config?.onConfirm) return;
    
    const externalSoftwareIds = Array.from(
        externalSoftwareList.querySelectorAll("input[type='checkbox']:checked:not(:disabled)"),
    ).map(checkbox => checkbox.value);
    
    if (!config.entityIds.length)
        return toastifyError("هیچ موردی انتخاب نشده است.");
    
    if (!externalSoftwareIds.length)
        return toastifyError("هیچ نرم‌افزاری انتخاب نشده است.");
    
    toggleElementsDisabled(sendToSoftwareBtn);
    
    await config.onConfirm({
        entityIds: config.entityIds,
        externalSoftwareIds,
    });
});


externalSoftwareModalElm?.addEventListener("hidden.bs.modal", () => {
    externalSoftwareConfig = null;
    externalSoftwareList.innerHTML = "";
    toggleElementsDisabled(sendToSoftwareBtn);
});


// Re-measure scrolling tables after fonts and hidden containers settle.
(() => {
    if (!window.jQuery?.fn.dataTable) return;
    const pending = new Set();
    let frame = 0;
    let adjusting = false;
    
    function schedule(table) {
        if (adjusting || !table?.isConnected) return;
        pending.add(table);
        if (frame) return;
        frame = requestAnimationFrame(() => {
            frame = 0;
            const tables = [...pending];
            pending.clear();
            adjusting = true;
            try {
                for (const element of tables) {
                    if (!element.isConnected || !element.getClientRects().length ||
                        !jQuery.fn.dataTable.isDataTable(element)) continue;
                    const api = new jQuery.fn.dataTable.Api(element);
                    api.columns.adjust();
                    api.responsive?.recalc();
                }
            } finally {
                adjusting = false;
            }
        });
    }
    
    function scheduleVisibleTables() {
        for (const table of jQuery.fn.dataTable.tables({ visible: true })) schedule(table);
    }
    
    jQuery(document).on("init.dt.crmSizing draw.dt.crmSizing", (event, settings) => {
        if (event.namespace?.split(".").includes("dt") && settings?.nTable) {
            schedule(settings.nTable);
        }
    });
    jQuery(document).on(
        "shown.bs.modal.crmSizing shown.bs.tab.crmSizing shown.bs.collapse.crmSizing",
        scheduleVisibleTables);
    document.fonts?.ready.then(scheduleVisibleTables);
    document.fonts?.addEventListener("loadingdone", scheduleVisibleTables);
    window.addEventListener("load", scheduleVisibleTables, { once: true });
    document.addEventListener("transitionend", event => {
        if (event.target.id === "mainWrapper" && event.propertyName === "margin-right") {
            scheduleVisibleTables();
        }
    });
})();
