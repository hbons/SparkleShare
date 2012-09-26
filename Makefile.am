basedirs = build SparkleLib SparkleLib/Git

SUBDIRS = $(basedirs) $(GUISUBDIRS)
DIST_SUBDIRS = $(basedirs) SparkleShare

EXTRA_DIST = \
	News.txt \
        README.md \
	legal/License.txt \
	legal/Trademark.txt \
	legal/Authors.txt

DISTCLEANFILES = \
	intltool-extract        \
	intltool-merge          \
	intltool-update		\
	gnome-doc-utils.make

clean-local:
	rm -rf $(top_builddir)/bin
