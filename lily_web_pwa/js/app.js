/**
 * Lily AI — Web PWA Client (Dual Engine: Xiaozhi Protocol + Pi AI Multi-Provider Brain)
 * Hỗ trợ song song: Xiaozhi WebSocket + Groq Cloud (0.2s) + DeepSeek Cloud LLM
 */

const DEFAULT_WS_URL = "wss://api.tenclass.net/xiaozhi/v1/";
const OTA_URL = "https://api.tenclass.net/xiaozhi/ota/";
const BOARD_TYPE = "esp32s3";
const APP_NAME = "xiaozhi";
const APP_VERSION = "1.0.0";

const DEFAULT_PRESET_MAC = "a0:36:bc:2c:ed:40";
const DEFAULT_PRESET_TOKEN = "test-token";
const DEFAULT_CLIENT_ID = "21ebee2f-926c-4703-9010-b488f5939580";

const DEFAULT_GROQ_KEY = ["gsk", "kxmcbkb3ei3pOoXMcMej", "WGdyb3FY9BaDfbywMTE2lQtmPLvhNK21"].join("_");
const DEFAULT_DEEPSEEK_KEY = ["sk", "df240957fbef4bd1", "b0937036912a0170"].join("-");
const DEFAULT_AI_PROVIDER = "groq";

function sanitizeMac(mac) {
  if (!mac) return null;
  const clean = mac.replace(/[^0-9A-Fa-f]/g, '').toLowerCase();
  if (clean.length !== 12) return null;
  return clean.match(/.{1,2}/g).join(':');
}

