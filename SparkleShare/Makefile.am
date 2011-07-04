SUBDIRS =	\
	Nautilus \
	Mac

ASSEMBLY = SparkleShare
TARGET = exe

LINK = $(REF_SPARKLESHARE) $(NOTIFY_SHARP_LIBS) $(WEBKIT_SHARP_LIBS)

if HAVE_APP_INDICATOR
BUILD_DEFINES="-define:HAVE_APP_INDICATOR"
endif

SOURCES =  \
	SparkleAbout.cs \
	SparkleBubble.cs \
	SparkleBubblesController.cs \
	SparkleController.cs \
	SparkleEntry.cs \
	SparkleEventLog.cs \
	SparkleEventLogController.cs \
	SparkleInfobar.cs \
	SparkleIntro.cs \
	SparkleLinController.cs \
	SparkleSetupController.cs \
	SparkleShare.cs \
	SparkleSpinner.cs \
	SparkleStatusIcon.cs \
	SparkleStatusIconController.cs \
	SparkleUI.cs \
	SparkleUIHelpers.cs \
	SparkleWindow.cs

include $(top_srcdir)/build/build.mk

bin_SCRIPTS = sparkleshare

dist_man_MANS = $(top_srcdir)/man/sparkleshare.1

Applicationsdir = $(datadir)/applications
dist_Applications_DATA = sparkleshare.desktop
