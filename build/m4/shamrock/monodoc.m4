AC_DEFUN([SHAMROCK_CHECK_MONODOC],
[
	AC_ARG_ENABLE(docs, AC_HELP_STRING([--disable-docs], 
		[Do not build documentation]), , enable_docs=yes)

	if test "x$enable_docs" = "xyes"; then
		AC_PATH_PROG(MONODOCER, monodocer, no)
		if test "x$MONODOCER" = "xno"; then
			AC_MSG_ERROR([You need to install monodoc, or pass --disable-docs to configure to skip documentation installation])
		fi

		AC_PATH_PROG(MDASSEMBLER, mdassembler, no)
		if test "x$MDASSEMBLER" = "xno"; then
			AC_MSG_ERROR([You need to install mdassembler, or pass --disable-docs to configure to skip documentation installation])
		fi

		DOCDIR=`$PKG_CONFIG monodoc --variable=sourcesdir`
		AC_SUBST(DOCDIR)
		AM_CONDITIONAL(BUILD_DOCS, true)
	else
		AC_MSG_NOTICE([not building ${PACKAGE} API documentation])
		AM_CONDITIONAL(BUILD_DOCS, false)
	fi
])

