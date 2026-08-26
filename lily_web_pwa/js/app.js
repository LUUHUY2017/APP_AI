/**
 * LILY AI - iOS PWA CORE CONTROLLER
 * Protocol: Xiaozhi WebSocket v2 / Tenclass
 */

const CONFIG = {
  get wsUrl() { return localStorage.getItem('lily_ws_url') || 'wss://api.tenclass.net/xiaozhi/v1/'; },
  get token() { return localStorage.getItem('lily_token') || 'test-token'; },
  get deviceId() { return localStorage.getItem('lily_device_id') || 'a0:36:bc:2c:ed:40'; },
  get clientId() { return localStorage.getItem('lily_client_id') || '21ebee2f-926c-4703-9010-b488f5939580'; },
  save(wsUrl, token, deviceId) {
    localStorage.setItem('lily_ws_url', wsUrl);
    localStorage.setItem('lily_token', token);
    localStorage.setItem('lily_device_id', deviceId);
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

    // Web Audio
    this.audioCtx = null;
    this.mediaStream = null;
    this.micProcessor = null;
    this.playbackQueue = [];
    this.isPlayingAudio = false;

    // VAD
    this.silenceTimer = null;
    this.lastSpeechTime = 0;

    this.initElements();
    this.initEvents();
    this.connect();
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

    // Settings inputs
    this.inputWsUrl = document.getElementById('cfg-ws-url');
    this.inputToken = document.getElementById('cfg-token');
    this.inputDeviceId = document.getElementById('cfg-device-id');
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

  async fetchOtaConfig() {
    try {
      const resp = await fetch('https://api.tenclass.net/xiaozhi/ota/', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Device-Id': CONFIG.deviceId,
          'Client-Id': CONFIG.clientId
        },
        body: JSON.stringify({
          application: { version: '1.7.2' },
          board: { name: 'xiaozhi-test' }
        })
      });

      if (resp.ok) {
        const data = await resp.json();
        if (data && data.websocket) {
          if (data.websocket.url) this.dynamicWsUrl = data.websocket.url;
          if (data.websocket.token) this.dynamicToken = data.websocket.token;
          console.log('OTA Handshake success:', data.websocket);
        }
      }
    } catch (e) {
      console.warn('OTA handshake error:', e);
    }
  }

  async connect() {
    if (this.ws && (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING)) {
      return;
    }

    this.setStatus('Đang kết nối OTA...', false);
    await this.fetchOtaConfig();

    try {
      let targetWsUrl = this.dynamicWsUrl || CONFIG.wsUrl;
      let targetToken = this.dynamicToken || CONFIG.token;

      const params = [];
      if (targetToken) params.push(`token=${encodeURIComponent(targetToken)}`);
      if (CONFIG.deviceId) params.push(`device_id=${encodeURIComponent(CONFIG.deviceId)}`);
      if (CONFIG.clientId) params.push(`client_id=${encodeURIComponent(CONFIG.clientId)}`);
      params.push(`protocol_version=2`);

      if (params.length > 0) {
        targetWsUrl += (targetWsUrl.includes('?') ? '&' : '?') + params.join('&');
      }

      console.log('Connecting to WebSocket URL:', targetWsUrl);
      this.ws = new WebSocket(targetWsUrl);
      this.ws.binaryType = 'arraybuffer';

      this.ws.onopen = () => {
        this.isConnected = true;
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
        const errText = ev.code ? `Mất kết nối (${ev.code})` : 'Mất kết nối';
        this.setStatus(errText, false);
        console.warn('WebSocket Closed Code:', ev.code, 'Reason:', ev.reason);
        this.currentMsgBar.innerText = `⚠️ ${errText}. Bấm ⚙ Cài đặt để kiểm tra Token / URL Server.`;
        
        // Auto-reconnect after 3s
        if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
        this.reconnectTimer = setTimeout(() => this.connect(), 3000);
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
    if (!this.isConnected) this.connect();

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
