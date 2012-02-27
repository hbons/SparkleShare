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


@ECHO OFF

echo.
echo Setting up global variables...
SET path_xgettext=..\..\..\Bin\Gnu.Gettext.Win32\xgettext.exe
SET path_aspextract=..\..\..\Tools\Gettext.AspExtract\bin\Debug\AspExtract.exe
SET path_output=.\Templates
SET file_list=..\*.cs
SET asp_files_root=..

echo.
echo Generating strings po file...
CALL ..\..\..\Scripts\ExtractAspNetStrings.bat Strings