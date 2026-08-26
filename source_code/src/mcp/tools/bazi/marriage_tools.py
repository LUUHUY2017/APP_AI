# Source Generated with Decompyle++
# File: marriage_tools.pyc (Python 3.12)

'''
婚姻分析工具函数.
'''
import json
from typing import Any, Dict, List
from src.utils.logging_config import get_logger
from bazi_calculator import get_bazi_calculator
from marriage_analyzer import get_marriage_analyzer
logger = get_logger(__name__)

async def analyze_marriage_timing(args = None):
    '''
    分析婚姻时机和配偶信息.
    '''
    pass
# WARNING: Decompyle incomplete


async def analyze_marriage_compatibility(args = None):
    '''
    分析两人八字婚姻合婚.
    '''
    pass
# WARNING: Decompyle incomplete


def _analyze_compatibility(male_bazi = None, female_bazi = None):
    '''分析两人八字合婚 - 使用专业算法'''
    male_day_gan = male_bazi.day_master
    female_day_gan = female_bazi.day_pillar['天干']['天干']
    male_day_zhi = male_bazi.day_pillar['地支']['地支']
    female_day_zhi = female_bazi.day_pillar['地支']['地支']
    element_analysis = _analyze_element_compatibility(male_day_gan, female_day_gan)
    zodiac_analysis = _analyze_zodiac_compatibility(male_bazi.zodiac, female_bazi.zodiac)
    pillar_analysis = _analyze_pillar_compatibility(male_day_gan + male_day_zhi, female_day_gan + female_day_zhi)
    branch_analysis = _analyze_branch_relationships(male_bazi, female_bazi)
    complement_analysis = _analyze_complement(male_bazi, female_bazi)
    total_score = element_analysis['score'] * 0.3 + zodiac_analysis['score'] * 0.2 + pillar_analysis['score'] * 0.2 + branch_analysis['score'] * 0.15 + complement_analysis['score'] * 0.15
    return {
        'overall_score': round(total_score, 1),
        'overall_level': _get_compatibility_level(total_score),
        'element_analysis': element_analysis,
        'zodiac_analysis': zodiac_analysis,
        'pillar_analysis': pillar_analysis,
        'branch_analysis': branch_analysis,
        'complement_analysis': complement_analysis,
        'suggestions': _get_professional_suggestions(total_score, element_analysis, zodiac_analysis) }


def _analyze_element_compatibility(male_gan = None, female_gan = None):
    '''
    专业五行相配分析.
    '''
    GAN_WUXING = GAN_WUXING
    WUXING_RELATIONS = WUXING_RELATIONS
    import professional_data
    male_element = GAN_WUXING.get(male_gan, '')
    female_element = GAN_WUXING.get(female_gan, '')
    element_relation = WUXING_RELATIONS.get((male_element, female_element), '')
    score_map = {
        '↓': 90,
        '=': 80,
        '←': 50,
        '→': 55,
        '↑': 85 }
    desc_map = {
        '↓': '男生女，夫妻恩爱，家庭和睦',
        '=': '同类相配，志趣相投，容易理解',
        '←': '女克男，女强男弱，需要平衡',
        '→': '男克女，男强女弱，需要包容',
        '↑': '女生男，妻贤夫贵，互相成就' }
    return {
        'male_element': male_element,
        'female_element': female_element,
        'relation': element_relation,
        'score': score_map.get(element_relation, 70),
        'description': desc_map.get(element_relation, '关系平和') }


def _analyze_zodiac_compatibility(male_zodiac = None, female_zodiac = None):
    '''
    专业生肖相配分析.
    '''
    ZHI_CHONG = ZHI_CHONG
    ZHI_HAI = ZHI_HAI
    ZHI_LIUHE = ZHI_LIUHE
    ZHI_SANHE = ZHI_SANHE
    ZHI_XING = ZHI_XING
    import professional_data
    zodiac_to_zhi = {
        '鼠': '子',
        '牛': '丑',
        '虎': '寅',
        '兔': '卯',
        '龙': '辰',
        '蛇': '巳',
        '马': '午',
        '羊': '未',
        '猴': '申',
        '鸡': '酉',
        '狗': '戌',
        '猪': '亥' }
    male_zhi = zodiac_to_zhi.get(male_zodiac, '')
    female_zhi = zodiac_to_zhi.get(female_zodiac, '')
    if (male_zhi, female_zhi) in ZHI_LIUHE or (female_zhi, male_zhi) in ZHI_LIUHE:
        return {
            'score': 90,
            'level': '天作之合',
            'description': '六合生肖，感情深厚',
            'relation': '六合' }
    for sanhe_group in None:
        if not male_zhi in sanhe_group:
            continue
        if not female_zhi in sanhe_group:
            continue
        
        return None, {
            'score': 85,
            'level': '天作之合',
            'description': '三合生肖，相处融洽',
            'relation': '三合' }
    if (male_zhi, female_zhi) in ZHI_CHONG or (female_zhi, male_zhi) in ZHI_CHONG:
        return {
            'score': 30,
            'level': '相冲不合',
            'description': '生肖相冲，矛盾较多',
            'relation': '相冲' }
    for None in None:
        if not male_zhi in xing_group:
            continue
        if not female_zhi in xing_group:
            continue
        return None, {
            'score': 40,
            'level': '相刑不合',
            'description': '生肖相刑，需要化解',
            'relation': '相刑' }
    if (male_zhi, female_zhi) in ZHI_HAI or (female_zhi, male_zhi) in ZHI_HAI:
        return {
            'score': 45,
            'level': '相害不合',
            'description': '生肖相害，小有不合',
            'relation': '相害' }
    return {
        'score': None,
        'level': '一般',
        'description': '生肖平和，无特别冲突',
        'relation': '平和' }


