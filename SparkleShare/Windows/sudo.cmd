@echo off
powershell -Command "Start-Process cmd -Verb RunAs -ArgumentList '/c %*'"
@echo on
