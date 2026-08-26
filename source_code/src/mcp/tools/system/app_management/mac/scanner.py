# Source Generated with Decompyle++
# File: scanner.pyc (Python 3.12)

'''macOS应用程序扫描器.

专门用于macOS系统的应用程序扫描和管理
'''
import platform
import subprocess
from pathlib import Path
from typing import Dict, List
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

def scan_installed_applications():
    '''扫描macOS系统中已安装的应用程序.

    Returns:
        List[Dict[str, str]]: 应用程序列表
    '''
    if platform.system() != 'Darwin':
        return []
    apps = None
    applications_dir = Path('/Applications')
    if applications_dir.exists():
        for app_path in applications_dir.glob('*.app'):
            app_name = app_path.stem
            clean_name = _clean_app_name(app_name)
            apps.append({
                'name': clean_name,
                'display_name': app_name,
                'path': str(app_path),
                'type': 'application' })
    user_apps_dir = Path.home() / 'Applications'
    if user_apps_dir.exists():
        for app_path in user_apps_dir.glob('*.app'):
            app_name = app_path.stem
            clean_name = _clean_app_name(app_name)
            apps.append({
                'name': clean_name,
                'display_name': app_name,
                'path': str(app_path),
                'type': 'user_application' })
    system_apps = [
        {
            'name': 'Calculator',
            'display_name': '计算器',
            'path': 'Calculator',
            'type': 'system' },
        {
            'name': 'TextEdit',
            'display_name': '文本编辑',
            'path': 'TextEdit',
            'type': 'system' },
        {
            'name': 'Preview',
            'display_name': '预览',
            'path': 'Preview',
            'type': 'system' },
        {
            'name': 'Safari',
            'display_name': 'Safari浏览器',
            'path': 'Safari',
            'type': 'system' },
        {
            'name': 'Finder',
            'display_name': '访达',
            'path': 'Finder',
            'type': 'system' },
        {
            'name': 'Terminal',
            'display_name': '终端',
            'path': 'Terminal',
            'type': 'system' },
        {
            'name': 'System Preferences',
            'display_name': '系统偏好设置',
            'path': 'System Preferences',
            'type': 'system' }]
    apps.extend(system_apps)
    logger.info(f'''[MacScanner] 扫描完成，找到 {len(apps)} 个应用程序''')
    return apps


def scan_running_applications():
    '''扫描macOS系统中正在运行的应用程序.

    Returns:
        List[Dict[str, str]]: 正在运行的应用程序列表
    '''
    if platform.system() != 'Darwin':
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
                                logger.info(f'''[MacScanner] 找到 {len(apps)} 个正在运行的应用程序''')
                                return apps
                            except Exception:
                                e = None
                                logger.error(f'''[MacScanner] 扫描运行应用失败: {e}''')
                                del e
                                return None
                                None = 
                                del e





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
    if '.app/Contents/MacOS/' in command:
        
        try:
            app_path = command.split('.app/Contents/MacOS/')[0] + '.app'
            app_name = Path(app_path).name.replace('.app', '')
            return app_name
            if '/Applications/' in command:
                
                try:
                    parts = command.split('/Applications/')[1].split('/')[0]
                    if parts.endswith('.app'):
                        return parts.replace('.app', '')
                    if comm:
                        return comm
                    return None
                    except (IndexError, AttributeError):
                        continue
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

