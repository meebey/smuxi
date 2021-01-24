Software Requirements
=====================
First you will need to install a few libraries to compile the source

Build tools & libraries:
* Automake, Autoconf, gettext, pkg-config
* Mono SDK (>= 4.6.2)
* Nini (>= 1.1)
* log4net
* SQLite3
* GTK# (>= 2.10) (optional, but required for the GNOME frontend)
* Notify# (optional)
* Indicate# / MessagingMenu# (optional)
* DBus# / NDesk.DBus (optional)
* GtkSpell (optional)
* STFL (optional)

Depending on your operating system and favorite distribution the installation of the listed applications varies. For Debian based distributions it's just a matter of the following commands:

    apt-get install mono-devel mono-xbuild libnini-cil-dev liblog4net-cil-dev libgtk2.0-cil-dev libglade2.0-cil-dev libnotify-cil-dev libdbus2.0-cil-dev libdbus2.0-cil-dev lsb-release

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
  [here]: http://git.debian.org/?p=pkg-cli-apps/packages/smuxi.git
