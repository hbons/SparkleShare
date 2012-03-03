SUBDIRS =	Nautilus

ASSEMBLY = SparkleShare
TARGET = exe

LINK = $(REF_SPARKLESHARE) $(NOTIFY_SHARP_LIBS) $(WEBKIT_SHARP_LIBS)

if HAVE_APP_INDICATOR
BUILD_DEFINES="-define:HAVE_APP_INDICATOR"
endif

SOURCES =  \
	SparkleAbout.cs \
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
