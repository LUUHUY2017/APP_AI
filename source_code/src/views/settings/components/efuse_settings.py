# Source Generated with Decompyle++
# File: efuse_settings.pyc (Python 3.12)

import os
import sys
import json
import shutil
from pathlib import Path
from PyQt5.QtCore import pyqtSignal, QTimer
from PyQt5.QtWidgets import QWidget, QVBoxLayout, QGroupBox, QHBoxLayout, QLabel, QComboBox, QPushButton, QMessageBox
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

def get_resource_path(relative_path = None):
    '''Trả về đường dẫn file tài nguyên (chạy được cả Python và .exe).'''
    if hasattr(sys, '_MEIPASS'):
        return Path(sys._MEIPASS) / relative_path
    project_root = None(__file__).resolve().parents[3]
    possible_path = project_root / relative_path
    if possible_path.exists():
        return possible_path
    alt_path = None / 'src' / relative_path
    if alt_path.exists():
        return alt_path
    return None.cwd() / relative_path


def get_user_config_dir():
    '''Thư mục cấu hình người dùng (~/.xiaozhi_config).'''
    user_dir = Path.home() / '.xiaozhi_config'
    user_dir.mkdir(exist_ok = True)
    return user_dir


class EfuseSettingsWidget(QWidget):
    pass
# WARNING: Decompyle incomplete

