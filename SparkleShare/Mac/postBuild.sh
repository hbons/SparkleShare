#!/bin/sh

# Expect path to app bundle argument
export bundle=$1
export projectFolder=$(dirname $0)

echo Postprocessing ${bundle}...

export PATH=/usr/local/bin:/opt/local/bin:/Library/Frameworks/Mono.framework/Versions/Current/bin:/usr/bin:/bin

${projectFolder}/checkGit.sh
tar -x -f ${projectFolder}/git.tar.gz --directory ${bundle}/Contents/Resources
cp -R SparkleShareInviteOpener.app ${bundle}/Contents/Resources
