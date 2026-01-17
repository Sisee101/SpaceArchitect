@echo off
echo 正在打开 Player.log 文件...
start notepad "%USERPROFILE%\AppData\LocalLow\DefaultCompany\SpaceArchitect\Player.log"
echo.
echo 如果文件不存在，请先运行 Build 后的 exe 游戏，然后关闭游戏，再运行此脚本。
pause

