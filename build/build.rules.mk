UNIQUE_FILTER_PIPE = tr [:space:] \\n | sort | uniq
BUILD_DATA_DIR = $(top_builddir)/bin/share/$(PACKAGE)

# Since all other attempts failed, we currently go this way:
# This code adds the file specified in ASSEMBLY_INFO_SOURCE to SOURCES_BUILD.
ASSEMBLY_INFO_SOURCE_REAL = \
	$(addprefix $(srcdir)/, $(ASSEMBLY_INFO_SOURCE))

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))
SOURCES_BUILD += $(ASSEMBLY_INFO_SOURCE_REAL)

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(notdir $(resource)))

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(top_builddir)/bin/$(ASSEMBLY).$(ASSEMBLY_EXTENSION)

INSTALL_DIR_RESOLVED = $(firstword $(subst , $(DEFAULT_INSTALL_DIR), $(INSTALL_DIR)))

if ENABLE_TESTS
    LINK = " $(NUNIT_LIBS)"
    ENABLE_TESTS_FLAG = "-define:ENABLE_TESTS"
endif

if ENABLE_ATK
    ENABLE_ATK_FLAG = "-define:ENABLE_ATK"
endif

FILTERED_LINK = $(shell echo "$(LINK)" | $(UNIQUE_FILTER_PIPE))
DEP_LINK = $(shell echo "$(LINK)" | $(UNIQUE_FILTER_PIPE) | sed s,-r:,,g | grep '$(top_builddir)/bin/')

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

moduledir = $(INSTALL_DIR_RESOLVED)
module_SCRIPTS = $(OUTPUT_FILES)

all: $(ASSEMBLY_FILE)

run: 
	@pushd $(top_builddir); \
	make run; \
	popd;

# uncommented for now.
# tests are currently excuted from Makefile in $(top_builddir)
#test:
#	@pushd $(top_builddir)/tests; \
#	make $(ASSEMBLY); \
#	popd;

build-debug:
	@echo $(DEP_LINK)

$(ASSEMBLY_FILE).mdb: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(DEP_LINK)
	@mkdir -p $(top_builddir)/bin
	$(MCS) \
		$(GMCS_FLAGS) \
		$(ASSEMBLY_BUILD_FLAGS) \
		-codepage:utf8 \
		-nowarn:0278 -nowarn:0078 $$warn \
		-define:HAVE_GTK_2_10 -define:NET_2_0 \
		-debug -target:$(TARGET) -out:$@ \
		$(BUILD_DEFINES) $(ENABLE_TESTS_FLAG) $(ENABLE_ATK_FLAG) \
		$(FILTERED_LINK) $(RESOURCES_BUILD) $(SOURCES_BUILD)
	@if [ -e $(srcdir)/$(notdir $@.config) ]; then \
		cp $(srcdir)/$(notdir $@.config) $(top_builddir)/bin; \
	fi;
	@if [ ! -z "$(EXTRA_BUNDLE)" ]; then \
		cp $(EXTRA_BUNDLE) $(top_builddir)/bin; \
	fi;

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED)

CLEANFILES = $(OUTPUT_FILES) $(ASSEMBLY_FILE).config
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in

