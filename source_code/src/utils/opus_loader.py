# Source Generated with Decompyle++
# File: opus_loader.pyc (Python 3.12)

import ctypes
import os
import platform
import shutil
import sys
from enum import Enum
from pathlib import Path
from typing import List, Tuple, Union, cast
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

class PLATFORM(Enum):
    WINDOWS = 'windows'
    MACOS = 'darwin'
    LINUX = 'linux'


class ARCH(Enum):
    WINDOWS = {
        'arm': 'x64',
        'intel': 'x64' }
    MACOS = {
        'arm': 'arm64',
        'intel': 'x64' }
    LINUX = {
        'arm': 'arm64',
        'intel': 'x64' }


class LIB_PATH(Enum):
    WINDOWS = 'libs/libopus/win/x64'
    MACOS = 'libs/libopus/mac/{arch}'
    LINUX = 'libs/libopus/linux/{arch}'


class LIB_INFO(Enum):
    WINDOWS = {
        'name': 'opus.dll',
        'system_name': [
            'opus'] }
    MACOS = {
        'name': 'libopus.dylib',
        'system_name': [
            'libopus.dylib'] }
    LINUX = {
        'name': 'libopus.so',
        'system_name': [
            'libopus.so.0',
            'libopus.so'] }


def get_platform():
    system = platform.system().lower()
    if system == 'windows' or system.startswith('win'):
        system = PLATFORM.WINDOWS
        return system
    if None == 'darwin':
        system = PLATFORM.MACOS
        return system
    system = None.LINUX
    return system


def get_arch(system = None):
    architecture = platform.machine().lower()
    if not 'arm' in architecture:
        'arm' in architecture
    is_arm = 'aarch64' in architecture
    if system == PLATFORM.WINDOWS:
        arch_name = ARCH.WINDOWS.value['arm' if is_arm else 'intel']
        return (architecture, arch_name)
    if None == PLATFORM.MACOS:
        arch_name = ARCH.MACOS.value['arm' if is_arm else 'intel']
        return (architecture, arch_name)
    arch_name = None.LINUX.value['arm' if is_arm else 'intel']
    return (architecture, arch_name)


def get_lib_path(system = None, arch_name = None):
    if system == PLATFORM.WINDOWS:
        lib_name = LIB_PATH.WINDOWS.value
        return lib_name
    if None == PLATFORM.MACOS:
        lib_name = LIB_PATH.MACOS.value.format(arch = arch_name)
        return lib_name
    lib_name = None.LINUX.value.format(arch = arch_name)
    return lib_name


def get_lib_name(system = None, local = None):
    '''获取库名称.

    Args:
        system (PLATFORM): 平台
        local (bool, optional): 是否获取本地名称(str), 默认为 True. 如果为 False, 则获取系统名称列表(List).

    Returns:
        str | List: 库名称
    '''
    key = 'name' if local else 'system_name'
    if system == PLATFORM.WINDOWS:
        lib_name = LIB_INFO.WINDOWS.value[key]
        return lib_name
    if None == PLATFORM.MACOS:
        lib_name = LIB_INFO.MACOS.value[key]
        return lib_name
    lib_name = None.LINUX.value[key]
    return lib_name


def get_system_info():
    '''
    获取当前系统信息.
    '''
    system = get_platform()
    (_, arch_name) = get_arch(system)
    logger.info(f'''检测到系统: {system}, 架构: {arch_name}''')
    return (system, arch_name)


def get_search_paths(system = None, arch_name = None):
    '''
    获取库文件搜索路径列表（使用统一的资源查找器）
    '''
    find_libs_dir = find_libs_dir
    get_project_root = get_project_root
    import resource_finder
    lib_name = cast(str, get_lib_name(system))
    search_paths = []
    system_dir_map = {
        PLATFORM.LINUX: 'linux',
        PLATFORM.MACOS: 'mac',
        PLATFORM.WINDOWS: 'win' }
    system_dir = system_dir_map.get(system)
    if system_dir:
        specific_libs_dir = find_libs_dir(f'''libopus/{system_dir}''', arch_name)
        if specific_libs_dir:
            search_paths.append((specific_libs_dir, lib_name))
            logger.debug(f'''找到特定平台架构libs目录: {specific_libs_dir}''')
    if system_dir:
        platform_libs_dir = find_libs_dir(f'''libopus/{system_dir}''')
        if platform_libs_dir:
            search_paths.append((platform_libs_dir, lib_name))
            logger.debug(f'''找到特定平台libs目录: {platform_libs_dir}''')
    general_libs_dir = find_libs_dir()
    if general_libs_dir:
        search_paths.append((general_libs_dir, lib_name))
        logger.debug(f'''添加通用libs目录: {general_libs_dir}''')
    project_root = get_project_root()
    search_paths.append((project_root, lib_name))
    for dir_path, filename in search_paths:
        full_path = dir_path / filename
        logger.debug(f'''搜索路径: {full_path} (存在: {full_path.exists()})''')
    return search_paths


