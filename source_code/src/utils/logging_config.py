# Source Generated with Decompyle++
# File: logging_config.pyc (Python 3.12)

import logging
from logging.handlers import TimedRotatingFileHandler
from colorlog import ColoredFormatter

def setup_logging():
    '''
    配置日志系统.
    '''
    get_project_root = get_project_root
    import resource_finder
    project_root = get_project_root()
    log_dir = project_root / 'logs'
    log_dir.mkdir(exist_ok = True)
    log_file = log_dir / 'app.log'
    root_logger = logging.getLogger()
    root_logger.setLevel(logging.INFO)
    if root_logger.handlers:
        root_logger.handlers.clear()
    console_handler = logging.StreamHandler()
    console_handler.setLevel(logging.INFO)
    file_handler = TimedRotatingFileHandler(log_file, when = 'midnight', interval = 1, backupCount = 30, encoding = 'utf-8')
    file_handler.setLevel(logging.INFO)
    file_handler.suffix = '%Y-%m-%d.log'
    formatter = logging.Formatter('%(asctime)s[%(name)s] - %(levelname)s - %(message)s - %(threadName)s')
    color_formatter = ColoredFormatter('%(green)s%(asctime)s%(reset)s[%(blue)s%(name)s%(reset)s] - %(log_color)s%(levelname)s%(reset)s - %(green)s%(message)s%(reset)s - %(cyan)s%(threadName)s%(reset)s', log_colors = {
        'DEBUG': 'cyan',
        'INFO': 'white',
        'WARNING': 'yellow',
        'ERROR': 'red',
        'CRITICAL': 'red,bg_white' }, secondary_log_colors = {
        'asctime': {
            'green': 'green' },
        'name': {
            'blue': 'blue' } })
    console_handler.setFormatter(color_formatter)
    file_handler.setFormatter(formatter)
    root_logger.addHandler(console_handler)
    root_logger.addHandler(file_handler)
    logging.info('日志系统已初始化，日志文件: %s', log_file)
    return log_file


def get_logger(name):
    '''获取统一配置的日志记录器.

    Args:
        name: 日志记录器名称，通常是模块名

    Returns:
        logging.Logger: 配置好的日志记录器

    示例:
        logger = get_logger(__name__)
        logger.info("这是一条信息")
        logger.error("出错了: %s", error_msg)
    '''
    pass
# WARNING: Decompyle incomplete

