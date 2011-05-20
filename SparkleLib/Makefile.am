ASSEMBLY = SparkleLib
TARGET = library

LINK = $(REF_SPARKLELIB)

SOURCES =  \
	Defines.cs \
	SparkleBackend.cs \
	SparkleChangeSet.cs \
	SparkleFetcherBase.cs \
	SparkleFetcherGit.cs \
	SparkleFetcherMercurial.cs \
	SparkleHelpers.cs \
	SparkleListenerBase.cs \
	SparkleListenerIrc.cs \
	SparkleOptions.cs \
	SparklePaths.cs \
	SparkleRepoBase.cs \
	SparkleRepoGit.cs \
	SparkleRepoMercurial.cs


SMARTIRC4NET_FILES_EXPANDED = $(foreach file, $(SMARTIRC4NET_FILES), $(top_builddir)/$(file))

EXTRA_BUNDLE = $(SMARTIRC4NET_FILES_EXPANDED)

install-data-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;

uninstall-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;

include $(top_srcdir)/build/build.mk
