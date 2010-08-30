AC_DEFUN([SHAMROCK_CHECK_GNOME_DOC_UTILS],
[
    AC_ARG_ENABLE([user-help],
           AC_HELP_STRING([--enable-user-help], [Enable building the user-help [[default=auto]]]),,
           enable_user_help=auto)

	if test "x$enable_user_help" = "xauto"; then
        PKG_CHECK_MODULES(GNOME_DOC_UTILS,
            gnome-doc-utils,
            enable_user_help=yes, enable_user_help=no)
    elif test "x$enable_user_help" = "xyes"; then
        PKG_CHECK_MODULES(GNOME_DOC_UTILS, gnome-doc-utils)
    fi

	if test "x$enable_user_help" = "xyes"; then
        GNOME_DOC_INIT([$1], enable_user_help=yes, enable_user_help=no)
    else
        AM_CONDITIONAL(ENABLE_SK, false)
    fi

	AM_CONDITIONAL(HAVE_GNOME_DOC_UTILS, test "x$enable_user_help" = "xyes")
])
