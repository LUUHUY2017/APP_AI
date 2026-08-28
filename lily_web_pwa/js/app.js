/**
 * Lily AI - Web PWA Client (Full MAUI / .EXE Parity)
 * Đồng bộ 100% cấu hình, thuật toán eFuse và giao diện Kích hoạt OTP với bản .EXE / MAUI.
 */

// ============================================================================
// HẰNG SỐ CẤU HÌNH HỆ THỐNG
// ============================================================================
const DEFAULT_WS_URL = "wss://api.tenclass.net/xiaozhi/v1/";
const OTA_URL = "https://api.tenclass.net/xiaozhi/ota/";
const BOARD_TYPE = "esp32s3";
const APP_NAME = "xiaozhi";
const APP_VERSION = "1.0.0";

// Profile mặc định eFuse
const DEFAULT_PRESET_MAC = "cc:30:80:20:64:7c";
const DEFAULT_PRESET_TOKEN = "test-token";
const DEFAULT_CLIENT_ID = "a927bd19-f917-4a3a-9f5a-4e453603c9b4";

// ============================================================================
// HÀM HỖ TRỢ ĐỊNH DẠNG VÀ THUẬT TOÁN EFUSE SERIAL NUMBER
// ============================================================================
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
    lX8 = (lX & 0x80000000);
    lY8 = (lY & 0x80000000);
    lX4 = (lX & 0x40000000);
    lY4 = (lY & 0x40000000);
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

// ============================================================================
// BỘ QUẢN LÝ CẤU HÌNH LOCALSTORAGE (CONFIG MANAGER)
// ============================================================================
const CONFIG = {
  get wsUrl() {
    return localStorage.getItem('lily_ws_url') || DEFAULT_WS_URL;
  },
  get token() {
    return localStorage.getItem('lily_access_token') || '';
  },
  get deviceId() {
    let mac = sanitizeMac(localStorage.getItem('lily_device_id'));
    if (!mac || mac === '00:00:00:00:00:00') {
      mac = DEFAULT_PRESET_MAC;
      localStorage.setItem('lily_device_id', mac);
    }
    return mac;
  },
  get clientId() {
    return localStorage.getItem('lily_client_id') || DEFAULT_CLIENT_ID;
  },
  get serialNumber() {
    return generateEfuseSerialNumber(this.deviceId);
  },
  get isActivated() {
    return !!this.token;
  },
  save(wsUrl, token, deviceId, clientId) {
    if (wsUrl) localStorage.setItem('lily_ws_url', wsUrl.trim());
    if (token !== undefined) localStorage.setItem('lily_access_token', token ? token.trim() : '');
    if (deviceId) {
      const clean = sanitizeMac(deviceId);
      if (clean) localStorage.setItem('lily_device_id', clean);
    }
    if (clientId) localStorage.setItem('lily_client_id', clientId.trim());
  }
};

// ============================================================================
// LỚP ĐIỀU KHIỂN CHÍNH (LILY PWA CLIENT)
// ============================================================================
class LilyPWA {
  constructor() {
    this.ws = null;
    this.sessionId = null;
    this.isConnected = false;
    this.isRecording = false;
    this.isSpeaking = false;
    this.handsFree = false;
    this.pollTimer = null;

    this.initElements();
    this.initEvents();
    this.updateSettingsUi();

    // Kết nối ngay nếu đã có token
    if (CONFIG.isActivated) {
      this.connect();
    } else {
      this.setStatus('🌐 Web Voice Sẵn sàng', true);
    }
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

    // Inputs
    this.inputWsUrl = document.getElementById('cfg-ws-url');
    this.inputToken = document.getElementById('cfg-token');
    this.inputDeviceId = document.getElementById('cfg-device-id');
    this.inputClientId = document.getElementById('cfg-client-id');
    this.btnPasteToken = document.getElementById('btn-paste-token');
    this.btnRandomMac = document.getElementById('btn-random-mac');
  }

