SOURCES =  \
	sparkleshare-nautilus-extension.py \
	sparkleshare-nautilus3-extension.py

if NAUTILUS2_EXTENSION_ENABLED
NAUTILUS_PYTHON_INSTALL_DIR=$(subst $(NAUTILUS_PREFIX)/,${prefix}/,$(NAUTILUS_PYTHON_DIR))
extensiondir = $(NAUTILUS_PYTHON_INSTALL_DIR)
extension_SCRIPTS = $(addprefix $(srcdir)/, sparkleshare-nautilus-extension.py))
endif
if NAUTILUS3_EXTENSION_ENABLED
NAUTILUS_PYTHON_INSTALL_DIR=$(subst $(NAUTILUS_PREFIX)/,${prefix}/,$(NAUTILUS_PYTHON_DIR))
extensiondir = $(NAUTILUS_PYTHON_INSTALL_DIR)
extension_SCRIPTS = $(addprefix $(srcdir)/, sparkleshare-nautilus3-extension.py)
endif
EXTRA_DIST = $(SOURCES) sparkleshare-nautilus-extension.py.in sparkleshare-nautilus3-extension.py.in
