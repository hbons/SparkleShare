ASSEMBLY = Sparkles.Git
TARGET = library

LINK = -r:$(DIR_BIN)/Sparkles.dll

SOURCES =  \
	SubversionCommand.cs \
	SubversionFetcher.cs \
	SubversionRepository.cs

install-data-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;

uninstall-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;

include $(top_srcdir)/build/build.mk
