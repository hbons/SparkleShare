dnl Process this file with autoconf to produce a configure script.
m4_define([sparkleshare_version], [2.0])

AC_PREREQ([2.54])
AC_INIT([SparkleShare], sparkleshare_version)
AM_INIT_AUTOMAKE([1.11 dist-bzip2 dist-zip foreign tar-pax])
AM_MAINTAINER_MODE

dnl Export Version Info
AC_SUBST([ASM_VERSION], [sparkleshare_version])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([pkg-config is not installed])
fi

AC_SUBST([ACLOCAL_AMFLAGS], ["-I build/m4/sparkleshare -I build/m4/shamrock -I build/m4/shave \${ACLOCAL_FLAGS}"])

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

AC_PROG_INSTALL

dnl Mono and gmcs
SHAMROCK_CHECK_MONO_MODULE(2.8)
SHAMROCK_FIND_MONO_RUNTIME
    
PKG_CHECK_EXISTS([mono >= 2.8],
[SHAMROCK_FIND_MONO_4_0_COMPILER
SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
    System
])]
)

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

AC_ARG_ENABLE(appindicator,
	AS_HELP_STRING([--enable-appindicator[=@<:@no/auto/yes@:>@]],[Build support for application indicators ]),
	[enable_appindicator=$enableval],
	[enable_appindicator="auto"])

GUISUBDIRS=

OSNAME=`uname -s`
if test "$OSNAME" = "Linux" ; then
	SPARKLESHARE_CHECK_GTK_SHARP
	PKG_CHECK_MODULES(NOTIFY_SHARP, notify-sharp-3.0)

	dnl check for webkit2-sharp
	PKG_CHECK_MODULES(WEBKIT2_SHARP, webkit2-sharp-4.0, have_webkit2_sharp=yes, have_webkit2_sharp=no)
	if test "x$have_webkit2_sharp" = "xno" ; then
		AC_MSG_ERROR([webkit2-sharp-4.0 is not installed])
	        AC_SUBST(WEBKIT2_SHARP_LIBS)
	fi

	APPINDICATOR_REQUIRED=0.1

	if test x$enable_appindicator = xauto ; then
		PKG_CHECK_EXISTS([appindicator3-sharp-0.1 >= $APPINDICATOR_REQUIRED],
			enable_appindicator="yes", enable_appindicator="no")
	fi

	if test x$enable_appindicator = xyes ; then
		PKG_CHECK_EXISTS([appindicator3-sharp-0.1 >= $APPINDICATOR_REQUIRED],,
			AC_MSG_ERROR([appindicator3-sharp-0.1 is not installed]))

		PKG_CHECK_MODULES(APP_INDICATOR, appindicator3-sharp-0.1 >= $APPINDICATOR_REQUIRED)
		AC_SUBST(APP_INDICATOR_CFLAGS)
		AC_SUBST(APP_INDICATOR_LIBS)
		AC_DEFINE(HAVE_APP_INDICATOR, 1, [Have AppIndicator])
	fi

	GUISUBDIRS=SparkleShare
fi

AM_CONDITIONAL(HAVE_APP_INDICATOR, test x"$enable_appindicator" = xyes)
AM_CONDITIONAL(BUILD_XDG_APP, test x"$prefix" = "/app")

AC_SUBST([GUISUBDIRS])
SHAMROCK_CHECK_NUNIT
SHAVE_INIT([build/m4/shave], [enable])

AC_OUTPUT([
Makefile
build/Makefile
build/m4/Makefile
build/m4/shave/shave
build/m4/shave/shave-libtool
Sparkles/InstallationInfo.Directory.cs
Sparkles/Makefile
Sparkles/Git/Makefile
SparkleShare/Makefile
SparkleShare/Common/Makefile
SparkleShare/Common/HTML/Makefile
SparkleShare/Common/Presets/Makefile
SparkleShare/Common/Pixmaps/Makefile
SparkleShare/Linux/Makefile
SparkleShare/Linux/Pixmaps/Makefile
SparkleShare/Linux/Pixmaps/icons/Makefile
SparkleShare/Linux/Pixmaps/icons/hicolor/Makefile
SparkleShare/Linux/Pixmaps/icons/ubuntu-mono-dark/Makefile
SparkleShare/Linux/Pixmaps/icons/ubuntu-mono-light/Makefile
SparkleShare/Linux/sparkleshare
])

echo "
SparkleShare ${VERSION}

Configuration:
       Prefix: ${prefix}

"

