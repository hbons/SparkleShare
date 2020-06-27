ECHO ON
REM if no target directory is passed use default on
IF [%1]==[]  (SET OUTDIR="%~dp0\..\..\bin\msysgit") ELSE (SET OUTDIR=%~1)

IF EXIST %OUTDIR%\cmd GOTO skipgitdownload
REM download helper tool
ECHO installing git
CALL :downloadandverify https://github.com/senthilrajasek/tartool/releases/download/1.0.0/TarTool.zip %~dp0\tartool.zip 5ed0d78cb4d83dd0e124015a30ea69f8e5101c62c8c29a9f448f208147b59c04
powershell -command "Expand-Archive -Force -Path %~dp0\tartool.zip -DestinationPath %~dp0/tools"

REM download git
FOR /F "usebackq tokens=1" %%i IN ("%~dp0\git.download") DO SET url=%%i
FOR /F "usebackq tokens=2" %%i IN ("%~dp0\git.download") DO SET md5hash=%%i
CALL :downloadandverify %url% %~dp0\git.tar.gz %md5hash%
%~dp0\tools\tartool %~dp0\git.tar.gz %OUTDIR%

DEL /s /q %~dp0\tartool.zip
DEL /s /q %~dp0\git.tar.gz
DEL /s /q %~dp0\tools
RMDIR %~dp0\tools

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
EXIT /B 0


:downloadandverify
REM first parameter url to download
REM second parameter local filename
REM third parameter md5string

curl -L %~1 -o %~2
for /f "usebackq" %%i in (`%~dp0\sha256.cmd %~2`) do set sha256=%%i
if "%sha256%" == "%~3" (echo) else (goto :sha256error)
goto:eof

:sha256error
ECHO sha256 sum of download wrong
SET ERRORLEVEL=2
goto skipopenSSH
