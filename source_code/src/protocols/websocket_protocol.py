# Source Generated with Decompyle++
# File: websocket_protocol.pyc (Python 3.12)

import asyncio
import json
import ssl
import time
import websockets
from src.constants.constants import AudioConfig
from src.protocols.protocol import Protocol
from src.utils.config_manager import ConfigManager
from src.utils.logging_config import get_logger
ssl_context = ssl._create_unverified_context()
logger = get_logger(__name__)

class WebsocketProtocol(Protocol):
    pass
# WARNING: Decompyle incomplete

