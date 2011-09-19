@echo off
set WinDirNet=%WinDir%\Microsoft.NET\Framework
set msbuild="%WinDirNet%\v3.5\msbuild.exe"
if not exist %msbuild% set msbuild="%WinDirNet%\v4.0.30319\msbuild.exe"
set wixBinDir=%WIX%\bin

%msbuild% /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" %~dp0\SparkleShare.sln

if "%1"=="installer" (
	if exist "%wixBinDir%" (
		"%wixBinDir%\candle" "%~dp0\SparkleShare.wxs"
		"%wixBinDir%\light" -ext WixUIExtension Sparkleshare.wixobj
		echo SparkleShare.msi created.

	) else (
		echo Not building installer ^(could not find wix, Windows Installer XML toolset^)
	    echo wix is available at http://wix.sourceforge.net/
	)
	
) else echo Not building installer, as it was not requested. ^(Issue "build.cmd installer" to build installer ^)
