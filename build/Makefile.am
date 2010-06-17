SUBDIRS = m4

DLL_MAP_VERIFIER_ASSEMBLY = dll-map-verifier.exe

ALL_TARGETS = $(DLL_MAP_VERIFIER_ASSEMBLY)

all: $(ALL_TARGETS)

$(DLL_MAP_VERIFIER_ASSEMBLY): DllMapVerifier.cs
	$(MCS) -out:$@ $<

EXTRA_DIST = \
	private-icon-theme-installer \
	DllMapVerifier.cs \
	dll-map-makefile-verifier

CLEANFILES = *.exe *.mdb
MAINTAINERCLEANFILES = Makefile.in
