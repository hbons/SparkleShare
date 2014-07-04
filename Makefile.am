basedirs = build SparkleLib SparkleLib/Git

SUBDIRS = $(basedirs) $(GUISUBDIRS)
DIST_SUBDIRS = $(basedirs) SparkleShare

EXTRA_DIST = \
	News.txt \
        README.md \
	legal/License_for_SparkleShare.txt \
	legal/License_for_SparkleLib.txt \
	legal/Trademark.txt \
	legal/Authors.txt \
	SparkleShare/Linux/README.md \
	SparkleShare/Linux/sparkleshare.appdata.xml

DISTCLEANFILES = \
	intltool-extract        \
	intltool-merge          \
	intltool-update		\
	gnome-doc-utils.make

clean-local:
	rm -rf $(top_builddir)/bin
