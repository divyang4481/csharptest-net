@ECHO OFF
ECHO CSharpTest.Net Build 1.0
ECHO Copyright 2009 by Roger Knapp, Licensed under the Apache License, Version 2.0
ECHO.

REM ------------------------------------------------------------------------------
REM  I Know, I Know, this is a cheesy build but it gets the job done and doesn't
REM  require any additional dependencies beyond the .Net framework install.
REM  
REM  If you are using the GZipViewer for viewing log output you will need to run
REM  GZipViewer.exe -register from the command line for proper registration with
REM  the windows explorer.
REM ------------------------------------------------------------------------------

SET MSBUILD=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\MSBuild.exe
if not exist %MSBUILD% goto no_msbuild20
SET MSBUILD=%MSBUILD% /p:MSBuildToolsPath=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\

goto continue

:no_msbuild20
SET MSBUILD=%WINDIR%\Microsoft.NET\Framework\v3.5\MSBuild.exe
if not exist %MSBUILD% goto no_msbuild35

goto continue

:no_msbuild35
ECHO Could not locate MSBuild.exe
goto FAIL

:continue

if exist bin\* @rd /s /q bin
MD bin
XCOPY /D /R /Y Depend\* .\bin

%MSBUILD% /nologo /t:Build /v:Minimal Log\Logging.csproj
%MSBUILD% /nologo /t:Build /v:Minimal Log\Test\Logging.Test.csproj
%MSBUILD% /nologo /t:Build /v:Minimal Log\GZipViewer\GZipViewer.csproj
%MSBUILD% /nologo /t:Build /v:Minimal Shared\Shared.Test.csproj

goto exit

:FAIL
FAIL

:exit