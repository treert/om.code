@echo off
:: 获取脚本所在的真实目录（即使通过快捷方式或其他路径调用）
set "script_dir=%~dp0"

:: 检查是否以管理员身份运行
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo 请求管理员权限...
    :: 重新以管理员身份运行当前脚本（并保持正确的工作目录）
    powershell -Command "Start-Process cmd -ArgumentList '/c cd /d \"%script_dir%\" && \"%script_dir%%~nx0\"' -Verb RunAs"
    exit /b
)

:: 检查源文件是否存在
@REM if not exist "%script_dir%test.as" (
@REM     echo 错误：脚本目录下未找到 test.as 文件！
@REM     echo 脚本目录: "%script_dir%"
@REM     pause
@REM     exit /b
@REM )

:: 目标目录
set "target_dir=C:\Program Files\DAUM\PotPlayer\Extension\Subtitle\Translate"

:: 如果目标目录不存在，则创建
if not exist "%target_dir%" (
    mkdir "%target_dir%"
)

:: 复制文件（强制覆盖）
xcopy /y "%script_dir%SubtitleTranslate - MyTranslate.as" "%target_dir%\"
xcopy /y "%script_dir%SubtitleTranslate - MyTranslate.ico" "%target_dir%\"

echo 文件已成功复制
pause