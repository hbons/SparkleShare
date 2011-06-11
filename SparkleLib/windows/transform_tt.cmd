@echo off

echo running texttransform..
cd %~dp0

"%CommonProgramFiles%\Microsoft Shared\TextTemplating\1.2\texttransform.exe" -out Defines.cs Defines.tt
"%CommonProgramFiles%\Microsoft Shared\TextTemplating\1.2\texttransform.exe" -out GlobalAssemblyInfo.cs GlobalAssemblyInfo.tt
