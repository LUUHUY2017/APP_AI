# Source Generated with Decompyle++
# File: manager.pyc (Python 3.12)

'''
日程管理器 负责日程数据的存储、查询、更新等核心功能.
'''
import os
from typing import List
from src.utils.logging_config import get_logger
from database import get_calendar_database
from models import CalendarEvent
logger = get_logger(__name__)

class CalendarManager:
    '''
    日程管理器.
    '''
    
    def __init__(self):
        self.db = get_calendar_database()
        self._migrate_from_json_if_exists()

    
    def init_tools(self, add_tool, PropertyList, Property, PropertyType):
        '''
        初始化并注册所有日程管理工具.
        '''
        create_event = create_event
        delete_event = delete_event
        delete_events_batch = delete_events_batch
        get_categories = get_categories
        get_events_by_date = get_events_by_date
        get_upcoming_events = get_upcoming_events
        update_event = update_event
        import tools
        create_event_props = PropertyList([
            Property('title', PropertyType.STRING),
            Property('start_time', PropertyType.STRING),
            Property('end_time', PropertyType.STRING, default_value = ''),
            Property('description', PropertyType.STRING, default_value = ''),
            Property('category', PropertyType.STRING, default_value = '默认'),
            Property('reminder_minutes', PropertyType.INTEGER, default_value = 15)])
        add_tool(('self.calendar.create_event', "Create a new calendar event with intelligent duration setting and conflict detection. Automatically sets appropriate duration based on category if end_time is not provided.\nUse this tool when user wants to:\n1. Schedule a meeting, appointment, or task\n2. Create reminders or notifications\n3. Block time for work, personal activities\n4. Set up recurring activities (meetings, breaks, etc.)\n\nIntelligent Duration Rules:\n- '提醒', '休息', '站立' category: 5 minutes\n- '会议', '工作' category: 1 hour\n- Title contains '提醒', '站立', '休息': 5 minutes\n- Default: 30 minutes\n\nArgs:\n  title: Event title (required)\n  start_time: Start time in ISO format '2024-01-01T10:00:00' (required)\n  end_time: End time, auto-calculated if not provided\n  description: Event description\n  category: Event category (默认/工作/个人/会议/提醒)\n  reminder_minutes: Reminder time in minutes before event", create_event_props, create_event))
        query_events_props = PropertyList([
            Property('date_type', PropertyType.STRING, default_value = 'today'),
            Property('category', PropertyType.STRING, default_value = ''),
            Property('start_date', PropertyType.STRING, default_value = ''),
            Property('end_date', PropertyType.STRING, default_value = '')])
        add_tool(('self.calendar.get_events', "Query calendar events within specified time range with flexible filtering options. Supports multiple time range types and category filtering.\nUse this tool when user asks about:\n1. What's scheduled for today/tomorrow/this week/this month\n2. What meetings/events are coming up\n3. Show me my schedule for specific dates\n4. Filter events by category (work, personal, meetings, etc.)\n5. Check availability for a time period\n\nTime Range Options:\n- 'today': Today's events\n- 'tomorrow': Tomorrow's events\n- 'week': This week's events\n- 'month': This month's events\n- Custom: Use start_date and end_date\n\nArgs:\n  date_type: Query type (today/tomorrow/week/month)\n  category: Filter by category (optional)\n  start_date: Custom start date in ISO format (optional)\n  end_date: Custom end date in ISO format (optional)", query_events_props, get_events_by_date))
        upcoming_events_props = PropertyList([
            Property('hours', PropertyType.INTEGER, default_value = 24)])
        add_tool(('self.calendar.get_upcoming_events', "Get upcoming calendar events within specified hours with time-until calculations. Shows how much time remains until each event starts.\nUse this tool when user asks about:\n1. What's coming up next\n2. What events are happening soon\n3. What's my next meeting/appointment\n4. Show me events in the next few hours\n5. What should I prepare for\n\nFeatures:\n- Shows time remaining until each event ('2小时30分钟后')\n- Sorts events by start time\n- Configurable time range (default 24 hours)\n- Excludes past events\n\nArgs:\n  hours: Time range in hours to look ahead (default: 24)", upcoming_events_props, get_upcoming_events))
        update_event_props = PropertyList([
            Property('event_id', PropertyType.STRING),
            Property('title', PropertyType.STRING, default_value = ''),
            Property('start_time', PropertyType.STRING, default_value = ''),
            Property('end_time', PropertyType.STRING, default_value = ''),
            Property('description', PropertyType.STRING, default_value = ''),
            Property('category', PropertyType.STRING, default_value = ''),
            Property('reminder_minutes', PropertyType.INTEGER, default_value = 15)])
        add_tool(('self.calendar.update_event', 'Update an existing calendar event with partial field updates. Allows modification of any event property without affecting others.\nUse this tool when user wants to:\n1. Change meeting time or duration\n2. Update event title or description\n3. Modify event category or reminder settings\n4. Reschedule appointments\n5. Add or change event details\n\nFeatures:\n- Partial updates (only specify fields to change)\n- Automatic timestamp updating\n- Preserves unchanged fields\n\nArgs:\n  event_id: Unique event identifier (required)\n  title: New event title (optional)\n  start_time: New start time in ISO format (optional)\n  end_time: New end time in ISO format (optional)\n  description: New description (optional)\n  category: New category (optional)\n  reminder_minutes: New reminder time in minutes (optional)', update_event_props, update_event))
        delete_event_props = PropertyList([
            Property('event_id', PropertyType.STRING)])
        add_tool(('self.calendar.delete_event', 'Delete a calendar event permanently from the schedule. Removes the event and all associated reminders.\nUse this tool when user wants to:\n1. Cancel a meeting or appointment\n2. Remove completed or outdated events\n3. Clear schedule conflicts\n4. Delete duplicate events\n5. Clean up old events\n\nArgs:\n  event_id: Unique identifier of the event to delete', delete_event_props, delete_event))
        delete_batch_props = PropertyList([
            Property('start_date', PropertyType.STRING, default_value = ''),
            Property('end_date', PropertyType.STRING, default_value = ''),
            Property('category', PropertyType.STRING, default_value = ''),
            Property('date_type', PropertyType.STRING, default_value = ''),
            Property('delete_all', PropertyType.BOOLEAN, default_value = False)])
        add_tool(('self.calendar.delete_events_batch', "Batch delete multiple calendar events based on specified criteria or delete all events. Supports flexible filtering and time-based deletion.\nUse this tool when user wants to:\n1. Clear all events from schedule\n2. Remove all events from a specific time period (today/week/month)\n3. Delete all events of a specific category\n4. Clean up schedule for a date range\n5. Bulk remove outdated or completed events\n\nDeletion Options:\n- delete_all=true: Remove all events from calendar\n- date_type: Remove events from 'today'/'tomorrow'/'week'/'month'\n- category: Remove all events of specific category\n- start_date + end_date: Remove events in custom date range\n\nSafety Features:\n- Returns count of deleted events\n- Lists titles of deleted events for confirmation\n- Transaction-safe deletion\n\nArgs:\n  start_date: Start date for range deletion (ISO format, optional)\n  end_date: End date for range deletion (ISO format, optional)\n  category: Delete events of specific category (optional)\n  date_type: Quick deletion for today/tomorrow/week/month (optional)\n  delete_all: Delete ALL events if true (default: false)", delete_batch_props, delete_events_batch))
        add_tool(('self.calendar.get_categories', 'Get all available calendar event categories for organizing and filtering events. Returns the complete list of categories that can be used when creating or updating events.\nUse this tool when user asks about:\n1. What categories are available for events\n2. How to organize or classify events\n3. What types of events can be created\n4. Available options for event categorization\n\nDefault Categories:\n- 默认 (Default)\n- 工作 (Work)\n- 个人 (Personal)\n- 会议 (Meeting)\n- 提醒 (Reminder)', PropertyList(), get_categories))

    
    def _migrate_from_json_if_exists(self):
        '''
        从旧的JSON文件迁移数据（如果存在）
        '''
        get_project_root = get_project_root
        get_user_cache_dir = get_user_cache_dir
        import src.utils.resource_finder
        
        try:
            project_root = get_project_root()
            json_file = project_root / 'cache' / 'calendar_data.json'
            if os.path.exists(json_file):
                logger.info('发现旧的JSON数据文件，开始迁移到SQLite...')
                if self.db.migrate_from_json(json_file):
                    backup_file = f'''{json_file}.backup'''
                    os.rename(json_file, backup_file)
                    logger.info(f'''数据迁移完成，原文件已备份为: {backup_file}''')
                    return None
                logger.warning('数据迁移失败，保留原JSON文件')
                return None
            return None
        except Exception:
            user_cache_dir = get_user_cache_dir(create = False)
            json_file = user_cache_dir / 'calendar_data.json'
            continue


    
    def add_event(self = None, event = None):
        '''
        添加事件.
        '''
        return self.db.add_event(event.to_dict())

    
    def get_events(self = None, start_date = None, end_date = None, category = (None, None, None)):
        '''
        获取事件列表.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def update_event(self = None, event_id = None, **kwargs):
        '''
        更新事件.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def delete_event(self = None, event_id = None):
        '''
        删除事件.
        '''
        return self.db.delete_event(event_id)

    
    def delete_events_batch(self = None, start_date = None, end_date = None, category = (None, None, None, False), delete_all = ('start_date', str, 'end_date', str, 'category', str, 'delete_all', bool)):
        '''
        批量删除事件.
        '''
        return self.db.delete_events_batch(start_date, end_date, category, delete_all)

    
    def get_categories(self = None):
        '''
        获取所有分类.
        '''
        return self.db.get_categories()


_calendar_manager = None

def get_calendar_manager():
    '''
    获取日程管理器单例.
    '''
    pass
# WARNING: Decompyle incomplete

