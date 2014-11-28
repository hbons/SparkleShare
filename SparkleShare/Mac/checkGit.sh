#!/bin/sh                                                                                                                                          
function abspath()
{
  case "${1}" in
    [./]*)
    echo "$(cd ${1%/*}; pwd)/${1##*/}"
    ;;
    *)
    echo "${PWD}/${1}"
    ;;
  esac
}

export projectFolder=$(dirname $0)
export projectFolder=$(abspath ${projectFolder})

export gitVersion=$(cat ${projectFolder}/git.version)

set -e

if [ ! -f ${projectFolder}/git-${gitVersion}.tar.gz ]
then
    curl -s https://codeload.github.com/git/git/zip/v${gitVersion} > git.zip
    unzip -q git.zip

    cd git-${gitVersion}

    make configure
    ./configure --prefix=${projectFolder}/git
    make install
    cd ..

    tar cfz git-${gitVersion}.tar.gz git
    rm -rf git
    rm -rf git-${gitVersion}
    rm git.zip
fi
