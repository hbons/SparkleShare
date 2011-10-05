@echo off
set WinDirNet=%WinDir%\Microsoft.NET\Framework
set msbuild="%WinDirNet%\v3.5\msbuild.exe"
set gitpath="C:\msysgit\bin\git.exe"
if not exist %msbuild% set msbuild="%WinDirNet%\v4.0.30319\msbuild.exe"
if not exist %gitpath% (
 echo "Could not find git binary at %gitpath%, please install msysgit to C:\msysgit"
 pause
 exit
 )
set PATH=C:\msysgit\bin;%PATH%
cd ..\..\
git submodule update --init
cd SparkleShare\Windows
%msbuild% /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" %~dp0\SparkleShare.sln
