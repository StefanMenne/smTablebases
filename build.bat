@echo off
setlocal enabledelayedexpansion

call clean.bat

set SLN_PATH=smTablebases\smTablebases.sln
set APP_PROJ=smTablebases\smTablebases\smTablebases.csproj
set OUTPUT_BASE=.\Releases

echo === Extract application version ===
for /f "tokens=*" %%i in ('powershell -NoProfile -Command "$v = ([xml](Get-Content %APP_PROJ%)).Project.PropertyGroup.Version; if ($v -match '^\d+\.\d+') { $matches[0] } else { $v }"') do set APP_VERSION=%%i

if "%APP_VERSION%"=="" (
    echo Warning: Version could not be found. Timestamp as fallback.
    set DIST_NAME=Build_%date:~6,4%%date:~3,2%%date:~0,2%
) else (
    set DIST_NAME=smTablebases_v%APP_VERSION%
)

set DIST_DIR=%OUTPUT_BASE%\%DIST_NAME%

if exist "%DIST_DIR%\debug" (
    echo Folder %DIST_DIR% already exists.
    pause
    exit /b
)


mkdir "%DIST_DIR%\debug"
mkdir "%DIST_DIR%\release"
mkdir "%DIST_DIR%\smTablebases_win_x64"
mkdir "%DIST_DIR%\smTablebases_linux_x64"
::mkdir "%DIST_DIR%\final_aot"


:: 1. DEBUG Build
echo [1/4] Build DEBUG...
dotnet build %SLN_PATH% -c Debug --property WarningLevel=0
if %errorlevel% neq 0 goto error
xcopy "smTablebases\smTablebases\bin\Debug\net10.0\*.*" "%DIST_DIR%\debug\" /Y /S >nul

:: 2. Build RELEASE
echo [2/4] Baue RELEASE...
dotnet build %SLN_PATH% -c Release --property WarningLevel=0
if %errorlevel% neq 0 goto error
xcopy "smTablebases\smTablebases\bin\Release\net10.0\*.*" "%DIST_DIR%\release\" /Y /S >nul

echo [3/4] Build RELEASE FINAL TRIMMED (Windows x64)...
dotnet publish %APP_PROJ% -c Release -r win-x64 --property WarningLevel=0 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DefineConstants="RELEASE%%3BRELEASEFINAL" -o "%DIST_DIR%\smTablebases_win_x64"
if %errorlevel% neq 0 goto error

echo [4/4] Build RELEASE FINAL TRIMMED (Linux x64)...
dotnet publish %APP_PROJ% -c Release -r linux-x64 --property WarningLevel=0 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DefineConstants="RELEASE%%3BRELEASEFINAL" -o "%DIST_DIR%\smTablebases_linux_x64"
if %errorlevel% neq 0 goto error

:: Build RELEASE FINAL TRIMMED (macOS x64)...
::dotnet publish %APP_PROJ% -c Release -r osx-x64 --property WarningLevel=0 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DefineConstants="RELEASE%3BRELEASEFINAL" -o "%DIST_DIR%\final_osx_x64"
::if %errorlevel% neq 0 goto error

::echo Build RELEASE FINAL TRIMMED (macOS ARM64)...
::dotnet publish %APP_PROJ% -c Release -r osx-arm64 --property WarningLevel=0 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DefineConstants="RELEASE%3BRELEASEFINAL" -o "%DIST_DIR%\final_osx_arm64"
::if %errorlevel% neq 0 goto error

::echo  FINAL AOT (Windows x64)...
::dotnet publish %APP_PROJ% -c Release -r win-x64 -p:PublishAot=true --property WarningLevel=0 -p:DefineConstants="RELEASE%%3BRELEASEFINAL" -o "%DIST_DIR%\final_aot"
::if %errorlevel% neq 0 goto error



echo.
echo === BUILD SUCCESS ===
echo Release-Ordner: %DIST_DIR%

goto end

:error
echo.
echo !!! ERROR !!!
pause

:end
:: Source Code
mkdir tmp
robocopy "smTablebases" "tmp" /mir /fft /XD "bin" "obj" ".vs" ".idea"
cd tmp
::..\Tools\7za a -r "..\%DIST_DIR%\source.zip" *.*
tar -a -c -f "..\%DIST_DIR%\source.zip" *.*
cd ..
rmdir /S /q tmp

pause