def _analyze_pillar_compatibility(male_pillar = None, female_pillar = None):
    '''
    专业日柱相配分析.
    '''
    if male_pillar == female_pillar:
        return {
            'score': 55,
            'description': '日柱相同，共通点多但需要差异化解' }
    male_zhi = male_pillar[1]
    male_gan = None[0]
    female_zhi = female_pillar[1]
    female_gan = female_pillar[0]
    score = 70
    get_ten_gods_relation = get_ten_gods_relation
    import professional_data
    gan_relation = get_ten_gods_relation(male_gan, female_gan)
    if gan_relation in ('正财', '偏财', '正官', '七杀'):
        score += 10
    ZHI_CHONG = ZHI_CHONG
    ZHI_LIUHE = ZHI_LIUHE
    import professional_data
    if (male_zhi, female_zhi) in ZHI_LIUHE or (female_zhi, male_zhi) in ZHI_LIUHE:
        score += 15
    elif (male_zhi, female_zhi) in ZHI_CHONG or (female_zhi, male_zhi) in ZHI_CHONG:
        score -= 20
    return {
        'score': min(95, max(30, score)),
        'description': f'''日柱组合分析：{gan_relation}关系''' }


def _analyze_branch_relationships(male_bazi = None, female_bazi = None):
    '''
    分析地支关系.
    '''
    male_branches = [
        male_bazi.year_pillar['地支']['地支'],
        male_bazi.month_pillar['地支']['地支'],
        male_bazi.day_pillar['地支']['地支'],
        male_bazi.hour_pillar['地支']['地支']]
    female_branches = [
        female_bazi.year_pillar['地支']['地支'],
        female_bazi.month_pillar['地支']['地支'],
        female_bazi.day_pillar['地支']['地支'],
        female_bazi.hour_pillar['地支']['地支']]
    analyze_zhi_combinations = analyze_zhi_combinations
    import professional_data
    combined_branches = male_branches + female_branches
    relationships = analyze_zhi_combinations(combined_branches)
    score = 70
    if relationships.get('liuhe', []):
        score += 10
    if relationships.get('sanhe', []):
        score += 8
    if relationships.get('chong', []):
        score -= 15
    if relationships.get('xing', []):
        score -= 10
    return {
        'score': min(95, max(30, score)),
        'relationships': relationships,
        'description': f'''地支关系分析：{len(relationships.get('liuhe', []))}个六合、{len(relationships.get('chong', []))}个相冲''' }


def _analyze_complement(male_bazi = None, female_bazi = None):
    '''
    分析八字互补性.
    '''
    GAN_WUXING = GAN_WUXING
    WUXING = WUXING
    ZHI_WUXING = ZHI_WUXING
    import professional_data
    male_elements = []
    female_elements = []
    for pillar in (male_bazi.year_pillar, male_bazi.month_pillar, male_bazi.day_pillar, male_bazi.hour_pillar):
        gan = pillar['天干']['天干']
        zhi = pillar['地支']['地支']
        male_elements.extend([
            GAN_WUXING.get(gan, ''),
            ZHI_WUXING.get(zhi, '')])
    for pillar in (female_bazi.year_pillar, female_bazi.month_pillar, female_bazi.day_pillar, female_bazi.hour_pillar):
        gan = pillar['天干']['天干']
        zhi = pillar['地支']['地支']
        female_elements.extend([
            GAN_WUXING.get(gan, ''),
            ZHI_WUXING.get(zhi, '')])
    Counter = Counter
    import collections
    male_counter = Counter(male_elements)
    female_counter = Counter(female_elements)
    complement_score = 0
    for element in WUXING:
        male_count = male_counter.get(element, 0)
        female_count = female_counter.get(element, 0)
        if male_count > 0 and female_count == 0:
            complement_score += 5
            continue
        if male_count == 0 and female_count > 0:
            complement_score += 5
            continue
        if not abs(male_count - female_count) <= 1:
            continue
        complement_score += 2
    return {
        'score': min(90, 50 + complement_score),
        'male_elements': dict(male_counter),
        'female_elements': dict(female_counter),
        'description': f'''五行互补性分析，补分{complement_score}''' }


def _get_professional_suggestions(total_score = None, element_analysis = None, zodiac_analysis = None):
    '''
    获取专业合婚建议.
    '''
    suggestions = []
    if total_score >= 80:
        suggestions.extend([
            '天作之合，婚姻美满',
            '互相扶持，白头偕老'])
    elif total_score >= 70:
        suggestions.extend([
            '基础良好，需要磨合',
            '多沟通理解，感情可长久'])
    elif total_score >= 60:
        suggestions.extend([
            '需要努力经营',
            '多包容对方，化解矛盾'])
    else:
        suggestions.extend([
            '建议谨慎考虑',
            '如结婚需要择日化解'])
    if element_analysis['relation'] == '←':
        suggestions.append('女方需要多体谅男方，避免过于强势')
    elif element_analysis['relation'] == '→':
        suggestions.append('男方需要多关心女方，避免过于专横')
    if zodiac_analysis['relation'] == '相冲':
        suggestions.append('生肖相冲，建议佩戴化解物品或择吉日结婚')
    return suggestions


def _get_compatibility_level(score = None):
    '''
    获取合婚等级.
    '''
    if score >= 80:
        return '上等婚'
    if score >= 70:
        return '中上婚'
    if score >= 60:
        return '中等婚'
    return '下等婚'


def _get_compatibility_suggestions(score = None):
