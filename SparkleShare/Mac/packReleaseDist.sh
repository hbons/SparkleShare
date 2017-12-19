#!/bin/sh

# expect path to app bundle argument
export bundle=$1
export projectFolder=$(dirname $0)

echo packing ${bundle} for release without Mono framework dependency

export MONO_PATH=`readlink /Library/Frameworks/Mono.framework/Versions/Current`
export PKG_CONFIG_PATH=/usr/lib/pkgconfig:${MONO_PATH}/lib/pkgconfig
export AS="as -arch i386"
export CC="cc -arch i386 -lobjc -liconv -framework Foundation"
export PATH=/usr/local/bin:/opt/local/bin:/Library/Frameworks/Mono.framework/Versions/Current/bin:/usr/bin:/bin

cd ${bundle}/Contents/MonoBundle/

# merge all Assemblies into one Mac binary
mkbundle --static --deps --config ./config  -o ../MacOS/SparkleShare SparkleShare.exe Sparkles.dll Xamarin.Mac.dll Sparkles.Git.dll
rm *.dll *.exe
