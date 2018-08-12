sparkleshare_src = ['../Common/SparkleShare.cs',
    '../Common/AboutController.cs',
    '../Common/Avatars.cs',
    '../Common/BubblesController.cs',
    '../Common/BaseController.cs',
    '../Common/EventLogController.cs',
    '../Common/NoteController.cs',
    '../Common/Page.cs',
    '../Common/SetupController.cs',
    '../Common/StatusIconController.cs',
    'About.cs',
    'Bubbles.cs',
    'Controller.cs',
    'EventLog.cs',
    'Note.cs',
    'Pages/Page.Add.cs',
    'Pages/Page.Address.cs',
    'Pages/Page.Setup.cs',
    'Pages/Page.Crypto.cs',
    'Pages/Page.Syncing.cs',
    'Pages/Page.Storage.cs',
    'Pages/Page.Error.cs',
    'Pages/Page.Finished.cs',
    'Pages/Page.Invite.cs',
    'Setup.cs',
    'SetupWindow.cs',
    'StatusIcon.cs',
    'UserInterface.cs',
    'UserInterfaceHelpers.cs']


# Startup script
configure_file(
    input: 'sparkleshare.in',
    output: 'sparkleshare',
    configuration: configuration, install_dir: get_option('bindir'))


# .desktop and .appdata files
apps_dir = join_paths(get_option('prefix'), 'share', 'applications')
install_data(sources: 'org.sparkleshare.SparkleShare.Invites.desktop', install_dir: apps_dir)
install_data(sources: 'SparkleShare.Autostart.desktop', install_dir: apps_dir)
install_data(sources: 'org.sparkleshare.SparkleShare.appdata.xml', install_dir: join_paths(get_option('prefix'), 'share', 'appdata'))

if get_option('nightly')
    install_data(sources: 'org.sparkleshare.SparkleShare.Nightly.desktop',
        install_dir: apps_dir,
        rename: 'org.sparkleshare.SparkleShare.desktop')
else
    install_data(sources: 'org.sparkleshare.SparkleShare.desktop',
        install_dir: apps_dir)
endif


# Build SparkleShare
gtk = dependency('gtk-sharp-3.0')
gdk = dependency('gdk-sharp-3.0')
gio = dependency('gio-sharp-3.0')
glib = dependency('glib-sharp-3.0')
webkit = dependency('webkit2-sharp-4.0')
notify = dependency('notify-sharp-3.0')
appindicator = dependency('appindicator3-sharp-0.1', required: get_option('ubuntu'))

args = '-r:Mono.Posix'

if appindicator.found()
   args = [args, '-define:HAVE_APP_INDICATOR']
endif

sparkleshare = executable('SparkleShare',
    dependencies: [gtk, gdk, gio, glib, webkit, notify, appindicator],
    link_with: [sparkles, sparkles_git],
    cs_args: args,
    sources: sparkleshare_src,
    install: true,
    install_dir: install_dir)


subdir('Images')
subdir('Images/icons')

