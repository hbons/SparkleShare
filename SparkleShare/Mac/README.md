## Building on Mac

You can choose to build SparkleShare from source or to download the SparkleShare bundle.


### Installing build requirements

Install [Xcode](https://developer.apple.com/xcode/), the [Mono Framework](http://www.mono-project.com/) 
and [MonoDevelop](http://monodevelop.com/).

Start MonoDevelop and install the MonoMac add-in (it's in the menus: <tt>MonoDevelop</tt> > <tt>Add-in Manager</tt>).


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
MonoDevelop and start the build (Build > Build All).


### Creating a Mac bundle

To create the <tt>SparkleShare.app</tt>, make sure the project is focused, select <tt>Project</tt> from the menu bar 
and click <tt>"Create Mac Installer..."</tt>. Make sure to select <tt>Don't link assemblies</tt>. 

Save the <tt>SparkleShare.app</tt> somewhere. We need to copy some files over:

```
cp SparkleShare/Mac/config SparkleShare.app/Contents/MonoBundle/config
cp /Library/Frameworks/Mono.framework/Versions/Current/lib/libintl.dylib SparkleShare.app/Contents/Resources
```

**Note:** Adjust `SparkleShare.app/Contents/Resources` to where you saved the bundle.

Now you have a working bundle that you can run by double-clicking.


### Resetting SparkleShare settings

```
rm -Rf ~/SparkleShare
rm -Rf ~/.config/sparkleshare
```

### Uninstalling

Simple remove the SparkleShare bundle.

