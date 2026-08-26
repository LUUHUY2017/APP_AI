# Source Generated with Decompyle++
# File: manager.pyc (Python 3.12)

from typing import Any, List
from base import Plugin

class PluginManager:
    '''
    轻量插件管理器：统一setup/start/stop/shutdown广播；错误隔离。
    '''
    
    def __init__(self = None):
        self._plugins = []
        self._by_name = { }

    
    def register(self = None, *plugins):
        for p in plugins:
            if not p not in self._plugins:
                continue
            self._plugins.append(p)
            name = getattr(p, 'name', None)
            if isinstance(name, str) and name:
                self._by_name[name] = p
        continue
        return None
        except Exception:
            continue

    
    def get_plugin(self = None, name = None):
        '''
        根据插件名获取插件实例。返回 None 表示未注册。
        '''
        
        try:
            return self._by_name.get(name)
        except Exception:
            return None


    
    async def setup_all(self = None, app = None):
        pass
    # WARNING: Decompyle incomplete

    
    async def start_all(self = None):
        pass
    # WARNING: Decompyle incomplete

    
    async def notify_protocol_connected(self = None, protocol = None):
        pass
    # WARNING: Decompyle incomplete

    
    async def notify_incoming_json(self = None, message = None):
        pass
    # WARNING: Decompyle incomplete

    
    async def notify_incoming_audio(self = None, data = None):
        pass
    # WARNING: Decompyle incomplete

    
    async def notify_device_state_changed(self = None, state = None):
        pass
    # WARNING: Decompyle incomplete

    
    async def stop_all(self = None):
        pass
    # WARNING: Decompyle incomplete

    
    async def shutdown_all(self = None):
        pass
    # WARNING: Decompyle incomplete


