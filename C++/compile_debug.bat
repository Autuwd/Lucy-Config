@echo off
setlocal
set RAYLIB_DIR=C:\raylib\raylib\src
set W64DEVKIT=C:\raylib\w64devkit\bin
set PATH=%W64DEVKIT%;%PATH%

REM Debug build with console window for error output
set FILENAME=%~f1
set EXT=%~x1

if "%FILENAME%"=="" (
    echo Usage: Drag a .c or .cpp file onto compile_debug.bat
    echo    Or: compile_debug.bat main.c [file2.c ...]
    pause
    exit /b
)

set COMPILER=gcc
set STD=c99
if /I "%EXT%"==".cpp" (
    set COMPILER=g++
    set STD=c++17
)

set SOURCES=%FILENAME%
set OUTPUT_DIR=%~dp1
set OUTPUT_NAME=%~n1

:loop
shift
if "%~1"=="" goto endloop
set SOURCES=%SOURCES% "%~f1"
goto loop
:endloop

echo Compiler: %COMPILER%  (Standard: %STD%)
echo Sources: %SOURCES%
echo Output: %OUTPUT_DIR%%OUTPUT_NAME%.exe
echo.

"%W64DEVKIT%\%COMPILER%" -o "%OUTPUT_DIR%%OUTPUT_NAME%.exe" %SOURCES% "%RAYLIB_DIR%\raylib.rc.data" ^
    -g -O0 -std=%STD% -Wall -Wno-missing-braces ^
    -I"%RAYLIB_DIR%" -L"%RAYLIB_DIR%" ^
    -lraylib -lopengl32 -lgdi32 -lwinmm

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build OK. Starting...
    "%OUTPUT_DIR%%OUTPUT_NAME%.exe"
) else (
    echo.
    echo Build FAILED. Check errors above.
    pause
)
