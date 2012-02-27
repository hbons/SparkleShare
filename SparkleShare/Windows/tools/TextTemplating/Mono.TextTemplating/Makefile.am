ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.TextTemplating
ASSEMBLY = $(ADDIN_BUILD)/Mono.TextTemplating.dll

DEPS =

REFS =  \
	-r:System \
	-r:System.Core

FILES =  \
	AssemblyInfo.cs \
	Microsoft.VisualStudio.TextTemplating/AssemblyCacheMonitor.cs \
	Microsoft.VisualStudio.TextTemplating/DirectiveProcessor.cs \
	Microsoft.VisualStudio.TextTemplating/DirectiveProcessorException.cs \
	Microsoft.VisualStudio.TextTemplating/EncodingHelper.cs \
	Microsoft.VisualStudio.TextTemplating/Engine.cs \
	Microsoft.VisualStudio.TextTemplating/Interfaces.cs \
	Microsoft.VisualStudio.TextTemplating/ParameterDirectiveProcessor.cs \
	Microsoft.VisualStudio.TextTemplating/RequiresProvidesDirectiveProcessor.cs \
	Microsoft.VisualStudio.TextTemplating/TextTemplatingSession.cs \
	Microsoft.VisualStudio.TextTemplating/TextTransformation.cs \
	Microsoft.VisualStudio.TextTemplating/ToStringHelper.cs \
	Mono.TextTemplating/CompiledTemplate.cs \
	Mono.TextTemplating/CrossAppDomainAssemblyResolver.cs \
	Mono.TextTemplating/ParsedTemplate.cs \
	Mono.TextTemplating/TemplateGenerator.cs \
	Mono.TextTemplating/TemplateSettings.cs \
	Mono.TextTemplating/TemplatingEngine.cs \
	Mono.TextTemplating/Tokeniser.cs

RES =

all: $(ASSEMBLY) $(ASSEMBLY).mdb $(DATA_FILE_BUILD)

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) -debug -out:$@ -target:library $(REFS) $(build_deps) \
		$(build_resources:%=/resource:%) $(build_sources)

$(ASSEMBLY).mdb: $(ASSEMBLY)

$(DATA_FILE_BUILD): $(srcdir)$(subst $(ADDIN_BUILD),, $@)
	mkdir -p $(ADDIN_BUILD)/Schemas
	cp $(srcdir)/$(subst $(ADDIN_BUILD),,$@) $@

check: all

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.TextTemplating
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

