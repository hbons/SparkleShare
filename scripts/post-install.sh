#!/bin/sh

echo "-> gtk-update-icon-cache"
gtk-update-icon-cache --quiet --force --ignore-theme-index /usr/share/icons/hicolor > /dev/null

echo "-> update-desktop-database"
update-desktop-database --quiet /usr/share/applications > /dev/null

