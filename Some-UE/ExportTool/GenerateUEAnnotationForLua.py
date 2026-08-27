#!/usr/bin/python

from __future__ import annotations
import os
import re
import stat
import string
import sys
import time
from turtle import st
from typing import List
import yaml as Yaml

WorkDir = os.path.abspath(os.path.join(os.path.dirname(__file__), '../'))

lua_enums:list[LuaEnum] = []
lua_classes:list[LuaClass] = []
lua_structs:list[LuaClass] = []

class NameInfo:
	def __init__(self, fullname:str):
		self.fullname = fullname
		segs = fullname.split(" ")
		self.type = segs[0]
		fullname = segs[1]
		fullname = fullname[fullname.rindex("/")+1:]
		segs = fullname.split(".")

		self.package = segs[0]
		self.name = segs[1]

def gen_tip_comment(tip:str,content:str,line_tail:str = "@", indent:str="") -> str:
	tip = tip.strip()
	tiplines:list[str] = []
	if len(tip) > 0:
		tiplines = tip.split("\n")
	ls = []
	if len(tiplines) == 1:
		ls.append(f"{indent}{content} {line_tail} {tiplines[0]}")
	else:
		for line in tiplines:
			ls.append(f"--- {line}")
		ls.append(f"{indent}{content}")
	return "\n".join(ls)

class LuaEnum:
	def __init__(self, yaml):
		self.fields:dict[str, (int,str)] = {}
		fullname:str = yaml["FullName"]
		self.nameinfo = NameInfo(fullname)
		EnumValues = yaml["EnumValues"]
		if EnumValues is None:
			EnumValues = []

		for item in EnumValues:
			tip = ""
			name = ""
			value = 0
			for k,v in item.items():
				if k == 'tip':
					tip = v
				else:
					name = k.split(".")[-1]
					value = v
			self.fields[name] = (value, tip)

	def get_lua_comment(self):
		ls  = []
		ls.append(f"---@region {self.nameinfo.fullname}")
		ls.append(f"---@class {self.nameinfo.name}:integer")
		ls.append(f"UE4.{self.nameinfo.name} = {{")
		for k,v in self.fields.items():
			ls.append(gen_tip_comment(v[1],f"{k} = {v[0]},", "--", "  "))
		ls.append(f"}}")
		ls.append(f"---@endregion {self.nameinfo.fullname}")
		return "\n".join(ls)

key_words = {"in", "end"}

def get_cpptype(cpptype:str):
	cpptype = cpptype.replace("*", "").replace(" ","")
	cpptype = re.sub(R'TWeakObjectPtr<(\w+)>', R"\1", cpptype)
	# if cpptype.startswith("TWeakObjectPtr<"):
	# 	cpptype = cpptype.removeprefix("TWeakObjectPtr<")[:-1]
	return cpptype

