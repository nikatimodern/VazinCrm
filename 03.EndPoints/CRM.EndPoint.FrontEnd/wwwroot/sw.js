const CACHE_NAME = "pwa-cache-v1";
const URLS_TO_CACHE = [
    "/",
    "/css/site.css",
    "/js/site.js"
];

self.addEventListener("install", event => {
    console.log("installed");
    /*event.waitUntil(
        caches.open(CACHE_NAME)
              .then(cache => cache.addAll(URLS_TO_CACHE))
    );*/
});

self.addEventListener("fetch", event => {
 /*   event.respondWith(
        caches.match(event.request)
              .then(response => response || fetch(event.request))
    );*/
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
            url: data.url || "/"
        },
        actions: [
            {
                action: "open",
                title: "مشاهده"
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
            options
        )
    );
});

self.addEventListener("notificationclick", event => {
    console.log("notificationclick", event);
    event.notification.close();
    
    const targetUrl = event.notification.data?.url || "/";
    /*event.waitUntil(
        clients.openWindow(targetUrl)
    );*/
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
               })
    );
});
