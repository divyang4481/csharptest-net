@ECHO OFF

if not exist ..\keys\csharptest.net.snk goto NOKEY

if exist bin\* @rd /s /q bin
MD bin
XCOPY /D /R /Y Depend\* .\bin

CSBuild.exe
goto EXIT

:NOKEY

ECHO.
ECHO You must create your own .snk key file before continuing run the following 
ECHO command from a visual studio command-prompt:
ECHO     sn.exe -k %~dp0..\keys\csharptest.net.snk
ECHO.

:EXIT