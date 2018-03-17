# Install app icons in system theme
theme_dir = join_paths(get_option('prefix'), 'share', 'icons', 'hicolor')
icon_sizes = ['16', '22', '24', '32', '48', '256', '512']
app_icon_name = 'org.sparkleshare.SparkleShare'

foreach size : icon_sizes
    icon_dir = join_paths(size + 'x' + size, 'apps')

    install_data(
        sources: join_paths('hicolor', icon_dir, app_icon_name + '.png'),
        install_dir: join_paths(theme_dir, icon_dir))
endforeach


# Don't forget the symbolic icons
install_data(
    sources: join_paths('hicolor', 'symbolic', 'apps', app_icon_name + '-symbolic.svg'),
    install_dir: join_paths(theme_dir, 'symbolic', 'apps'))


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

sizes = ['24', '48']

foreach size : sizes
    install_data(sources: join_paths('hicolor', size + 'x' + size, category, 'process-syncing.png'),
        install_dir: join_paths(app_theme_dir, size + 'x' + size, category))

    install_data(sources: join_paths('hicolor', size + 'x' + size, category, 'process-syncing-up.png'),
        install_dir: join_paths(app_theme_dir, size + 'x' + size, category))

    install_data(sources: join_paths('hicolor', size + 'x' + size, category, 'process-syncing-down.png'),
        install_dir: join_paths(app_theme_dir, size + 'x' + size, category))

    install_data(sources: join_paths('hicolor', size + 'x' + size, category, 'process-syncing-error.png'),
        install_dir: join_paths(app_theme_dir, size + 'x' + size, category))
endforeach

