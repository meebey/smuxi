Smuxi
=====
[![License](https://img.shields.io/github/license/meebey/smuxi.svg)](https://github.com/meebey/smuxi/blob/master/LICENSE) [![GitHubCI pipeline status badge](https://github.com/meebey/smuxi/workflows/auto-ci-builds/badge.svg)](https://github.com/meebey/smuxi/commits/master) ![GitHub contributors](https://img.shields.io/github/contributors-anon/meebey/smuxi)

![GitHub Repo stars](https://img.shields.io/github/stars/meebey/smuxi?style=social) [![Twitter Follow](https://img.shields.io/twitter/follow/smuxi?style=social)](https://twitter.com/intent/follow?screen_name=smuxi)

![GitHub Release Date](https://img.shields.io/github/release-date/meebey/smuxi)
![Debian package](https://img.shields.io/debian/v/smuxi)
![Ubuntu package](https://img.shields.io/ubuntu/v/smuxi)
![AUR version](https://img.shields.io/aur/version/smuxi?label=AUR)

Software Requirements
=====================
First you will need to install a few libraries to compile the source

Build tools & libraries:
* Automake, Autoconf, gettext, pkg-config
* Mono SDK (>= 4.6.2)
* Nini (>= 1.1)
* log4net
* SQLite3
* GTK# (>= 2.12.39) (optional, but required for the GNOME frontend)
* Notify# (optional)
* Indicate# / MessagingMenu# (optional)
* DBus# / NDesk.DBus (optional)
* GtkSpell (optional)
* STFL (optional, but enabled by default)

Depending on your operating system and favorite distribution the installation of
the listed applications varies.

For Debian based distributions it's just a matter of the following commands:

    apt-get install build-essential git autoconf automake intltool mono-devel mono-xbuild libnini-cil-dev liblog4net-cil-dev libgtk2.0-cil-dev libnotify-cil-dev libdbus2.0-cil-dev libdbus-glib2.0-cil-dev lsb-release

Compiling Source
================

    ./autogen.sh || ./configure
    make

Installing
==========

    make install

Running
=======

Now you can start Smuxi from the GNOME or KDE menu.

Source Structure
================

src/
----

This directory contains the source code of all Smuxi components.

lib/
----

This directory contains libraries that Smuxi needs and ships as part of Smuxi.

po\*/
-----

These directories contain translation files based on gettext.

debian/
-------

The debian/ directory contains upstream packaging used for the daily development
builds for Ubuntu and Debian found on [launchpad][] (which you can subscribe to
via `sudo add-apt-repository -y ppa:meebey/smuxi-daily && sudo apt update`).
The official (downstream) Debian packaging can be found on [here][].

  [launchpad]: https://launchpad.net/~meebey/+archive/smuxi-daily
  [here]: https://salsa.debian.org/dotnet-team/smuxi
