# Build on windows

* When you clone the repository, remember to run `git submodule update --init`

* Install [.NET Framework 4.0](http://www.microsoft.com/download/en/details.aspx?id=17851) (if not installed yet)

* Install [msysGit](http://code.google.com/p/msysgit/downloads/detail?name=Git-1.7.8-preview20111206.exe)
  *  I recommend you to install in `C:\msysgit`

* Download [SmartIrc4net-0.4.0.bin.zip](http://sourceforge.net/projects/smartirc4net/files/SmartIrc4net/0.4.0/SmartIrc4net-0.4.0.bin.zip/download)
  * Copy `Meebey.SmartIrc4net.dll` and `Meebey.SmartIrc4net.xml` from the zip file in `bin\release` to `SparkleShare\bin` (create that directory if it does not exist)

* Download [CefSharp-0.3.1.7z](https://github.com/downloads/chillitom/CefSharp/CefSharp-0.3.1.7z)
  * Copy `avcodec-52.dll`, `avformat-52.dll`, `avutil-50.dll`, `CefSharp.dll`, `icudt42.dll` and `libcef.dll` from the 7z file in `CefSharp-0.3.1\Release\` to `SparkleShare\bin`

* Copy the entire contents of the msysGit folder to `SparkleShare\bin\msysgit` 

* Open a command shell and execute

        sparkleshare\windows\build.cmd

* `SparkleShare\bin` should now contain `SparkleLib.dll` and `SparkleShare.exe`

* If you want to build the Windows installer download and install [WiX](http://wix.sourceforge.net/)

* Then run

        sparkleshare\windows\build.cmd installer

