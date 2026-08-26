# Source Generated with Decompyle++
# File: killer.pyc (Python 3.12)

'''macOS系统应用程序关闭器.

提供macOS平台下的应用程序关闭功能
'''
import json
import subprocess
from typing import Any, Dict, List
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

def list_running_applications(filter_name = None):
    '''列出macOS上正在运行的、有用户界面的应用程序.

    使用AppleScript (JXA) 来获取更精确的应用列表.
    '''
    apps = []
    script = '\n    ObjC.import(\'AppKit\');\n\n    function run() {\n        let procs = $.NSWorkspace.sharedWorkspace.runningApplications;\n        let apps = [];\n        for (let i = 0; i < procs.count; i++) {\n            let app = procs.objectAtIndex(i);\n            // NSApplicationActivationPolicyRegular are regular apps that appear in the Dock.\n            if (app.activationPolicy === $.NSApplicationActivationPolicyRegular) {\n                apps.push({\n                    \'name\': app.localizedName.js,\n                    \'pid\': app.processIdentifier,\n                    \'path\': app.bundleURL ? app.bundleURL.path.js : ""\n                });\n            }\n        }\n        return JSON.stringify(apps);\n    }\n    '
    
    try:
        result = subprocess.run([
            'osascript',
            '-l',
            'JavaScript',
            '-e',
            script], capture_output = True, text = True, timeout = 10, check = True)
        running_apps = json.loads(result.stdout)
        for app_info in running_apps:
            app_name = app_info.get('name', '')
            if not filter_name and filter_name.lower() in app_name.lower():
                continue
                
                try:
                    apps.append({
                        'pid': app_info.get('pid'),
                        'ppid': -1,
                        'name': app_name,
                        'display_name': app_name,
                        'command': app_info.get('path', ''),
                        'type': 'application' })
                    continue
                    logger.info(f'''[MacKiller] 使用JXA找到 {len(apps)} 个正在运行的应用程序''')
                    return apps
                except (subprocess.TimeoutExpired, subprocess.SubprocessError, FileNotFoundError, subprocess.CalledProcessError):
                    e = None
                    logger.warning(f'''[MacKiller] JXA进程扫描失败 ({e})，回退到ps命令''')
                    del e
                    return None
                    None = 
                    del e
                    except json.JSONDecodeError:
                        e = None
                        logger.error(f'''[MacKiller] 解析JXA输出失败 ({e})，回退到ps命令''')
                        del e
                        return None
                        None = 
                        del e




def _list_running_applications_ps(filter_name = None):
    '''
    列出macOS上正在运行的应用程序 (基于ps命令).
    '''
    pass
# WARNING: Decompyle incomplete


def kill_application(pid = None, force = None):
    '''
    在macOS上关闭应用程序.
    '''
    
    try:
        logger.info(f'''[MacKiller] 尝试关闭macOS应用程序，PID: {pid}, 强制关闭: {force}''')
        if force:
            result = subprocess.run([
                'kill',
                '-9',
                str(pid)], capture_output = True, text = True, timeout = 5)
        else:
            result = subprocess.run([
                'kill',
                '-15',
                str(pid)], capture_output = True, text = True, timeout = 5)
        success = result.returncode == 0
        if success:
            logger.info(f'''[MacKiller] 成功关闭应用程序，PID: {pid}''')
            return success
        None.warning(f'''[MacKiller] 关闭应用程序失败，PID: {pid}''')
        return success
    except (subprocess.TimeoutExpired, subprocess.SubprocessError):
        e = None
        logger.error(f'''[MacKiller] macOS关闭应用程序失败: {e}''')
        e = None
        del e
        return False
        e = None
        del e


