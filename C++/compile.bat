@echo off
setlocal
set RAYLIB_DIR=C:\raylib\raylib\src
set W64DEVKIT=C:\raylib\w64devkit\bin
set PATH=%W64DEVKIT%;%PATH%

REM Usage 1: Drag a single .c / .cpp file onto this bat
REM Usage 2: compile.bat main.c [file2.c ...]
REM Usage 3: compile.bat folder\     (compiles all .c / .cpp in that folder)

set FILENAME=%~f1
set EXT=%~x1

if "%FILENAME%"=="" (
    echo Usage: Drag a .c or .cpp file onto compile.bat
    echo    Or: compile.bat main.c [file2.c file3.cpp ...]
    pause
    exit /b
)

REM Detect compiler based on file extension
set COMPILER=gcc
set STD=c99
if /I "%EXT%"==".cpp" (
    set COMPILER=g++
    set STD=c++17
)

REM Build source file list
set SOURCES=%FILENAME%
set OUTPUT_DIR=%~dp1
set OUTPUT_NAME=%~n1

REM If more arguments, include them too
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
    -s -O2 -std=%STD% -Wall ^
    -I"%RAYLIB_DIR%" -L"%RAYLIB_DIR%" ^
    -lraylib -lopengl32 -lgdi32 -lwinmm -mwindows

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build OK. Starting...
    "%OUTPUT_DIR%%OUTPUT_NAME%.exe"
) else (
    echo.
    echo Build FAILED. Check errors above.
    pause
)
