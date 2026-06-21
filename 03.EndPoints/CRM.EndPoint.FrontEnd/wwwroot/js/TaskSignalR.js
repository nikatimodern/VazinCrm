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

connectionCall.on("IncomingCall", function (callerNumber, destNumber, uniqueid) {
    // console.log("IncomingCall " + uniqueid);
    notificationIncomingCall(callerNumber, uniqueid, destNumber);
});

connectionCall.on("CallEnded", function (uniqueid) {
    // console.log("CallEnded " + uniqueid);
    closeCallNotif(uniqueid);
});

connectionCall.on("CallAnswered", function (uniqueid) {
    // console.log("CallAnswered " + uniqueid);
    answerCallNotif(uniqueid);
});


////////// sms toastify ////////////
connection.on("SmsSuccess", () => {
    //toastifySuccess("پیامک با موفقیت ارسال شد");
    updateSmsToast("پیامک با موفقیت ارسال شد", "success");
});

connection.on("SmsFailed", () => {
    //toastifyError("ارسال پیامک ناموفق بود");
    updateSmsToast("ارسال پیامک ناموفق بود", "error");
});

connection.on("SmsSending", () => {
    showSmsSendingToast();
});

