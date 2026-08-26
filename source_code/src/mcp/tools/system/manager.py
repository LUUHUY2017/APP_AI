# Source Generated with Decompyle++
# File: manager.pyc (Python 3.12)

'''系统工具管理器.

负责系统工具的初始化、配置和MCP工具注册
'''
from typing import Any, Dict
from src.utils.logging_config import get_logger
from app_management.killer import kill_application, list_running_applications
from app_management.launcher import launch_application
from app_management.scanner import scan_installed_applications
from tools import get_system_status, set_volume
logger = get_logger(__name__)

class SystemToolsManager:
    '''
    系统工具管理器.
    '''
    
    def __init__(self):
        '''
        初始化系统工具管理器.
        '''
        self._initialized = False
        logger.info('[SystemManager] 系统工具管理器初始化')

    
    def init_tools(self, add_tool, PropertyList, Property, PropertyType):
        '''
        初始化并注册所有系统工具.
        '''
        
        try:
            logger.info('[SystemManager] 开始注册系统工具')
            self._register_device_status_tool(add_tool, PropertyList)
            self._register_volume_control_tool(add_tool, PropertyList, Property, PropertyType)
            self._register_app_launcher_tool(add_tool, PropertyList, Property, PropertyType)
            self._register_app_scanner_tool(add_tool, PropertyList, Property, PropertyType)
            self._register_app_killer_tools(add_tool, PropertyList, Property, PropertyType)
            self._initialized = True
            logger.info('[SystemManager] 系统工具注册完成')
            return None
        except Exception:
            e = None
            logger.error(f'''[SystemManager] 系统工具注册失败: {e}''', exc_info = True)
            raise 
            e = None
            del e


    
    def _register_device_status_tool(self, add_tool, PropertyList):
        '''
        注册设备状态查询工具.
        '''
        add_tool(('self.get_device_status', 'Provides comprehensive real-time system information including OS details, CPU usage, memory status, disk usage, battery info, audio speaker volume and settings, and application state.\nUse this tool for: \n1. Answering questions about current system condition\n2. Getting detailed hardware and software status\n3. Checking current audio volume level and mute status\n4. As the first step before controlling device settings', PropertyList(), get_system_status))
        logger.debug('[SystemManager] 注册设备状态工具成功')

    
    def _register_volume_control_tool(self, add_tool, PropertyList, Property, PropertyType):
        '''
        注册音量控制工具.
        '''
        volume_props = PropertyList([
            Property('volume', PropertyType.INTEGER, min_value = 0, max_value = 100)])
        add_tool(('self.audio_speaker.set_volume', "Set system speaker volume to an absolute value (0–100). Always provide integer 'volume'.\nUse this tool when:\n1. User asks to set volume to a specific percent/number (e.g., '音量设为50%')\n2. User asks to increase/decrease volume relatively ('调大/调小一点'): first call `self.get_device_status` to read current audio_speaker.volume, compute a target within 0–100, then call this tool\n3. Ensuring volume stays within 0–100 (do not guess current value)\n\nParameters:\n- volume: INTEGER in [0, 100] (absolute target)\n\nNotes: If the current volume is unknown, do NOT assume it — call `self.get_device_status` first. To mute, set volume=0. This tool does not toggle mute state.", volume_props, set_volume))
        logger.debug('[SystemManager] 注册音量控制工具成功')

    
    def _register_app_launcher_tool(self, add_tool, PropertyList, Property, PropertyType):
        '''
        注册应用程序启动工具.
        '''
        app_props = PropertyList([
            Property('app_name', PropertyType.STRING)])
        add_tool(('self.application.launch', "Launch desktop applications and software programs by name. This tool opens applications installed on the user's computer across Windows, macOS, and Linux platforms. It automatically detects the operating system and uses appropriate launch methods.\nUse this tool when the user wants to:\n1. Open specific software applications (e.g., 'QQ', 'QQ音乐', 'WeChat', '微信')\n2. Launch system utilities (e.g., 'Calculator', '计算器', 'Notepad', '记事本')\n3. Start browsers (e.g., 'Chrome', 'Firefox', 'Safari')\n4. Open media players (e.g., 'VLC', 'Windows Media Player')\n5. Launch development tools (e.g., 'VS Code', 'PyCharm')\n6. Start games or other installed programs\n\nExamples of valid app names:\n- Chinese: 'QQ音乐', '微信', '计算器', '记事本', '浏览器'\n- English: 'QQ', 'WeChat', 'Calculator', 'Notepad', 'Chrome'\n- Mixed: 'QQ Music', 'Microsoft Word', 'Adobe Photoshop'\n\nThe system will try multiple launch strategies including direct execution, system commands, and path searching to find and start the application.", app_props, launch_application))
        logger.debug('[SystemManager] 注册应用程序启动工具成功')

    
    def _register_app_scanner_tool(self, add_tool, PropertyList, Property, PropertyType):
        '''
        注册应用程序扫描工具.
        '''
        scanner_props = PropertyList([
            Property('force_refresh', PropertyType.BOOLEAN, default_value = False)])
        add_tool(('self.application.scan_installed', "Scan and list all installed applications on the system. This tool provides a comprehensive list of available applications that can be launched using the launch tool. It scans system directories, registry (Windows), and application folders to find installed software.\nUse this tool when:\n1. User asks what applications are available on the system\n2. You need to find the correct application name before launching\n3. User wants to see all installed software\n4. Application launch fails and you need to check available apps\n\nThe scan results include both system applications (Calculator, Notepad) and user-installed software (QQ, WeChat, Chrome, etc.). Each application entry contains the clean name for launching and display name for reference.\n\nAfter scanning, use the 'name' field from results with self.application.launch to start applications. For example, if scan shows {name: 'QQ', display_name: 'QQ音乐'}, use self.application.launch with app_name='QQ' to launch it.", scanner_props, scan_installed_applications))
        logger.debug('[SystemManager] 注册应用程序扫描工具成功')

    
    def _register_app_killer_tools(self, add_tool, PropertyList, Property, PropertyType):
        '''
        注册应用程序关闭工具.
        '''
        killer_props = PropertyList([
            Property('app_name', PropertyType.STRING),
            Property('force', PropertyType.BOOLEAN, default_value = False)])
        add_tool(('self.application.kill', "Close or terminate running applications by name. This tool can gracefully close applications or force-kill them if needed. It automatically finds running processes matching the application name and terminates them.\nUse this tool when:\n1. User asks to close, quit, or exit an application\n2. User wants to stop or terminate a running program\n3. Application is unresponsive and needs to be force-closed\n4. User says 'close QQ', 'quit Chrome', 'stop music player', etc.\n\nParameters:\n- app_name: Name of the application to close (e.g., 'QQ', 'Chrome', 'Calculator')\n- force: Set to true for force-kill unresponsive applications (default: false)\n\nThe tool will find all running processes matching the application name and attempt to close them gracefully. If force=true, it will use system kill commands to immediately terminate the processes.", killer_props, kill_application))
        list_props = PropertyList([
            Property('filter_name', PropertyType.STRING, default_value = '')])
        add_tool(('self.application.list_running', 'List all currently running applications and processes. This tool provides real-time information about active applications on the system, including process IDs, names, and commands.\nUse this tool when:\n1. User asks what applications are currently running\n2. You need to check if a specific application is running before closing it\n3. User wants to see active processes or programs\n4. Troubleshooting application issues\n\nParameters:\n- filter_name: Optional filter to show only applications containing this name\n\nReturns detailed information about running applications including process IDs which can be useful for targeted application management.', list_props, list_running_applications))
        logger.debug('[SystemManager] 注册应用程序关闭工具成功')

    
    def is_initialized(self = None):
        '''
        检查管理器是否已初始化.
        '''
        return self._initialized

    
    def get_status(self = None):
        '''
        获取管理器状态.
        '''
        return {
            'initialized': self._initialized,
            'tools_count': 6,
            'available_tools': [
                'get_device_status',
                'set_volume',
                'launch_application',
                'scan_installed_applications',
                'kill_application',
                'list_running_applications'] }


_system_tools_manager = None

def get_system_tools_manager():
    '''
    获取系统工具管理器单例.
    '''
    pass
# WARNING: Decompyle incomplete

