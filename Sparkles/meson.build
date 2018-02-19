sparkles_src = ['AuthenticationInfo.cs',
    'BaseFetcher.cs',
    'BaseListener.cs',
    'BaseRepository.cs',
    'ChangeSet.cs',
    'Command.cs',
    'Configuration.cs',
    'Extensions.cs',
    'InstallationInfo.cs',
    'InstallationInfo.Directory.cs',
    'Invite.cs',
    'ListenerFactory.cs',
    'Logger.cs',
    'Preset.cs',
    'SSHAuthenticationInfo.cs',
    'SSHCommand.cs',
    'SSHFetcher.cs',
    'TcpListener.cs',
    'User.cs',
    'Watcher.cs']

# generator = custom_target('replace',
#    input: 'InstallationInfo.Directory.cs.in',
#    output: 'InstallationInfo.Directory.cs',
#    command: ['sed', 's/@VERSION@/2.0.1/g @INPUT@', '@OUTPUT@'])

sparkles = library('Sparkles',
    sources: sparkles_src,
    install: true,
    install_dir: install_dir)

