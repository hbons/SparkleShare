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
export gitLfsVersion=$(cat ${projectFolder}/git-lfs.version)

set -e

if [ ! -f ${projectFolder}/git-${gitVersion}.tar.gz ]
then
  curl -s -L https://www.kernel.org/pub/software/scm/git/git-${gitVersion}.tar.gz > git.tar.gz
  curl -s -L https://github.com/github/git-lfs/releases/download/v${gitLfsVersion}/git-lfs-darwin-amd64-${gitLfsVersion}.tar.gz > git-lfs.tar.gz

  test -e git.tar.gz || { echo "Failed to download git"; exit 1; }
  test -e git-lfs.tar.gz || { echo "Failed to download git-lfs"; exit 1; }

  tar xzf git.tar.gz
  tar xzf git-lfs.tar.gz

  cd git-${gitVersion}
  make configure
  ./configure --prefix=${projectFolder}/git --with-openssl=no
  make install
  cd ..

  cd git-lfs-${gitLfsVersion}
  cp git-lfs ${projectFolder}/git/libexec/git-core
  cd ..

  tar czf git-${gitVersion}.tar.gz git

  rm -rf git
  rm -rf git-${gitVersion}
  rm -rf git-lfs-${gitLfsVersion}

  rm git.tar.gz
  rm git-lfs.tar.gz
fi
