@echo off
pushd %~dp0

set xslt=..\..\tools\xslt\bin\release\xslt.exe
if not exist %xslt% call ..\..\tools\xslt\build.cmd

for %%a in (*.xml.in) do (
  %xslt% parse_plugins.xsl %%a %%~dpna
)

popd
