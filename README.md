# SparkleShare

SparkleShare is a collaboration and sharing tool that is designed to keep
things simple and to stay out of your way. It allows you to instantly sync
with any Git repository you have access to.

SparkleShare currently works on Linux and Mac. A Windows port and mobile
device support are planned for the future.


## License

SparkleShare is free software and licensed under the GNU GPLv3 or later. You
are welcome to change and redistribute it under certain conditions. For more
information see the LICENSE file or visit http://www.gnu.org/licenses/gpl-3.0.html


## Run on Linux

Requirements:

   - git >= 1.7.0
   - gtk-sharp2
   - gvfs
   - intltool
   - libnotify
   - mono-core >= 2.8
   - notify-sharp
   - nautilus-python
   - openssh
   - pygtk
   - webkitgtk
   - webkit-sharp

Run the service, either click the SparkleShare launcher or:

```bash
$ sparkleshare start
```

You can stop the service via the graphical interface or by typing:

```bash
$ sparkleshare stop
```

For help:

```bash
$ sparkleshare --help
```

**Note:**

   SparkleShare creates its own RSA keypair in `~/config/sparkleshare/` and uses 
   that for authentication. Please mind this if you're planning to set up your 
   own server by hand.


## Build on Linux

### Install build dependencies

#### Debian or Ubuntu (apt):

```bash
$ sudo apt-get install gtk-sharp2 mono-runtime mono-devel monodevelop \
  libndesk-dbus1.0-cil-dev nant libnotify-cil-dev libgtk2.0-cil-dev mono-mcs mono-gmcs \
  libwebkit-cil-dev intltool libtool python-nautilus libndesk-dbus-glib1.0-cil-dev
```

#### Fedora (yum):

```bash
$ sudo yum install gtk-sharp2-devel mono-core mono-devel monodevelop \
  ndesk-dbus-devel ndesk-dbus-glib-devel nautilus-python-devel nant \
  notify-sharp-devel webkit-sharp-devel webkitgtk-devel libtool intltool \
  gnome-doc-utils
```

For Ubuntu `libappindicator` support, install the following package:

```bash
$ sudo apt-get install libappindicator0.1-cil-dev 
```

You can then build and install SparkleShare like this:

```bash
$ ./configure --prefix=/usr (or ./autogen.sh if you build from the repository)
$ make
$ sudo make install
```

**Note:**  Use `--prefix=/usr` if you want the Nautilus extension to work.


## Run on Mac

Just double-click the SparkleShare bundle.


## Build on Mac

Install <tt>Xcode</tt>, the <tt>Mono</tt> Framework, <tt>MonoDevelop</tt> and the <tt>MonoMac</tt> plugin
(you can find it in <tt>MonoDevelop</tt> => <tt>Add-in Manager</tt>).

You may need to adjust some environment variables to let the build environment tools find mono:
   
```bash
$ export PATH=/Library/Frameworks/Mono.framework/Versions/Current/bin:$PATH
$ export PKG_CONFIG=/Library/Frameworks/Mono.framework/Versions/Current/bin/pkg-config
$ export PKG_CONFIG_PATH=/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig
```

Install <tt>git</tt>, <tt>automake</tt>, and <tt>intltool</tt> using <tt>MacPorts</tt>:

```bash
$ sudo port install git-core automake intltool
```
   
Start the first part of the build:

```bash
$ ./autogen.sh --enable-gtkui=no
$ make
```

Now that you have compiled the libraries, open `SparkleShare/Mac/SparkleShare.sln` in
MonoDevelop and start the build.

To create the <tt>SparkleShare.app</tt>, make sure the project is focused and select Project from the menu bar 
and click <tt>"Create Mac Installer..."</tt>. Make sure to select <tt>"Don't link assemblies"</tt>. 

Save the <tt>SparkleShare.app</tt> somewhere. Paste the contents of 
the following file in `SparkleShare.app/Contents/MonoBundle/config`:

```
https://raw.github.com/gist/1aeffa61bac73fc08eca/0c0f09ef9e36864c35f34fd5e8bf4f99886be193/gistfile1.txt
```

Copy `/Library/Frameworks/Mono.framework/Versions/Current/lib/libintl.dylib` to `SparkleShare.app/Contents/Resources`


Now you should have a working bundle that you can run.


## Info

|||
|-----------------------------------:|:--------------------------|
|     **Official website**: | http://www.sparkleshare.org/ |
|          **Source code**: | http://github.com/SparkleShare/ |
|          **IRC Channel**: | #sparkleshare on irc.gnome.org |
|                 **Wiki**: | http://github.com/hbons/SparkleShare/wiki/ |
|        **Report issues**: | http://github.com/hbons/SparkleShare/issues/ |
|  **Translation project**: | http://www.transifex.net/projects/p/sparkleshare/ |


Now have fun and create cool things together! :)
