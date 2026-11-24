@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo Starting Willowreach account service on 127.0.0.1:18765 ...
python auth_server.py --port 18765
echo.
echo Service stopped. Press any key to close.
pause >nul
