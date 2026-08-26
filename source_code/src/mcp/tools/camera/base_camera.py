# Source Generated with Decompyle++
# File: base_camera.pyc (Python 3.12)

'''
Base camera implementation.
'''
import threading
from abc import ABC, abstractmethod
from typing import Dict
from src.utils.config_manager import ConfigManager
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

class BaseCamera(ABC):
    '''
    基础摄像头类，定义接口.
    '''
    _instance = None
    _lock = threading.Lock()
    
    def __init__(self):
        '''
        初始化基础摄像头.
        '''
        self.jpeg_data = {
            'buf': b'',
            'len': 0 }
        config = ConfigManager.get_instance()
        self.camera_index = config.get_config('CAMERA.camera_index', 0)
        self.frame_width = config.get_config('CAMERA.frame_width', 640)
        self.frame_height = config.get_config('CAMERA.frame_height', 480)

    capture = (lambda self = None: pass)()
    analyze = (lambda self = None, question = None: pass)()
    
    def get_jpeg_data(self = None):
        '''
        获取JPEG数据.
        '''
        return self.jpeg_data

    
    def set_jpeg_data(self = None, data_bytes = None):
        '''
        设置JPEG数据.
        '''
        self.jpeg_data['buf'] = data_bytes
        self.jpeg_data['len'] = len(data_bytes)


