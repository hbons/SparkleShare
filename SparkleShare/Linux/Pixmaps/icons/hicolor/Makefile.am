theme = hicolor
system_themedir = $(datadir)/icons/$(theme)
app_themedir = $(pkgdatadir)/icons/$(theme)

if BUILD_XDG_APP
system_themedir = $(app_themedir)
endif

system_theme_icons = \
	apps,org.sparkleshare.SparkleShare-16.png \
	apps,org.sparkleshare.SparkleShare-symbolic.svg \
	apps,org.sparkleshare.SparkleShare-22.png \
	apps,org.sparkleshare.SparkleShare-24.png \
	apps,org.sparkleshare.SparkleShare-32.png \
	apps,org.sparkleshare.SparkleShare-48.png \
        apps,org.sparkleshare.SparkleShare-256.png

app_theme_icons = \
	status,document-added-12.png \
	status,document-edited-12.png \
	status,document-deleted-12.png \
	status,document-moved-12.png \
	status,list-point-16.png \
        status,process-syncing-up-24.png \
        status,process-syncing-down-24.png \
        status,process-syncing-24.png \
        status,process-syncing-error-24.png \
        status,process-syncing-up-48.png \
        status,process-syncing-down-48.png \
        status,process-syncing-48.png \
        status,process-syncing-error-48.png

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

