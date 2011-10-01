SOURCES =  \
	sparkleshare-nautilus-extension.py

if NAUTILUS_EXTENSION_ENABLED
NAUTILUS_PYTHON_INSTALL_DIR=$(subst $(NAUTILUS_PREFIX)/,${prefix}/,$(NAUTILUS_PYTHON_DIR))
extensiondir = $(NAUTILUS_PYTHON_INSTALL_DIR)
extension_SCRIPTS = $(addprefix $(srcdir)/, $(SOURCES))
else
EXTRA_DIST = $(SOURCES) sparkleshare-nautilus-extension.py.in
endif
