var taskList = [];
let currentMainChatId;

// اتصال به SignalR Hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/taskHub") // آدرس SignalR Hub
    .build();

connection.start();


connection.on("ReceiveNotification", async (item) => {
    if (!taskList.includes(item.id)) {
        taskList.push(item.id);
        notification(item.id, item.title, item.description, item.dateMiladi, item.entityType, item.entityId);
    }
    /*if (!preventGetRemind) {
        await getRemind();
    }*/
    
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
///////////////////////////////////////////

connection.on("countPreFactorToAccept", (countObj) => {
    refreshCounterCartable();
    //setCountEnvelope(countObj);
});
connection.on("ShowReport", async () => {
    await getAllReports(false);
});

connection.on("ShowAccessDeniedMessage", function (message) {
    toastifyError(message);
});
connection.on("Chat", async (mainChatId, text) => {
    if (location.pathname !== "/Chat/Main") {
        notificationChange(text, { link: "/Chat/Main", isBlank: false }, "chat");
        refreshCounterCartable();
        return;
    }
    if (currentMainChatId === mainChatId) {
        showChat(mainChatId, false);
    }
    showListChats();
});


////////// connectionCall ////////////
const connectionCall = new signalR.HubConnectionBuilder()
    .withUrl("/CallHub")
    .build();

connectionCall.start()
              .catch(err => console.error("خطا: " + err));

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
connection.on("SmsSent", () => {
    toastifySuccess("پیامک با موفقیت ارسال شد");
});

connection.on("SmsFailed", () => {
    toastifyError("ارسال پیامک ناموفق بود");
});