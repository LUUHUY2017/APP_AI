# Luồng chạy ứng dụng Lily Windows (.exe)

## 1. Phạm vi của bản `.exe`

Điểm vào Windows nằm trong project `src/Xiaozhi.App.Wpf`. File project khai báo `OutputType=WinExe`, target `net8.0-windows` và `UseWPF=true`. WPF gọi năm thư viện nội bộ:

- `Xiaozhi.Core`: model, hằng số, interface, cấu hình và danh tính thiết bị.
- `Xiaozhi.Audio`: thu micro, phát loa, Opus, VAD và text-to-audio streaming.
- `Xiaozhi.Protocols`: OTA/activation và WebSocket với máy chủ Xiaozhi.
- `Xiaozhi.Plugins`: phím tắt toàn cục.
- `Xiaozhi.Mcp`: công cụ mở/đóng ứng dụng hệ thống.

`Xiaozhi.App.Maui` là client đa nền tảng khác, không nằm trên đường khởi động của file WPF `.exe`.

## 2. Đường khởi động

```text
Windows mở Xiaozhi.App.Wpf.exe
  -> App.xaml chọn StartupUri="MainWindow.xaml"
  -> WPF tạo MainWindow và DataContext=MainViewModel
  -> MainWindow constructor nối sự kiện UI/ViewModel
  -> sự kiện Loaded đăng ký Ctrl+J, Ctrl+K, Ctrl+Q
  -> MainViewModel.InitializeAsync()
  -> EnsureConnectedAsync()
  -> đọc config -> gọi OTA discovery -> tạo WebSocket -> nối event -> ConnectAsync
```

`ConfigManager` đọc `%APPDATA%/XiaozhiLily/config.json` và `efuse.json`. Nếu chưa có hoặc JSON lỗi, lớp này tạo cấu hình mặc định. Riêng efuse còn thử file `config/efuse.json` cạnh thư mục chạy trước khi tự sinh fingerprint.

## 3. Luồng nói bằng micro

```text
Người dùng bấm micro / Ctrl+J
  -> MainWindow.TalkBtn_MouseDown
  -> MainViewModel.StartListeningAsync
  -> gửi thông báo listen.start qua WebSocket
  -> NAudioAudioService.StartRecording
  -> từng buffer PCM 16 kHz mono phát OnAudioRecorded
  -> MainViewModel.OnAudioCaptured
       -> VoiceActivityDetector.ProcessPcm
       -> OpusCodec.Encode
       -> XiaozhiWebSocketClient.SendAudioAsync
  -> im lặng đủ lâu làm VAD phát OnSpeechEnded
  -> StopListeningAsync đóng micro và gửi listen.stop
```

`NAudioAudioService` ưu tiên micro mặc định qua WASAPI. Nếu thất bại, nó dùng thiết bị `WaveInEvent` đầu tiên. Audio WASAPI được đổi về PCM 16-bit, mono, 16 kHz trước khi đi tiếp.

## 4. Luồng phản hồi của AI

Sau `listen.stop`, server xử lý theo chuỗi STT -> LLM -> TTS và trả các loại message khác nhau:

- STT: `OnSttReceived` thêm câu người dùng vào chat và chuyển trạng thái sang “AI đang suy nghĩ”.
- LLM: `OnLlmResponse` thêm nội dung trả lời của assistant vào chat.
- TTS state: `OnTtsStateChanged` bật/tắt trạng thái đang nói và avatar.
- Audio: `OnIncomingAudio` giải mã Opus 24 kHz thành PCM rồi gọi `NAudioAudioService.PlayAudio`.

`MainViewModel` chỉ phát `PropertyChanged` và `MessageAdded`. `MainWindow` nhận các event này qua `Dispatcher`, rồi cập nhật nhãn, màu trạng thái, animation và bong bóng chat trên UI thread.

## 5. Luồng gửi văn bản

```text
Enter hoặc nút Gửi
  -> MainWindow.SendCurrentText
  -> MainViewModel.SendTextMessageAsync
  -> thêm bong bóng user
  -> câu <= 8 ký tự: SendTextQueryAsync
  -> câu dài hơn: TextToAudioStreamer.StreamTextAsAudioAsync
  -> phản hồi quay về cùng các event STT/LLM/TTS/audio
```

