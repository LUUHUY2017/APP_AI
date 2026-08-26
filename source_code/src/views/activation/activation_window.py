# Source Generated with Decompyle++
# File: activation_window.pyc (Python 3.12)

'''
Cửa sổ kích hoạt thiết bị - Hiển thị tiến trình kích hoạt, thông tin thiết bị và trạng thái kích hoạt.
'''
from pathlib import Path
from typing import Optional
from PyQt5.QtCore import QSize, Qt, QUrl, pyqtSignal
from PyQt5.QtGui import QPainterPath, QRegion
from PyQt5.QtQuickWidgets import QQuickWidget
from PyQt5.QtWidgets import QApplication, QVBoxLayout, QWidget
from src.core.system_initializer import SystemInitializer
from src.utils.device_activator import DeviceActivator
from src.utils.logging_config import get_logger
from base.async_mixins import AsyncMixin, AsyncSignalEmitter
from base.base_window import BaseWindow
from activation_model import ActivationModel
logger = get_logger(__name__)

class ActivationWindow(AsyncMixin, BaseWindow):
    pass
# WARNING: Decompyle incomplete

