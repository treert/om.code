from dataclasses import dataclass, field
from pathlib import Path
import sys
import babel
import iso639
import colorlog
import logging

# nllb-200 基于的语言编码，格式 是 iso639_3 + _ + script. 【里面有不少 iso639 库不能识别。 _dump_lang_map 先过滤掉】
_nllb_200_lang_codes = [
'ace_Arab','ace_Latn','acm_Arab','acq_Arab','aeb_Arab','afr_Latn','ajp_Arab','aka_Latn','amh_Ethi','apc_Arab','arb_Arab','arb_Latn','ars_Arab','ary_Arab','arz_Arab','asm_Beng','ast_Latn','awa_Deva','ayr_Latn','azb_Arab','azj_Latn',
'bak_Cyrl','bam_Latn','ban_Latn','bel_Cyrl','bem_Latn','ben_Beng','bho_Deva','bjn_Arab','bjn_Latn','bod_Tibt','bos_Latn','bug_Latn','bul_Cyrl',
'cat_Latn','ceb_Latn','ces_Latn','cjk_Latn','ckb_Arab','crh_Latn','cym_Latn',
'dan_Latn','deu_Latn','dik_Latn','dyu_Latn','dzo_Tibt',
'ell_Grek','eng_Latn','epo_Latn','est_Latn','eus_Latn','ewe_Latn',
'fao_Latn','fij_Latn','fin_Latn','fon_Latn','fra_Latn','fur_Latn','fuv_Latn',
'gla_Latn','gle_Latn','glg_Latn','grn_Latn','guj_Gujr',
'hat_Latn','hau_Latn','heb_Hebr','hin_Deva','hne_Deva','hrv_Latn','hun_Latn','hye_Armn',
'ibo_Latn','ilo_Latn','ind_Latn','isl_Latn','ita_Latn',
'jav_Latn','jpn_Jpan',
'kab_Latn','kac_Latn','kam_Latn','kan_Knda','kas_Arab','kas_Deva','kat_Geor','knc_Arab','knc_Latn','kaz_Cyrl','kbp_Latn','kea_Latn','khm_Khmr','kik_Latn','kin_Latn','kir_Cyrl','kmb_Latn','kmr_Latn','kon_Latn','kor_Hang',
'lao_Laoo','lij_Latn','lim_Latn','lin_Latn','lit_Latn','lmo_Latn','ltg_Latn','ltz_Latn','lua_Latn','lug_Latn','luo_Latn','lus_Latn','lvs_Latn',
'mag_Deva','mai_Deva','mal_Mlym','mar_Deva','min_Arab','min_Latn','mkd_Cyrl',
'plt_Latn',# Plateau Malagasy
'mlt_Latn','mni_Beng',
'khk_Cyrl',
'mos_Latn','mri_Latn','mya_Mymr',
'nld_Latn','nno_Latn','nob_Latn','npi_Deva','nso_Latn','nus_Latn','nya_Latn',
'oci_Latn',
'gaz_Latn',# West Central Oromo
'ory_Orya',
'pag_Latn','pan_Guru','pap_Latn','pes_Arab','pol_Latn','por_Latn','prs_Arab','pbt_Arab',
'quy_Latn',
'ron_Latn','run_Latn','rus_Cyrl',
'sag_Latn','san_Deva','sat_Olck','scn_Latn','shn_Mymr','sin_Sinh','slk_Latn','slv_Latn','smo_Latn','sna_Latn','snd_Arab','som_Latn','sot_Latn','spa_Latn','als_Latn','srd_Latn','srp_Cyrl','ssw_Latn','sun_Latn','swe_Latn','swh_Latn','szl_Latn',
'tam_Taml','tat_Cyrl','tel_Telu','tgk_Cyrl','tgl_Latn','tha_Thai','tir_Ethi','taq_Latn','taq_Tfng','tpi_Latn','tsn_Latn','tso_Latn','tuk_Latn','tum_Latn','tur_Latn','twi_Latn','tzm_Tfng',
'uig_Arab','ukr_Cyrl','umb_Latn','urd_Arab','uzn_Latn',
'vec_Latn','vie_Latn',
'war_Latn','wol_Latn',
'xho_Latn',
'ydd_Hebr','yor_Latn','yue_Hant',
'zho_Hans','zho_Hant','zsm_Latn','zul_Latn',
]

