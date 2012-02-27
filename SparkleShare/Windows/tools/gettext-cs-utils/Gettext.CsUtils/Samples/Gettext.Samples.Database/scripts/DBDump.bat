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

SET dbrsgen=..\..\..\Tools\Gettext.DatabaseResourceGenerator\bin\Debug\DatabaseResourceGenerator.exe

echo.
echo Dumping culture sets into DB...

echo Culture es
CALL %dbrsgen% -i ..\Translated\es\Strings.po -c es -a

echo Culture en
CALL %dbrsgen% -i ..\Translated\en\Strings.po -c en -a

echo Culture pt
CALL %dbrsgen% -i ..\Translated\pt\Strings.po -c pt -a

echo Culture fr
CALL %dbrsgen% -i ..\Translated\fr\Strings.po -c fr -a

pause