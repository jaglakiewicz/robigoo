@echo off
REM ==================== ROBIGOO START SCRIPT (SINGLE WINDOW) ====================
REM This script starts the backend server in background and frontend client in main window
REM Both run simultaneously - server output hidden, client in main terminal
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

echo Starting backend server (running in background)...
REM Start server in background without new window
start /b cmd /c "cd Server && dotnet run > server.log 2>&1"

echo Waiting for backend to initialize...
timeout /t 3 /nobreak

echo Starting frontend client (running in this window)...
echo.
echo ==================== ROBIGOO RUNNING ====================
echo Backend Server: http://localhost:5235 (running in background)
echo Frontend Client: http://localhost:4200 (in this window)
echo Server output logged to: Server\server.log
echo Press Ctrl+C to stop client. Server will continue running.
echo ==========================================================
echo.

REM Start client in main window
cd Client
npm start

REM Cleanup when npm start exits
echo.
echo Stopping backend server...
taskkill /F /IM dotnet.exe 2>nul
echo Done!
