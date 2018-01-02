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


LINE=$(cat ${projectFolder}/git.download)
TMP=()

for val in $LINE ; do
        TMP+=("$val")
done

export projectFolder=$(dirname $0)
export projectFolder=$(abspath ${projectFolder})
export gitDownload="${TMP[0]}"
export gitSHA256="${TMP[1]}"

set -e


if [ ! -f ${projectFolder}/git.tar.gz ]
then
  curl --silent --location ${gitDownload} > git.tar.gz
  test -e git.tar.gz || { echo "Failed to download git"; exit 1; }

  printf "${gitSHA256}  git.tar.gz" | shasum --check --algorithm 256

  mkdir git/
  tar xzf git.tar.gz --directory git/
  tar czf git.tar.gz git/
  rm -rf git/
fi
