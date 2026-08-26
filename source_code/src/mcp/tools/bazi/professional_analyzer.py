# Source Generated with Decompyle++
# File: professional_analyzer.pyc (Python 3.12)

'''
八字命理专业分析器 使用内置专业数据进行准确的传统命理分析.
'''
from typing import Any, Dict, List
from professional_data import GAN_WUXING, WUXING, WUXING_RELATIONS, ZHI_CANG_GAN, ZHI_WUXING, analyze_zhi_combinations, get_changsheng_state, get_nayin, get_shensha, get_ten_gods_relation

class ProfessionalAnalyzer:
    '''专业八字分析器 - 使用完整的传统命理数据'''
    
    def __init__(self):
        '''
        初始化分析器.
        '''
        pass

    
    def get_ten_gods_analysis(self = None, day_master = None, other_stem = None):
        '''
        获取十神分析.
        '''
        return get_ten_gods_relation(day_master, other_stem)

    
    def analyze_eight_char_structure(self = None, eight_char_data = None):
        '''
        全面分析八字结构.
        '''
        year_gan = eight_char_data.get('year', { }).get('heaven_stem', { }).get('name', '')
        year_zhi = eight_char_data.get('year', { }).get('earth_branch', { }).get('name', '')
        month_gan = eight_char_data.get('month', { }).get('heaven_stem', { }).get('name', '')
        month_zhi = eight_char_data.get('month', { }).get('earth_branch', { }).get('name', '')
        day_gan = eight_char_data.get('day', { }).get('heaven_stem', { }).get('name', '')
        day_zhi = eight_char_data.get('day', { }).get('earth_branch', { }).get('name', '')
        hour_gan = eight_char_data.get('hour', { }).get('heaven_stem', { }).get('name', '')
        hour_zhi = eight_char_data.get('hour', { }).get('earth_branch', { }).get('name', '')
        gan_list = [
            year_gan,
            month_gan,
            day_gan,
            hour_gan]
        zhi_list = [
            year_zhi,
            month_zhi,
            day_zhi,
            hour_zhi]
        analysis = {
            'day_master': day_gan,
            'ten_gods': self._analyze_ten_gods(day_gan, gan_list, zhi_list),
            'nayin': self._analyze_nayin(gan_list, zhi_list),
            'changsheng': self._analyze_changsheng(day_gan, zhi_list),
            'zhi_relations': analyze_zhi_combinations(zhi_list),
            'wuxing_balance': self._analyze_wuxing_balance(gan_list, zhi_list),
            'shensha': self._analyze_shensha(gan_list, zhi_list),
            'strength': self._analyze_day_master_strength(day_gan, month_zhi, zhi_list),
            'useful_god': self._determine_useful_god(day_gan, month_zhi, gan_list, zhi_list) }
        return analysis

    
    def _analyze_ten_gods(self = None, day_master = None, gan_list = None, zhi_list = ('day_master', str, 'gan_list', List[str], 'zhi_list', List[str], 'return', Dict[(str, List[str])])):
        '''
        分析十神分布.
        '''
        ten_gods = {
            '比肩': [],
            '劫财': [],
            '食神': [],
            '伤官': [],
            '偏财': [],
            '正财': [],
            '七杀': [],
            '正官': [],
            '偏印': [],
            '正印': [] }
        for i, gan in enumerate(gan_list):
            if gan == day_master:
                continue
            ten_god = get_ten_gods_relation(day_master, gan)
            pillar_names = [
                '年干',
                '月干',
                '日干',
                '时干']
            if not ten_god in ten_gods:
                continue
            ten_gods[ten_god].append(f'''{pillar_names[i]}{gan}''')
        pillar_names = [
            '年支',
            '月支',
            '日支',
            '时支']
        for i, zhi in enumerate(zhi_list):
            cang_gan = ZHI_CANG_GAN.get(zhi, { })
            for gan, strength in cang_gan.items():
                if gan == day_master:
                    continue
                ten_god = get_ten_gods_relation(day_master, gan)
                if not ten_god in ten_gods:
                    continue
                ten_gods[ten_god].append(f'''{pillar_names[i]}{zhi}藏{gan}({strength})''')
        return ten_gods

    
    def _analyze_nayin(self = None, gan_list = None, zhi_list = None):
        '''
        分析纳音.
        '''
        nayin_list = []
        pillar_names = [
            '年柱',
            '月柱',
            '日柱',
            '时柱']
        for gan, zhi in enumerate(zip(gan_list, zhi_list)):
            nayin = get_nayin(gan, zhi)
            nayin_list.append(f'''{pillar_names[i]}{gan}{zhi}：{nayin}''')
        return nayin_list

    
    def _analyze_changsheng(self = None, day_master = None, zhi_list = None):
        '''
        分析长生十二宫.
        '''
        changsheng_list = []
        pillar_names = [
            '年支',
            '月支',
            '日支',
            '时支']
        for i, zhi in enumerate(zhi_list):
            state = get_changsheng_state(day_master, zhi)
            changsheng_list.append(f'''{pillar_names[i]}{zhi}：{state}''')
        return changsheng_list

    
    def _analyze_wuxing_balance(self = None, gan_list = None, zhi_list = None):
        '''
        分析五行平衡.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _calculate_balance_score(self = None, wuxing_count = None):
        '''
        计算五行平衡分数（0-100，100为完全平衡）
        '''
        pass
    # WARNING: Decompyle incomplete

    
    def _analyze_shensha(self = None, gan_list = None, zhi_list = None):
        '''
        分析神煞 - 修复版本，正确区分以日干查和以日支查的神煞.
        '''
        shensha = {
            '天乙贵人': [],
            '文昌贵人': [],
            '驿马星': [],
            '桃花星': [],
            '华盖星': [] }
        day_gan = gan_list[2] if len(gan_list) > 2 else ''
        day_zhi = zhi_list[2] if len(zhi_list) > 2 else ''
        pillar_names = [
            '年支',
            '月支',
            '日支',
            '时支']
        day_gan_shensha = [
            ('tianyi', '天乙贵人'),
            ('wenchang', '文昌贵人')]
        for shensha_type, shensha_name in day_gan_shensha:
            shensha_zhi = get_shensha(day_gan, shensha_type)
            if not shensha_zhi:
                continue
            for i, zhi in enumerate(zhi_list):
                if not zhi in shensha_zhi:
                    continue
                shensha[shensha_name].append(f'''{pillar_names[i]}{zhi}''')
        day_zhi_shensha = [
            ('yima', '驿马星'),
            ('taohua', '桃花星'),
            ('huagai', '华盖星')]
        for shensha_type, shensha_name in day_zhi_shensha:
            shensha_zhi = get_shensha(day_zhi, shensha_type)
            if not shensha_zhi:
                continue
            for i, zhi in enumerate(zhi_list):
                if not zhi == shensha_zhi:
                    continue
                shensha[shensha_name].append(f'''{pillar_names[i]}{zhi}''')
        return shensha

    
    def _analyze_day_master_strength(self = None, day_master = None, month_zhi = None, zhi_list = ('day_master', str, 'month_zhi', str, 'zhi_list', List[str], 'return', Dict[(str, Any)])):
        '''
        分析日主强弱.
        '''
        month_element = ZHI_WUXING.get(month_zhi, '')
        day_element = GAN_WUXING.get(day_master, '')
        month_relation = WUXING_RELATIONS.get((day_element, month_element), '')
        same_element_count = 0
        help_element_count = 0
        for zhi in zhi_list:
            zhi_element = ZHI_WUXING.get(zhi, '')
            if zhi_element == day_element:
                same_element_count += 1
                continue
            if not WUXING_RELATIONS.get((zhi_element, day_element)) == '↓':
                continue
            help_element_count += 1
        strength_score = 0
        if month_relation == '↑':
            strength_score -= 30
        elif month_relation == '↓':
            strength_score += 30
        elif month_relation == '=':
            strength_score += 20
        elif month_relation == '←':
            strength_score -= 20
        elif month_relation == '→':
            strength_score -= 10
        strength_score += same_element_count * 15
        strength_score += help_element_count * 10
        if strength_score >= 30:
            strength_level = '偏强'
        elif strength_score >= 10:
            strength_level = '中和'
        elif strength_score >= -10:
            strength_level = '偏弱'
        else:
            strength_level = '很弱'
        return {
            'level': strength_level,
            'score': strength_score,
            'month_relation': month_relation,
            'same_element_count': same_element_count,
            'help_element_count': help_element_count }

    
    def _determine_useful_god(self, day_master = None, month_zhi = None, gan_list = None, zhi_list = ('day_master', str, 'month_zhi', str, 'gan_list', List[str], 'zhi_list', List[str], 'return', Dict[(str, Any)])):
        '''
        确定用神.
        '''
        day_element = GAN_WUXING.get(day_master, '')
        strength_analysis = self._analyze_day_master_strength(day_master, month_zhi, zhi_list)
        useful_gods = []
        avoid_gods = []
        if strength_analysis['level'] in ('偏强', '很强'):
            for element in WUXING:
                relation = WUXING_RELATIONS.get((day_element, element), '')
                if relation == '→':
                    useful_gods.append(f'''{element}（财星）''')
                    continue
                if relation == '↓':
                    useful_gods.append(f'''{element}（食伤）''')
                    continue
                if not relation == '←':
                    continue
                useful_gods.append(f'''{element}（官杀）''')
        else:
            for element in WUXING:
                relation = WUXING_RELATIONS.get((element, day_element), '')
                if relation == '↓':
                    useful_gods.append(f'''{element}（印星）''')
                    continue
                if not relation == '=':
                    continue
                useful_gods.append(f'''{element}（比劫）''')
        if strength_analysis['level'] in ('偏弱', '很弱'):
            return {
                'useful_gods': useful_gods[:3],
                'avoid_gods': avoid_gods[:3],
                'strategy': '扶抑' }
        return {
            'useful_gods': None,
            'avoid_gods': useful_gods[:3],
            'strategy': avoid_gods[:3] }

    
    def get_detailed_fortune_analysis(self = None, eight_char_data = None):
        '''
        获取详细的命理分析文本.
        '''
        analysis = self.analyze_eight_char_structure(eight_char_data)
        result_lines = []
        result_lines.append('=== 八字命理详细分析 ===\n')
        result_lines.append(f'''【日主】{analysis['day_master']}（{GAN_WUXING.get(analysis['day_master'], '')}）''')
        result_lines.append(f'''【强弱】{analysis['strength']['level']}（得分：{analysis['strength']['score']}）''')
        result_lines.append('')
        result_lines.append('【十神分布】')
        for god_name, positions in analysis['ten_gods'].items():
            if not positions:
                continue
            result_lines.append(f'''  {god_name}：{', '.join(positions)}''')
        result_lines.append('')
        result_lines.append('【用神分析】')
        result_lines.append(f'''  策略：{analysis['useful_god']['strategy']}''')
        if analysis['useful_god']['useful_gods']:
            result_lines.append(f'''  用神：{', '.join(analysis['useful_god']['useful_gods'])}''')
        result_lines.append('')
        result_lines.append('【五行分布】')
        for element, count in analysis['wuxing_balance']['distribution'].items():
            result_lines.append(f'''  {element}：{count:.1f}''')
        result_lines.append(f'''  平衡分：{analysis['wuxing_balance']['balance_score']}''')
        result_lines.append('')
        result_lines.append('【地支关系】')
        for relation_type, relations in analysis['zhi_relations'].items():
            if not relations:
                continue
            result_lines.append(f'''  {relation_type}：{', '.join(relations)}''')
        result_lines.append('')
        result_lines.append('【神煞分析】')
        for shensha_name, positions in analysis['shensha'].items():
            if not positions:
                continue
            result_lines.append(f'''  {shensha_name}：{', '.join(positions)}''')
        result_lines.append('')
        result_lines.append('【纳音五行】')
        for nayin in analysis['nayin']:
            result_lines.append(f'''  {nayin}''')
        return '\n'.join(result_lines)


_professional_analyzer = None

def get_professional_analyzer():
    '''
    获取专业分析器单例.
    '''
    pass
# WARNING: Decompyle incomplete

