#!/usr/bin/env python3
"""
日程查询脚本 用于查看和管理日程安排.
"""
import argparse
import asyncio
import sys
from datetime import datetime, timedelta
from pathlib import Path

from src.mcp.tools.calendar import get_calendar_manager
from src.utils.logging_config import get_logger

# Thêm thư mục gốc dự án vào Python path - phải trước khi import module src
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))

logger = get_logger(__name__)


class CalendarQueryScript:
    """
    Lớp script truy vấn lịch.
    """

    def __init__(self):
        self.manager = get_calendar_manager()

    def format_event_display(self, event, show_details=True):
        """
        Định dạng hiển thị sự kiện.
        """
        start_dt = datetime.fromisoformat(event.start_time)
        end_dt = datetime.fromisoformat(event.end_time)

        # Thông tin cơ bản
        time_str = f"{start_dt.strftime('%m/%d %H:%M')} - {end_dt.strftime('%H:%M')}"
        basic_info = f"📅 {time_str} | 【{event.category}】{event.title}"

        if not show_details:
            return basic_info

        # Thông tin chi tiết
        details = []
        if event.description:
            details.append(f"   📝 Ghi chú: {event.description}")

        # Thông tin nhắc nhở
        if event.reminder_minutes > 0:
            details.append(f"   ⏰ Nhắc: Trước {event.reminder_minutes} phút")
            if hasattr(event, "reminder_sent") and event.reminder_sent:
                details.append("   ✅ Trạng thái nhắc: Đã gửi")
            else:
                details.append("   ⏳ Trạng thái nhắc: Chưa gửi")

        # Khoảng cách thời gian
        now = datetime.now()
        time_diff = start_dt - now
        if time_diff.total_seconds() > 0:
            days = time_diff.days
            hours = int(time_diff.seconds // 3600)
            minutes = int((time_diff.seconds % 3600) // 60)

            time_until_parts = []
            if days > 0:
                time_until_parts.append(f"{days} ngày")
            if hours > 0:
                time_until_parts.append(f"{hours} giờ")
            if minutes > 0:
                time_until_parts.append(f"{minutes} phút")

            if time_until_parts:
                details.append(f"   🕐 Còn đến bắt đầu: {' '.join(time_until_parts)}")
            else:
                details.append("   🕐 Còn đến bắt đầu: Sắp bắt đầu")
        elif start_dt <= now <= end_dt:
            details.append("   🔴 Trạng thái: Đang diễn ra")
        else:
            details.append("   ✅ Trạng thái: Đã kết thúc")

        if details:
            return basic_info + "\n" + "\n".join(details)
        return basic_info

    async def query_today(self):
        """
        Truy vấn lịch ngày hôm nay.
        """
        print("📅 Lịch hôm nay")
        print("=" * 50)

        now = datetime.now()
        today_start = now.replace(hour=0, minute=0, second=0, microsecond=0)
        today_end = today_start + timedelta(days=1)

        events = self.manager.get_events(
            start_date=today_start.isoformat(), end_date=today_end.isoformat()
        )

        if not events:
            print("🎉 Hôm nay không có lịch nào")
            return

        print(f"📊 Tổng cộng {len(events)} sự kiện:\n")
        for i, event in enumerate(events, 1):
            print(f"{i}. {self.format_event_display(event)}")
            if i < len(events):
                print()

    async def query_tomorrow(self):
        """
        Truy vấn lịch ngày mai.
        """
        print("📅 Lịch ngày mai")
        print("=" * 50)

        now = datetime.now()
        tomorrow_start = (now + timedelta(days=1)).replace(
            hour=0, minute=0, second=0, microsecond=0
        )
        tomorrow_end = tomorrow_start + timedelta(days=1)

        events = self.manager.get_events(
            start_date=tomorrow_start.isoformat(), end_date=tomorrow_end.isoformat()
        )

        if not events:
            print("🎉 Ngày mai không có lịch nào")
            return

        print(f"📊 Tổng cộng {len(events)} sự kiện:\n")
        for i, event in enumerate(events, 1):
            print(f"{i}. {self.format_event_display(event)}")
            if i < len(events):
                print()

    async def query_week(self):
        """
        Truy vấn lịch tuần này.
        """
        print("📅 Lịch tuần này")
        print("=" * 50)

        now = datetime.now()
        # Thứ Hai tuần này
        days_since_monday = now.weekday()
        week_start = (now - timedelta(days=days_since_monday)).replace(
            hour=0, minute=0, second=0, microsecond=0
        )
        week_end = week_start + timedelta(days=7)

        events = self.manager.get_events(
            start_date=week_start.isoformat(), end_date=week_end.isoformat()
        )

        if not events:
            print("🎉 Tuần này không có lịch nào")
            return

        print(f"📊 Tổng cộng {len(events)} sự kiện:\n")

        # Hiển thị theo nhóm ngày
        events_by_date = {}
        for event in events:
            event_date = datetime.fromisoformat(event.start_time).date()
            if event_date not in events_by_date:
                events_by_date[event_date] = []
            events_by_date[event_date].append(event)

        for date in sorted(events_by_date.keys()):
            weekday = ["Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy", "Chủ Nhật"][
                date.weekday()
            ]
            print(f"📆 {date.strftime('%m/%d')} ({weekday})")
            print("-" * 30)

            for event in events_by_date[date]:
                print(f"  {self.format_event_display(event, show_details=False)}")
            print()

    async def query_upcoming(self, hours=24):
        """
        Truy vấn các sự kiện sắp tới trong N giờ.
        """
        print(f"📅 Lịch trong {hours} giờ tới")
        print("=" * 50)

        now = datetime.now()
        end_time = now + timedelta(hours=hours)

        events = self.manager.get_events(
            start_date=now.isoformat(), end_date=end_time.isoformat()
        )

        if not events:
            print(f"🎉 Trong {hours} giờ tới không có sự kiện nào")
            return

        print(f"📊 Tổng cộng {len(events)} sự kiện:\n")
        for i, event in enumerate(events, 1):
            print(f"{i}. {self.format_event_display(event)}")
            if i < len(events):
                print()
    async def query_by_category(self, category=None):
        """
        Truy vấn lịch theo danh mục.
        """
        if category:
            print(f"📅 Lịch thuộc danh mục 【{category}】")
            print("=" * 50)

            events = self.manager.get_events(category=category)

            if not events:
                print(f"🎉 Không có lịch nào trong danh mục 【{category}】")
                return

            print(f"📊 Tổng cộng {len(events)} sự kiện:\n")
            for i, event in enumerate(events, 1):
                print(f"{i}. {self.format_event_display(event)}")
                if i < len(events):
                    print()
        else:
            print("📅 Thống kê tất cả danh mục")
            print("=" * 50)

            categories = self.manager.get_categories()

            if not categories:
                print("🎉 Chưa có danh mục nào")
                return

            print("📊 Danh sách danh mục:")
            for i, cat in enumerate(categories, 1):
                # Thống kê số lượng sự kiện trong từng danh mục
                events = self.manager.get_events(category=cat)
                print(f"{i}. 【{cat}】- {len(events)} sự kiện")

    async def query_all(self):
        """
        Truy vấn tất cả các sự kiện.
        """
        print("📅 Tất cả lịch")
        print("=" * 50)

        events = self.manager.get_events()

        if not events:
            print("🎉 Chưa có sự kiện nào")
            return

        print(f"📊 Tổng cộng {len(events)} sự kiện:\n")

        # Sắp xếp theo thời gian và phân nhóm
        now = datetime.now()
        past_events = []
        current_events = []
        future_events = []

        for event in events:
            start_dt = datetime.fromisoformat(event.start_time)
            end_dt = datetime.fromisoformat(event.end_time)

            if end_dt < now:
                past_events.append(event)
            elif start_dt <= now <= end_dt:
                current_events.append(event)
            else:
                future_events.append(event)

        # Hiển thị các sự kiện đang diễn ra
        if current_events:
            print("🔴 Đang diễn ra:")
            for event in current_events:
                print(f"  {self.format_event_display(event, show_details=False)}")
            print()

        # Hiển thị các sự kiện sắp tới
        if future_events:
            print("⏳ Sắp tới:")
            for event in future_events[:5]:  # chỉ hiển thị 5 sự kiện đầu
                print(f"  {self.format_event_display(event, show_details=False)}")
            if len(future_events) > 5:
                print(f"  ... còn {len(future_events) - 5} sự kiện nữa")
            print()

        # Hiển thị các sự kiện đã kết thúc gần đây
        if past_events:
            recent_past = sorted(past_events, key=lambda e: e.start_time, reverse=True)[:3]
            print("✅ Mới hoàn thành:")
            for event in recent_past:
                print(f"  {self.format_event_display(event, show_details=False)}")
            if len(past_events) > 3:
                print(f"  ... còn {len(past_events) - 3} sự kiện đã hoàn thành")

    async def search_events(self, keyword):
        """
        Tìm kiếm sự kiện.
        """
        print(f"🔍 Tìm kiếm sự kiện chứa '{keyword}'")
        print("=" * 50)

        all_events = self.manager.get_events()
        matched_events = []

        for event in all_events:
            if (
                keyword.lower() in event.title.lower()
                or keyword.lower() in event.description.lower()
                or keyword.lower() in event.category.lower()
            ):
                matched_events.append(event)

        if not matched_events:
            print(f"🎉 Không tìm thấy sự kiện nào chứa '{keyword}'")
            return

        print(f"📊 Tìm thấy {len(matched_events)} sự kiện khớp:\n")
        for i, event in enumerate(matched_events, 1):
            print(f"{i}. {self.format_event_display(event)}")
            if i < len(matched_events):
                print()


async def main():
    """
    Hàm chính.
    """
    parser = argparse.ArgumentParser(description="Script truy vấn lịch")
    parser.add_argument(
        "command",
        nargs="?",
        default="today",
        choices=["today", "tomorrow", "week", "upcoming", "category", "all", "search"],
        help="Loại truy vấn",
    )
    parser.add_argument("--hours", type=int, default=24, help="Số giờ cho truy vấn upcoming")
    parser.add_argument("--category", type=str, help="Tên danh mục cụ thể")
    parser.add_argument("--keyword", type=str, help="Từ khóa tìm kiếm")

    args = parser.parse_args()

    script = CalendarQueryScript()

    try:
        if args.command == "today":
            await script.query_today()
        elif args.command == "tomorrow":
            await script.query_tomorrow()
        elif args.command == "week":
            await script.query_week()
        elif args.command == "upcoming":
            await script.query_upcoming(args.hours)
        elif args.command == "category":
            await script.query_by_category(args.category)
        elif args.command == "all":
            await script.query_all()
        elif args.command == "search":
            if not args.keyword:
                print("❌ Tìm kiếm cần cung cấp từ khóa, sử dụng --keyword")
                return
            await script.search_events(args.keyword)

        print("\n" + "=" * 50)
        print("💡 Hướng dẫn sử dụng:")
        print("  python scripts/calendar_query.py today      # Xem lịch hôm nay")
        print("  python scripts/calendar_query.py tomorrow   # Xem lịch ngày mai")
        print("  python scripts/calendar_query.py week       # Xem lịch tuần này")
        print(
            "  python scripts/calendar_query.py upcoming --hours 48  # Xem lịch 48 giờ tới"
        )
        print(
            "  python scripts/calendar_query.py category --category Công việc  # Xem theo danh mục Công việc"
        )
        print("  python scripts/calendar_query.py all        # Xem tất cả lịch")
        print("  python scripts/calendar_query.py search --keyword Phát triển  # Tìm kiếm sự kiện")

    except Exception as e:
        logger.error(f"Truy vấn lịch thất bại: {e}", exc_info=True)
        print(f"❌ Truy vấn thất bại: {e}")


if __name__ == "__main__":
    asyncio.run(main())
