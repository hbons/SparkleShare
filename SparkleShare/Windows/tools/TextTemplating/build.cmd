@echo off
set WinDirNet=%WinDir%\Microsoft.NET\Framework
set msbuild="%WinDirNet%\v4.0.30319\msbuild.exe"

%msbuild% /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" "%~dp0\TextTemplating.sln"
