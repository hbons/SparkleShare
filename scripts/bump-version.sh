#!/bin/sh

if [ "$1" = "" ]; then
    echo "No version number specified. Usage: ./bump-version.sh VERSION_NUMBER"
else
    sed -i.bak "s/ Version='[^']*'/ Version='$1'/" ../SparkleShare/Windows/SparkleShare.wxs
    sed -i.bak "s/assembly:AssemblyVersion *(\"[^\"]*\")/assembly:AssemblyVersion (\"$1\")/" ../Sparkles/InstallationInfo.Directory.cs                 
    sed -i.bak "s/configuration.set('VERSION', '[^\"]*')/configuration.set('VERSION', '$1')/" ../meson.build
    cat ../SparkleShare/Mac/Info.plist | eval "sed -e '/<key>CFBundleShortVersionString<\/key>/{N;s#<string>.*<\/string>#<string>$1<\/string>#;}'" > ../SparkleShare/Mac/Info.plist.tmp
    cat ../SparkleShare/Mac/Info.plist.tmp | eval "sed -e '/<key>CFBundleVersion<\/key>/{N;s#<string>.*<\/string>#<string>$1<\/string>#;}'" > ../SparkleShare/Mac/Info.plist
    rm ../meson.build.bak
    rm ../SparkleShare/Mac/Info.plist.tmp
    rm ../SparkleShare/Windows/SparkleShare.wxs.bak
    rm ../Sparkles/InstallationInfo.Directory.cs.bak
fi

