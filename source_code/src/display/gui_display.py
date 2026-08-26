# Source Generated with Decompyle++
# File: gui_display.pyc (Python 3.12)

'''
GUI hiển thị giao diện người dùng - sử dụng QML.
Tương thích PyInstaller onefile.
'''
import asyncio
import os
import sys
import signal
from abc import ABCMeta
from pathlib import Path
from typing import Callable, Optional
from PyQt5.QtCore import QObject, Qt, QTimer, QUrl
from PyQt5.QtGui import QCursor, QFont
from PyQt5.QtQuickWidgets import QQuickWidget
from PyQt5.QtWidgets import QApplication, QVBoxLayout, QWidget
from PyQt5.QtCore import pyqtSlot
import shutil
from src.display.base_display import BaseDisplay
from src.display.gui_display_model import GuiDisplayModel
from src.utils.resource_finder import find_assets_dir

def resource_path(relative_path = None):
    '''
    Lấy đường dẫn tuyệt đối đến file tài nguyên.
    - Khi chạy dev: dùng đường dẫn thật trên ổ đĩa.
    - Khi đóng gói PyInstaller onefile: trỏ đến thư mục tạm sys._MEIPASS.
    '''
    if getattr(sys, 'frozen', False) and hasattr(sys, '_MEIPASS'):
        base_path = sys._MEIPASS
    else:
        base_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
    return os.path.join(base_path, relative_path)


def CombinedMeta():
    '''CombinedMeta'''
    pass

CombinedMeta = <NODE:27>(CombinedMeta, 'CombinedMeta', type(QObject), ABCMeta)

def GuiDisplay():
    '''GuiDisplay'''
    pass
# WARNING: Decompyle incomplete

GuiDisplay = <NODE:27>(GuiDisplay, 'GuiDisplay', BaseDisplay, QObject, metaclass = CombinedMeta)
