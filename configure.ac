dnl Process this file with autoconf to produce a configure script.
m4_define([sparkleshare_version],
	[0.4.0])

m4_define([sparkleshare_asm_version],
	[0.4.0])

AC_PREREQ([2.54])
AC_INIT([SparkleShare], sparkleshare_version)
AM_INIT_AUTOMAKE([1.11 dist-bzip2 dist-zip foreign tar-ustar])
AM_MAINTAINER_MODE

dnl Export Version Info
AC_SUBST([ASM_VERSION], [sparkleshare_asm_version])

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

dnl Help files
SHAMROCK_CHECK_GNOME_DOC_UTILS(0.17.3)

AC_PROG_INSTALL

dnl Mono and gmcs
SHAMROCK_CHECK_MONO_MODULE(2.2)
SHAMROCK_FIND_MONO_RUNTIME
    
PKG_CHECK_EXISTS([mono >= 2.8],
[SHAMROCK_FIND_MONO_4_0_COMPILER
SHAMROCK_CHECK_MONO_4_0_GAC_ASSEMBLIES([
    System
    System.Security
    Mono.Posix
])],
[SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_CHECK_MONO_2_0_GAC_ASSEMBLIES([
    System
    System.Security
    Mono.Posix
])])

SPARKLESHARE_CHECK_NOTIFY_SHARP

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
#PKG_CHECK_MODULES([NDESK_DBUS], [ndesk-dbus-1.0])
#AC_SUBST(NDESK_DBUS_LIBS)
#PKG_CHECK_MODULES([NDESK_DBUS_GLIB], [ndesk-dbus-glib-1.0])
#AC_SUBST(NDESK_DBUS_GLIB_LIBS)


SPARKLESHARE_CHECK_GTK_SHARP

SPARKLESHARE_SMARTIRC4NET
PKG_CHECK_MODULES([SMARTIRC4NET], [smartirc4net >= 0.5],
	SMARTIRC4NET_ASSEMBLY=""
	AC_SUBST(SMARTIRC4NET_ASSEMBLY)
	,
 	SMARTIRC4NET_LIBS=""
	AC_SUBST(SMARTIRC4NET_LIBS)
	SPARKLE_SMARTIRC4NETDIR=SmartIrc4Net
	AC_SUBST(SPARKLE_SMARTIRC4NETDIR)
)
AM_CONDITIONAL([HAVE_SMARTIRC4NET], test x$SPARKLE_SMARTIRC4NETDIR = x)

AC_ARG_ENABLE(gtkui,
	AS_HELP_STRING([--disable-gtkui], [Do not build the Gtk+ user interface]),
	[ enable_gtkui=no ], [ enable_gtkui=yes ])

AC_ARG_ENABLE(appindicator,
	AS_HELP_STRING([--enable-appindicator[=@<:@no/auto/yes@:>@]],[Build support for application indicators ]),
	[enable_appindicator=$enableval],
	[enable_appindicator="auto"])

AM_CONDITIONAL(ENABLE_GTKUI, test x$enable_gtkui = xyes)
if test "x$enable_gtkui" = "xyes" ; then
	dnl check for webkit-sharp
	PKG_CHECK_MODULES(WEBKIT_SHARP, webkit-sharp-1.0, have_webkit_sharp=yes, have_webkit_sharp=no)
	if test "x$have_webkit_sharp" = "xno" ; then
		AC_ERROR("webkit-sharp is a required dependency: you need to install the appropriate devel package before you can compile")
	fi
	AC_SUBST(WEBKIT_SHARP_LIBS)

	dnl check for notify-sharp
	PKG_CHECK_MODULES(NOTIFY_SHARP, notify-sharp, have_notify_sharp=yes, have_notify_sharp=no)
	if test "x$have_notify_sharp" = "xno" ; then
		AC_ERROR("notify-sharp is a required dependency: you need to install the appropriate devel package before you can compile")
	fi
	AC_SUBST(NOTIFY_SHARP_LIBS)

	APPINDICATOR_REQUIRED=0.0.7

	if test x$enable_appindicator = xauto ; then
			PKG_CHECK_EXISTS([appindicator-sharp-0.1 >= $APPINDICATOR_REQUIRED],
							 enable_appindicator="yes",
							 enable_appindicator="no")
	fi

	if test x$enable_appindicator = xyes ; then
			PKG_CHECK_EXISTS([appindicator-sharp-0.1 >= $APPINDICATOR_REQUIRED],,
							 AC_MSG_ERROR([appindicator-sharp-0.1 is not installed]))
			PKG_CHECK_MODULES(APP_INDICATOR,
							appindicator-sharp-0.1 >= $APPINDICATOR_REQUIRED)
			AC_SUBST(APP_INDICATOR_CFLAGS)
			AC_SUBST(APP_INDICATOR_LIBS)
			AC_DEFINE(HAVE_APP_INDICATOR, 1, [Have AppIndicator])
	fi

	GUISUBDIRS=SparkleShare
else
	GUISUBDIRS=
fi

AM_CONDITIONAL(HAVE_APP_INDICATOR, test x"$enable_appindicator" = xyes)
AC_SUBST([GUISUBDIRS])

SHAMROCK_CHECK_NUNIT

dnl Get nautilus extensions directory
SPARKLESHARE_NAUTILUS_PYTHON

SHAVE_INIT([build/m4/shave], [enable])

AC_OUTPUT([
build/Makefile
build/m4/Makefile
build/m4/shave/shave
build/m4/shave/shave-libtool
data/Makefile
data/icons/Makefile
data/html/Makefile
data/plugins/Makefile
help/Makefile
SparkleLib/AssemblyInfo.cs
SparkleLib/Defines.cs
SparkleLib/Makefile
SparkleShare/sparkleshare
SparkleShare/Makefile
SparkleShare/Mac/Makefile
SparkleShare/Nautilus/Makefile
SparkleShare/Nautilus/sparkleshare-nautilus-extension.py
SparkleShare/Nautilus/sparkleshare-nautilus3-extension.py
po/Makefile.in
Makefile
])

echo "
SparkleShare ${VERSION}

Configuration:
       Prefix              : ${prefix}
       Build Gtk+ UI       : ${enable_gtkui}
       Nautilus 2.x plugin : ${have_nautilus2_python}
       Nautilus 3.x plugin : ${have_nautilus3_python}
       User Help           : ${enable_user_help} (requires gnome-doc-utils >= 0.17.3)

"

