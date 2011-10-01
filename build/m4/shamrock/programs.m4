AC_DEFUN([SHAMROCK_FIND_PROGRAM],
[
	AC_PATH_PROG($1, $2, $3)
	AC_SUBST($1)
])

AC_DEFUN([SHAMROCK_FIND_PROGRAM_OR_BAIL],
[
	SHAMROCK_FIND_PROGRAM($1, $2, no)
	if test "x$$1" = "xno"; then
		AC_MSG_ERROR([You need to install '$2'])
	fi
])


