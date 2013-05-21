# Initializers
MONO_BASE_PATH = 

# Install Paths
DEFAULT_INSTALL_DIR = $(pkglibdir)
DIR_BIN = $(top_builddir)/bin

# External libraries to link against, generated from configure
LINK_SYSTEM = -r:System
LINK_SYSTEM_WEB = -r:System.Web
LINK_MONO_POSIX = -r:Mono.Posix

LINK_GLIB = $(GLIBSHARP_LIBS)
LINK_GTK = $(GTKSHARP_LIBS)
LINK_GNOME = $(GNOME_SHARP_LIBS)
LINK_DBUS = $(NDESK_DBUS_LIBS) $(NDESK_DBUS_GLIB_LIBS)
LINK_DBUS_NO_GLIB = $(NDESK_DBUS_LIBS)
LINK_APP_INDICATOR = $(APP_INDICATOR_LIBS)

REF_NOTIFY_SHARP = $(LINK_SYSTEM) $(LINK_DBUS) $(GTKSHARP_LIBS) $(GLIBSHARP_LIBS)
LINK_NOTIFY_SHARP = -r:$(DIR_BIN)/NotifySharp.dll
LINK_NOTIFY_SHARP_DEPS = $(REF_NOTIFY_SHARP) $(LINK_NOTIFY_SHARP)

REF_SPARKLELIB = $(LINK_SYSTEM) $(LINK_MONO_POSIX)
LINK_SPARKLELIB = -r:$(DIR_BIN)/SparkleLib.dll
LINK_SPARKLELIB_DEPS = $(REF_SPARKLELIB) $(LINK_SPARKLELIB)

REF_SPARKLESHARE = $(LINK_SYSTEM_WEB) $(LINK_DBUS) $(LINK_GTK) $(LINK_SPARKLELIB_DEPS) $(LINK_APP_INDICATOR)

# Cute hack to replace a space with something
colon:= :
empty:=
space:= $(empty) $(empty)

# Build path to allow running uninstalled
RUN_PATH = $(subst $(space),$(colon), $(MONO_BASE_PATH))

