# Source Generated with Decompyle++
# File: audio_codec.pyc (Python 3.12)

import asyncio
import gc
import time
from collections import deque
from typing import Optional
import numpy as np
import os
import sys
from pathlib import Path
dll_dirs = []
dll_dirs.append(Path(__file__).resolve().parents[2] / 'libs' / 'libopus' / 'win' / 'x64')
if getattr(sys, 'frozen', False):
    dll_dirs.append(Path(sys._MEIPASS) / 'libs')
    dll_dirs.append(Path(sys._MEIPASS) / 'libs' / 'libopus' / 'win' / 'x64')
for d in dll_dirs:
    if not d.exists():
        continue
    os.add_dll_directory(str(d))
    print(f'''[OpusLoader] Added DLL path: {d}''')
import opuslib
import sounddevice as sd
import soxr
from src.audio_codecs.aec_processor import AECProcessor
from src.constants.constants import AudioConfig
from src.utils.config_manager import ConfigManager
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

class AudioCodec:
    '''
    音频编解码器，负责录音编码和播放解码
    主要功能：
    1. 录音：麦克风 -> 重采样16kHz -> Opus编码 -> 发送
    2. 播放：接收 -> Opus解码24kHz -> 播放队列 -> 扬声器
    '''
    
    def __init__(self):
        self.config = ConfigManager.get_instance()
        self.opus_encoder = None
        self.opus_decoder = None
        self.device_input_sample_rate = None
        self.device_output_sample_rate = None
        self.mic_device_id = None
        self.speaker_device_id = None
        self.input_resampler = None
        self.output_resampler = None
        self._resample_input_buffer = deque()
        self._resample_output_buffer = deque()
        self._device_input_frame_size = None
        self._is_closing = False
        self.input_stream = None
        self.output_stream = None
        self._wakeword_buffer = asyncio.Queue(maxsize = 100)
        self._output_buffer = asyncio.Queue(maxsize = 500)
        self._encoded_audio_callback = None
        self.aec_processor = AECProcessor()
        self._aec_enabled = False

    
    def _auto_pick_device(self = None, kind = None):
        """
        自动挑选一个稳定的设备索引（优先 WASAPI）。
        kind: 'input' 或 'output'
        """
        pass
    # WARNING: Decompyle incomplete

    
    async def initialize(self):
        '''
        初始化音频设备.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _create_resamplers(self):
        '''
        创建重采样器 输入：设备采样率 -> 16kHz（用于编码） 输出：24kHz -> 设备采样率（播放用）
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _select_audio_devices(self):
        '''显示并选择音频设备.

        优先使用配置文件中的设备，如果没有则自动选择并保存到配置（只在首次写入，之后不覆盖）。
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _save_default_audio_config(self = None, input_device_id = None, output_device_id = None):
        '''
        保存默认音频设备配置到配置文件（仅针对传入的非空设备；不会覆盖已有字段）。
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _create_streams(self):
        '''
        创建音频流.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _input_callback(self, indata, frames, time_info, status):
        '''
        录音回调，硬件驱动调用 处理流程：原始音频 -> 重采样16kHz -> 编码发送 + 唤醒词检测.
        '''
        if status and 'overflow' not in str(status).lower():
            logger.warning(f'''输入流状态: {status}''')
        if self._is_closing:
            return None
    # WARNING: Decompyle incomplete

    
    def _process_input_resampling(self, audio_data):
        '''
        输入重采样到16kHz.
        '''
        
        try:
            resampled_data = self.input_resampler.resample_chunk(audio_data, last = False)
            if len(resampled_data) > 0:
                self._resample_input_buffer.extend(resampled_data.astype(np.int16))
            expected_frame_size = AudioConfig.INPUT_FRAME_SIZE
            if len(self._resample_input_buffer) < expected_frame_size:
                return None
                
                try:
                    frame_data = []
                    for _ in range(expected_frame_size):
                        frame_data.append(self._resample_input_buffer.popleft())
                    return np.array(frame_data, dtype = np.int16)
                except Exception:
                    e = None
                    logger.error(f'''输入重采样失败: {e}''')
                    e = None
                    del e
                    return None
                    e = None
                    del e



    
    def _put_audio_data_safe(self, queue, audio_data):
        '''
        安全入队，队列满时丢弃最旧数据.
        '''
        
        try:
            queue.put_nowait(audio_data)
            return None
        except asyncio.QueueFull:
            queue.get_nowait()
            queue.put_nowait(audio_data)
            return None
            except asyncio.QueueEmpty:
                queue.put_nowait(audio_data)
                return None


    
    def _output_callback(self, outdata = None, frames = None, time_info = None, status = ('outdata', np.ndarray, 'frames', int)):
        '''
        播放回调，硬件驱动调用 从播放队列取数据输出到扬声器.
        '''
        if status and 'underflow' not in str(status).lower():
            logger.warning(f'''输出流状态: {status}''')
    # WARNING: Decompyle incomplete

    
    def _output_callback_direct(self = None, outdata = None, frames = None):
        '''
        直接播放24kHz数据（设备支持24kHz时）
        '''
        
        try:
            audio_data = self._output_buffer.get_nowait()
            if len(audio_data) >= frames * AudioConfig.CHANNELS:
                output_frames = audio_data[:frames * AudioConfig.CHANNELS]
                outdata[:] = output_frames.reshape(-1, AudioConfig.CHANNELS)
                return None
                
                try:
                    out_len = len(audio_data) // AudioConfig.CHANNELS
                    if out_len > 0:
                        outdata[:out_len] = audio_data[:out_len * AudioConfig.CHANNELS].reshape(-1, AudioConfig.CHANNELS)
                    if out_len < frames:
                        outdata[out_len:] = 0
                        return None
                    return None
                except asyncio.QueueEmpty:
                    outdata.fill(0)
                    return None



    
    def _output_callback_with_resample(self = None, outdata = None, frames = None):
        '''
        重采样播放（24kHz -> 设备采样率）
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _input_finished_callback(self):
        '''
        输入流结束.
        '''
        logger.info('输入流已结束')

    
    def _reference_finished_callback(self):
        '''
        参考信号流结束.
        '''
        logger.info('参考信号流已结束')

    
    def _output_finished_callback(self):
        '''
        输出流结束.
        '''
        logger.info('输出流已结束')

    
    async def reinitialize_stream(self, is_input = (True,)):
        '''
        重建音频流.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def get_raw_audio_for_detection(self = None):
        '''
        获取唤醒词音频数据.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def set_encoded_audio_callback(self, callback):
        '''
        设置编码回调.
        '''
        self._encoded_audio_callback = callback
        if callback:
            logger.info('启用实时编码')
            return None
        logger.info('禁用编码回调')

    
    def is_aec_enabled(self = None):
        '''
        检查AEC是否启用.
        '''
        return self._aec_enabled

    
    def get_aec_status(self = None):
        '''
        获取AEC状态信息.
        '''
        if not self._aec_enabled or self.aec_processor:
            return {
                'enabled': False,
                'reason': 'AEC未启用或初始化失败' }
    # WARNING: Decompyle incomplete

    
    def toggle_aec(self = None, enabled = None):
        '''切换AEC启用状态.

        Args:
            enabled: 是否启用AEC

        Returns:
            实际的AEC状态
        '''
        if not self.aec_processor:
            logger.warning('AEC处理器未初始化，无法切换状态')
            return False
        if enabled:
            enabled
        self._aec_enabled = self.aec_processor._is_initialized
        if not enabled and self._aec_enabled:
            logger.warning('无法启用AEC，处理器未正确初始化')
        logger.info(f'''AEC状态: {'启用' if self._aec_enabled else '禁用'}''')
        return self._aec_enabled

    
    async def write_audio(self = None, opus_data = None):
        '''
        解码音频并播放 网络接收的Opus数据 -> 解码24kHz -> 播放队列.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def wait_for_audio_complete(self, timeout = (10,)):
        '''
        等待播放完成.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def clear_audio_queue(self):
        '''
        清空音频队列.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def start_streams(self):
        '''
        启动音频流.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def stop_streams(self):
        '''
        停止音频流.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _cleanup_resampler(self, resampler, name):
        '''
        清理重采样器 - 刷新缓冲区并释放资源.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def close(self):
        '''关闭音频编解码器.

        正确的销毁顺序：
        1. 设置关闭标志，阻止新的操作
        2. 停止音频流（停止硬件回调）
        3. 等待回调完全结束
        4. 清空所有队列和缓冲区（打破对 resampler 的间接引用）
        5. 清空回调引用
        6. 清理 resampler（刷新 + 关闭）
        7. 置 None + 强制 GC（释放 nanobind 包装的 C++ 对象）
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def __del__(self):
        '''
        析构函数.
        '''
        if not self._is_closing:
            logger.warning('AudioCodec未正确关闭，请调用close()')
            return None


return None
except Exception:
    e = None
    print(f'''[OpusLoader] Failed to add {d}: {e}''')
    e = None
    del e
    continue
    e = None
    del e
