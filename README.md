# TONY AI (SUSU FILM AI) - Trợ Lý Giọng Nói Thông Minh Đa Nền Tảng (.NET 10 WPF & iOS MAUI)

Dự án Trợ lý giọng nói AI **Tony AI** (tên thương hiệu **SUSU FILM AI**) được phát triển trên nền tảng **.NET 10 (C# WPF)** dành cho Windows PC và **.NET MAUI** dành cho hệ điều hành iOS (iPhone/iPad), tuân thủ theo chuẩn giao thức **Tenclass / Xiaozhi Protocols**.

---

## 🎨 Giao diện & Trải nghiệm Người dùng (Google Gemini iOS Theme)
- **Giao diện Pitch Black OLED (`#000000`)**: Thiết kế chuẩn ứng dụng Google Gemini iOS với thanh nhập liệu capsule nổi (`#171A20`).
- **Nút bấm thu âm linh hoạt (Clean Icon-Only Mic)**:
  - Trạng thái chờ: Biểu tượng Micro `🎙️` tinh tế.
  - Trạng thái đang thu âm: Chấm đỏ thu âm rực rỡ `🔴` (`#FF3B30`).
- **Phản hồi Rung cảm ứng (Haptic Vibration Feedback)**: Rung nẩy nhẹ 1 nhịp (220ms) ngay khi phát hiện người dùng vừa ngắt câu nói.
- **Tự động đẩy tin nhắn lên trên Bàn phím (`Keyboard Auto-Scroll`)**: Tự động cuộn mượt trượt danh sách tin nhắn chat lên mép trên bàn phím khi bấm ô nhập liệu.

---

## 🗣️ Danh sách Lệnh Giọng nói & Thao tác Tự động (Voice Actions)

| Nhóm Tính năng | Lệnh Giọng nói Mẫu | Hành động Tự động của Tony AI |
| :--- | :--- | :--- |
| 🛡️ **Bảo vệ Ngân hàng** | *"Tony mở Vietcombank"*, *"Mở MoMo"*, *"Chuyển tiền"* | **Nghiêm cấm & Từ chối 100%** để bảo vệ an toàn tuyệt đối cho tài khoản tài chính người dùng. |
| 📸 **Camera & Máy ảnh** | *"Tony mở camera"*, *"Tony chụp ảnh"* | Tự động kích hoạt Máy ảnh iOS/PC ở chế độ Chụp ảnh. |
| 🎥 **Quay Video** | *"Tony quay video"*, *"Tony quay phim"* | Tự động mở chế độ Quay Video thời gian thực. |
| 🔊 **Điều chỉnh Âm lượng**| *"Tony tăng âm lượng"*, *"Giảm âm lượng"* | Tăng/giảm trực tiếp âm lượng giọng phát của Tony AI. |
| 📞 **Gọi điện thoại** | *"Tony gọi điện cho [SĐT/Tên]"* | Tự động mở trình gọi điện thoại native (`tel:`). |
| 💬 **Gọi / Nhắn Zalo** | *"Tony gọi Zalo cho [Tên]"*, *"Nhắn Zalo"* | Tự động kích hoạt ứng dụng Zalo. |
| 🗺️ **Bản đồ Dẫn đường** | *"Tony chỉ đường tới [Địa điểm]"* | Tự động mở Apple Maps dẫn đường Turn-by-Turn. |
| ⏰ **Xem Giờ & Ngày** | *"Tony mấy giờ rồi"*, *"Hôm nay ngày mấy"* | Phản hồi chính xác giờ và thứ ngày tháng Việt Nam. |
| ☀️ **Thời tiết Real-time** | *"Tony thời tiết hôm nay"* | Dự báo thời tiết nhiệt độ thực tế out loud. |
| 🚀 **Khởi chạy App** | *"Tony mở YouTube"*, *"Mở Facebook"*, *"Mở TikTok"* | Tự động khởi chạy ứng dụng tương ứng. |

---

## ⚙️ Thuật toán Core & Động cơ Giọng nói (Voice Engine)
- **VAD Silence Auto-Send (1.4s)**: Tự động ngắt micro và chuyển sang xử lý AI ngay khi người dùng ngừng nói 1.4 giây.
- **Chế độ Rảnh tay liên tục (Auto Hands-Free Conversational Loop)**: Tự động lắng nghe câu tiếp theo ngay khi Tony trả lời xong mà không cần bấm nút.
- **Chạy nền iOS (`UIBackgroundModes: Audio`)**: Lắng nghe câu lệnh giọng nói kể cả khi ứng dụng thu nhỏ hoặc tắt màn hình (khi bật Rảnh tay).
- **1-Click Auto Token Pairing & OTP**: Tự sinh Token kích hoạt 1-chạm hoặc ghép nối mã 6 số OTP chuẩn OTA với server `xiaozhi.me`.
- **Đồng bộ ConfigManager**: Dùng chung file cấu hình `config.json` giữa Windows PC (.exe) và iOS (.ipa).

---

## 📁 Cấu trúc Thư mục Mã nguồn
- `csharp_dotnet_src/`: Mã nguồn C# .NET 10 (Solution `Xiaozhi.Lily.sln`)
  - `src/Xiaozhi.App.Wpf`: Ứng dụng Desktop Windows (.exe)
  - `src/Xiaozhi.App.Maui`: Ứng dụng Cross-Platform iOS (.ipa)
  - `src/Xiaozhi.Core`: Quản lý cấu hình `ConfigManager`, Models & Interfaces
  - `src/Xiaozhi.Protocols`: Tầng giao thức WebSocket (`wss://`) và OTA (`https://`)
  - `src/Xiaozhi.Audio`: Bộ nén/giải nén Opus Codec & VAD Silence Detector
- `lily_web_pwa/`: Giao diện Web PWA ứng dụng Google Gemini Dark Theme

---

## 🚀 Hướng dẫn Biên dịch & Chạy
### 1. Trên Windows PC (WPF):
```bash
cd csharp_dotnet_src
dotnet build
dotnet run --project src/Xiaozhi.App.Wpf/Xiaozhi.App.Wpf.csproj
```

### 2. Trên iOS (Codemagic CI/CD Build):
Push code lên kho GitHub `https://github.com/LUUHUY2017/APP_AI.git` -> Trigger **`Start new build`** trên Codemagic để tự động đóng gói file `.ipa` cài đặt vào iPhone qua 3uTools!
