## Building on macOS

You can build SparkleShare from source or download the SparkleShare bundle.


### Installing build requirements

  Install [Xcode](https://itunes.apple.com/gb/app/xcode/id497799835?mt=12) from the macOS App Store, or [download](https://developer.apple.com/xcode/) it manually.
  Install [Visual Studio](https://www.visualstudio.com/vs/visual-studio-mac/).

For building the distribution release, where Mono libraries are merged into SparkleShare, we need 
 the `autoconf` and `pkg-config`. You can install these in several ways, here's how it's done using [Homebrew](https://brew.sh/):

```bash
brew install autoconf automake libtool pkg-config
```

### Building

There are three build configurations available:

* DebugMac

  with debug symbols and having the Symbol DEBUG defined. Requires an installed Mono framework.
  
* Release

  without debug symbols. Requires an installed Mono framework.
  
* ReleaseMac

  without debug symbols, the Mono framework is linked statically into the binary, so it does not require an installed Mono framework.

To build any of these configurations,

* open `./SparkleShare.sln` in Visual Studio
* select the SparkleShare.Mac project in the Solution view
* select the required configuration
* select `Build`, then `"Build SparkleShare.Mac"` from the menu

To build SparkleShare from a command line (e.g. for using a CI system), use this command:

```bash
 /Applications/Visual\ Studio.app/Contents/MacOS/vstool build "--configuration:ReleaseMac" "SparkleShare.sln"
```


### Resetting SparkleShare settings

```
rm -Rf ~/SparkleShare
rm -Rf ~/.config/org.sparkleshare.SparkleShare
```


### Uninstalling

Simply remove the SparkleShare bundle.
