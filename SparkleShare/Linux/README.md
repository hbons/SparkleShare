
## Running SparkleShare

### Linux

You can choose to build from source or get the packages through your distribution's repositories.

Requirements:

```
curl
git >= 1.7.3
gtk-sharp2
mono-core >= 2.8
notify-sharp
webkit-sharp
```

Optional:

```
nautilus-python
gvfs
libappindicator
```


## Building on Linux

### Ubuntu:

```bash
$ sudo apt-get install libappindicator0.1-cil-dev gtk-sharp2 mono-runtime mono-devel \
  monodevelop libndesk-dbus1.0-cil-dev nant libnotify-cil-dev libgtk2.0-cil-dev mono-mcs 
  mono-gmcs libwebkit-cil-dev intltool libtool python-nautilus libndesk-dbus-glib1.0-cil-dev
```

### Fedora:

```bash
$ sudo yum install gtk-sharp2-devel mono-core mono-devel monodevelop \
  ndesk-dbus-devel ndesk-dbus-glib-devel nautilus-python-devel nant \
  notify-sharp-devel webkit-sharp-devel webkitgtk-devel libtool intltool \
  desktop-file-utils
```

### Debian:

```bash
$ sudo apt-get install gtk-sharp2 mono-runtime mono-devel monodevelop \
  libndesk-dbus1.0-cil-dev nant libnotify-cil-dev libgtk2.0-cil-dev mono-mcs mono-gmcs \
  libwebkit-cil-dev intltool libtool python-nautilus libndesk-dbus-glib1.0-cil-dev \
  desktop-file-utils
```

### openSUSE:

```bash
$ sudo zypper install gtk-sharp2 mono-core mono-devel monodevelop \
  ndesk-dbus-glib-devel python-nautilus-devel nant desktop-file-utils \
  notify-sharp-devel webkit-sharp libwebkitgtk-devel libtool intltool
```


You can then build and install SparkleShare like this:

```bash
$ ./configure --prefix=/usr (or ./autogen.sh if you build from the repository)
$ make
$ sudo make install
```

**Note:**  The Nautilus extension will only be enabled if you build with `--prefix=/usr`.

