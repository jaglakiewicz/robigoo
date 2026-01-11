@echo off
echo Building Robigoo Launcher...
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Bin

echo.
echo Build complete! RobigooLauncher.exe is in Launcher\Bin folder.
echo.
echo Copying to root folder...
copy /Y Bin\RobigooLauncher.exe ..\RobigooLauncher.exe

echo Done!
pause
