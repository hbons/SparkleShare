project('SparkleShare', 'cs')

install_dir = 'share/sparkleshare'

configuration = configuration_data()
configuration.set('ABS_INSTALL_DIR', join_paths(get_option('prefix'), install_dir))
configuration.set('VERSION', '2.0.1')

subdir('Sparkles')
subdir('Sparkles/Git')
subdir('SparkleShare')

