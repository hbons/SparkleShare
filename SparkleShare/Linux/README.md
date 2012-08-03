## Building on Linux distributions

You can choose to build SparkleShare from source or to get the package through your distribution's repositories.

To run SparkleShare, you'll need the following packages:

```
curl
git >= 1.7.3
gtk-sharp2
mono-core >= 2.8
notify-sharp
webkit-sharp
```

**Note:** These packages may not overlap with the packages required to perform a build, so please make sure that at least the above packages are installed.

Optional packages:

```
gvfs (to change file/folder icons)
libappindicator (for Ubuntu integration)
nautilus-python (for Nautilus integration)
```

### Installing build requirements

You can use one of the commands listed below for the most used Linux distributions:


#### Ubuntu

```bash
$ sudo apt-get install libappindicator0.1-cil-dev gtk-sharp2 mono-runtime mono-devel \
  monodevelop libndesk-dbus1.0-cil-dev nant libnotify-cil-dev libgtk2.0-cil-dev mono-mcs \
  mono-gmcs libwebkit-cil-dev intltool libtool python-nautilus libndesk-dbus-glib1.0-cil-dev
```

#### Fedora

```bash
$ sudo yum install gtk-sharp2-devel mono-core mono-devel monodevelop \
  ndesk-dbus-devel ndesk-dbus-glib-devel nautilus-python-devel nant \
  notify-sharp-devel webkit-sharp-devel webkitgtk-devel libtool intltool \
  desktop-file-utils
```

#### Debian

```bash
$ sudo apt-get install gtk-sharp2 mono-runtime mono-devel monodevelop \
  libndesk-dbus1.0-cil-dev nant libnotify-cil-dev libgtk2.0-cil-dev mono-mcs mono-gmcs \
  libwebkit-cil-dev intltool libtool python-nautilus libndesk-dbus-glib1.0-cil-dev \
  desktop-file-utils
```

#### openSUSE

```bash
$ sudo zypper install gtk-sharp2 mono-core mono-devel monodevelop \
  ndesk-dbus-glib-devel python-nautilus-devel nant desktop-file-utils \
  notify-sharp-devel webkit-sharp libwebkitgtk-devel libtool intltool
```

### Starting the build

You can build and install SparkleShare like this:

```bash
$ ./configure --prefix=/usr (or ./autogen.sh if you build from the repository)
$ make
$ sudo make install
```

**Note:** The Nautilus extension will only be enabled if you build with `--prefix=/usr`.
**Note:** If there is no `configure` file, first run `./autogen.sh`


### Resetting SparkleShare settings

```
rm -Rf ~/SparkleShare
rm -Rf ~/.config/sparkleshare
```

### Uninstalling

```
sudo make uninstall
```

