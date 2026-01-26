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
    const data = event.data?.json();
    console.log("push", event);
    self.registration.showNotification(data.title, {
        body: data.body,
        //icon: "/icons/icon-192.png",
        data: {
            url: data.url
        }
    }).then(r => console.log(r));
});

self.addEventListener("notificationclick", event => {
    console.log("notificationclick", event);
    event.notification.close();
    event.waitUntil(
        clients.openWindow(event.notification.data.url)
    );
});
