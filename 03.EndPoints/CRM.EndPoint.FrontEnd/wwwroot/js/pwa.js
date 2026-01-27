// Service Worker Registration
if ("serviceWorker" in navigator) {
    window.addEventListener("load", async () => {
        try {
            await navigator.serviceWorker.register("/sw.js");
            console.log("Service Worker registered");
            await initPush();
            console.log(await axios.post(`/Push/TestPush/`));
        } catch (err) {
            console.error("Service Worker registration failed", err);
        }
    });
}

// Push Init
async function initPush() {
    if (!("Notification" in window)) return;
    
    const registration = await navigator.serviceWorker.ready;
    
    // آیا قبلاً subscribe شده؟
    const existingSubscription = await registration.pushManager.getSubscription();
    
    if (existingSubscription) {
        console.log("Already subscribed", existingSubscription);
        return;
    }
    
    // اجازه کاربر
    const permission = await Notification.requestPermission();
    if (permission !== "granted") {
        console.warn("Notification permission denied");
        return;
    }
    
    await subscribeUser(registration);
}


// Subscribe User
async function subscribeUser(registration) {
    const publicKey = (await axios.get("/Push/GetPublicKey")).data;
    const applicationServerKey = urlBase64ToUint8Array(publicKey);
    
    const subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey
    });
    
    console.log("New subscription", subscription);
    
    await axios.post("/Push/Subscribe", subscription);
}

// Utils
function urlBase64ToUint8Array(base64String) {
    const padding = "=".repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
        .replace(/-/g, "+")
        .replace(/_/g, "/");
    
    const rawData = atob(base64);
    return Uint8Array.from([...rawData].map(c => c.charCodeAt(0)));
}

