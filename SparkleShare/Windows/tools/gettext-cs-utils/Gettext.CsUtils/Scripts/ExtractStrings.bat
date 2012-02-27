rem gettext-cs-utils
rem
rem Copyright 2011 Manas Technology Solutions 
rem http://www.manas.com.ar/
rem 
rem This library is free software; you can redistribute it and/or
rem modify it under the terms of the GNU Lesser General Public
rem License as published by the Free Software Foundation; either 
rem version 2.1 of the License, or (at your option) any later version.
rem 
rem This library is distributed in the hope that it will be useful,
rem but WITHOUT ANY WARRANTY; without even the implied warranty of
rem MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
rem Lesser General Public License for more details.
rem 
rem You should have received a copy of the GNU Lesser General Public 
rem License along with this library.  If not, see <http://www.gnu.org/licenses/>.


@echo off

if "%1" == "" goto NONAME
if not "%3" == "" SET file_list=%3

if "%file_list%" == "" goto NOFILELIST

if "%path_xgettext%" == "" goto NOXGETTEXT
if "%path_output%" == "" goto NOOUTPUT

SET class_name=%2
if "%class_name%" == "" SET class_name=%1

echo.
echo Creating files lists to be retrieved by gettext...
dir %file_list% /S /B > %1.gettext.fileslist

echo.
echo Creating %1 pot file from all %class_name% strings...
%path_xgettext% -k -k%class_name%.T -k%class_name%.M --from-code=UTF-8 -LC# --omit-header -o%1.pot -f%1.gettext.fileslist

echo.
echo Copy %1.pot file to %path_output% folder...
copy %1.pot %path_output%\%1.pot

echo.
echo Removing all temporary files...
del /Q *.pot
del /Q *.gettext.fileslist

echo.
echo Finished
goto END

:NONAME
echo.
echo Must specify as first parameter the name of the resource.
goto END

:NOFILELIST
echo.
echo Must specify file_list environment variable.
goto END

:NOXGETTEXT
echo.
echo Must specify path_xgettext environment variable.
goto END

:NOOUTPUT
echo.
echo Must specify path_output environment variable.
goto END

:END
