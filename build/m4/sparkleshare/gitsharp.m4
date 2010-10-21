AC_DEFUN([SPARKLESHARE_GITSHARP],
[
	SHAMROCK_FIND_PROGRAM_OR_BAIL(NANT, nant)

	if test ! -d "$srcdir/GitSharp"; then
		AC_MSG_ERROR([GitSharp folder not found])
	fi

	dnl Assemblies for GitSharp and their dependencies
	dnl GitSharp also brings in Winterdom.IO.FileMap.dll but it is not used
	asms="GitSharp/bin/ICSharpCode.SharpZipLib.dll GitSharp/bin/Tamir.SharpSSH.dll GitSharp/bin/GitSharp.Core.dll GitSharp/bin/GitSharp.dll"
	GITSHARP_ASSEMBLIES="$asms"
	for asm in $asms; do
		GITSHARP_FILES="$GITSHARP_FILES $asm"
		[[ -r "$asm.mdb" ]] && GITSHARP_FILES="$GITSHARP_FILES $asm.mdb"
	done
	# Additional dependencies that we need to install
	GITSHARP_DEPS="GitSharp/lib/DiffieHellman.dll GitSharp/lib/Org.Mentalis.Security.dll"

	AC_SUBST([GITSHARP_ASSEMBLIES])
	AC_SUBST([GITSHARP_FILES])
	AC_SUBST([GITSHARP_DEPS])
])

