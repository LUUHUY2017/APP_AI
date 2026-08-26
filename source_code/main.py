# Source Generated with Decompyle++
# File: main.pyc (Python 3.12)

"""
Main entry point for Xiaozhi.
This file is designed to:
 - Preload the user's selected config (configN.json, efuseN.json -> config.json / efuse.json)
 - Provide minimal logging immediately (to catch early import-time crashes)
 - Then initialize full project logging (if present)
 - Start the app in GUI (qasync) or CLI mode
 - Be robust when packaged by PyInstaller (onefile/onedir)
"""
import os
import sys
import json
import shutil
import argparse
import traceback
import asyncio
import signal
from pathlib import Path
from typing import Optional
import logging
LOG_FILE = 'xiaozhi_error.log'
logging.basicConfig(level = logging.DEBUG, format = '%(asctime)s %(levelname)s %(name)s - %(message)s', handlers = [
    logging.FileHandler(LOG_FILE, encoding = 'utf-8'),
    logging.StreamHandler(sys.stdout)])
_logger = logging.getLogger('main_prelog')

def log_uncaught(exctype, value, tb):
    
    try:
        f = open(LOG_FILE, 'a', encoding = 'utf-8')
        f.write('\n\n--- Uncaught exception ---\n')
        traceback.print_exception(exctype, value, tb, file = f)
        
        try:
            None(None, None)
            _logger.error('Uncaught exception', exc_info = (exctype, value, tb))
            return None
            with None:
                if not None:
                    pass
            
            try:
                continue
            except Exception:
                return None




sys.excepthook = log_uncaught
os.environ.setdefault('QT_DEBUG_PLUGINS', '1')
os.environ.setdefault('QT_LOGGING_RULES', '*=true')

def _default_bundled_config_dir():
    """
    Return a good guess for the bundled 'config' directory:
    - When running as script (dev): project_root/config (project root = parent of this file)
    - When running from PyInstaller onefile: sys._MEIPASS/config (but we avoid importing resource_finder)
    - When running from PyInstaller onedir: executable dir / 'config'
    """
    
    try:
        if getattr(sys, 'frozen', False) and hasattr(sys, '_MEIPASS'):
            return Path(getattr(sys, '_MEIPASS')) / 'config'
        if None(sys, 'frozen', False):
            return Path(sys.executable).parent / 'config'
        return Path(__file__).resolve().parent / 'config'
    except Exception:
        continue



def preload_selected_config():
    
    try:
        user_cfg_dir = Path.home() / '.xiaozhi_config'
        sel_file = user_cfg_dir / 'selected_config.json'
        if not sel_file.exists():
            _logger.debug('No selected_config.json found; skipping preload.')
            return None
        
        try:
            data = json.loads(sel_file.read_text(encoding = 'utf-8'))
            
            try:
                idx = int(data.get('selected_index', 0)) + 1
                _logger.info('Preload: selected_index=%s -> N=%s', data.get('selected_index'), idx)
                bundled = _default_bundled_config_dir()
                if not bundled.exists():
                    bundled = Path.cwd() / 'config'
                src_cfg = bundled / f'''config{idx}.json'''
                src_efuse = bundled / f'''efuse{idx}.json'''
                user_cfg_dir.mkdir(parents = True, exist_ok = True)
                dst_cfg = user_cfg_dir / 'config.json'
                dst_efuse = user_cfg_dir / 'efuse.json'
                if src_cfg.exists():
                    shutil.copy2(src_cfg, dst_cfg)
                    _logger.info('Preloaded: %s -> %s', src_cfg, dst_cfg)
                else:
                    _logger.debug('Preload source config not found: %s', src_cfg)
                if src_efuse.exists():
                    shutil.copy2(src_efuse, dst_efuse)
                    _logger.info('Preloaded: %s -> %s', src_efuse, dst_efuse)
                    return None
                    
                    try:
                        _logger.debug('Preload source efuse not found: %s', src_efuse)
                        return None
                        except Exception:
                            e = None
                            _logger.warning('Unable to parse selected_config.json: %s', e)
                            
                            try:
                                e = None
                                del e
                                return None
                                e = None
                                del e
                                
                                try:
                                    pass
                                except Exception:
                                    e = None
                                    _logger.exception('Failed during preload_selected_config: %s', e)
                                    e = None
                                    del e
                                    return None
                                    e = None
                                    del e







