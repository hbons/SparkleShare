# SparkleShare for Linux

## Building on Linux

### Common build requirements

You will need the packages listed below for the most used Linux distributions:

```shell
# On Ubuntu 16.04:

# Run requirements
sudo apt-get install \
  curl \
  git \
  git-lfs \
  gvfs  

# Build requirements
sudo apt-get install \
  desktop-file-utils \
  gtk-sharp3-gapi \
  libappindicator3-0.1-cil-dev \
  libdbus-glib2.0-cil-dev \
  libgtk3.0-cil-dev \
  libnotify3.0-cil-dev \
  libsoup2.4-dev \
  libtool-bin \
  libwebkit2gtk-4.0 \
  meson \
  mono-devel \
  mono-mcs \
  xsltproc


# On Fedora 27:

# Run requirements
sudo dnf install \
  curl \
  git \
  git-lfs \
  gvfs

# Build requirements
sudo dnf install \
  gtk-sharp3-devel \
  gtk-sharp3-gapi \
  libtool \
  meson \
  notify-sharp3-devel \
  webkitgtk4-devel \
  webkit2-sharp
```


### Additional source build requirements

Install mono-complete, [see instructions](https://www.mono-project.com/download/stable/#download-lin-ubuntu)

Install these `soup-sharp` and `webkit2gtk-sharp` bindings:

```bash
git clone https://github.com/hbons/soup-sharp
cd soup-sharp/
./autogen.sh
make
sudo make install
```

```bash
git clone https://github.com/hbons/webkit2-sharp
cd webkit2-sharp/
./autogen.sh
make
sudo make install
```

On Ubuntu, also install these `appindicator-sharp` bindings:

```bash
sudo apt-get install libappindicator3-dev
git clone https://github.com/hbons/appindicator-sharp
cd appindicator-sharp/
./autogen.sh
make
sudo make install
```


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

