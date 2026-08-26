# Source Generated with Decompyle++
# File: device_activator.pyc (Python 3.12)

import asyncio
import json
from typing import Optional
from src.utils.common_utils import handle_verification_code
from src.utils.device_fingerprint import DeviceFingerprint
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

class DeviceActivator:
    '''Trình quản lý kích hoạt thiết bị - phiên bản OFFLINE (không lưu file, không gọi server).'''
    
    def __init__(self, config_manager):
        '''Khởi tạo bộ kích hoạt thiết bị.'''
        self.logger = get_logger(__name__)
        self.config_manager = config_manager
        self.device_fingerprint = DeviceFingerprint.get_instance()
        self._ensure_device_identity()
        self._activation_task = None

    
    def _ensure_device_identity(self):
        '''Đảm bảo thiết bị có thông tin định danh.'''
        (serial_number, hmac_key, is_activated) = self.device_fingerprint.ensure_device_identity()
        self.logger.info(f'''Định danh thiết bị: serial={serial_number}, trạng thái={'Đã kích hoạt' if is_activated else 'Chưa kích hoạt'}''')

    
    def cancel_activation(self):
        '''Hủy bỏ tiến trình kích hoạt.'''
        if self._activation_task:
            if not self._activation_task.done():
                self.logger.info('Đang hủy tiến trình kích hoạt...')
                self._activation_task.cancel()
                return None
            return None

    
    def has_serial_number(self = None):
        '''Kiểm tra thiết bị có serial hay không.'''
        return self.device_fingerprint.has_serial_number()

    
    def get_serial_number(self = None):
        '''Lấy serial number.'''
        return self.device_fingerprint.get_serial_number()

    
    def get_hmac_key(self = None):
        '''Lấy HMAC key.'''
        return self.device_fingerprint.get_hmac_key()

    
    def set_activation_status(self = None, status = None):
        '''Cập nhật trạng thái kích hoạt.'''
        return self.device_fingerprint.set_activation_status(status)

    
    def is_activated(self = None):
        '''Kiểm tra trạng thái đã kích hoạt.'''
        return self.device_fingerprint.is_activated()

    
    def generate_hmac(self = None, challenge = None):
        '''Tạo chữ ký HMAC từ challenge.'''
        return self.device_fingerprint.generate_hmac(challenge)

    
    async def process_activation(self = None, activation_data = None):
        '''
        Xử lý kích hoạt (OFFLINE) — không gửi request, không lưu file.

        Args:
            activation_data (dict): Dữ liệu kích hoạt (challenge, code, message)

        Returns:
            bool: Luôn True nếu quá trình cục bộ thành công
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def activate(self = None, challenge = None, code = None):
        '''
        Hàm activate (OFFLINE) — không gửi yêu cầu, không ghi dữ liệu.
        '''
        pass
    # WARNING: Decompyle incomplete


