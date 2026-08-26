# Source Generated with Decompyle++
# File: device_status.pyc (Python 3.12)

'''
设备状态管理模块 - 提供基本的系统设备状态信息
'''
import datetime
import platform
import socket
from typing import Any, Dict
import psutil
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

def get_device_status():
    '''
    获取当前主机的整体设备状态.
    '''
    
    try:
        status = { }
        uname = platform.uname()
        status['system'] = {
            'os': uname.system,
            'node_name': uname.node,
            'release': uname.release,
            'version': uname.version,
            'machine': uname.machine,
            'processor': uname.processor,
            'hostname': socket.gethostname(),
            'ip_address': _get_local_ip(),
            'timestamp': datetime.datetime.now().isoformat() }
        status['cpu'] = {
            'physical_cores': psutil.cpu_count(logical = False),
            'logical_cores': psutil.cpu_count(logical = True),
            'usage_percent': psutil.cpu_percent(interval = 0.1),
            'per_core_usage': psutil.cpu_percent(interval = 0.1, percpu = True) }
        virtual_mem = psutil.virtual_memory()
        status['memory'] = {
            'total': virtual_mem.total,
            'available': virtual_mem.available,
            'used': virtual_mem.used,
            'percent': virtual_mem.percent }
        disk = psutil.disk_usage('/')
        status['disk'] = {
            'total': disk.total,
            'used': disk.used,
            'free': disk.free,
            'percent': disk.percent }
        battery = psutil.sensors_battery()
        if battery:
            status['battery'] = {
                'percent': battery.percent,
                'plugged': battery.power_plugged,
                'secs_left': battery.secsleft }
        else:
            status['battery'] = None
        logger.info('[DeviceStatus] 设备状态获取成功')
        return status
    except Exception:
        e = None
        logger.error(f'''[DeviceStatus] 获取设备状态失败: {e}''', exc_info = True)
        del e
        return None
        None = 
        del e



def _get_local_ip():
    '''
    获取本地IP地址.
    '''
    
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(('8.8.8.8', 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except Exception:
        return 
        except Exception:
            return '127.0.0.1'


