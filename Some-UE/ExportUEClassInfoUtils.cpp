#include "Misc/ExportUEClassInfoUtils.h"

#include <string>

#include <UObject/Class.h>
#include <Editor.h>
#include <Misc/FileHelper.h>
#include <HAL/IConsoleManager.h>
#include <Dom/JsonObject.h>
#include <Framework/Notifications/NotificationManager.h>
#include <Misc/Paths.h>
#include <HAL/PlatformFilemanager.h>
#include <Widgets/Notifications/SNotificationList.h>
#include <UObject/UObjectHash.h>

static int32 bOnlyExportOneBigFile = 1;
static FAutoConsoleVariableRef CVarIsOnlyExportOneBigFile(
	TEXT("om.ue.export.one.file"),
	bOnlyExportOneBigFile,
	HELP_TEXT("Onlu Export On Big File"),
	ECVF_Default);

namespace
{
	struct FYaml
	{
		FYaml(TArray<FString>* InContent, int InLevel = 0)
			:Content(InContent),CurLevel(InLevel){}

		FString Format2Str(const FString&InLine)
		{
			FString Line = InLine.TrimStartAndEnd();
			// int Pos = Line.FindLastCharByPredicate([](TCHAR C)
			// {
			// 	return C == '[' || C == ']' || C == ':' || C == '-' || C == '\'' || C == '"';
			// });
			// if(Pos == INDEX_NONE && !(
			// 	Line.StartsWith(">")
			// 	|| Line.StartsWith("!")
			// 	|| Line.StartsWith("*")))
			// {
			// 	return Line;
			// }
			// else
			{
				FString NewLine = Line.Replace(TEXT("'"),TEXT("''"));
				return FString::Printf(TEXT("'%s'"), *NewLine);
			}
		}

		void AddLine()
		{
			Content->Add("");
		}
		void AddLine(int level, const FString&Line)
		{
			Content->Add(FString::ChrN(level, ' ') + Line);
		}

		void AddComment(const FString&Value)
		{
			AddLine(CurLevel, TEXT("# ") + Value);
		}

		void AddMultiText(const FString&InText)
		{
			TArray<FString> Lines;
			InText.ParseIntoArrayLines(Lines, false);
			for(auto line: Lines)
			{
				if (Lines.Num() < 3)
				{
					line = line.TrimStartAndEnd();
				}
				AddLine(CurLevel + 2, line);
			}
		}

		void AddKeyValue(const FString&Key, int32 Value)
		{
			AddLine(CurLevel, Key + TEXT(" : ") + FString::FormatAsNumber(Value));
		}
		
		void AddKeyValue(const FString&Key, const FString&Value)
		{
			if(Value.Contains("\n"))
			{
				AddLine(CurLevel, Key + " : |-");
				AddMultiText(Value);
			}
			else
			{
				AddLine(CurLevel, Key + TEXT(" : ") + Format2Str(Value));
			}
		}

		void AddValue(const FString&Value)
		{
			AddLine(CurLevel, Value);
		}
		
		FYaml AddChild(const FString&Key)
		{
			AddLine(CurLevel, Key + TEXT(" :"));
			return FYaml(Content, CurLevel+2);
		}
		
		void AddArrayLine(const FString&Value)
		{
			if(Value.Contains("\n"))
			{
				AddLine(CurLevel, "- |-");
				AddMultiText(Value);
			}
			else
			{
				AddLine(CurLevel, TEXT("- ") + Format2Str(Value));
			}
		}

		FYaml AddArrayChild()
		{
			AddLine(CurLevel, TEXT("-"));
			return FYaml(Content, CurLevel+2);
		}

		// 能省一行
		FYaml AddArrayChild(const FString&Key, const FString&Value)
		{
			AddLine(CurLevel, TEXT("- ") + Key + " : " + Format2Str(Value));
			return FYaml(Content, CurLevel+2);
		}

		FYaml AddArrayChild(const FString&Key, int32 Value)
		{
			AddLine(CurLevel, TEXT("- ") + Key + " : " + FString::FromInt(Value));
			return FYaml(Content, CurLevel+2);
		}
		
		void ExprotToFile(const FString&FilePath)
		{
			FString Path = FPaths::ConvertRelativePathToFull(FilePath);
			if(FPaths::FileExists(Path))
			{
				FPlatformFileManager::Get().GetPlatformFile().SetReadOnly(*Path, false);
			}
			FFileHelper::SaveStringArrayToFile(*Content, *Path, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM);
		}

		TArray<FString>* Content;
		int CurLevel;
	};

	FString GetCppType4Property(const FProperty* Property)
	{
		FString ExtendedTypeText;
		FString CppType = Property->GetCPPType(&ExtendedTypeText);
		return CppType + ExtendedTypeText;
	}

