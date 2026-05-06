const CACHE = 'alba-v1';
const OFFLINE = ['/'];

self.addEventListener('install', e => {
    e.waitUntil(caches.open(CACHE)
        .then(c => c.addAll(OFFLINE)));
});

self.addEventListener('fetch', e => {
    e.respondWith(
        fetch(e.request).catch(() =>
            caches.match(e.request))
    );
});