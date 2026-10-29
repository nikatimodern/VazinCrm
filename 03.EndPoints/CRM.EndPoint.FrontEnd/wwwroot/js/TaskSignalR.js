var taskList = [];

// اتصال به SignalR Hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/taskHub") // آدرس SignalR Hub
    .build();

connection
    .start()
    .then(() => {
        window.signalRConnectionId = connection.connectionId;
    })
    .catch(err => console.error("SignalR Error:", err));


////////// Set ConnectionId in Header Axios ////////////
axios.interceptors.request.use(config => {
    if (window.signalRConnectionId) {
        config.headers["X-SignalR-ConnectionId"] = window.signalRConnectionId;
    }
    return config;
});


////////// Carizo notification ////////////
connection.on("ReceiveNotification", async (item) => {
    if (!taskList.includes(item.id)) {
        taskList.push(item.id);
        notification(item.id, item.title, item.description, item.dateMiladi, item.entityType, item.entityId);
    }
});

////////// notificationChange ////////////
connection.on("notifActionRequired", (text, link) => {
    notificationChange(text, { link, isBlank: true }, "actionRequired");
});
connection.on("notifInfo", (text, link) => {
    notificationChange(text, { link, isBlank: true }, "info");
});
connection.on("notifTask", (text, link = "/carizo/Index") => {
    notificationChange(text, { link, isBlank: true }, "task");
});
connection.on("notifSystem", (text, link) => {
    notificationChange(text, { link, isBlank: true }, "default");
});


////////// Count Cartable ////////////

connection.on("countOfPendingCartable", (countObj) => {
    refreshCounterCartable();
    //setCountEnvelope(countObj);
});

////////// ShowReport ////////////
connection.on("ShowReport", async () => {
    await getAllReports(false);
});


////////// AccessDeniedMessage ////////////
connection.on("ShowAccessDeniedMessage", function (message) {
    toastifyError(message);
});


////////// Chat ////////////
connection.on("NotifChat", async (mainChatId, text) => {
    if (location.pathname.toLowerCase() !== "/chat/main") {
        notificationChange(text, { link: "/Chat/Main", isBlank: false }, "chat");
        refreshCounterCartable();
        return;
    }
    const { mci } = getUrlParams();
    if (mci === mainChatId) {
        await showChat(mainChatId, false);
    }
    showListChats();
});
connection.on("UpdateChat", async (mainChatId) => {
    if (location.pathname.toLowerCase() !== "/chat/main") return;
    const { mci } = getUrlParams();
    if (mci === mainChatId) {
        await showChat(mainChatId, false);
    }
    showListChats();
});


////////// connectionCall ////////////
const connectionCall = new signalR.HubConnectionBuilder()
    .withUrl("/CallHub")
    .build();

connectionCall
    .start()
    .catch(err => console.error("SignalR Error:", err));

connectionCall.on("IncomingCall", handleIncomingCall);
connectionCall.on("CallAnswered", handleCallAnswered);
connectionCall.on("CallEnded", handleCallEnded);

const callHistoryBtn = document.getElementById("callHistoryReportBtn");

// Incoming call
function handleIncomingCall(callerTextNumber, number, destNumber, uniqueid, srcIsInternal) {
    if (!notificationIncomingCall(callerTextNumber, uniqueid, destNumber)) return;
    if (srcIsInternal) return;
    
    localStorage.setItem("callData", JSON.stringify(number));
    
    callHistoryBtn.classList.remove("is-answered", "is-open", "is-ended");
    callHistoryBtn.classList.add("is-visible", "is-ringing");
}

// Call answered
async function handleCallAnswered(uniqueid) {
    if (!answerCallNotif(uniqueid)) return;
    
    await showIncomingCallReport();
    
    callHistoryBtn.classList.remove("is-ringing");
    callHistoryBtn.classList.add("is-answered");
}

// Call ended
async function handleCallEnded(uniqueid) {
    if (!closeCallNotif(uniqueid)) return;
    
    closeIncomingCallReport();
}

// Show call report
async function showIncomingCallReport() {
    if (isModalOpen("incomingCallReportModal")) {
        ModalToggle("incomingCallReportModal", "hide");
        return;
    }
    const mobileNumber = JSON.parse(localStorage.getItem("callData"));
    if (!mobileNumber) return;
    
    // callHistoryBtn.classList.remove("is-ringing", "is-answered");
    callHistoryBtn.classList.add("is-open");
    
    ModalToggle("incomingCallReportModal", "show");
    
    await sendApiRequest({
        url: "/Report/GetCallReport",
        params: { mobileNumber },
        onSuccess: (res) => {
            document.getElementById("incomingCallReportContainer").innerHTML = res.data.html;
        },
    });
}

// Close call report
function closeIncomingCallReport() {
    callHistoryBtn.classList.add("is-ended");
    
    setTimeout(() => {
        ModalToggle("incomingCallReportModal", "hide");
        localStorage.removeItem("callData");
        
        callHistoryBtn.classList.remove("is-visible", "is-ringing", "is-answered", "is-open", "is-ended");
    }, 5000);
}


////////// sms toastify ////////////
connection.on("SmsSuccess", () => {
    //toastifySuccess("پیامک با موفقیت ارسال شد");
    updateSmsToast("پیامک با موفقیت ارسال شد", "success");
});

connection.on("SmsFailed", (message) => {
    //toastifyError("ارسال پیامک ناموفق بود");
    updateSmsToast(message || "ارسال پیامک ناموفق بود", "error");
});

connection.on("SmsSending", () => {
    showSmsSendingToast();
});


let smsToast = null;
function showSmsSendingToast() {
    if (smsToast) {
        smsToast.hideToast();
    }
    
    const notifDiv = document.createElement("div");
    
    notifDiv.className = "flex-center gap-3";
    notifDiv.innerHTML = `
        در حال ارسال پیامک
        <span class="spinner-border spinner-border-sm"></span>
    `;
    
    smsToast = createToast("info", "", {
        node: notifDiv,
        duration: -1,
        stopOnFocus: false,
        style: {
            cursor: "auto",
        },
    });
}


function updateSmsToast(text, type) {
    if (!smsToast) return;
    const smsToastElement = smsToast.toastElement;
    
    smsToastElement.textContent = text;
    
    
    if (type === "success" || type === "error") {
        
        smsToastElement.style.background =
            type === "success"
                ? "linear-gradient(135deg, rgba(2,81,75,1) 21%, rgba(0,59,54,1) 100%)"
                : "linear-gradient(135deg, rgba(121,9,9,1) 21%, rgba(78,0,0,0.9) 100%)";
        
        setTimeout(() => {
            smsToast?.hideToast();
            smsToast = null;
        }, type === "success" ? 3000 : 5000);
    }
}
