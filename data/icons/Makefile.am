theme = hicolor
system_themedir = $(datadir)/icons/$(theme)
app_themedir = $(pkgdatadir)/icons/$(theme)

system_theme_icons =					\
	apps,folder-sparkleshare-16.png			\
	apps,folder-sparkleshare-22.png			\
	apps,folder-sparkleshare-24.png			\
	apps,folder-sparkleshare-256.png		\
	apps,folder-sparkleshare-32.png			\
	apps,folder-sparkleshare-48.png

app_theme_icons =					\
	animations,process-syncing-sparkleshare-24.png	\
	animations,process-working-22.png		\
	places,folder-sparkleshare-16.png		\
	places,folder-sparkleshare-22.png		\
	places,folder-sparkleshare-24.png		\
	places,folder-sparkleshare-256.png		\
	places,folder-sparkleshare-32.png		\
	places,folder-sparkleshare-48.png		\
	status,sparkleshare-syncing-error-24.png			\
	status,avatar-default-16.png			\
	status,avatar-default-22.png			\
	status,avatar-default-24.png			\
	status,avatar-default-32.png			\
	status,avatar-default-48.png			\
	status,document-added-16.png			\
	status,document-edited-16.png			\
	status,document-moved-16.png			\
	status,document-removed-16.png

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
