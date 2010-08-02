# Initializers
MONO_BASE_PATH = 

# Install Paths
DEFAULT_INSTALL_DIR = $(pkglibdir)


## Directories
DIR_DOCS = $(top_builddir)/docs

DIR_ICONS = $(top_builddir)/icons
DIR_NOTIFYSHARP = $(top_builddir)/notify-sharp
DIR_SRC = $(top_builddir)/src

DIR_BIN = $(top_builddir)/bin


# External libraries to link against, generated from configure
LINK_SYSTEM = -r:System
LINK_MONO_POSIX = -r:Mono.Posix

LINK_GLIB = $(GLIBSHARP_LIBS)
LINK_GTK = $(GTKSHARP_LIBS)
LINK_GNOME = $(GNOME_SHARP_LIBS)
LINK_DBUS = $(NDESK_DBUS_LIBS) $(NDESK_DBUS_GLIB_LIBS)
LINK_DBUS_NO_GLIB = $(NDESK_DBUS_LIBS)


REF_NOTIFY_SHARP = $(LINK_SYSTEM) $(LINK_DBUS) $(GTKSHARP_LIBS) $(GLIBSHARP_LIBS)
LINK_NOTIFY_SHARP = -r:$(DIR_BIN)/NotifySharp.dll
LINK_NOTIFY_SHARP_DEPS = $(REF_NOTIFY_SHARP) $(LINK_NOTIFY_SHARP)

REF_FRIENDFACE = $(LINK_SYSTEM) $(LINK_GTK) $(LINK_MONO_POSIX)
LINK_FRIENDFACE = -r:$(DIR_BIN)/FriendFace.dll
LINK_FRIENDFACE_DEPS = $(REF_FRIENDFACE) $(LINK_FRIENDFACE)

REF_SPARKLELIB = $(LINK_SYSTEM) $(LINK_GTK) $(LINK_MONO_POSIX)
LINK_SPARKLELIB = -r:$(DIR_BIN)/SparkleLib.dll
LINK_SPARKLELIB_DEPS = $(REF_SPARKLELIB) $(LINK_SPARKLELIB)

REF_SPARKLESHARE = $(LINK_DBUS) $(LINK_NOTIFY_SHARP_DEPS) $(LINK_SPARKLELIB_DEPS)

REF_SPARKLEDIFF = $(LINK_FRIENDFACE_DEPS) $(LINK_SPARKLELIB_DEPS)

# Cute hack to replace a space with something
colon:= :
empty:=
space:= $(empty) $(empty)

# Build path to allow running uninstalled
RUN_PATH = $(subst $(space),$(colon), $(MONO_BASE_PATH))

