const CACHE_NAME = "offline-only-v1";
const OFFLINE_URL = [
    "/static/offline.html",
    "/static/noNet.png",
];

self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll(OFFLINE_URL);
        }),
    );
    self.skipWaiting();
});
self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(
                keys
                    .filter(k => k !== CACHE_NAME)
                    .map(k => caches.delete(k))
            )
        )
    );
    self.clients.claim();
});

self.addEventListener("fetch", event => {
    
    // فقط navigation (load صفحه)
    if (event.request.mode === "navigate") {
        event.respondWith(
            fetch(event.request)
                .then(response => {
                    return response;
                })
                .catch(error => {
                    return caches.match(OFFLINE_URL[0]);
                }),
        );
    }
});


self.addEventListener("push", event => {
    if (!event.data) return;
    const data = event.data.json();
    console.log("push", event);
    
    const options = {
        body: data.body || "",
        // icon: "/icons/icon-192.png",
        // badge: "/icons/badge.png",
        data: {
            url: data.url || "/",
        },
        actions: [
            {
                action: "open",
                title: "مشاهده",
            },
            /* {
                 action: "dismiss",
                 title: "بستن"
             }*/
        ],
        tag: data.tag || "general",
        renotify: true,
        silent: false,
    };
    event.waitUntil(
        self.registration.showNotification(
            data.title || "پیام جدید",
            options,
        ),
    );
});

self.addEventListener("notificationclick", event => {
    console.log("notificationclick", event);
    event.notification.close();
    
    const targetUrl = event.notification.data?.url || "/";
    event.waitUntil(
        clients.matchAll({ type: "window", includeUncontrolled: true })
               .then(clientList => {
                   for (const client of clientList) {
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
