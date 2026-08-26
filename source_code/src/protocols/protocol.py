# Source Generated with Decompyle++
# File: protocol.pyc (Python 3.12)

import json
from src.constants.constants import AbortReason, ListeningMode
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

class Protocol:
    
    def __init__(self):
        self.session_id = ''
        self._on_incoming_json = None
        self._on_incoming_audio = None
        self._on_audio_channel_opened = None
        self._on_audio_channel_closed = None
        self._on_network_error = None
        self._on_connection_state_changed = None
        self._on_reconnecting = None

    
    def on_incoming_json(self, callback):
        '''
        设置JSON消息接收回调函数.
        '''
        self._on_incoming_json = callback

    
    def on_incoming_audio(self, callback):
        '''
        设置音频数据接收回调函数.
        '''
        self._on_incoming_audio = callback

    
    def on_audio_channel_opened(self, callback):
        '''
        设置音频通道打开回调函数.
        '''
        self._on_audio_channel_opened = callback

    
    def on_audio_channel_closed(self, callback):
        '''
        设置音频通道关闭回调函数.
        '''
        self._on_audio_channel_closed = callback

    
    def on_network_error(self, callback):
        '''
        设置网络错误回调函数.
        '''
        self._on_network_error = callback

    
    def on_connection_state_changed(self, callback):
        '''设置连接状态变化回调函数.

        Args:
            callback: 回调函数，接收参数 (connected: bool, reason: str)
        '''
        self._on_connection_state_changed = callback

    
    def on_reconnecting(self, callback):
        '''设置重连尝试回调函数.

        Args:
            callback: 回调函数，接收参数 (attempt: int, max_attempts: int)
        '''
        self._on_reconnecting = callback

    
    async def send_text(self, message):
        '''
        发送文本消息的抽象方法，需要在子类中实现.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def send_audio(self = None, data = None):
        '''
        发送音频数据的抽象方法，需要在子类中实现.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def is_audio_channel_opened(self = None):
        '''
        检查音频通道是否打开的抽象方法，需要在子类中实现.
        '''
        raise NotImplementedError('is_audio_channel_opened方法必须由子类实现')

    
    async def open_audio_channel(self = None):
        '''
        打开音频通道的抽象方法，需要在子类中实现.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def close_audio_channel(self):
        '''
        关闭音频通道的抽象方法，需要在子类中实现.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def send_abort_speaking(self, reason):
        '''
        发送中止语音的消息.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def send_wake_word_detected(self, wake_word):
        '''
        发送检测到唤醒词的消息.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def send_start_listening(self, mode):
        '''
        发送开始监听的消息.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def send_stop_listening(self):
        '''
        发送停止监听的消息.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def send_iot_descriptors(self, descriptors):
        '''
        发送物联网设备描述信息.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def send_iot_states(self, states):
        '''
        发送物联网设备状态信息.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def send_mcp_message(self, payload):
        '''
        发送MCP消息.
        '''
        pass
    # WARNING: Decompyle incomplete


