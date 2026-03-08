@echo off
setlocal enabledelayedexpansion

call clean.bat

set "APP_PROJ=smTablebases\smTablebases\smTablebases.csproj"
set "OUTPUT_BASE=.\Releases"
set "DOCKER_FILE=linux-aot.Dockerfile"
set "IMAGE_NAME=smtablebases-aot-builder"

echo === Prüfe Docker Status ===
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo [FEHLER] Docker läuft nicht! Bitte starte Docker Desktop.
    pause
    exit /b
)

echo === Extrahiere App-Version ===
for /f "tokens=*" %%i in ('powershell -NoProfile -Command "$v = ([xml](Get-Content %APP_PROJ%)).Project.PropertyGroup.Version; if ($v -match '^\d+\.\d+') { $matches[0] } else { $v }"') do set APP_VERSION=%%i

if "%APP_VERSION%"=="" (
    set DIST_NAME=Build_LinuxAOT_%date:~6,4%%date:~3,2%%date:~0,2%
) else (
    set DIST_NAME=smTablebases_v%APP_VERSION%
)

:: Zielpfad: Prj\smTablebases\Releases\...
set "TARGET_DIR=%OUTPUT_BASE%\%DIST_NAME%\final_aot_linux"

if exist "%TARGET_DIR%" (
    echo Ordner %TARGET_DIR% existiert bereits.
    pause
    exit /b
)

mkdir "%TARGET_DIR%"

echo.
echo [1/3] Erstelle Docker-Image...
docker build -t %IMAGE_NAME% -f %DOCKER_FILE% .
if %errorlevel% neq 0 goto error

echo.
echo [2/3] Extrahiere Build-Dateien aus Container...
docker rm -f temp-aot-build >nul 2>&1
docker create --name temp-aot-build %IMAGE_NAME%
if %errorlevel% neq 0 goto error

:: Kopiert die Dateien in den Releases-Ordner auf deiner Ebene
docker cp temp-aot-build:/out/. "%TARGET_DIR%"
if %errorlevel% neq 0 goto error

echo.
echo [3/3] Aufräumen...
docker rm temp-aot-build
rd /s /q DIST_NAME

echo.
echo === LINUX AOT BUILD ERFOLGREICH ===
echo Pfad: %TARGET_DIR%
goto end

:error
echo.
echo !!! FEHLER BEIM BUILD !!!
pause

:end
pause