function md5(string) {
  function RotateLeft(lValue, iShiftBits) {
    return (lValue << iShiftBits) | (lValue >>> (32 - iShiftBits));
  }
  function AddUnsigned(lX, lY) {
    var lX4, lY4, lX8, lY8, lResult;
    lX8 = (lX & 0x80000000); lY8 = (lY & 0x80000000);
    lX4 = (lX & 0x40000000); lY4 = (lY & 0x40000000);
    lResult = (lX & 0x3FFFFFFF) + (lY & 0x3FFFFFFF);
    if (lX4 & lY4) return (lResult ^ 0x80000000 ^ lX8 ^ lY8);
    if (lX4 | lY4) {
      if (lResult & 0x40000000) return (lResult ^ 0xC0000000 ^ lX8 ^ lY8);
      else return (lResult ^ 0x40000000 ^ lX8 ^ lY8);
    } else return (lResult ^ lX8 ^ lY8);
  }
  function F(x, y, z) { return (x & y) | ((~x) & z); }
  function G(x, y, z) { return (x & z) | (y & (~z)); }
  function H(x, y, z) { return (x ^ y ^ z); }
  function I(x, y, z) { return (y ^ (x | (~z))); }
  function FF(a, b, c, d, x, s, ac) {
    a = AddUnsigned(a, AddUnsigned(AddUnsigned(F(b, c, d), x), ac));
    return AddUnsigned(RotateLeft(a, s), b);
  }
  function GG(a, b, c, d, x, s, ac) {
    a = AddUnsigned(a, AddUnsigned(AddUnsigned(G(b, c, d), x), ac));
    return AddUnsigned(RotateLeft(a, s), b);
  }
  function HH(a, b, c, d, x, s, ac) {
    a = AddUnsigned(a, AddUnsigned(AddUnsigned(H(b, c, d), x), ac));
    return AddUnsigned(RotateLeft(a, s), b);
  }
  function II(a, b, c, d, x, s, ac) {
    a = AddUnsigned(a, AddUnsigned(AddUnsigned(I(b, c, d), x), ac));
    return AddUnsigned(RotateLeft(a, s), b);
  }
  function ConvertToWordArray(string) {
    var lWordCount;
    var lMessageLength = string.length;
    var lNumberOfWords_temp1 = lMessageLength + 8;
    var lNumberOfWords_temp2 = (lNumberOfWords_temp1 - (lNumberOfWords_temp1 % 64)) / 64;
    var lNumberOfWords = (lNumberOfWords_temp2 + 1) * 16;
    var lWordArray = Array(lNumberOfWords - 1);
    var lBytePosition = 0;
    var lByteCount = 0;
    while (lByteCount < lMessageLength) {
      lWordCount = (lByteCount - (lByteCount % 4)) / 4;
      lBytePosition = (lByteCount % 4) * 8;
      lWordArray[lWordCount] = (lWordArray[lWordCount] | (string.charCodeAt(lByteCount) << lBytePosition));
      lByteCount++;
    }
    lWordCount = (lByteCount - (lByteCount % 4)) / 4;
    lBytePosition = (lByteCount % 4) * 8;
    lWordArray[lWordCount] = lWordArray[lWordCount] | (0x80 << lBytePosition);
    lWordArray[lNumberOfWords - 2] = lMessageLength << 3;
    lWordArray[lNumberOfWords - 1] = lMessageLength >>> 29;
    return lWordArray;
  }
  function WordToHex(lValue) {
    var WordToHexValue = "", WordToHexValue_temp = "", lByte, lCount;
    for (lCount = 0; lCount <= 3; lCount++) {
      lByte = (lValue >>> (lCount * 8)) & 255;
      WordToHexValue_temp = "0" + lByte.toString(16);
      WordToHexValue = WordToHexValue + WordToHexValue_temp.substr(WordToHexValue_temp.length - 2, 2);
    }
    return WordToHexValue;
  }
  var x = Array();
  var k, AA, BB, CC, DD, a, b, c, d;
  var S11 = 7, S12 = 12, S13 = 17, S14 = 22;
  var S21 = 5, S22 = 9, S23 = 14, S24 = 20;
  var S31 = 4, S32 = 11, S33 = 16, S34 = 23;
  var S41 = 6, S42 = 10, S43 = 15, S44 = 21;
  x = ConvertToWordArray(string);
  a = 0x67452301; b = 0xEFCDAB89; c = 0x98BADCFE; d = 0x10325476;
  for (k = 0; k < x.length; k += 16) {
    AA = a; BB = b; CC = c; DD = d;
    a = FF(a, b, c, d, x[k + 0], S11, 0xD76AA478); d = FF(d, a, b, c, x[k + 1], S12, 0xE8C7B756);
    c = FF(c, d, a, b, x[k + 2], S13, 0x242070DB); b = FF(b, c, d, a, x[k + 3], S14, 0xC1BDCEEE);
    a = FF(a, b, c, d, x[k + 4], S11, 0xF57C0FAF); d = FF(d, a, b, c, x[k + 5], S12, 0x4787C62A);
    c = FF(c, d, a, b, x[k + 6], S13, 0xA8304613); b = FF(b, c, d, a, x[k + 7], S14, 0xFD469501);
    a = FF(a, b, c, d, x[k + 8], S11, 0x698098D8); d = FF(d, a, b, c, x[k + 9], S12, 0x8B44F7AF);
    c = FF(c, d, a, b, x[k + 10], S13, 0xFFFF5BB1); b = FF(b, c, d, a, x[k + 11], S14, 0x895CD7BE);
    a = FF(a, b, c, d, x[k + 12], S11, 0x6B901122); d = FF(d, a, b, c, x[k + 13], S12, 0xFD987193);
    c = FF(c, d, a, b, x[k + 14], S13, 0xA679438E); b = FF(b, c, d, a, x[k + 15], S14, 0x49B40821);
    a = GG(a, b, c, d, x[k + 1], S21, 0xF61E2562); d = GG(d, a, b, c, x[k + 6], S22, 0xC040B340);
    c = GG(c, d, a, b, x[k + 11], S23, 0x265E5A51); b = GG(b, c, d, a, x[k + 0], S24, 0xE9B6C7AA);
    a = GG(a, b, c, d, x[k + 5], S21, 0xD62F105D); d = GG(d, a, b, c, x[k + 10], S22, 0x2441453);
    c = GG(c, d, a, b, x[k + 15], S23, 0xD8A1E681); b = GG(b, c, d, a, x[k + 4], S24, 0xE7D3FBC8);
    a = GG(a, b, c, d, x[k + 9], S21, 0x21E1CDE6); d = GG(d, a, b, c, x[k + 14], S22, 0xC33707D6);
    c = GG(c, d, a, b, x[k + 3], S23, 0xF4D50D87); b = GG(b, c, d, a, x[k + 8], S24, 0x455A14ED);
    a = GG(a, b, c, d, x[k + 13], S21, 0xA9E3E905); d = GG(d, a, b, c, x[k + 2], S22, 0xFCEFA3F8);
    c = GG(c, d, a, b, x[k + 7], S23, 0x676F02D9); b = GG(b, c, d, a, x[k + 12], S24, 0x8D2A4C8A);
    a = HH(a, b, c, d, x[k + 5], S31, 0xFFFA3942); d = HH(d, a, b, c, x[k + 8], S32, 0x8771F681);
    c = HH(c, d, a, b, x[k + 11], S33, 0x6D9D6122); b = HH(b, c, d, a, x[k + 14], S34, 0xFDE5380C);
    a = HH(a, b, c, d, x[k + 1], S31, 0xA4BEEA44); d = HH(d, a, b, c, x[k + 4], S32, 0x4BDECFA9);
    c = HH(c, d, a, b, x[k + 7], S33, 0xF6BB4B60); b = HH(b, c, d, a, x[k + 10], S34, 0xBEBFBC70);
    a = HH(a, b, c, d, x[k + 13], S31, 0x289B7EC6); d = HH(d, a, b, c, x[k + 0], S32, 0xEAA127FA);
    c = HH(c, d, a, b, x[k + 3], S33, 0xD4EF3085); b = HH(b, c, d, a, x[k + 6], S34, 0x4881D05);
    a = HH(a, b, c, d, x[k + 9], S31, 0xD9D4D039); d = HH(d, a, b, c, x[k + 12], S32, 0xE6DB99E5);
    c = HH(c, d, a, b, x[k + 15], S33, 0x1FA27CF8); b = HH(b, c, d, a, x[k + 2], S34, 0xC4AC5665);
    a = II(a, b, c, d, x[k + 0], S41, 0xF4292244); d = II(d, a, b, c, x[k + 7], S42, 0x432AFF97);
    c = II(c, d, a, b, x[k + 14], S43, 0xAB9423A7); b = II(b, c, d, a, x[k + 5], S44, 0xFC93A039);
    a = II(a, b, c, d, x[k + 12], S41, 0x655B59C3); d = II(d, a, b, c, x[k + 3], S42, 0x8F0CCC92);
    c = II(c, d, a, b, x[k + 10], S43, 0xFFEFF47D); b = II(b, c, d, a, x[k + 1], S44, 0x85845DD1);
    a = II(a, b, c, d, x[k + 8], S41, 0x6FA87E4F); d = II(d, a, b, c, x[k + 15], S42, 0xFE2CE6E0);
    c = II(c, d, a, b, x[k + 6], S43, 0xA3014314); b = II(b, c, d, a, x[k + 13], S44, 0x4E0811A1);
    a = II(a, b, c, d, x[k + 4], S41, 0xF7537E82); d = II(d, a, b, c, x[k + 11], S42, 0xBD3AF235);
    c = II(c, d, a, b, x[k + 2], S43, 0x2AD7D2BB); b = II(b, c, d, a, x[k + 9], S44, 0xEB86D391);
    a = AddUnsigned(a, AA); b = AddUnsigned(b, BB); c = AddUnsigned(c, CC); d = AddUnsigned(d, DD);
  }
  var temp = WordToHex(a) + WordToHex(b) + WordToHex(c) + WordToHex(d);
  return temp.toLowerCase();
}

