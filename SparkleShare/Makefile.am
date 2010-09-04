SUBDIRS =	\
	Nautilus

ASSEMBLY = SparkleShare
TARGET = exe

LINK = $(REF_SPARKLESHARE)


SOURCES =  \
	SparkleBubble.cs \
	SparkleEntry.cs \
	SparkleIntro.cs \
	SparkleInvitation.cs \
	SparkleLink.cs \
	SparkleLog.cs \
	SparkleShare.cs \
	SparkleSpinner.cs \
	SparkleStatusIcon.cs \
	SparkleUI.cs \
	SparkleUIHelpers.cs \
	SparkleWindow.cs

include $(top_srcdir)/build/build.mk

bin_SCRIPTS = sparkleshare

dist_man_MANS = $(top_srcdir)/man/sparkleshare.1

Applicationsdir = $(datadir)/applications
dist_Applications_DATA = sparkleshare.desktop
