AC_DEFUN([SPARKLESHARE_SMARTIRC4NET],
[
	if test ! -d "$srcdir/SmartIrc4net"; then
		AC_MSG_ERROR([SmartIrc4net folder not found])
	fi
	AC_CONFIG_SUBDIRS([SmartIrc4net])
	AC_SUBST([SMARTIRC4NET_ASSEMBLY], "SmartIrc4net/bin/Meebey.SmartIrc4net.dll")
])