	void Gen4Property(FYaml& Yaml, const FProperty* Property)
	{
		Yaml.AddKeyValue("cpptype", GetCppType4Property(Property));
		Yaml.AddKeyValue("dim", Property->ArrayDim);
		Yaml.AddKeyValue("class.name", Property->GetClass()->GetName());
		FString tip = Property->GetToolTipText().ToString();
		Yaml.AddKeyValue("tip",tip);
	}

	void Gen4Function(FYaml& Yaml, const UFunction*Func)
	{
		bool is_static = Func->HasAllFunctionFlags(FUNC_Static);
		Yaml.AddKeyValue("is_static", is_static);
		Yaml.AddKeyValue("tip", Func->GetToolTipText().ToString());
		FYaml YamlParams = Yaml.AddChild("Params");
		for(FProperty* Prop : TFieldRange<FProperty>(Func))
		{
			EPropertyFlags propflag = Prop->PropertyFlags;
			if((propflag & CPF_ReturnParm) == 0)
			{
				FYaml ch = YamlParams.AddArrayChild("name", Prop->GetName());
				Gen4Property(ch, Prop);
				// Out Params
				bool isOutParam = ((propflag&CPF_OutParm) && !(propflag&CPF_ReturnParm) && !(propflag&CPF_ConstParm) && !(propflag&CPF_BlueprintReadOnly));
				ch.AddKeyValue("isOutParam",isOutParam);
			}
		}
		FYaml rets = Yaml.AddChild("Returns");
		{
			auto* Prop = Func->GetReturnProperty();
			if(Prop)
			{
				FYaml ch = rets.AddArrayChild("name", Prop->GetName());
				Gen4Property(ch, Prop);
			}
		}
	}

	void Gen4Struct(FYaml& Yaml, const UStruct* Cls)
	{
		Yaml.AddKeyValue("cppname", Cls->GetPrefixCPP()+Cls->GetName());
		Yaml.AddKeyValue("tip", Cls->GetToolTipText().ToString());
		{
			TArray<FString> Parents;
			UStruct* super = Cls->GetSuperStruct();
			while(super)
			{
				Parents.Add(super->GetPrefixCPP()+super->GetName());
				super = super->GetSuperStruct();
			}
			if(Parents.Num() > 0)
			{
				Yaml.AddKeyValue("Parent", Parents[0]);
				Yaml.AddKeyValue("Ancestors", FString::Join(Parents, TEXT(":")));
			}
		}
		
		FString ClsName = Cls->GetName();
		FYaml props = Yaml.AddChild("Fields");
		for (TFieldIterator<FProperty> It(Cls,
			EFieldIteratorFlags::ExcludeSuper,
			EFieldIteratorFlags::ExcludeDeprecated); It; ++It)
		{
			const FProperty* Property = *It;
			FYaml ch = props.AddArrayChild("longname", ClsName + "." + Property->GetName());
			Gen4Property(ch, Property);
		}

		FYaml funcs = Yaml.AddChild("Functions");
		for (TFieldIterator<UFunction> It(Cls,
			EFieldIteratorFlags::ExcludeSuper,
			EFieldIteratorFlags::ExcludeDeprecated); It; ++It)
		{
			UFunction* func = *It;
			FYaml ch = funcs.AddArrayChild("longname", ClsName + "." + func->GetName());
			Gen4Function(ch, func);
		}
	}

