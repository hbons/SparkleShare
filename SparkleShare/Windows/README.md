## Windows
You can choose to build SparkleShare from source or to run the Windows installer.


### Installing build requirements

Install version 4.0 of the [.NET Framework](http://www.microsoft.com/download/en/details.aspx?id=17851) if you haven't already.

Install [msysGit](https://github.com/msysgit/msysgit/releases) and copy the contents of the install folder
(`C:\Program Files (x86)\Git` by default) to `C:\path\to\SparkleShare-sources\bin\msysgit\` (create the "bin"-folder in the SparkleShare source directory).

Open a command prompt and execute the following:

```
cd C:\path\to\SparkleShare-sources
cd SparkleShare\Windows
build
```

`C:\path\to\SparkleShare-sources\bin` should now contain `SparkleShare.exe`, which you can run.


### Creating a Windows installer

To create an installer package, install [WiX 3.7](http://wix.codeplex.com/releases/view/99514), restart Windows and run:

```
cd C:\path\to\SparkleShare-sources\SparkleShare\Windows\
build installer
```

This will create `SparkleShare.msi` in the same directory.


### Resetting SparkleShare settings

Remove `My Documents\SparkleShare` and `AppData\Roaming\org.sparkleshare.SparkleShare` (`AppData` is hidden by default).


### Uninstalling

You can uninstall SparkleShare through the Windows Control Panel.

