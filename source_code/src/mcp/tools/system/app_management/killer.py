# Source Generated with Decompyle++
# File: killer.pyc (Python 3.12)

'''统一的应用程序关闭器.

根据系统自动选择对应的关闭器实现
'''
import asyncio
import json
import platform
from typing import Any, Dict, List
from src.utils.logging_config import get_logger
from utils import AppMatcher
logger = get_logger(__name__)

async def kill_application(args = None):
    '''关闭应用程序.

    Args:
        args: 包含应用程序名称的参数字典
            - app_name: 应用程序名称
            - force: 是否强制关闭（可选，默认False）

    Returns:
        bool: 关闭是否成功
    '''
    pass
# WARNING: Decompyle incomplete


async def list_running_applications(args = None):
    '''列出所有正在运行的应用程序.

    Args:
        args: 包含列出参数的字典
            - filter_name: 过滤应用程序名称（可选）

    Returns:
        str: JSON格式的运行中应用程序列表
    '''
    pass
# WARNING: Decompyle incomplete


async def _find_running_applications(app_name = None):
    '''查找正在运行的匹配应用程序.

    Args:
        app_name: 要查找的应用程序名称

    Returns:
        匹配的正在运行应用程序列表
    '''
    pass
# WARNING: Decompyle incomplete


def _list_running_apps_sync(filter_name = None):
    '''同步列出正在运行的应用程序.

    Args:
        filter_name: 过滤应用程序名称

    Returns:
        正在运行的应用程序列表
    '''
    system = platform.system()
    if system == 'Darwin':
        list_running_applications = list_running_applications
        import mac.killer
        return list_running_applications(filter_name)
    if None == 'Windows':
        list_running_applications = list_running_applications
        import windows.killer
        return list_running_applications(filter_name)
    if None == 'Linux':
        list_running_applications = list_running_applications
        import linux.killer
        return list_running_applications(filter_name)
    None.warning(f'''[AppKiller] 不支持的操作系统: {system}''')
    return []


def _kill_app_sync(app = None, force = None, system = None):
    '''同步关闭应用程序.

    Args:
        app: 应用程序信息
        force: 是否强制关闭
        system: 操作系统类型

    Returns:
        bool: 关闭是否成功
    '''
    
    try:
        pid = app.get('pid')
        if not pid:
            return False
            
            try:
                if system == 'Windows':
                    kill_application = kill_application
                    import windows.killer
                    return kill_application(pid, force)
                if None == 'Darwin':
                    kill_application = kill_application
                    import mac.killer
                    return kill_application(pid, force)
                if None == 'Linux':
                    kill_application = kill_application
                    import linux.killer
                    return kill_application(pid, force)
                None.error(f'''[AppKiller] 不支持的操作系统: {system}''')
                return False
            except Exception:
                e = None
                logger.error(f'''[AppKiller] 同步关闭应用程序失败: {e}''')
                e = None
                del e
                return False
                e = None
                del e




def _kill_windows_app_group(apps = None, app_name = None, force = None):
    '''Windows系统的分组关闭策略.

    Args:
        apps: 匹配的应用程序进程列表
        app_name: 应用程序名称
        force: 是否强制关闭

    Returns:
        bool: 关闭是否成功
    '''
    
    try:
        kill_application_group = kill_application_group
        import windows.killer
        return kill_application_group(apps, app_name, force)
    except Exception:
        e = None
        logger.error(f'''[AppKiller] Windows分组关闭失败: {e}''')
        e = None
        del e
        return False
        e = None
        del e



def get_system_killer():
    '''根据当前系统获取对应的关闭器模块.

    Returns:
        对应系统的关闭器模块
    '''
    system = platform.system()
    if system == 'Darwin':
        killer = killer
        import mac
        return killer
    if None == 'Windows':
        killer = killer
        import windows
        return killer
    if None == 'Linux':
        killer = killer
        import linux
        return killer
    None.warning(f'''[AppKiller] 不支持的系统: {system}''')

