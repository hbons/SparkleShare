basedirs = build SparkleLib SparkleLib/Git po

SUBDIRS = $(basedirs) $(GUISUBDIRS)
DIST_SUBDIRS = $(basedirs) SparkleShare

EXTRA_DIST = \
	NEWS \
        README.md \
	legal/LICENSE \
	legal/TRADEMARK \
	legal/AUTHORS

DISTCLEANFILES = \
	intltool-extract        \
	intltool-merge          \
	intltool-update		\
	gnome-doc-utils.make

clean-local:
	rm -rf $(top_builddir)/bin
