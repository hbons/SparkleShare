@echo off
set WinDirNet=%WinDir%\Microsoft.NET\Framework
set msbuild="%WinDirNet%\v3.5\msbuild.exe"
if not exist %msbuild% set msbuild="%WinDirNet%\v4.0.30319\msbuild.exe"

%msbuild% /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" "%~dp0\xslt.sln"
