SUBDIRS = ubuntu-mono-dark \
	ubuntu-mono-light\
	gnome

theme = hicolor
system_themedir = $(datadir)/icons/$(theme)
app_themedir = $(pkgdatadir)/icons/$(theme)

system_theme_icons = \
	apps,sparkleshare-16.png \
	apps,sparkleshare-22.png	\
	apps,sparkleshare-24.png \
	apps,sparkleshare-256.png \
	apps,sparkleshare-32.png \
	apps,sparkleshare-48.png \
    status,process-syncing-up-24.png \
    status,process-syncing-down-24.png \
    status,process-syncing-24.png \
    status,process-syncing-error-24.png \
    status,process-syncing-up-48.png \
    status,process-syncing-down-48.png \
    status,process-syncing-48.png \
    status,process-syncing-error-48.png

app_theme_icons = \
	animations,process-working-22.png \
	status,document-added-12.png \
	status,document-edited-12.png \
	status,document-deleted-12.png \
	status,document-moved-12.png \
	status,list-point-16.png

install_icon_exec = $(top_srcdir)/build/icon-theme-installer \
	-t "$(theme)" \
	-s "$(srcdir)" \
	-d "x$(DESTDIR)" \
	-m "$(mkinstalldirs)" \
	-x "$(INSTALL_DATA)"

install-data-local:
	@-$(install_icon_exec) -i -b "$(system_themedir)" $(system_theme_icons)
	@-$(install_icon_exec) -i -b "$(app_themedir)" $(app_theme_icons)

uninstall-hook:
	@-$(install_icon_exec) -u -b "$(system_themedir)" $(system_theme_icons)
	@-$(install_icon_exec) -u -b "$(app_themedir)" $(app_theme_icons)

MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(wildcard *.png *.svg)