class LuaClass:
	class Field:
		def __init__(self, yaml, cls:LuaClass):
			self.cls = cls
			self.name = yaml["longname"].split(".")[-1]
			self.cpptype = yaml["cpptype"]
			self.dim = yaml.get("dim", 1)
			self.tip = yaml.get("tip", "")

		def get_lua_commnet(self):
			arrsuffix = "[]" if self.dim > 1 else ""
			cpptype:str = get_cpptype(self.cpptype)
			line = gen_tip_comment(self.tip, f"---@field {self.name} {cpptype}{arrsuffix}")
			return line


	class Function:
		class Param:
			def __init__(self, yaml, cls:LuaClass, is_return:bool = False):
				self.cls = cls
				self.is_return = is_return
				self.name = yaml["name"]
				# 如果是关键字，则加个下划线
				if self.name in key_words:
					self.name += "_"
				self.cpptype = yaml["cpptype"]
				self.dim = yaml.get("dim", 1)
				self.tip = yaml.get("tip", "")
				self.isOutParam:bool = yaml.get("isOutParam", False)

			def get_lua_comment(self, for_return:bool = False):
				# TMap TArray TSet 不做处理
				typename = self.cpptype.replace("*", "")
				name = "" if for_return else self.name
				head = "return" if for_return else "param "
				line = gen_tip_comment(self.tip, f"---@{head}{name} {typename}")
				return line

		def __init__(self, yaml, cls:LuaClass):
			self.params:list[LuaClass.Function.Param] = []
			self.return_type:LuaClass.Function.Param = None

			self.cls = cls
			self.name = yaml["longname"].split(".")[-1]
			self.is_static = yaml.get("is_static", 0) == 1
			self.tip = yaml.get("tip", "")
			
			# 特殊处理下
			segs:list[str] = self.tip.split("\n")
			tips:dict[str,str] = {}
			return_tip:str = ""
			idx = len(segs) - 1
			while idx >= 0:
				seg = segs[idx]
				if m := re.match(R"@param\s+(\w+)\s+(.*)",seg):
					tips[m.group(1)] = m.group(2)
					segs.pop(idx)
				elif m := re.match(R"@return\s+(.*)",seg):
					return_tip = m.group(1)
					segs.pop(idx)
				else:
					break
				idx -= 1
			
			self.tip = "\n".join(segs).strip()

			for it in yaml["Params"] or []:
				param = LuaClass.Function.Param(it, cls, False)
				param.tip = tips.get(param.name, "")
				self.params.append(param)
			rets = yaml["Returns"] or []
			if len(rets) > 0:
				self.return_type = LuaClass.Function.Param(rets[0], cls, True)
				self.return_type.tip = return_tip

		def get_lua_comment(self):
			ls = []
			if len(self.tip) > 0:
				ls.append("--[==[")
				ls.append(self.tip)
				ls.append("]==]")
			params = []
			for p in self.params:
				params.append(p.name)
				ls.append(p.get_lua_comment())
			if self.return_type is not None:
				ls.append(self.return_type.get_lua_comment(True))
			for p in self.params:
				if p.isOutParam:
					ls.append(p.get_lua_comment(True))
			connect = "." if self.is_static else ":" 
			ls.append(f"function {self.cls.cppname}{connect}{self.name}({','.join(params)}) end")
			return "\n".join(ls)


	def __init__(self, yaml):
		self.nameinfo:NameInfo = None
		self.cppname:str = ""
		self.parent:str = ""
		self.tip:str = ""
		self.fields:dict[str,LuaClass.Field] = {}
		self.funcs:dict[str,LuaClass.Function] = {}
		self.nameinfo = NameInfo(yaml["FullName"])
		self.cppname = yaml["cppname"]
		self.tip = yaml.get("tip", "")
		self.parent = yaml.get("Parent", "")
		for item in yaml["Fields"] or []:
			f = LuaClass.Field(item,self)
			self.fields[f.name] = f
		for item in yaml["Functions"] or []:
			f = LuaClass.Function(item,self)
			self.funcs[f.name] = f
		pass

	def get_lua_comment(self):
		ls = []
		ls.append(f"---@region {self.nameinfo.fullname}")
		if len(self.tip) > 0:
			ls.append("--[==[")
			ls.append(self.tip)
			ls.append("]==]")
		parent = f": {self.parent}" if len(self.parent) > 0 else ""
		ls.append(f"---@class {self.cppname}{parent}")
		for f in self.fields.values():
			ls.append(f.get_lua_commnet())
		ls.append(f"{self.cppname} = {{}}")
		ls.append(f"---@type {self.cppname}")
		ls.append(f"UE4.{self.cppname} = nil")
		for f in self.funcs.values():
			ls.append("")
			ls.append(f.get_lua_comment())
		ls.append("")
		ls.append(f"---@endregion {self.nameinfo.fullname}")
		
		return "\n".join(ls)

class TimeWatch:
	def __init__(self):
		self.start = time.time()
		self.last = self.start
		self.end = 0

	def stop(self):
		self.end = time.time()

	def get_one_cost(self):
		last = self.last
		self.last = time.time()
		return self.last - last

	def get_total(self):
		return time.time() - self.start

