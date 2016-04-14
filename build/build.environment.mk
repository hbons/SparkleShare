# Initializers
MONO_BASE_PATH = 

# Install paths
DEFAULT_INSTALL_DIR = $(pkglibdir)
DIR_BIN = $(top_builddir)/bin

# External libraries to link against, generated from configure
LINK_SYSTEM = -r:System
LINK_MONO_POSIX = -r:Mono.Posix

LINK_GLIB = $(GLIB_SHARP_LIBS)
LINK_GTK = $(GTK_SHARP_LIBS)
LINK_WEBKIT = $(WEBKIT2_SHARP_LIBS)
LINK_APP_INDICATOR = $(APP_INDICATOR_LIBS)

REF_SPARKLES = $(LINK_SYSTEM) $(LINK_MONO_POSIX)
LINK_SPARKLES = -r:$(DIR_BIN)/Sparkles.dll -r:$(DIR_BIN)/Sparkles.Git.dll
LINK_SPARKLES_DEPS = $(REF_SPARKLES) $(LINK_SPARKLES)

REF_SPARKLESHARE = $(LINK_GTK) $(LINK_SPARKLES_DEPS) $(LINK_APP_INDICATOR) $(LINK_WEBKIT)

# Cute hack to replace a space with something
colon:= :
empty:=
space:= $(empty) $(empty)

# Build path to allow running uninstalled
RUN_PATH = $(subst $(space),$(colon), $(MONO_BASE_PATH))

