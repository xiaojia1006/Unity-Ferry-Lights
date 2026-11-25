@echo off
chcp 936 >nul
cd /d "%~dp0"
set PORT=18765

netstat -ano | findstr ":%PORT%" | findstr "LISTENING" >nul
if not errorlevel 1 goto running

where python >nul 2>nul
if not errorlevel 1 goto runpy

where py >nul 2>nul
if not errorlevel 1 goto runpy3

echo [错误] 没找到 Python。请先安装 Python 3，或把 python.exe 加入 PATH。
echo.
pause
exit /b 1

:runpy
echo [启动] python auth_server.py --port %PORT%
python auth_server.py --port %PORT%
goto ended

:runpy3
echo [启动] py -3 auth_server.py --port %PORT%
py -3 auth_server.py --port %PORT%
goto ended

:running
echo [提示] 端口 %PORT% 已被占用，服务可能已经在运行，无需重复启动。
echo.
pause
exit /b 0

:ended
echo.
echo [退出] 服务已停止，日志见 service.log
pause
