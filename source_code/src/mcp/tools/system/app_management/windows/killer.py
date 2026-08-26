# Source Generated with Decompyle++
# File: killer.pyc (Python 3.12)

'''Windows系统应用程序关闭器.

提供Windows平台下的应用程序关闭功能
'''
import json
import subprocess
from typing import Any, Dict, List
from src.utils.logging_config import get_logger
from utils import AppMatcher
logger = get_logger(__name__)

def list_running_applications(filter_name = None):
    '''
    列出Windows上正在运行的应用程序.
    '''
    apps = []
# WARNING: Decompyle incomplete


def kill_application_group(apps = None, app_name = None, force = None):
    '''按分组关闭Windows应用程序.

    Args:
        apps: 匹配的应用程序进程列表
        app_name: 应用程序名称
        force: 是否强制关闭

    Returns:
        bool: 关闭是否成功
    '''
    
    try:
        logger.info(f'''[WindowsKiller] 开始分组关闭Windows应用: {app_name}, 找到 {len(apps)} 个相关进程''')
        success = _kill_by_image_name(apps, force)
        if success:
            logger.info(f'''[WindowsKiller] 成功通过应用名称整体关闭: {app_name}''')
            return True
            
            try:
                success = _kill_by_process_groups(apps, force)
                if success:
                    logger.info(f'''[WindowsKiller] 成功通过进程分组关闭: {app_name}''')
                    return True
                    
                    try:
                        success = _kill_individual_processes(apps, force)
                        logger.info(f'''[WindowsKiller] 通过逐个关闭完成: {app_name}, 成功: {success}''')
                        return success
                    except Exception:
                        e = None
                        logger.error(f'''[WindowsKiller] Windows分组关闭失败: {e}''')
                        e = None
                        del e
                        return False
                        e = None
                        del e





def kill_application(pid = None, force = None):
    '''
    在Windows上关闭单个应用程序.
    '''
    
    try:
        logger.info(f'''[WindowsKiller] 尝试关闭Windows应用程序，PID: {pid}, 强制关闭: {force}''')
        if force:
            result = subprocess.run([
                'taskkill',
                '/PID',
                str(pid),
                '/F'], capture_output = True, text = True, timeout = 10)
        else:
            result = subprocess.run([
                'taskkill',
                '/PID',
                str(pid)], capture_output = True, text = True, timeout = 10)
        success = result.returncode == 0
        if success:
            logger.info(f'''[WindowsKiller] 成功关闭应用程序，PID: {pid}''')
            return success
        None.warning(f'''[WindowsKiller] 关闭应用程序失败，PID: {pid}, 错误信息: {result.stderr}''')
        return success
    except (subprocess.TimeoutExpired, subprocess.SubprocessError):
        e = None
        logger.error(f'''[WindowsKiller] Windows关闭应用程序异常，PID: {pid}, 错误: {e}''')
        e = None
        del e
        return False
        e = None
        del e



def _matches_process_name(filter_name = None, proc_name = None, window_title = None, exe_path = ('', '')):
    '''
    智能匹配进程名称.
    '''
    
    try:
        app_info = {
            'name': proc_name,
            'display_name': proc_name,
            'window_title': window_title,
            'command': exe_path }
        score = AppMatcher.match_application(filter_name, app_info)
        return score >= 30
    except Exception:
        filter_lower = filter_name.lower()
        proc_lower = proc_name.lower()
        if not filter_lower == proc_lower:
            filter_lower == proc_lower
            if not filter_lower in proc_lower:
                filter_lower in proc_lower
                if window_title:
                    window_title
        return 



def _is_system_process(proc_name = None):
    '''
    判断是否为系统进程.
    '''
    system_processes = {
        'dwm',
        'smss',
        'csrss',
        'lsass',
        'ctfmon',
        'sihost',
        'audiodg',
        'conhost',
        'cortana',
        'dllhost',
        'lockapp',
        'spoolsv',
        'svchost',
        'wininit',
        'explorer',
        'searchui',
        'services',
        'winlogon',
        'taskhostw',
        'fontdrvhost',
        'runtimebroker',
        'useroobebroker',
        'shellexperiencehost',
        'applicationframehost',
        'startmenuexperiencehost'}
    return proc_name.lower() in system_processes


def _deduplicate_and_sort_apps(apps = None):
    '''
    去重并排序应用程序列表.
    '''
    seen_pids = set()
    unique_apps = []
    for app in apps:
        if not app['pid'] not in seen_pids:
            continue
        seen_pids.add(app['pid'])
        unique_apps.append(app)
    unique_apps.sort(key = (lambda x: x['name'].lower()))
    logger.info(f'''[WindowsKiller] 进程扫描完成，去重后找到 {len(unique_apps)} 个应用程序''')
    return unique_apps


