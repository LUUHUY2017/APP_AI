# Source Generated with Decompyle++
# File: database.pyc (Python 3.12)

'''
日程管理SQLite数据库操作模块.
'''
import os
import sqlite3
from contextlib import contextmanager
from datetime import datetime
from typing import Any, Dict, List, Optional
from src.utils.logging_config import get_logger
from src.utils.resource_finder import get_user_data_dir
logger = get_logger(__name__)

def _get_database_file_path():
    '''
    获取数据库文件路径，确保在可写目录中.
    '''
    data_dir = get_user_data_dir()
    database_file = str(data_dir / 'calendar.db')
    logger.debug(f'''使用数据库文件路径: {database_file}''')
    return database_file

DATABASE_FILE = _get_database_file_path()

class CalendarDatabase:
    '''
    日程管理数据库操作类.
    '''
    
    def __init__(self):
        self.db_file = DATABASE_FILE
        self._ensure_database()

    
    def _ensure_database(self):
        '''
        确保数据库和表存在.
        '''
        os.makedirs(os.path.dirname(self.db_file), exist_ok = True)
        conn = self._get_connection()
        conn.execute("\n                CREATE TABLE IF NOT EXISTS events (\n                    id TEXT PRIMARY KEY,\n                    title TEXT NOT NULL,\n                    start_time TEXT NOT NULL,\n                    end_time TEXT NOT NULL,\n                    description TEXT DEFAULT '',\n                    category TEXT DEFAULT '默认',\n                    reminder_minutes INTEGER DEFAULT 15,\n                    reminder_time TEXT,\n                    reminder_sent BOOLEAN DEFAULT 0,\n                    created_at TEXT NOT NULL,\n                    updated_at TEXT NOT NULL\n                )\n            ")
        conn.execute('\n                CREATE TABLE IF NOT EXISTS categories (\n                    id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    name TEXT UNIQUE NOT NULL\n                )\n            ')
        default_categories = [
            '默认',
            '工作',
            '个人',
            '会议',
            '提醒']
        for category in default_categories:
            conn.execute('INSERT OR IGNORE INTO categories (name) VALUES (?)', (category,))
        conn.commit()
        self._upgrade_database(conn)
        logger.info('数据库初始化完成')
        None(None, None)
        return None
        with None:
            if not None:
                pass

    _get_connection = (lambda self: pass# WARNING: Decompyle incomplete
)()
    
    def add_event(self = None, event_data = None):
        '''
        添加事件.
        '''
        
        try:
            conn = self._get_connection()
            if self._has_conflict(conn, event_data):
                
                try:
                    None(None, None)
                    return False
                    conn.execute('\n                    INSERT INTO events (\n                        id, title, start_time, end_time, description,\n                        category, reminder_minutes, reminder_time, reminder_sent,\n                        created_at, updated_at\n                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)\n                ', (event_data['id'], event_data['title'], event_data['start_time'], event_data['end_time'], event_data['description'], event_data['category'], event_data['reminder_minutes'], event_data.get('reminder_time'), event_data.get('reminder_sent', False), event_data['created_at'], event_data['updated_at']))
                    conn.commit()
                    logger.info(f'''添加事件成功: {event_data['title']}''')
                    
                    try:
                        None(None, None)
                        return True
                        with None:
                            if not None:
                                pass
                        
                        try:
                            return None
                            
                            try:
                                pass
                            except Exception:
                                e = None
                                logger.error(f'''添加事件失败: {e}''')
                                e = None
                                del e
                                return False
                                e = None
                                del e






    
    def get_events(self = None, start_date = None, end_date = None, category = (None, None, None)):
        '''
        获取事件列表.
        '''
        
        try:
            conn = self._get_connection()
            query = 'SELECT * FROM events WHERE 1=1'
            params = []
            if start_date:
                query += ' AND start_time >= ?'
                params.append(start_date)
            if end_date:
                query += ' AND start_time <= ?'
                params.append(end_date)
            if category:
                query += ' AND category = ?'
                params.append(category)
            query += ' ORDER BY start_time'
            cursor = conn.execute(query, params)
            rows = cursor.fetchall()
            events = []
            for row in rows:
                events.append(dict(row))
            
            try:
                None(None, None)
                return 
                with None:
                    if not None, events:
                        pass
                
                try:
                    return None
                    
                    try:
                        pass
                    except Exception:
                        logger.error(f'''获取事件失败: {e}''')
                        del e
                        return None
                        None = 
                        del e





    
    def update_event(self = None, event_id = None, **kwargs):
        '''
        更新事件.
        '''
        
        try:
            conn = self._get_connection()
            set_clauses = []
            params = []
            for key, value in kwargs.items():
                if not key in ('title', 'start_time', 'end_time', 'description', 'category', 'reminder_minutes'):
                    continue
                set_clauses.append(f'''{key} = ?''')
                params.append(value)
            if not set_clauses:
                
                try:
                    None(None, None)
                    return False
                    set_clauses.append('updated_at = ?')
                    params.append(datetime.now().isoformat())
                    params.append(event_id)
                    query = f'''UPDATE events SET {', '.join(set_clauses)} WHERE id = ?'''
                    cursor = conn.execute(query, params)
                    conn.commit()
                    if cursor.rowcount > 0:
                        logger.info(f'''更新事件成功: {event_id}''')
                        
                        try:
                            None(None, None)
                            return True
                            logger.warning(f'''事件不存在: {event_id}''')
                            
                            try:
                                None(None, None)
                                return False
                                with None:
                                    if not None:
                                        pass
                                
                                try:
                                    return None
                                    
                                    try:
                                        pass
                                    except Exception:
                                        e = None
                                        logger.error(f'''更新事件失败: {e}''')
                                        e = None
                                        del e
                                        return False
                                        e = None
                                        del e







    
    def delete_event(self = None, event_id = None):
        '''
        删除事件.
        '''
        
        try:
            conn = self._get_connection()
            cursor = conn.execute('DELETE FROM events WHERE id = ?', (event_id,))
            conn.commit()
            if cursor.rowcount > 0:
                logger.info(f'''删除事件成功: {event_id}''')
                
                try:
                    None(None, None)
                    return True
                    logger.warning(f'''事件不存在: {event_id}''')
                    
                    try:
                        None(None, None)
                        return False
                        with None:
                            if not None:
                                pass
                        
                        try:
                            return None
                            
                            try:
                                pass
                            except Exception:
                                e = None
                                logger.error(f'''删除事件失败: {e}''')
                                e = None
                                del e
                                return False
                                e = None
                                del e






    
    def delete_events_batch(self = None, start_date = None, end_date = None, category = (None, None, None, False), delete_all = ('start_date', str, 'end_date', str, 'category', str, 'delete_all', bool, 'return', Dict[(str, Any)])):
        '''批量删除事件.

        Args:
            start_date: 开始日期，ISO格式
            end_date: 结束日期，ISO格式
            category: 分类筛选
            delete_all: 是否删除所有事件

        Returns:
            包含删除结果的字典
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def get_event_by_id(self = None, event_id = None):
        '''
        根据ID获取事件.
        '''
        
        try:
            conn = self._get_connection()
            cursor = conn.execute('SELECT * FROM events WHERE id = ?', (event_id,))
            row = cursor.fetchone()
            if row:
                
                try:
                    None(None, None)
                    return 
                    
                    try:
                        None(None, None)
                        return None
                        with None:
                            if not None, dict(row):
                                pass
                        
                        try:
                            return None
                            
                            try:
                                pass
                            except Exception:
                                logger.error(f'''获取事件失败: {e}''')
                                None = None
                                del e
                                return None
                                e = None
                                del e






    
    def get_categories(self = None):
        '''
        获取所有分类.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def add_category(self = None, category_name = None):
        '''
        添加新分类.
        '''
        
        try:
            conn = self._get_connection()
            conn.execute('INSERT OR IGNORE INTO categories (name) VALUES (?)', (category_name,))
            conn.commit()
            logger.info(f'''添加分类成功: {category_name}''')
            
            try:
                None(None, None)
                return True
                with None:
                    if not None:
                        pass
                
                try:
                    return None
                    
                    try:
                        pass
                    except Exception:
                        e = None
                        logger.error(f'''添加分类失败: {e}''')
                        e = None
                        del e
                        return False
                        e = None
                        del e





    
    def delete_category(self = None, category_name = None):
        '''
        删除分类（如果没有事件使用）
        '''
        
        try:
            conn = self._get_connection()
            cursor = conn.execute('SELECT COUNT(*) FROM events WHERE category = ?', (category_name,))
            count = cursor.fetchone()[0]
            if count > 0:
                logger.warning(f'''分类 \'{category_name}\' 正在使用中，无法删除''')
                
                try:
                    None(None, None)
                    return False
                    cursor = conn.execute('DELETE FROM categories WHERE name = ?', (category_name,))
                    conn.commit()
                    if cursor.rowcount > 0:
                        logger.info(f'''删除分类成功: {category_name}''')
                        
                        try:
                            None(None, None)
                            return True
                            logger.warning(f'''分类不存在: {category_name}''')
                            
                            try:
                                None(None, None)
                                return False
                                with None:
                                    if not None:
                                        pass
                                
                                try:
                                    return None
                                    
                                    try:
                                        pass
                                    except Exception:
                                        e = None
                                        logger.error(f'''删除分类失败: {e}''')
                                        e = None
                                        del e
                                        return False
                                        e = None
                                        del e







    
    def _has_conflict(self = None, conn = None, event_data = None):
        '''
        检查时间冲突.
        '''
        cursor = conn.execute('\n            SELECT title FROM events\n            WHERE id != ? AND (\n                (start_time < ? AND end_time > ?) OR\n                (start_time < ? AND end_time > ?)\n            )\n        ', (event_data['id'], event_data['end_time'], event_data['start_time'], event_data['start_time'], event_data['end_time']))
        conflicting_events = cursor.fetchall()
        if conflicting_events:
            for event in conflicting_events:
                logger.warning(f'''时间冲突: 与事件 \'{event[0]}\' 冲突''')
            return True
        return False

    
    def get_statistics(self = None):
        '''
        获取统计信息.
        '''
        
        try:
            conn = self._get_connection()
            cursor = conn.execute('SELECT COUNT(*) FROM events')
            total_events = cursor.fetchone()[0]
            cursor = conn.execute('\n                    SELECT category, COUNT(*)\n                    FROM events\n                    GROUP BY category\n                    ORDER BY COUNT(*) DESC\n                ')
            category_stats = dict(cursor.fetchall())
            today = datetime.now().strftime('%Y-%m-%d')
            cursor = conn.execute('\n                    SELECT COUNT(*) FROM events\n                    WHERE date(start_time) = ?\n                ', (today,))
            today_events = cursor.fetchone()[0]
            
            try:
                None(None, None)
                return 
                with None:
                    if not None, {
                        'total_events': total_events,
                        'category_stats': category_stats,
                        'today_events': today_events }:
                        pass
                
                try:
                    return None
                    
                    try:
                        pass
                    except Exception:
                        logger.error(f'''获取统计信息失败: {e}''')
                        del e
                        return None
                        None = 
                        del e





    
    def migrate_from_json(self = None, json_file_path = None):
        '''
        从JSON文件迁移数据.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _upgrade_database(self = None, conn = None):
        '''
        升级数据库结构.
        '''
        pass
    # WARNING: Decompyle incomplete


_calendar_db = None

def get_calendar_database():
    '''
    获取数据库实例单例.
    '''
    pass
# WARNING: Decompyle incomplete

