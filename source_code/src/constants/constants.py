# Source Generated with Decompyle++
# File: constants.pyc (Python 3.12)

import platform
from src.utils.config_manager import ConfigManager
config = ConfigManager.get_instance()

class ListeningMode:
    '''
    Chế độ lắng nghe.
    '''
    REALTIME = 'realtime'
    AUTO_STOP = 'auto_stop'
    MANUAL = 'manual'


class AbortReason:
    '''
    Nguyên nhân dừng/interruption.
    '''
    NONE = 'none'
    WAKE_WORD_DETECTED = 'wake_word_detected'
    USER_INTERRUPTION = 'user_interruption'


class DeviceState:
    '''
    Trạng thái thiết bị.
    '''
    IDLE = 'idle'
    CONNECTING = 'connecting'
    LISTENING = 'listening'
    SPEAKING = 'speaking'


class EventType:
    '''
    Loại sự kiện.
    '''
    SCHEDULE_EVENT = 'schedule_event'
    AUDIO_INPUT_READY_EVENT = 'audio_input_ready_event'
    AUDIO_OUTPUT_READY_EVENT = 'audio_output_ready_event'


def is_official_server(ws_addr = None):
    '''Kiểm tra xem đây có phải là server chính thức của Xiaozhi không.

    Args:
        ws_addr (str): Địa chỉ WebSocket

    Returns:
        bool: Có phải server chính thức của Xiaozhi không
    '''
    return 'api.tenclass.net' in ws_addr


def get_frame_duration():
    '''Lấy độ dài khung (frame) của thiết bị.

    Returns:
        int: Độ dài khung (ms)
    '''
    pass
# WARNING: Decompyle incomplete


class AudioConfig:
    '''
    Lớp cấu hình âm thanh.
    '''
    INPUT_SAMPLE_RATE = 16000
    _ota_url = config.get_config('SYSTEM_OPTIONS.NETWORK.OTA_VERSION_URL')
    OUTPUT_SAMPLE_RATE = 24000 if is_official_server(_ota_url) else 16000
    CHANNELS = 1
    FRAME_DURATION = get_frame_duration()
    INPUT_FRAME_SIZE = int(INPUT_SAMPLE_RATE * (FRAME_DURATION / 1000))
    OUTPUT_FRAME_SIZE = int(OUTPUT_SAMPLE_RATE * (FRAME_DURATION / 1000))

