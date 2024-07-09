REM Description: This batch file runs the accoreconsole.exe with the lmv.scr script and House.dwg file in the same directory as the batch file.
REM Written by: Madhukar Moogala
REM Date: 04/07/2024

@echo off
setlocal

REM Define the registry path and key
set "regPath=HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\AutoCAD\R25.0\ACAD-8100\Install"
set "regKey=INSTALLDIR"
set "fp=%~dp0"
set "DIR_NAME=%fp%ToDelete"
set "CLB_PATH=%fp%House.collaboration"
if exist "%DIR_NAME%" (
    rmdir /s /q "%DIR_NAME%"
)
mkdir "%DIR_NAME%"
echo Isolate directory "%DIR_NAME%" has been recreated.

if exist "%CLB_PATH%" (
    del "%CLB_PATH%"
    echo File "%CLB_PATH%" has been deleted.
)
REM Define the folder path
set "folderPath=%fp%output"
REM Check if the folder exists
if exist "%folderPath%" (
    REM If it exists, delete all contents
    echo Deleting all contents of %folderPath%
    rmdir /s /q "%folderPath%"
)

REM Query the registry for the key value
for /f "tokens=3*" %%A in ('reg query "%regPath%" /v %regKey%') do (
    set "installDir=%%A %%B"
)
set "accoreConsolePath=%installDir%\accoreconsole.exe"
echo The path to accoreconsole.exe is: %accoreConsolePath%
set "scr=%fp%lmv.scr"
set "dwg=%fp%House.dwg"
set "bundle=%fp%\Bundle"

echo Running accoreconsole.exe with lmv.scr script and House.dwg file...
endlocal & call "%accoreConsolePath%" /i "%dwg%" /al "%bundle%" /s "%scr%" /isolate user1 "%DIR_NAME%"
pause

REM End of batch file
