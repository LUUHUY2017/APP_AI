# Source Generated with Decompyle++
# File: base.pyc (Python 3.12)

import asyncio
from typing import Any

class Plugin:
    '''
    最小插件基类：提供异步生命周期钩子。按需覆写。
    '''
    name: str = 'plugin'
    
    def __init__(self = None):
        self._started = False

    
    async def setup(self = None, app = None):
        '''
        插件准备阶段（在应用 run 早期调用）。
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def start(self = None):
        '''
        插件启动（通常在协议连接建立后调用）。
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def on_protocol_connected(self = None, protocol = None):
        '''
        协议通道建立后的通知。
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def on_incoming_json(self = None, message = None):
        '''
        收到JSON消息时的通知。
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def on_incoming_audio(self = None, data = None):
        '''
        收到音频数据时的通知。
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def on_device_state_changed(self = None, state = None):
        '''
        设备状态变更通知（由应用广播）。
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def stop(self = None):
        '''
        插件停止（在应用 shutdown 前调用）。
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def shutdown(self = None):
        '''
        插件最终清理（在应用 shutdown 过程中调用）。
        '''
        pass
    # WARNING: Decompyle incomplete


