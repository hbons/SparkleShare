sparkleshare_src = ['../Common/SparkleShare.cs',
    '../Common/AboutController.cs',
    '../Common/Avatars.cs',
    '../Common/BubblesController.cs',
    '../Common/BaseController.cs',
    '../Common/EventLogController.cs',
    '../Common/NoteController.cs',
    '../Common/SetupController.cs',
    '../Common/StatusIconController.cs',
    'About.cs',
    'Bubbles.cs',
    'Controller.cs',
    'EventLog.cs',
    'Note.cs',
    'Setup.cs',
    'SetupWindow.cs',
    'StatusIcon.cs',
    'UserInterface.cs',
    'UserInterfaceHelpers.cs']

gtk = dependency('gtk-sharp-3.0')
gdk = dependency('gdk-sharp-3.0')
gio = dependency('gio-sharp-3.0')
glib = dependency('glib-sharp-3.0')
webkit = dependency('webkit2-sharp-4.0')
notify = dependency('notify-sharp-3.0')

sparkleshare = executable('SparkleShare',
    dependencies: [gtk, gdk, gio, glib, webkit, notify],
    link_with: [sparkles, sparkles_git],
    cs_args: '-r:Mono.Posix',
    sources: sparkleshare_src,
    install: true,
    install_dir: install_dir)

# Install the startup script
install_data(sources: 'sparkleshare', install_dir: '/bin')

# Install .desktop and .appdata files
install_data(sources: 'org.sparkleshare.SparkleShare.desktop', install_dir: join_paths('share', 'applications'))
install_data(sources: 'org.sparkleshare.SparkleShare.Invites.desktop', install_dir: join_paths('share', 'applications'))
install_data(sources: 'SparkleShare.Autostart.desktop', install_dir: join_paths('share', 'applications'))
install_data(sources: 'org.sparkleshare.SparkleShare.appdata.xml', install_dir: join_paths('share', 'appdata'))

subdir('Images')
subdir('Images/icons')


# TODO:
# - preprocess startup script, and InstallationInfo.Directory.cs.in
# - special Ubuntu icons. custom icons to right location
# - preprocess InstallationInfo.Directory.cs.in
# - Keep around bump version script
# - update .desktop file database and icon cache

