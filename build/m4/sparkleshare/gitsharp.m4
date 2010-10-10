AC_DEFUN([SPARKLESHARE_GITSHARP],
[
	if test ! -d "$srcdir/GitSharp"; then
		AC_MSG_ERROR([GitSharp folder not found])
	fi

	dnl Assemblies for GitSharp and their dependencies
	dnl GitSharp also brings in Winterdom.IO.FileMap.dll but it is not used
	asms="ICSharpCode.SharpZipLib.dll Tamir.SharpSSH.dll GitSharp.Core.dll GitSharp.dll"
	for asm in $asms; do
		GITSHARP_ASSEMBLIES="$GITSHARP_ASSEMBLIES $asm"
		[[ -r "$asm.mdb" ]] && GITSHARP_ASSEMBLIES="$GITSHARP_ASSEMBLIES $asm.mdb"
	done

	AC_SUBST([GITSHARP_ASSEMBLIES])
])

