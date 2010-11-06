SUBDIRS = \
	icons \
	html

EXTRA_DIST = \
	sparkleshare-gnome.svg \
	sparkleshare-mist.svg \
	sparkleshare-about.png \
	side-splash.png

MAINTAINERCLEANFILES = \
	Makefile.in

install-data-local:
	$(mkinstalldirs) $(pkgdatadir)/pixmaps
	$(INSTALL_DATA) $(srcdir)/side-splash.png $(pkgdatadir)/pixmaps/side-splash.png
	$(INSTALL_DATA) $(srcdir)/sparkleshare-about.png $(pkgdatadir)/pixmaps/sparkleshare-about.png
	
uninstall-hook:
	rm -f $(pkgdatadir)/pixmaps/side-splash.png
	rm -f $(pkgdatadir)/pixmaps/sparkleshare-about.png
