gnome_theme = gnome
gnome_themedir = /usr/share/icons/$(gnome_theme)

gnome_theme_icons = apps,sparkleshare-symbolic.svg

install_icon_exec = $(top_srcdir)/build/icon-theme-installer \
	-t "$(gnome_theme)" \
	-s "$(srcdir)" \
	-d "x$(DESTDIR)" \
	-m "$(mkinstalldirs)" \
	-x "$(INSTALL_DATA)"

install-data-local:
	@-$(install_icon_exec) -i -b "$(gnome_themedir)" $(gnome_theme_icons)

uninstall-hook:
	@-$(install_icon_exec) -u -b "$(gnome_themedir)" $(gnome_theme_icons)

MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(wildcard *.png *.svg)
