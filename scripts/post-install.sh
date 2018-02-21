#!/bin/sh

echo "-> gtk-update-icon-cache"
gtk-update-icon-cache --quiet --force --ignore-theme-index ${MESON_INSTALL_PREFIX}/share/icons/hicolor

echo "-> update-desktop-database"
update-desktop-database --quiet ${MESON_INSTALL_PREFIX}/share/applications

