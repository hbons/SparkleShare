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

ASSEMBLY = GitSharp.dll
ASSEMBLY_FILE = $(top_builddir)/bin/$(ASSEMBLY)

all-local: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE):
	@pushd GitSharp; \
	$(MONO) tools/nant/NAnt.exe -buildfile:GitSharp.build -D:path.build.output=$(top_builddir)/../bin compile-gitsharp; \
	popd;

clean-local:
	rm -rf $(top_builddir)/bin

EXTRA_DIST = gnome-doc-utils.make

DISTCLEANFILES = \
	intltool-extract        \
	intltool-merge          \
	intltool-update		\
	gnome-doc-utils.make