def _get_language_name(iso_code, target_locale='zh') -> str:
    """
    用 babel 库获取给定 ISO 语言代码在指定目标语言（默认中文）下的名称。获取失败返回 ''
    iso_code 可以是 ISO 639-1 或 ISO 639-3。
    """
    try:
        # 创建源语言的 Locale 对象
        # Babel 会尝试根据 ISO 639-1 或 639-3 自动识别
        # 对于 'zh'，Babel 通常会理解为 'zh_Hans'
        lang_locale = babel.Locale.parse(iso_code)

        # 获取该语言在目标语言环境下的名称
        # 如果是 ISO 639-1 或 639-3，get_display_name() 应该能处理
        return lang_locale.get_display_name(target_locale) or ''
    except Exception as e:
        # 对于某些非常不常见的语言，Babel 可能也没有对应的本地化名称
        # print(f"Error getting Chinese name for {iso_code}: {e}")
        return ''

# 打印语言编码映射
def dump_support_lang_map():
    lang_arr = []
    miss_arr = []
    for nllb_code in _nllb_200_lang_codes:
        lang_code_3 = nllb_code.split('_')[0]
        try:
            lang_code_1 = iso639.to_iso639_1(lang_code_3)
            lang_name = iso639.to_name(lang_code_3)
            zh_name = _get_language_name(lang_code_3)   # 可选的
            if lang_code_1 and lang_name and zh_name:
                lang_arr.append((nllb_code, lang_code_3, lang_code_1, lang_name, zh_name))
            else:
                miss_arr.append(lang_code_3)
        except:
            miss_arr.append(lang_code_3)
            # logging.error(f'miss lang_name lang_code_3: {lang_code_3}')
            continue
    print(f"_nllb_200_lang_codes.num={len(_nllb_200_lang_codes)}")
    print(f'miss_arr {len(miss_arr)}: ',miss_arr)
    print(f'# support_lang_map[{len(lang_arr)}] = [(nllb_code, lang_code_3, lang_code_1, lang_name, zh_name)]')
    print('support_lang_map:list[(str,str,str,str,str)] = [',)
    for it in lang_arr:
        nllb_code, lang_code_3, lang_code_1, lang_name, zh_name = tuple(f"'{x}'" for x in it)
        print(f"    ( {lang_code_1:4}, {lang_code_3:5}, {nllb_code:10}, {lang_name+',':<40} {zh_name}),")
    # print(*lang_arr,sep=',\n    ', end=',\n')
    print(']')

