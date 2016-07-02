light_theme = ubuntu-mono-light
light_themedir = /usr/share/icons/$(light_theme)

light_theme_icons = \
	status,process-syncing-idle-24.png \
	status,process-syncing-up-24.png \
	status,process-syncing-down-24.png \
	status,process-syncing-24.png \
	status,process-syncing-error-24.png

install_icon_exec = $(top_srcdir)/build/icon-theme-installer \
	-t "$(light_theme)" \
	-s "$(srcdir)" \
	-d "x$(DESTDIR)" \
	-m "$(mkinstalldirs)" \
	-x "$(INSTALL_DATA)"

install-data-local:
	@-$(install_icon_exec) -i -b "$(light_themedir)" $(light_theme_icons)

uninstall-hook:
	@-$(install_icon_exec) -u -b "$(light_themedir)" $(light_theme_icons)

MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(wildcard *.png *.svg)
