# Source Generated with Decompyle++
# File: tools.pyc (Python 3.12)

'''
八字命理MCP工具函数 提供给MCP服务器调用的异步工具函数。
'''
import json
from typing import Any, Dict
from src.utils.logging_config import get_logger
from bazi_calculator import get_bazi_calculator
from engine import get_bazi_engine
logger = get_logger(__name__)

async def get_bazi_detail(args = None):
    '''
    根据时间（公历或农历）、性别来获取八字信息。
    '''
    pass
# WARNING: Decompyle incomplete


async def get_solar_times(args = None):
    '''
    根据八字获取公历时间列表。
    '''
    pass
# WARNING: Decompyle incomplete


async def get_chinese_calendar(args = None):
    '''
    获取指定公历时间（默认今天）的黄历信息。
    '''
    pass
# WARNING: Decompyle incomplete


async def build_bazi_from_lunar_datetime(args = None):
    '''
    根据农历时间、性别来获取八字信息（已弃用，使用get_bazi_detail替代）。
    '''
    pass
# WARNING: Decompyle incomplete


async def build_bazi_from_solar_datetime(args = None):
    '''
    根据阳历时间、性别来获取八字信息（已弃用，使用get_bazi_detail替代）。
    '''
    pass
# WARNING: Decompyle incomplete

