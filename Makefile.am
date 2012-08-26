basedirs = build SparkleLib SparkleLib/Git

SUBDIRS = $(basedirs) $(GUISUBDIRS)
DIST_SUBDIRS = $(basedirs) SparkleShare

EXTRA_DIST = \
	NEWS \
        README.md \
	legal/LICENSE.txt \
	legal/TRADEMARK.txt \
	legal/AUTHORS.txt

DISTCLEANFILES = \
	intltool-extract        \
	intltool-merge          \
	intltool-update		\
	gnome-doc-utils.make

clean-local:
	rm -rf $(top_builddir)/bin
