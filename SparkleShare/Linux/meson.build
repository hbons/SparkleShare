sparkleshare_src = ['../Common/SparkleShare.cs',
    '../Common/AboutController.cs',
    '../Common/Avatars.cs',
    '../Common/BubblesController.cs',
    '../Common/BaseController.cs',
    '../Common/EventLogController.cs',
    '../Common/NoteController.cs',
    '../Common/Page.cs',
    '../Common/PageController.cs',
    '../Common/PageControllers/PageController.Address.cs',
    '../Common/PageControllers/PageController.Crypto.cs',
    '../Common/PageControllers/PageController.Host.cs',
    '../Common/PageControllers/PageController.Invite.cs',
    '../Common/PageControllers/PageController.Progress.cs',
    '../Common/PageControllers/PageController.Privacy.cs',
    '../Common/PageControllers/PageController.User.cs',
    '../Common/PageControllers/PageController.Storage.cs',
    '../Common/StatusIconController.cs',
    'Controller.cs',
    'UserInterface/About.cs',
    'UserInterface/Bubbles.cs',
    'UserInterface/EventLog.cs',
    'UserInterface/Note.cs',
    'UserInterface/Setup.cs',
    'UserInterface/SetupWindow.cs',
    'UserInterface/StatusIcon.cs',
    'UserInterface/UserInterface.cs',
    'UserInterface/UserInterfaceHelpers.cs',
    'UserInterface/Pages/Page.Address.cs',
    'UserInterface/Pages/Page.Crypto.cs',
    'UserInterface/Pages/Page.Host.cs',
    'UserInterface/Pages/Page.Invite.cs',
    'UserInterface/Pages/Page.Privacy.cs',
    'UserInterface/Pages/Page.Progress.cs',
    'UserInterface/Pages/Page.Storage.cs',
    'UserInterface/Pages/Page.User.cs']


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

