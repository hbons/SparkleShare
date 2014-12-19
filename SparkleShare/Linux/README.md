## Building on Linux distributions

You can choose to build SparkleShare from source or to get the package through your distribution's repositories.
To run SparkleShare, you'll need the following packages:

**Note:** Git 1.9 changed the way local projects without a history are handled, and may cause protocol errors. Until a solution is found, it's recommended to use Git 1.8.

```
git 1.8.x
gtk-sharp3
mono-core >= 2.8
notify-sharp
webkitgtk-sharp
```

**Note:** These packages may not overlap with the packages required to perform a build, so please make sure that at least the above packages are installed. 

Optional packages:

```
gvfs (to change file/folder icons)
libappindicator (for Ubuntu integration)
curl (to make the "sparkleshare://" protocol handler work)
```


### Installing common build requirements

You will need the packages listed below for the most used Linux distributions:  

```
desktop-file-utils
intltool
libtool
mono-devel
mono-gmcs
mono-mcs
monodevelop
nant
```


### Installing additional source build requirements

Install the `gtk-sharp3` bindings from:  
https://github.com/mono/gtk-sharp  
Or on Ubuntu, get it from this PPA:  
https://launchpad.net/~meebey/+archive/mono-preview

Install the `notify-sharp` bindings from:  
https://download.gnome.org/sources/notify-sharp/3.0/

Install the `soup-sharp` and `webkitgtk-sharp` bindings from:  
https://github.com/xDarkice/soup-sharp  
https://github.com/xDarkice/webkitgtk-sharp

All with the usual:

```
./autogen.sh
make
sudo make install
```

If you're using Ubuntu, also install the `appindicator-sharp` bindings from:  
https://github.com/xDarkice/appindicator-sharp



### Starting the build

You can build and install SparkleShare like this:

```bash
$ ./configure (or ./autogen.sh if you build from the repository)
$ make
$ sudo make install
```


### Resetting SparkleShare settings

```
rm -Rf ~/SparkleShare
rm -Rf ~/.config/sparkleshare
```


### Uninstalling

```
sudo make uninstall
```
