SUBDIRS = Pixmaps

ASSEMBLY = SparkleShare
TARGET = exe

LINK = $(REF_SPARKLESHARE) $(NOTIFY_SHARP_LIBS) $(WEBKIT_SHARP_LIBS)

if HAVE_APP_INDICATOR
BUILD_DEFINES="-define:HAVE_APP_INDICATOR"
endif

SOURCES =  \
	../Program.cs \
	../SparkleAboutController.cs \
	../SparkleBubblesController.cs \
	../SparkleControllerBase.cs \
	../SparkleEventLogController.cs \
	../SparkleExtensions.cs \
	../SparkleInvite.cs \
	../SparkleKeys.cs \
	../SparklePlugin.cs \
	../SparkleSetupController.cs \
	../SparkleStatusIconController.cs \
	SparkleAbout.cs \
	../SparkleAvatars.cs \
	SparkleBubbles.cs \
	SparkleController.cs \
	SparkleEventLog.cs \
	SparkleSetup.cs \
	SparkleSetupWindow.cs \
	SparkleSpinner.cs \
	SparkleStatusIcon.cs \
	SparkleUI.cs \
	SparkleUIHelpers.cs

include $(top_srcdir)/build/build.mk

bin_SCRIPTS = sparkleshare

Applicationsdir = $(datadir)/applications
dist_Applications_DATA = sparkleshare.desktop \
	sparkleshare-invite-opener.desktop

install-data-hook:
	test -f $(datadir)/applications/defaults.list && \
	test -z $(DESTDIR) && update-desktop-database $(datadir)/applications || :
