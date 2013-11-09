## Building on GNOME 3

SparkleShare now successfully runs on the GNOME 3 platform!
Here's how you compile it against GTK+3 and other dependencies.

You will still need to have the regular build dependencies installed:
    https://github.com/hbons/SparkleShare/blob/master/SparkleShare/Linux/README.md

The C# bindings that SparkleShare uses need to be recompiled against GTK+3, so
you will need to get the the dependencies from the specific locations listed
below, where they are GTK+3-enabled.

Install the `gtk-sharp3` bindings from:  
https://github.com/mono/gtk-sharp  
Or on Ubuntu, get it from this PPA:  
https://launchpad.net/~meebey/+archive/mono-preview

Install the `notify-sharp` bindings from:
https://github.com/meebey/notify-sharp

Install the `webkitgtk-sharp` bindings from:
https://github.com/xDarkice/webkitgtk-sharp

All with the usual:

```
./autogen.sh --prefix=/usr
make
sudo make install
```

### Ubuntu

If you're using Ubuntu, install `appindicator-sharp` bindings from:
https://github.com/xDarkice/appindicator-sharp

