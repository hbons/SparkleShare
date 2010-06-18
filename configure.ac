dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([SparkleShare], [0.1])
AM_INIT_AUTOMAKE([1.11 dist-bzip2 dist-zip foreign])
AM_MAINTAINER_MODE

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

AC_SUBST([ACLOCAL_AMFLAGS], ["-I build/m4/sparkleshare -I build/m4/shamrock -I build/m4/shave \${ACLOCAL_FLAGS}"])

dnl i18n
IT_PROG_INTLTOOL([0.40.6])
GETTEXT_PACKAGE=sparkleshare
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE,"$GETTEXT_PACKAGE", [Gettext package])

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

AC_PROG_INSTALL

AC_PATH_PROG(GMCS, gmcs, no)
if test "x$GMCS" = "xno"; then
        AC_MSG_ERROR([gmcs Not found])
fi


AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=YES]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=NO]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_DEBUG, true)
	enable_debug=yes
fi


dnl package checks, common for all configs
PKG_CHECK_MODULES([NDESK_DBUS], [ndesk-dbus-1.0])
AC_SUBST(NDESK_DBUS_LIBS)
PKG_CHECK_MODULES([NDESK_DBUS_GLIB], [ndesk-dbus-glib-1.0])
AC_SUBST(NDESK_DBUS_GLIB_LIBS)


SPARKLESHARE_CHECK_GTK_SHARP
#SPARKLESHARE_CHECK_NOTIFY_SHARP

SHAMROCK_CHECK_NUNIT

dnl Mono and gmcs
SHAMROCK_CHECK_MONO_MODULE(2.2)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_CHECK_MONO_2_0_GAC_ASSEMBLIES([
    System
    System.Security
    Mono.Posix
])

SHAVE_INIT([build/m4/shave], [enable])


AC_OUTPUT([
build/Makefile
build/m4/Makefile
build/m4/shave/shave
build/m4/shave/shave-libtool
data/Makefile
data/icons/Makefile
notify-sharp/Makefile
SparkleShare/sparkleshare
SparkleShare/Defines.cs
SparkleShare/AssemblyInfo.cs
SparkleShare/Makefile
po/Makefile.in
Makefile
])

