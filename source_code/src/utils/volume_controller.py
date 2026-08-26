# Source Generated with Decompyle++
# File: volume_controller.pyc (Python 3.12)

import platform
import re
import shutil
import subprocess
from functools import wraps
from typing import Any, Callable, List, Optional
from src.utils.logging_config import get_logger

class VolumeController:
    '''
    跨平台音量控制器.
    '''
    DEFAULT_VOLUME = 70
    PLATFORM_INIT = {
        'Windows': '_init_windows',
        'Darwin': '_init_macos',
        'Linux': '_init_linux' }
    VOLUME_METHODS = {
        'Windows': ('_get_windows_volume', '_set_windows_volume'),
        'Darwin': ('_get_macos_volume', '_set_macos_volume'),
        'Linux': ('_get_linux_volume', '_set_linux_volume') }
    LINUX_VOLUME_METHODS = {
        'pactl': ('_get_pactl_volume', '_set_pactl_volume'),
        'wpctl': ('_get_wpctl_volume', '_set_wpctl_volume'),
        'amixer': ('_get_amixer_volume', '_set_amixer_volume'),
        'alsamixer': (None, '_set_alsamixer_volume') }
    PLATFORM_MODULES = {
        'Windows': {
            'pycaw': 'pycaw.pycaw',
            'comtypes': 'comtypes',
            'ctypes': 'ctypes' },
        'Darwin': {
            'applescript': 'applescript' },
        'Linux': { } }
    
    def __init__(self):
        '''
        初始化音量控制器.
        '''
        self.logger = get_logger('VolumeController')
        self.system = platform.system()
        self.is_arm = platform.machine().startswith(('arm', 'aarch'))
        self.linux_tool = None
        self._module_cache = { }
        init_method_name = self.PLATFORM_INIT.get(self.system)
        if init_method_name:
            init_method = getattr(self, init_method_name)
            init_method()
            return None
        self.logger.warning(f'''不支持的操作系统: {self.system}''')
        raise NotImplementedError(f'''不支持的操作系统: {self.system}''')

    
    def _lazy_import(self = None, module_name = None, attr = None):
        '''懒加载模块，支持缓存和属性导入.

        Args:
            module_name: 模块名称
            attr: 可选，模块中的属性名

        Returns:
            导入的模块或属性
        '''
        if module_name in self._module_cache:
            module = self._module_cache[module_name]
        else:
            
            try:
                module = __import__(module_name, fromlist = [
                    '*'] if '.' in module_name else [])
                self._module_cache[module_name] = module
                if attr:
                    return getattr(module, attr)
                return None
            except ImportError:
                e = None
                self.logger.warning(f'''导入模块 {module_name} 失败: {e}''')
                raise 
                e = None
                del e


    
    def _safe_execute(self = None, func_name = None, default_return = None):
        '''
        安全执行函数的装饰器.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _run_command(self = None, cmd = None, check = None):
        '''
        通用命令执行方法.
        '''
        
        try:
            return subprocess.run(cmd, capture_output = True, text = True, check = check)
        except Exception:
            e = None
            self.logger.debug(f'''执行命令失败 {' '.join(cmd)}: {e}''')
            e = None
            del e
            return None
            e = None
            del e


    
    def _init_windows(self = None):
        '''
        初始化Windows音量控制.
        '''
        
        try:
            POINTER = self._lazy_import('ctypes', 'POINTER')
            cast = self._lazy_import('ctypes', 'cast')
            CLSCTX_ALL = self._lazy_import('comtypes', 'CLSCTX_ALL')
            AudioUtilities = self._lazy_import('pycaw.pycaw', 'AudioUtilities')
            IAudioEndpointVolume = self._lazy_import('pycaw.pycaw', 'IAudioEndpointVolume')
            self.devices = AudioUtilities.GetSpeakers()
            interface = self.devices.Activate(IAudioEndpointVolume._iid_, CLSCTX_ALL, None)
            self.volume_control = cast(interface, POINTER(IAudioEndpointVolume))
            self.logger.debug('Windows音量控制初始化成功')
            return None
        except Exception:
            e = None
            self.logger.error(f'''Windows音量控制初始化失败: {e}''')
            raise 
            e = None
            del e


    
    def _init_macos(self = None):
        '''
        初始化macOS音量控制.
        '''
        
        try:
            applescript = self._lazy_import('applescript')
            result = applescript.run('get volume settings')
            if result or result.code != 0:
                raise Exception('无法访问macOS音量控制')
            self.logger.debug('macOS音量控制初始化成功')
            return None
        except Exception:
            e = None
            self.logger.error(f'''macOS音量控制初始化失败: {e}''')
            raise 
            e = None
            del e


    
    def _init_linux(self = None):
        '''
        初始化Linux音量控制.
        '''
        linux_tools = [
            'pactl',
            'wpctl',
            'amixer']
        for tool in linux_tools:
            if not shutil.which(tool):
                continue
            self.linux_tool = tool
            linux_tools
        if self.linux_tool and shutil.which('alsamixer') and shutil.which('expect'):
            self.linux_tool = 'alsamixer'
        if not self.linux_tool:
            self.logger.error('未找到可用的Linux音量控制工具')
            raise Exception('未找到可用的Linux音量控制工具')
        self.logger.debug(f'''Linux音量控制初始化成功，使用: {self.linux_tool}''')

    
    def get_volume(self = None):
        '''
        获取当前音量 (0-100)
        '''
        (get_method_name, _) = self.VOLUME_METHODS.get(self.system, (None, None))
        if not get_method_name:
            return self.DEFAULT_VOLUME
        get_method = None(self, get_method_name)
        return get_method()

    
    def set_volume(self = None, volume = None):
        '''
        设置音量 (0-100)
        '''
        volume = max(0, min(100, volume))
        (_, set_method_name) = self.VOLUME_METHODS.get(self.system, (None, None))
        if set_method_name:
            set_method = getattr(self, set_method_name)
            set_method(volume)
            return None

    _get_windows_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    _set_windows_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    _get_macos_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    _set_macos_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    
    def _get_linux_volume(self = None):
        '''
        获取Linux音量.
        '''
        (get_method_name, _) = self.LINUX_VOLUME_METHODS.get(self.linux_tool, (None, None))
        if not get_method_name:
            return self.DEFAULT_VOLUME
        get_method = None(self, get_method_name)
        return get_method()

    
    def _set_linux_volume(self = None, volume = None):
        '''
        设置Linux音量.
        '''
        (_, set_method_name) = self.LINUX_VOLUME_METHODS.get(self.linux_tool, (None, None))
        if set_method_name:
            set_method = getattr(self, set_method_name)
            set_method(volume)
            return None

    _get_pactl_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    _set_pactl_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    _get_wpctl_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    _set_wpctl_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    _get_amixer_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    _set_amixer_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    _set_alsamixer_volume = (lambda self = None: pass# WARNING: Decompyle incomplete
)()
    check_dependencies = (lambda : system = platform.system()missing = []VolumeController._check_python_modules(system, missing)if system == 'Linux':
VolumeController._check_linux_tools(missing)VolumeController._report_missing_dependencies(system, missing))()
    _check_python_modules = (lambda system = None, missing = None: if system == 'Windows':
for module in ('pycaw', 'comtypes'):
__import__(module)Noneif system == 'Darwin':
try:
__import__('applescript')NoneNoneexcept ImportError:
missing.append(module)continueexcept ImportError:
missing.append('applescript')None)()
    _check_linux_tools = (lambda missing = None: tools = [
'pactl',
'wpctl',
'amixer',
'alsamixer']found = (lambda .0: pass# WARNING: Decompyle incomplete
)(tools())
        if not found:
            missing.append('pulseaudio-utils、wireplumber 或 alsa-utils')
            return None
)()
    _report_missing_dependencies = (lambda system = None, missing = None: if missing:
print(f'''警告: 音量控制需要以下依赖，但未找到: {', '.join(missing)}''')print('请使用以下命令安装缺少的依赖:')if system in ('Windows', 'Darwin'):
print('pip install ' + ' '.join(missing))Falseif system == 'Linux':
print('sudo apt-get install ' + ' '.join(missing))FalseTrue)()

