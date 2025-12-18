@echo off
REM ==================== ROBIGOO START SCRIPT (SEPARATE WINDOWS) ====================
REM This script starts both the backend server and frontend client in separate windows
REM Best for development - see both outputs clearly
REM ==============================================================

echo.
echo ==================== ROBIGOO STARTUP ====================
echo.

REM Check if we're in the correct directory
if not exist "Server" (
    echo ERROR: Server folder not found!
    echo Make sure you run this script from the Robigoo root directory
    pause
    exit /b 1
)

if not exist "Client" (
    echo ERROR: Client folder not found!
    echo Make sure you run this script from the Robigoo root directory
    pause
    exit /b 1
)

echo Starting backend server in new window...
echo.
start cmd /k "cd Server && dotnet run"

timeout /t 3 /nobreak

echo Starting frontend client in new window...
echo.
start cmd /k "cd Client && npm start"

echo.
echo ==================== STARTUP COMPLETE ====================
echo Backend Server: http://localhost:5235 (in first window)
echo Frontend Client: http://localhost:4200 (in second window)
echo ==========================================================
echo.
echo Close either window to stop that service.
echo.
pause
