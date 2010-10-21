SUBDIRS = \
	icons

EXTRA_DIST = \
	sparkleshare-gnome.svg \
	sparkleshare-mist.svg \
	sparkleshare-about.png \
	side-splash.png

MAINTAINERCLEANFILES = \
	Makefile.in

install-data-local:
	$(mkinstalldirs) $(DESTDIR)$(datadir)/pixmaps
	$(INSTALL_DATA) $(srcdir)/side-splash.png $(DESTDIR)$(datadir)/pixmaps/side-splash.png
	$(INSTALL_DATA) $(srcdir)/sparkleshare-about.png $(DESTDIR)$(datadir)/pixmaps/sparkleshare-about.png
	
uninstall-hook:
	rm -f $(DESTDIR)$(datadir)/pixmaps/side-splash.png
	rm -f $(DESTDIR)$(datadir)/pixmaps/sparkleshare-about.png
