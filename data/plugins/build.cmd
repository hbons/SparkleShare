@echo off
pushd %~dp0

set xslt=..\..\SparkleShare\Windows\tools\xslt\bin\release\xslt.exe
if not exist %xslt% call ..\..\SparkleShare\Windows\tools\xslt\build.cmd

for %%a in (*.xml.in) do (
  %xslt% parse_plugins.xsl %%a %%~dpna
)

popd
