# Source Generated with Decompyle++
# File: base_display.pyc (Python 3.12)

from abc import ABC, abstractmethod
from typing import Callable, Optional
from src.utils.logging_config import get_logger

class BaseDisplay(ABC):
    '''
    Lớp cơ sở trừu tượng cho giao diện hiển thị.
    '''
    
    def __init__(self):
        self.logger = get_logger(self.__class__.__name__)

    set_callbacks = (lambda self, press_callback, release_callback = None, mode_callback = None, auto_callback = abstractmethod, abort_callback = (None, None, None, None, None, None), send_text_callback = ('press_callback', Optional[Callable], 'release_callback', Optional[Callable], 'mode_callback', Optional[Callable], 'auto_callback', Optional[Callable], 'abort_callback', Optional[Callable], 'send_text_callback', Optional[Callable]): pass# WARNING: Decompyle incomplete
)()
    update_button_status = (lambda self = None, text = None: pass# WARNING: Decompyle incomplete
)()
    update_status = (lambda self = None, status = None, connected = abstractmethod: pass# WARNING: Decompyle incomplete
)()
    update_text = (lambda self = None, text = None: pass# WARNING: Decompyle incomplete
)()
    update_emotion = (lambda self = None, emotion_name = None: pass# WARNING: Decompyle incomplete
)()
    start = (lambda self: pass# WARNING: Decompyle incomplete
)()
    close = (lambda self: pass# WARNING: Decompyle incomplete
)()

