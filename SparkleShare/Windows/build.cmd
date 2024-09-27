@echo on

set WinDirNet=%WinDir%\Microsoft.NET\Framework
set msbuild="%WinDirNet%\v4.0\msbuild.exe"
if not exist %msbuild% set msbuild="%WinDirNet%\v4.0.30319\msbuild.exe"
set WIX=C:\Program Files (x86)\WiX Toolset v3.11
set wixBinDir=%WIX%\bin
set OutputDir=%~dp0bin
if not exist "%OutputDir%" mkdir "%OutputDir%"

%msbuild% "%~dp0..\..\SparkleShare.sln" /target:SparkleShare_Windows:Rebuild /p:Configuration=ReleaseWindows /p:Platform="Any CPU" -m

if "%1"=="installer" (
	if exist "%wixBinDir%" (
	  if exist "%~dp0\SparkleShare.msi" del "%~dp0\SparkleShare.msi"
		"%wixBinDir%\heat.exe" dir "%OutputDir%\git_scm" -cg gitScmComponentGroup -gg -scom -sreg -sfrag -srd -dr GITSCM_DIR -var wix.gitscmpath -o "%~dp0\git_scm.wxs"
		"%wixBinDir%\heat.exe" dir "%OutputDir%\Images"  -cg ImagesComponentGroup  -gg -scom -sreg -sfrag -srd -dr IMAGES_DIR  -var wix.imagespath  -o "%~dp0\images.wxs"
		"%wixBinDir%\heat.exe" dir "%OutputDir%\Presets" -cg PresetsComponentGroup -gg -scom -sreg -sfrag -srd -dr PRESETS_DIR -var wix.presetspath -o "%~dp0\presets.wxs"
		"%wixBinDir%\candle" "%~dp0\SparkleShare.wxs" -ext WixUIExtension -ext WixUtilExtension -o "%~dp0\"
		"%wixBinDir%\candle" "%~dp0\git_scm.wxs" -ext WixUIExtension -ext WixUtilExtension -o "%~dp0\"
		"%wixBinDir%\candle" "%~dp0\images.wxs" -ext WixUIExtension -ext WixUtilExtension -o "%~dp0\"
		"%wixBinDir%\candle" "%~dp0\presets.wxs" -ext WixUIExtension -ext WixUtilExtension -o "%~dp0\"
		"%wixBinDir%\light" -ext WixUIExtension -ext WixUtilExtension "%~dp0Sparkleshare.wixobj" "%~dp0git_scm.wixobj" "%~dp0images.wixobj" "%~dp0presets.wixobj" -droot="%~dp0." -dgitscmpath="%OutputDir%\git_scm" -dimagespath="%OutputDir%\Images"  -dpresetspath="%OutputDir%\Presets" -o "%~dp0SparkleShare.msi"
		if exist "%~dp0\SparkleShare.msi" echo SparkleShare.msi created.
	) else (
		echo Not building installer ^(could not find wix, Windows Installer XML toolset^)
	  echo wix is available at http://wix.sourceforge.net/
		SET ERRORLEVEL=2
	)
) else echo Not building installer, as it was not requested. ^(Issue "build.cmd installer" to build installer ^)
