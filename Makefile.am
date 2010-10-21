SUBDIRS =	\
	build	\
	help	\
	NotifySharp \
	SmartIrc4net \
	. \
	SparkleLib \
	SparkleShare \
	data \
	po

EXTRA_DIST = gnome-doc-utils.make

DISTCLEANFILES = \
	intltool-extract        \
	intltool-merge          \
	intltool-update		\
	gnome-doc-utils.make

clean-local: clean-gitsharp
	rm -rf $(top_builddir)/bin

include $(top_srcdir)/build/gitsharp.mk
