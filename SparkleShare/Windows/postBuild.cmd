@ECHO ON
setlocal enableDelayedExpansion
REM if no target directory is passed use default on
IF [%1]==[]  (SET "OUTDIR=%~dp0bin\git_scm") ELSE (SET OUTDIR="%~1")

IF EXIST %OUTDIR% GOTO skipgitdownload
ECHO installing git
REM download git
FOR /F "usebackq tokens=1" %%i IN ("%~dp0git.download") DO SET url=%%i
FOR /F "usebackq tokens=2" %%i IN ("%~dp0git.download") DO SET md5hash=%%i
CALL :downloadandverify %url% "%~dp0PortableGit.7z.exe" %md5hash%
"%~dp0PortableGit.7z.exe" -o %OUTDIR% -y
DEL "%~dp0PortableGit.7z.exe"
DEL /s /q "%~dp0OpenSSH-Win32.zip"

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

curl -L %~1 -o "%~2"
set "SHA256="
for /f "skip=1 tokens=* delims=" %%# in ('certutil -hashfile "%~2" SHA256') do (
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
