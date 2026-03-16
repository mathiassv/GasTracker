const CACHE = 'gastracker-v2';

// Static assets to pre-cache on install
const PRECACHE = [
    '/app.css',
    '/theme.js',
    '/manifest.json',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css',
    'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css',
    'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js',
    'https://cdn.jsdelivr.net/npm/chart.js@4.4.3/dist/chart.umd.min.js'
];

self.addEventListener('install', event => {
    self.skipWaiting();
    event.waitUntil(
        caches.open(CACHE).then(cache => cache.addAll(PRECACHE).catch(() => {}))
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k)))
        ).then(() => self.clients.claim())
    );
});

// Auth paths that must never be intercepted — the OAuth state cookie
// relies on these responses coming directly from the server.
const AUTH_PATHS = /^\/(signin-google|auth\/|connect\/|_\/auth)/i;

self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // Only handle GET requests
    if (request.method !== 'GET') return;

    // Never intercept auth / OAuth callback routes
    if (url.origin === self.location.origin && AUTH_PATHS.test(url.pathname)) return;

    // For same-origin navigation requests: network-first, fall back to cached '/'
    if (request.mode === 'navigate' && url.origin === self.location.origin) {
        event.respondWith(
            fetch(request)
                .catch(() => caches.match('/').then(r => r || fetch(request)))
        );
        return;
    }

    // For static assets (CSS, JS, images, fonts): cache-first
    const isStatic =
        url.pathname.match(/\.(css|js|png|jpg|svg|woff2?|ttf|ico)$/) ||
        url.hostname.includes('jsdelivr.net') ||
        url.hostname.includes('cloudflare.com');

    if (isStatic) {
        event.respondWith(
            caches.match(request).then(cached => {
                if (cached) return cached;
                return fetch(request).then(response => {
                    if (response.ok) {
                        caches.open(CACHE).then(cache => cache.put(request, response.clone()));
                    }
                    return response;
                });
            })
        );
        return;
    }

    // Everything else: network only
});
