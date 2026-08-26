# Source Generated with Decompyle++
# File: engine.pyc (Python 3.12)

'''
八字计算核心引擎.
'''
from datetime import datetime
from typing import Any, Dict, List, Optional
import pendulum
from lunar_python import Lunar, Solar
from models import ChineseCalendar, EarthBranch, EightChar, HeavenStem, LunarTime, SixtyCycle, SolarTime
from professional_data import GAN, GAN_WUXING, GAN_YINYANG, SHENG_XIAO, ZHI, ZHI_CANG_GAN, ZHI_WUXING, ZHI_YINYANG

class BaziEngine:
    '''
    八字计算引擎.
    '''
    HEAVEN_STEMS = { }
    for gan in GAN:
        HEAVEN_STEMS[gan] = HeavenStem(name = gan, element = GAN_WUXING[gan], yin_yang = GAN_YINYANG[gan])
    EARTH_BRANCHES = { }
    for i, zhi in enumerate(ZHI):
        cang_gan = ZHI_CANG_GAN.get(zhi, { })
        cang_gan_list = list(cang_gan.keys())
        EARTH_BRANCHES[zhi] = EarthBranch(name = zhi, element = ZHI_WUXING[zhi], yin_yang = ZHI_YINYANG[zhi], zodiac = SHENG_XIAO[i], hide_heaven_main = cang_gan_list[0] if len(cang_gan_list) > 0 else None, hide_heaven_middle = cang_gan_list[1] if len(cang_gan_list) > 1 else None, hide_heaven_residual = cang_gan_list[2] if len(cang_gan_list) > 2 else None)
    
    def __init__(self):
        '''
        初始化.
        '''
        pass

    
    def parse_solar_time(self = None, iso_date = None):
        '''
        解析公历时间字符串（支持多种格式）- 使用pendulum优化，增强时区处理.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def solar_to_lunar(self = None, solar_time = None):
        '''
        公历转农历 - 增强闰月处理.
        '''
        
        try:
            solar = Solar.fromYmdHms(solar_time.year, solar_time.month, solar_time.day, solar_time.hour, solar_time.minute, solar_time.second)
            lunar = solar.getLunar()
            is_leap = lunar.isLeap() if hasattr(lunar, 'isLeap') else False
            if not hasattr(lunar, 'isLeap'):
                month_str = lunar.getMonthInChinese()
                is_leap = '闰' in month_str
            return LunarTime(year = lunar.getYear(), month = lunar.getMonth(), day = lunar.getDay(), hour = lunar.getHour(), minute = lunar.getMinute(), second = lunar.getSecond(), is_leap = is_leap)
        except Exception:
            e = None
            raise ValueError(f'''公历转农历失败: {e}''')
            e = None
            del e


    
    def lunar_to_solar(self = None, lunar_time = None):
        '''
        农历转公历 - 增强闰月处理.
        '''
        
        try:
            if lunar_time.is_leap:
                lunar = Lunar.fromYmdHms(lunar_time.year, -(lunar_time.month), lunar_time.day, lunar_time.hour, lunar_time.minute, lunar_time.second)
            else:
                lunar = Lunar.fromYmdHms(lunar_time.year, lunar_time.month, lunar_time.day, lunar_time.hour, lunar_time.minute, lunar_time.second)
            solar = lunar.getSolar()
            return SolarTime(year = solar.getYear(), month = solar.getMonth(), day = solar.getDay(), hour = solar.getHour(), minute = solar.getMinute(), second = solar.getSecond())
        except Exception:
            e = None
            raise ValueError(f'''农历转公历失败: {e}''')
            e = None
            del e


    
    def build_eight_char(self = None, solar_time = None):
        '''
        构建八字.
        '''
        
        try:
            solar = Solar.fromYmdHms(solar_time.year, solar_time.month, solar_time.day, solar_time.hour, solar_time.minute, solar_time.second)
            lunar = solar.getLunar()
            bazi = lunar.getEightChar()
            year_gan = bazi.getYearGan()
            year_zhi = bazi.getYearZhi()
            year_cycle = self._create_sixty_cycle(year_gan, year_zhi)
            month_gan = bazi.getMonthGan()
            month_zhi = bazi.getMonthZhi()
            month_cycle = self._create_sixty_cycle(month_gan, month_zhi)
            day_gan = bazi.getDayGan()
            day_zhi = bazi.getDayZhi()
            day_cycle = self._create_sixty_cycle(day_gan, day_zhi)
            time_gan = bazi.getTimeGan()
            time_zhi = bazi.getTimeZhi()
            time_cycle = self._create_sixty_cycle(time_gan, time_zhi)
            return EightChar(year = year_cycle, month = month_cycle, day = day_cycle, hour = time_cycle)
        except Exception:
            e = None
            raise ValueError(f'''构建八字失败: {e}''')
            e = None
            del e


    
    def _create_sixty_cycle(self = None, gan_name = None, zhi_name = None):
        '''
        创建六十甲子对象.
        '''
        heaven_stem = self.HEAVEN_STEMS[gan_name]
        earth_branch = self.EARTH_BRANCHES[zhi_name]
        
        try:
            sound = self._get_nayin(gan_name, zhi_name)
            ten = self._get_ten(gan_name, zhi_name)
            extra_branches = self._get_kong_wang(gan_name, zhi_name)
            return SixtyCycle(heaven_stem = heaven_stem, earth_branch = earth_branch, sound = sound, ten = ten, extra_earth_branches = extra_branches)
        except Exception:
            e = None
            print(f'''纳音计算失败: {gan_name}{zhi_name} - {e}''')
            sound = '未知'
            e = None
            del e
            continue
            e = None
            del e


    
    def _get_nayin(self = None, gan = None, zhi = None):
        '''
        获取纳音.
        '''
        get_nayin = get_nayin
        import professional_data
        return get_nayin(gan, zhi)

    
    def _get_ten(self = None, gan = None, zhi = None):
        '''获取旬 - 使用六十甲子旬空算法'''
        GAN = GAN
        ZHI = ZHI
        import professional_data
        
        try:
            gan_idx = GAN.index(gan)
            zhi_idx = ZHI.index(zhi)
            jiazi_number = (gan_idx * 6 + zhi_idx * 5) % 60
            if jiazi_number == 0:
                jiazi_number = 60
            xun_starts = [
                '甲子',
                '甲戌',
                '甲申',
                '甲午',
                '甲辰',
                '甲寅']
            xun_index = (jiazi_number - 1) // 10
            if  <= 0, xun_index or 0, xun_index < len(xun_starts):
                pass
            
        return xun_starts[xun_index]
        try:
            return self._calculate_xun_by_position(jiazi_number)
        except (ValueError, IndexError):
            print(f'''旬计算失败: {gan}{zhi} - {e}''')
            None = None
            del e
            return '甲子'
            e = None
            del e


    
    def _get_kong_wang(self = None, gan = None, zhi = None):
        '''获取空亡 - 使用传统旬空算法'''
        GAN = GAN
        ZHI = ZHI
        import professional_data
        
        try:
            gan_idx = GAN.index(gan)
            zhi_idx = ZHI.index(zhi)
            jiazi_number = (gan_idx * 6 + zhi_idx * 5) % 60
            if jiazi_number == 0:
                jiazi_number = 60
            xun_index = (jiazi_number - 1) // 10
            kong_wang_table = [
                [
                    '戌',
                    '亥'],
                [
                    '申',
                    '酉'],
                [
                    '午',
                    '未'],
                [
                    '辰',
                    '巳'],
                [
                    '寅',
                    '卯'],
                [
                    '子',
                    '丑']]
            if  <= 0, xun_index or 0, xun_index < len(kong_wang_table):
                pass
            
        return kong_wang_table[xun_index]
        try:
            return self._calculate_kong_wang_by_position(jiazi_number)
        except (ValueError, IndexError):
            print(f'''空亡计算失败: {gan}{zhi} - {e}''')
            del e
            return None
            None = 
            del e


    
    def format_solar_time(self = None, solar_time = None):
        '''
        格式化公历时间.
        '''
        return f'''{solar_time.year}年{solar_time.month}月{solar_time.day}日{solar_time.hour}时{solar_time.minute}分{solar_time.second}秒'''

    
    def format_lunar_time(self = None, lunar_time = None):
        '''
        格式化农历时间.
        '''
        return f'''农历{lunar_time.year}年{lunar_time.month}月{lunar_time.day}日{lunar_time.hour}时{lunar_time.minute}分{lunar_time.second}秒'''

    
    def get_chinese_calendar(self = None, solar_time = None):
        '''获取中国传统历法信息 - 使用lunar-python'''
        pass
    # WARNING: Decompyle incomplete

    
    def _calculate_xun_by_position(self = None, jiazi_number = None):
        '''
        根据六十甲子序号计算旬.
        '''
        xun_starts = [
            '甲子',
            '甲戌',
            '甲申',
            '甲午',
            '甲辰',
            '甲寅']
        xun_index = (jiazi_number - 1) // 10
        if  <= 0, xun_index or 0, xun_index < len(xun_starts):
            return xun_starts[xun_index]
        return '甲子'
        return '甲子'

    
    def _calculate_kong_wang_by_position(self = None, jiazi_number = None):
        '''
        根据六十甲子序号计算空亡.
        '''
        kong_wang_table = [
            [
                '戌',
                '亥'],
            [
                '申',
                '酉'],
            [
                '午',
                '未'],
            [
                '辰',
                '巳'],
            [
                '寅',
                '卯'],
            [
                '子',
                '丑']]
        xun_index = (jiazi_number - 1) // 10
        if  <= 0, xun_index or 0, xun_index < len(kong_wang_table):
            return kong_wang_table[xun_index]
        return [
            '戌',
            '亥']
        return [
            '戌',
            '亥']

    
    def get_detailed_lunar_info(self = None, solar_time = None):
        '''
        获取详细的农历信息.
        '''
        
        try:
            solar = Solar.fromYmdHms(solar_time.year, solar_time.month, solar_time.day, solar_time.hour, solar_time.minute, solar_time.second)
            lunar = solar.getLunar()
            current_jieqi = lunar.getJieQi()
            next_jieqi = lunar.getNextJieQi()
            prev_jieqi = lunar.getPrevJieQi()
            return {
                'current_jieqi': current_jieqi,
                'next_jieqi': next_jieqi.toString() if next_jieqi else None,
                'prev_jieqi': prev_jieqi.toString() if prev_jieqi else None,
                'lunar_festivals': lunar.getFestivals(),
                'solar_festivals': solar.getFestivals(),
                'twenty_eight_star': lunar.getXiu(),
                'day_position': {
                    'xi': lunar.getPositionXi(),
                    'yang_gui': lunar.getPositionYangGui(),
                    'yin_gui': lunar.getPositionYinGui(),
                    'fu': lunar.getPositionFu(),
                    'cai': lunar.getPositionCai() },
                'pengzu_taboo': {
                    'gan': lunar.getPengZuGan(),
                    'zhi': lunar.getPengZuZhi() },
                'day_suitable': lunar.getDayYi(),
                'day_avoid': lunar.getDayJi(),
                'day_clash': lunar.getDayChongDesc() }
        except Exception:
            e = None
            print(f'''获取详细农历信息失败: {e}''')
            del e
            return None
            None = 
            del e



_bazi_engine = None

def get_bazi_engine():
    '''
    获取八字引擎单例.
    '''
    pass
# WARNING: Decompyle incomplete

