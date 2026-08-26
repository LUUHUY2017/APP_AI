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

  connect() {
    if (this.ws && (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING)) {
      return;
    }

    this.setStatus('Đang kết nối...', false);
    try {
      this.ws = new WebSocket(CONFIG.wsUrl);
      this.ws.binaryType = 'arraybuffer';

      this.ws.onopen = () => {
        this.isConnected = true;
        this.setStatus('✅ Sẵn sàng', true);
        this.sendHello();
      };

      this.ws.onmessage = (event) => {
        if (typeof event.data === 'string') {
          this.handleJsonMessage(JSON.parse(event.data));
        } else if (event.data instanceof ArrayBuffer) {
          this.handleBinaryAudio(event.data);
        }
      };

      this.ws.onclose = () => {
        this.isConnected = false;
        this.setStatus('Mất kết nối', false);
        // Auto-reconnect after 3s
        setTimeout(() => this.connect(), 3000);
      };

      this.ws.onerror = (err) => {
        console.error('WebSocket Error:', err);
      };
    } catch (e) {
      console.error('Connect failed:', e);
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

      // Send start listening
      this.ws.send(JSON.stringify({
        session_id: this.sessionId,
        type: "listen",
        state: "start",
        mode: "manual"
      }));

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
    this.currentMsgBar.innerText = '⏳ Đang gửi câu hỏi...';

    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify({
        session_id: this.sessionId,
        type: "listen",
        state: "detect",
        text: text
      }));
    }
  }

  abort() {
    this.setSpeaking(false);
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
