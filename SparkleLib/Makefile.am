ASSEMBLY = SparkleLib
TARGET = library

SOURCES =  \
	Defines.cs \
	SparkleAnnouncement.cs \
	SparkleBackend.cs \
	SparkleChangeSet.cs \
	SparkleConfig.cs \
	SparkleExtensions.cs \
	SparkleFetcherBase.cs \
	SparkleHelpers.cs \
	SparkleListenerBase.cs \
	SparkleListenerFactory.cs \
	SparkleListenerTcp.cs \
	SparkleRepoBase.cs \
	SparkleUser.cs \
	SparkleWatcher.cs


install-data-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;

uninstall-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;

include $(top_srcdir)/build/build.mk
