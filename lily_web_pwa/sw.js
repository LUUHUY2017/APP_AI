// Đổi hậu tố (v2, v3...) MỖI KHI đổi asset để buộc trình duyệt tải bản mới,
// tránh lặp lại lỗi "sửa code rồi nhưng web vẫn hiện bản cũ" do cache-first.
const CACHE_NAME = 'lily-pwa-v2';
const ASSETS = [
  './',
  './index.html',
  './css/style.css',
  './js/app.js',
  './js/audio-processor.js',
  './manifest.json'
];

self.addEventListener('install', (e) => {
  self.skipWaiting();
  e.waitUntil(caches.open(CACHE_NAME).then((cache) => cache.addAll(ASSETS)));
});

self.addEventListener('activate', (e) => {
  e.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (e) => {
  // Network-first cho HTML/CSS/JS: luôn ưu tiên bản mới nhất từ server, chỉ
  // dùng cache khi mất mạng (offline). Cache-first trước đây khiến deploy mới
  // không bao giờ tới tay người dùng đã từng mở app 1 lần.
  e.respondWith(
    fetch(e.request)
      .then((res) => {
        const clone = res.clone();
        caches.open(CACHE_NAME).then((cache) => cache.put(e.request, clone));
        return res;
      })
      .catch(() => caches.match(e.request))
  );
});
