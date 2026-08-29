# Giải thích `StartListeningAsync` và `OnPropertyChanged`

Tài liệu này giải thích đường chạy thật của bản Windows `.exe` trong `csharp_dotnet_src/src/Xiaozhi.App.Wpf`.

## 1. `StartListeningAsync` làm gì?

Đây là hàm điều phối việc bắt đầu một lượt nói. Nó không chỉ bật micro mà chạy tuần tự bảy bước:

```text
1. EnsureConnectedAsync: bảo đảm WebSocket đang kết nối
2. _vad.Reset: xóa trạng thái giọng nói/im lặng của câu trước
3. _isListening = true: cho phép xử lý và gửi buffer audio
4. Cập nhật các property: báo UI chuyển sang trạng thái đang nghe
5. Dừng timer cũ: tránh phiên trả lời trước can thiệp UI
6. protocolClient.StartListeningAsync: báo server bắt đầu lượt nói
7. audioService.StartRecording: mở micro Windows thật sự
```

Chữ `Async` cho biết hàm chứa công việc bất đồng bộ. Kiểu trả về `Task` cho phép nơi gọi dùng:

```csharp
await _vm.StartListeningAsync();
```

Khi đang chờ kết nối hoặc gửi message WebSocket, UI thread được trả lại cho WPF, vì vậy cửa sổ không bị treo.

## 2. Tại sao phải kết nối trước khi mở micro?

Đầu hàm có:

```csharp
if (!await EnsureConnectedAsync())
    return;
```

Nếu đã có WebSocket thì hàm tiếp tục ngay. Nếu chưa có, `EnsureConnectedAsync` đọc config, gọi OTA discovery, tạo client, gắn event và kết nối. Khi thất bại, `return` dừng lượt nghe; nếu vẫn mở micro thì audio thu được cũng không thể gửi đi.

## 3. `_isListening`, `IsRecording` và micro khác nhau thế nào?

- `_isListening` là cờ nội bộ. `OnAudioCaptured` chỉ xử lý buffer khi cờ này là `true`.
- `IsRecording` là property công khai dùng để báo trạng thái cho giao diện.
- `_audioService.StartRecording()` mới là lệnh thật sự mở thiết bị micro.

Vì vậy `IsRecording = true` không tự mở micro. Nó phát `PropertyChanged("IsRecording")` để UI đổi nút và animation.

## 4. Audio đi đâu sau khi micro mở?

```text
Windows/NAudio thu được buffer
  -> NAudioAudioService.OnAudioRecorded
  -> MainViewModel.OnAudioCaptured
  -> VoiceActivityDetector.ProcessPcm
  -> đổi byte[] PCM thành short[]
  -> OpusCodec.Encode
  -> XiaozhiWebSocketClient.SendAudioAsync
  -> server
```

Khi VAD phát hiện im lặng đủ lâu, event `OnSpeechEnded` gọi `StopListeningAsync`. Hàm stop chặn buffer mới, đóng micro, cập nhật UI và gửi message kết thúc câu. Server sau đó thực hiện STT, LLM và TTS.

## 5. `OnPropertyChanged` dùng như thế nào?

`MainViewModel` triển khai `INotifyPropertyChanged` và khai báo:

```csharp
public event PropertyChangedEventHandler? PropertyChanged;
```

Mỗi property có một backing field và gọi thông báo trong setter:

```csharp
public bool IsRecording
{
    get => _isRecording;
    set
    {
        _isRecording = value;
        OnPropertyChanged();
    }
}
```

`OnPropertyChanged`:

```csharp
protected void OnPropertyChanged([CallerMemberName] string? name = null)
{
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

`[CallerMemberName]` làm compiler tự điền tên nơi gọi. Trong setter `IsRecording`, lời gọi `OnPropertyChanged()` tương đương với `OnPropertyChanged("IsRecording")`.

Dấu `?.` chỉ gọi event khi có subscriber. Nếu chưa có ai đăng ký, chương trình bỏ qua thay vì phát sinh `NullReferenceException`.

## 6. Ai nhận event này?

Trong constructor `MainWindow`:

```csharp
_vm.PropertyChanged += Vm_PropertyChanged;
```

Khi ViewModel phát event, `Vm_PropertyChanged` đọc `e.PropertyName` và cập nhật control tương ứng:

```text
IsRecording = true
  -> setter lưu _isRecording
  -> OnPropertyChanged()
  -> PropertyChanged("IsRecording")
  -> MainWindow.Vm_PropertyChanged
  -> UpdateRecordingUI(true)
  -> nút micro đổi đỏ và vòng sáng bắt đầu chạy
```

`Dispatcher.Invoke` được dùng vì event có thể bắt nguồn từ timer, audio hoặc mạng chạy trên thread nền; WPF chỉ cho phép sửa control trên UI thread.

## 7. Luồng đầy đủ khi người dùng bấm micro

```text
TalkBtn_MouseDown
  -> MainViewModel.StartListeningAsync
  -> EnsureConnectedAsync
  -> VAD reset
  -> PropertyChanged cập nhật UI
  -> server nhận listen/start
  -> NAudio mở micro
  -> audio được nén Opus và gửi liên tục
  -> VAD phát hiện người dùng nói xong
  -> StopListeningAsync
  -> server nhận listen/stop
  -> server trả STT
  -> server trả nội dung LLM
  -> server gửi audio TTS
  -> client giải mã và phát loa
