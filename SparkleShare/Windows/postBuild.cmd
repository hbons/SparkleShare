@ECHO ON
setlocal enableDelayedExpansion
REM if no target directory is passed use default on
IF [%1]==[]  (SET OUTDIR="%~dp0..\..\bin\msysgit") ELSE (SET OUTDIR=""%~1)

IF EXIST %OUTDIR%\cmd GOTO skipgitdownload
ECHO installing git
REM download git
FOR /F "usebackq tokens=1" %%i IN ("%~dp0\git.download") DO SET url=%%i
FOR /F "usebackq tokens=2" %%i IN ("%~dp0\git.download") DO SET md5hash=%%i
CALL :downloadandverify %url% %~dp0\git.tar.gz %md5hash%
mkdir %OUTDIR%
REM check if tar is available else use tartool and unzip
IF EXIST C:\Windows\System32\TAR.exe (
	C:\Windows\System32\TAR -zxvf %~dp0\git.tar.gz -C %OUTDIR%
) ELSE (
	tartool %~dp0\git.tar.gz %OUTDIR%
)
DEL /s /q %~dp0\git.tar.gz

curl -L https://github.com/PowerShell/Win32-OpenSSH/releases/download/V8.6.0.0p1-Beta/OpenSSH-Win32.zip -o %~dp0\OpenSSH-Win32.zip
REM check if tar is available else use tartool and unzip
IF EXIST C:\Windows\System32\TAR.exe (
	C:\Windows\System32\TAR -zxvf %~dp0\OpenSSH-Win32.zip -C %~dp0
) ELSE (
	echo "Using linux / GitBash tar arguments"
  unzip %~dp0\OpenSSH-Win32.zip -d %~dp0
)
DEL /s /q %~dp0\OpenSSH-Win32.zip
XCOPY  %~dp0\OpenSSH-Win32\ssh-keygen.exe %OUTDIR%\usr\bin /Y
XCOPY  %~dp0\OpenSSH-Win32\ssh-keyscan.exe %OUTDIR%\usr\bin /Y
XCOPY  %~dp0\OpenSSH-Win32\ssh.exe %OUTDIR%\usr\bin /Y
XCOPY  %~dp0\OpenSSH-Win32\libcrypto.dll %OUTDIR%\usr\bin /Y
RMDIR /s /q %~dp0\OpenSSH-Win32

:skipgitdownload
SET ERRORLEVEL=0
ECHO ready

:ready
endlocal
EXIT /B %ERRORLEVEL%


:downloadandverify
REM first parameter url to download
REM second parameter local filename
REM third parameter md5string

curl -L %~1 -o %~2
set "SHA256="
for /f "skip=1 tokens=* delims=" %%# in ('certutil -hashfile %~2 SHA256') do (
	if not defined SHA256 (
		for %%Z in (%%#) do set "SHA256=!SHA256!%%Z"
	)
)
if "%SHA256%" == "%~3" (
echo
) else (
del %~2
goto :sha256error
)
goto:eof

:sha256error
ECHO sha256 sum of download wrong
SET ERRORLEVEL=2
goto ready
