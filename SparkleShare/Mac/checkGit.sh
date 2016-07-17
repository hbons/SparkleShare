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

  list_hash=$(sed -n -e "/git-${gitVersion}.tar.gz/p" sha256sums.asc | cut -c 1-64)
  file_hash=$(shasum -a 256 git.tar.gz | cut -c 1-64)

  curl -s https://www.kernel.org/pub/software/scm/git/git-${gitVersion}.tar.gz > git.tar.gz

  test -e git.tar.gz || { echo "Failed to download git"; exit 1; }

  test "$file_hash" = "$list_hash" || { echo "SHA256 Mistmatch"; exit 1; }

    tar xf git.tar.gz
    cd git-${gitVersion}

    make configure
    ./configure --prefix=${projectFolder}/git --with-openssl=no
    make install
    cd ..

    tar cfz git-${gitVersion}.tar.gz git
    rm -rf git
    rm -rf git-${gitVersion}
    rm git.tar.gz
fi