# 结果是 dump_support_lang_map 复制过来的。这样看到这个文件，就知道支持哪些文本了
# support_lang_map[112] = [(nllb_code, lang_code_3, lang_code_1, lang_name, zh_name)]
support_lang_list:list[(str,str,str,str,str)] = [
    ( 'af', 'afr', 'afr_Latn', 'Afrikaans',                             '南非荷兰语 (南非)'),
    ( 'ak', 'aka', 'aka_Latn', 'Akan',                                  '阿肯语 (加纳)'),
    ( 'am', 'amh', 'amh_Ethi', 'Amharic',                               '阿姆哈拉语 (埃塞俄比亚)'),
    ( 'as', 'asm', 'asm_Beng', 'Assamese',                              '阿萨姆语 (印度)'),
    ( 'ba', 'bak', 'bak_Cyrl', 'Bashkir',                               '巴什基尔语 (俄罗斯)'),
    ( 'bm', 'bam', 'bam_Latn', 'Bambara',                               '班巴拉语 (马里)'),
    ( 'be', 'bel', 'bel_Cyrl', 'Belarusian',                            '白俄罗斯语 (白俄罗斯)'),
    ( 'bn', 'ben', 'ben_Beng', 'Bengali; Bangla',                       '孟加拉语 (孟加拉国)'),
    ( 'bo', 'bod', 'bod_Tibt', 'Tibetan; Tibetan Standard; Central',    '藏语 (中国)'),
    ( 'bs', 'bos', 'bos_Latn', 'Bosnian',                               '波斯尼亚语 (拉丁文, 波斯尼亚和黑塞哥维那)'),
    ( 'bg', 'bul', 'bul_Cyrl', 'Bulgarian',                             '保加利亚语 (保加利亚)'),
    ( 'ca', 'cat', 'cat_Latn', 'Catalan; Valencian',                    '加泰罗尼亚语 (西班牙)'),
    ( 'cs', 'ces', 'ces_Latn', 'Czech',                                 '捷克语 (捷克)'),
    ( 'cy', 'cym', 'cym_Latn', 'Welsh',                                 '威尔士语 (英国)'),
    ( 'da', 'dan', 'dan_Latn', 'Danish',                                '丹麦语 (丹麦)'),
    ( 'de', 'deu', 'deu_Latn', 'German',                                '德语 (德国)'),
    ( 'dz', 'dzo', 'dzo_Tibt', 'Dzongkha',                              '宗卡语 (不丹)'),
    ( 'el', 'ell', 'ell_Grek', 'Greek, Modern (1453-); Greek',          '希腊语 (希腊)'),
    ( 'en', 'eng', 'eng_Latn', 'English',                               '英语 (美国)'),
    ( 'eo', 'epo', 'epo_Latn', 'Esperanto',                             '世界语 (世界)'),
    ( 'et', 'est', 'est_Latn', 'Estonian',                              '爱沙尼亚语 (爱沙尼亚)'),
    ( 'eu', 'eus', 'eus_Latn', 'Basque',                                '巴斯克语 (西班牙)'),
    ( 'ee', 'ewe', 'ewe_Latn', 'Ewe',                                   '埃维语 (加纳)'),
    ( 'fo', 'fao', 'fao_Latn', 'Faroese',                               '法罗语 (法罗群岛)'),
    ( 'fi', 'fin', 'fin_Latn', 'Finnish',                               '芬兰语 (芬兰)'),
    ( 'fr', 'fra', 'fra_Latn', 'French',                                '法语 (法国)'),
    ( 'gd', 'gla', 'gla_Latn', 'Gaelic; Scottish Gaelic',               '苏格兰盖尔语 (英国)'),
    ( 'ga', 'gle', 'gle_Latn', 'Irish',                                 '爱尔兰语 (爱尔兰)'),
    ( 'gl', 'glg', 'glg_Latn', 'Galician',                              '加利西亚语 (西班牙)'),
    ( 'gn', 'grn', 'grn_Latn', 'Guarani; Guaraní',                      '瓜拉尼语 (巴拉圭)'),
    ( 'gu', 'guj', 'guj_Gujr', 'Gujarati',                              '古吉拉特语 (印度)'),
    ( 'ha', 'hau', 'hau_Latn', 'Hausa',                                 '豪萨语 (尼日利亚)'),
    ( 'he', 'heb', 'heb_Hebr', 'Hebrew',                                '希伯来语 (以色列)'),
    ( 'hi', 'hin', 'hin_Deva', 'Hindi',                                 '印地语 (印度)'),
    ( 'hr', 'hrv', 'hrv_Latn', 'Croatian',                              '克罗地亚语 (克罗地亚)'),
    ( 'hu', 'hun', 'hun_Latn', 'Hungarian',                             '匈牙利语 (匈牙利)'),
    ( 'hy', 'hye', 'hye_Armn', 'Armenian',                              '亚美尼亚语 (亚美尼亚)'),
    ( 'ig', 'ibo', 'ibo_Latn', 'Igbo',                                  '伊博语 (尼日利亚)'),
    ( 'id', 'ind', 'ind_Latn', 'Indonesian',                            '印度尼西亚语 (印度尼西亚)'),
    ( 'is', 'isl', 'isl_Latn', 'Icelandic',                             '冰岛语 (冰岛)'),
    ( 'it', 'ita', 'ita_Latn', 'Italian',                               '意大利语 (意大利)'),
    ( 'jv', 'jav', 'jav_Latn', 'Javanese',                              '爪哇语 (印度尼西亚)'),
    ( 'ja', 'jpn', 'jpn_Jpan', 'Japanese',                              '日语 (日本)'),
    ( 'kn', 'kan', 'kan_Knda', 'Kannada',                               '卡纳达语 (印度)'),
    ( 'ks', 'kas', 'kas_Arab', 'Kashmiri',                              '克什米尔语 (阿拉伯文, 印度)'),
    ( 'ks', 'kas', 'kas_Deva', 'Kashmiri',                              '克什米尔语 (阿拉伯文, 印度)'),
    ( 'ka', 'kat', 'kat_Geor', 'Georgian',                              '格鲁吉亚语 (格鲁吉亚)'),
    ( 'kk', 'kaz', 'kaz_Cyrl', 'Kazakh',                                '哈萨克语 (西里尔文, 哈萨克斯坦)'),
    ( 'km', 'khm', 'khm_Khmr', 'Central Khmer; Khmer',                  '高棉语 (柬埔寨)'),
    ( 'ki', 'kik', 'kik_Latn', 'Kikuyu; Gikuyu',                        '吉库尤语 (肯尼亚)'),
    ( 'rw', 'kin', 'kin_Latn', 'Kinyarwanda',                           '卢旺达语 (卢旺达)'),
    ( 'ky', 'kir', 'kir_Cyrl', 'Kirghiz; Kyrgyz',                       '柯尔克孜语 (吉尔吉斯斯坦)'),
    ( 'ko', 'kor', 'kor_Hang', 'Korean',                                '韩语 (韩国)'),
    ( 'lo', 'lao', 'lao_Laoo', 'Lao',                                   '老挝语 (老挝)'),
    ( 'ln', 'lin', 'lin_Latn', 'Lingala',                               '林加拉语 (刚果（金）)'),
    ( 'lt', 'lit', 'lit_Latn', 'Lithuanian',                            '立陶宛语 (立陶宛)'),
    ( 'lb', 'ltz', 'ltz_Latn', 'Luxembourgish; Letzeburgesch',          '卢森堡语 (卢森堡)'),
    ( 'lg', 'lug', 'lug_Latn', 'Ganda',                                 '卢干达语 (乌干达)'),
    ( 'ml', 'mal', 'mal_Mlym', 'Malayalam',                             '马拉雅拉姆语 (印度)'),
    ( 'mr', 'mar', 'mar_Deva', 'Marathi; Marāṭhī',                      '马拉地语 (印度)'),
    ( 'mk', 'mkd', 'mkd_Cyrl', 'Macedonian',                            '马其顿语 (北马其顿)'),
    ( 'mt', 'mlt', 'mlt_Latn', 'Maltese',                               '马耳他语 (马耳他)'),
    ( 'mi', 'mri', 'mri_Latn', 'Maori; Māori',                          '毛利语 (新西兰)'),
    ( 'my', 'mya', 'mya_Mymr', 'Burmese',                               '缅甸语 (缅甸)'),
    ( 'nl', 'nld', 'nld_Latn', 'Dutch; Flemish',                        '荷兰语 (荷兰)'),
    ( 'nn', 'nno', 'nno_Latn', 'Norwegian Nynorsk; Nynorsk, Norwegian', '挪威尼诺斯克语 (挪威)'),
    ( 'nb', 'nob', 'nob_Latn', 'Bokmål, Norwegian; Norwegian Bokmål',   '书面挪威语 (挪威)'),
    ( 'ny', 'nya', 'nya_Latn', 'Chichewa; Chewa; Nyanja',               '齐切瓦语 (马拉维)'),
    ( 'oc', 'oci', 'oci_Latn', 'Occitan (post 1500); Provençal',        '奥克语 (法国)'),
    ( 'pa', 'pan', 'pan_Guru', 'Panjabi; Punjabi',                      '旁遮普语 (果鲁穆奇文, 印度)'),
    ( 'pl', 'pol', 'pol_Latn', 'Polish',                                '波兰语 (波兰)'),
    ( 'pt', 'por', 'por_Latn', 'Portuguese',                            '葡萄牙语 (巴西)'),
    ( 'ro', 'ron', 'ron_Latn', 'Romanian; Moldavian; Moldovan',         '罗马尼亚语 (罗马尼亚)'),
    ( 'rn', 'run', 'run_Latn', 'Rundi; Kirundi',                        '隆迪语 (布隆迪)'),
    ( 'ru', 'rus', 'rus_Cyrl', 'Russian',                               '俄语 (俄罗斯)'),
    ( 'sg', 'sag', 'sag_Latn', 'Sango',                                 '桑戈语 (中非共和国)'),
    ( 'sa', 'san', 'san_Deva', 'Sanskrit; Saṁskṛta',                    '梵语 (印度)'),
    ( 'si', 'sin', 'sin_Sinh', 'Sinhala; Sinhalese',                    '僧伽罗语 (斯里兰卡)'),
    ( 'sk', 'slk', 'slk_Latn', 'Slovak',                                '斯洛伐克语 (斯洛伐克)'),
    ( 'sl', 'slv', 'slv_Latn', 'Slovenian; Slovene',                    '斯洛文尼亚语 (斯洛文尼亚)'),
    ( 'sn', 'sna', 'sna_Latn', 'Shona',                                 '绍纳语 (津巴布韦)'),
    ( 'sd', 'snd', 'snd_Arab', 'Sindhi',                                '信德语 (阿拉伯文, 巴基斯坦)'),
    ( 'so', 'som', 'som_Latn', 'Somali',                                '索马里语 (索马里)'),
    ( 'st', 'sot', 'sot_Latn', 'Sotho, Southern; Southern Sotho',       '南索托语 (南非)'),
    ( 'es', 'spa', 'spa_Latn', 'Spanish; Castilian',                    '西班牙语 (西班牙)'),
    ( 'sc', 'srd', 'srd_Latn', 'Sardinian',                             '萨丁语 (意大利)'),
    ( 'sr', 'srp', 'srp_Cyrl', 'Serbian',                               '塞尔维亚语 (西里尔文, 塞尔维亚)'),
    ( 'ss', 'ssw', 'ssw_Latn', 'Swati',                                 '斯瓦蒂语 (南非)'),
    ( 'su', 'sun', 'sun_Latn', 'Sundanese',                             '巽他语 (拉丁文, 印度尼西亚)'),
    ( 'sv', 'swe', 'swe_Latn', 'Swedish',                               '瑞典语 (瑞典)'),
    ( 'ta', 'tam', 'tam_Taml', 'Tamil',                                 '泰米尔语 (印度)'),
    ( 'tt', 'tat', 'tat_Cyrl', 'Tatar',                                 '鞑靼语 (俄罗斯)'),
    ( 'te', 'tel', 'tel_Telu', 'Telugu',                                '泰卢固语 (印度)'),
    ( 'tg', 'tgk', 'tgk_Cyrl', 'Tajik',                                 '塔吉克语 (塔吉克斯坦)'),
    ( 'tl', 'tgl', 'tgl_Latn', 'Tagalog',                               '菲律宾语 (菲律宾)'),
    ( 'th', 'tha', 'tha_Thai', 'Thai',                                  '泰语 (泰国)'),
    ( 'ti', 'tir', 'tir_Ethi', 'Tigrinya',                              '提格利尼亚语 (埃塞俄比亚)'),
    ( 'tn', 'tsn', 'tsn_Latn', 'Tswana',                                '茨瓦纳语 (南非)'),
    ( 'ts', 'tso', 'tso_Latn', 'Tsonga',                                '聪加语 (南非)'),
    ( 'tk', 'tuk', 'tuk_Latn', 'Turkmen',                               '土库曼语 (土库曼斯坦)'),
    ( 'tr', 'tur', 'tur_Latn', 'Turkish',                               '土耳其语 (土耳其)'),
    ( 'tw', 'twi', 'twi_Latn', 'Twi',                                   '阿肯语 (加纳)'),
    ( 'ug', 'uig', 'uig_Arab', 'Uighur; Uyghur',                        '维吾尔语 (中国)'),
    ( 'uk', 'ukr', 'ukr_Cyrl', 'Ukrainian',                             '乌克兰语 (乌克兰)'),
    ( 'ur', 'urd', 'urd_Arab', 'Urdu',                                  '乌尔都语 (巴基斯坦)'),
    ( 'vi', 'vie', 'vie_Latn', 'Vietnamese',                            '越南语 (越南)'),
    ( 'wo', 'wol', 'wol_Latn', 'Wolof',                                 '沃洛夫语 (塞内加尔)'),
    ( 'xh', 'xho', 'xho_Latn', 'Xhosa',                                 '科萨语 (南非)'),
    ( 'yo', 'yor', 'yor_Latn', 'Yoruba',                                '约鲁巴语 (尼日利亚)'),
    ( 'zh', 'zho', 'zho_Hans', 'Chinese',                               '中文 (简体, 中国)'),
    ( 'zh', 'zho', 'zho_Hant', 'Chinese',                               '中文 (简体, 中国)'),
    ( 'zu', 'zul', 'zul_Latn', 'Zulu',                                  '祖鲁语 (南非)'),
]

