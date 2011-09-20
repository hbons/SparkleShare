@echo off
set WinDirNet=%WinDir%\Microsoft.NET\Framework
set msbuild="%WinDirNet%\v3.5\msbuild.exe"
if not exist %msbuild% set msbuild="%WinDirNet%\v4.0.30319\msbuild.exe"
set wixBinDir=%WIX%\bin

rem %msbuild% /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" %~dp0\SparkleShare.sln

if "%1"=="installer" (
	if exist "%wixBinDir%" (
		"%wixBinDir%\heat.exe" dir "%git_install_root%." -cg msysGitComponentGroup -gg -scom -sreg -sfrag -srd -dr MSYSGIT_DIR -t addmedia.xlst -var wix.msysgitpath -o msysgit.wxs
		"%wixBinDir%\candle" "%~dp0\SparkleShare.wxs"
		"%wixBinDir%\candle" "msysgit.wxs
		"%wixBinDir%\light" -ext WixUIExtension Sparkleshare.wixobj msysgit.wixobj -dmsysgitpath=%git_install_root% -o SparkleShare.msi
		echo SparkleShare.msi created.

	) else (
		echo Not building installer ^(could not find wix, Windows Installer XML toolset^)
	    echo wix is available at http://wix.sourceforge.net/
	)
	
) else echo Not building installer, as it was not requested. ^(Issue "build.cmd installer" to build installer ^)
