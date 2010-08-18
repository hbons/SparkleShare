SUBDIRS = \
	icons

EXTRA_DIST = \
	sparkleshare-gnome.svg \
	sparkleshare-mist.svg \
	work-status-icons.svg \
	side-splash.png

MAINTAINERCLEANFILES = \
	Makefile.in

install-data-local:
	$(mkinstalldirs) $(DESTDIR)$(datadir)/pixmaps
	$(INSTALL_DATA) $(srcdir)/side-splash.png $(DESTDIR)$(datadir)/pixmaps/side-splash.png
	
uninstall-hook:
	rm -f $(DESTDIR)$(datadir)/pixmaps/side-splash.png
