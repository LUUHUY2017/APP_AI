# Source Generated with Decompyle++
# File: launcher.pyc (Python 3.12)

'''macOS系统应用程序启动器.

提供macOS平台下的应用程序启动功能
'''
import os
import subprocess
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

def launch_application(app_name = None):
    '''在macOS上启动应用程序.

    Args:
        app_name: 应用程序名称

    Returns:
        bool: 启动是否成功
    '''
    
    try:
        logger.info(f'''[MacLauncher] 启动应用程序: {app_name}''')
        
        try:
            subprocess.Popen([
                'open',
                '-a',
                app_name])
            logger.info(f'''[MacLauncher] 使用open -a成功启动: {app_name}''')
            return True
        except (OSError, subprocess.SubprocessError):
            logger.debug(f'''[MacLauncher] open -a启动失败: {app_name}''')
            
            try:
                pass
            try:
                
                try:
                    subprocess.Popen([
                        app_name])
                    logger.info(f'''[MacLauncher] 直接启动成功: {app_name}''')
                    return True
                except (OSError, subprocess.SubprocessError):
                    logger.debug(f'''[MacLauncher] 直接启动失败: {app_name}''')
                    
                    try:
                        pass
                    try:
                        app_path = f'''/Applications/{app_name}.app'''
                        if os.path.exists(app_path):
                            subprocess.Popen([
                                'open',
                                app_path])
                            logger.info(f'''[MacLauncher] 通过Applications目录启动成功: {app_name}''')
                            return True
                            
                            try:
                                script = f'''tell application "{app_name}" to activate'''
                                subprocess.Popen([
                                    'osascript',
                                    '-e',
                                    script])
                                logger.info(f'''[MacLauncher] 使用osascript启动成功: {app_name}''')
                                return True
                            except Exception:
                                e = None
                                logger.error(f'''[MacLauncher] macOS启动失败: {e}''')
                                e = None
                                del e
                                return False
                                e = None
                                del e







