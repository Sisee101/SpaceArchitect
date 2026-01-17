@echo off
chcp 65001 >nul
echo ========================================
echo 正在查找 GlobalOverviewUI 日志文件...
echo ========================================
echo.

REM 方法1：尝试默认路径
set "LOG_PATH=%USERPROFILE%\AppData\LocalLow\DefaultCompany\SpaceArchitect\GlobalOverviewUI_Log.txt"

if exist "%LOG_PATH%" (
    echo [找到] 日志文件: %LOG_PATH%
    echo.
    start notepad "%LOG_PATH%"
    goto :end
)

REM 方法2：搜索整个 LocalLow 目录
echo 在默认路径未找到，正在搜索整个 LocalLow 目录...
echo.

for /r "%USERPROFILE%\AppData\LocalLow" %%f in (GlobalOverviewUI_Log.txt) do (
    if exist "%%f" (
        echo [找到] 日志文件: %%f
        echo.
        start notepad "%%f"
        goto :end
    )
)

REM 如果都没找到
echo [未找到] 日志文件不存在
echo.
echo 已搜索的路径：
echo   %LOG_PATH%
echo.
echo 可能的原因：
echo   1. 还没有运行 Build 后的 exe 游戏
echo   2. GlobalOverviewUI 脚本的 logToFile 设置为 false
echo   3. 日志文件在其他位置
echo.
echo 请先运行游戏，然后关闭游戏，再运行此脚本。
echo.
pause

:end

