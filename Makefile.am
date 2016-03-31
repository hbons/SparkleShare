basedirs = build Sparkles Sparkles/Git

SUBDIRS = $(basedirs) $(GUISUBDIRS)
DIST_SUBDIRS = $(basedirs) SparkleShare

EXTRA_DIST = \
	News.txt \
	README.md \
	legal/License_for_SparkleShare.txt \
	legal/License_for_Sparkles.txt \
	legal/Authors.txt \
	SparkleShare/Linux/README.md \
	SparkleShare/Linux/org.sparkleshare.SparkleShare.appdata.xml

DISTCLEANFILES = \
	intltool-extract \
	intltool-merge \
	intltool-update \
	gnome-doc-utils.make

clean-local:
	rm -rf $(top_builddir)/bin

