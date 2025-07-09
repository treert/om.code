#!/usr/bin/env python3
# -*- coding: utf-8 -*-
from langconv import *
import sys
import os
import chardet
import codecs

# print("sys.maxunicode", sys.maxunicode)
# print(sys.version)
# print(sys.version_info)

# Convert traditional to simplified Chinese, line is unicode
def cht_to_chs(line):
    line = Converter('zh-hans').convert(line)
    line.encode('utf-8')
    return line

# Convert simplified to traditional Chinese, line is unicode
def chs_to_cht(line):
    line = Converter('zh-hant').convert(line)
    line.encode('utf-8')
    return line


convertfiletypes = [
  ".xml",
  ".lua",
  ".csd"
  ]


def isHidenFile(filePath):
    if filePath.count('.') > 1:
        return True
    return False

def check_need_convert(filename):
    if isHidenFile(filename):
        return False
    for filetype in convertfiletypes:
        if filename.lower().endswith(filetype):
            return True
    return False

total_cnt = 0
success_cnt = 0
unkown_cnt = 0
def convert_file_to_traditional_chinese(filename):
    global total_cnt,success_cnt,unkown_cnt
    # Backup the origin file.

    # convert file from the source encoding to target encoding
    content = codecs.open(filename, 'r').readbytes()
    source_encoding = chardet.detect(content)['encoding']
    total_cnt += 1
    if source_encoding is None:
        print("??", filename)
        unkown_cnt += 1
        return
    print("  ", source_encoding, filename)
    content = content.decode(source_encoding, 'ignore')
    content = chs_to_cht(content)
    codecs.open(filename, 'w', encoding="utf-8").write(content)
    success_cnt += 1

def convert_dir(root_dir):
    if not os.path.exists(root_dir):
        print("[error] dir:", root_dir, "do not exit")
        return
    if os.path.isfile(root_dir):
        convert_file_to_traditional_chinese(root_dir)
        return
    print("work in", convertdir)
    for root, dirs, files in os.walk(root_dir):
        for f in files:
            filename = os.path.join(root, f)
            if check_need_convert(filename):
                try:
                    convert_file_to_traditional_chinese(filename)
                except Exception as e:
                    print("WA", filename, e)
    print("finish total:", total_cnt, "success:", success_cnt, "unkown_cnt", unkown_cnt)

if __name__ == '__main__':
    if len(sys.argv) == 1:
        input("[error] need root dir")
        sys.exit(-1)
    convertdir = sys.argv[1]
    convert_dir(convertdir)
