#!/bin/bash

if [ "$1" = "" ]; then
  echo "No version number specified. Usage: ./bump-version.sh VERSION_NUMBER"
else
  sed -i.bak "s/ Version='[^']*'/ Version='$1'/" ../SparkleShare/Windows/SparkleShare.wxs
  sed -i.bak "s/assembly:AssemblyVersion *(\"[^\"]*\")/assembly:AssemblyVersion (\"$1\")/" ../SparkleLib/Defines.cs                 
  sed -i.bak "s/m4_define(.sparkleshare_version[^)]*)/m4_define([sparkleshare_version], [$1])/" ../configure.ac
  rm ../SparkleShare/Windows/SparkleShare.wxs.bak
  rm ../SparkleLib/Defines.cs.bak
  rm ../configure.ac.bak
fi

