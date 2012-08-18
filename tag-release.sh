#!/bin/sh

if [ "$1" = "" ]; then
  echo No release specified
  echo Current releases:
  git fetch --tags
  git tag
  exit
fi

sed -i -e "s/ Version='[^']*'/ Version='$1'/" SparkleShare/Windows/SparkleShare.wxs
sed -i -e "s/assembly:AssemblyVersion *(\"[^\"]*\")/assembly:AssemblyVersion (\"$1\")/" SparkleLib/Defines.cs
sed -i -e "s/m4_define(.sparkleshare_version[^)]*)/m4_define([sparkleshare_version], [$1])/" configure.ac

git add SparkleShare/Windows/SparkleShare.wxs SparkleLib/Defines.cs configure.ac
git commit -m "tagged $1"
git tag "$1"
git push
git push --tags
