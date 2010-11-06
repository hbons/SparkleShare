EXTRA_DIST = \
	day-entry.html \
	event-entry.html \
	event-log.html

MAINTAINERCLEANFILES = \
	Makefile.in

install-data-local:
	$(mkinstalldirs) $(pkgdatadir)/html
	$(INSTALL_DATA) $(srcdir)/day-entry.html $(pkgdatadir)/html/day-entry.html
	$(INSTALL_DATA) $(srcdir)/event-entry.html $(pkgdatadir)/html/event-entry.html
	$(INSTALL_DATA) $(srcdir)/event-log.html $(pkgdatadir)/html/event-log.html
	
uninstall-hook:
	rm -f $(pkgdatadir)/html/day-entry.html
	rm -f $(pkgdatadir)/html/event-entry.html
	rm -f $(pkgdatadir)/html/event-log.html