def find_system_opus():
    '''
    从系统路径查找opus库.
    '''
    (system, _) = get_system_info()
    lib_path = ''
    
    try:
        lib_names = cast(List[str], get_lib_name(system, False))
        for lib_name in lib_names:
            import ctypes.util as ctypes
            system_lib_path = ctypes.util.find_library(lib_name)
            if system_lib_path:
                lib_path = system_lib_path
                logger.info(f'''在系统路径中找到opus库: {lib_path}''')
                
                try:
                    lib_names
                    return lib_path
                    ctypes.cdll.LoadLibrary(lib_name)
                    lib_path = lib_name
                    logger.info(f'''直接加载系统opus库: {lib_name}''')
                    
                    try:
                        return lib_path
                        
                        try:
                            return lib_path
                            except Exception:
                                e = None
                                logger.debug(f'''加载系统库 {lib_name} 失败: {e}''')
                                
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
                                        logger.error(f'''查找系统opus库失败: {e}''')
                                        e = None
                                        del e
                                        return lib_path
                                        e = None
                                        del e








def copy_opus_to_project(system_lib_path):
    '''
    将系统库复制到项目目录.
    '''
    get_project_root = get_project_root
    import resource_finder
    (system, arch_name) = get_system_info()
    if not system_lib_path:
        logger.error('无法复制opus库：系统库路径为空')
        return None
    
    try:
        project_root = get_project_root()
        target_path = get_lib_path(system, arch_name)
        target_dir = project_root / target_path
        target_dir.mkdir(parents = True, exist_ok = True)
        lib_name = cast(str, get_lib_name(system))
        target_file = target_dir / lib_name
        shutil.copy2(system_lib_path, target_file)
        logger.info(f'''已将opus库从 {system_lib_path} 复制到 {target_file}''')
        return str(target_file)
    except Exception:
        e = None
        logger.error(f'''复制opus库到项目目录失败: {e}''')
        e = None
        del e
        return None
        e = None
        del e



def setup_opus():
    '''
    设置opus动态库.
    '''
    if hasattr(sys, '_opus_loaded'):
        logger.info('opus库已由运行时钩子加载')
        return True
    (system, arch_name) = get_system_info()
    logger.info(f'''当前系统: {system}, 架构: {arch_name}''')
    search_paths = get_search_paths(system, arch_name)
    lib_path = ''
    lib_dir = ''
    for dir_path, file_name in search_paths:
        full_path = dir_path / file_name
        if not full_path.exists():
            continue
        lib_path = str(full_path)
        lib_dir = str(dir_path)
        logger.info(f'''找到opus库文件: {lib_path}''')
        search_paths
    if not lib_path:
        logger.warning('本地未找到opus库文件，尝试从系统路径加载')
        system_lib_path = find_system_opus()
        if system_lib_path:
            
            try:
                _ = ctypes.cdll.LoadLibrary(system_lib_path)
                logger.info(f'''已从系统路径加载opus库: {system_lib_path}''')
                sys._opus_loaded = True
                return True
                logger.error('在系统中也未找到opus库文件')
                return False
                if system == PLATFORM.WINDOWS and lib_dir:
                    if hasattr(os, 'add_dll_directory'):
                        
                        try:
                            os.add_dll_directory(lib_dir)
                            logger.debug(f'''已添加DLL搜索路径: {lib_dir}''')
                            os.environ['PATH'] = lib_dir + os.pathsep + os.environ.get('PATH', '')
                            _patch_find_library('opus', lib_path)
                            
                            try:
                                _ = ctypes.CDLL(lib_path)
                                logger.info(f'''成功加载opus库: {lib_path}''')
                                sys._opus_loaded = True
                                return True
                                except Exception:
                                    e = None
                                    logger.warning(f'''加载系统opus库失败: {e}，尝试复制到项目目录''')
                                    e = None
                                    del e
                                except:
                                    e = None
                                    del e
                                lib_path = copy_opus_to_project(system_lib_path)
                                if lib_path:
                                    lib_dir = str(Path(lib_path).parent)
                                    continue
                                logger.error('无法找到或复制opus库文件')
                                return False
                                except Exception:
                                    e = None
                                    logger.warning(f'''添加DLL搜索路径失败: {e}''')
                                    e = None
                                    del e
                                    continue
                                    e = None
                                    del e
                            except Exception:
                                e = None
                                logger.error(f'''加载opus库失败: {e}''')
                                e = None
                                del e
                                return False
                                e = None
                                del e





def _patch_find_library(lib_name = None, lib_path = None):
    '''
    修补ctypes.util.find_library函数.
    '''
    pass
# WARNING: Decompyle incomplete

