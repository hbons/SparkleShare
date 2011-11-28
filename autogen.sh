#!/usr/bin/env bash

PROJECT=sparkleshare

function error () {
	echo "Error: $1" 1>&2
	exit 1
}

function check_autotool_version () {
	which $1 &>/dev/null || {
		error "$1 is not installed, and is required to configure $PACKAGE"
	}

	version=$($1 --version | head -n 1 | cut -f4 -d' ')
	major=$(echo $version | cut -f1 -d.)
	minor=$(echo $version | cut -f2 -d.)
	rev=$(echo $version | cut -f3 -d. | sed 's/[^0-9].*$//')
	major_check=$(echo $2 | cut -f1 -d.)
	minor_check=$(echo $2 | cut -f2 -d.)
	rev_check=$(echo $2 | cut -f3 -d.)

	if [ $major -lt $major_check ]; then
		do_bail=yes
	elif [[ $minor -lt $minor_check && $major = $major_check ]]; then
		do_bail=yes
	elif [[ $rev -lt $rev_check && $minor = $minor_check && $major = $major_check ]]; then
		do_bail=yes
	fi

	if [ x"$do_bail" = x"yes" ]; then
		error "$1 version $2 or better is required to configure $PROJECT"
	fi
}

function run () {
	echo "Running $@ ..."
	$@ 2>.autogen.log || {
		cat .autogen.log 1>&2
		rm .autogen.log
		error "Could not run $1, which is required to configure $PROJECT"
	}
	rm .autogen.log
}

srcdir=$(dirname $0)
test -z "$srcdir" && srcdir=.

(test -f $srcdir/configure.ac) || {
	error "Directory \"$srcdir\" does not look like the top-level $PROJECT directory"
}

# MacPorts on OS X only seems to have glibtoolize
WHICHLIBTOOLIZE=$(which libtoolize || which glibtoolize)
if [ x"$WHICHLIBTOOLIZE" == x"" ]; then
	error "libtool is required to configure $PROJECT"
fi
LIBTOOLIZE=$(basename $WHICHLIBTOOLIZE)

check_autotool_version aclocal 1.9
check_autotool_version automake 1.9
check_autotool_version autoconf 2.53
check_autotool_version $LIBTOOLIZE 1.4.3
check_autotool_version intltoolize 0.35.0
check_autotool_version pkg-config 0.14.0

if [ $(pkg-config --modversion gnome-doc-utils 2> /dev/null) ]; then
    run gnome-doc-prepare --automake --force
else
    echo "gnome-doc-utils not found; user help will not be built"
    echo "AC_DEFUN([GNOME_DOC_INIT], [AC_MSG_NOTICE([])])" > build/m4/gnome-doc-utils.m4
    ACLOCAL_FLAGS="-I build/m4 $ACLOCAL_FLAGS"
    touch gnome-doc-utils.make
fi

run intltoolize --force --copy
run $LIBTOOLIZE --force --copy --automake
run aclocal -I build/m4/sparkleshare -I build/m4/shamrock -I build/m4/shave $ACLOCAL_FLAGS
run autoconf

run automake --gnu --add-missing --force --copy \
	-Wno-portability -Wno-portability

if [ ! -z "$NOCONFIGURE" ]; then
	echo "Done. ./configure skipped."
	exit $?
fi

if [ $# = 0 ]; then
	echo "WARNING: I am going to run configure without any arguments."
fi

run ./configure --enable-maintainer-mode $@

