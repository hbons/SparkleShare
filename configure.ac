dnl Process this file with autoconf to produce a configure script.
m4_define([sparkleshare_version], [2.0.1])

AC_PREREQ([2.54])
AC_INIT([SparkleShare], sparkleshare_version)
AM_INIT_AUTOMAKE([1.11 dist-bzip2 dist-zip foreign tar-pax])
AM_MAINTAINER_MODE
LT_INIT

AC_CONFIG_MACRO_DIRS([build/m4/sparkleshare build/m4/shamrock build/m4/shave])

dnl Export Version Info
AC_SUBST([ASM_VERSION], [sparkleshare_version])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "$PKG_CONFIG" = "no"; then
	AC_MSG_ERROR([pkg-config is not installed])
fi

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

AC_PROG_INSTALL

dnl Mono and mcs
SHAMROCK_CHECK_MONO_MODULE(3.2.8)
SHAMROCK_FIND_MONO_RUNTIME

PKG_CHECK_EXISTS([mono >= 3.2.8],
	[SHAMROCK_FIND_MONO_4_5_COMPILER
	SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
		System
	])])

AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=YES]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test "$enable_debug" = "yes")
if test "$enable_debug" = "yes" ; then
	CONFIG_REQUESTED="yes"
fi

AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=NO]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test "$enable_release" = "yes")
if test "$enable_release" = "yes" ; then
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
	if test "$have_webkit2_sharp" = "no" ; then
		AC_MSG_ERROR([webkit2-sharp-4.0 is not installed])
	        AC_SUBST(WEBKIT2_SHARP_LIBS)
	fi

	APPINDICATOR_REQUIRED=0.1

	if test "$enable_appindicator" = "auto" ; then
		PKG_CHECK_EXISTS([appindicator3-sharp-0.1 >= $APPINDICATOR_REQUIRED],
			enable_appindicator="yes", enable_appindicator="no")
	fi

	if test "$enable_appindicator" = "yes" ; then
		PKG_CHECK_EXISTS([appindicator3-sharp-0.1 >= $APPINDICATOR_REQUIRED],,
			AC_MSG_ERROR([appindicator3-sharp-0.1 is not installed]))

		PKG_CHECK_MODULES(APP_INDICATOR, appindicator3-sharp-0.1 >= $APPINDICATOR_REQUIRED)
		AC_SUBST(APP_INDICATOR_CFLAGS)
		AC_SUBST(APP_INDICATOR_LIBS)
		AC_DEFINE(HAVE_APP_INDICATOR, 1, [Have AppIndicator])
	fi

	GUISUBDIRS=SparkleShare
fi

AM_CONDITIONAL(HAVE_APP_INDICATOR, test "$enable_appindicator" = "yes")
AM_CONDITIONAL(BUILD_FLATPAK, test "$prefix" = "/app")

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
		SparkleShare/Common/Images/Makefile
		SparkleShare/Linux/Makefile
		SparkleShare/Linux/Images/Makefile
		SparkleShare/Linux/Images/icons/Makefile
		SparkleShare/Linux/Images/icons/hicolor/Makefile
		SparkleShare/Linux/Images/icons/ubuntu-mono-dark/Makefile
		SparkleShare/Linux/Images/icons/ubuntu-mono-light/Makefile
		SparkleShare/Linux/sparkleshare
])

echo "
SparkleShare ${VERSION}

Configuration:
       Prefix: ${prefix}

"

