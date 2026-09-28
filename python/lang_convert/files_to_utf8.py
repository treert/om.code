#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""批量把源码文件转换为 UTF-8（无 BOM）。

用法:
    python files_to_utf8.py <目录>                # dry-run，只预览将要转换的文件
    python files_to_utf8.py <目录> --write        # 真正写入
    python files_to_utf8.py <目录> -e .txt .md    # 覆盖默认扩展名列表
    python files_to_utf8.py <目录> --exclude foo  # 额外排除目录
    python files_to_utf8.py <目录> -v             # 显示无需转换的文件

特性:
    - 默认 dry-run，加 --write 才写盘
    - 已是 UTF-8（无 BOM）的文件不动；带 BOM 的去掉 BOM
    - 解码失败/识别失败的文件跳过并报告，绝不丢字（不用 errors='ignore'）
    - 行尾风格（CRLF/LF）原样保留
    - 自动跳过 .git / node_modules 等目录

依赖: pip install charset-normalizer
"""
import argparse
import sys
from pathlib import Path

try:
    from charset_normalizer import from_bytes
except ImportError:
    print("[error] 缺少依赖 charset-normalizer，请先执行: pip install charset-normalizer",
          file=sys.stderr)
    sys.exit(1)

DEFAULT_EXTS = [
    ".c", ".cpp", ".h", ".hpp",
    ".cs",
    ".go",
    # ".ss",
    ".xml",
    ".lua",
    # ".csd",
    ".py",
]

DEFAULT_EXCLUDES = {
    ".git", ".hg", ".svn",
    "__pycache__",
    "node_modules",
    ".idea", ".vscode",
    "build", "dist", "target",
    "venv", ".venv",
}

UTF8_BOM = b"\xef\xbb\xbf"
UTF32_BOMS = (b"\xff\xfe\x00\x00", b"\x00\x00\xfe\xff")
UTF16_BOMS = (b"\xff\xfe", b"\xfe\xff")


def decode_file(data):
    """识别编码并完整解码，返回 (text, encoding)。

    识别失败或解码失败时抛 ValueError，调用方跳过该文件。
    """
    # 1. 显式 BOM 的直接按对应编码处理
    if data.startswith(UTF8_BOM):
        return data[len(UTF8_BOM):].decode("utf-8"), "utf-8-sig"
    if data.startswith(UTF32_BOMS):
        return data.decode("utf-32"), "utf-32"
    if data.startswith(UTF16_BOMS):
        return data.decode("utf-16"), "utf-16"

    # 2. 能严格按 UTF-8 解码的就已经是 UTF-8，无需猜测
    try:
        return data.decode("utf-8"), "utf-8"
    except UnicodeDecodeError:
        pass

    # 3. 其余交给 charset-normalizer 猜测，并对每个候选做可信度校验。
    #    不直接采信 best()：短中文内容下 GBK/Big5/UTF-16 等都能"成功"
    #    解码但结果是乱码，需要打分挑出真正可信的那个。
    candidates = [m.encoding for m in list(from_bytes(data))[:5]]
    if "gb18030" not in candidates:
        candidates.append("gb18030")
    best = None  # (han_score, text, enc)，平分时先到先得
    for enc in candidates:
        try:
            text = data.decode(enc)
        except (UnicodeDecodeError, LookupError):
            continue
        if not _looks_reasonable(data, text):
            continue
        score = _han_score(text)
        if best is None or score > best[0]:
            best = (score, text, enc)
    if best is None:
        raise ValueError("无法识别编码（候选: %s 均不可信）"
                         % ", ".join(candidates or ["无"]))
    return best[1], best[2]


def _looks_reasonable(data, text):
    """校验解码结果是否可信。"""
    # 1. 不应出现 NUL 和异常控制字符
    for ch in text:
        if ch == "\x00" or (ch < " " and ch not in "\t\n\r\x0b\x0c"):
            return False
    # 2. ASCII 占比不应大幅缩水：GBK 源码里 ASCII 字节占大头，
    #    若被误判成 UTF-16，解码后 ASCII 会成对消失变成生僻字。
    if not data:
        return True
    ascii_in = sum(1 for b in data if 0x20 <= b < 0x7F) / len(data)
    ascii_out = (sum(1 for ch in text if 0x20 <= ord(ch) < 0x7F)
                 / max(len(text), 1))
    return ascii_out >= ascii_in * 0.8 or ascii_out > 0.7


# 常用汉字表（按语频取前 150 左右）。正确解码的中文内容必然大量命中，
# 而错误编码解出的乱码几乎都是生僻字，以此在多个候选中选优。
_COMMON_HAN = set(
    "的一是了我不人在他有这个上们来到时大地为子中你说生国年着就那和要她出也"
    "得里后自以会家可下而过天去能对小多然于心学么之都好看起发当没成只如事把"
    "还用第样道想作种开美总从无情己面最女但现前些所同日手又行意动方期它头经"
    "长儿回位分爱老因很给名法间斯知世什两次使身者被高已亲其进此话常与活正感"
    "到写代码文件函数参数变量编译运行测试项目错误定义类型结构返回注释内容"
)


def _han_score(text):
    """统计解码结果命中的常用汉字数。"""
    return sum(1 for ch in text if ch in _COMMON_HAN)


def iter_files(root, exts, excludes):
    for p in root.rglob("*"):
        if not p.is_file():
            continue
        rel_parts = p.relative_to(root).parts
        if any(part in excludes for part in rel_parts[:-1]):
            continue
        if p.suffix.lower() in exts:
            yield p


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="批量把源码文件转换为 UTF-8（无 BOM）。默认 dry-run，只预览。")
    parser.add_argument("root", type=Path, help="要处理的根目录")
    parser.add_argument("-w", "--write", action="store_true",
                        help="真正写入文件（默认只预览）")
    parser.add_argument("-e", "--ext", nargs="+", metavar="EXT",
                        help="覆盖默认扩展名列表，如: -e .txt .md")
    parser.add_argument("--exclude", nargs="+", metavar="DIR", default=[],
                        help="额外要排除的目录名")
    parser.add_argument("-v", "--verbose", action="store_true",
                        help="同时显示无需转换的文件")
    args = parser.parse_args(argv)

    root = args.root
    if not root.is_dir():
        print("[error] 目录不存在: %s" % root, file=sys.stderr)
        return 1

    if args.ext:
        exts = {e.lower() if e.startswith(".") else "." + e.lower()
                for e in args.ext}
    else:
        exts = set(DEFAULT_EXTS)
    excludes = DEFAULT_EXCLUDES | set(args.exclude)

    print("[%s] root: %s  exts: %s" % (
        "write" if args.write else "dry-run", root, " ".join(sorted(exts))))

    to_convert = skipped = errors = 0
    for p in iter_files(root, exts, excludes):
        try:
            data = p.read_bytes()
            text, src = decode_file(data)
        except (ValueError, OSError) as e:
            print("[error] %s: %s" % (p, e))
            errors += 1
            continue

        new_data = text.encode("utf-8")
        if new_data == data:
            if args.verbose:
                print("[skip]   已是 UTF-8: %s" % p)
            skipped += 1
            continue

        if args.write:
            p.write_bytes(new_data)
            print("[ok]   %-8s -> UTF-8  %s" % (src, p))
        else:
            print("[dry]  %-8s -> UTF-8  %s" % (src, p))
        to_convert += 1

    action = "已转换" if args.write else "待转换(加 --write 生效)"
    print("finish: %d 个%s, %d 个无需转换, %d 个失败"
          % (to_convert, action, skipped, errors))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
