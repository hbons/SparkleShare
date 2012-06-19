### Windows

Download and run the installer and start SparkleShare from the Windows menu.






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

