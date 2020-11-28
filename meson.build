project('SparkleShare', 'cs')

install_dir = 'share/sparkleshare'

configuration = configuration_data()
configuration.set('ABS_INSTALL_DIR', join_paths(get_option('prefix'), install_dir))

# This line is changed by scripts/bump-version.sh, so keep it intact
configuration.set('VERSION', '3.38')

subdir('Sparkles')
subdir('Sparkles/Git')
subdir('SparkleShare')

meson.add_install_script('scripts/post-install.sh')

