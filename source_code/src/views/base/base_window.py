# Source Generated with Decompyle++
# File: base_window.pyc (Python 3.12)

'''
Lớp cửa sổ cơ sở - lớp cha của tất cả các cửa sổ PyQt
Hỗ trợ thao tác bất đồng bộ và tích hợp qasync
'''
import asyncio
from typing import Optional
from PyQt5.QtCore import QTimer, pyqtSignal
from PyQt5.QtWidgets import QMainWindow, QWidget
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

class BaseWindow(QMainWindow):
    pass
# WARNING: Decompyle incomplete

