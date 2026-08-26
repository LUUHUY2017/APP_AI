# Source Generated with Decompyle++
# File: bazi_calculator.pyc (Python 3.12)

'''
八字命理分析核心算法.
'''
from typing import Any, Dict, List, Optional
from engine import get_bazi_engine
from models import BaziAnalysis, EightChar, LunarTime, SolarTime
from professional_analyzer import get_professional_analyzer

class BaziCalculator:
    '''
    八字分析计算器.
    '''
    
    def __init__(self):
        self.engine = get_bazi_engine()
        self.professional_analyzer = get_professional_analyzer()

    
    def build_hide_heaven_object(self = None, heaven_stem = None, day_master = None):
        '''
        构建藏干对象.
        '''
        if not heaven_stem:
            return None
        return {
            '天干': heaven_stem,
            '十神': self._get_ten_star(day_master, heaven_stem) }

    
    def _get_ten_star(self = None, day_master = None, other_stem = None):
        '''
        计算十神关系.
        '''
        return self.professional_analyzer.get_ten_gods_analysis(day_master, other_stem)

    
    def build_sixty_cycle_object(self = None, sixty_cycle = None, day_master = None):
        '''
        构建干支对象.
        '''
        heaven_stem = sixty_cycle.get_heaven_stem()
        earth_branch = sixty_cycle.get_earth_branch()
        if not day_master:
            day_master = heaven_stem.name
        return {
            '天干': {
                '天干': heaven_stem.name,
                '五行': heaven_stem.element,
                '阴阳': '阳' if heaven_stem.yin_yang == 1 else '阴',
                '十神': None if day_master == heaven_stem.name else self._get_ten_star(day_master, heaven_stem.name) },
            '地支': {
                '地支': earth_branch.name,
                '五行': earth_branch.element,
                '阴阳': '阳' if earth_branch.yin_yang == 1 else '阴',
                '藏干': {
                    '主气': self.build_hide_heaven_object(earth_branch.hide_heaven_main, day_master),
                    '中气': self.build_hide_heaven_object(earth_branch.hide_heaven_middle, day_master),
                    '余气': self.build_hide_heaven_object(earth_branch.hide_heaven_residual, day_master) } },
            '纳音': sixty_cycle.sound,
            '旬': sixty_cycle.ten,
            '空亡': ''.join(sixty_cycle.extra_earth_branches),
            '星运': self._get_terrain(day_master, earth_branch.name),
            '自坐': self._get_terrain(heaven_stem.name, earth_branch.name) }

    
    def _get_terrain(self = None, stem = None, branch = None):
        '''
        计算十二长生.
        '''
        get_changsheng_state = get_changsheng_state
        import professional_data
        return get_changsheng_state(stem, branch)

    
    def build_gods_object(self = None, eight_char = None, gender = None):
        '''
        构建神煞对象.
        '''
        get_shensha = get_shensha
        import professional_data
        eight_char.year.heaven_stem.name
        eight_char.month.heaven_stem.name
        day_gan = eight_char.day.heaven_stem.name
        eight_char.hour.heaven_stem.name
        year_zhi = eight_char.year.earth_branch.name
        month_zhi = eight_char.month.earth_branch.name
        day_zhi = eight_char.day.earth_branch.name
        hour_zhi = eight_char.hour.earth_branch.name
        result = {
            '年柱': [],
            '月柱': [],
            '日柱': [],
            '时柱': [] }
        tianyi = get_shensha(day_gan, 'tianyi')
        if tianyi:
            for zhi in (year_zhi, month_zhi, day_zhi, hour_zhi):
                if not zhi in tianyi:
                    continue
                if zhi == year_zhi:
                    result['年柱'].append('天乙贵人')
                if zhi == month_zhi:
                    result['月柱'].append('天乙贵人')
                if zhi == day_zhi:
                    result['日柱'].append('天乙贵人')
                if not zhi == hour_zhi:
                    continue
                result['时柱'].append('天乙贵人')
        wenchang = get_shensha(day_gan, 'wenchang')
        if wenchang:
            for zhi in (year_zhi, month_zhi, day_zhi, hour_zhi):
                if not zhi == wenchang:
                    continue
                if zhi == year_zhi:
                    result['年柱'].append('文昌贵人')
                if zhi == month_zhi:
                    result['月柱'].append('文昌贵人')
                if zhi == day_zhi:
                    result['日柱'].append('文昌贵人')
                if not zhi == hour_zhi:
                    continue
                result['时柱'].append('文昌贵人')
        yima = get_shensha(day_zhi, 'yima')
        if yima:
            for zhi in (year_zhi, month_zhi, day_zhi, hour_zhi):
                if not zhi == yima:
                    continue
                if zhi == year_zhi:
                    result['年柱'].append('驿马星')
                if zhi == month_zhi:
                    result['月柱'].append('驿马星')
                if zhi == day_zhi:
                    result['日柱'].append('驿马星')
                if not zhi == hour_zhi:
                    continue
                result['时柱'].append('驿马星')
        taohua = get_shensha(day_zhi, 'taohua')
        if taohua:
            for zhi in (year_zhi, month_zhi, day_zhi, hour_zhi):
                if not zhi == taohua:
                    continue
                if zhi == year_zhi:
                    result['年柱'].append('桃花星')
                if zhi == month_zhi:
                    result['月柱'].append('桃花星')
                if zhi == day_zhi:
                    result['日柱'].append('桃花星')
                if not zhi == hour_zhi:
                    continue
                result['时柱'].append('桃花星')
        huagai = get_shensha(day_zhi, 'huagai')
        if huagai:
            for zhi in (year_zhi, month_zhi, day_zhi, hour_zhi):
                if not zhi == huagai:
                    continue
                if zhi == year_zhi:
                    result['年柱'].append('华盖星')
                if zhi == month_zhi:
                    result['月柱'].append('华盖星')
                if zhi == day_zhi:
                    result['日柱'].append('华盖星')
                if not zhi == hour_zhi:
                    continue
                result['时柱'].append('华盖星')
        return result

    
    def build_decade_fortune_object(self, solar_time = None, eight_char = None, gender = None, day_master = ('solar_time', SolarTime, 'eight_char', EightChar, 'gender', int, 'day_master', str, 'return', Dict[(str, Any)])):
        '''
        构建大运对象.
        '''
        year_yin_yang = eight_char.year.heaven_stem.yin_yang
        month_gan = eight_char.month.heaven_stem.name
        month_zhi = eight_char.month.earth_branch.name
        fortune_list = []
        start_age = self._calculate_start_age(solar_time, eight_char, gender)
        for i in range(10):
            age_start = start_age + i * 10
            age_end = age_start + 9
            year_start = solar_time.year + age_start
            year_end = solar_time.year + age_end
            fortune_gz = self._calculate_fortune_ganzhi(month_gan, month_zhi, i + 1, gender, year_yin_yang)
            fortune_gan = fortune_gz[0]
            fortune_zhi = fortune_gz[1]
            ZHI_CANG_GAN = ZHI_CANG_GAN
            import professional_data
            zhi_ten_gods = []
            zhi_canggan = []
            if fortune_zhi in ZHI_CANG_GAN:
                canggan_data = ZHI_CANG_GAN[fortune_zhi]
                for hidden_gan, strength in canggan_data.items():
                    ten_god = self._get_ten_star(day_master, hidden_gan)
                    zhi_ten_gods.append(f'''{ten_god}({hidden_gan})''')
                    zhi_canggan.append(f'''{hidden_gan}({strength})''')
            fortune_list.append({
                '干支': fortune_gz,
                '开始年份': year_start,
                '结束': year_end,
                '天干十神': self._get_ten_star(day_master, fortune_gan),
                '地支十神': zhi_ten_gods if zhi_ten_gods else [
                    f'''地支{fortune_zhi}'''],
                '地支藏干': zhi_canggan if zhi_canggan else [
                    fortune_zhi],
                '开始年龄': age_start,
                '结束年龄': age_end })
        return {
            '起运日期': f'''{solar_time.year + start_age}-{solar_time.month}-{solar_time.day}''',
            '起运年龄': start_age,
            '大运': fortune_list }

    
    def _calculate_fortune_ganzhi(self, month_gan, month_zhi = None, step = None, gender = None, year_yin_yang = ('month_gan', str, 'month_zhi', str, 'step', int, 'gender', int, 'year_yin_yang', int, 'return', str)):
        '''
        计算大运干支.
        '''
        GAN = GAN
        ZHI = ZHI
        import professional_data
        if (gender == 1 or year_yin_yang == 1 or gender == 0) and year_yin_yang == -1:
            direction = 1
        else:
            direction = -1
        month_gan_idx = GAN.index(month_gan)
        month_zhi_idx = ZHI.index(month_zhi)
        fortune_gan_idx = (month_gan_idx + step * direction) % 10
        fortune_zhi_idx = (month_zhi_idx + step * direction) % 12
        return GAN[fortune_gan_idx] + ZHI[fortune_zhi_idx]

    
    def build_bazi(self = None, solar_datetime = None, lunar_datetime = None, gender = (None, None, 1, 2), eight_char_provider_sect = ('solar_datetime', Optional[str], 'lunar_datetime', Optional[str], 'gender', int, 'eight_char_provider_sect', int, 'return', BaziAnalysis)):
        '''
        构建八字分析.
        '''
        if not solar_datetime and lunar_datetime:
            raise ValueError('solarDatetime和lunarDatetime必须传且只传其中一个')
        if solar_datetime:
            solar_time = self.engine.parse_solar_time(solar_datetime)
            lunar_time = self.engine.solar_to_lunar(solar_time)
        else:
            lunar_dt = self._parse_lunar_datetime(lunar_datetime)
            lunar_time = lunar_dt
            solar_time = self._lunar_to_solar(lunar_dt)
        eight_char = self.engine.build_eight_char(solar_time)
        day_master = eight_char.day.heaven_stem.name
        zodiac = self._get_zodiac_by_lunar_year(solar_time)
    # WARNING: Decompyle incomplete

    
    def _parse_lunar_datetime(self = None, lunar_datetime = None):
        '''
        解析农历时间字符串 - 支持多种格式.
        '''
        import re
        datetime = datetime
        import datetime
        chinese_match = re.match('农历(\\d{4})年(\\S+)月(\\S+)(?:\\s+(.+))?', lunar_datetime)
        if chinese_match:
            year = int(chinese_match.group(1))
            month_str = chinese_match.group(2)
            day_str = chinese_match.group(3)
            time_str = chinese_match.group(4)
            month = self._chinese_month_to_number(month_str)
            day = self._chinese_day_to_number(day_str)
            (hour, minute, second) = self._parse_time_part(time_str)
            return LunarTime(year = year, month = month, day = day, hour = hour, minute = minute, second = second)
        
        try:
            dt = datetime.fromisoformat(lunar_datetime)
            return LunarTime(year = dt.year, month = dt.month, day = dt.day, hour = dt.hour, minute = dt.minute, second = dt.second)
        except ValueError:
            formats = [
                '%Y-%m-%d %H:%M:%S',
                '%Y-%m-%d %H:%M',
                '%Y-%m-%d',
                '%Y/%m/%d %H:%M:%S',
                '%Y/%m/%d %H:%M',
                '%Y/%m/%d']
            dt = None
            for fmt in formats:
                dt = datetime.strptime(lunar_datetime, fmt)
                formats
            except ValueError:
                continue

    # WARNING: Decompyle incomplete

    
    def _lunar_to_solar(self = None, lunar_time = None):
        '''
        农历转公历.
        '''
        
        try:
            Lunar = Lunar
            import lunar_python
            lunar = Lunar.fromYmdHms(lunar_time.year, lunar_time.month, lunar_time.day, lunar_time.hour, lunar_time.minute, lunar_time.second)
            solar = lunar.getSolar()
            return SolarTime(year = solar.getYear(), month = solar.getMonth(), day = solar.getDay(), hour = solar.getHour(), minute = solar.getMinute(), second = solar.getSecond())
        except Exception:
            e = None
            raise ValueError(f'''农历转公历失败: {e}''')
            e = None
            del e


    
    def _calculate_fetal_origin(self = None, eight_char = None):
        '''
        计算胎元.
        '''
        GAN = GAN
        ZHI = ZHI
        import professional_data
        month_gan = eight_char.month.heaven_stem.name
        month_zhi = eight_char.month.earth_branch.name
        gan_idx = GAN.index(month_gan)
        fetal_gan = GAN[(gan_idx + 1) % 10]
        zhi_idx = ZHI.index(month_zhi)
        fetal_zhi = ZHI[(zhi_idx + 3) % 12]
        return f'''{fetal_gan}{fetal_zhi}'''

    
    def _calculate_fetal_breath(self = None, eight_char = None):
        '''
        计算胎息.
        '''
        GAN = GAN
        ZHI = ZHI
        import professional_data
        day_gan = eight_char.day.heaven_stem.name
        day_zhi = eight_char.day.earth_branch.name
        gan_idx = GAN.index(day_gan)
        zhi_idx = ZHI.index(day_zhi)
        breath_gan = GAN[(gan_idx + 1) % 10 if gan_idx % 2 == 0 else (gan_idx - 1) % 10]
        breath_zhi = ZHI[(zhi_idx + 6) % 12]
        return f'''{breath_gan}{breath_zhi}'''

    
    def _calculate_own_sign(self = None, eight_char = None):
        '''
        计算命宫.
        '''
        GAN = GAN
        ZHI = ZHI
        import professional_data
        month_zhi = eight_char.month.earth_branch.name
        hour_zhi = eight_char.hour.earth_branch.name
        month_idx = ZHI.index(month_zhi)
        hour_idx = ZHI.index(hour_zhi)
        ming_gong_num = (month_idx - 2) % 12
        hour_offset = (hour_idx - 3) % 12
        ming_gong_num = (ming_gong_num - hour_offset) % 12
        ming_gong_zhi = ZHI[(ming_gong_num + 2) % 12]
        ming_gong_gan = GAN[ming_gong_num % 10]
        return f'''{ming_gong_gan}{ming_gong_zhi}'''

    
    def _calculate_body_sign(self = None, eight_char = None):
        '''
        计算身宫.
        '''
        GAN = GAN
        ZHI = ZHI
        import professional_data
        month_zhi = eight_char.month.earth_branch.name
        hour_zhi = eight_char.hour.earth_branch.name
        month_idx = ZHI.index(month_zhi)
        hour_idx = ZHI.index(hour_zhi)
        shen_gong_idx = (month_idx + hour_idx) % 12
        shen_gong_zhi = ZHI[shen_gong_idx]
        shen_gong_gan = GAN[shen_gong_idx % 10]
        return f'''{shen_gong_gan}{shen_gong_zhi}'''

    
    def _build_relations_object(self = None, eight_char = None):
        '''
        构建刑冲合会关系.
        '''
        analyze_zhi_combinations = analyze_zhi_combinations
        import professional_data
        zhi_list = [
            eight_char.year.earth_branch.name,
            eight_char.month.earth_branch.name,
            eight_char.day.earth_branch.name,
            eight_char.hour.earth_branch.name]
        relations = analyze_zhi_combinations(zhi_list)
        return {
            '三合': relations.get('sanhe', []),
            '六合': relations.get('liuhe', []),
            '三会': relations.get('sanhui', []),
            '相冲': relations.get('chong', []),
            '相刑': relations.get('xing', []),
            '相害': relations.get('hai', []) }

    
    def get_solar_times(self = None, bazi = None):
        '''
        根据八字获取可能的公历时间.
        '''
        pillars = bazi.split(' ')
        if len(pillars) != 4:
            raise ValueError('八字格式错误')
        (year_pillar, month_pillar, day_pillar, hour_pillar) = pillars
        if len(year_pillar) != 2 and len(month_pillar) != 2 and len(day_pillar) != 2 or len(hour_pillar) != 2:
            raise ValueError('八字格式错误，每柱应为两个字符')
        year_zhi = year_pillar[1]
        year_gan = year_pillar[0]
        month_zhi = month_pillar[1]
        month_gan = month_pillar[0]
        day_zhi = day_pillar[1]
        day_gan = day_pillar[0]
        hour_zhi = hour_pillar[1]
        hour_gan = hour_pillar[0]
        result_times = []
        for year in range(1900, 2100):
            if self._match_year_pillar(year, year_gan, year_zhi):
                for month in range(1, 13):
                    if not self._match_month_pillar(year, month, month_gan, month_zhi):
                        continue
                    import calendar
                    max_day = calendar.monthrange(year, month)[1]
                    for day in range(1, max_day + 1):
                        if self._match_day_pillar(year, month, day, day_gan, day_zhi):
                            for hour in (0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22):
                                if not self._match_hour_pillar(hour, hour_gan, hour_zhi, year, month, day):
                                    continue
                                solar_time = f'''{year}-{month:02d}-{day:02d} {hour:02d}:00:00'''
                                result_times.append(solar_time)
                                if not len(result_times) >= 20:
                                    continue
                                
                                
                                
                                
                                return range(1900, 2100), range(1, 13), range(1, max_day + 1), (0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22), result_times
                continue
        continue
        return result_times[:20]
        except ValueError:
            continue
        except Exception:
            continue

    
    def _calculate_start_age(self = None, solar_time = None, eight_char = None, gender = ('solar_time', SolarTime, 'eight_char', EightChar, 'gender', int, 'return', int)):
        '''
        计算起运年龄.
        '''
        Solar = Solar
        import lunar_python
        GAN_YINYANG = GAN_YINYANG
        import professional_data
        year_gan = eight_char.year.heaven_stem.name
        year_gan_yinyang = GAN_YINYANG.get(year_gan, 1)
        
        try:
            birth_solar = Solar.fromYmdHms(solar_time.year, solar_time.month, solar_time.day, solar_time.hour, solar_time.minute, solar_time.second)
            if (gender == 1 or year_gan_yinyang == 1 or gender == 0) and year_gan_yinyang == -1:
                lunar = birth_solar.getLunar()
                next_jieqi = lunar.getNextJieQi()
                if next_jieqi:
                    next_jieqi_solar = next_jieqi.getSolar()
                    days_diff = self._calculate_days_diff(birth_solar, next_jieqi_solar)
                    start_age = max(1, days_diff // 3)
                else:
                    start_age = 3
            else:
                lunar = birth_solar.getLunar()
                prev_jieqi = lunar.getPrevJieQi()
                if prev_jieqi:
                    prev_jieqi_solar = prev_jieqi.getSolar()
                    days_diff = self._calculate_days_diff(prev_jieqi_solar, birth_solar)
                    start_age = max(1, days_diff // 3)
                else:
                    start_age = 5
            return max(1, min(start_age, 10))
        except Exception:
            if (gender == 1 or year_gan_yinyang == 1 or gender == 0) and year_gan_yinyang == -1:
                base_age = 3
            else:
                base_age = 5
            month_adjustment = {
                1: 0,
                2: 1,
                3: 0,
                4: 1,
                5: 0,
                6: 1,
                7: 0,
                8: 1,
                9: 0,
                10: 1,
                11: 0,
                12: 1 }
            final_age = base_age + month_adjustment.get(solar_time.month, 0)
            return 


    
    def _parse_time_part(self = None, time_str = None):
        '''
        解析时间部分，返回(hour, minute, second)
        '''
        if not time_str:
            return (0, 0, 0)
        time_str = time_str.strip()
    # WARNING: Decompyle incomplete

    
    def _chinese_month_to_number(self = None, month_str = None):
        '''
        转换中文月份为数字.
        '''
        month_map = {
            '正': 1,
            '一': 1,
            '二': 2,
            '三': 3,
            '四': 4,
            '五': 5,
            '六': 6,
            '七': 7,
            '八': 8,
            '九': 9,
            '十': 10,
            '冬': 11,
            '腊': 12 }
        return month_map.get(month_str, 1)

    
    def _chinese_day_to_number(self = None, day_str = None):
        '''
        转换中文日期为数字.
        '''
        chinese_numbers = {
            '一': 1,
            '二': 2,
            '三': 3,
            '四': 4,
            '五': 5,
            '六': 6,
            '七': 7,
            '八': 8,
            '九': 9,
            '十': 10,
            '廿': 20,
            '卅': 30 }
        if '初' in day_str:
            day_num = day_str.replace('初', '')
            if day_num in chinese_numbers:
                return chinese_numbers[day_num]
            if None.isdigit():
                return int(day_num)
            return None
        if None in day_str:
            if day_str == '十':
                return 10
            if day_str.startswith('十'):
                remaining = day_str[1:]
                if remaining.isdigit():
                    return 10 + chinese_numbers.get(remaining, int(remaining))
                return 10 + None(chinese_numbers.get, remaining)
            if None.endswith('十'):
                prefix = day_str[:-1]
                if prefix.isdigit():
                    return chinese_numbers.get(prefix, int(prefix)) * 10
                return None(chinese_numbers.get, prefix) * 10
        if '廿' in day_str:
            remaining = day_str.replace('廿', '')
            if remaining in chinese_numbers:
                return 20 + chinese_numbers[remaining]
            if remaining.isdigit():
                return None + int(remaining)
            return None + None
        if None in day_str:
            return 30
        if day_str in chinese_numbers:
            return chinese_numbers[day_str]
        
        try:
            return int(day_str)
        except ValueError:
            return 1


    
    def _calculate_days_diff(self = None, solar1 = None, solar2 = None):
        '''
        计算两个Solar对象之间的天数差.
        '''
        
        try:
            datetime = datetime
            import datetime
            dt1 = datetime(solar1.getYear(), solar1.getMonth(), solar1.getDay())
            dt2 = datetime(solar2.getYear(), solar2.getMonth(), solar2.getDay())
            return abs((dt2 - dt1).days)
        except Exception:
            return 3


    
    def _match_year_pillar(self = None, year = None, gan = None, zhi = ('year', int, 'gan', str, 'zhi', str, 'return', bool)):
        '''匹配年柱 - 修复版本，考虑立春节气'''
        
        try:
            Solar = Solar
            import lunar_python
            solar_start = Solar.fromYmdHms(year, 1, 1, 0, 0, 0)
            lunar_start = solar_start.getLunar()
            bazi_start = lunar_start.getEightChar()
            solar_mid = Solar.fromYmdHms(year, 6, 1, 0, 0, 0)
            lunar_mid = solar_mid.getLunar()
            bazi_mid = lunar_mid.getEightChar()
            solar_end = Solar.fromYmdHms(year, 12, 31, 23, 59, 59)
            lunar_end = solar_end.getLunar()
            bazi_end = lunar_end.getEightChar()
            year_gans = [
                bazi_start.getYearGan(),
                bazi_mid.getYearGan(),
                bazi_end.getYearGan()]
            year_zhis = [
                bazi_start.getYearZhi(),
                bazi_mid.getYearZhi(),
                bazi_end.getYearZhi()]
            for i in range(len(year_gans)):
                if not year_gans[i] == gan:
                    continue
                    
                    try:
                        if not year_zhis[i] == zhi:
                            continue
                            
                            try:
                                range(len(year_gans))
                                return True
                                
                                try:
                                    return False
                                except Exception:
                                    return False





    
    def _match_month_pillar(self, year = None, month = None, gan = None, zhi = ('year', int, 'month', int, 'gan', str, 'zhi', str, 'return', bool)):
        '''匹配月柱 - 修复版本，考虑节气边界'''
        
        try:
            Solar = Solar
            import lunar_python
            test_days = [
                1,
                8,
                15,
                22,
                28]
            month_pillars = set()
            for day in test_days:
                import calendar
                max_day = calendar.monthrange(year, month)[1]
                if day > max_day:
                    day = max_day
                solar = Solar.fromYmdHms(year, month, day, 12, 0, 0)
                lunar = solar.getLunar()
                bazi = lunar.getEightChar()
                month_gan = bazi.getMonthGan()
                month_zhi = bazi.getMonthZhi()
                month_pillars.add(f'''{month_gan}{month_zhi}''')
                
                try:
                    continue
                    target_pillar = f'''{gan}{zhi}'''
                    return target_pillar in month_pillars
                    except Exception:
                        
                        try:
                            continue
                            
                            try:
                                pass
                            except Exception:
                                return False





    
    def _match_day_pillar(self, year, month = None, day = None, gan = None, zhi = ('year', int, 'month', int, 'day', int, 'gan', str, 'zhi', str, 'return', bool)):
        '''
        匹配日柱.
        '''
        
        try:
            Solar = Solar
            import lunar_python
            solar = Solar.fromYmdHms(year, month, day, 0, 0, 0)
            lunar = solar.getLunar()
            bazi = lunar.getEightChar()
            day_gan = bazi.getDayGan()
            day_zhi = bazi.getDayZhi()
            if day_gan == gan:
                day_gan == gan
            return day_zhi == zhi
        except Exception:
            return False


    
    def _match_hour_pillar(self, hour, gan = None, zhi = None, year = None, month = (None, None, None), day = ('hour', int, 'gan', str, 'zhi', str, 'year', int, 'month', int, 'day', int, 'return', bool)):
        '''匹配时柱 - 修复版本，使用实际日期'''
        
        try:
            Solar = Solar
            import lunar_python
            use_year = year if year else 2024
            use_month = month if month else 1
            use_day = day if day else 1
            solar = Solar.fromYmdHms(use_year, use_month, use_day, hour, 0, 0)
            lunar = solar.getLunar()
            bazi = lunar.getEightChar()
            hour_gan = bazi.getTimeGan()
            hour_zhi = bazi.getTimeZhi()
            if hour_gan == gan:
                hour_gan == gan
            return hour_zhi == zhi
        except Exception:
            return False


    
    def _get_zodiac_by_lunar_year(self = None, solar_time = None):
        '''
        根据农历年份获取生肖（以春节为界，不是立春）
        '''
        
        try:
            Solar = Solar
            import lunar_python
            solar = Solar.fromYmdHms(solar_time.year, solar_time.month, solar_time.day, solar_time.hour, solar_time.minute, solar_time.second)
            lunar = solar.getLunar()
            return lunar.getYearShengXiao()
        except Exception:
            e = None
            print(f'''获取农历生肖失败，使用八字年柱生肖: {e}''')
            eight_char = self.engine.build_eight_char(solar_time)
            del e
            return None
            None = 
            del e



_bazi_calculator = None

def get_bazi_calculator():
    '''
    获取八字计算器单例.
    '''
    pass
# WARNING: Decompyle incomplete

