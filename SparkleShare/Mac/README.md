## Building on Mac

You can choose to build SparkleShare from source or to download the SparkleShare bundle.


### Installing build requirements


Install

* Xcode
  
  Install Xcode from *App Store* or download manually from [Apple XCode](https://developer.apple.com/xcode/)

* MonoDevelop

  Install Xamarin Studio from [MonoDevelop](http://monodevelop.com/download/), Xamarin comes with the Mono framework

The required `git` binaries are now built automatically. For doing this and for building the distribution release, where Mono libraries are merged into SparkleShare, we need 
 the packes <tt>autoconf</tt> and <tt>pkg-config</tt>. You can install these using `homebrew`

```bash
$ brew port install autoconf pkg-config
```

### Building

There are three build configurations available:

* Debug

  with debug symbols and having the Symbol DEBUG defined, does require an installed Mono framework
  
* Release

  without debug symbols, does require an installed Mono framework
  
* ReleaseDist

  without debug symbols, the Mono framework is linked statically into the binary, so it does not require an installed Mono framework

To build any of these configurations,

* open SparkleShare.sln
* select the required configuraion
* select <tt>Build</tt> from the menu bar and click <tt>"Build SparkleShare"</tt>.

To build SparkleShare from a command line (e.g. for using a CI system), use this command:

```
/Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool -v build "--configuration:ReleaseDist" "${WORKSPACE}/SparkleShare/Mac/SparkleShare.sln"
```

### Resetting SparkleShare settings

```
rm -Rf ~/SparkleShare
rm -Rf ~/.config/sparkleshare
```


### Uninstalling

Simply remove the SparkleShare bundle.

