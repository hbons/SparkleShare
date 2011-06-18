@echo off

if "%CommonProgramFiles(x86)%"=="" (set common=%CommonProgramFiles%) else (set common=%CommonProgramFiles(x86)%)
if "%ProgramFiles(x86)%"=="" (set program=%ProgramFiles%) else (set program=%ProgramFiles(x86)%)

set TextTransform="%common%\Microsoft Shared\TextTemplating\10.0\texttransform.exe"
if not exist %TextTransform% set TextTransform="%common%\Microsoft Shared\TextTemplating\1.2\texttransform.exe"
if not exist %TextTransform% set TextTransform="%program%\MonoDevelop\AddIns\MonoDevelop.TextTemplating\TextTransform.exe"

echo running texttransform..
cd %~dp0


cd ..\..\Sparklelib\windows
%TextTransform% -out GlobalAssemblyInfo.cs GlobalAssemblyInfo.tt