@dataclass
class SupportLangIndex:
    code_info_map: dict[str, int] = field(default_factory=dict)
    
    def _init(self):
        if len(self.code_info_map) > 0:
            return
        for idx, it in enumerate(support_lang_list):
            code_1 = it[0].lower()
            code_3 = it[1].lower()
            code_nllb = it[2].lower()
            self.code_info_map.setdefault(code_1, idx)
            self.code_info_map.setdefault(code_3, idx)
            self.code_info_map.setdefault(code_nllb, idx)
            # 简体，繁体特殊处理下
            if code_nllb == 'zho_hans':
                self.code_info_map.setdefault('zh-hans', idx)
                self.code_info_map.setdefault('zh-cn', idx)
            elif code_nllb == 'zho_hant':
                self.code_info_map.setdefault('zh-hant', idx)
                self.code_info_map.setdefault('zh-tw', idx)
                self.code_info_map.setdefault('zh-hk', idx)
                self.code_info_map.setdefault('zh-mo', idx)


    def get_info(self, lang_code:str) -> tuple[str,str,str,str,str]|None:
        self._init()
        lang_code = lang_code.lower()
        idx = self.code_info_map.get(lang_code)
        if idx is None:
            return None
        return support_lang_list[idx]

    def get_nllb_code(self, lang_code:str) -> str:
        info = self.get_info(lang_code)
        return info and info[2] or ''
    
    def get_language_name(self, lang_code:str, get_en = False) -> str:
        info = self.get_info(lang_code)
        if info:
            return get_en and info[3] or info[4]
        else:
            return ''

g_SupportLangIndex = SupportLangIndex()

def get_nllb_code(lang_code:str) -> str:
    """
    Get the NLLB code for the given ISO code.
    support iso_code_1, iso_code_3
    If not found, return an empty string.
    """
    return g_SupportLangIndex.get_nllb_code(lang_code)

def get_language_name(lang_code, get_en = False) -> str:
    """
    Get the language name for the given lang code.
    support iso_code_1, iso_code_3, nllb_code
    If not found, return an empty string.
    """
    return g_SupportLangIndex.get_language_name(lang_code)

if __name__ == '__main__':
    # dump_support_lang_map()

    print(get_language_name('zh'))
    print(get_nllb_code('zh'))
    print(get_language_name('en'))
    print(get_nllb_code('en'))
    print(get_language_name('kin'))
    print(get_nllb_code('kin'))