# Source Generated with Decompyle++
# File: cli_activation.pyc (Python 3.12)

'''
Quy trình kích hoạt thiết bị chế độ CLI - Cung cấp cùng chức năng với cửa sổ GUI, nhưng sử dụng đầu ra thuần terminal.
'''
from datetime import datetime
from typing import Optional
from src.core.system_initializer import SystemInitializer
from src.utils.device_activator import DeviceActivator
from src.utils.logging_config import get_logger
logger = get_logger(__name__)

class CLIActivation:
    '''
    Bộ xử lý kích hoạt thiết bị chế độ CLI.
    '''
    
    def __init__(self = None, system_initializer = None):
        self.system_initializer = system_initializer
        self.device_activator = None
        self.current_stage = None
        self.activation_data = None
        self.is_activated = False
        self.logger = logger

    
    async def run_activation_process(self = None):
        '''Chạy toàn bộ quy trình kích hoạt CLI.

        Returns:
            bool: Kết quả kích hoạt có thành công không
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _print_header(self):
        '''
        In thông tin tiêu đề quy trình kích hoạt CLI.
        '''
        print('\n============================================================')
        print('Xiaozhi AI Client - Quy trình kích hoạt thiết bị')
        print('============================================================')
        print('Đang khởi tạo thiết bị, vui lòng chờ...')
        print()

    
    def _update_device_info(self):
        '''
        Cập nhật hiển thị thông tin thiết bị.
        '''
        if not self.system_initializer or self.system_initializer.device_fingerprint:
            return None
        device_fp = self.system_initializer.device_fingerprint
        serial_number = device_fp.get_serial_number()
        mac_address = device_fp.get_mac_address_from_efuse()
        activation_status = self.system_initializer.get_activation_status()
        local_activated = activation_status.get('local_activated', False)
        server_activated = activation_status.get('server_activated', False)
        status_consistent = activation_status.get('status_consistent', True)
        self.is_activated = local_activated
        print('📱 Thông tin thiết bị:')
        print(f'''   Số serial: {serial_number if serial_number else '--'}''')
        print(f'''   Địa chỉ MAC: {mac_address if mac_address else '--'}''')
        if not status_consistent:
            if not local_activated and server_activated:
                status_text = 'Trạng thái không nhất quán (cần kích hoạt lại)'
            else:
                status_text = 'Trạng thái không nhất quán (đã tự sửa)'
        elif local_activated:
            pass
        
        status_text = 'Chưa kích hoạt'
        print(f'''   Trạng thái kích hoạt: {status_text}''')

    
    async def _start_activation_process(self = None):
        '''
        Bắt đầu quy trình kích hoạt.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _show_activation_info(self = None, activation_data = None):
        '''
        Hiển thị thông tin kích hoạt.
        '''
        code = activation_data.get('code', '------')
        message = activation_data.get('message', 'Vui lòng truy cập xiaozhi.me để nhập mã kích hoạt')
        print('\n============================================================')
        print('Thông tin kích hoạt thiết bị')
        print('============================================================')
        print(f'''Mã kích hoạt: {code}''')
        print(f'''Hướng dẫn: {message}''')
        print('============================================================')
        formatted_code = ' '.join(code)
        print(f'''\nMã kích hoạt (vui lòng nhập trên website): {formatted_code}''')
        print('\nVui lòng làm theo các bước sau để hoàn tất kích hoạt:')
        print('1. Mở trình duyệt và truy cập xiaozhi.me')
        print('2. Đăng nhập vào tài khoản của bạn')
        print("3. Chọn 'Thêm thiết bị'")
        print(f'''4. Nhập mã kích hoạt: {formatted_code}''')
        print('5. Xác nhận thêm thiết bị')
        print('\nĐang chờ xác nhận kích hoạt, vui lòng hoàn tất thao tác trên website...')
        self._log_and_print(f'''Mã kích hoạt: {code}''')
        self._log_and_print(f'''Hướng dẫn kích hoạt: {message}''')

    
    def _print_activation_success(self):
        '''
        In thông tin kích hoạt thành công.
        '''
        print('\n============================================================')
        print('Kích hoạt thiết bị thành công!')
        print('============================================================')
        print('Thiết bị đã được thêm thành công vào tài khoản của bạn')
        print('Cấu hình đã được cập nhật tự động')
        print('Chuẩn bị khởi động Xiaozhi AI Client...')
        print('============================================================')

    
    def _print_activation_failure(self):
        '''
        In thông tin kích hoạt thất bại.
        '''
        print('\n============================================================')
        print('Kích hoạt thiết bị thất bại')
        print('============================================================')
        print('Nguyên nhân có thể:')
        print('• Kết nối mạng không ổn định')
        print('• Mã kích hoạt nhập sai hoặc đã hết hạn')
        print('• Máy chủ tạm thời không khả dụng')
        print('\nGiải pháp:')
        print('• Kiểm tra kết nối mạng')
        print('• Chạy lại chương trình để nhận mã kích hoạt mới')
        print('• Nhập đúng mã kích hoạt trên website')
        print('============================================================')

    
    def _log_and_print(self = None, message = None):
        '''
        Đồng thời ghi log và in ra terminal.
        '''
        timestamp = datetime.now().strftime('%H:%M:%S')
        log_message = f'''[{timestamp}] {message}'''
        print(log_message)
        self.logger.info(message)

    
    def get_activation_result(self = None):
        '''
        Lấy kết quả kích hoạt.
        '''
        device_fingerprint = None
        config_manager = None
        if self.system_initializer:
            device_fingerprint = self.system_initializer.device_fingerprint
            config_manager = self.system_initializer.config_manager
        return {
            'is_activated': self.is_activated,
            'device_fingerprint': device_fingerprint,
            'config_manager': config_manager }


