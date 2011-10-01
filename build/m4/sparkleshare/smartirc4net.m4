AC_DEFUN([SPARKLESHARE_SMARTIRC4NET],
[
	if test ! -d "$srcdir/SmartIrc4net"; then
		AC_MSG_ERROR([SmartIrc4net folder not found])
	fi
	ac_configure_args="$ac_configure_args --disable-pkg-config --disable-pkg-lib --disable-pkg-gac"
	AC_CONFIG_SUBDIRS([SmartIrc4net])
	asm="SmartIrc4net/bin/Meebey.SmartIrc4net.dll"
	SMARTIRC4NET_ASSEMBLY="$asm"
	SMARTIRC4NET_FILES="$asm"
	[[ -r "$asm.mdb" ]] && SMARTIRC4NET_FILES="$SMARTIRC4NET_FILES $asm.mdb"

	AC_SUBST([SMARTIRC4NET_ASSEMBLY])
	AC_SUBST([SMARTIRC4NET_FILES])
])

