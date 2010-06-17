intltoolize --copy --force
aclocal -I build/m4/sparkleshare -I build/m4/shamrock -I build/m4/shave
automake --add-missing
autoconf

