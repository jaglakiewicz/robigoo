@echo off
start "Backend" cmd /k "cd /d %~dp0Server && dotnet run"
start "Frontend" cmd /k "cd /d %~dp0Client && npx ng serve"
