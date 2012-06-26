@echo off

call %~dp0\..\..\data\plugins\build.cmd

set WinDirNet=%WinDir%\Microsoft.NET\Framework
set msbuild="%WinDirNet%\v4.0\msbuild.exe"
if not exist %msbuild% set msbuild="%WinDirNet%\v4.0.30319\msbuild.exe"
set wixBinDir=%WIX%\bin


if not exist ..\..\bin mkdir ..\..\bin
copy Pixmaps\sparkleshare-app.ico ..\..\bin\

%msbuild% /t:Rebuild /p:Configuration=Release /p:Platform="AnyCPU"   "%~dp0\tools\gettext-cs-utils\Gettext.CsUtils\Core\Gettext.Cs\Gettext.Cs.csproj"
%msbuild% /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" "%~dp0\SparkleShare.sln"

if "%1"=="installer" (
	if exist "%wixBinDir%" (
	  if exist "%~dp0\SparkleShare.msi" del "%~dp0\SparkleShare.msi"
		"%wixBinDir%\heat.exe" dir "%~dp0\..\..\bin\msysgit" -cg msysGitComponentGroup -gg -scom -sreg -sfrag -srd -dr MSYSGIT_DIR -var wix.msysgitpath -o msysgit.wxs
		"%wixBinDir%\heat.exe" dir "%~dp0\..\..\bin\plugins" -cg pluginsComponentGroup -gg -scom -sreg -sfrag -srd -dr PLUGINS_DIR -var wix.pluginsdir -o plugins.wxs
		"%wixBinDir%\candle" "%~dp0\SparkleShare.wxs" -ext WixUIExtension -ext WixUtilExtension
		"%wixBinDir%\candle" "%~dp0\msysgit.wxs" -ext WixUIExtension -ext WixUtilExtension
		"%wixBinDir%\candle" "%~dp0\plugins.wxs" -ext WixUIExtension -ext WixUtilExtension
		"%wixBinDir%\light" -ext WixUIExtension -ext WixUtilExtension Sparkleshare.wixobj msysgit.wixobj plugins.wixobj -droot="%~dp0\..\.." -dmsysgitpath="%~dp0\..\..\bin\msysgit" -dpluginsdir="%~dp0\..\..\bin\plugins"  -o SparkleShare.msi 
		if exist "%~dp0\SparkleShare.msi" echo SparkleShare.msi created.
	) else (
		echo Not building installer ^(could not find wix, Windows Installer XML toolset^)
	  echo wix is available at http://wix.sourceforge.net/
	)
) else echo Not building installer, as it was not requested. ^(Issue "build.cmd installer" to build installer ^)
