@echo off
cd %~dp0

set TextTransform=..\..\SparkleShare\Windows\tools\TextTemplating\bin\TextTransform.exe
if not exist %TextTransform% call ..\..\SparkleShare\Windows\tools\TextTemplating\build.cmd

echo running texttransform..

%TextTransform% -out Defines.cs Defines.tt
%TextTransform% -out GlobalAssemblyInfo.cs GlobalAssemblyInfo.tt
%TextTransform% -out GlobalAssemblyInfoGit.cs GlobalAssemblyInfoGit.tt
