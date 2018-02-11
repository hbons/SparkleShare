# SparkleShare for Linux

To run SparkleShare, you'll need these packages:

```
curl
git >= 2.3
git-lfs >= 1.3.0
gtk-sharp3
gvfs
mono-core >= 4.0
notify-sharp
webkit2gtk-sharp
```

On Ubuntu you'll also need:

```
libappindicator-sharp
```


## Building on Linux

### Common build requirements

Make sure to have the `git` package installed when you're building on Linux.
You will need the packages listed below for the most used Linux distributions:  

```shell
# On Ubuntu 16.04:
sudo apt-get install \
  desktop-file-utils \
  git \ # Run requirement
  git-lfs \ # Run requirement
  gtk-sharp3-gapi \ # To build webkit2-sharp
  intltool \
  libappindicator3-0.1-cil-dev \
  libdbus-glib2.0-cil-dev \
  libgtk3.0-cil-dev \
  libsoup2.4-dev \
  libtool-bin \
  libwebkit2gtk-4.0 \
  mono-devel \
  mono-mcs \
  nant \
  xsltproc

# On Fedora 27:
sudo dnf install \
  git \ # Run requirement
  git-lfs \ # Run requirement
  gtk-sharp3-devel \
  gtk-sharp3-gapi \ # To build webkit2-sharp
  mono-devel \
  notify-sharp3-devel \
  webkitgtk4-devel \
  webkit2-sharp
```


### Additional source build requirements

Install the `gtk-sharp3` bindings from:  
https://github.com/mono/gtk-sharp  
Or on Ubuntu, get it from this PPA:  
https://launchpad.net/~meebey/+archive/mono-preview

Install the `notify-sharp` bindings from:  
https://download.gnome.org/sources/notify-sharp/3.0/

Install the `soup-sharp` and `webkit2gtk-sharp` bindings from:  
https://github.com/hbons/soup-sharp  
https://github.com/hbons/webkit2gtk-sharp

All with the usual:

```bash
./autogen.sh
make
sudo make install
```

If you're using Ubuntu, also install the `appindicator-sharp` bindings from:  
https://github.com/hbons/appindicator-sharp


### Start the build

You can build and install SparkleShare like this:

```bash
$ ./configure (or ./autogen.sh if you build from the repository)
$ make
$ sudo make install
```


### Uninstall

```bash
sudo make uninstall
```


### Reset SparkleShare settings

```bash
rm -Rf ~/SparkleShare
rm -Rf ~/.config/org.sparkleshare.SparkleShare
```

