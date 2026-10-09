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

const callHistoryBtn = document.getElementById("callHistoryReportBtn");

connectionCall.on("IncomingCall", function (callerTextNumber, number, destNumber, uniqueid) {
    // console.log("IncomingCall " + uniqueid);
    notificationIncomingCall(callerTextNumber, uniqueid, destNumber);
    localStorage.setItem("callData", JSON.stringify(number));
    
    callHistoryBtn.classList.add("is-visible", "is-ringing");
    callHistoryBtn.classList.remove("is-answered", "is-open");
});


async function showCallReport() {
    callHistoryBtn.classList.remove("is-ringing", "is-answered");
    callHistoryBtn.classList.add("is-open");
    
    const mobileNumber = JSON.parse(localStorage.getItem("callData"));
    
    ModalToggle("incomingCallReportModal", "show");
    
    await sendApiRequest({
        url: "/Report/GetCallReport",
        params: { mobileNumber },
        onSuccess: (res) => {
            document.getElementById("incomingCallReportContainer").innerHTML = res.data.html;
        },
    });
}
async function closeCallReport() {
    // ModalToggle("incomingCallReportModal", "hide");
    await delay(5000);
    localStorage.removeItem("callData");
    callHistoryBtn.classList.remove("is-visible", "is-ringing", "is-answered", "is-open");
}

connectionCall.on("CallEnded", async function (uniqueid) {
    console.log("CallEnded " + uniqueid);
    
    const notif = activeNotifCall[uniqueid];
    if (notif) {
        await closeCallReport();
    }
    
    closeCallNotif(uniqueid);
});

connectionCall.on("CallAnswered", function (uniqueid) {
    // console.log("CallAnswered " + uniqueid);
    answerCallNotif(uniqueid);
    
    const notif = activeNotifCall[uniqueid];
    if (notif) {
        callHistoryBtn.classList.remove("is-ringing");
        callHistoryBtn.classList.add("is-answered");
    }
});


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

