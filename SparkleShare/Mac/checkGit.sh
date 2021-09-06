#!/bin/sh
set -x
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

LINE=$(cat ${projectFolder}/git.download)
TMP=()

for val in $LINE ; do
        TMP+=("$val")
done

export gitDownload="${TMP[0]}"
export gitName=${gitDownload##*/}
export gitSHA256="${TMP[1]}"


set -e


if [[ ! -f ${projectFolder}/${gitName} ]];
then
  curl --silent --location ${gitDownload} > ${gitName}
  test -e ${gitName} || { echo "Failed to download git"; exit 1; }

  printf "${gitSHA256}  ${gitName}" | shasum --check --algorithm 256

fi
if [[ -f ${projectFolder}/git.tar.gz ]];
then
    rm ${projectFolder}/git.tar.gz
fi
echo $gitName
echo ${projectFolder}/git.tar.gz
ln -s $gitName ${projectFolder}/git.tar.gz
