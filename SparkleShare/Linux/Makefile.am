SUBDIRS = Images

ASSEMBLY = SparkleShare
TARGET = exe

LINK = $(REF_SPARKLESHARE) $(NOTIFY_SHARP_LIBS) $(WEBKITGTK_SHARP_LIBS)

if HAVE_APP_INDICATOR
BUILD_DEFINES = "-define:HAVE_APP_INDICATOR"
endif

SOURCES =  \
	../Common/SparkleShare.cs \
	../Common/AboutController.cs \
	../Common/Avatars.cs \
	../Common/BubblesController.cs \
	../Common/BaseController.cs \
	../Common/EventLogController.cs \
	../Common/NoteController.cs \
	../Common/SetupController.cs \
	../Common/StatusIconController.cs \
	Userinterface/About.cs \
	Userinterface/Bubbles.cs \
	Userinterface/EventLog.cs \
	Userinterface/Note.cs \
	Userinterface/Setup.cs \
	Userinterface/SetupWindow.cs \
	Userinterface/StatusIcon.cs \
	Userinterface/UserInterface.cs \
	Userinterface/UserInterfaceHelpers.cs \
	Controller.cs

include $(top_srcdir)/build/build.mk

bin_SCRIPTS = sparkleshare

Applicationsdir = $(datadir)/applications
dist_Applications_DATA = \
	org.sparkleshare.SparkleShare.desktop \
	org.sparkleshare.SparkleShare.Invites.desktop \
        SparkleShare.Autostart.desktop

install-data-hook:
	test -f $(datadir)/applications/defaults.list && \
	test -z $(DESTDIR) && update-desktop-database $(datadir)/applications || :

