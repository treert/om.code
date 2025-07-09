#!/usr/bin/env python3
# -*- coding: utf-8 -*-
from langconv import *
import sys

print("sys.maxunicode", sys.maxunicode)
print(sys.version)
print(sys.version_info)
print(sys.getdefaultencoding())

# 转换繁体到简体
def cht_to_chs(line):
    line = Converter('zh-hans').convert(line)
    line.encode('utf-8')
    return line

# 转换简体到繁体
def chs_to_cht(line):
    line = Converter('zh-hant').convert(line)
    line.encode('utf-8')
    return line

line_chs='<>123asdasd把中文字符串进行繁体和简体中文的转换'
line_cht='<>123asdasd把中文字符串進行繁體和簡體中文的轉換'

ret_chs = cht_to_chs(line_cht)
ret_cht = chs_to_cht(line_chs)

print("chs='%s'" % ret_chs)
print("cht='%s'" % ret_cht)

# with open('ret.txt', 'w', encoding='utf-8') as file:
#     file.write(f"{ret_chs}\n{ret_cht}\n")

a = "我是𪚥"
print(type(a))
print(type("我是𪚥"))