def _kill_by_image_name(apps = None, force = None):
    '''
    通过镜像名称整体关闭应用程序.
    '''
    
    try:
        image_names = set()
        for app in apps:
            name = app.get('name', '')
            if not name:
                continue
                
                try:
                    if not name.lower().endswith('.exe'):
                        name += '.exe'
                    image_names.add(name)
                    continue
                    if not image_names:
                        return False
                        
                        try:
                            logger.info(f'''[WindowsKiller] 尝试通过镜像名称关闭: {list(image_names)}''')
                            success_count = 0
                            for image_name in image_names:
                                result = subprocess.run(cmd, capture_output = True, text = True, timeout = 10)
                                if result.returncode == 0:
                                    success_count += 1
                                    logger.info(f'''[WindowsKiller] 成功关闭镜像: {image_name}''')
                                else:
                                    logger.debug(f'''[WindowsKiller] 关闭镜像失败: {image_name}, 错误: {result.stderr}''')
                                    
                                    try:
                                        continue
                                        return success_count > 0
                                        except (subprocess.TimeoutExpired, subprocess.SubprocessError):
                                            e = None
                                            logger.debug(f'''[WindowsKiller] 关闭镜像异常: {image_name}, 错误: {e}''')
                                            
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
                                                    logger.debug(f'''[WindowsKiller] 镜像名称关闭异常: {e}''')
                                                    e = None
                                                    del e
                                                    return False
                                                    e = None
                                                    del e








def _kill_by_process_groups(apps = None, force = None):
    '''
    按进程组智能关闭应用程序.
    '''
    
    try:
        process_groups = { }
        for app in apps:
            name = app.get('name', '')
            if not name:
                continue
                
                try:
                    base_name = _get_base_process_name(name)
                    if base_name not in process_groups:
                        process_groups[base_name] = []
                    process_groups[base_name].append(app)
                    continue
                    logger.info(f'''[WindowsKiller] 识别出 {len(process_groups)} 个进程组: {list(process_groups.keys())}''')
                    success_count = 0
                    for group_name, group_apps in process_groups.items():
                        main_process = _find_main_process(group_apps)
                        if main_process:
                            pid = main_process.get('pid')
                            if pid:
                                success = kill_application(pid, force)
                                if success:
                                    success_count += 1
                                    logger.info(f'''[WindowsKiller] 成功关闭进程组 {group_name} 的主进程 (PID: {pid})''')
                                else:
                                    for app in group_apps:
                                        if not kill_application(app.get('pid'), force):
                                            continue
                                        success_count += 1
                                    
                                    try:
                                        continue
                                        return success_count > 0
                                        except Exception:
                                            e = None
                                            logger.debug(f'''[WindowsKiller] 关闭进程组失败: {group_name}, 错误: {e}''')
                                            
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
                                                    logger.debug(f'''[WindowsKiller] 进程组关闭异常: {e}''')
                                                    e = None
                                                    del e
                                                    return False
                                                    e = None
                                                    del e







def _kill_individual_processes(apps = None, force = None):
    '''
    逐个关闭进程（兜底方案）.
    '''
    
    try:
        logger.info(f'''[WindowsKiller] 开始逐个关闭 {len(apps)} 个进程''')
        success_count = 0
        for app in apps:
            pid = app.get('pid')
            if not pid:
                continue
                
                try:
                    success = kill_application(pid, force)
                    if not success:
                        continue
                        
                        try:
                            success_count += 1
                            logger.debug(f'''[WindowsKiller] 成功关闭进程: {app.get('name')} (PID: {pid})''')
                            continue
                            logger.info(f'''[WindowsKiller] 逐个关闭完成，成功关闭 {success_count}/{len(apps)} 个进程''')
                            return success_count > 0
                        except Exception:
                            e = None
                            logger.error(f'''[WindowsKiller] 逐个关闭异常: {e}''')
                            e = None
                            del e
                            return False
                            e = None
                            del e





def _get_base_process_name(process_name = None):
    '''
    获取基础进程名称（用于分组）.
    '''
    
    try:
        return AppMatcher.get_process_group(process_name)
    except Exception:
        name = process_name.lower().replace('.exe', '')
        if 'chrome' in name:
            return 'chrome'
        if 'qq' in name and 'music' not in name:
            return 'qq'
        return 



def _find_main_process(processes = None):
    '''
    在进程组中找到主进程.
    '''
    if not processes:
        return { }
    for proc in None:
        window_title = proc.get('window_title', '')
        if not window_title:
            continue
        if not window_title.strip():
            continue
        
        return None, proc
    
    try:
        return main_proc
    except (ValueError, TypeError):
        pass

    
    try:
        min(processes, key = (lambda p: p.get('pid', 999999))) = None
        return main_proc
    except (ValueError, TypeError):
        return processes[0]


