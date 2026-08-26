# Source Generated with Decompyle++
# File: system.pyc (Python 3.12)

from enum import Enum

class InitializationStage(Enum):
    '''
    Enum các giai đoạn khởi tạo.
    '''
    DEVICE_FINGERPRINT = 'Giai đoạn 1: Chuẩn bị nhận diện thiết bị'
    CONFIG_MANAGEMENT = 'Giai đoạn 2: Khởi tạo quản lý cấu hình'
    OTA_CONFIG = 'Giai đoạn 3: Lấy cấu hình OTA'
    ACTIVATION = 'Giai đoạn 4: Quy trình kích hoạt'


class SystemConstants:
    '''
    Hằng số hệ thống.
    '''
    APP_NAME = 'py-xiaozhi'
    APP_VERSION = '2.0.0'
    BOARD_TYPE = 'bread-compact-wifi'
    DEFAULT_TIMEOUT = 10
    ACTIVATION_MAX_RETRIES = 60
    ACTIVATION_RETRY_INTERVAL = 5
    CONFIG_FILE = 'config.json'
    EFUSE_FILE = 'efuse.json'

