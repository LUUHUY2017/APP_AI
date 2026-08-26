# Source Generated with Decompyle++
# File: scanner.pyc (Python 3.12)

'''Windows应用程序扫描器.

专门用于Windows系统的应用程序扫描和管理
'''
import json
import os
import platform
import subprocess
from typing import Dict, List, Optional
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

def scan_installed_applications():
    '''扫描Windows系统中已安装的应用程序.

    Returns:
        List[Dict[str, str]]: 应用程序列表
    '''
    if platform.system() != 'Windows':
        return []
    apps = None
# WARNING: Decompyle incomplete


def scan_running_applications():
    '''扫描Windows系统中正在运行的应用程序.

    Returns:
        List[Dict[str, str]]: 正在运行的应用程序列表
    '''
    if platform.system() != 'Windows':
        return []
    apps = None
# WARNING: Decompyle incomplete


def _scan_main_start_menu_apps():
    '''
    扫描开始菜单中的主要应用程序（过滤系统组件和辅助工具）.
    '''
    apps = []
    start_menu_paths = [
        os.path.join(os.environ.get('PROGRAMDATA', ''), 'Microsoft', 'Windows', 'Start Menu', 'Programs'),
        os.path.join(os.environ.get('APPDATA', ''), 'Microsoft', 'Windows', 'Start Menu', 'Programs')]
    for start_path in start_menu_paths:
        if not os.path.exists(start_path):
            continue
        for root, dirs, files in os.walk(start_path):
            for file in files:
                if not file.lower().endswith('.lnk'):
                    continue
                shortcut_path = os.path.join(root, file)
                display_name = file[:-4]
                if _should_include_app(display_name):
                    clean_name = _clean_app_name(display_name)
                    target_path = _resolve_shortcut_target(shortcut_path)
                    if not target_path:
                        target_path
                    apps.append({
                        'name': clean_name,
                        'display_name': display_name,
                        'path': shortcut_path,
                        'type': 'shortcut' })
    continue
    return apps
    except Exception:
        e = None
        logger.debug(f'''[WindowsScanner] 处理快捷方式失败 {file}: {e}''')
        e = None
        del e
        continue
        e = None
        del e
    except Exception:
        e = None
        logger.debug(f'''[WindowsScanner] 扫描开始菜单失败 {start_path}: {e}''')
        e = None
        del e
        continue
        e = None
        del e


def _scan_main_registry_apps():
    '''
    扫描注册表中的主要应用程序（过滤系统组件）.
    '''
    apps = []
    
    try:
        powershell_cmd = [
            'powershell',
            '-Command',
            'Get-ItemProperty HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\* | Select-Object DisplayName, InstallLocation, Publisher | Where-Object {$_.DisplayName -ne $null} | ConvertTo-Json']
        result = subprocess.run(powershell_cmd, capture_output = True, text = True, timeout = 30)
        if result.returncode == 0 and result.stdout:
            
            try:
                installed_apps = json.loads(result.stdout)
                if isinstance(installed_apps, dict):
                    installed_apps = [
                        installed_apps]
                for app in installed_apps:
                    display_name = app.get('DisplayName', '')
                    publisher = app.get('Publisher', '')
                    if not display_name:
                        continue
                        
                        try:
                            if not _should_include_app(display_name, publisher):
                                continue
                                
                                try:
                                    clean_name = _clean_app_name(display_name)
                                    apps.append({
                                        'name': clean_name,
                                        'display_name': display_name,
                                        'path': app.get('InstallLocation', ''),
                                        'type': 'installed' })
                                    continue
                                    return apps
                                    return apps
                                except json.JSONDecodeError:
                                    logger.warning('[WindowsScanner] 无法解析PowerShell输出')
                                    
                                    try:
                                        return apps
                                        
                                        try:
                                            pass
                                        except (subprocess.TimeoutExpired, subprocess.SubprocessError):
                                            e = None
                                            logger.warning(f'''[WindowsScanner] PowerShell扫描失败: {e}''')
                                            e = None
                                            del e
                                            return apps
                                            e = None
                                            del e








def _should_include_app(display_name = None, publisher = None):
    '''判断是否应该包含该应用程序.

    Args:
        display_name: 应用程序显示名称
        publisher: 发布者（可选）

    Returns:
        bool: 是否应该包含
    '''
    pass
# WARNING: Decompyle incomplete


def _should_include_process(image_name = None, window_title = None):
    '''判断是否应该包含该进程.

    Args:
        image_name: 进程映像名称
        window_title: 窗口标题

    Returns:
        bool: 是否包含
    '''
    system_processes = {
        'dwm.exe',
        'lsm.exe',
        'smss.exe',
        'csrss.exe',
        'lsass.exe',
        'audiodg.exe',
        'conhost.exe',
        'dllhost.exe',
        'msiexec.exe',
        'spoolsv.exe',
        'svchost.exe',
        'wininit.exe',
        'explorer.exe',
        'rundll32.exe',
        'services.exe',
        'winlogon.exe',
        'taskhostw.exe'}
    image_lower = image_name.lower()
    if image_lower in system_processes:
        return False
    if window_title or window_title == 'N/A':
        return False
    if len(window_title.strip()) < 3:
        return False
    return True


def _extract_app_name(image_name = None, window_title = None):
    '''从进程信息中提取应用程序名称.

    Args:
        image_name: 进程映像名称
        window_title: 窗口标题

    Returns:
        str: 应用程序名称
    '''
    if window_title and window_title != 'N/A' and len(window_title.strip()) > 0:
        return window_title.strip()
    if None.lower().endswith('.exe'):
        return image_name[:-4]


def _resolve_shortcut_target(shortcut_path = None):
    '''解析Windows快捷方式的目标路径.

    Args:
        shortcut_path: 快捷方式文件路径

    Returns:
        目标路径，如果解析失败则返回None
    '''
    
    try:
        import win32com.client as win32com
        shell = win32com.client.Dispatch('WScript.Shell')
        shortcut = shell.CreateShortCut(shortcut_path)
        target_path = shortcut.Targetpath
        if target_path and os.path.exists(target_path):
            return target_path
    except ImportError:
        logger.debug('[WindowsScanner] win32com模块不可用，无法解析快捷方式')
        return None
        except Exception:
            e = None
            logger.debug(f'''[WindowsScanner] 解析快捷方式失败: {e}''')
            e = None
            del e
            return None
            e = None
            del e



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

