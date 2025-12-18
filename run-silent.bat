@echo off
REM ==================== ROBIGOO START SCRIPT (NO PAUSE) ====================
REM This script starts both the backend server and frontend client
REM Closes automatically after starting - use run.bat for interactive mode
REM ==============================================================

REM Check if we're in the correct directory
if not exist "Server" (
    echo ERROR: Server folder not found!
    pause
    exit /b 1
)

if not exist "Client" (
    echo ERROR: Client folder not found!
    pause
    exit /b 1
)

echo Starting Robigoo backend and frontend...

start cmd /k "cd Server && dotnet run"
timeout /t 2 /nobreak
start cmd /k "cd Client && npm start"
