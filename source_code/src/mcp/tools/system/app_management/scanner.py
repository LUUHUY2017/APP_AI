# Source Generated with Decompyle++
# File: scanner.pyc (Python 3.12)

'''统一的应用程序扫描器入口.

根据当前系统自动选择对应的扫描器实现
'''
import asyncio
import json
from typing import Any, Dict
from src.utils.logging_config import get_logger
from utils import get_system_scanner
logger = get_logger(__name__)

async def scan_installed_applications(args = None):
    '''扫描系统中所有已安装的应用程序.

    Args:
        args: 包含扫描参数的字典
            - force_refresh: 是否强制重新扫描（可选，默认False）

    Returns:
        str: JSON格式的应用程序列表
    '''
    pass
# WARNING: Decompyle incomplete


async def list_running_applications(args = None):
    '''列出系统中正在运行的应用程序.

    Args:
        args: 包含过滤参数的字典
            - filter_name: 应用名称过滤条件（可选）

    Returns:
        str: JSON格式的运行应用程序列表
    '''
    pass
# WARNING: Decompyle incomplete

