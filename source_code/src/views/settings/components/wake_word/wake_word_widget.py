# Source Generated with Decompyle++
# File: wake_word_widget.pyc (Python 3.12)

from pathlib import Path
from PyQt5.QtCore import pyqtSignal
from PyQt5.QtWidgets import QCheckBox, QFileDialog, QLineEdit, QMessageBox, QPushButton, QTextEdit, QWidget
from src.utils.config_manager import ConfigManager
from src.utils.logging_config import get_logger
from src.utils.resource_finder import get_project_root, resource_finder

try:
    from pypinyin import lazy_pinyin, Style
    PYPINYIN_AVAILABLE = True
    
    class WakeWordWidget(QWidget):
        pass
    # WARNING: Decompyle incomplete

    return None
except ImportError:
    PYPINYIN_AVAILABLE = False
    continue

