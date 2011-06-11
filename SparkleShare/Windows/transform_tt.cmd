@echo off

echo running texttransform..
cd %~dp0


cd ..\..\Sparklelib\windows
"%CommonProgramFiles%\Microsoft Shared\TextTemplating\1.2\texttransform.exe" -out GlobalAssemblyInfo.cs GlobalAssemblyInfo.tt
