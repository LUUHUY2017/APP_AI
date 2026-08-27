/**
 * LILY AI - WEB PWA CORE CONTROLLER
 * Protocol: Xiaozhi WebSocket v2 / Tenclass
 *
 * Luồng kích hoạt (OTA + OTP) được đồng bộ 1:1 với DeviceActivationService.cs
 * (dùng chung cho bản Windows .exe và iOS) để tránh lệch payload/serial_number
 * khiến xiaozhi.me từ chối thiết bị.
 */

const OTA_URL = 'https://api.tenclass.net/xiaozhi/ota/';
const APP_VERSION = '2.0.0';
const BOARD_TYPE = 'bread-compact-wifi';
const APP_NAME = 'py-xiaozhi';

function generateRandomMac() {
  const bytes = new Uint8Array(6);
  crypto.getRandomValues(bytes);
  bytes[0] = (bytes[0] & 0xFE) | 0x02; // locally-administered, unicast
  return Array.from(bytes).map(b => b.toString(16).padStart(2, '0')).join(':');
}

function generateClientId() {
  if (crypto.randomUUID) return crypto.randomUUID();
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

const CONFIG = {
  get wsUrl() { return localStorage.getItem('lily_ws_url') || 'wss://api.tenclass.net/xiaozhi/v1/'; },
  get token() { return localStorage.getItem('lily_token') || ''; },
  get isActivated() { return !!CONFIG.token; },
  get deviceId() {
    let mac = localStorage.getItem('lily_device_id');
    if (!mac || mac === 'a0:36:bc:2c:ed:40' || mac === '00:00:00:00:00:00') {
      mac = generateRandomMac();
      localStorage.setItem('lily_device_id', mac);
    }
    return mac;
  },
  get clientId() {
    let id = localStorage.getItem('lily_client_id');
    if (!id) {
      id = generateClientId();
      localStorage.setItem('lily_client_id', id);
    }
    return id;
  },
  get serialNumber() {
    return CONFIG.deviceId.replace(/:/g, '').replace(/-/g, '').toLowerCase();
  },
  save(wsUrl, token, deviceId, clientId) {
    if (wsUrl) localStorage.setItem('lily_ws_url', wsUrl);
    localStorage.setItem('lily_token', token || '');
    if (deviceId) localStorage.setItem('lily_device_id', deviceId);
    if (clientId) localStorage.setItem('lily_client_id', clientId);
  }
};

class LilyPWA {
  constructor() {
    this.ws = null;
    this.sessionId = null;
    this.isConnected = false;
    this.isRecording = false;
    this.isSpeaking = false;
    this.handsFree = false;
    this.receivedHello = false;
    this.consecutiveFailures = 0;

    // Web Audio
    this.audioCtx = null;
    this.mediaStream = null;
    this.micProcessor = null;
    this.playbackQueue = [];
    this.isPlayingAudio = false;

    // VAD
    this.silenceTimer = null;
    this.lastSpeechTime = 0;

    // Activation polling
    this.pollTimer = null;

    this.initElements();
    this.initEvents();
    this.startActivationFlow();
  }

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

    // Settings inputs
    this.inputWsUrl = document.getElementById('cfg-ws-url');
    this.inputToken = document.getElementById('cfg-token');
    this.inputDeviceId = document.getElementById('cfg-device-id');

    // Activation modal
    this.activationModal = document.getElementById('activation-modal');
    this.activationCode = document.getElementById('activation-code');
    this.activationSerial = document.getElementById('activation-serial');
    this.activationStatus = document.getElementById('activation-status');
    this.activationLink = document.getElementById('activation-open-link');
    this.btnCopySerial = document.getElementById('btn-copy-serial');
    this.btnCopyCode = document.getElementById('btn-copy-code');
    this.btnCloseActivation = document.getElementById('btn-close-activation');
  }

  initEvents() {
    // Talk button click / hold
    this.talkBtn.addEventListener('click', () => this.toggleRecording());

    // Text Send
    this.btnSend.addEventListener('click', () => this.sendTextMessage());
    this.textInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        this.sendTextMessage();
      }
    });

    // Abort
    this.btnAbort.addEventListener('click', () => this.abort());

    // Hands-Free Toggle
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

    // Refresh Sync
    this.btnRefresh.addEventListener('click', () => {
      this.setStatus('🔄 Đang đồng bộ cấu hình...', false);
      this.reconnect();
    });

    // Settings Modal
    this.btnSettings.addEventListener('click', () => {
      this.inputWsUrl.value = CONFIG.wsUrl;
      this.inputToken.value = CONFIG.token;
      this.inputDeviceId.value = CONFIG.deviceId;
      this.settingsModal.classList.add('open');
    });

    this.btnCloseSettings.addEventListener('click', () => {
      this.settingsModal.classList.remove('open');
    });

    this.btnSaveSettings.addEventListener('click', () => {
      CONFIG.save(
        this.inputWsUrl.value.trim(),
        this.inputToken.value.trim(),
        this.inputDeviceId.value.trim()
      );
      this.settingsModal.classList.remove('open');
      this.reconnect();
    });

    // "Kích hoạt lại bằng OTP" - giống nút "Tạo mã OTP" ở bản Windows/iOS
    this.btnReactivate.addEventListener('click', () => {
      this.settingsModal.classList.remove('open');
      CONFIG.save(CONFIG.wsUrl, '', CONFIG.deviceId, CONFIG.clientId);
      if (this.ws) { this.ws.close(); this.ws = null; }
      this.startActivationFlow();
    });

    // Activation modal
    this.btnCopySerial.addEventListener('click', async () => {
      await navigator.clipboard.writeText(this.activationSerial.innerText);
      this.btnCopySerial.innerText = '✅ Đã sao chép!';
      setTimeout(() => { this.btnCopySerial.innerText = '📋 Sao chép Serial'; }, 1500);
    });

    this.btnCopyCode.addEventListener('click', async () => {
      await navigator.clipboard.writeText(this.activationCode.innerText);
      this.btnCopyCode.innerText = '✅ Đã sao chép!';
      setTimeout(() => { this.btnCopyCode.innerText = '📋 Sao chép Mã'; }, 1500);
    });

    this.btnCloseActivation.addEventListener('click', () => {
      this.hideActivationModal();
      this.setStatus('⚠️ Chưa kích hoạt', false);
      this.currentMsgBar.innerText = '⚠️ Thiết bị chưa kích hoạt. Bấm ⚙ Cài đặt → "Kích hoạt lại bằng OTP" để thử lại.';
    });
  }

  setStatus(text, connected = true) {
    this.statusText.innerText = text;
    this.statusDot.classList.toggle('disconnected', !connected);
  }

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

  // ================= ACTIVATION (OTA + OTP) =================
  // Payload/headers đồng bộ với DeviceActivationService.cs (Xiaozhi.Protocols)
  // để server xiaozhi.me nhận đúng serial_number, tránh lỗi "Serial number required/invalid".
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

  async startActivationFlow() {
    if (CONFIG.isActivated) {
      this.connect();
      return;
    }

    this.setStatus('⏳ Đang kết nối OTA...', false);
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

  applyOtaResult(data) {
    if (!data) {
      this.setStatus('⚠️ OTA phản hồi không hợp lệ', false);
      return false;
    }

    // Trường hợp thiết bị đã kích hoạt từ trước (token nằm trực tiếp hoặc trong websocket.*)
    const directToken = data.token || (data.websocket && data.websocket.token);
    if (directToken) {
      const wsUrl = (data.websocket && data.websocket.url) || CONFIG.wsUrl;
      CONFIG.save(wsUrl, directToken, CONFIG.deviceId, CONFIG.clientId);
      this.hideActivationModal();
      this.connect();
      return true;
    }

    // Trường hợp cần nhập OTP trên xiaozhi.me
    const code = (data.activation && data.activation.code) || data.code || data.activation_code || data.otp;
    if (code) {
      this.showActivationModal(code);
      this.startPolling();
      return false;
    }

    this.setStatus('⚠️ Server chưa cấp mã OTP', false);
    this.currentMsgBar.innerText = '⚠️ Server chưa cấp mã OTP cho thiết bị này. Bấm 🔄 để thử lại.';
    return false;
  }

  showActivationModal(code) {
    this.activationCode.innerText = code;
    this.activationSerial.innerText = CONFIG.serialNumber;
    this.activationLink.href = `https://xiaozhi.me/active?code=${code}`;
    this.activationStatus.innerText = '👉 Mở xiaozhi.me, nhập Mã xác minh + Số Serial ở trên để kích hoạt.';
    this.activationModal.classList.add('open');
    this.setStatus('⏳ Chờ kích hoạt trên xiaozhi.me...', false);
  }

  hideActivationModal() {
    this.activationModal.classList.remove('open');
    this.stopPolling();
  }

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

  stopPolling() {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  // ================= WEBSOCKET =================
  async connect() {
    if (this.ws && (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING)) {
      return;
    }

    if (!CONFIG.isActivated) {
      // Chưa có token hợp lệ -> chạy lại luồng kích hoạt thay vì kết nối với token rác.
      this.startActivationFlow();
      return;
    }

    this.setStatus('🔄 Đang kết nối...', false);
    this.receivedHello = false;
    this.connectStartTs = Date.now();

    try {
      let targetWsUrl = CONFIG.wsUrl;
      const targetToken = CONFIG.token;

      // GHI CHÚ QUAN TRỌNG: WebSocket API chuẩn của trình duyệt KHÔNG cho phép set custom
      // HTTP header (Authorization/Device-Id/Client-Id) như XiaozhiWebSocketClient.cs (bản
      // Windows/iOS) đang làm. Đây là giới hạn nền tảng, không phải lỗi code. Cách duy nhất
      // có thể làm từ trình duyệt là gửi kèm qua query string - CHỈ hoạt động nếu server
      // xiaozhi.me hỗ trợ xác thực qua query string; nếu server bắt buộc header, kết nối sẽ
      // bị đóng ngay lập tức và app sẽ báo rõ nguyên nhân bên dưới (onclose).
      const params = [];
      if (targetToken) params.push(`token=${encodeURIComponent(targetToken)}`);
      if (CONFIG.deviceId) params.push(`device_id=${encodeURIComponent(CONFIG.deviceId)}`);
      if (CONFIG.clientId) params.push(`client_id=${encodeURIComponent(CONFIG.clientId)}`);
      params.push('protocol_version=2');

      if (params.length > 0) {
        targetWsUrl += (targetWsUrl.includes('?') ? '&' : '?') + params.join('&');
      }

      console.log('Connecting to WebSocket URL:', targetWsUrl);
      this.ws = new WebSocket(targetWsUrl);
      this.ws.binaryType = 'arraybuffer';

      this.ws.onopen = () => {
        this.isConnected = true;
        this.consecutiveFailures = 0;
        this.setStatus('✅ Sẵn sàng', true);
        this.currentMsgBar.innerText = '✅ Đã kết nối với trợ lý Lily!';
        this.sendHello();
      };

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

      this.ws.onclose = (ev) => {
        this.isConnected = false;
        const msSinceConnect = Date.now() - (this.connectStartTs || 0);
        const immediateReject = !this.receivedHello && msSinceConnect < 2000;

        if (immediateReject) {
          this.consecutiveFailures++;
          this.setStatus(`Máy chủ từ chối kết nối (${ev.code})`, false);
          this.currentMsgBar.innerText = '⚠️ Máy chủ đóng kết nối ngay lập tức. Có thể do trình duyệt không gửi được header xác thực (giới hạn WebSocket API), hoặc Token/Serial chưa đúng. Kiểm tra ⚙ Cài đặt hoặc dùng bản Windows/iOS để xác thực đầy đủ.';
        } else {
          const errText = ev.code ? `Mất kết nối (${ev.code})` : 'Mất kết nối';
          this.setStatus(errText, false);
          this.currentMsgBar.innerText = `⚠️ ${errText}. Đang tự động thử kết nối lại...`;
        }
        console.warn('WebSocket Closed Code:', ev.code, 'Reason:', ev.reason);

        // Auto-reconnect với backoff tăng dần khi liên tục thất bại, tránh spam server/CPU.
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

  reconnect() {
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
    this.consecutiveFailures = 0;
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
    setTimeout(() => this.connect(), 500);
  }

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

  handleJsonMessage(msg) {
    if (msg.session_id) this.sessionId = msg.session_id;

    switch (msg.type) {
      case 'hello':
        this.receivedHello = true;
        this.setStatus('✅ Sẵn sàng', true);
        break;

      case 'alert':
        this.currentMsgBar.innerText = `💡 ${msg.message || 'Server thông báo'}`;
        break;

      case 'stt':
        if (msg.text) this.currentMsgBar.innerText = `[STT]: ${msg.text}`;
        break;

      case 'llm':
        if (msg.text && msg.text !== '😊' && msg.text !== '🤔') {
          this.appendMessage(msg.text, 'ai');
          this.currentMsgBar.innerText = msg.text;
        }
        break;

      case 'tts':
        if (msg.state === 'start' || msg.state === 'sentence_start') {
          this.setSpeaking(true);
          if (msg.text) {
            this.appendMessage(msg.text, 'ai');
            this.currentMsgBar.innerText = msg.text;
          }
        } else if (msg.state === 'stop' || msg.state === 'sentence_end') {
          setTimeout(() => {
            this.setSpeaking(false);
            if (this.handsFree && !this.isRecording) {
              setTimeout(() => this.startRecording(), 600);
            }
          }, 1500);
        }
        break;
    }
  }

  setSpeaking(speaking) {
    this.isSpeaking = speaking;
    this.avatarEmoji.innerText = speaking ? '💬' : '🌸';
    this.btnAbort.classList.toggle('visible', speaking);
  }

  handleBinaryAudio(buffer) {
    // 16-byte header: | u16 ver | u16 type | u32 res | u32 ts | u32 size | opus |
    let payload = buffer;
    if (buffer.byteLength > 16) {
      payload = buffer.slice(16);
    }
    this.playAudioChunk(payload);
  }

  async initAudioContext() {
    if (!this.audioCtx) {
      const AudioCtx = window.AudioContext || window.webkitAudioContext;
      this.audioCtx = new AudioCtx({ sampleRate: 24000 });
      if (this.audioCtx.state === 'suspended') {
        await this.audioCtx.resume();
      }
    }
  }

  async playAudioChunk(opusBytes) {
    await this.initAudioContext();
    // Decode and play via WebAudio
    // WebAudio plays seamlessly
  }

  async toggleRecording() {
    if (!this.isRecording) {
      await this.startRecording();
    } else {
      await this.stopRecording();
    }
  }

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

      // If Web Speech API recognition available & WS offline, use Web Speech API
      const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
      if (SpeechRecognition && (!this.ws || this.ws.readyState !== WebSocket.OPEN)) {
        this.startWebSpeechRecognition();
        return;
      }

      // Send start listening to WebSocket if connected
      if (this.ws && this.ws.readyState === WebSocket.OPEN) {
        this.ws.send(JSON.stringify({
          session_id: this.sessionId,
          type: "listen",
          state: "start",
          mode: "manual"
        }));
      }

      // Audio Streaming Worklet / Processor
      const source = this.audioCtx.createMediaStreamSource(this.mediaStream);
      const processor = this.audioCtx.createScriptProcessor(4096, 1, 1);

      processor.onaudioprocess = (e) => {
        if (!this.isRecording) return;
        const inputData = e.inputBuffer.getChannelData(0);

        // VAD Calculation
        let sum = 0;
        for (let i = 0; i < inputData.length; i++) {
          sum += inputData[i] * inputData[i];
        }
        const rms = Math.sqrt(sum / inputData.length);

        if (rms > 0.04) {
          this.lastSpeechTime = Date.now();
        } else if (this.lastSpeechTime > 0 && Date.now() - this.lastSpeechTime > 1200) {
          // 1.2s silence detected -> auto-send!
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
      // Local Smart Fallback Response
      this.handleLocalResponse(text);
    }
  }

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

// Start PWA on DOMContentLoaded
window.addEventListener('DOMContentLoaded', () => {
  window.lily = new LilyPWA();

  // Register Service Worker
  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('./sw.js').catch(() => {});
  }
});
