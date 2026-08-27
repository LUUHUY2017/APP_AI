/**
 * ============================================================================
 * LILY AI - SERVICE WORKER (Bộ quản lý Cache & Tự động Cập nhật Offline)
 * ============================================================================
 * Mục đích:
 * 1. Lưu đệm (Cache) các tài nguyên giao diện để app mở nhanh và chạy được offline.
 * 2. Cung cấp cơ chế Network-First: Luôn ưu tiên lấy bản mới nhất từ server GitHub Pages.
 * 3. Tự động dọn dẹp cache cũ khi deploy phiên bản mới mà không cần xóa lịch sử duyệt web.
 */

// Tên Cache phiên bản hiện tại (Mỗi khi thay đổi lớn có thể tăng v3, v4...)
const CACHE_NAME = 'lily-pwa-v3';

// Danh sách các file cốt lõi cần nạp vào bộ nhớ đệm
const ASSETS = [
  './',                  // Trang gốc
  './index.html',        // Giao diện HTML chính
  './css/style.css',     // Toàn bộ CSS phong cách Neon Glassmorphism
  './js/app.js',         // Logic xử lý chính của ứng dụng
  './manifest.json',     // Cấu hình Progressive Web App (icon, theme)
  './icons/icon-192.png',// Icon ứng dụng độ phân giải 192px
  './icons/icon-512.png' // Icon ứng dụng độ phân giải 512px
];

/**
 * Sự kiện INSTALL: Kích hoạt khi trình duyệt phát hiện Service Worker mới.
 * Sử dụng self.skipWaiting() để lập tức thay thế Service Worker cũ đang chạy.
 */
self.addEventListener('install', (e) => {
  self.skipWaiting();
  e.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(ASSETS).catch((err) => {
        console.warn('SW cache addAll warning:', err);
      });
    })
  );
});

/**
 * Sự kiện ACTIVATE: Kích hoạt khi Service Worker mới chính thức tiếp quản trang.
 * Tự động quét và xóa toàn bộ các Cache cũ (v1, v2...) để giải phóng bộ nhớ.
 */
self.addEventListener('activate', (e) => {
  e.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

/**
 * Sự kiện FETCH: Bắt mọi yêu cầu tải tài nguyên từ trang web.
 * Chiến lược: NETWORK-FIRST
 * - Bước 1: Gửi request lên server để lấy file mới nhất.
 * - Bước 2: Nếu có mạng và tải thành công -> lưu bản mới vào Cache và trả về cho người dùng.
 * - Bước 3: Nếu mất mạng (offline) -> lấy bản đã lưu trong Cache ra phục vụ.
 */
self.addEventListener('fetch', (e) => {
  // Chỉ can thiệp vào các phương thức GET (tải trang, css, js, ảnh)
  if (e.request.method !== 'GET') return;
  
  e.respondWith(
    fetch(e.request)
      .then((res) => {
        if (res && res.status === 200 && res.type === 'basic') {
          const clone = res.clone();
          caches.open(CACHE_NAME).then((cache) => cache.put(e.request, clone));
        }
        return res;
      })
      .catch(() => caches.match(e.request)) // Fallback offline
  );
});
