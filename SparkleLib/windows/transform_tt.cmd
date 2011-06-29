@echo off

set common=%CommonProgramFiles(x86)%
if "%common%"=="" set common=%CommonProgramFiles%
set program=%ProgramFiles(x86)%
if "%program%"=="" set program=%ProgramFiles%

set TextTransform="%common%\Microsoft Shared\TextTemplating\10.0\texttransform.exe"
if not exist %TextTransform% set TextTransform="%common%\Microsoft Shared\TextTemplating\1.2\texttransform.exe"
if not exist %TextTransform% set TextTransform="%program%\MonoDevelop\AddIns\MonoDevelop.TextTemplating\TextTransform.exe"

echo running texttransform..
cd %~dp0

%TextTransform% -out Defines.cs Defines.tt
%TextTransform% -out GlobalAssemblyInfo.cs GlobalAssemblyInfo.tt
