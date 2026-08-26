# Source Generated with Decompyle++
# File: application.pyc (Python 3.12)

import asyncio
import sys
import threading
from pathlib import Path
from typing import Any, Awaitable

try:
    project_root = Path(__file__).resolve().parents[1]
    if str(project_root) not in sys.path:
        sys.path.insert(0, str(project_root))
    from src.constants.constants import DeviceState, ListeningMode
    from src.plugins.calendar import CalendarPlugin
    from src.plugins.iot import IoTPlugin
    from src.plugins.manager import PluginManager
    from src.plugins.mcp import McpPlugin
    from src.plugins.shortcuts import ShortcutsPlugin
    from src.plugins.ui import UIPlugin
    from src.plugins.wake_word import WakeWordPlugin
    from src.protocols.mqtt_protocol import MqttProtocol
    from src.protocols.websocket_protocol import WebsocketProtocol
    from src.utils.config_manager import ConfigManager
    from src.utils.logging_config import get_logger
    from src.utils.opus_loader import setup_opus
    logger = get_logger(__name__)
    setup_opus()
    
    class Application:
        _instance = None
        _lock = threading.Lock()
        get_instance = (lambda cls: pass# WARNING: Decompyle incomplete
)()
        
        def __init__(self):
            pass
        # WARNING: Decompyle incomplete

        
        async def run(self = None, *, protocol, mode):
            pass
        # WARNING: Decompyle incomplete

        
        async def connect_protocol(self):
            '''
        确保协议通道打开并广播一次协议就绪。返回是否已打开。
        '''
            pass
        # WARNING: Decompyle incomplete

        
        def _initialize_async_objects(self = None):
            logger.debug('初始化异步对象')
            self._shutdown_event = asyncio.Event()
            self._state_lock = asyncio.Lock()
            self._connect_lock = asyncio.Lock()

        
        def _set_protocol(self = None, protocol_type = None):
            logger.debug('设置协议类型: %s', protocol_type)
            if protocol_type == 'mqtt':
                self.protocol = MqttProtocol(asyncio.get_running_loop())
                return None
            self.protocol = WebsocketProtocol()

        
        async def start_listening_manual(self = None):
            pass
        # WARNING: Decompyle incomplete

        
        async def stop_listening_manual(self = None):
            pass
        # WARNING: Decompyle incomplete

        
        async def start_auto_conversation(self = None):
            pass
        # WARNING: Decompyle incomplete

        
        def _setup_protocol_callbacks(self = None):
            self.protocol.on_network_error(self._on_network_error)
            self.protocol.on_incoming_json(self._on_incoming_json)
            self.protocol.on_incoming_audio(self._on_incoming_audio)
            self.protocol.on_audio_channel_opened(self._on_audio_channel_opened)
            self.protocol.on_audio_channel_closed(self._on_audio_channel_closed)

        
        async def _wait_shutdown(self = None):
            pass
        # WARNING: Decompyle incomplete

        
        def spawn(self = None, coro = None, name = None):
            '''
        创建任务并登记，关停时统一取消。
        '''
            pass
        # WARNING: Decompyle incomplete

        
        def schedule_command_nowait(self = None, fn = None, *args, **kwargs):
            '''简化的“立即调度”：把任意可调用丢回主loop执行。

        - 若返回协程，会被自动创建子任务执行（fire-and-forget）。
        - 若是同步函数，直接在事件循环线程里运行（尽量保持轻量）。
        '''
            pass
        # WARNING: Decompyle incomplete

        
        def _on_network_error(self, error_message = (None,)):
            if error_message:
                logger.error(error_message)
            self.keep_listening = False

        
        def _on_incoming_audio(self = None, data = None):
            logger.debug(f'''收到二进制消息，长度: {len(data)}''')
            self.spawn(self.plugins.notify_incoming_audio(data), 'plugin:on_audio')

        
        def _on_incoming_json(self, json_data):
            pass
        # WARNING: Decompyle incomplete

        
        async def _on_audio_channel_opened(self):
            pass
        # WARNING: Decompyle incomplete

        
        async def _on_audio_channel_closed(self):
            pass
        # WARNING: Decompyle incomplete

        
        async def set_device_state(self = None, state = None):
            '''
        仅供主程序内部调用：设置设备状态。插件请只读获取。
        '''
            pass
        # WARNING: Decompyle incomplete

        
        def get_device_state(self):
            return self.device_state

        
        def is_idle(self = None):
            return self.device_state == DeviceState.IDLE

        
        def is_listening(self = None):
            return self.device_state == DeviceState.LISTENING

        
        def is_speaking(self = None):
            return self.device_state == DeviceState.SPEAKING

        
        def get_listening_mode(self):
            return self.listening_mode

        
        def is_keep_listening(self = None):
            return bool(self.keep_listening)

        
        def is_audio_channel_opened(self = None):
            
            try:
                if self.protocol:
                    self.protocol
                return bool(self.protocol.is_audio_channel_opened())
            except Exception:
                return False


        
        def get_state_snapshot(self = None):
            return {
                'device_state': self.device_state,
                'listening_mode': self.listening_mode,
                'keep_listening': bool(self.keep_listening),
                'audio_opened': self.is_audio_channel_opened() }

        
        async def abort_speaking(self, reason):
            '''
        中止语音输出.
        '''
            pass
        # WARNING: Decompyle incomplete

        
        def set_chat_message(self = None, role = None, message = None):
            '''将文本更新转发为 UI 可识别的 JSON 消息（复用 UIPlugin 的 on_incoming_json）。
        role: "assistant" | "user" 影响消息类型映射。
        '''
            
            try:
                msg_type = 'tts' if str(role).lower() == 'assistant' else 'stt'
                payload = {
                    'type': msg_type,
                    'text': message }
                self.spawn(self.plugins.notify_incoming_json(payload), 'ui:text_update')
                return None
            except Exception:
                msg_type = 'tts'
                continue


        
        def set_emotion(self = None, emotion = None):
            '''
        设置情绪表情：通过 UIPlugin 的 on_incoming_json 路由。
        '''
            payload = {
                'type': 'llm',
                'emotion': emotion }
            self.spawn(self.plugins.notify_incoming_json(payload), 'ui:emotion_update')

        
        async def shutdown(self):
            pass
        # WARNING: Decompyle incomplete


    return None
except Exception:
    continue

