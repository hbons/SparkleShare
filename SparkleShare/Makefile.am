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
	SparkleLog.cs \
	SparkleShare.cs \
	SparkleSpinner.cs \
	SparkleStatusIcon.cs \
	SparkleUI.cs \
	SparkleWindow.cs

include $(top_srcdir)/build/build.mk

bin_SCRIPTS = sparkleshare

Applicationsdir = $(datadir)/applications
dist_Applications_DATA = sparkleshare.desktop
