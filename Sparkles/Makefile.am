ASSEMBLY = Sparkles
TARGET = library

ASSEMBLY_INFO_SOURCE = InstallationInfo.Directory.cs

SOURCES =  \
	AuthenticationInfo.cs \
	InstallationInfo.cs \
	BaseFetcher.cs \
	BaseListener.cs \
	BaseRepository.cs \
	ChangeSet.cs \
	Command.cs \
	Configuration.cs \
	Extensions.cs \
	Invite.cs \
	ListenerFactory.cs \
	Logger.cs \
	Preset.cs \
	SSHAuthenticationInfo.cs \
	SSHFetcher.cs \
	TcpListener.cs \
	User.cs \
	Watcher.cs

install-data-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;

uninstall-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;

include $(top_srcdir)/build/build.mk