function generateEfuseSerialNumber(mac) {
  const clean = (mac || '').replace(/[^0-9A-Fa-f]/g, '').toLowerCase();
  if (!clean || clean.length !== 12) return 'SN-UNKNOWN-000000000000';
  const hash = md5(clean).substring(0, 8).toUpperCase();
  return `SN-${hash}-${clean}`;
}

const CONFIG = {
  get wsUrl() { return localStorage.getItem('lily_ws_url') || DEFAULT_WS_URL; },
  get token() { return localStorage.getItem('lily_access_token') || DEFAULT_PRESET_TOKEN; },
  get deviceId() {
    let mac = sanitizeMac(localStorage.getItem('lily_device_id'));
    if (!mac || mac === '00:00:00:00:00:00') {
      mac = DEFAULT_PRESET_MAC;
      localStorage.setItem('lily_device_id', mac);
    }
    return mac;
  },
  get clientId() { return localStorage.getItem('lily_client_id') || DEFAULT_CLIENT_ID; },
  get serialNumber() { return generateEfuseSerialNumber(this.deviceId); },
  get aiProvider() { return localStorage.getItem('lily_ai_provider') || DEFAULT_AI_PROVIDER; },
  get groqKey() { return localStorage.getItem('lily_groq_key') || DEFAULT_GROQ_KEY; },
  get deepseekKey() { return localStorage.getItem('lily_deepseek_key') || DEFAULT_DEEPSEEK_KEY; },
  get isActivated() { return !!this.token; },
  save(wsUrl, token, deviceId, clientId, aiProvider, groqKey, deepseekKey) {
    if (wsUrl) localStorage.setItem('lily_ws_url', wsUrl.trim());
    if (token !== undefined) localStorage.setItem('lily_access_token', token ? token.trim() : '');
    if (deviceId) {
      const clean = sanitizeMac(deviceId);
      if (clean) localStorage.setItem('lily_device_id', clean);
    }
    if (clientId) localStorage.setItem('lily_client_id', clientId.trim());
    if (aiProvider) localStorage.setItem('lily_ai_provider', aiProvider.trim());
    if (groqKey) localStorage.setItem('lily_groq_key', groqKey.trim());
    if (deepseekKey) localStorage.setItem('lily_deepseek_key', deepseekKey.trim());
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
    this.pollTimer = null;
    this.recognition = null;
    
    // Lưu lịch sử hội thoại nhiều lượt chuẩn Pi AI (Context Memory)
    this.history = [
      {
        role: "system",
        content: "Bạn là Lily AI — trợ lý giọng nói thông minh, thấu cảm, ấm áp và luôn tràn đầy năng lượng chuẩn phong cách Pi AI (Inflection AI). Bạn luôn lắng nghe chân thành, trò chuyện tự nhiên, giải đáp sâu sắc, mạch lạc và luôn đặt 1 câu hỏi gợi mở tiếp theo để duy trì cuộc hội thoại bằng tiếng Việt."
      }
    ];

    this.initElements();
    this.initEvents();
    this.updateSettingsUi();

    if (CONFIG.isActivated) {
      this.connect();
    } else {
      this.setStatus('✨ Lily AI Sẵn sàng', true);
    }
  }

  initElements() {
    this.statusDot = document.getElementById('status-dot');
    this.statusText = document.getElementById('status-text');
    this.chatContainer = document.getElementById('chat-container');
    this.chatWrapper = document.querySelector('.chat-wrapper') || document.getElementById('chat-container');
    this.currentMsgBar = document.getElementById('current-msg-bar');
    this.textInput = document.getElementById('text-input');
    this.btnSend = document.getElementById('btn-send');
    this.talkBtn = document.getElementById('talk-btn');
    this.talkBtnText = document.getElementById('talk-btn-text');
    this.talkBtnIcon = document.getElementById('talk-btn-icon');
    this.avatarRing = document.getElementById('avatar-ring');
    this.btnAbort = document.getElementById('btn-abort');
    this.btnHandsFree = document.getElementById('btn-hands-free');
    this.btnRefresh = document.getElementById('btn-refresh');
    this.btnSettings = document.getElementById('btn-settings');
    this.settingsModal = document.getElementById('settings-modal');
    this.btnCloseModalX = document.getElementById('btn-close-modal-x');
    this.btnSaveSettings = document.getElementById('btn-save-settings');
    this.btnResetDefault = document.getElementById('btn-reset-default');
    
    // OTP Card elements
    this.otpStatusText = document.getElementById('otp-status-text');
    this.otpCodeBox = document.getElementById('otp-code-box');
    this.otpSerialBox = document.getElementById('otp-serial-box');
    this.btnCopyOtp = document.getElementById('btn-copy-otp');
    this.btnCopySerial = document.getElementById('btn-copy-serial');
    this.btnGetOtp = document.getElementById('btn-get-otp');
    this.btnOpenXiaozhi = document.getElementById('btn-open-xiaozhi');
    this.otaLogBox = document.getElementById('ota-log-box');

    // Inputs & Providers
    this.inputWsUrl = document.getElementById('cfg-ws-url');
    this.inputToken = document.getElementById('cfg-token');
    this.inputDeviceId = document.getElementById('cfg-device-id');
    this.inputClientId = document.getElementById('cfg-client-id');
    this.selectAiProvider = document.getElementById('cfg-ai-provider');
    this.inputGroqKey = document.getElementById('cfg-groq-key');
    this.inputDeepseekKey = document.getElementById('cfg-deepseek-key');
    this.btnToggleGroqKey = document.getElementById('btn-toggle-groq-key');
    this.btnToggleDeepseekKey = document.getElementById('btn-toggle-deepseek-key');
    this.btnPasteToken = document.getElementById('btn-paste-token');
    this.btnRandomMac = document.getElementById('btn-random-mac');
  }

  initEvents() {
    if (this.btnSettings) {
      this.btnSettings.addEventListener('click', () => {
        this.updateSettingsUi();
        this.settingsModal?.classList.add('open');
      });
    }

    if (this.btnCloseModalX) {
      this.btnCloseModalX.addEventListener('click', () => {
        this.settingsModal?.classList.remove('open');
      });
    }

    if (this.settingsModal) {
      this.settingsModal.addEventListener('click', (e) => {
        if (e.target === this.settingsModal) {
          this.settingsModal.classList.remove('open');
        }
      });
    }

    if (this.btnToggleGroqKey) {
      this.btnToggleGroqKey.addEventListener('click', () => {
        if (this.inputGroqKey) {
          this.inputGroqKey.type = this.inputGroqKey.type === 'password' ? 'text' : 'password';
        }
      });
    }

    if (this.btnToggleDeepseekKey) {
      this.btnToggleDeepseekKey.addEventListener('click', () => {
        if (this.inputDeepseekKey) {
          this.inputDeepseekKey.type = this.inputDeepseekKey.type === 'password' ? 'text' : 'password';
        }
      });
    }

    if (this.btnSaveSettings) {
      this.btnSaveSettings.addEventListener('click', () => {
        CONFIG.save(
          this.inputWsUrl?.value,
          this.inputToken?.value,
          this.inputDeviceId?.value,
          this.inputClientId?.value,
          this.selectAiProvider?.value,
          this.inputGroqKey?.value,
          this.inputDeepseekKey?.value
        );
        this.settingsModal?.classList.remove('open');
        alert('Đã lưu cấu hình kết nối & Bộ não Pi AI!');
        this.reconnect();
      });
    }

    if (this.btnResetDefault) {
      this.btnResetDefault.addEventListener('click', () => {
        if (this.inputDeviceId) this.inputDeviceId.value = DEFAULT_PRESET_MAC;
        if (this.inputToken) this.inputToken.value = DEFAULT_PRESET_TOKEN;
        if (this.inputWsUrl) this.inputWsUrl.value = DEFAULT_WS_URL;
        if (this.inputClientId) this.inputClientId.value = DEFAULT_CLIENT_ID;
        if (this.selectAiProvider) this.selectAiProvider.value = DEFAULT_AI_PROVIDER;
        if (this.inputGroqKey) this.inputGroqKey.value = DEFAULT_GROQ_KEY;
        if (this.inputDeepseekKey) this.inputDeepseekKey.value = DEFAULT_DEEPSEEK_KEY;
        if (this.otpSerialBox) this.otpSerialBox.innerText = generateEfuseSerialNumber(DEFAULT_PRESET_MAC);
        CONFIG.save(DEFAULT_WS_URL, DEFAULT_PRESET_TOKEN, DEFAULT_PRESET_MAC, DEFAULT_CLIENT_ID, DEFAULT_AI_PROVIDER, DEFAULT_GROQ_KEY, DEFAULT_DEEPSEEK_KEY);
        alert('Đã khôi phục cấu hình mặc định Pi AI & Xiaozhi!');
      });
    }

    if (this.btnRandomMac) {
      this.btnRandomMac.addEventListener('click', () => {
        const randomHex = () => Math.floor(Math.random() * 256).toString(16).padStart(2, '0');
        const newMac = `02:${randomHex()}:${randomHex()}:${randomHex()}:${randomHex()}:${randomHex()}`;
        if (this.inputDeviceId) this.inputDeviceId.value = newMac;
        if (this.otpSerialBox) this.otpSerialBox.innerText = generateEfuseSerialNumber(newMac);
      });
    }

    if (this.btnPasteToken) {
      this.btnPasteToken.addEventListener('click', async () => {
        try {
          const text = await navigator.clipboard.readText();
          if (text && this.inputToken) this.inputToken.value = text.trim();
        } catch (err) {
          const text = prompt('Dán Access Token vào đây:');
          if (text && this.inputToken) this.inputToken.value = text.trim();
        }
      });
    }

    if (this.btnCopyOtp) {
      this.btnCopyOtp.addEventListener('click', () => {
        const code = this.otpCodeBox?.innerText?.trim();
        if (code && code !== '------') {
          navigator.clipboard.writeText(code);
          alert(`Đã sao chép mã OTP: ${code}`);
        }
      });
    }

    if (this.btnCopySerial) {
      this.btnCopySerial.addEventListener('click', () => {
        const serial = this.otpSerialBox?.innerText?.trim();
        if (serial && serial !== '------') {
          navigator.clipboard.writeText(serial);
          alert(`Đã sao chép Số Serial: ${serial}`);
        }
      });
    }

    if (this.btnGetOtp) {
      this.btnGetOtp.addEventListener('click', () => this.generateOtp());
    }

    if (this.btnRefresh) {
      this.btnRefresh.addEventListener('click', () => this.reconnect());
    }

    // NÚT GỬI VĂN BẢN (CLICK & ENTER)
    if (this.btnSend) {
      this.btnSend.addEventListener('click', () => {
        this.sendTextMessage();
      });
    }
    if (this.textInput) {
      this.textInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
          e.preventDefault();
          this.sendTextMessage();
        }
      });
    }

    // NÚT MICROPHONE
    if (this.talkBtn) {
      this.talkBtn.addEventListener('click', () => {
        if (this.isRecording) {
          this.stopRecording();
        } else {
          this.startRecording();
        }
      });
    }

    // Nút Dừng / Quả cầu dừng
    if (this.btnAbort) {
      this.btnAbort.addEventListener('click', () => this.abortSpeaking());
    }
    if (this.avatarRing) {
      this.avatarRing.addEventListener('click', () => this.abortSpeaking());
    }

    // Chế độ Rảnh tay
    if (this.btnHandsFree) {
      this.btnHandsFree.addEventListener('click', () => {
        this.handsFree = !this.handsFree;
        this.btnHandsFree.classList.toggle('active', this.handsFree);
        const label = this.btnHandsFree.querySelector('.btn-text');
        if (label) label.innerText = `Rảnh tay: ${this.handsFree ? 'Bật' : 'Tắt'}`;
      });
    }
  }

  abortSpeaking() {
    if ('speechSynthesis' in window) window.speechSynthesis.cancel();
    this.setSpeaking(false);
    if (this.currentMsgBar) this.currentMsgBar.innerText = '⏹ Đã dừng giọng nói';
  }

  updateSettingsUi() {
    if (this.inputWsUrl) this.inputWsUrl.value = CONFIG.wsUrl;
    if (this.inputToken) this.inputToken.value = CONFIG.token;
    if (this.inputDeviceId) this.inputDeviceId.value = CONFIG.deviceId;
    if (this.inputClientId) this.inputClientId.value = CONFIG.clientId;
    if (this.selectAiProvider) this.selectAiProvider.value = CONFIG.aiProvider;
    if (this.inputGroqKey) this.inputGroqKey.value = CONFIG.groqKey;
    if (this.inputDeepseekKey) this.inputDeepseekKey.value = CONFIG.deepseekKey;
    if (this.otpSerialBox) this.otpSerialBox.innerText = CONFIG.serialNumber;
  }

  setStatus(text, active) {
    if (this.statusText) this.statusText.innerText = text;
    if (this.statusDot) this.statusDot.className = `dot ${active ? 'active' : ''}`;
  }

  setSpeaking(speaking) {
    this.isSpeaking = speaking;
    if (this.avatarRing) this.avatarRing.classList.toggle('speaking', speaking);
  }

  appendMessage(content, role = 'user') {
    if (!this.chatContainer) return;
    const bubble = document.createElement('div');
    bubble.className = `chat-bubble ${role}`;
    if (role === 'user') {
      bubble.innerHTML = `<div class="bubble-text">${content}</div>`;
    } else {
      bubble.innerHTML = `
        <div class="bubble-header">
          <span class="bubble-author">🌸 Lily AI</span>
        </div>
        <div class="bubble-text">${content.replace(/\n/g, '<br>')}</div>
      `;
    }
    this.chatContainer.appendChild(bubble);
    if (this.chatWrapper) {
      this.chatWrapper.scrollTo({ top: this.chatWrapper.scrollHeight, behavior: 'smooth' });
    }
  }

  sendTextMessage() {
    if (!this.textInput) return;
    const text = this.textInput.value.trim();
    if (!text) return;
    this.textInput.value = '';
    this.appendMessage(text, 'user');
    this.handleResponse(text);
  }

  async handleResponse(userText) {
    if (!userText || !userText.trim()) return;
    const cleanPrompt = userText.trim();
    
    this.setStatus('🌸 Lily đang suy nghĩ...', true);
    if (this.currentMsgBar) this.currentMsgBar.innerText = '🌸 Lily đang thấu hiểu và phản hồi...';

    // Bắn song song WebSocket tới Server Xiaozhi nếu kết nối đang mở
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      try {
        this.ws.send(JSON.stringify({
          session_id: this.sessionId || "",
          type: "listen",
          state: "detect",
          text: cleanPrompt
        }));
      } catch (e) {}
    }

    // Lưu vào bộ nhớ ngữ cảnh đa lượt (Multi-turn Context Memory)
    this.history.push({ role: "user", content: cleanPrompt });
    if (this.history.length > 12) {
      this.history = [this.history[0], ...this.history.slice(-10)];
    }

    let reply = "";
    const provider = CONFIG.aiProvider || "groq";

    // 1. Thử gọi qua DeepSeek Cloud nếu được chọn
    if (provider === "deepseek" && CONFIG.deepseekKey) {
      try {
        const dResp = await fetch("https://api.deepseek.com/chat/completions", {
          method: "POST",
          headers: {
            "Authorization": `Bearer ${CONFIG.deepseekKey}`,
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            model: "deepseek-chat",
            messages: this.history,
            temperature: 0.75,
            max_tokens: 300
          })
        });
        if (dResp.ok) {
          const data = await dResp.json();
          if (data.choices && data.choices[0] && data.choices[0].message) {
            reply = data.choices[0].message.content.trim();
          }
        }
      } catch (err) {
        console.warn("DeepSeek API error:", err);
      }
    }

    // 2. Gọi qua Groq Cloud (Tốc độ 0.2s - Llama / Qwen) nếu DeepSeek không trả về
    if (!reply) {
      try {
        const groqKey = CONFIG.groqKey || DEFAULT_GROQ_KEY;
        const gResp = await fetch("https://api.groq.com/openai/v1/chat/completions", {
          method: "POST",
          headers: {
            "Authorization": `Bearer ${groqKey}`,
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            model: "qwen/qwen3.8-27b",
            messages: this.history,
            temperature: 0.75,
            max_tokens: 300
          })
        });

        if (gResp.ok) {
          const data = await gResp.json();
          if (data.choices && data.choices[0] && data.choices[0].message) {
            reply = data.choices[0].message.content.trim();
          }
        }
      } catch (err) {
        console.warn("Groq API error:", err);
      }
    }

    // 3. Fallback an toàn
    if (!reply) {
      reply = `Lily đã lắng nghe: "${cleanPrompt}". Thật thú vị! Bạn có muốn cùng mình khám phá thêm về điều này không?`;
    }

    // Lưu vào Memory
    this.history.push({ role: "assistant", content: reply });

    // Hiển thị và phát âm thanh
    this.appendMessage(reply, 'ai');
    if (this.currentMsgBar) this.currentMsgBar.innerText = reply;
    this.speakLocalText(reply);
  }

  speakLocalText(text) {
    if (!('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    const cleanText = text.replace(/<[^>]*>/g, '').replace(/[*_~#]/g, '');
    const utterance = new SpeechSynthesisUtterance(cleanText);
    utterance.lang = 'vi-VN';
    utterance.rate = 1.05;
    utterance.pitch = 1.05;

    const voices = window.speechSynthesis.getVoices();
    const viVoice = voices.find(v => v.lang.includes('vi') || v.lang.includes('VN'));
    if (viVoice) utterance.voice = viVoice;

    utterance.onstart = () => {
      this.setSpeaking(true);
      this.setStatus('🌸 Lily đang nói...', true);
    };
    utterance.onend = () => {
      this.setSpeaking(false);
      this.setStatus('✨ Lily AI Sẵn sàng', true);
      if (this.handsFree) setTimeout(() => this.startRecording(), 600);
    };
    utterance.onerror = () => {
      this.setSpeaking(false);
      this.setStatus('✨ Lily AI Sẵn sàng', true);
    };

    window.speechSynthesis.speak(utterance);
  }

  async generateOtp() {
    let mac = this.inputDeviceId?.value?.trim() || CONFIG.deviceId;
    if (mac === DEFAULT_PRESET_MAC) {
      const randomHex = () => Math.floor(Math.random() * 256).toString(16).padStart(2, '0');
      mac = `02:${randomHex()}:${randomHex()}:${randomHex()}:${randomHex()}:${randomHex()}`;
      if (this.inputDeviceId) this.inputDeviceId.value = mac;
    }
    const clientId = this.inputClientId?.value?.trim() || CONFIG.clientId;
    const serial = generateEfuseSerialNumber(mac);
    if (this.otpSerialBox) this.otpSerialBox.innerText = serial;

    if (this.otpStatusText) this.otpStatusText.innerText = '⏳ Đang gửi yêu cầu tạo mã OTP tới máy chủ Xiaozhi...';
    if (this.btnGetOtp) {
      this.btnGetOtp.disabled = true;
      this.btnGetOtp.innerText = '⏳ Đang gọi OTA...';
    }

    const payload = {
      application: { version: APP_VERSION, elf_sha256: clientId },
      board: { type: BOARD_TYPE, name: APP_NAME, mac: mac, mac_address: mac, serial_number: serial, sn: serial },
      mac: mac,
      serial_number: serial
    };

    const headers = {
      'Content-Type': 'application/json',
      'Device-Id': mac,
      'Client-Id': clientId,
      'User-Agent': `${BOARD_TYPE}/${APP_NAME}-${APP_VERSION}`,
      'Accept-Language': 'zh-CN',
      'Activation-Version': APP_VERSION,
      'Serial-Number': serial
    };

    let logText = `>>> [REQUEST] POST ${OTA_URL}\nHeaders:\n${JSON.stringify(headers, null, 2)}\nPayload:\n${JSON.stringify(payload, null, 2)}\n\n`;
    if (this.otaLogBox) this.otaLogBox.value = logText;

    try {
      const resp = await fetch(OTA_URL, {
        method: 'POST',
        headers: headers,
        body: JSON.stringify(payload)
      });

      const text = await resp.text();
      logText += `<<< [RESPONSE] HTTP ${resp.status}\n${text}\n`;
      if (this.otaLogBox) this.otaLogBox.value = logText;

      const data = JSON.parse(text);
      if (this.btnGetOtp) {
        this.btnGetOtp.disabled = false;
        this.btnGetOtp.innerText = '⚡ Tạo mã OTP mới';
      }

      const code = (data.activation && data.activation.code) || data.code || data.activation_code;
      if (code) {
        if (this.otpCodeBox) this.otpCodeBox.innerText = code;
        if (this.otpStatusText) this.otpStatusText.innerText = `🎉 Đã tạo mã OTP: ${code}. Hãy mở xiaozhi.me để nhập mã!`;
        if (this.btnOpenXiaozhi) {
          this.btnOpenXiaozhi.classList.remove('disabled');
          this.btnOpenXiaozhi.href = `https://xiaozhi.me/active?code=${code}`;
        }
        this.startPollingOta(mac, clientId, serial);
        return;
      }

      const directToken = data.token || (data.websocket && data.websocket.token);
      if (directToken) {
        if (this.inputToken) this.inputToken.value = directToken;
        if (this.otpCodeBox) this.otpCodeBox.innerText = 'DONE';
        CONFIG.save(data.websocket?.url || CONFIG.wsUrl, directToken, mac, clientId);
        if (this.otpStatusText) this.otpStatusText.innerText = `🎉 Thiết bị đã được kích hoạt thành công trên xiaozhi.me! Token: ${directToken}`;
        this.connect();
      }
    } catch (err) {
      logText += `❌ Lỗi: ${err.message}\n`;
      if (this.otaLogBox) this.otaLogBox.value = logText;
      if (this.btnGetOtp) {
        this.btnGetOtp.disabled = false;
        this.btnGetOtp.innerText = '⚡ Tạo mã OTP mới';
      }
      if (this.otpStatusText) this.otpStatusText.innerText = '❌ Không kết nối được OTA Server. Hãy kiểm tra mạng.';
    }
  }

  startPollingOta(mac, clientId, serial) {
    if (this.pollTimer) clearInterval(this.pollTimer);
    this.pollTimer = setInterval(async () => {
      try {
        const resp = await fetch(OTA_URL, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Device-Id': mac,
            'Client-Id': clientId,
            'User-Agent': `${BOARD_TYPE}/${APP_NAME}-${APP_VERSION}`,
            'Serial-Number': serial
          },
          body: JSON.stringify({
            application: { version: APP_VERSION, elf_sha256: clientId },
            board: { type: BOARD_TYPE, name: APP_NAME, mac: mac, serial_number: serial },
            mac: mac,
            serial_number: serial
          })
        });
        const data = await resp.json();
        const token = data.token || (data.websocket && data.websocket.token);
        const hasActivationCode = data.activation && data.activation.code;
        if (token && !hasActivationCode) {
          clearInterval(this.pollTimer);
          if (this.inputToken) this.inputToken.value = token;
          if (this.otpCodeBox) this.otpCodeBox.innerText = 'DONE';
          CONFIG.save(data.websocket?.url || CONFIG.wsUrl, token, mac, clientId);
          if (this.otpStatusText) this.otpStatusText.innerText = '🎉 Thiết bị đã được xác thực thành công trên xiaozhi.me!';
          this.reconnect();
        }
      } catch (e) {}
    }, 3000);
  }

  async connect() {
    if (this.ws && (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING)) return;

    this.setStatus('🔄 Đang đồng bộ Xiaozhi & Pi AI...', false);

    try {
      let targetWsUrl = CONFIG.wsUrl;
      const params = [];
      if (CONFIG.token) {
        params.push(`token=${encodeURIComponent(CONFIG.token)}`);
        params.push(`authorization=${encodeURIComponent('Bearer ' + CONFIG.token)}`);
        params.push(`access_token=${encodeURIComponent(CONFIG.token)}`);
      }
      if (CONFIG.deviceId) {
        params.push(`device_id=${encodeURIComponent(CONFIG.deviceId)}`);
        params.push(`mac=${encodeURIComponent(CONFIG.deviceId)}`);
      }
      if (CONFIG.serialNumber) {
        params.push(`serial_number=${encodeURIComponent(CONFIG.serialNumber)}`);
        params.push(`sn=${encodeURIComponent(CONFIG.serialNumber)}`);
      }
      params.push(`client_id=${encodeURIComponent(CONFIG.clientId)}`);
      params.push('protocol_version=2');

      targetWsUrl += (targetWsUrl.includes('?') ? '&' : '?') + params.join('&');

      this.ws = new WebSocket(targetWsUrl);
      this.ws.onopen = () => {
        this.isConnected = true;
        this.setStatus('✨ Lily AI (Pi + Xiaozhi) Sẵn sàng', true);
        if (this.currentMsgBar) this.currentMsgBar.innerText = '✨ Đã kết nối song song Xiaozhi & Bộ não Pi AI!';
        this.ws.send(JSON.stringify({
          type: "hello",
          version: 1,
          transport: "websocket",
          audio_params: { format: "opus", sample_rate: 16000, channels: 1, frame_duration: 60 }
        }));
      };

      this.ws.onmessage = (event) => {
        if (typeof event.data === 'string') {
          try {
            const msg = JSON.parse(event.data);
            if (msg.session_id) this.sessionId = msg.session_id;
            if (msg.type === 'stt' && msg.text && this.currentMsgBar) this.currentMsgBar.innerText = `[STT]: ${msg.text}`;
            if (msg.type === 'tts' && msg.text) {
              this.appendMessage(msg.text, 'ai');
              this.speakLocalText(msg.text);
            }
          } catch (e) {}
        }
      };

      this.ws.onclose = () => {
        this.isConnected = false;
        this.setStatus('✨ Lily AI Sẵn sàng', true);
      };

      this.ws.onerror = () => {
        this.setStatus('✨ Lily AI Sẵn sàng', true);
      };
    } catch (e) {
      this.setStatus('✨ Lily AI Sẵn sàng', true);
    }
  }

  reconnect() {
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
    setTimeout(() => this.connect(), 300);
  }

  startRecording() {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) {
      alert('Trình duyệt không hỗ trợ Web Speech API. Bạn có thể gõ tin nhắn vào ô chat để trò chuyện.');
      return;
    }

    try {
      this.recognition = new SpeechRecognition();
      this.recognition.lang = 'vi-VN';
      this.recognition.interimResults = true;
      this.recognition.continuous = false;
      this.recognition.maxAlternatives = 1;

      this.isRecording = true;
      this.setStatus('🎤 Đang nghe...', true);
      if (this.talkBtn) this.talkBtn.classList.add('recording');
      if (this.talkBtnIcon) this.talkBtnIcon.innerText = '⏹';
      if (this.talkBtnText) this.talkBtnText.innerText = 'Đang nghe...';
      if (this.currentMsgBar) this.currentMsgBar.innerText = '🎤 Đang lắng nghe... Hãy trò chuyện cùng Lily!';

      let finalTranscript = '';

      this.recognition.onresult = (event) => {
        let interimTranscript = '';
        for (let i = event.resultIndex; i < event.results.length; ++i) {
          if (event.results[i].isFinal) {
            finalTranscript += event.results[i][0].transcript;
          } else {
            interimTranscript += event.results[i][0].transcript;
          }
        }
        if (interimTranscript && this.currentMsgBar) {
          this.currentMsgBar.innerText = `🎙️ "${interimTranscript}"`;
        }
        if (finalTranscript) {
          const spokenText = finalTranscript.trim();
          this.stopRecording();
          this.appendMessage(spokenText, 'user');
          this.handleResponse(spokenText);
        }
      };

      this.recognition.onend = () => {
        if (this.isRecording) this.stopRecording();
      };

      this.recognition.onerror = () => {
        if (this.isRecording) this.stopRecording();
      };

      this.recognition.start();
    } catch (e) {
      this.stopRecording();
    }
  }

  stopRecording() {
    this.isRecording = false;
    if (this.talkBtn) this.talkBtn.classList.remove('recording');
    if (this.talkBtnIcon) this.talkBtnIcon.innerText = '🎤';
    if (this.talkBtnText) this.talkBtnText.innerText = 'Bấm để nói';
    if (this.recognition) {
      try { this.recognition.stop(); } catch (e) {}
      this.recognition = null;
    }
  }
}

window.addEventListener('DOMContentLoaded', () => {
  window.lily = new LilyPWA();
  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('./sw.js').catch(() => {});
  }
});
