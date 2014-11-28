#!/bin/sh

# expect path to app bundle argument
export bundle=$1
export projectFolder=$(dirname $0)
export gitVersion=$(cat ${projectFolder}/git.version)

echo postprocessing ${bundle}

export PATH=/usr/local/bin:/opt/local/bin:/Library/Frameworks/Mono.framework/Versions/Current/bin:/usr/bin:/bin

${projectFolder}/checkGit.sh
tar -x -f ${projectFolder}/git-${gitVersion}.tar.gz -C ${bundle}/Contents/Resources
cp -R SparkleShareInviteOpener.app ${bundle}/Contents/Resources
cp config ${bundle}/Contents/MonoBundle
