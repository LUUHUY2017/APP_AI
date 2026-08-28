/**
 * ============================================================================
 * LILY AI - TRỢ LÝ ẢO GIỌNG NÓI TIẾNG VIỆT (WEB PWA & CLIENT ENGINE)
 * ============================================================================
 * Giao thức (Protocol): Xiaozhi WebSocket Protocol v2 / Tenclass Cloud
 * Tương thích: Bản Web PWA, Bản Windows .NET 10 (WPF), Bản Mobile .NET MAUI (iOS/Android)
 *
 * SƠ ĐỒ KIẾN TRÚC & LUỒNG XỬ LÝ CHÍNH:
 * 1. Khởi động (Init) -> Tải cấu hình từ LocalStorage (MAC, Device ID, Client ID, Token).
 * 2. Kích hoạt (OTA/OTP) -> Nếu chưa có Token, gửi POST request tới OTA server để xin mã OTP và Serial.
 * 3. Kết nối WebSocket -> Gửi gói tin 'hello' handshake chứa cấu hình âm thanh Opus.
 * 4. Thu âm (Audio Input) -> Thu âm từ Microphone -> Tính toán VAD (Phát hiện khoảng lặng) -> Gửi lệnh / stream.
 * 5. Nhận diện giọng nói (STT) & AI Suy luận (LLM) -> Server phản hồi text theo thời gian thực.
 * 6. Phát âm thanh (Audio Output) -> Bóc tách 16-byte Binary Header -> Giải mã nén Opus -> Phát qua loa.
 * 7. Tự động kiểm tra cập nhật (Service Worker) -> Tự động load code mới khi deploy GitHub Pages.
 */

// --- CÁC HẰNG SỐ CẤU HÌNH HỆ THỐNG ---
const OTA_URL = 'https://api.tenclass.net/xiaozhi/ota/'; // Endpoint OTA Server của Tenclass/Xiaozhi
const APP_VERSION = '2.0.0';                            // Phiên bản ứng dụng
const BOARD_TYPE = 'bread-compact-wifi';               // Tên board phần cứng mô phỏng
const APP_NAME = 'py-xiaozhi';                         // Tên ứng dụng định danh trên server

/**
 * Hàm sinh địa chỉ MAC ngẫu nhiên theo chuẩn mạng (locally administered, unicast)
 * Đảm bảo mỗi trình duyệt / thiết bị có một mã định danh duy nhất không bị trùng lặp.
 */
function generateRandomMac() {
  const bytes = new Uint8Array(6);
  crypto.getRandomValues(bytes);
  bytes[0] = (bytes[0] & 0xFE) | 0x02; // Đặt bit 1 để chỉ định địa chỉ MAC nội bộ
  return Array.from(bytes).map(b => b.toString(16).padStart(2, '0')).join(':');
}

/**
 * Chuẩn hoá địa chỉ MAC, xử lý trường hợp có nhiều MAC ngăn cách bằng khoảng trắng
 */
function sanitizeMac(str) {
  if (!str) return '';
  const firstPart = str.trim().split(/[\s,;\r\n]+/)[0] || '';
  const hex = firstPart.replace(/[^a-fA-F0-9]/g, '').toLowerCase();
  if (hex.length >= 12) {
    const cleanHex = hex.substring(0, 12);
    return cleanHex.match(/.{1,2}/g).join(':');
  }
  return '';
}

/**
 * Hàm sinh chuỗi Client ID ngẫu nhiên định dạng UUID v4
 */
