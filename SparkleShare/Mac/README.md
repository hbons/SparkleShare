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

Install <tt>git</tt>, <tt>automake</tt>, <tt>libtool</tt>, <tt>pkgconfig</tt> and <tt>intltool</tt> using <tt>MacPorts</tt>:

```bash
$ sudo port install git-core automake intltool pkgconfig libtool
```

Get a [Git](http://code.google.com/p/git-osx-installer/) install, and place both the `bin` and `libexec` directories in `SparkleShare/Mac/git`.
The exact commands depend on where you installed/have Git. Assuming it's in `/usr/local`:

```bash
$ mkdir SparkleShare/Mac/git
$ cp -R /usr/local/git/bin SparkleShare/Mac/git
$ cp -R /usr/local/git/libexec SparkleShare/Mac/git
```

Start the first part of the build:

```bash
$ ./autogen.sh
```

Now that you have compiled the libraries, open `SparkleShare/Mac/SparkleShare.sln` in
MonoDevelop and start the build (Build > Build All).

If you get `Are you missing a using directive or an assembly reference?` errors related to MacOS objects, then run:

```
git clone https://github.com/mono/monomac
git clone https://github.com/mono/maccore
cd monomac
make
```

It should generate `MonoMac.dll`. Copy it over any `MonoMac.dll` you might have on your system, then restart Monodevelop, and the project should now build fine.

### Creating a Mac bundle

To create the <tt>SparkleShare.app</tt> select <tt>Build</tt> from the menu bar 
and click <tt>"Build SparkleShare"</tt>.

You'll find a SparkleShare.app in SparkleShare/Mac/bin. Now we need to copy some files over:

```
cp SparkleShare/Mac/config SparkleShare.app/Contents/MonoBundle/config
cp /Library/Frameworks/Mono.framework/Versions/Current/lib/libintl.dylib SparkleShare.app/Contents/Resources
```

To play nice with GateKeeper, open `SparkleShare.app/Contents/Info.plist` and remove the `CFBundleResourceSpecification` property.

**Note:** Adjust `SparkleShare.app/Contents/...` to where you saved the bundle.

Now you have a working bundle that you can run by double-clicking.


### Resetting SparkleShare settings

```
rm -Rf ~/SparkleShare
rm -Rf ~/.config/sparkleshare
```


### Uninstalling

Simply remove the SparkleShare bundle.

