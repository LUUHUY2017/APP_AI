# Source Generated with Decompyle++
# File: aec_processor.pyc (Python 3.12)

import platform
from collections import deque
from typing import Any, Dict, Optional
import numpy as np
import sounddevice as sd
from src.constants.constants import AudioConfig
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

class AECProcessor:
    '''
    音频回声消除处理器 专门用于处理参考信号（扬声器输出）和麦克风输入的AEC.
    '''
    
    def __init__(self):
        self._platform = platform.system().lower()
        self._is_macos = self._platform == 'darwin'
        self._is_linux = self._platform == 'linux'
        self._is_windows = self._platform == 'windows'
        self.apm = None
        self.apm_config = None
        self.capture_config = None
        self.render_config = None
        self.reference_stream = None
        self.reference_device_id = None
        self.reference_sample_rate = None
        self._reference_buffer = deque()
        self._webrtc_frame_size = 160
        self._system_frame_size = AudioConfig.INPUT_FRAME_SIZE
        self._is_initialized = False
        self._is_closing = False

    
    async def initialize(self):
        '''
        初始化AEC处理器.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _initialize_apm(self):
        '''
        初始化WebRTC音频处理模块（仅macOS）
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _initialize_reference_capture(self):
        '''
        初始化参考信号捕获（仅macOS）
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _find_blackhole_device(self = None):
        '''
        查找BlackHole 2ch虚拟设备.
        '''
        
        try:
            devices = sd.query_devices()
            for i, device in enumerate(devices):
                device_name = device['name'].lower()
                if not 'blackhole' in device_name:
                    continue
                    
                    try:
                        if not '2ch' in device_name:
                            continue
                            
                            try:
                                if not device['max_input_channels'] >= 1:
                                    continue
                                    
                                    try:
                                        device_info = dict(device)
                                        device_info['id'] = i
                                        logger.info(f'''找到BlackHole设备: [{i}] {device['name']}''')
                                        
                                        return enumerate(devices), device_info
                                        
                                        try:
                                            for i, device in enumerate(devices):
                                                device_name = device['name'].lower()
                                                if not 'blackhole' in device_name:
                                                    continue
                                                    
                                                    try:
                                                        if not device['max_input_channels'] >= 1:
                                                            continue
                                                            
                                                            try:
                                                                device_info = dict(device)
                                                                device_info['id'] = i
                                                                logger.info(f'''找到BlackHole设备: [{i}] {device['name']}''')
                                                                
                                                                return enumerate(devices), device_info
                                                                
                                                                try:
                                                                    return None
                                                                except Exception:
                                                                    logger.error(f'''查找BlackHole设备失败: {e}''')
                                                                    None = None
                                                                    del e
                                                                    return None
                                                                    e = None
                                                                    del e









    
    def _reference_callback(self, indata, frames, time_info, status):
        '''
        参考信号回调.
        '''
        _ = (frames, time_info)
        if status and 'overflow' not in str(status).lower():
            logger.warning(f'''参考信号流状态: {status}''')
        if self._is_closing:
            return None
        
        try:
            audio_data = indata.copy().flatten()
            if self.reference_sample_rate != AudioConfig.INPUT_SAMPLE_RATE:
                ratio = AudioConfig.INPUT_SAMPLE_RATE / self.reference_sample_rate
                target_length = int(len(audio_data) * ratio)
                audio_data = np.interp(np.linspace(0, len(audio_data) - 1, target_length), np.arange(len(audio_data)), audio_data).astype(np.int16)
            self._reference_buffer.extend(audio_data)
            max_buffer_size = self._webrtc_frame_size * 20
            if len(self._reference_buffer) > max_buffer_size:
                self._reference_buffer.popleft()
                if len(self._reference_buffer) > max_buffer_size:
                    continue
                return None
            return None
        except Exception:
            e = None
            logger.error(f'''参考信号回调错误: {e}''')
            e = None
            del e
            return None
            e = None
            del e


    
    def _reference_finished_callback(self):
        '''
        参考信号流结束回调.
        '''
        logger.info('参考信号流已结束')

    
    def process_audio(self = None, capture_audio = None):
        '''处理音频帧，应用AEC 支持10ms/20ms/40ms/60ms等不同帧长度，通过分割处理实现.

        Args:
            capture_audio: 麦克风采集的音频数据 (16kHz, int16)

        Returns:
            处理后的音频数据
        '''
        if not self._is_initialized:
            return capture_audio
        if None._is_windows or self._is_linux:
            return capture_audio
    # WARNING: Decompyle incomplete

    
    def _process_single_aec_frame(self = None, capture_audio = None):
        '''
        处理单个10ms WebRTC帧（仅macOS）
        '''
        if not self._is_macos:
            return capture_audio
    # WARNING: Decompyle incomplete

    
    def _process_chunked_aec_frames(self = None, capture_audio = None, num_chunks = None):
        '''
        分割处理大帧（20ms/40ms/60ms等）
        '''
        processed_chunks = []
        for i in range(num_chunks):
            start_idx = i * self._webrtc_frame_size
            end_idx = (i + 1) * self._webrtc_frame_size
            chunk = capture_audio[start_idx:end_idx]
            processed_chunk = self._process_single_aec_frame(chunk)
            processed_chunks.append(processed_chunk)
        return np.concatenate(processed_chunks)

    
    def _get_reference_frame(self = None, frame_size = None):
        '''
        获取指定大小的参考信号帧.
        '''
        if len(self._reference_buffer) < frame_size:
            return np.zeros(frame_size, dtype = np.int16)
        frame_data = None
        for _ in range(frame_size):
            frame_data.append(self._reference_buffer.popleft())
        return np.array(frame_data, dtype = np.int16)

    
    def is_reference_available(self = None):
        '''
        检查参考信号是否可用.
        '''
        if self._is_windows or self._is_linux:
            return self._is_initialized
        if None.reference_stream is not None:
            None.reference_stream is not None
            if self.reference_stream.active:
                self.reference_stream.active
        return len(self._reference_buffer) >= self._webrtc_frame_size

    
    def get_status(self = None):
        '''
        获取AEC处理器状态.
        '''
        status = {
            'initialized': self._is_initialized,
            'platform': self._platform,
            'reference_available': self.is_reference_available() }
        if self._is_windows:
            status.update({
                'aec_type': 'system_level',
                'description': 'Windows 系统底层回声消除' })
            return status
        if None._is_linux:
            status.update({
                'aec_type': 'system_level',
                'description': 'Linux 系统级回声消除（PulseAudio）' })
            return status
        if None._is_macos:
            status.update({
                'aec_type': 'webrtc_blackhole',
                'description': 'WebRTC + BlackHole 参考信号',
                'reference_device_id': self.reference_device_id,
                'reference_buffer_size': len(self._reference_buffer),
                'webrtc_apm_active': self.apm is not None })
            return status
        None.update({
            'aec_type': 'unsupported',
            'description': f'''平台 {self._platform} 暂不支持AEC''' })
        return status

    
    async def close(self):
        '''
        关闭AEC处理器.
        '''
        pass
    # WARNING: Decompyle incomplete


