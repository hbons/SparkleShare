ASSEMBLY = SparkleLib
TARGET = library

LINK = $(REF_SPARKLELIB)

SOURCES =  \
	Defines.cs \
	SparkleCommit.cs \
	SparkleEvents.cs \
	SparkleFetcher.cs \
	SparkleHelpers.cs \
	SparkleListener.cs \
	SparkleOptions.cs \
	SparklePaths.cs \
	SparklePlatform.cs \
	SparkleRepo.cs

SMARTIRC4NET_FILES_EXPANDED = $(foreach file, $(SMARTIRC4NET_FILES), $(top_builddir)/$(file))
GITSHARP_FILES_EXPANDED = $(foreach file, $(GITSHARP_FILES), $(top_builddir)/$(file))
GITSHARP_DEPS_EXPANDED = $(foreach file, $(GITSHARP_DEPS), $(top_srcdir)/$(file))

EXTRA_BUNDLE = $(SMARTIRC4NET_FILES_EXPANDED) $(GITSHARP_FILES_EXPANDED)  $(GITSHARP_DEPS_EXPANDED)

install-data-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;

uninstall-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;

include $(top_srcdir)/build/build.mk