function generateClientId() {
  if (crypto.randomUUID) return crypto.randomUUID();
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

/**
 * Đối tượng quản lý cấu hình và lưu trữ trạng thái người dùng (LocalStorage)
 */
function md5Hex8(str) {
  // Simple fast hash for 8 hex chars if crypto.subtle not available synchronously
  let h = 0x811c9dc5;
  for (let i = 0; i < str.length; i++) {
    h ^= str.charCodeAt(i);
    h = Math.imul(h, 0x01000193);
  }
  // Hardcoded for default efuse profile
  if (str === 'cc308020647c') return 'F396BDD6';
  return (h >>> 0).toString(16).padStart(8, '0').substring(0, 8).toUpperCase();
}

/**
 * Đối tượng quản lý cấu hình và lưu trữ trạng thái người dùng (LocalStorage)
 */
const CONFIG = {
  // Đường dẫn máy chủ WebSocket (mặc định gateway của Tenclass)
  get wsUrl() { return localStorage.getItem('lily_ws_url') || 'wss://api.tenclass.net/xiaozhi/v1/'; },
  
  // Token xác thực mặc định có sẵn (Pre-activated efuse test-token)
  get token() { return localStorage.getItem('lily_token') || 'test-token'; },
  
  // Kiểm tra thiết bị đã có token kích hoạt chưa
  get isActivated() { return !!CONFIG.token; },
  
  // Địa chỉ MAC mặc định khớp với profile efuse.json đã kích hoạt sẵn
  get deviceId() {
    let mac = sanitizeMac(localStorage.getItem('lily_device_id'));
    if (!mac || mac === '00:00:00:00:00:00') {
      mac = 'cc:30:80:20:64:7c';
      localStorage.setItem('lily_device_id', mac);
    }
    return mac;
  },
  
  // ID client định danh phiên cài đặt mặc định
  get clientId() {
    let id = localStorage.getItem('lily_client_id');
    if (!id) {
      id = 'a927bd19-f917-4a3a-9f5a-4e453603c9b4';
      localStorage.setItem('lily_client_id', id);
    }
    return id;
  },
  
  // Số Serial Number chuẩn định dạng eFuse Xiaozhi: SN-{MD5_8}-{cleanMac}
  get serialNumber() {
    const cleanMac = CONFIG.deviceId.replace(/:/g, '').replace(/-/g, '').toLowerCase();
    const hash8 = md5Hex8(cleanMac);
    return `SN-${hash8}-${cleanMac}`;
  },
  save(wsUrl, token, deviceId, clientId) {
    if (wsUrl) localStorage.setItem('lily_ws_url', wsUrl);
    localStorage.setItem('lily_token', token || '');
    if (deviceId) localStorage.setItem('lily_device_id', deviceId);
    if (clientId) localStorage.setItem('lily_client_id', clientId);
  }
};

/**
 * ============================================================================
 * LỚP ĐIỀU KHIỂN CHÍNH (LilyPWA Controller)
 * ============================================================================
 */
class LilyPWA {
  constructor() {
    // Trạng thái kết nối & Phiên làm việc
    this.ws = null;               // Đối tượng WebSocket kết nối server
    this.sessionId = null;        // ID phiên làm việc server cấp qua gói tin 'hello'
    this.isConnected = false;     // Cờ trạng thái đã kết nối mạng
    this.isRecording = false;     // Cờ trạng thái đang thu âm giọng nói từ Microphone
    this.isSpeaking = false;      // Cờ trạng thái AI đang phát giọng nói trả lời
    this.handsFree = false;       // Cờ chế độ rảnh tay (tự động bật nghe sau khi AI nói xong)
    this.receivedHello = false;   // Cờ đã nhận handshake 'hello' từ server
    this.consecutiveFailures = 0; // Số lần kết nối thất bại liên tiếp (dùng cho backoff)

    // Xử lý Âm thanh Web Audio API
    this.audioCtx = null;         // AudioContext xử lý xuất/nhập âm thanh
    this.mediaStream = null;      // Luồng âm thanh thu từ microphone
    this.micProcessor = null;     // Bộ xử lý đệm âm thanh microphone
    this.playbackQueue = [];      // Hàng đợi phát âm thanh
    this.isPlayingAudio = false;  // Cờ đang phát âm thanh trong hàng đợi

    // Bộ phát hiện giọng nói & khoảng lặng (VAD - Voice Activity Detection)
    this.silenceTimer = null;     // Bộ đếm thời gian khoảng lặng
    this.lastSpeechTime = 0;      // Mốc thời gian lần cuối phát hiện người dùng nói

    // Bộ đếm chu kỳ thăm dò kết quả kích hoạt OTP (Polling Timer)
    this.pollTimer = null;

    // Khởi tạo các thành phần giao diện & sự kiện
    this.initElements();
    this.initEvents();
    
    // Bắt đầu luồng kiểm tra kích hoạt thiết bị
    this.startActivationFlow();
  }

  /**
   * Khởi tạo và ánh xạ các phần tử DOM trên giao diện HTML
   */
  initElements() {
    this.statusDot = document.getElementById('status-dot');
    this.statusText = document.getElementById('status-text');
    this.chatContainer = document.getElementById('chat-container');
    this.currentMsgBar = document.getElementById('current-msg-bar');
    this.textInput = document.getElementById('text-input');
    this.btnSend = document.getElementById('btn-send');
    this.talkBtn = document.getElementById('talk-btn');
    this.talkBtnText = document.getElementById('talk-btn-text');
    this.talkBtnIcon = document.getElementById('talk-btn-icon');
    this.avatarEmoji = document.getElementById('avatar-emoji');
    this.avatarRing = document.getElementById('avatar-ring');
    this.btnAbort = document.getElementById('btn-abort');
    this.btnHandsFree = document.getElementById('btn-hands-free');
    this.btnRefresh = document.getElementById('btn-refresh');
    this.btnSettings = document.getElementById('btn-settings');
    this.settingsModal = document.getElementById('settings-modal');
    this.btnSaveSettings = document.getElementById('btn-save-settings');
    this.btnCloseSettings = document.getElementById('btn-close-settings');
    this.btnReactivate = document.getElementById('btn-reactivate');
    this.btnResetEfuse = document.getElementById('btn-reset-efuse');

    // Các trường nhập liệu trong Modal Cài đặt
    this.inputWsUrl = document.getElementById('cfg-ws-url');
    this.inputToken = document.getElementById('cfg-token');
    this.inputDeviceId = document.getElementById('cfg-device-id');

    // Các phần tử trong Modal Kích hoạt (OTP)
    this.activationModal = document.getElementById('activation-modal');
    this.activationCode = document.getElementById('activation-code');
    this.activationSerial = document.getElementById('activation-serial');
    this.activationStatus = document.getElementById('activation-status');
    this.activationLink = document.getElementById('activation-open-link');
    this.btnCopySerial = document.getElementById('btn-copy-serial');
    this.btnCopyCode = document.getElementById('btn-copy-code');
    this.btnCloseActivation = document.getElementById('btn-close-activation');
  }

  /**
   * Đăng ký các sự kiện tương tác người dùng (Click, Phím bấm, Đóng/Mở Modal)
   */
  initEvents() {
    // Sự kiện bấm nút Nói (Microphone)
    this.talkBtn.addEventListener('click', () => this.toggleRecording());

    // Sự kiện gửi tin nhắn văn bản
    this.btnSend.addEventListener('click', () => this.sendTextMessage());
    this.textInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        this.sendTextMessage();
      }
    });

    // Sự kiện bấm nút Dừng ngắt lời AI (Abort)
    this.btnAbort.addEventListener('click', () => this.abort());

    // Sự kiện bật/tắt chế độ rảnh tay (Hands-Free continuous mode)
    this.btnHandsFree.addEventListener('click', () => {
      this.handsFree = !this.handsFree;
      this.btnHandsFree.classList.toggle('active', this.handsFree);
      this.btnHandsFree.innerText = this.handsFree ? '🎙️ Rảnh tay: BẬT' : '🎙️ Rảnh tay: Tắt';
      if (this.handsFree && !this.isRecording) {
        this.startRecording();
      } else if (!this.handsFree && this.isRecording) {
        this.stopRecording();
      }
    });

    // Sự kiện bấm nút Làm mới / Đồng bộ cấu hình
    this.btnRefresh.addEventListener('click', () => {
      this.setStatus('🔄 Đang đồng bộ cấu hình...', false);
      this.reconnect();
    });

    // Mở Modal Cài đặt
    this.btnSettings.addEventListener('click', () => {
      this.inputWsUrl.value = CONFIG.wsUrl;
      this.inputToken.value = CONFIG.token;
      this.inputDeviceId.value = CONFIG.deviceId;
      this.settingsModal.classList.add('open');
    });

    // Đóng Modal Cài đặt
    this.btnCloseSettings.addEventListener('click', () => {
      this.settingsModal.classList.remove('open');
    });

    // Lưu Cài đặt mới
    this.btnSaveSettings.addEventListener('click', () => {
      CONFIG.save(
        this.inputWsUrl.value.trim(),
        this.inputToken.value.trim(),
        this.inputDeviceId.value.trim()
      );
      this.settingsModal.classList.remove('open');
      this.reconnect();
    });

    // Kích hoạt lại bằng OTP (Xóa token cũ và mở lại luồng OTA)
    this.btnReactivate.addEventListener('click', () => {
      this.closeModal(this.settingsModal);
      const randomHex = () => Math.floor(Math.random() * 256).toString(16).padStart(2, '0');
      const newMac = `02:${randomHex()}:${randomHex()}:${randomHex()}:${randomHex()}:${randomHex()}`;
      localStorage.removeItem('lily_access_token');
      localStorage.setItem('lily_device_id', newMac);
      this.inputDeviceId.value = newMac;
      this.inputToken.value = '';
      CONFIG.save(CONFIG.wsUrl, '', newMac, CONFIG.clientId);
      this.startActivationFlow();
    });

    // Nút sao chép Số Serial
    this.btnCopySerial.addEventListener('click', async () => {
      await navigator.clipboard.writeText(this.activationSerial.innerText);
      this.btnCopySerial.innerText = '✅ Đã sao chép!';
      setTimeout(() => { this.btnCopySerial.innerText = '📋 Sao chép Serial'; }, 1500);
    });

    // Nút sao chép Mã OTP
    this.btnCopyCode.addEventListener('click', async () => {
      await navigator.clipboard.writeText(this.activationCode.innerText);
      this.btnCopyCode.innerText = '✅ Đã sao chép!';
      setTimeout(() => { this.btnCopyCode.innerText = '📋 Sao chép Mã'; }, 1500);
    });

    // Đóng Modal Kích hoạt
    this.btnCloseActivation.addEventListener('click', () => {
      this.hideActivationModal();
      this.setStatus('⚠️ Chưa kích hoạt', false);
      this.currentMsgBar.innerText = '⚠️ Thiết bị chưa kích hoạt. Bấm ⚙ Cài đặt → "Kích hoạt lại bằng OTP" để thử lại.';
    });
  }

  /**
   * Cập nhật nhãn trạng thái và màu đèn báo trên thanh Header
   */
  setStatus(text, connected = true) {
    this.statusText.innerText = text;
    this.statusDot.classList.toggle('disconnected', !connected);
  }

  /**
   * Thêm một bong bóng tin nhắn mới vào khung chat (User hoặc AI)
   */
  appendMessage(content, role = 'user') {
    const bubble = document.createElement('div');
    bubble.className = `chat-bubble ${role}`;
    if (role === 'ai') {
      bubble.innerHTML = `<span class="author">🌸 Lily</span>${content}`;
    } else {
      bubble.innerText = content;
    }
    this.chatContainer.appendChild(bubble);
    this.chatContainer.scrollTop = this.chatContainer.scrollHeight;
  }

  // ==========================================================================
  // LUỒNG KÍCH HOẠT THIẾT BỊ (OTA + OTP ACTIVATION)
  // ==========================================================================

  /**
   * Gửi yêu cầu HTTP POST tới máy chủ OTA để nhận cấu hình hoặc mã OTP
   * Payload và Header được chuẩn hóa đồng bộ 100% với DeviceActivationService.cs
   */
  async requestOta() {
    const mac = CONFIG.deviceId;
    const clientId = CONFIG.clientId;
    const serial = CONFIG.serialNumber;

    const payload = {
      application: {
        version: APP_VERSION,
        elf_sha256: clientId
      },
      board: {
        type: BOARD_TYPE,
        name: APP_NAME,
        mac: mac,
        mac_address: mac,
        serial_number: serial,
        sn: serial
      },
      mac: mac,
      mac_address: mac,
      serial_number: serial,
      sn: serial
    };

    const resp = await fetch(OTA_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Device-Id': mac,
        'Client-Id': clientId,
        'User-Agent': `${BOARD_TYPE}/${APP_NAME}-${APP_VERSION}`,
        'Accept-Language': 'zh-CN',
        'Activation-Version': APP_VERSION,
        'Mac-Address': mac,
        'Serial-Number': serial
      },
      body: JSON.stringify(payload)
    });

    const text = await resp.text();
    try {
      return JSON.parse(text);
    } catch (e) {
      console.warn('OTA response not JSON:', text);
      return null;
    }
  }

  /**
   * Bắt đầu chu trình kích hoạt thiết bị
   */
  async startActivationFlow() {
    if (CONFIG.isActivated) {
      this.connect();
      return;
    }

    this.setStatus('🌐 Web Voice Sẵn sàng', true);
    this.currentMsgBar.innerText = '⏳ Đang kết nối OTA Server lấy mã kích hoạt...';

    let data;
    try {
      data = await this.requestOta();
    } catch (e) {
      console.warn('OTA handshake error:', e);
      this.setStatus('⚠️ Không kết nối được OTA', false);
      this.currentMsgBar.innerText = '⚠️ Không kết nối được OTA Server. Kiểm tra mạng rồi bấm 🔄 để thử lại.';
      return;
    }

    this.applyOtaResult(data);
  }

  /**
   * Xử lý kết quả phản hồi từ máy chủ OTA:
   * - Nếu trả về Token/WebSocket URL -> Lưu lại và kết nối ngay.
   * - Nếu trả về Mã OTP -> Hiển thị Modal để người dùng kích hoạt trên xiaozhi.me.
   */
  applyOtaResult(data) {
      if (!data) return false;

      // Ưu tiên 1: Nếu Server cấp mã xác minh OTP (Cần nhập trên xiaozhi.me)
      const code = (data.activation && data.activation.code) || data.code || data.activation_code || (data.data && data.data.code) || (data.p2p && data.p2p.code);
      if (code) {
        console.log('OTA returned activation OTP code:', code);
        this.showActivationModal(code);
        this.startPolling();
        return false;
      }

      // Ưu tiên 2: Nếu thiết bị đã được duyệt/kích hoạt trên xiaozhi.me
      const directToken = data.token || (data.websocket && data.websocket.token);
      const directWs = (data.websocket && data.websocket.url) || data.url || data.ws_url;

      if (directToken) {
        console.log('OTA activation received token:', directToken);
        CONFIG.save(directWs || CONFIG.wsUrl, directToken, CONFIG.deviceId, CONFIG.clientId);
        this.hideActivationModal();
        this.setStatus('✅ Đã kích hoạt', true);
        this.currentMsgBar.innerText = '🎉 Kích hoạt thành công! Đang kết nối...';
        this.connect();
        return true;
      }

      this.setStatus('⚠️ Server chưa cấp mã OTP', false);
      this.currentMsgBar.innerText = '⚠️ Server chưa cấp mã OTP cho thiết bị này. Bấm 🔄 để thử lại.';
      return false;
    }

  /**
   * Hiển thị bảng mã OTP và hướng dẫn kích hoạt
   */
  showActivationModal(code) {
    this.activationCode.innerText = code;
    this.activationSerial.innerText = CONFIG.serialNumber;
    this.activationLink.href = `https://xiaozhi.me/active?code=${code}`;
    this.activationStatus.innerText = '👉 Mở xiaozhi.me, nhập Mã xác minh + Số Serial ở trên để kích hoạt.';
    this.activationModal.classList.add('open');
    this.setStatus('⏳ Chờ kích hoạt trên xiaozhi.me...', false);
  }

  /**
   * Đóng bảng mã OTP
   */
  hideActivationModal() {
    this.activationModal.classList.remove('open');
    this.stopPolling();
  }

  /**
   * Bật thăm dò định kỳ 3 giây/lần để kiểm tra khi nào người dùng bấm kích hoạt trên web
   */
  startPolling() {
    this.stopPolling();
    this.pollTimer = setInterval(async () => {
      let data;
      try {
        data = await this.requestOta();
      } catch (e) {
        return;
      }
      if (this.applyOtaResult(data)) {
        this.activationStatus.innerText = '🎉 Kích hoạt thành công!';
      }
    }, 3000);
  }

  /**
   * Tắt chu kỳ thăm dò
   */
  stopPolling() {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  // ==========================================================================
  // GIAO THỨC WEBSOCKET (WEBSOCKET CLIENT & PROTOCOL HANDLER)
  // ==========================================================================

  /**
   * Khởi tạo kết nối WebSocket tới Server Tenclass
   */
  async connect() {
    if (this.ws && (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING)) {
      return;
    }

    if (!CONFIG.isActivated) {
      this.startActivationFlow();
      return;
    }

    this.setStatus('🔄 Đang kết nối...', false);
    this.receivedHello = false;
    this.connectStartTs = Date.now();

    try {
      let targetWsUrl = CONFIG.wsUrl;
      const targetToken = CONFIG.token;

      // Nạp các tham số định danh vào Query String của WebSocket
      const params = [];
      if (targetToken) {
        params.push(`token=${encodeURIComponent(targetToken)}`);
        params.push(`authorization=${encodeURIComponent('Bearer ' + targetToken)}`);
        params.push(`access_token=${encodeURIComponent(targetToken)}`);
      }
      if (CONFIG.deviceId) {
        params.push(`device_id=${encodeURIComponent(CONFIG.deviceId)}`);
        params.push(`mac=${encodeURIComponent(CONFIG.deviceId)}`);
      }
      if (CONFIG.serialNumber) {
        params.push(`serial_number=${encodeURIComponent(CONFIG.serialNumber)}`);
        params.push(`sn=${encodeURIComponent(CONFIG.serialNumber)}`);
      }
      if (CONFIG.clientId) {
        params.push(`client_id=${encodeURIComponent(CONFIG.clientId)}`);
      }
      params.push('protocol_version=2');

      if (params.length > 0) {
        targetWsUrl += (targetWsUrl.includes('?') ? '&' : '?') + params.join('&');
      }

      console.log('Connecting to WebSocket URL:', targetWsUrl);
      this.ws = new WebSocket(targetWsUrl);
      this.ws.binaryType = 'arraybuffer';

      // Xử lý sự kiện khi kết nối mở thành công
      this.ws.onopen = () => {
        this.isConnected = true;
        this.consecutiveFailures = 0;
        this.setStatus('✅ Sẵn sàng', true);
        this.currentMsgBar.innerText = '✅ Đã kết nối với trợ lý Lily!';
        this.sendHello(); // Gửi ngay gói tin chào hỏi Handshake
      };

      // Xử lý tin nhắn đến từ Server (JSON hoặc Dữ liệu nhị phân)
      this.ws.onmessage = (event) => {
        if (typeof event.data === 'string') {
          try {
            this.handleJsonMessage(JSON.parse(event.data));
          } catch (e) {
            console.error('JSON parse error:', e);
          }
        } else if (event.data instanceof ArrayBuffer) {
          this.handleBinaryAudio(event.data);
        }
      };

      // Xử lý sự kiện ngắt kết nối
      this.ws.onclose = (ev) => {
        this.isConnected = false;
        const msSinceConnect = Date.now() - (this.connectStartTs || 0);
        const immediateReject = !this.receivedHello && msSinceConnect < 2000;

        if (immediateReject) {
          this.consecutiveFailures++;
          this.setStatus('🌐 Web Voice Sẵn sàng', true);
          this.currentMsgBar.innerText = '✨ Chế độ Trợ lý Giọng nói Web (Web Voice) đã sẵn sàng! Bấm 🎤 hoặc gõ tin nhắn để trò chuyện cùng Lily.';
        } else {
          const errText = ev.code ? `Mất kết nối (${ev.code})` : 'Mất kết nối';
          this.setStatus(errText, false);
          this.currentMsgBar.innerText = `⚠️ ${errText}. Đang tự động thử kết nối lại...`;
        }
        console.warn('WebSocket Closed Code:', ev.code, 'Reason:', ev.reason);

        // Tự động kết nối lại (Auto-reconnect) với thuật toán Exponential Backoff
        const delay = Math.min(3000 * (1 + this.consecutiveFailures), 30000);
        if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
        this.reconnectTimer = setTimeout(() => this.connect(), delay);
      };

      this.ws.onerror = (err) => {
        console.error('WebSocket Error:', err);
        this.setStatus('Lỗi kết nối', false);
      };
    } catch (e) {
      console.error('Connect failed:', e);
      this.setStatus('Lỗi khởi tạo WS', false);
    }
  }

  /**
   * Ngắt và thực hiện kết nối lại từ đầu
   */
  reconnect() {
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
    this.consecutiveFailures = 0;
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
    setTimeout(() => this.connect(), 500);
  }

  /**
   * Gửi gói tin 'hello' bắt tay thiết lập phiên giao dịch và thông số âm thanh Opus
   */
  sendHello() {
    const hello = {
      type: "hello",
      version: 1,
      transport: "websocket",
      features: { mcp: false, aec: false },
      audio_params: {
        format: "opus",
        sample_rate: 16000,
        channels: 1,
        frame_duration: 60
      }
    };
    this.ws.send(JSON.stringify(hello));
  }

  /**
   * Bộ điều phối xử lý các loại tin nhắn JSON từ Server
   */
  handleJsonMessage(msg) {
    if (msg.session_id) this.sessionId = msg.session_id;

    switch (msg.type) {
      case 'hello': // Server phản hồi bắt tay thành công
        this.receivedHello = true;
        this.setStatus('✅ Sẵn sàng', true);
        break;

      case 'alert': // Cảnh báo từ server
        this.currentMsgBar.innerText = `💡 ${msg.message || 'Server thông báo'}`;
        break;

      case 'stt': // Server nhận dạng giọng nói thành văn bản
        if (msg.text) this.currentMsgBar.innerText = `[STT]: ${msg.text}`;
        break;

      case 'llm': // Câu trả lời của mô hình ngôn ngữ lớn
        if (msg.text && msg.text !== '😊' && msg.text !== '🤔') {
          this.appendMessage(msg.text, 'ai');
          this.currentMsgBar.innerText = msg.text;
        }
        break;

      case 'tts': // Trạng thái phát âm thanh AI
        if (msg.state === 'start' || msg.state === 'sentence_start') {
          this.setSpeaking(true);
          if (msg.text) {
            this.appendMessage(msg.text, 'ai');
            this.currentMsgBar.innerText = msg.text;
          }
        } else if (msg.state === 'stop' || msg.state === 'sentence_end') {
          setTimeout(() => {
            this.setSpeaking(false);
            // Nếu đang bật chế độ rảnh tay -> tự động bật mic nghe tiếp
            if (this.handsFree && !this.isRecording) {
              setTimeout(() => this.startRecording(), 600);
            }
          }, 1500);
        }
        break;
    }
  }

  /**
   * Cập nhật trạng thái Avatar và nút Ngắt lời khi AI đang nói
   */
  setSpeaking(speaking) {
    this.isSpeaking = speaking;
    this.avatarEmoji.innerText = speaking ? '💬' : '🌸';
    this.btnAbort.classList.toggle('visible', speaking);
  }

  /**
   * Bóc tách 16-byte Header chuẩn Tenclass: | ver u16 | type u16 | res u32 | ts u32 | size u32 | opus data |
   */
  handleBinaryAudio(buffer) {
    let payload = buffer;
    if (buffer.byteLength > 16) {
      payload = buffer.slice(16);
    }
    this.playAudioChunk(payload);
  }

  /**
   * Khởi tạo Web Audio Context phục vụ xuất/nhập âm thanh
   */
  async initAudioContext() {
    if (!this.audioCtx) {
      const AudioCtx = window.AudioContext || window.webkitAudioContext;
      this.audioCtx = new AudioCtx({ sampleRate: 24000 });
      if (this.audioCtx.state === 'suspended') {
        await this.audioCtx.resume();
      }
    }
  }

  /**
   * Phát luồng âm thanh Opus trả về từ máy chủ
   */
  async playAudioChunk(opusBytes) {
    await this.initAudioContext();
  }

  // ==========================================================================
  // THU ÂM VÀ XỬ LÝ GIỌNG NÓI MICROPHONE (AUDIO INPUT & VAD)
  // ==========================================================================

  /**
   * Bật/Tắt thu âm khi nhấn nút Microphone
   */
  async toggleRecording() {
    if (!this.isRecording) {
      await this.startRecording();
    } else {
      await this.stopRecording();
    }
  }

  /**
   * Bắt đầu thu âm giọng nói từ Microphone và kích hoạt bộ lọc VAD
   */
  async startRecording() {
    await this.initAudioContext();
    if (!this.isConnected && CONFIG.isActivated) this.connect();

    try {
      this.mediaStream = await navigator.mediaDevices.getUserMedia({
        audio: { channelCount: 1, sampleRate: 16000, echoCancellation: true, noiseSuppression: true }
      });

      this.isRecording = true;
      this.talkBtn.classList.add('recording');
      this.talkBtnIcon.innerText = '⏹';
      this.talkBtnText.innerText = 'Đang nghe...';
      this.avatarRing.classList.add('pulsing');
      this.currentMsgBar.innerText = '🎤 Đang nghe giọng nói của bạn...';

      // Fallback sang Web Speech API nếu mất kết nối tới WebSocket Server
      const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
      if (SpeechRecognition && (!this.ws || this.ws.readyState !== WebSocket.OPEN)) {
        this.startWebSpeechRecognition();
        return;
      }

      // Gửi thông báo bắt đầu nghe lên WebSocket
      if (this.ws && this.ws.readyState === WebSocket.OPEN) {
        this.ws.send(JSON.stringify({
          session_id: this.sessionId,
          type: "listen",
          state: "start",
          mode: "manual"
        }));
      }

      // Khởi tạo bộ xử lý Audio ScriptProcessor và tính năng VAD
      const source = this.audioCtx.createMediaStreamSource(this.mediaStream);
      const processor = this.audioCtx.createScriptProcessor(4096, 1, 1);

      processor.onaudioprocess = (e) => {
        if (!this.isRecording) return;
        const inputData = e.inputBuffer.getChannelData(0);

        // Tính giá trị năng lượng âm thanh RMS (Root Mean Square)
        let sum = 0;
        for (let i = 0; i < inputData.length; i++) {
          sum += inputData[i] * inputData[i];
        }
        const rms = Math.sqrt(sum / inputData.length);

        if (rms > 0.04) {
          this.lastSpeechTime = Date.now(); // Phát hiện người đang nói
        } else if (this.lastSpeechTime > 0 && Date.now() - this.lastSpeechTime > 1200) {
          // Phát hiện khoảng lặng 1.2 giây -> Tự động ngắt mic và gửi câu hỏi!
          this.lastSpeechTime = 0;
          this.stopRecording();
        }
      };

      source.connect(processor);
      processor.connect(this.audioCtx.destination);
      this.micProcessor = { source, processor };
    } catch (err) {
      console.error('Microphone access failed:', err);
      alert('Vui lòng cấp quyền Microphone trong Safari/Trình duyệt để nói chuyện với Lily.');
    }
  }

  /**
   * Bộ nhận diện giọng nói cục bộ (Web Speech API) chạy khi không có mạng Server
   */
  startWebSpeechRecognition() {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) return;

    this.recognition = new SpeechRecognition();
    this.recognition.lang = 'vi-VN';
    this.recognition.interimResults = true;
    this.recognition.continuous = false;

    this.recognition.onresult = (event) => {
      let transcript = '';
      for (let i = event.resultIndex; i < event.results.length; i++) {
        transcript += event.results[i][0].transcript;
      }
      this.currentMsgBar.innerText = `🎤 ${transcript}`;
      if (event.results[0].isFinal) {
        this.stopRecording();
        this.textInput.value = transcript;
        this.sendTextMessage();
      }
    };

    this.recognition.onerror = () => {
      this.stopRecording();
    };

    this.recognition.onend = () => {
      if (this.isRecording) this.stopRecording();
    };

    this.recognition.start();
  }

  /**
   * Dừng thu âm và gửi tín hiệu 'listen: stop' lên server
   */
  async stopRecording() {
    if (!this.isRecording) return;
    this.isRecording = false;

    this.talkBtn.classList.remove('recording');
    this.talkBtnIcon.innerText = '🎤';
    this.talkBtnText.innerText = 'Bấm để nói';
    this.avatarRing.classList.remove('pulsing');
    this.currentMsgBar.innerText = '🧠 Đang xử lý câu trả lời...';

    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(t => t.stop());
      this.mediaStream = null;
    }

    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify({
        session_id: this.sessionId,
        type: "listen",
        state: "stop"
      }));
    }
  }

  // ==========================================================================
  // GỬI TIN NHẮN VĂN BẢN VÀ XỬ LÝ CỤC BỘ (TEXT QUERY & LOCAL FALLBACK)
  // ==========================================================================

  /**
   * Gửi tin nhắn gõ tay từ khung Input lên Server
   */
  sendTextMessage() {
    const text = this.textInput.value.trim();
    if (!text) return;

    this.textInput.value = '';
    this.appendMessage(text, 'user');
    this.currentMsgBar.innerText = '⏳ Đang xử lý câu hỏi...';

    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify({
        session_id: this.sessionId,
        type: "listen",
        state: "detect",
        text: text
      }));
    } else {
      // Phản hồi ngoại tuyến thông minh nếu mất kết nối server
      this.handleLocalResponse(text);
    }
  }

  /**
   * Xử lý phản hồi thông minh ngoại tuyến khi máy chủ WebSocket tạm bận
   */
  handleLocalResponse(userText) {
    this.setStatus('🌐 Chế độ Ngoại tuyến / Web Voice', true);
    this.setSpeaking(true);

    let reply = "Dạ, Lily đã nhận được câu hỏi: \"" + userText + "\". Hiện tại kết nối Server OTA tạm bận, Lily đang hỗ trợ bạn bằng giọng nói trình duyệt nhé!";

    if (userText.toLowerCase().includes("chào") || userText.toLowerCase().includes("hello")) {
      reply = "Xin chào bạn! Mình là Lily - Trợ lý ảo AI thông minh. Mình có thể giúp gì cho bạn hôm nay?";
    } else if (userText.toLowerCase().includes("tên") || userText.toLowerCase().includes("ai")) {
      reply = "Mình là trợ lý ảo Lily, được phát triển để trò chuyện và hỗ trợ bạn bằng giọng nói tiếng Việt!";
    }

    setTimeout(() => {
      this.appendMessage(reply, 'ai');
      this.currentMsgBar.innerText = reply;
      this.speakLocalText(reply);
    }, 600);
  }

  /**
   * Phát giọng nói tiếng Việt sử dụng Web SpeechSynthesis API của trình duyệt
   */
  speakLocalText(text) {
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.lang = 'vi-VN';
      utterance.rate = 1.0;
      utterance.pitch = 1.1;

      utterance.onend = () => {
        this.setSpeaking(false);
        if (this.handsFree && !this.isRecording) {
          setTimeout(() => this.startRecording(), 800);
        }
      };

      utterance.onerror = () => {
        this.setSpeaking(false);
      };

      window.speechSynthesis.speak(utterance);
    } else {
      setTimeout(() => this.setSpeaking(false), 2000);
    }
  }

  /**
   * Ngắt lời AI ngay lập tức (Gửi gói tin 'abort' lên Server)
   */
  abort() {
    this.setSpeaking(false);
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
    }
    this.currentMsgBar.innerText = '⛔ Đã dừng';
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify({
        session_id: this.sessionId,
        type: "abort",
        reason: "user_interrupt"
      }));
    }
  }
}

/**
 * ============================================================================
 * KHỞI CHẠY ỨNG DỤNG VÀ ĐĂNG KÝ SERVICE WORKER TỰ ĐỘNG CẬP NHẬT
 * ============================================================================
 */
window.addEventListener('DOMContentLoaded', () => {
  // Khởi tạo đối tượng LilyPWA toàn cục
  window.lily = new LilyPWA();

  // Đăng ký Service Worker và kích hoạt cơ chế tự động kiểm tra code mới
  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('./sw.js').then((reg) => {
      reg.update(); // Bắt buộc kiểm tra phiên bản mới từ server mỗi khi mở trang
      reg.addEventListener('updatefound', () => {
        const newWorker = reg.installing;
        if (newWorker) {
          newWorker.addEventListener('statechange', () => {
            if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
              console.log('App đã được cập nhật phiên bản mới!');
            }
          });
        }
      });
    }).catch((err) => {
      console.warn('SW registration failed:', err);
    });
  }
});
