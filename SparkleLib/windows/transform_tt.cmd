@echo off
cd %~dp0

set TextTransform=..\..\tools\TextTemplating\bin\TextTransform.exe
if not exist %TextTransform% call ..\..\tools\TextTemplating\build.cmd

echo running texttransform..

%TextTransform% -out Defines.cs Defines.tt
%TextTransform% -out GlobalAssemblyInfo.cs GlobalAssemblyInfo.tt

