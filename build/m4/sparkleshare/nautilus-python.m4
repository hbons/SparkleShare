AC_DEFUN([SPARKLESHARE_NAUTILUS_PYTHON],
[
        AC_ARG_ENABLE(nautilus-extension,
           AC_HELP_STRING([--disable-nautilus-extension],[Do not install the Nautilus plugin]), enable_nautilus_extension=$enableval, enable_nautilus_extension=yes )
        if test x$enable_nautilus_extension = xyes; then
		PKG_CHECK_MODULES(NAUTILUS_PYTHON, nautilus-python < 1.1, have_nautilus2_python=yes, have_nautilus2_python=no)
		if test "x$have_nautilus2_python" = "xyes"; then
			NAUTILUS_PREFIX="`$PKG_CONFIG --variable=prefix nautilus-python`"
			AC_SUBST(NAUTILUS_PREFIX)
			NAUTILUS_PYTHON_DIR="`$PKG_CONFIG --variable=pythondir nautilus-python`"
			AC_SUBST(NAUTILUS_PYTHON_DIR)
			AM_CONDITIONAL(NAUTILUS2_EXTENSION_ENABLED, true)
		else
			AM_CONDITIONAL(NAUTILUS2_EXTENSION_ENABLED, false)
		fi
		PKG_CHECK_MODULES(NAUTILUS3_PYTHON, nautilus-python >= 1.1, have_nautilus3_python=yes, have_nautilus3_python=no)
		if test "x$have_nautilus3_python" = "xyes"; then
			NAUTILUS_PREFIX="`$PKG_CONFIG --variable=prefix nautilus-python`"
			AC_SUBST(NAUTILUS_PREFIX)
			NAUTILUS_PYTHON_DIR="`$PKG_CONFIG --variable=pythondir nautilus-python`"
			AC_SUBST(NAUTILUS_PYTHON_DIR)
			AM_CONDITIONAL(NAUTILUS3_EXTENSION_ENABLED, true)
		else
			AM_CONDITIONAL(NAUTILUS3_EXTENSION_ENABLED, false)
		fi
	else
		have_nautilus2_python="disabled"
		have_nautilus3_python="disabled"
	fi

	AM_CONDITIONAL(NAUTILUS2_EXTENSION_ENABLED, test "x$enable_nautilus_extension" = "xyes")
	AM_CONDITIONAL(NAUTILUS3_EXTENSION_ENABLED, test "x$enable_nautilus_extension" = "xyes")
])

