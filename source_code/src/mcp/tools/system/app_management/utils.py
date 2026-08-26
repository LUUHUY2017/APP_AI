# Source Generated with Decompyle++
# File: utils.pyc (Python 3.12)

'''应用程序管理通用工具.

提供统一的应用程序匹配、查找和缓存功能
'''
import platform
import re
import time
from typing import Any, Dict, List, Optional
from src.utils.logging_config import get_logger
logger = get_logger(__name__)
_cached_applications: Optional[List[Dict[(str, Any)]]] = None
_cache_timestamp: float = 0
_cache_duration = 300

class AppMatcher:
    __module__ = __name__
    __qualname__ = 'AppMatcher'
    __doc__ = '\n    统一的应用程序匹配器.\n    '
# WARNING: Decompyle incomplete


async def get_cached_applications(force_refresh = None):
    '''获取缓存的应用程序列表.

    Args:
        force_refresh: 是否强制刷新缓存

    Returns:
        应用程序列表
    '''
    pass
# WARNING: Decompyle incomplete


async def find_best_matching_app(app_name = None, app_type = None):
    '''查找最佳匹配的应用程序.

    Args:
        app_name: 应用程序名称
        app_type: 应用程序类型过滤 ("installed", "running", "any")

    Returns:
        最佳匹配的应用程序信息
    '''
    pass
# WARNING: Decompyle incomplete


def clear_app_cache():
    '''
    清空应用程序缓存.
    '''
    global _cached_applications, _cache_timestamp
    _cached_applications = None
    _cache_timestamp = 0
    logger.info('[AppUtils] 应用程序缓存已清空')


def get_cache_info():
    '''
    获取缓存信息.
    '''
    current_time = time.time()
    cache_age = current_time - _cache_timestamp if _cache_timestamp > 0 else -1
    if cache_age >= 0:
        cache_age >= 0
    return {
        'cached': _cached_applications is not None,
        'count': len(_cached_applications) if _cached_applications else 0,
        'age_seconds': int(cache_age) if cache_age >= 0 else None,
        'valid': cache_age < _cache_duration,
        'cache_duration': _cache_duration }


def get_system_scanner():
    '''根据当前系统获取对应的扫描器模块.

    Returns:
        对应系统的扫描器模块
    '''
    system = platform.system()
    if system == 'Darwin':
        scanner = scanner
        import mac
        return scanner
    if None == 'Windows':
        scanner = scanner
        import windows
        return scanner
    if None == 'Linux':
        scanner = scanner
        import linux
        return scanner
    None.warning(f'''[AppUtils] 不支持的系统: {system}''')

