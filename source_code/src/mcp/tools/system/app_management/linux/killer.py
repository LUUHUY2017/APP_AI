# Source Generated with Decompyle++
# File: killer.pyc (Python 3.12)

'''Linux系统应用程序关闭器.

提供Linux平台下的应用程序关闭功能
'''
import subprocess
from typing import Any, Dict, List
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

def list_running_applications(filter_name = None):
    '''
    列出Linux上正在运行的应用程序.
    '''
    apps = []
    
    try:
        result = subprocess.run([
            'ps',
            '-eo',
            'pid,ppid,comm,command',
            '--no-headers'], capture_output = True, text = True, timeout = 10)
        if result.returncode == 0:
            lines = result.stdout.strip().split('\n')
            for line in lines:
                parts = line.strip().split(None, 3)
                if not len(parts) >= 4:
                    continue
                    
                    try:
                        (pid, ppid, comm, command) = parts
                        if not command.startswith('/usr/bin/'):
                            not command.startswith('/usr/bin/')
                            if not command.startswith('/bin/'):
                                not command.startswith('/bin/')
                                if not command.startswith('['):
                                    not command.startswith('[')
                        is_gui_app = len(comm) > 2
                        if not is_gui_app:
                            continue
                            
                            try:
                                app_name = comm
                                if not filter_name and filter_name.lower() in app_name.lower():
                                    continue
                                    
                                    try:
                                        apps.append({
                                            'pid': int(pid),
                                            'ppid': int(ppid),
                                            'name': app_name,
                                            'display_name': app_name,
                                            'command': command,
                                            'type': 'application' })
                                        continue
                                        return apps
                                    except (subprocess.TimeoutExpired, subprocess.SubprocessError):
                                        e = None
                                        logger.warning(f'''[LinuxKiller] Linux进程扫描失败: {e}''')
                                        e = None
                                        del e
                                        return apps
                                        e = None
                                        del e






def kill_application(pid = None, force = None):
    '''
    在Linux上关闭应用程序.
    '''
    
    try:
        logger.info(f'''[LinuxKiller] 尝试关闭Linux应用程序，PID: {pid}, 强制关闭: {force}''')
        if force:
            result = subprocess.run([
                'kill',
                '-9',
                str(pid)], capture_output = True, timeout = 5)
        else:
            result = subprocess.run([
                'kill',
                '-15',
                str(pid)], capture_output = True, timeout = 5)
        success = result.returncode == 0
        if success:
            logger.info(f'''[LinuxKiller] 成功关闭应用程序，PID: {pid}''')
            return success
        None.warning(f'''[LinuxKiller] 关闭应用程序失败，PID: {pid}''')
        return success
    except (subprocess.TimeoutExpired, subprocess.SubprocessError):
        e = None
        logger.error(f'''[LinuxKiller] Linux关闭应用程序失败: {e}''')
        e = None
        del e
        return False
        e = None
        del e