```

## 8. Ghi chú về kiến trúc hiện tại

Dự án đang dùng kiểu MVVM lai. ViewModel phát `PropertyChanged`, nhưng `MainWindow` tự bắt event và cập nhật control. WPF thuần thường binding trực tiếp, ví dụ:

```xml
<TextBlock Text="{Binding StatusText}" />
```

Khi binding như vậy, WPF tự nghe `PropertyChanged`; không cần case thủ công cho `StatusText`. Tuy nhiên việc đổi sang binding hoàn toàn là một đợt refactor riêng, không cần thiết để hiểu hoặc chạy code hiện tại.

## 9. Bảng rà soát toàn bộ function của bản WPF `.exe`

Phạm vi bảng này gồm mọi mã nguồn tham gia bản Windows: `App.Wpf`, `Core`, `Audio`, `Protocols`, `Plugins` và `Mcp`. `App.Maui` không thuộc file WPF `.exe`.

### WPF

- `MainWindow`: tạo UI, nối ViewModel, hotkey và vòng đời cửa sổ.
- `Vm_PropertyChanged`: ánh xạ property thay đổi sang control WPF.
- `UpdateHandsFreeUI`, `UpdateRecordingUI`: đồng bộ màu, nhãn và animation.
- `OnMessageAdded`: dựng bong bóng chat động.
- `TalkBtn_MouseDown`, `TalkBtn_MouseUp`, `TalkBtn_MouseLeave`: điều khiển thu âm bằng click/nhấn giữ.
- `SendText_Click`, `TxtInput_PreviewKeyDown`, `TxtInput_KeyDown`, `SendCurrentText`: pipeline gửi ô nhập.
- `AbortBtn_Click`, `OpenSettings_Click`, `RefreshSync_Click`, `SimulateIPhone_Click`, `HandsFree_Click`: các thao tác phụ của cửa sổ chính.
- `MainViewModel`, `OnAudioCaptured`, `InitializeAsync`, `EnsureConnectedAsync`, `ReconnectAsync`, `WireEvents`, `StartListeningAsync`, `StopListeningAsync`, `SendTextMessageAsync`, `AbortAsync`, `OnPropertyChanged`: toàn bộ nghiệp vụ giao diện, mạng và audio.
- `ActivationWindow`, `StartActivationFlowAsync`, `RenderQrCode`, `Close_Click`: quy trình activation.
- `SettingsWindow`, `LoadSettings`, `GetTokenQr_Click`, `Save_Click`, `Cancel_Click`: quy trình cấu hình.

### Core

- `ConfigManager`, `LoadOrCreate`, `LoadOrCreateEfuse`, `CreateDefault`, `CreateDefaultEfuse`, `SaveConfig`, `SaveEfuse`, `GetMacAddress`: vòng đời cấu hình và efuse.
- `DeviceFingerprint.GetMacAddress`, `GenerateDeviceId`, `GenerateSerialNumber`, `GenerateHmacKey`: danh tính thiết bị.
- `Constants`, `Interfaces` và `Models` không có function; chúng định nghĩa trạng thái, hợp đồng và cấu trúc dữ liệu.

### Audio

- `OpusCodec`, `Encode`, `Decode24k`, `Dispose`: codec Opus hai chiều.
- `NAudioAudioService`, `InitializePlayback`, `StartRecording`, `ResampleToPcm16k`, `StopRecording`, `PlayAudio`, `StopPlayback`, `SetVolume`, `Dispose`: micro và loa.
- `TextToAudioStreamer.StreamTextAsAudioAsync`, `FetchVietnameseTtsPcmAsync`, `SplitTextIntoChunks`, `ConvertMp3ToPcm16k`: biến câu text dài thành lượt nói audio.
- `VoiceActivityDetector.Reset`, `ProcessPcm`: phát hiện giọng nói và im lặng.
- `WakeWordDetector`, `InitializeModel`, `ProcessAudio`, `Dispose`: model từ đánh thức SherpaOnnx.

### Protocols

- `DeviceActivationService`, `CheckOrRequestActivationAsync`, `PollForTokenAsync`, `GetLocalIpAddress`: OTA và activation.
- `XiaozhiWebSocketClient.Log`, constructor, `ConnectAsync`, `SendHelloHandshakeAsync`: khởi tạo phiên WebSocket.
- `StartListeningAsync`, `StopListeningAsync`, `SendTextQueryAsync`, `SendAbortAsync`, `SendAudioAsync`: API nghiệp vụ gửi lên server.
- `SendTextAsync`, `SendJsonAsync`: serialize và gửi frame ở mức thấp.
- `ReceiveLoopAsync`, `HandleServerJsonMessageAsync`: nhận frame và phân loại hello/STT/LLM/TTS/goodbye.
- `CloseInternalAsync`, `DisconnectAsync`, `DisposeAsync`: kết thúc phiên và nhả tài nguyên.

### Plugins và MCP

- `GlobalShortcutPlugin.RegisterWindow`, `HwndHook`, `InitializeAsync`, `ShutdownAsync`: hotkey native Windows.
- `PluginManager.RegisterPlugin`, `InitializeAllAsync`, `ShutdownAllAsync`: vòng đời plugin.
- `SystemAppTool.ListRunningApps`, `FindInstalledApps`, `LaunchApp`, `KillApp`, `GetSystemStatus`: thao tác ứng dụng và trạng thái Windows.

Các file `Class1.cs` là placeholder rỗng do template tạo, không có function và không tham gia luồng chạy.
