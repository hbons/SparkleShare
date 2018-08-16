directory_info_file = configure_file(
    input: 'InstallationInfo.Directory.cs.in',
    output: 'InstallationInfo.Directory.cs',
    configuration: configuration)

sparkles_src = ['BaseFetcher.cs',
    'BaseListener.cs',
    'BaseRepository.cs',
    'ChangeSet.cs',
    'Command.cs',
    'Configuration.cs',
    'Extensions.cs',
    'InstallationInfo.cs',
    directory_info_file,
    'Invite.cs',
    'ListenerFactory.cs',
    'Logger.cs',
    'Preset.cs',
    'SSH/SSH.AuthenticationInfo.cs',
    'SSH/SSH.Command.cs',
    'SSH/SSH.Fetcher.cs',
    'TcpListener.cs',
    'User.cs',
    'Watcher.cs']


sparkles = library('Sparkles',
    sources: sparkles_src,
    cs_args: '-r:System.Xml.Linq',
    install: true,
    install_dir: install_dir)
