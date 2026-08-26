# Source Generated with Decompyle++
# File: common_utils.pyc (Python 3.12)

'''
Module tập hợp các hàm công cụ chung, bao gồm chuyển văn bản thành giọng nói,
tương tác trình duyệt, clipboard, và các tiện ích khác.
'''
import queue
import shutil
import threading
import time
import webbrowser
from typing import Optional
from src.utils.logging_config import get_logger
logger = get_logger(__name__)
_audio_queue = queue.Queue()
_audio_lock = threading.Lock()
_audio_worker_thread = None
_audio_worker_running = False
_audio_device_warmed_up = False

def _warm_up_audio_device():
    '''
    Làm nóng thiết bị âm thanh, tránh trường hợp chữ đầu bị cắt.
    '''
    global _audio_device_warmed_up
    if _audio_device_warmed_up:
        return None
    
    try:
        import platform
        import subprocess
        system = platform.system()
        if system == 'Darwin':
            subprocess.run([
                'say',
                '-v',
                'Ting-Ting',
                '嗡'], stdout = subprocess.DEVNULL, stderr = subprocess.DEVNULL)
        elif system == 'Linux' and shutil.which('espeak'):
            subprocess.run([
                'espeak',
                '-v',
                'zh',
                '嗡'], stdout = subprocess.DEVNULL, stderr = subprocess.DEVNULL)
        elif system == 'Windows':
            import win32com.client as win32com
            speaker = win32com.client.Dispatch('SAPI.SpVoice')
            speaker.Speak('嗡')
        _audio_device_warmed_up = True
        logger.info('Thiết bị âm thanh đã được làm nóng')
        return None
    except Exception:
        e = None
        logger.warning(f'''Làm nóng thiết bị âm thanh thất bại: {e}''')
        e = None
        del e
        return None
        e = None
        del e



def _audio_queue_worker():
    '''
    Thread làm việc cho hàng đợi âm thanh, đảm bảo âm thanh được phát theo thứ tự
    và không bị cắt ngang.
    '''
    pass
# WARNING: Decompyle incomplete


def _ensure_audio_worker():
    '''
    Đảm bảo thread xử lý âm thanh đang chạy.
    '''
    pass
# WARNING: Decompyle incomplete


def open_url(url = None):
    '''
    Mở một URL trong trình duyệt mặc định.
    '''
    
    try:
        success = webbrowser.open(url)
        if success:
            logger.info(f'''Đã mở trang web thành công: {url}''')
            return success
        None.warning(f'''Không thể mở trang web: {url}''')
        return success
    except Exception:
        e = None
        logger.error(f'''Lỗi khi mở trang web: {e}''')
        e = None
        del e
        return False
        e = None
        del e



def copy_to_clipboard(text = None):
    '''
    Sao chép văn bản vào clipboard.
    '''
    
    try:
        import pyperclip
        pyperclip.copy(text)
        logger.info(f'''Tex "{text}" đã được sao chép vào clipboard''')
        return True
    except ImportError:
        logger.warning('Chưa cài module pyperclip, không thể sao chép vào clipboard')
        return False
        except Exception:
            e = None
            logger.error(f'''Lỗi khi sao chép vào clipboard: {e}''')
            e = None
            del e
            return False
            e = None
            del e



def _play_windows_tts(text = None, set_chinese_voice = None):
    '''
    Phát văn bản bằng TTS trên Windows.
    '''
    
    try:
        import win32com.client as win32com
        speaker = win32com.client.Dispatch('SAPI.SpVoice')
        if set_chinese_voice:
            
            try:
                voices = speaker.GetVoices()
                for i in range(voices.Count):
                    if not 'Chinese' in voices.Item(i).GetDescription():
                        continue
                        
                        try:
                            speaker.Voice = voices.Item(i)
                            range(voices.Count)
                        try:
                            speaker.Rate = -2
                            
                            try:
                                enhanced_text = text + '。 。 。'
                                speaker.Speak(enhanced_text)
                                logger.info('Đã phát văn bản bằng Windows TTS')
                                time.sleep(0.5)
                                return True
                                except Exception:
                                    e = None
                                    logger.warning(f'''Lỗi khi đặt giọng tiếng Trung: {e}''')
                                    
                                    try:
                                        e = None
                                        del e
                                        continue
                                        e = None
                                        del e
                                        
                                        try:
                                            except Exception:
                                                
                                                try:
                                                    continue
                                                    
                                                    try:
                                                        pass
                                                    except ImportError:
                                                        logger.warning('Windows TTS không khả dụng, bỏ qua phát âm thanh')
                                                        return False
                                                        except Exception:
                                                            e = None
                                                            logger.error(f'''Lỗi phát Windows TTS: {e}''')
                                                            e = None
                                                            del e
                                                            return False
                                                            e = None
                                                            del e










def _play_linux_tts(text = None):
    '''
    Phát văn bản bằng TTS trên Linux (espeak).
    '''
    import subprocess
    if shutil.which('espeak'):
        
        try:
            enhanced_text = text + '。 。 。'
            result = subprocess.run([
                'espeak',
                '-v',
                'zh',
                '-s',
                '150',
                '-g',
                '10',
                enhanced_text], stdout = subprocess.DEVNULL, stderr = subprocess.DEVNULL, timeout = 30)
            time.sleep(0.5)
            return result.returncode == 0
            logger.warning('espeak không khả dụng, bỏ qua phát âm thanh')
            return False
        except subprocess.TimeoutExpired:
            logger.warning('espeak phát vượt quá thời gian')
            return False
            except Exception:
                e = None
                logger.error(f'''Lỗi khi phát bằng espeak: {e}''')
                e = None
                del e
                return False
                e = None
                del e



def _play_macos_tts(text = None):
    '''
    Phát văn bản bằng TTS trên macOS (say).
    '''
    import subprocess
    if shutil.which('say'):
        
        try:
            enhanced_text = text + '。 。 。'
            result = subprocess.run([
                'say',
                '-r',
                '180',
                enhanced_text], stdout = subprocess.DEVNULL, stderr = subprocess.DEVNULL, timeout = 30)
            time.sleep(0.5)
            return result.returncode == 0
            logger.warning('say không khả dụng, bỏ qua phát âm thanh')
            return False
        except subprocess.TimeoutExpired:
            logger.warning('say phát vượt quá thời gian')
            return False
            except Exception:
                e = None
                logger.error(f'''Lỗi khi phát bằng say: {e}''')
                e = None
                del e
                return False
                e = None
                del e



def _play_system_tts(text = None):
    '''
    Phát văn bản bằng TTS hệ thống dựa trên OS.
    '''
    import os
    import platform
    if os.name == 'nt':
        return _play_windows_tts(text)
    system = None.system()
    if system == 'Linux':
        return _play_linux_tts(text)
    if None == 'Darwin':
        return _play_macos_tts(text)
    None.warning(f'''Hệ thống {system} không hỗ trợ, bỏ qua phát âm thanh''')
    return False


def play_audio_nonblocking(text = None):
    '''
    Thêm tác vụ phát âm thanh vào hàng đợi, không chặn main thread.
    '''
    pass
# WARNING: Decompyle incomplete


def extract_verification_code(text = None):
    '''
    Trích xuất mã xác thực từ văn bản dựa trên các từ khóa liên quan.
    '''
    pass
# WARNING: Decompyle incomplete


def handle_verification_code(text = None):
    '''
    Xử lý mã xác thực: trích xuất từ văn bản và sao chép vào clipboard.
    '''
    code = extract_verification_code(text)
    if not code:
        return None
    copy_to_clipboard(code)

