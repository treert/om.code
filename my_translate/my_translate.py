"""
翻译语言的工具
1. 使用 lingua 检测语言。【最早想使用 fasttext 来着的，非常垃圾，还不如langdetect 】
2. 使用 ctranslate 调用 nllb-200 模型来翻译。

以来安装：
pip install lingua-language-detector
pip install ctranslate2 sentencepiece
"""

import math
from pathlib import Path
import sys
import ctranslate2
import sentencepiece as spm
from lingua import IsoCode639_3, LanguageDetectorBuilder

import my_lang_utils

# 只支持常用语言，其他的按需增加
languages = [
    # 中日韩
    IsoCode639_3.ZHO, IsoCode639_3.JPN, IsoCode639_3.KOR,
    # 英语
    IsoCode639_3.ENG,
]

# detector = LanguageDetectorBuilder.from_iso_codes_639_3(*languages).build()
detector = LanguageDetectorBuilder.from_all_spoken_languages().build()

def detect_land_code(text: str) -> str:
    """ 检测语言，返回用于翻译的编码。现在是返回nllb-200的编码，失败返回 '' """
    lang = detector.detect_language_of(text)
    if lang is None:
        return ''
    return my_lang_utils.get_nllb_code(lang.iso_code_639_3.name)


sp_model_path = Path(__file__).parent / "sp-models/sentencepiece.bpe.model"
sp_model_path = sp_model_path.resolve().__str__()
# Load the source SentecePiece model
sp = spm.SentencePieceProcessor()
sp.load(sp_model_path)

# ct_model_path = Path(__file__).parent / "ct-models/nllb-200-1.3B-ct2"
ct_model_path = Path(__file__).parent / "ct-models/nllb-200-distilled-1.3B-ct2"
# ct_model_path = Path(__file__).parent / "ct-models/nllb-200-3.3B-ct2"
ct_model_path = ct_model_path.resolve().__str__()
device = "cuda"  # or "cpu"
# 初始化（首次加载约 10-20 秒）
translator = ctranslate2.Translator(ct_model_path, device=device)
# 增大束搜索宽度可能提高分数
beam_size = 4

def translate_line(text: str, tgt_lang: str = 'zho_Hans', src_lang = 'auto', score_limit = -1) -> str:
    """ 
        翻译文本，返回翻译后的文本。如果翻译不成功，返回原文。
        score_limit is log P(translate | origin), 值小于0，越接近0，置信度越高。默认值解决 log(0.37)
    """
    text = text.strip()
    if src_lang.lower() == 'auto':
        src_code = detect_land_code(text)
    else:
        src_code = my_lang_utils.get_nllb_code(src_lang)
    tgt_code = my_lang_utils.get_nllb_code(tgt_lang)

    if src_code == '' or tgt_code == '':
        return text         # 不支持，原样返回
    if src_code == tgt_code:
        return text         # 没必要翻译
    
    pieces = [src_code] + sp.EncodeAsPieces(text) + ["</s>"]
    results = translator.translate_batch([pieces],
                        batch_type="tokens",
                        max_batch_size=2024,
                        beam_size=beam_size, 
                        target_prefix=[[tgt_code]],
                        return_scores = True,
                        replace_unknowns = True,
                        )
    result_first = results[0]
    result_pieces = result_first.hypotheses[0]
    result_score = result_first.scores[0]
    if result_score <= score_limit:
        result_text = text      # 置信度太低了，原样返回
    else:
        if tgt_code in result_pieces:
            result_pieces.remove(tgt_code)
        if "</s>" in result_pieces:
            result_pieces.remove("</s>")
        if src_code in result_pieces:
            result_pieces.remove(src_code)
        result_text = sp.DecodePieces(result_pieces)

    return result_text

if __name__ == "__main__":
    text = "Hello, world!"
    print(translate_line(text))
    for line in sys.stdin:
        print(translate_line(line))