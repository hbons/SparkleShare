ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.TextTemplating
ASSEMBLY = $(ADDIN_BUILD)/TextTransform.exe

DEPS = $(top_builddir)/build/AddIns/MonoDevelop.TextTemplating/Mono.TextTemplating.dll

REFS =  \
	-r:System \
	-r:System.Core

FILES =  \
	AssemblyInfo.cs \
	Options.cs \
	TextTransform.cs

RES =

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:exe $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

check: all

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.TextTemplating
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

