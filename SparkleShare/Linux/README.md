# SparkleShare for Linux

## Building on Linux

### Common build requirements

You will need the packages listed below for the most used Linux distributions (some are run requirements):

```shell
# On Ubuntu 16.04:
sudo apt-get install \
  curl \ # Run requirement
  desktop-file-utils \
  git \ # Run requirement
  git-lfs \ # Run requirement
  gtk-sharp3-gapi \ # To build webkit2-sharp
  gvfs \ # Run requirement
  libappindicator3-0.1-cil-dev \
  libdbus-glib2.0-cil-dev \
  libgtk3.0-cil-dev \
  libnotify3.0-cil-dev \
  libsoup2.4-dev \
  libwebkit2gtk-4.0 \
  meson

# On Fedora 27:
sudo dnf install \
  curl \ # Run requirement
  git \ # Run requirement
  git-lfs \ # Run requirement
  gtk-sharp3-devel \
  gtk-sharp3-gapi \ # To build webkit2-sharp
  gvfs \ # Run requirement
  meson \
  notify-sharp3-devel \
  webkitgtk4-devel \
  webkit2-sharp
```


### Additional source build requirements

Install the `soup-sharp` and `webkit2gtk-sharp` bindings from:
https://github.com/hbons/soup-sharp
https://github.com/hbons/webkit2gtk-sharp

Both with:

```bash
./autogen.sh
make
sudo make install
```

Om Ubuntu, also install the `appindicator-sharp` bindings from:
https://github.com/hbons/appindicator-sharp


### Start the build

You can build and install SparkleShare with `meson` like this:

```bash
meson build/
ninja -C build/
sudo ninja install -C build/
```


If your distribution has an out of date meson package, you can install the latest version using the Python package manager:

```bash
# Install pip using your system's package manager
sudo apt-get install python3-pip # Ubuntu
sudo dnf install python3-pip # Fedora

pip3 install meson
```


### Uninstall

```bash
sudo ninja uninstall
```


### Reset SparkleShare settings

```bash
rm -Rf ~/SparkleShare
rm -Rf ~/.config/org.sparkleshare.SparkleShare
```

