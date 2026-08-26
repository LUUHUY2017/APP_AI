# Source Generated with Decompyle++
# File: shortcuts.pyc (Python 3.12)

import asyncio
import time
from dataclasses import dataclass
from typing import Any, Dict, Optional, Set
from src.constants.constants import AbortReason
from src.plugins.base import Plugin
from src.utils.config_manager import ConfigManager
from src.utils.logging_config import get_logger
logger = get_logger(__name__)
ShortcutConfig = <NODE:12>()

class _AppAdapter:
    '''
    Adapter cho ứng dụng chính, cung cấp các phương thức bất đồng bộ
    để tương tác với chức năng lắng nghe và hội thoại.
    '''
    
    def __init__(self = None, app = None):
        self._app = app

    
    async def start_listening(self):
        '''
        Bắt đầu lắng nghe thủ công.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def stop_listening(self):
        '''
        Dừng lắng nghe thủ công.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def toggle_chat_state(self):
        '''
        Bật/tắt chế độ trò chuyện tự động.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def abort_speaking(self, reason):
        '''
        Ngắt lời nói hiện tại với lý do.
        '''
        pass
    # WARNING: Decompyle incomplete



class PluginShortcutManager:
    '''
    Trình quản lý phím tắt toàn cục tích hợp trong plugin 
    (thay thế views/components/shortcut_manager.py).
    '''
    
    def __init__(self = None, loop = None):
        self._main_loop = loop
        self.config = ConfigManager.get_instance()
        if not self.config.get_config('SHORTCUTS', { }):
            self.config.get_config('SHORTCUTS', { })
        self.shortcuts_config = { }
        self.enabled = bool(self.shortcuts_config.get('ENABLED', True))
        self.pressed_keys = set()
        self.manual_press_active = False
        self.running = False
        self._listener = None
        self._health_check_task = None
        self._restart_in_progress = False
        self._last_activity_time = 0
        self.application = None
        self.display = None
    # WARNING: Decompyle incomplete

    
    def _load_shortcuts(self):
        '''
        Tải cấu hình phím tắt từ file cấu hình.
        '''
        self.shortcuts.clear()
        for name in ('MANUAL_PRESS', 'AUTO_TOGGLE', 'ABORT', 'MODE_TOGGLE', 'WINDOW_TOGGLE'):
            if not self.shortcuts_config.get(name, { }):
                self.shortcuts_config.get(name, { })
            cfg = { }
            modifier = str(cfg.get('modifier', 'ctrl')).lower()
            key = str(cfg.get('key', '')).lower()
            self.shortcuts[name] = ShortcutConfig(modifier = modifier, key = key)

    
    async def start(self = None):
        '''
        Bắt đầu lắng nghe phím tắt toàn cục.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def stop(self):
        '''
        Dừng lắng nghe phím tắt toàn cục.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def reload_from_config(self):
        '''
        Tải lại cấu hình phím tắt từ file cấu hình.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _on_key_press(self, key):
        '''
        Xử lý sự kiện nhấn phím.
        '''
        if not self.running:
            return None
        self._last_activity_time = time.time()
        name = self._get_key_name(key)
        if not name:
            return None
        self.pressed_keys.add(name)
        self._check_shortcuts(True)

    
    def _on_key_release(self, key):
        '''
        Xử lý sự kiện nhả phím.
        '''
        if not self.running:
            return None
        self._last_activity_time = time.time()
        name = self._get_key_name(key)
        if not name:
            return None
        if name in self.pressed_keys:
            self.pressed_keys.remove(name)
        if self.manual_press_active and len(self.pressed_keys) == 0 and self.application:
            self._run_coroutine_threadsafe(self.application.stop_listening())
            self.manual_press_active = False
        self._check_shortcuts(False)

    
    def _get_key_name(self = None, key = None):
        '''
        Lấy tên phím từ đối tượng key.
        '''
        
        try:
            if hasattr(key, 'name'):
                if key.name in ('ctrl_l', 'ctrl_r'):
                    return 'ctrl'
                    
                    try:
                        if key.name in ('alt_l', 'alt_r'):
                            return 'alt'
                            
                            try:
                                if key.name in ('shift_l', 'shift_r'):
                                    return 'shift'
                                    
                                    try:
                                        if key.name == 'cmd':
                                            return 'cmd'
                                            
                                            try:
                                                if key.name == 'esc':
                                                    return 'esc'
                                                    
                                                    try:
                                                        if key.name == 'enter':
                                                            return 'enter'
                                                            
                                                            try:
                                                                return key.name.lower()
                                                                
                                                                try:
                                                                    if hasattr(key, 'char') and key.char:
                                                                        if key.char == '\n':
                                                                            return 'enter'
                                                                            
                                                                            try:
                                                                                if key.char in self.key_mapping:
                                                                                    return self.key_mapping[key.char]
                                                                                return None.char.lower()
                                                                                return None
                                                                            except Exception:
                                                                                return None










    
    def _check_shortcuts(self = None, is_press = None):
        '''
        Kiểm tra các phím tắt đang nhấn.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _match(self, cfg, ctrl = None, alt = None, shift = None, cmd = ('cfg', ShortcutConfig, 'ctrl', bool, 'alt', bool, 'shift', bool, 'cmd', bool, 'return', bool)):
        '''
        Kiểm tra xem phím tắt có khớp với tổ hợp phím hiện tại không.
        '''
        if not cfg.modifier == 'ctrl' and ctrl:
            return False
        if not cfg.modifier == 'alt' and alt:
            return False
        if not cfg.modifier == 'shift' and shift:
            return False
        if not cfg.modifier == 'cmd' and cmd:
            return False
    # WARNING: Decompyle incomplete

    
    def _handle(self = None, kind = None, is_press = None):
        '''
        Xử lý hành vi của từng phím tắt.
        '''
        if kind == 'MANUAL_PRESS':
            if is_press and self.manual_press_active and self.application:
                self._run_coroutine_threadsafe(self.application.start_listening())
                self.manual_press_active = True
                return None
            if is_press and self.manual_press_active and self.application:
                self._run_coroutine_threadsafe(self.application.stop_listening())
                self.manual_press_active = False
            return None
        if kind == 'ABORT':
            if is_press and self.application:
                self._run_coroutine_threadsafe(self.application.abort_speaking(AbortReason.NONE))
            return None
        if kind == 'AUTO_TOGGLE' and is_press and self.application:
            self._run_coroutine_threadsafe(self.application.toggle_chat_state())
            return None
        if kind == 'MODE_TOGGLE' and is_press and self.display:
            self._run_coroutine_threadsafe(self.display.toggle_mode())
            return None
        if kind == 'WINDOW_TOGGLE':
            if is_press:
                if self.display:
                    print('Hiển thị/ẩn giao diện')
                    self._run_coroutine_threadsafe(self.display.toggle_window_visibility())
                    return None
                return None
            return None

    
    def _run_coroutine_threadsafe(self, coro):
        '''
        Chạy coroutine trong vòng lặp asyncio chính từ thread khác.
        '''
        
        try:
            if self._main_loop:
                if self.running:
                    asyncio.run_coroutine_threadsafe(coro, self._main_loop)
                    return None
                return None
            return None
        except Exception:
            return None


    
    def _start_health_check_task(self):
        '''
        Bắt đầu task kiểm tra sức khỏe phím tắt định kỳ.
        '''
        if self._main_loop:
            if not self._health_check_task:
                self._health_check_task = asyncio.run_coroutine_threadsafe(self._health_check_loop(), self._main_loop)
                return None
            return None

    
    async def _health_check_loop(self):
        '''
        Vòng lặp kiểm tra sức khỏe phím tắt (heartbeat nhẹ).
        '''
        pass
    # WARNING: Decompyle incomplete



class ShortcutsPlugin(Plugin):
    pass
# WARNING: Decompyle incomplete

