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
export gitDownload=$(cat ${projectFolder}/git.download)

set -e

if [ ! -f ${projectFolder}/git.tar.gz ]
then
  curl -s -L ${gitDownload} > git.tar.gz
  test -e git.tar.gz || { echo "Failed to download git"; exit 1; }

  mkdir git/
  tar xzf git.tar.gz --directory git/
  tar czf git.tar.gz git/
  rm -rf git/
fi
