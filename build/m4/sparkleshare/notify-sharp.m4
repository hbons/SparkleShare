AC_DEFUN([SPARKLESHARE_CHECK_NOTIFY_SHARP],
[
	PKG_CHECK_MODULES(NOTIFY_SHARP, notify-sharp, have_notify_sharp=yes, have_notify_sharp=no)
	if test "x$have_notify_sharp" = "xyes"; then
		AC_SUBST(NOTIFY_SHARP_LIBS)
		AM_CONDITIONAL(EXTERNAL_NOTIFY_SHARP, true)
	else
		AM_CONDITIONAL(EXTERNAL_NOTIFY_SHARP, false)
		AC_MSG_RESULT([no])
	fi
])