	void ExportOneFile(TArray<UObject*>& Enums, TArray<UObject*>& Structs, TArray<UObject*>& Classes, const FString&Path)
	{
		TArray<FString> Content;
		FYaml Yaml(&Content);
		Yaml.AddComment(TEXT("auto export UCLASS UENUM USTRUCT..."));

		{
			auto func = [](const UObject* It){ return It->IsNative() && !It->IsEditorOnly(); };
			Enums = Enums.FilterByPredicate(func);
			Structs = Structs.FilterByPredicate(func);
			Classes = Classes.FilterByPredicate(func);
			if(Enums.Num() + Structs.Num() + Classes.Num() == 0)
			{
				return;
			}
		}

		// UEnum
		{
			TArray<UObject*> &Arr = Enums;
			Yaml.AddKeyValue("UEnumCount",Arr.Num());
			FYaml Yaml4Enums = Yaml.AddChild("UEnums");
			for (const auto&It : Arr)
			{
				const auto* Cls = Cast<UEnum>(It);
				FYaml Yamlch = Yaml4Enums.AddArrayChild("FullName", Cls->GetFullName());
				FString ClsName = Cls->GetName();
				FYaml YamlEnumValues = Yamlch.AddChild("EnumValues");
				int NumEnums = Cls->NumEnums() - 1;// 剔除掉最后的 Max
				for(int i = 0; i < NumEnums; i++)
				{
					FString name = Cls->GetNameStringByIndex(i);
					int value = Cls->GetValueByIndex(i);
					FYaml ch = YamlEnumValues.AddArrayChild(ClsName+"."+name, value);
					FString tip = Cls->GetToolTipTextByIndex(i).ToString().TrimStartAndEnd();
					if(tip.Len() > 0)
					{
						ch.AddKeyValue("tip",tip);
					}
					// FString display = Cls->GetDisplayNameTextByIndex(i).ToString().TrimStartAndEnd();
					// if(display.Len() > 0)
					// {
					// 	ch.AddKeyValue("display", display);
					// }
				}
				Yamlch.AddLine();
			}
			Yaml.AddLine();
		}

		// UStruct
		{
			TArray<UObject*> &Arr = Structs;
			Yaml.AddKeyValue("UStructCount",Arr.Num());
			FYaml YamlStructs = Yaml.AddChild("UStructs");
			for (const auto&It : Arr)
			{
				const auto* Cls = Cast<UScriptStruct>(It);
				FString ClsName = Cls->GetName();
				FYaml Yamlch = YamlStructs.AddArrayChild("FullName", Cls->GetFullName());
				Gen4Struct(Yamlch, Cls);
			}
			Content.Add("");
		}

		// UClass
		{
			TArray<UObject*> &Arr = Classes;
			Yaml.AddKeyValue("UClassCount",Arr.Num());
			FYaml YamlClasses = Yaml.AddChild("UClasses");
			for (const auto&It : Arr)
			{
				const UClass* Cls = Cast<UClass>(It);
				FYaml Yamlch = YamlClasses.AddArrayChild("FullName", Cls->GetFullName());
				Gen4Struct(Yamlch, Cls);
			}
			Content.Add("");
		}

		Yaml.ExprotToFile(Path);
	}
	
	/**
	 * @brief 遍历导出UClass直接的信息
	 */
	void ExportUEClassInfo()
	{
		if (!bOnlyExportOneBigFile){
			TArray<UObject*> Fields;
			GetObjectsOfClass(UField::StaticClass(), Fields);
			TMap<FString,TArray<UObject*>> Map;
			for(auto*field : Fields)
			{
				FString key = field->GetOuter()->GetPathName().Replace(TEXT("/"),TEXT("-"));
				Map.FindOrAdd(key).Add(field);
			}
			FString Dir = FPaths::Combine(FPaths::ProjectDir(), TEXT("UEAnnotation/ue.class.info"));
			IFileManager::Get().DeleteDirectory(*Dir,false,true);
			for(auto&it : Map)
			{
				FString Path = FPaths::Combine(Dir, it.Key + ".yaml");
				TArray<UObject*> Enums;
				TArray<UObject*> Structs;
				TArray<UObject*> Classes;
				for(auto *f : it.Value)
				{
					if(UScriptStruct* Struct = Cast<UScriptStruct>(f))
					{
						Structs.Add(Struct);
					}
					else if(UEnum* Enum = Cast<UEnum>(f))
					{
						Enums.Add(Enum);
					}
					else if(UClass* Class = Cast<UClass>(f))
					{
						Classes.Add(Class);
					}
				}
				if(Enums.Num() + Structs.Num() + Classes.Num() > 0)
				{
					ExportOneFile(Enums, Structs, Classes, Path);
				}
			}
		}
		
		{
			// UEnum
			TArray<UObject*> Enums;
			GetObjectsOfClass(UEnum::StaticClass(), Enums);
		
			// UStruct
			TArray<UObject*> Structs;
			GetObjectsOfClass(UScriptStruct::StaticClass(), Structs);
		
			// UClass
			TArray<UObject*> Classes;
			GetObjectsOfClass(UClass::StaticClass(), Classes);
		
			ExportOneFile(Enums, Structs, Classes, FPaths::Combine(FPaths::ProjectDir(), TEXT("UEAnnotation/Misc/ue-class-info.yaml")));
		}
	}
}

void FExportUEClassInfoUtils::Export()
{
	FDateTime StartTime = FDateTime::Now();
	ExportUEClassInfo();
	FText Txt = FText::FromString(TEXT("Export Finished! Start Run Py To Gen Comment."));
	FNotificationInfo Info(Txt);
	Info.ExpireDuration = Info.ExpireDuration + (FDateTime::Now() - StartTime).GetTotalSeconds();
	auto notify = FSlateNotificationManager::Get().AddNotification( Info );
	
	FString PyFile = FPaths::Combine(FPaths::ProjectDir(),TEXT("UEAnnotation/ExportTool/GenerateUEAnnotationForLua.py"));
	PyFile = FPaths::ConvertRelativePathToFull(PyFile);
	FPlatformMisc::OsExecute(nullptr, TEXT("py"), *PyFile);
}
