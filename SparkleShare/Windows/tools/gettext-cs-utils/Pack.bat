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
echo Copying all files to package to local gettext-cs-utils folder

mkdir .\gettext-cs-utils\Gnu.Gettext.Win32\
copy .\Gettext.CsUtils\Bin\Gnu.Gettext.Win32\*.dll .\gettext-cs-utils\Gnu.Gettext.Win32\
copy .\Gettext.CsUtils\Bin\Gnu.Gettext.Win32\*.exe .\gettext-cs-utils\Gnu.Gettext.Win32\

mkdir .\gettext-cs-utils\Scripts
copy .\Gettext.CsUtils\Scripts\*.bat .\gettext-cs-utils\Scripts\

mkdir .\gettext-cs-utils\Binaries\
copy .\Gettext.CsUtils\Core\Gettext.Cs\bin\Release\*.dll .\gettext-cs-utils\Binaries\
copy .\Gettext.CsUtils\Core\Gettext.Cs.Web\bin\Release\*.dll .\gettext-cs-utils\Binaries\

mkdir .\gettext-cs-utils\Templates\
copy .\Gettext.CsUtils\Core\Gettext.Cs\Templates\*.tt .\gettext-cs-utils\Templates\

mkdir .\gettext-cs-utils\Tools\
copy .\Gettext.CsUtils\Tools\Gettext.AspExtract\bin\Release\*.exe .\gettext-cs-utils\Tools\
copy .\Gettext.CsUtils\Tools\Gettext.DatabaseResourceGenerator\bin\Release\*.exe .\gettext-cs-utils\Tools\
copy .\Gettext.CsUtils\Tools\Gettext.Iconv\bin\Release\*.exe .\gettext-cs-utils\Tools\
copy .\Gettext.CsUtils\Tools\Gettext.Msgfmt\bin\Release\*.exe .\gettext-cs-utils\Tools\
copy .\Gettext.CsUtils\Tools\Gettext.ResourcesReplacer\bin\Release\*.exe .\gettext-cs-utils\Tools\

pause