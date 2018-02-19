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

# Startup script
configure_file(
    input: 'sparkleshare.in',
    output: 'sparkleshare',
    configuration: configuration, install_dir: 'bin/')

# .desktop and .appdata files
install_data(sources: 'org.sparkleshare.SparkleShare.desktop', install_dir: join_paths('share', 'applications'))
install_data(sources: 'org.sparkleshare.SparkleShare.Invites.desktop', install_dir: join_paths('share', 'applications'))
install_data(sources: 'SparkleShare.Autostart.desktop', install_dir: join_paths('share', 'applications'))
install_data(sources: 'org.sparkleshare.SparkleShare.appdata.xml', install_dir: join_paths('share', 'appdata'))

# Dependencies
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

subdir('Images')
subdir('Images/icons')


# TODO:
# - install icons into right structure. Ubuntu icons. custom icons to right location
# - update .desktop file database and icon cache

