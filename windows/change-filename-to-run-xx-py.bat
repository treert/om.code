@echo off
:: try run xx.py in console. change xx to the name of py file.

if exist "%~dp0%~n0.py" (
	python "%~dp0%~n0.py" %*
	exit /b %errorlevel%
) else if exist "%~dp0%~n0" (
	python "%~dp0%~n0" %*
	exit /b %errorlevel%
) else (
	echo Error: %~dp0%~n0.py not found
	exit /b 1
)
