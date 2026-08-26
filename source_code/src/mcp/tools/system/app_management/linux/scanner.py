# Source Generated with Decompyle++
# File: scanner.pyc (Python 3.12)

'''Linux应用程序扫描器.

专门用于Linux系统的应用程序扫描和管理
'''
import platform
import subprocess
from pathlib import Path
from typing import Dict, List
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

def scan_installed_applications():
    '''扫描Linux系统中已安装的应用程序.

    Returns:
        List[Dict[str, str]]: 应用程序列表
    '''
    if platform.system() != 'Linux':
        return []
    apps = None
    desktop_dirs = [
        '/usr/share/applications',
        '/usr/local/share/applications',
        Path.home() / '.local/share/applications']
    for desktop_dir in desktop_dirs:
        desktop_path = Path(desktop_dir)
        if not desktop_path.exists():
            continue
        for desktop_file in desktop_path.glob('*.desktop'):
            app_info = _parse_desktop_file(desktop_file)
            if app_info and _should_include_app(app_info['display_name']):
                apps.append(app_info)
    continue
    system_apps = [
        {
            'name': 'gedit',
            'display_name': '文本编辑器',
            'path': 'gedit',
            'type': 'system' },
        {
            'name': 'firefox',
            'display_name': 'Firefox浏览器',
            'path': 'firefox',
            'type': 'system' },
        {
            'name': 'gnome-calculator',
            'display_name': '计算器',
            'path': 'gnome-calculator',
            'type': 'system' },
        {
            'name': 'nautilus',
            'display_name': '文件管理器',
            'path': 'nautilus',
            'type': 'system' },
        {
            'name': 'gnome-terminal',
            'display_name': '终端',
            'path': 'gnome-terminal',
            'type': 'system' },
        {
            'name': 'gnome-control-center',
            'display_name': '设置',
            'path': 'gnome-control-center',
            'type': 'system' }]
    apps.extend(system_apps)
    logger.info(f'''[LinuxScanner] 扫描完成，找到 {len(apps)} 个应用程序''')
    return apps
    except Exception:
        e = None
        logger.debug(f'''[LinuxScanner] 解析desktop文件失败 {desktop_file}: {e}''')
        e = None
        del e
        continue
        e = None
        del e


def scan_running_applications():
    '''扫描Linux系统中正在运行的应用程序.

    Returns:
        List[Dict[str, str]]: 正在运行的应用程序列表
    '''
    if platform.system() != 'Linux':
        return []
    apps = None
    
    try:
        result = subprocess.run([
            'ps',
            '-eo',
            'pid,ppid,comm,command'], capture_output = True, text = True, timeout = 10)
        if result.returncode == 0:
            lines = result.stdout.strip().split('\n')[1:]
            for line in lines:
                parts = line.strip().split(None, 3)
                if not len(parts) >= 4:
                    continue
                    
                    try:
                        (pid, ppid, comm, command) = parts
                        if not _should_include_process(comm, command):
                            continue
                            
                            try:
                                display_name = _extract_app_name(comm, command)
                                clean_name = _clean_app_name(display_name)
                                apps.append({
                                    'pid': int(pid),
                                    'ppid': int(ppid),
                                    'name': clean_name,
                                    'display_name': display_name,
                                    'command': command,
                                    'type': 'application' })
                                continue
                                logger.info(f'''[LinuxScanner] 找到 {len(apps)} 个正在运行的应用程序''')
                                return apps
                            except Exception:
                                e = None
                                logger.error(f'''[LinuxScanner] 扫描运行应用失败: {e}''')
                                del e
                                return None
                                None = 
                                del e





def _parse_desktop_file(desktop_file = None):
    '''解析.desktop文件.

    Args:
        desktop_file: .desktop文件路径

    Returns:
        Dict[str, str]: 应用程序信息
    '''
    pass
# WARNING: Decompyle incomplete


def _should_include_app(display_name = None):
    '''判断是否应该包含该应用程序.

    Args:
        display_name: 应用程序显示名称

    Returns:
        bool: 是否包含
    '''
    if not display_name:
        return False
    exclude_patterns = [
        'gnome-',
        'kde-',
        'xfce-',
        'unity-',
        'gdb',
        'valgrind',
        'strace',
        'ltrace',
        'dconf',
        'gsettings',
        'xdg-',
        'desktop-file-',
        'help',
        'about',
        'preferences',
        'settings']
    display_lower = display_name.lower()
    for pattern in exclude_patterns:
        if not pattern in display_lower:
            continue
        exclude_patterns
        return False
    return True


def _should_include_process(comm = None, command = None):
    '''判断是否应该包含该进程.

    Args:
        comm: 进程名称
        command: 完整命令

    Returns:
        bool: 是否包含
    '''
    pass
# WARNING: Decompyle incomplete


def _extract_app_name(comm = None, command = None):
    '''从进程信息中提取应用程序名称.

    Args:
        comm: 进程名称
        command: 完整命令

    Returns:
        str: 应用程序名称
    '''
    if '/' in command:
        
        try:
            exec_path = command.split()[0]
            app_name = Path(exec_path).name
            if app_name.endswith('.py'):
                app_name = app_name[:-3]
                return app_name
            if None.endswith('.sh'):
                app_name = app_name[:-3]
            return app_name
            if comm:
                return comm
            return None
        except (IndexError, AttributeError):
            continue



def _clean_app_name(name = None):
    '''清理应用程序名称，移除版本号和特殊字符.

    Args:
        name: 原始名称

    Returns:
        str: 清理后的名称
    '''
    if not name:
        return ''
    import re
    name = re.sub('\\s+v?\\d+[\\.\\d]*', '', name)
    name = re.sub('\\s*\\(\\d+\\)', '', name)
    name = re.sub('\\s*\\[.*?\\]', '', name)
    name = ' '.join(name.split())
    return name.strip()

