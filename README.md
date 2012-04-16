# SparkleShare

SparkleShare is a collaboration and sharing tool that is designed to keep
things simple and to stay out of your way. SparkleShare is available for Linux, Mac and Windows.

SparkleShare allows you to instantly sync with any [Git](http://www.git-scm.org/) repository you have access to.

[![Flattr this git repo](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/thing/21770/SparkleShare-Sharing-work-made-easy)

## Resources

|||
|-----------------------------------:|:--------------------------|
|              **Website**: | http://www.sparkleshare.org/ |
|          **Source code**: | https://github.com/hbons/SparkleShare |
|          **IRC channel**: | #sparkleshare on irc.gnome.org |
|                 **Wiki**: | http://github.com/hbons/SparkleShare/wiki/ |
|        **Report issues**: | http://github.com/hbons/SparkleShare/issues/ |
|  **Translation project**: | http://www.transifex.net/projects/p/sparkleshare/ |


## License

SparkleShare is free software and licensed under the GNU GPLv3 or later. You
are welcome to change and redistribute it under certain conditions. For more
information see the LICENSE file or visit http://www.gnu.org/licenses/gpl-3.0.html


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


### Mac

Download, unzip and open the SparkleShare bundle.


### Windows

Download and run the installer and start SparkleShare from the Windows menu.


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
  notify-sharp-devel webkit-sharp-devel webkitgtk-devel libtool intltool
```

### Debian:

```bash
$ sudo apt-get install gtk-sharp2 mono-runtime mono-devel monodevelop \
  libndesk-dbus1.0-cil-dev nant libnotify-cil-dev libgtk2.0-cil-dev mono-mcs mono-gmcs \
  libwebkit-cil-dev intltool libtool python-nautilus libndesk-dbus-glib1.0-cil-dev
```

### openSUSE:

```bash
$ sudo zypper install gtk-sharp2 mono-core mono-devel monodevelop \
  ndesk-dbus-glib-devel python-nautilus-devel nant \
  notify-sharp-devel webkit-sharp libwebkitgtk-devel libtool intltool
```


You can then build and install SparkleShare like this:

```bash
$ ./configure --prefix=/usr (or ./autogen.sh if you build from the repository)
$ make
$ sudo make install
```

**Note:**  The Nautilus extension will only be enabled if you build with `--prefix=/usr`.


## Building on Mac

Install [Xcode](https://developer.apple.com/xcode/), the [Mono Framework](http://www.mono-project.com/) 
and [MonoDevelop](http://monodevelop.com/).

Start MonoDevelop and install the MonoMac add-in (it's in <tt>MonoDevelop</tt> > <tt>Add-in Manager</tt>).


You may need to adjust some environment variables to let the build environment tools find mono:
   
```bash
$ export PATH=/Library/Frameworks/Mono.framework/Versions/Current/bin:$PATH
$ export PKG_CONFIG=/Library/Frameworks/Mono.framework/Versions/Current/bin/pkg-config
$ export PKG_CONFIG_PATH=/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig
```

Install <tt>git</tt>, <tt>automake</tt>, <tt>libtool</tt> and <tt>intltool</tt> using <tt>MacPorts</tt>:

```bash
$ sudo port install git-core automake intltool libtool
```
   
Start the first part of the build:

```bash
$ ./autogen.sh --enable-gtkui=no
$ make
```

Now that you have compiled the libraries, open `SparkleShare/Mac/SparkleShare.sln` in
MonoDevelop and start the build.


### Creating a Mac bundle

To create the <tt>SparkleShare.app</tt>, make sure the project is focused and select Project from the menu bar 
and click <tt>"Create Mac Installer..."</tt>. Make sure to select <tt>"Don't link assemblies"</tt>. 

Save the <tt>SparkleShare.app</tt> somewhere. Copy `SparkleShare/Mac/config` to
 `SparkleShare.app/Contents/MonoBundle/config` (adjust the paths to where you saved the .app):

```
cp SparkleShare/Mac/config SparkleShare.app/Contents/MonoBundle/config
```

Copy `/Library/Frameworks/Mono.framework/Versions/Current/lib/libintl.dylib` to `SparkleShare.app/Contents/Resources`

Now you should have a working bundle that you can run.


## Building on Windows

Install version 4.0 of the [.NET Framework](http://www.microsoft.com/download/en/details.aspx?id=17851) if you haven't already.

Install [msysGit](http://code.google.com/p/msysgit/downloads/detail?name=Git-1.7.8-preview20111206.exe). Change the install location to `C:\msysgit` and use the default settings for the other settings during the installation. Copy the `C:\msysgit` directory into `bin\msysgit` (in the SparkleShare source directory).

Open a command prompt and execute the following:

```
cd C:\path\to\SparkleShare\source
cd SparkleShare\Windows
build
```

`C:\path\to\SparkleShare\source\bin` should now contain `SparkleShare.exe`, which you can run.



### Creating a Windows installer

To create an installer package, install [WiX 3.5](http://wix.sourceforge.net/), restart Windows and run:

```
build installer
```

This will create `SparkleShare.msi` in the same directory.


## Reset SparkleShare settings

### Linux and Mac:

```
rm -Rf ~/SparkleShare
rm -Rf ~/.config/sparkleshare
```

### Windows

Remove `My Documents\SparkleShare` and `AppData\Roaming\sparkleshare` (`AppData` is hidden by default).


## Finally

Have fun! :)
