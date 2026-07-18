window.addEventListener("load", async () => {
    const swRegistered = await registerServiceWorker();
    if (!swRegistered) return;
    await navigator.serviceWorker.ready;
    
    
    // initInstallPrompt();
    await initPush();
    // await showIosPwaInstall();
});


async function registerServiceWorker() {
    if ("serviceWorker" in navigator) {
        try {
            await navigator.serviceWorker.register("/sw.js");
            return true;
        } catch (err) {
            console.error("Service Worker registration failed", err);
            return false;
        }
    }
    return false;
}


let deferredPrompt = null;
function initInstallPrompt() {
    //if (!isDesktop()) return;
    window.addEventListener("beforeinstallprompt", (e) => {
        e.preventDefault();
        deferredPrompt = e;
        
        if (sessionStorage.getItem("pwa-install-shown")) return;
        
        setTimeout(() => {
            ModalToggle("#pwaInstallModal");
            sessionStorage.setItem("pwa-install-shown", "1");
        }, 1000);
    });
}


async function showIosPwaInstall() {
    if (!isIos()) return;
    if (isRunningInPwa()) return;
    if (sessionStorage.getItem("pwaios-install-shown")) return;
    setTimeout(() => {
        ModalToggle("#iosPwaInstallModal");
        sessionStorage.setItem("pwaios-install-shown", "1");
    }, 2000);
}


document.getElementById("pwaInstallBtn")?.addEventListener("click", async () => {
    if (!deferredPrompt) return;
    
    deferredPrompt.prompt();
    const { outcome } = await deferredPrompt.userChoice;
    
    console.log("Install result:", outcome);
    
    deferredPrompt = null;
    ModalToggle("#pwaInstallModal", "hide");
});


// Push Init
async function initPush() {
    if (!("Notification" in window)) return;
    
    const registration = await navigator.serviceWorker.ready;
    
    // آیا قبلاً subscribe شده؟
    const existingSubscription = await registration.pushManager.getSubscription();
    
    if (existingSubscription) {
        //console.log("Already subscribed", existingSubscription);
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
        applicationServerKey,
    });
    
    console.log("New subscription", subscription);
    
    const res = await axios.post("/Push/Subscribe", subscription);
    if (res.status === 200) {
        console.log("Subscribed successfully", res);
    }
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

function isIos() {
    return /iphone|ipad|ipod/i.test(navigator.userAgent);
}

function isRunningInPwa() {
    return window.matchMedia("(display-mode: standalone)").matches
        || window.navigator.standalone === true;
}

function isDesktop() {
    if (navigator.userAgentData) {
        return !navigator.userAgentData.mobile;
    }
    
    return !/Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
}

function goBack() {
    if (document.referrer && document.referrer.startsWith(location.origin)) {
        history.back();
    } else {
        location.href = "/";
    }
}

if (isRunningInPwa() && isDesktop()) {
    document.querySelector(".header-actions")?.classList.remove("d-none");
    
    /*if (document.referrer && document.referrer !== location.href) {
        const backBtn = document.querySelector(".back-btn");
        if (backBtn) backBtn.style.display = "flex";
    }*/
}