_logger.debug('Calling preload_selected_config() (safe pre-import preload).')
preload_selected_config()
_logger.debug('preload_selected_config() done.')

try:
    from src.utils.logging_config import setup_logging, get_logger
    
    try:
        setup_logging()
        
        try:
            logger = get_logger(__name__)
            logger.debug('Project logging initialized.')
            
            try:
                from PyQt5 import QtCore
                QtCore.QCoreApplication.setAttribute(QtCore.Qt.AA_EnableHighDpiScaling, True)
                QtCore.QCoreApplication.setAttribute(QtCore.Qt.AA_UseHighDpiPixmaps, True)
                logger.debug('Qt high-DPI attributes set.')
                
                def _project_uncaught(exctype, value, tb):
                    
                    try:
                        logger.critical('Unhandled exception', exc_info = (exctype, value, tb))
                        f = open(LOG_FILE, 'a', encoding = 'utf-8')
                        f.write('\n\n--- Unhandled exception (project) ---\n')
                        traceback.print_exception(exctype, value, tb, file = f)
                        
                        try:
                            None(None, None)
                            return None
                            with None:
                                if not None:
                                    pass
                            
                            try:
                                return None
                                
                                try:
                                    pass
                                except Exception:
                                    return None





                sys.excepthook = _project_uncaught
                
                def parse_args():
                    p = argparse.ArgumentParser(description = 'Xiaozhi - assistant')
                    p.add_argument('--mode', choices = [
                        'gui',
                        'cli'], default = 'gui')
                    p.add_argument('--protocol', choices = [
                        'mqtt',
                        'websocket'], default = 'websocket')
                    p.add_argument('--skip-activation', action = 'store_true')
                    return p.parse_args()

                
                async def handle_activation(mode = None):
                    pass
                # WARNING: Decompyle incomplete

                
                async def start_app(mode = None, protocol = None, skip_activation = None):
                    pass
                # WARNING: Decompyle incomplete

                
                def main():
                    args = parse_args()
                # WARNING: Decompyle incomplete

                if __name__ == '__main__':
                    
                    try:
                        logger.info('Starting main()')
                        code = main()
                        logger.info('Exiting with code %s', code)
                        logging.shutdown()
                        if not getattr(sys, 'frozen', False):
                            pass
                        
                        try:
                            if os.getenv('XIAOZHI_DEBUG_PAUSE', '1') == '1':
                                print('\nPress Enter to exit...')
                                input()
                            sys.exit(code)
                            return None
                            return None
                            except Exception:
                                e = None
                                _logger.warning('setup_logging() failed: %s', e)
                                
                                try:
                                    e = None
                                    del e
                                    continue
                                    e = None
                                    del e
                                    
                                    try:
                                        pass
                                    except Exception:
                                        e = None
                                        logger = _logger
                                        logger.warning('Could not import project logging_config; continuing with basic logging. %s', e)
                                        e = None
                                        del e
                                        continue
                                        e = None
                                        del e
                                        except Exception:
                                            logger.debug('PyQt5 not available at this moment (maybe CLI mode).')
                                            continue
                                        except Exception:
                                            e = None
                                            logger.exception('Fatal exception in __main__: %s', e)
                                            code = 1
                                            
                                            try:
                                                e = None
                                                del e
                                                continue
                                                e = None
                                                del e
                                                
                                                try:
                                                    except Exception:
                                                        continue
                                                except:
                                                    logger.info('Exiting with code %s', code)
                                                    logging.shutdown()
                                                    if not getattr(sys, 'frozen', False):
                                                        pass
                                                    if os.getenv('XIAOZHI_DEBUG_PAUSE', '1') == '1':
                                                        print('\nPress Enter to exit...')
                                                        input()
                                                    else:
                                                        except Exception:
                                                            pass
                                                        sys.exit(code)










