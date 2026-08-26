# Source Generated with Decompyle++
# File: mcp_server.pyc (Python 3.12)

'''
Triển khai máy chủ MCP cho Python
Tham khảo: https://modelcontextprotocol.io/specification/2024-11-05
'''
import asyncio
import json
from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Callable, Dict, List, Optional, Tuple, Union
from src.constants.system import SystemConstants
from src.utils.logging_config import get_logger
logger = get_logger(__name__)
ReturnValue = Union[(bool, int, str)]

class PropertyType(Enum):
    '''
    Enum loại thuộc tính.
    '''
    BOOLEAN = 'boolean'
    INTEGER = 'integer'
    STRING = 'string'

Property = <NODE:12>()
PropertyList = <NODE:12>()
McpTool = <NODE:12>()

class McpServer:
    '''
    Triển khai máy chủ MCP.
    '''
    _instance = None
    get_instance = (lambda cls: pass# WARNING: Decompyle incomplete
)()
    
    def __init__(self):
        self.tools = []
        self._send_callback = None
        self._camera = None

    
    def set_send_callback(self = None, callback = None):
        '''
        Thiết lập hàm callback để gửi tin nhắn.
        '''
        self._send_callback = callback

    
    def add_tool(self = None, tool = None):
        '''
        Thêm công cụ mới.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def add_common_tools(self):
        '''
        Thêm các công cụ phổ biến.
        '''
        original_tools = self.tools.copy()
        self.tools.clear()
        get_system_tools_manager = get_system_tools_manager
        import src.mcp.tools.system
        system_manager = get_system_tools_manager()
        system_manager.init_tools(self.add_tool, PropertyList, Property, PropertyType)
        get_calendar_manager = get_calendar_manager
        import src.mcp.tools.calendar
        calendar_manager = get_calendar_manager()
        calendar_manager.init_tools(self.add_tool, PropertyList, Property, PropertyType)
        get_timer_manager = get_timer_manager
        import src.mcp.tools.timer
        timer_manager = get_timer_manager()
        timer_manager.init_tools(self.add_tool, PropertyList, Property, PropertyType)
        get_music_tools_manager = get_music_tools_manager
        import src.mcp.tools.music
        music_manager = get_music_tools_manager()
        music_manager.init_tools(self.add_tool, PropertyList, Property, PropertyType)
        take_photo = take_photo
        import src.mcp.tools.camera
        properties = PropertyList([
            Property('question', PropertyType.STRING)])
        VISION_DESC = "【Hình ảnh/Nhận dạng/OCR/Hỏi đáp】Khi người dùng nhắc đến: chụp ảnh, nhận dạng hình ảnh, đọc/trích xuất văn bản, OCR, dịch văn bản trong ảnh, xem hình/screenshot, đây là gì, đếm số, nhận diện mã QR/vạch, so sánh hai hình, phân tích ảnh chụp lỗi, trích xuất thông tin bảng/biên lai, hoặc hỏi đáp về hình ảnh — thì gọi công cụ này. Chức năng: ①Chụp ảnh hoặc nhận ảnh/screenshot/URL; ②Nhận dạng vật thể/cảnh vật/nhãn; ③OCR (đa ngôn ngữ) và dịch; ④Đếm/định vị; ⑤Đọc mã QR/vạch; ⑥Trích xuất thông tin chính (bảng/biên lai); ⑦So sánh hai ảnh; ⑧Trả lời câu hỏi dựa trên ảnh. Đầu vào đề xuất: { mode:'capture'|'upload'|'url', image?, url?, question?, target_lang? }; Nếu người dùng chưa cung cấp ảnh và được phép, có thể kích hoạt chụp ảnh (mode='capture'). Không dùng cho hỏi đáp thuần văn bản hoặc yêu cầu không liên quan đến hình ảnh. English: Vision/OCR/QA tool — use when user provides or asks about a photo/screenshot/image. Describe, classify, OCR, translate, count objects, read QR/barcodes, extract tables/receipts, compare images, or image QA. Not for pure text queries."
        self.add_tool(McpTool('take_photo', VISION_DESC, properties, take_photo))
        take_screenshot = take_screenshot
        import src.mcp.tools.screenshot
        screenshot_properties = PropertyList([
            Property('question', PropertyType.STRING),
            Property('display', PropertyType.STRING, default_value = None)])
        SCREENSHOT_DESC = "【Ảnh chụp màn hình/Phân tích màn hình】Khi người dùng nhắc đến: chụp màn hình, xem desktop, phân tích màn hình, có gì trên màn hình, OCR màn hình... thì gọi công cụ này. Chức năng: ①Chụp toàn bộ desktop; ②Nhận dạng nội dung màn hình; ③OCR trích xuất văn bản; ④Phân tích giao diện; ⑤Nhận diện ứng dụng; ⑥Phân tích lỗi qua ảnh chụp; ⑦Kiểm tra trạng thái desktop; ⑧Chụp nhiều màn hình. Tham số: { question: 'câu hỏi về màn hình/desktop', display: 'chọn màn hình (tùy chọn)' }; Giá trị display có thể là: 'main'/'主屏'/'笔记本'(màn hình chính), 'secondary'/'副屏'/'外屏'(màn hình phụ), hoặc để trống (tất cả màn hình). Lưu ý: công cụ này sẽ chụp màn hình, hãy đảm bảo người dùng đồng ý. English: Desktop screenshot/screen analysis tool. Used when user mentions screenshot, screen capture, desktop analysis, etc. Captures desktop, performs OCR and UI analysis."
        self.add_tool(McpTool('take_screenshot', SCREENSHOT_DESC, screenshot_properties, take_screenshot))
        get_bazi_manager = get_bazi_manager
        import src.mcp.tools.bazi
        bazi_manager = get_bazi_manager()
        bazi_manager.init_tools(self.add_tool, PropertyList, Property, PropertyType)
        self.tools.extend(original_tools)

    
    async def parse_message(self = None, message = None):
        '''
        Phân tích tin nhắn MCP.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _handle_initialize(self = None, id = None, params = None):
        '''
        Xử lý yêu cầu khởi tạo.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _handle_tools_list(self = None, id = None, params = None):
        '''
        Xử lý yêu cầu danh sách công cụ.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _handle_tool_call(self = None, id = None, params = None):
        '''
        Xử lý yêu cầu gọi công cụ.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _parse_capabilities(self, capabilities):
        '''
        Phân tích capabilities.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _reply_result(self = None, id = None, result = None):
        '''
        Gửi phản hồi thành công.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def _reply_error(self = None, id = None, message = None):
        '''
        Gửi phản hồi lỗi.
        '''
        pass
    # WARNING: Decompyle incomplete


