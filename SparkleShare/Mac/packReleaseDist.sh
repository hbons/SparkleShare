#!/bin/sh

# expect path to app bundle argument
export bundle=$1
export projectFolder=$(dirname $0)

echo packing ${bundle} for release without Mono framework dependency

export PKG_CONFIG_PATH=/usr/lib/pkgconfig:/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig
export AS="as -arch i386"
export CC="cc -arch i386 -lobjc -liconv -framework Foundation"
export PATH=/usr/local/bin:/opt/local/bin:/Library/Frameworks/Mono.framework/Versions/Current/bin:/usr/bin:/bin

cd ${bundle}/Contents/MonoBundle/
mkbundle --static --deps -o ../MacOS/SparkleShare  SparkleShare.exe SparkleLib.dll MonoMac.dll SparkleLib.Git.dll
rm *.dll *.exe
