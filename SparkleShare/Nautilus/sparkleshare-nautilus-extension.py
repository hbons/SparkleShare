#   SparkleShare, an instant update workflow to Git.
#   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
#
#   This program is free software: you can redistribute it and/or modify
#   it under the terms of the GNU General Public License as published by
#   the Free Software Foundation, either version 3 of the License, or
#   (at your option) any later version.
#
#   This program is distributed in the hope that it will be useful,
#   but WITHOUT ANY WARRANTY; without even the implied warranty of
#   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#   GNU General Public License for more details.
#
#   You should have received a copy of the GNU General Public License
#   along with this program.  If not, see <http:#www.gnu.org/licenses/>.

import time
import gio
import nautilus
import os

SPARKLESHARE_DIR = os.path.expanduser ('~') + '/SparkleShare'

class SparkleShareExtension (nautilus.MenuProvider):

    def __init__ (self):

        name = "Loaded Nautilus SparkleShareExtension."

    def checkout_file (commit)
        return

    def get_file_items (self, window, files):

		# Only work when one file is selected
        if len (files) != 1:
            return

        file_reference = gio.File (files [0].get_uri ())

		# Only work if we're in a SparkleShare repo
        if file_reference.get_path () [:len (SPARKLESHARE_DIR)] != SPARKLESHARE_DIR:
            return

        item = nautilus.MenuItem ("Nautilus::OpenOlderVersion", "Get Earlier Version",
                                  "Make a copy of an earlier version in this folder")

        submenu = nautilus.Menu ()
        item.set_submenu (submenu)

        timestamps = array ([0, 0, 0, 0, 0, 0, 0, 0, 0, 0])

        os.chdir (file_reference.get_parent ().get_path ())
        time_command   = os.popen ("git log -10 --format='%at' " + file_reference.get_path ())
        author_command = os.popen ("git log -10 --format='%an' " + file_reference.get_path ())

        i = 0
        for line in time_command.readlines ():
            timestamps [i] = line
            i += 1

        i = 0
        for line in author_command.readlines ():
            timestamp = time.strftime ("%a, %d %b %Y %H:%M", time.localtime (timestamps [i]))
            submenu.append_item (nautilus.MenuItem ("Nautilus::Version" + timestamps [i], timestamp +
                                                    " " + line.strip ("\n"),
                                                    "Select to get a copy of this version"))
            i += 1

        item_open_log = nautilus.MenuItem ("Nautilus::s", "Open Event Log" + file_reference.get_path (),
                                           "Open the event log to see more versions")
		
        submenu.append_item(item_open_log)

        return item,
