const CACHE_VERSION = 1;

const CACHE_NAME = `static-fallback-v${CACHE_VERSION}`;

const STATIC_PAGES = {
    OFFLINE: "/static/offline.html",
};

const STATIC_ASSETS = [
    "/static/noNet.png",
];

self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache =>
            cache.addAll([
                ...Object.values(STATIC_PAGES),
                ...STATIC_ASSETS,
            ]),
        ),
    );
    self.skipWaiting();
});
self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(
                keys
                    .filter(key => key !== CACHE_NAME)
                    .map(key => caches.delete(key)),
            ),
        ),
    );
    self.clients.claim();
});

self.addEventListener("fetch", event => {
    const req = event.request;
    
    // صفحه‌ها
    if (req.mode === "navigate") {
        event.respondWith(
            fetch(req).catch(() => caches.match(STATIC_PAGES.OFFLINE)),
        );
        return;
    }
    
    // تصاویر (برای offline page)
    if (req.destination === "image") {
        event.respondWith(
            caches.match(req).then(res => res || fetch(req)),
        );
    }
});


self.addEventListener("push", event => {
    console.log(event);
    if (!event.data) return;
    const data = event.data.json();
    
    const options = {
        body: data.body || "",
        icon: "/static/icons/logo-192x192.png",
        badge: "/static/icons/logo-72x72.png",
        //image: "/static/noNet.png",
        tag: data.tag || "general",
        renotify: true,
        silent: false,
        vibrate: [200, 50, 200, 50, 200],
        requireInteraction: false,
        dir: "rtl",
        lang: "fa-IR",
        data: {
            url: data.url || "/",
        },
        actions: [
            {
                action: "open",
                title: "مشاهده",
            },
        ],
    };
    event.waitUntil(
        self.registration.showNotification(
            data.title || "پیام جدید",
            options,
        ),
    );
});

self.addEventListener("notificationclick", event => {
    event.notification.close();
    
    const targetUrl = event.notification.data?.url || "/";
    
    /* if (event.action === "dismiss") {
         return;
     }*/
    
    event.waitUntil(
        clients.matchAll({ type: "window", includeUncontrolled: true })
               .then(clientList => {
                   for (const client of clientList) {
                       console.log(client.url);
                       console.log(targetUrl);
                       if (client.url === targetUrl && "focus" in client) {
                           return client.focus();
                       }
                   }
                   if (clients.openWindow) {
                       return clients.openWindow(targetUrl);
                   }
               }),
    );
});
