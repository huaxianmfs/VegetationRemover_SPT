@echo off
setlocal
cd /d "%~dp0"
dotnet build VegetationRemover.csproj -c Release
if errorlevel 1 (
    echo BUILD FAILED.
    pause
    exit /b 1
)
echo BUILD SUCCEEDED.
pause