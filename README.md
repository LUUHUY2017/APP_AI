# Lily - Trợ lý giọng nói AI (.NET 10 & C#)

Dự án trợ lý để bàn AI Lily được chuyển đổi và tối ưu từ Python sang **C# .NET 10 (WPF)**, tuân thủ theo chuẩn giao thức **Tenclass / Xiaozhi Protocols**.

## Tính năng chính
- **Giao thức Xiaozhi Protocol v3**: Đầy đủ bắt tay Hello, quản lý phiên `session_id`, lệnh điều khiển `listen`, `abort`, tích hợp OTA kích hoạt thiết bị.
- **Xử lý âm thanh độ trễ thấp**:
  - Ghi âm thời gian thực 16kHz Mono qua `NAudio`.
  - Mã hóa/Giải mã luồng âm thanh thời gian thực qua `Concentus (Opus Codec)` (16kHz đầu vào, 24kHz đầu ra chất lượng cao).
  - Tự động đóng gói Header 16-byte Big-Endian theo đúng đặc tả phần cứng Tenclass.
- **Giao diện hiện đại & tiện lợi (WPF Modern Dark UI)**:
  - Nút bấm **Dual-Action**: Click để bật/tắt ghi âm hoặc Nhấn giữ để nói.
  - Khung chat bong bóng hiển thị câu trả lời dạng văn bản + phát giọng nói AI.
  - Hỗ trợ gõ văn bản chat trực tiếp song song với giọng nói.

## Cấu trúc thư mục
- `csharp_dotnet_src/`: Mã nguồn C# .NET 10 (Solution `Xiaozhi.Lily.sln`)
  - `Xiaozhi.App.Wpf`: Giao diện người dùng WPF (MVVM)
  - `Xiaozhi.Core`: Models, Interfaces, Constants, Config Manager
  - `Xiaozhi.Protocols`: Tầng giao thức WebSocket và OTA
  - `Xiaozhi.Audio`: Codec Opus và Audio Service (NAudio)
  - `Xiaozhi.Mcp`: MCP Tools
  - `Xiaozhi.Plugins`: Plugin mở rộng
- `source_code/`: Mã nguồn Python gốc để tham chiếu

## Yêu cầu & Hướng dẫn chạy
1. Yêu cầu cài đặt [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Mở terminal tại thư mục gốc và chạy:
```bash
cd csharp_dotnet_src
dotnet build
dotnet run --project src/Xiaozhi.App.Wpf/Xiaozhi.App.Wpf.csproj
```
