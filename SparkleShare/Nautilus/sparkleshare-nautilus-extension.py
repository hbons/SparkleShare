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


import gio
import nautilus
import os

SPARKLESHARE_DIR = os.path.expanduser ('~') + '/SparkleShare'

class SparkleShareExtension (nautilus.MenuProvider):

    def __init__ (self):

        name = 'Loaded Nautilus SparkleShareExtension.'		


    def get_file_items (self, window, files):

        # Only work when one file is selected
        if len (files) != 1:
            return

        # Get info about the selected file
        file_reference = gio.File (files [0].get_uri ())

		# Only work if in a SparkleShare repo
        if file_reference.get_path () [:len(SPARKLESHARE_DIR)] != SPARKLESHARE_DIR:
            return

        submenu = nautilus.Menu ()

        # Create the submenu for the nautilus context menu
        item = nautilus.MenuItem ('Nautilus::OpenOlderVersion', 'Open Older Version',
                                  'Make a copy of an older version of this document in SparkleShare')
        item.set_submenu (submenu)

        os.chdir (file_reference.get_parent ().get_path ())
        command = os.popen ("git log --pretty=oneline")

        for line in command.readlines ():
            submenu.append_item (nautilus.MenuItem ('Nautilus::Version', line,
                                                    'Select to open a copy of this version'))

        item_open_log = nautilus.MenuItem ('Nautilus::OpenEventLog', 'Open Event Log' + file_reference.get_path (),
                                           'Open the event log for this document to see more versions')
		
        submenu.append_item(item_open_log)

        return item,