  initEvents() {
    this.btnSettings.addEventListener('click', () => {
      this.updateSettingsUi();
      this.settingsModal.classList.add('open');
    });

    this.btnCloseModalX.addEventListener('click', () => {
      this.settingsModal.classList.remove('open');
    });

    this.settingsModal.addEventListener('click', (e) => {
      if (e.target === this.settingsModal) {
        this.settingsModal.classList.remove('open');
      }
    });

    this.btnSaveSettings.addEventListener('click', () => {
      CONFIG.save(
        this.inputWsUrl.value,
        this.inputToken.value,
        this.inputDeviceId.value,
        this.inputClientId.value
      );
      this.settingsModal.classList.remove('open');
      alert('Đã lưu cấu hình kết nối!');
      this.reconnect();
    });

    this.btnResetDefault.addEventListener('click', () => {
      this.inputDeviceId.value = DEFAULT_PRESET_MAC;
      this.inputToken.value = DEFAULT_PRESET_TOKEN;
      this.inputWsUrl.value = DEFAULT_WS_URL;
      this.inputClientId.value = DEFAULT_CLIENT_ID;
      this.otpSerialBox.innerText = generateEfuseSerialNumber(DEFAULT_PRESET_MAC);
      CONFIG.save(DEFAULT_WS_URL, DEFAULT_PRESET_TOKEN, DEFAULT_PRESET_MAC, DEFAULT_CLIENT_ID);
      alert('Đã khôi phục cấu hình mặc định eFuse (cc:30:80:20:64:7c / test-token)!');
    });

    this.btnRandomMac.addEventListener('click', () => {
      const randomHex = () => Math.floor(Math.random() * 256).toString(16).padStart(2, '0');
      const newMac = `02:${randomHex()}:${randomHex()}:${randomHex()}:${randomHex()}:${randomHex()}`;
      this.inputDeviceId.value = newMac;
      this.otpSerialBox.innerText = generateEfuseSerialNumber(newMac);
    });

    this.btnPasteToken.addEventListener('click', async () => {
      try {
        const text = await navigator.clipboard.readText();
        if (text) this.inputToken.value = text.trim();
      } catch (err) {
        const text = prompt('Dán Access Token vào đây:');
        if (text) this.inputToken.value = text.trim();
      }
    });

    this.btnCopyOtp.addEventListener('click', () => {
      const code = this.otpCodeBox.innerText.trim();
      if (code && code !== '------') {
        navigator.clipboard.writeText(code);
        alert(`Đã sao chép mã OTP: ${code}`);
      }
    });

    this.btnCopySerial.addEventListener('click', () => {
      const serial = this.otpSerialBox.innerText.trim();
      if (serial && serial !== '------') {
        navigator.clipboard.writeText(serial);
        alert(`Đã sao chép Số Serial: ${serial}`);
      }
    });

    // Bấm Tạo mã OTP
    this.btnGetOtp.addEventListener('click', () => this.generateOtp());

    this.btnRefresh.addEventListener('click', () => this.reconnect());

    this.btnSend.addEventListener('click', () => this.sendTextMessage());
    this.textInput.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') this.sendTextMessage();
    });

    this.talkBtn.addEventListener('click', () => {
      if (this.isRecording) {
        this.stopRecording();
      } else {
        this.startRecording();
      }
    });

    this.btnAbort.addEventListener('click', () => {
      if ('speechSynthesis' in window) window.speechSynthesis.cancel();
      this.setSpeaking(false);
      this.currentMsgBar.innerText = '⏹ Đã dừng';
    });

    this.btnHandsFree.addEventListener('click', () => {
      this.handsFree = !this.handsFree;
      this.btnHandsFree.classList.toggle('active', this.handsFree);
      this.btnHandsFree.querySelector('.btn-label').innerText = `Rảnh tay: ${this.handsFree ? 'Bật' : 'Tắt'}`;
    });
  }

  updateSettingsUi() {
    this.inputWsUrl.value = CONFIG.wsUrl;
    this.inputToken.value = CONFIG.token;
    this.inputDeviceId.value = CONFIG.deviceId;
    this.inputClientId.value = CONFIG.clientId;
    this.otpSerialBox.innerText = CONFIG.serialNumber;
  }

  setStatus(text, active) {
    this.statusText.innerText = text;
    this.statusDot.className = `dot ${active ? 'active' : ''}`;
  }

  setSpeaking(speaking) {
    this.isSpeaking = speaking;
    this.avatarRing.classList.toggle('speaking', speaking);
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

  // ==========================================================================
  // LUỒNG TẠO MÃ OTP VÀ KÍCH HOẠT XIAOZHI.ME (MATCHING .EXE)
  // ==========================================================================
  async generateOtp() {
    const mac = this.inputDeviceId.value.trim() || CONFIG.deviceId;
    const clientId = this.inputClientId.value.trim() || CONFIG.clientId;
    const serial = generateEfuseSerialNumber(mac);

    this.otpStatusText.innerText = '⏳ Đang gửi yêu cầu tạo mã OTP tới máy chủ Xiaozhi...';
    this.btnGetOtp.disabled = true;
    this.btnGetOtp.innerText = '⏳ Đang gọi OTA...';

    const payload = {
      application: { version: APP_VERSION, elf_sha256: clientId },
      board: { type: BOARD_TYPE, name: APP_NAME, mac: mac, mac_address: mac, serial_number: serial, sn: serial },
      mac: mac,
      mac_address: mac,
      serial_number: serial,
      sn: serial
    };

    const headers = {
      'Content-Type': 'application/json',
      'Device-Id': mac,
      'Client-Id': clientId,
      'User-Agent': `${BOARD_TYPE}/${APP_NAME}-${APP_VERSION}`,
      'Accept-Language': 'zh-CN',
      'Activation-Version': APP_VERSION,
      'Mac-Address': mac,
      'Serial-Number': serial
    };

    let logText = `>>> [REQUEST] POST ${OTA_URL}
Headers:
${JSON.stringify(headers, null, 2)}
Payload:
${JSON.stringify(payload, null, 2)}

`;
    this.otaLogBox.value = logText;

    try {
      const resp = await fetch(OTA_URL, {
        method: 'POST',
        headers: headers,
        body: JSON.stringify(payload)
      });

      const text = await resp.text();
      logText += `<<< [RESPONSE] HTTP ${resp.status}
${text}
`;
      this.otaLogBox.value = logText;

      const data = JSON.parse(text);
      this.btnGetOtp.disabled = false;
      this.btnGetOtp.innerText = '⚡ Tạo mã OTP';

      // 1. Nếu có mã OTP activation.code
      const code = (data.activation && data.activation.code) || data.code || data.activation_code;
      if (code) {
        this.otpCodeBox.innerText = code;
        this.otpSerialBox.innerText = serial;
        this.otpStatusText.innerText = `🎉 Đã tạo mã OTP: ${code}. Hãy mở xiaozhi.me để nhập mã!`;
        this.btnOpenXiaozhi.classList.remove('disabled');
        this.btnOpenXiaozhi.href = `https://xiaozhi.me/active?code=${code}`;
        this.startPollingOta(mac, clientId, serial);
        return;
      }

      // 2. Nếu đã có token trả về trực tiếp
      const directToken = data.token || (data.websocket && data.websocket.token);
      if (directToken) {
        this.inputToken.value = directToken;
        CONFIG.save(CONFIG.wsUrl, directToken, mac, clientId);
        this.otpStatusText.innerText = `✅ Thiết bị đã kích hoạt sẵn token (${directToken})!`;
        alert('Thiết bị đã có token hợp lệ!');
      }
    } catch (err) {
      logText += `❌ Lỗi: ${err.message}
`;
      this.otaLogBox.value = logText;
      this.btnGetOtp.disabled = false;
      this.btnGetOtp.innerText = '⚡ Tạo mã OTP';
      this.otpStatusText.innerText = '❌ Không kết nối được OTA Server. Hãy kiểm tra mạng.';
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
        if (token && token !== 'test-token' && !data.activation) {
          clearInterval(this.pollTimer);
          this.inputToken.value = token;
          CONFIG.save(data.websocket?.url || CONFIG.wsUrl, token, mac, clientId);
          this.otpStatusText.innerText = '🎉 Kích hoạt thành công trên xiaozhi.me! Token đã được nạp.';
          alert('🎉 Chúc mừng! Thiết bị đã được kích hoạt thành công trên Xiaozhi.me!');
          this.reconnect();
        }
      } catch (e) {}
    }, 3000);
  }

  // ==========================================================================
  // WEBSOCKET & FALLBACK TRỢ LÝ GIỌNG NÓI
  // ==========================================================================
  async connect() {
    if (this.ws && (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING)) return;

    this.setStatus('🔄 Đang kết nối...', false);

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
        this.setStatus('✅ Lily AI - Sẵn sàng', true);
        this.currentMsgBar.innerText = '✅ Đã kết nối với trợ lý Lily!';
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
            if (msg.type === 'stt' && msg.text) this.currentMsgBar.innerText = `[STT]: ${msg.text}`;
            if (msg.type === 'tts' && msg.text) {
              this.appendMessage(msg.text, 'ai');
              this.speakLocalText(msg.text);
            }
          } catch (e) {}
        }
      };

      this.ws.onclose = (ev) => {
        this.isConnected = false;
        this.setStatus('🌐 Web Voice Sẵn sàng', true);
        this.currentMsgBar.innerText = '✨ Chế độ Web Voice đã sẵn sàng! Bấm 🎤 hoặc gõ tin nhắn để trò chuyện cùng Lily.';
      };

      this.ws.onerror = () => {
        this.setStatus('🌐 Web Voice Sẵn sàng', true);
      };
    } catch (e) {
      this.setStatus('🌐 Web Voice Sẵn sàng', true);
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
      alert('Trình duyệt không hỗ trợ Web Speech API. Vui lòng gõ tin nhắn vào ô chat.');
      return;
    }

    try {
      this.recognition = new SpeechRecognition();
      this.recognition.lang = 'vi-VN';
      this.recognition.interimResults = false;
      this.recognition.continuous = false;

      this.isRecording = true;
      this.talkBtn.classList.add('recording');
      this.talkBtnIcon.innerText = '⏹';
      this.talkBtnText.innerText = 'Đang nghe...';
      this.avatarRing.classList.add('pulsing');
      this.currentMsgBar.innerText = '🎤 Đang nghe giọng nói của bạn...';

      this.recognition.onresult = (event) => {
        const text = event.results[0][0].transcript;
        if (text) {
          this.appendMessage(text, 'user');
          this.currentMsgBar.innerText = `Bạn nói: "${text}"`;
          this.handleResponse(text);
        }
      };

      this.recognition.onend = () => {
        this.stopRecording();
      };

      this.recognition.onerror = () => {
        this.stopRecording();
      };

      this.recognition.start();
    } catch (e) {
      this.stopRecording();
    }
  }

  stopRecording() {
    this.isRecording = false;
    this.talkBtn.classList.remove('recording');
    this.talkBtnIcon.innerText = '🎤';
    this.talkBtnText.innerText = 'Bấm để nói';
    this.avatarRing.classList.remove('pulsing');
    if (this.recognition) {
      try { this.recognition.stop(); } catch (e) {}
      this.recognition = null;
    }
  }

  sendTextMessage() {
    const text = this.textInput.value.trim();
    if (!text) return;
    this.textInput.value = '';
    this.appendMessage(text, 'user');
    this.handleResponse(text);
  }

  handleResponse(userText) {
    this.setSpeaking(true);
    let reply = `Dạ, Lily đã nhận được câu hỏi: "${userText}". Mình là trợ lý AI, luôn sẵn sàng hỗ trợ bạn!`;

    const lower = userText.toLowerCase();
    if (lower.includes("chào") || lower.includes("hello")) {
      reply = "Xin chào bạn! Mình là Lily - Trợ lý ảo AI thông minh. Mình có thể giúp gì cho bạn hôm nay?";
    } else if (lower.includes("tên") || lower.includes("bạn là ai")) {
      reply = "Mình là Lily, trợ lý giọng nói thông minh được xây dựng để trò chuyện cùng bạn bằng tiếng Việt!";
    } else if (lower.includes("thời tiết")) {
      reply = "Hôm nay thời tiết rất đẹp và thoáng mát. Chúc bạn một ngày làm việc tràn đầy năng lượng nhé!";
    }

    setTimeout(() => {
      this.appendMessage(reply, 'ai');
      this.currentMsgBar.innerText = reply;
      this.speakLocalText(reply);
    }, 400);
  }

  speakLocalText(text) {
    if (!('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    const cleanText = text.replace(/<[^>]*>/g, '');
    const utterance = new SpeechSynthesisUtterance(cleanText);
    utterance.lang = 'vi-VN';
    utterance.rate = 1.0;
    utterance.pitch = 1.1;

    const voices = window.speechSynthesis.getVoices();
    const viVoice = voices.find(v => v.lang.includes('vi') || v.lang.includes('VN'));
    if (viVoice) utterance.voice = viVoice;

    utterance.onstart = () => this.setSpeaking(true);
    utterance.onend = () => {
      this.setSpeaking(false);
      if (this.handsFree) setTimeout(() => this.startRecording(), 600);
    };
    utterance.onerror = () => this.setSpeaking(false);

    window.speechSynthesis.speak(utterance);
  }
}

window.addEventListener('DOMContentLoaded', () => {
  window.lily = new LilyPWA();
  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('./sw.js').catch(() => {});
  }
});
