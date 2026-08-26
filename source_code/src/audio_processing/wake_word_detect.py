# Source Generated with Decompyle++
# File: wake_word_detect.pyc (Python 3.12)


def init_keyword_spotter():
    pass

import asyncio
import time
from pathlib import Path
from typing import Callable, Optional
import numpy as np
import sherpa_onnx
from src.constants.constants import AudioConfig
from src.utils.config_manager import ConfigManager
from src.utils.logging_config import get_logger
from src.utils.resource_finder import resource_finder
logger = get_logger(__name__)

class WakeWordDetector:
    
    def __init__(self):
        self.audio_codec = None
        self.is_running_flag = False
        self.paused = False
        self.detection_task = None
        self.last_detection_time = 0
        self.detection_cooldown = 1.5
        self.on_detected_callback = None
        self.on_error = None
        config = ConfigManager.get_instance()
        if not config.get_config('WAKE_WORD_OPTIONS.USE_WAKE_WORD', False):
            logger.info('唤醒词功能已禁用')
            self.enabled = False
            return None
        self.enabled = True
        self.sample_rate = AudioConfig.INPUT_SAMPLE_RATE
        self.keyword_spotter = None
        self.stream = None
        self._load_config(config)
        self._init_kws_model()
        self._validate_config()

    
    def _load_config(self, config):
        '''
        加载配置参数.
        '''
        model_path = config.get_config('WAKE_WORD_OPTIONS.MODEL_PATH', 'models')
        self.model_dir = resource_finder.find_directory(model_path)
    # WARNING: Decompyle incomplete

    
    def _init_kws_model(self):
        '''
        初始化Sherpa-ONNX KeywordSpotter模型.
        '''
        
        try:
            encoder_path = self.model_dir / 'encoder.onnx'
            decoder_path = self.model_dir / 'decoder.onnx'
            joiner_path = self.model_dir / 'joiner.onnx'
            tokens_path = self.model_dir / 'tokens.txt'
            keywords_path = self.model_dir / 'keywords.txt'
            required_files = [
                encoder_path,
                decoder_path,
                joiner_path,
                tokens_path,
                keywords_path]
            for file_path in required_files:
                if file_path.exists():
                    continue
                    
                    try:
                        raise FileNotFoundError(f'''模型文件不存在: {file_path}''')
                        logger.info(f'''加载Sherpa-ONNX KeywordSpotter模型: {self.model_dir}''')
                        self.keyword_spotter = sherpa_onnx.KeywordSpotter(tokens = str(tokens_path), encoder = str(encoder_path), decoder = str(decoder_path), joiner = str(joiner_path), keywords_file = str(keywords_path), num_threads = self.num_threads, sample_rate = self.sample_rate, feature_dim = 80, max_active_paths = self.max_active_paths, keywords_score = self.keywords_score, keywords_threshold = self.keywords_threshold, num_trailing_blanks = self.num_trailing_blanks, provider = self.provider)
                        logger.info('Sherpa-ONNX KeywordSpotter模型加载成功')
                        return None
                    except Exception:
                        e = None
                        logger.error(f'''Sherpa-ONNX KeywordSpotter初始化失败: {e}''', exc_info = True)
                        self.enabled = False
                        e = None
                        del e
                        return None
                        e = None
                        del e



    
    def on_detected(self = None, callback = None):
        '''
        设置检测到唤醒词的回调函数.
        '''
        self.on_detected_callback = callback

    
    async def start(self = None, audio_codec = None):
        '''
        启动唤醒词检测器.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _detection_loop(self):
        '''
        检测循环.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _process_audio(self):
        '''处理音频数据 - 批量处理优化'''
        pass
    # WARNING: Decompyle incomplete

    
    async def _handle_detection_result(self, result):
        '''
        处理检测结果.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def stop(self):
        '''
        停止检测器.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def pause(self):
        '''
        暂停检测.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def resume(self):
        '''
        恢复检测.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def is_running(self = None):
        '''
        检查是否正在运行.
        '''
        if self.is_running_flag:
            self.is_running_flag
        return not (self.paused)

    
    def _validate_config(self):
        '''
        验证配置参数.
        '''
        if not self.enabled:
            return None
        if not  <= 0.1, self.keywords_threshold or 0.1, self.keywords_threshold <= 1:
            pass
        
        logger.warning(f'''关键词阈值 {self.keywords_threshold} 超出范围，重置为0.25''')
        if not  <= 0.1, self.keywords_score or 0.1, self.keywords_score <= 10:
            pass
        else:
            0.25
        logger.warning(f'''关键词分数 {self.keywords_score} 超出范围，重置为2.0''')
        logger.info(f'''KWS配置验证完成 - 阈值: {self.keywords_threshold}, 分数: {self.keywords_score}''')

    
    def get_performance_stats(self):
        '''
        获取性能统计信息.
        '''
        return {
            'enabled': self.enabled,
            'engine': 'sherpa-onnx-kws',
            'provider': self.provider,
            'num_threads': self.num_threads,
            'keywords_threshold': self.keywords_threshold,
            'keywords_score': self.keywords_score,
            'is_running': self.is_running() }

    
    def clear_cache(self):
        '''
        清空缓存.
        '''
        pass