def main():
	watcher = TimeWatch()
	# global WorkDir
	ue_info_path = os.path.join(WorkDir, R"Misc\ue-class-info.yaml")
	ue_info_file = open(ue_info_path, "r", encoding="utf-8")
	ue_info = Yaml.load(ue_info_file, Loader=Yaml.CLoader)
	print(f"load ue-class-info.yaml use {watcher.get_one_cost():.2f}s")

	enum_infos = ue_info["UEnums"]
	for info in enum_infos:
		lua_enums.append(LuaEnum(info))
	print(f"parse UEnums use {watcher.get_one_cost():.2f}s")
	
	for info in ue_info["UStructs"]:
		lua_structs.append(LuaClass(info))
	print(f"parse UStructs use {watcher.get_one_cost():.2f}s")

	for info in ue_info["UClasses"]:
		lua_classes.append(LuaClass(info))
	print(f"parse UClasses use {watcher.get_one_cost():.2f}s")

	moudles:dict[str,list[LuaClass]] = {}
	for it in lua_structs + lua_classes:
		moudles.setdefault(it.nameinfo.package, []).append(it)
	
	# 写入文件
	lua_comment_dir = os.path.join(WorkDir, R"LuaComment\auto_gen\ue-lua-comment")
	os.makedirs(lua_comment_dir, exist_ok=True)
	watcher2 = TimeWatch()
	for moudle, classes in moudles.items():
		path = os.path.join(lua_comment_dir, f"{moudle}-annotation.lua")
		with open(path, "w", encoding="utf-8") as lua_file:
			lua_file.write("------ autogenerated by GenerateUEAnnotationForLua.py")
			for cls in classes:
				lua_file.write("\n\n\n")
				lua_file.write(cls.get_lua_comment())
				# print(f"one class {cls.nameinfo.package}.{cls.cppname} done, cost {watcher2.get_one_cost():.2f}s")
		# print(f"{moudle} done, cost {watcher.get_one_cost():.2f}s")

	# export_enum_names = {
	# 	'EWorldMapSoldierGroupActionType',
	# 	'EWorldMapFormationState',
	# 	'EInstancedSkeletalEnum',
	# 	'EOrientation',
	# 	'EOrientationType',
	# 	'EStretch',
	# 	'ECivCraftGameMode',
	# 	'ELimitCharacterMode',
	# 	'EStretchDirection',
	# 	'EWorldMapSoldierActionType',
	# 	'EWorldFollowUIContainerType',
    #     'EWorldInfiniteLevel',
    #     'EWorldFollowHiddenType',
    #     'EWorldVisibilityLayer',
	# }
	export_enum_strs = ["------ autogenerated by GenerateUEAnnotationForLua.py"]
	normal_enum_strs = ["------ autogenerated by GenerateUEAnnotationForLua.py"]

	export_enum_names_config = []
	configPath = os.path.join(WorkDir, R"ExportTool\ExportConfig.txt")
	with open(configPath, 'r', encoding='utf-8') as file:
		for line in file:
			export_enum_names_config.insert(0, line.strip())

	def WriteLinesToFile(path, lines):
		path = os.path.join(WorkDir, path)
		if os.path.exists(path):
			os.chmod(path, stat.S_IWRITE)
		with open(path, 'w', encoding='utf-8') as file:
			for line in lines:
				file.write(line)
		pass

	for it in lua_enums:
		if it.nameinfo.name in export_enum_names_config:
			export_enum_strs.append("\n\n\n")
			export_enum_strs.append(it.get_lua_comment())
		else:
			normal_enum_strs.append("\n\n\n")
			normal_enum_strs.append(it.get_lua_comment())

	# WriteLinesToFile(R"Civ\Content\Lua\Generated\AutoGenEnum.lua", export_enum_strs)
	WriteLinesToFile(R"LuaComment\ue-lua-enum.lua", normal_enum_strs)
	# print(f"enum done, cost {watcher.get_one_cost():.2f}s")

	print(f"Total cost {watcher.get_total():.2f}s")

if __name__ == '__main__':
	main()