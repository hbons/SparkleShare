# Install app icons in system theme
theme_dir = join_paths(get_option('prefix'), 'share', 'icons', 'hicolor')
icon_sizes = ['16', '24', '48', '256', '512']

app_icon_name = 'org.sparkleshare.SparkleShare'
source_icon_name = app_icon_name

if get_option('nightly')
    source_icon_name = app_icon_name + '.Nightly'
endif

foreach size : icon_sizes
    icon_dir = join_paths(size + 'x' + size, 'apps')

    install_data(
        sources: join_paths('hicolor', icon_dir, source_icon_name + '.png'),
        install_dir: join_paths(theme_dir, icon_dir),
        rename: app_icon_name + '.png')
endforeach

install_data(
    sources: join_paths('hicolor', 'symbolic', 'apps', app_icon_name + '-symbolic.svg'),
    install_dir: join_paths(theme_dir, 'symbolic', 'apps'))


# Install the status icons
status_icons = ['syncing',
                'syncing-up',
                'syncing-down',
                'syncing-error']

foreach icon : status_icons
    install_data(
        sources: join_paths('hicolor', '24x24', 'status', app_icon_name + '-' + icon + '.png'),
        install_dir: join_paths(theme_dir, '24x24', 'apps'))

    install_data(
        sources: join_paths('hicolor', '48x48', 'status', app_icon_name + '-' + icon + '.png'),
        install_dir: join_paths(theme_dir, '48x48', 'apps'))

    install_data(
        sources: join_paths('hicolor', 'symbolic', 'status', app_icon_name + '-' + icon + '-symbolic.svg'),
        install_dir: join_paths(theme_dir, 'symbolic', 'apps'))
endforeach


# Install other icons in app theme
app_theme_dir = join_paths(install_dir, 'icons', 'hicolor')
category = 'status'

size = '12'

install_data(sources: join_paths('hicolor', size + 'x' + size, category, 'document-added.png'),
    install_dir: join_paths(app_theme_dir, size + 'x' + size, category))

install_data(sources: join_paths('hicolor', size + 'x' + size, category, 'document-edited.png'),
    install_dir: join_paths(app_theme_dir, size + 'x' + size, category))

install_data(sources: join_paths('hicolor', size + 'x' + size, category, 'document-deleted.png'),
    install_dir: join_paths(app_theme_dir, size + 'x' + size, category))

install_data(sources: join_paths('hicolor', size + 'x' + size, category, 'document-moved.png'),
    install_dir: join_paths(app_theme_dir, size + 'x' + size, category))

size = '16'

install_data(sources: join_paths('hicolor', size + 'x' + size, category, 'list-point.png'),
    install_dir: join_paths(app_theme_dir, size + 'x' + size, category))

size = '22'

install_data(sources: join_paths('hicolor', size + 'x' + size, category, 'process-working.png'),
    install_dir: join_paths(app_theme_dir, size + 'x' + size, category))

