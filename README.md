# SparkleShare

SparkleShare is a collaboration and sharing tool that is designed to keep
things simple and to stay out of your way. It allows you to instantly sync
with any Git repository you have access to.

SparkleShare currently works on Linux, Mac and Windows.

[![Flattr this git repo](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/thing/21770/SparkleShare-Sharing-work-made-easy)


## License

SparkleShare is free software and licensed under the GNU GPLv3 or later. You
are welcome to change and redistribute it under certain conditions. For more
information see the LICENSE file or visit http://www.gnu.org/licenses/gpl-3.0.html


## Running SparkleShare

**Note:**

   SparkleShare creates its own RSA keypair in `$HOME/.config/sparkleshare/` and uses 
   that for authentication. Please mind this if you're planning to set up your 
   own server by hand.

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


### Mac

Download, unzip and open the SparkleShare bundle.


### Windows

Download the installer and run SparkleShare from the start menu.


## Build on Linux

### Debian or Ubuntu (apt):

```bash
$ sudo apt-get install gtk-sharp2 mono-runtime mono-devel monodevelop \
  libndesk-dbus1.0-cil-dev nant libnotify-cil-dev libgtk2.0-cil-dev mono-mcs mono-gmcs \
  libwebkit-cil-dev intltool libtool python-nautilus libndesk-dbus-glib1.0-cil-dev
```

For Ubuntu `libappindicator` support, install the following package:

```bash
$ sudo apt-get install libappindicator0.1-cil-dev
```

### Fedora (yum):

```bash
$ sudo yum install gtk-sharp2-devel mono-core mono-devel monodevelop \
  ndesk-dbus-devel ndesk-dbus-glib-devel nautilus-python-devel nant \
  notify-sharp-devel webkit-sharp-devel webkitgtk-devel libtool intltool
```


You can then build and install SparkleShare like this:

```bash
$ ./configure --prefix=/usr (or ./autogen.sh if you build from the repository)
$ make
$ sudo make install
```

**Note:**  Use `--prefix=/usr` if you want the Nautilus extension to work.


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

Save the <tt>SparkleShare.app</tt> somewhere. Copy `SparkleShare/Mac/config` to
 `SparkleShare.app/Contents/MonoBundle/config` (adjust the paths to where you saved the .app):

```
cp SparkleShare/Mac/config SparkleShare.app/Contents/MonoBundle/config
```

Copy `/Library/Frameworks/Mono.framework/Versions/Current/lib/libintl.dylib` to `SparkleShare.app/Contents/Resources`


Now you should have a working bundle that you can run.

## Build on Windows

Install version 4.0 of the [.NET Framework](http://www.microsoft.com/download/en/details.aspx?id=17851) if you haven't already.

Download the latest sourcecode of SparkleShare from here and extract the contents of the ZIP file into `C:\SparkleShareSource\` (make sure that this file `README.md` lies in exactly this directory!)

Install the latest version of [msysGit](http://code.google.com/p/msysgit/downloads/). Change the install location to `C:\msysgit` and use the default settings for the other settings during the installation. Copy the `C:\msysgit` directory into `C:\SparkleShareSource\bin\msysgit`

Open a command prompt and execute the following:

```
cd C:\SparkleShareSource
cd SparkleShare\Windows
build
```

* `C:\SparkleShareSource\bin` should now contain `SparkleShare.exe`, which you can run. You may want to copy the contents of `C:\SparkleShareSource\bin` into a different directory as `C:\Program Files (x86)\SparkleShare\`.

If you want to create an installer package, install [WiX 3.6](http://wix.sourceforge.net/), restart Windows and run:

```
build installer
```
Check for errors when the building finishes. (`LICENSE.rtf` was missing for me, creating an empty file in `C:\SparkleShareSource\` fixed that for me.)

You can find the file `SparkleShare.msi` in the folder where you ran the command (likely `C:\SparkleShareSource\SparkleShare\Windows\`)

Please note: SparkleShare needs to be run with administrator priviledges in newer versions of Windows (Vista, 7). If not the application may crash (should happen during the installer installation, no worries!). To fix this crash you need to edit the preferences of the file `C:\Program Files (x86)\sparkleshare.org\SparkleShare\SparkleShare.exe` and tick the option *Run application as administrator* in the *Compatibility* tab.

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