Biến `_lastSentUserText` ngăn cùng một câu xuất hiện hai lần nếu server gửi lại STT cho nội dung đã nhập bằng bàn phím.

## 6. Hands-free, hủy và timeout

- Hands-free bật micro ngay. Khi TTS kết thúc, timer chờ ngắn rồi tự gọi `StartListeningAsync`, tạo vòng lặp hội thoại liên tục.
- Nút hủy hoặc `Ctrl+Q` xóa audio đang phát và gửi `abort` với lý do `user_interrupt`.
- Timer request 15 giây đưa giao diện về trạng thái sẵn sàng nếu không nhận phản hồi.
- Timer TTS 3 giây sửa trạng thái khi server/audio không gửi tín hiệu kết thúc đúng lúc.

## 7. Vai trò các file mã nguồn đang dùng

| File/nhóm | Vai trò |
|---|---|
| `App.xaml` | Chọn cửa sổ khởi động và tài nguyên cấp ứng dụng. |
| `MainWindow.xaml` | Cây giao diện WPF và binding/event handler. |
| `MainWindow.xaml.cs` | Điều phối thao tác chuột/phím, modal và cập nhật UI. |
| `ViewModels/MainViewModel.cs` | Trung tâm trạng thái; nối giao diện với audio và protocol. |
| `Views/SettingsWindow.*` | Đọc/sửa/lưu endpoint, token và thông tin thiết bị. |
| `Views/ActivationWindow.*` | Hiển thị mã kích hoạt/QR và theo dõi activation. |
| `Core/Models/*` | Kiểu dữ liệu cấu hình, chat và message giao thức. |
| `Core/Utils/ConfigManager.cs` | Nạp, tạo mặc định và lưu cấu hình trong AppData. |
| `Core/Utils/DeviceFingerprint.cs` | Sinh serial/HMAC từ danh tính thiết bị. |
| `Audio/Codecs/OpusCodec.cs` | Mã hóa PCM gửi đi và giải mã Opus nhận về. |
| `Audio/Services/NAudioAudioService.cs` | Giao tiếp micro/loa Windows. |
| `Audio/Services/VoiceActivityDetector.cs` | Phát hiện bắt đầu nói và khoảng im lặng kết thúc câu. |
| `Audio/Services/TextToAudioStreamer.cs` | Biến câu text dài thành luồng audio tương thích giao thức. |
| `Audio/WakeWord/WakeWordDetector.cs` | Nhận diện từ đánh thức; hiện chưa được `MainViewModel` WPF khởi tạo. |
| `Protocols/WebSocket/XiaozhiWebSocketClient.cs` | Kết nối, serialize message, gửi audio và phân loại message nhận. |
| `Protocols/Ota/DeviceActivationService.cs` | OTA discovery và quy trình kích hoạt thiết bị. |
| `Plugins/GlobalShortcutPlugin.cs` | Đăng ký/phân phối hotkey native Windows. |
| `Mcp/Tools/SystemAppTool.cs` | Liệt kê, mở và đóng ứng dụng; chưa được UI WPF gọi trực tiếp. |

Các file `Class1.cs` là placeholder do template .NET tạo, hiện không tham gia luồng chạy.

## 8. Điểm cần lưu ý khi bảo trì

- Nhiều khối `catch { }` cố giữ ứng dụng không bị crash nhưng cũng che mất nguyên nhân lỗi; nên thay bằng logging khi debug production.
- `async void` chỉ phù hợp với event handler. `OnAudioCaptured` là callback event nên chấp nhận được, nhưng lỗi hiện đang bị nuốt.
- `ReconnectAsync` bỏ tham chiếu client cũ nhưng chưa đóng/dispose WebSocket cũ một cách tường minh.
- Token mặc định `test-token` chỉ là fallback; kết nối thật phụ thuộc OTA/config hợp lệ.
- `MainWindow` cập nhật UI thủ công dù đã có ViewModel; có thể tăng binding để giảm code-behind, nhưng đó là refactor và không được thực hiện trong lần chú thích này.
