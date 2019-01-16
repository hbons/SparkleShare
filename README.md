# [SparkleShare](https://www.sparkleshare.org/)

[SparkleShare](https://www.sparkleshare.org/) is a file sharing and collaboration app. It works just like Dropbox, and you can run it on your own server. It's available for Linux distributions, macOS, and Windows. [Support the project on Patreon](https://www.patreon.com/SparkleShare).

![Banner](https://raw.githubusercontent.com/hbons/SparkleShare/master/SparkleShare/Common/Images/readme-banner.png)


## How does it work?

SparkleShare creates a special folder on your computer. You can add remotely hosted folders (or "projects") to this folder. These projects will be automatically kept in sync with both the host and all of your peers when someone adds, removes or edits a file.

## Install on Ubuntu or Fedora

You can install the package from your distribution (likely old and not updated often), but we recommend to get our Flatpak with automatic updates to always enjoy the latest and greatest:

```bash
flatpak remote-add flathub https://flathub.org/repo/flathub.flatpakrepo
flatpak install flathub org.sparkleshare.SparkleShare
```

Now you can run SparkleShare from the apps menu.

**Note:** by default SparkleShare uses an AppIndicator status icon on Linux. If you use GNOME on a distribution other than Ubuntu, please install the [AppIndicator extension](https://extensions.gnome.org/extension/615/appindicator-support/). If you don't use GNOME, you can start SparkleShare with `--status-icon=gtk`.


## Install on macOS

Download the app from the [releases page](https://github.com/hbons/SparkleShare/releases).


## Set up a host

Under the hood SparkleShare uses the version control system [Git](https://git-scm.com/) and the large files extension [Git LFS](https://git-lfs.github.com), so setting up a host yourself is relatively easy. Using your own host gives you more privacy and control, as well as lots of cheap storage space and higher transfer speeds. We've made a simple [script](https://github.com/hbons/Dazzle) that does the hard work for you. If you need to manage a lot of projects and/or users we recommend hosting a [GitLab Community Edition](https://about.gitlab.com/installation/) instance.


## Build from source
`SparkleShare` is Free and Open Source software and licensed under the [GNU GPLv3 or later](LICENSE.md). You are welcome to change and redistribute it under certain conditions. Its library `Sparkles` is licensed under the [GNU LGPLv3 or later](LICENSE_Sparkles.md).

Here are instructions to build SparkleShare on [Linux distributions](SparkleShare/Linux/README.md), [macOS](SparkleShare/Mac/README.md), and [Windows](SparkleShare/Windows/README.md).


[![Build Status](https://travis-ci.org/hbons/SparkleShare.svg?branch=master)](https://travis-ci.org/hbons/SparkleShare)
[![Join the chat at https://gitter.im/hbons/SparkleShare](https://badges.gitter.im/hbons/SparkleShare.svg)](https://gitter.im/hbons/SparkleShare?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Useful links
- [sparkleshare.org](https://www.sparkleshare.org/)
- [@SparkleShare](https://www.twitter.com/SparkleShare), [@hbons](https://www.twitter.com/hbons)
- Community chatroom on [Gitter](https://www.gitter.im/hbons/SparkleShare)
- [Wiki](https://www.github.com/hbons/SparkleShare/wiki)


Have fun, make awesome. :)

