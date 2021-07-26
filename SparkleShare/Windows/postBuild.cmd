ECHO ON
setlocal enableDelayedExpansion
REM if no target directory is passed use default on
IF [%1]==[]  (SET OUTDIR="%~dp0..\..\bin\msysgit") ELSE (SET OUTDIR=%~1)

IF EXIST %OUTDIR%\cmd GOTO skipgitdownload
ECHO installing git
REM download git
FOR /F "usebackq tokens=1" %%i IN ("%~dp0\git.download") DO SET url=%%i
FOR /F "usebackq tokens=2" %%i IN ("%~dp0\git.download") DO SET md5hash=%%i
CALL :downloadandverify %url% %~dp0\git.tar.gz %md5hash%
XCOPY  %~dp0\git.tar.gz %OUTDIR%\  /Y /i
CD %OUTDIR%
TAR -xf git.tar.gz
DEL /s /q git.tar.gz
CD %~dp0
PAUSE

:skipgitdownload

rem This simple check is for 32-bit Windows and for 64-bit Windows with batch
rem file executed in 64-bit environment by 64-bit Windows command processor.
set FolderSSH=%SystemRoot%\System32\OpenSSH
if exist %FolderSSH%\ssh-keygen.exe if exist %FolderSSH%\ssh-keyscan.exe (
  goto skipInstallOpenSSH
  echo Found ssh-keygen and ssh-keyscan in: "%FolderSSH%"
)

rem This check is for 64-bit Windows with batch file executed
rem in 32-bit environment by 32-bit Windows command processor.
if exist %SystemRoot%\Sysnative\cmd.exe set FolderSSH=%SystemRoot%\Sysnative\OpenSSH
if exist %FolderSSH%\ssh-keygen.exe if exist %FolderSSH%\ssh-keyscan.exe (
  goto skipInstallOpenSSH
  echo Found ssh-keygen and ssh-keyscan in: "%FolderSSH%"
)
echo ERROR: ssh-keygen.exe AND ssh-keyscan.exe not found.
ECHO installing openSSH
%~dp0\sudo.cmd %~dp0\installSSH
REM powershell -command "Add-Type -AssemblyName PresentationCore,PresentationFramework; [System.Windows.MessageBox]::Show('Sometimes windows has to be restarted to get use of ssh-keyscan and ssh-keygen. If it not works restart and the commands are available.','OpenSSH was installed','Ok','Warning')"
:skipInstallOpenSSH
SET ERRORLEVEL=
ECHO ready
endlocal
EXIT /B 0


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
goto skipInstallOpenSSH
