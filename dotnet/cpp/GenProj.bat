@echo off
set batdir=%~dp0
cd /d "%batdir%"

if not exist build ( md build )
@echo on
pushd build
cmake -DCMAKE_INSTALL_PREFIX=%batdir%output ..
popd
pause