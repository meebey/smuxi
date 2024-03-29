AC_PREREQ([2.54])
AC_INIT([smuxi], [1.2.1], [https://github.com/meebey/smuxi/issues/new])
# using the --foreign option makes automake less strict about GNU policy 
AC_CONFIG_MACRO_DIR([m4])
AM_INIT_AUTOMAKE([foreign tar-ustar])
AM_MAINTAINER_MODE

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PROG_INSTALL

AC_MSG_CHECKING([whether the shell understands "+="])
shell_append=no
( foo=bar; set foo baz; eval "$[1]+=\$[2]" && test "$foo" = barbaz ) \
    >/dev/null 2>&1 \
  && shell_append=yes
AC_MSG_RESULT([$shell_append])
if test "x$shell_append" = "xno"; then
	AC_MSG_ERROR([Your shell (/bin/sh) is not supporting += which is required by this configure script. Try CONFIG_SHELL="env bash" ./configure instead.])
fi

# I18N
IT_PROG_INTLTOOL([0.25])

POSUB="
	po
	po-Engine
	po-Engine-MessageBuffer
	po-Engine-Campfire
	po-Engine-IRC
	po-Engine-JabbR
	po-Engine-Twitter
	po-Engine-XMPP
	po-Server
	po-Frontend
	po-Frontend-GNOME
	po-Frontend-GNOME-IRC
	po-Frontend-GNOME-Twitter
	po-Frontend-GNOME-XMPP
	po-Frontend-SWF
"
AC_SUBST(POSUB)
GETTEXT_PACKAGE=smuxi
AC_SUBST(GETTEXT_PACKAGE)

GETTEXT_PACKAGE_ENGINE=smuxi-engine
AC_SUBST(GETTEXT_PACKAGE_ENGINE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE, "$GETTEXT_PACKAGE_ENGINE", [Gettext package])

GETTEXT_PACKAGE_ENGINE_IRC=smuxi-engine-irc
AC_SUBST(GETTEXT_PACKAGE_ENGINE_IRC)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE_IRC, "$GETTEXT_PACKAGE_ENGINE_IRC", [Gettext package])

GETTEXT_PACKAGE_ENGINE_XMPP=smuxi-engine-xmpp
AC_SUBST(GETTEXT_PACKAGE_ENGINE_XMPP)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE_XMPP, "$GETTEXT_PACKAGE_ENGINE_XMPP", [Gettext package])

GETTEXT_PACKAGE_ENGINE_TWITTER=smuxi-engine-twitter
AC_SUBST(GETTEXT_PACKAGE_ENGINE_TWITTER)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE_TWITTER, "$GETTEXT_PACKAGE_ENGINE_TWITTER", [Gettext package])

GETTEXT_PACKAGE_ENGINE_CAMPFIRE=smuxi-engine-campfire
AC_SUBST(GETTEXT_PACKAGE_ENGINE_CAMPFIRE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE_CAMPFIRE, "$GETTEXT_PACKAGE_ENGINE_CAMPFIRE", [Gettext package])

GETTEXT_PACKAGE_ENGINE_JABBR=smuxi-engine-jabbr
AC_SUBST(GETTEXT_PACKAGE_ENGINE_JABBR)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE_JABBR, "$GETTEXT_PACKAGE_ENGINE_JABBR", [Gettext package])

GETTEXT_PACKAGE_MSG_BUFFER=smuxi-message-buffer
AC_SUBST(GETTEXT_PACKAGE_MSG_BUFFER)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_MSG_BUFFER, "$GETTEXT_PACKAGE_MSG_BUFFER", [Gettext package])

GETTEXT_PACKAGE_SERVER=smuxi-server
AC_SUBST(GETTEXT_PACKAGE_SERVER)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_SERVER, "$GETTEXT_PACKAGE_SERVER", [Gettext package])

GETTEXT_PACKAGE_FRONTEND=smuxi-frontend
AC_SUBST(GETTEXT_PACKAGE_FRONTEND)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND, "$GETTEXT_PACKAGE_FRONTEND", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_GNOME=smuxi-frontend-gnome
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_GNOME)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_GNOME, "$GETTEXT_PACKAGE_FRONTEND_GNOME", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_GNOME_IRC=smuxi-frontend-gnome-irc
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_GNOME_IRC)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_GNOME_IRC, "$GETTEXT_PACKAGE_FRONTEND_GNOME_IRC", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_GNOME_TWITTER=smuxi-frontend-gnome-twitter
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_GNOME_TWITTER)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_GNOME_TWITTER, "$GETTEXT_PACKAGE_FRONTEND_GNOME_TWITTER", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_GNOME_XMPP=smuxi-frontend-gnome-xmpp
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_GNOME_XMPP)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_GNOME_XMPP, "$GETTEXT_PACKAGE_FRONTEND_GNOME_XMPP", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_STFL=smuxi-frontend-stfl
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_STFL)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_STFL, "$GETTEXT_PACKAGE_FRONTEND_STFL", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_CURSES=smuxi-frontend-curses
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_CURSES)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_CURSES, "$GETTEXT_PACKAGE_FRONTEND_CURSES", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_SWF=smuxi-frontend-swf
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_SWF)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_SWF, "$GETTEXT_PACKAGE_FRONTEND_SWF", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_WPF=smuxi-frontend-wpf
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_WPF)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_WPF, "$GETTEXT_PACKAGE_FRONTEND_WPF", [Gettext package])

AM_GNU_GETTEXT([external])
AM_GNU_GETTEXT_VERSION([0.17])
# doesn't support multiple po directories :(
#AM_GLIB_GNU_GETTEXT

AC_ARG_WITH([vendor-package-version],
	AC_HELP_STRING([--with-vendor-package-version="DISTRO_NAME PACKAGE_VERSION"],
		[Set the distro name and package version, e.g. "Debian 0.8-1"]),
	[],
	with_vendor_package_version=
)
WITH_VENDOR_PACKAGE_VERSION=$with_vendor_package_version
if test "x$WITH_VENDOR_PACKAGE_VERSION" = "x"; then
	AC_MSG_CHECKING([for git version])
	if git log --oneline 295b37c8ac4939829a3c7f9150943dba8fff07f0 > /dev/null 2>&1; then
		GIT_BRANCH=$(git branch | grep '^\*' | cut -d ' ' -f 2)
		GIT_COMMIT_HASH=$(git log --no-color --first-parent -n1 --pretty=format:%h)
		DIST_VERSION=$GIT_BRANCH/$GIT_COMMIT_HASH
		DEV_VERSION_SUFFIX="-dev"
		AC_MSG_RESULT(yes)
	else
		DIST_VERSION=tarball
		DEV_VERSION_SUFFIX=
		AC_MSG_RESULT(no)
	fi
else
	DIST_VERSION=$WITH_VENDOR_PACKAGE_VERSION
fi
AC_SUBST([git_branch], "$GIT_BRANCH")
AC_SUBST([git_commit_hash], "$GIT_COMMIT_HASH")
AC_SUBST([DEV_VERSION_SUFFIX])
AC_SUBST([dist_version], "$DIST_VERSION")

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_CHECK_MONO_MODULE(5.18)
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
	System
	System.Core
	System.Data
	System.Runtime.Remoting
	System.Runtime.Serialization
	System.Xml
	Mono.Posix
	Mono.Data.Sqlite
])

PROFILE=debug
# Release builds, that is without log4net and without debug symbol files (.mdb)
# are not supported at this point, as they have no advantage at runtime but
# disadvantages to analyze bugs.
#AC_ARG_ENABLE(release,
#	AC_HELP_STRING([--enable-release],
#		[Use 'RELEASE' Configuration [default=NO]]),
#		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
#if test "x$enable_release" = "xyes" ; then
#	PROFILE=release
#fi
AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=YES]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	PROFILE=debug
fi
AC_SUBST(PROFILE)

SHAMROCK_FIND_MONO_2_0_COMPILER_OR_HIGHER
# SHAMROCK_FIND_MONO_2_0_COMPILER_OR_HIGHER sets MCS to the found mono compiler
# supporting the highest .NET version (mcs >> dmcs >> gmcs)
AC_SUBST(CSC, "$MCS")
CSC_FLAGS=
if test "x$PROFILE" = "xdebug"; then
	AM_CONDITIONAL(ENABLE_DEBUG, true)
	CSC_FLAGS+="-define:DEBUG,TRACE,LOG4NET -debug"
else
	AM_CONDITIONAL(ENABLE_RELEASE, true)
fi

MCS_BASENAME=$(basename $MCS)
CLI_RUNTIME=4.7.2
if test "$MCS_BASENAME" = "gmcs"; then
	CLI_RUNTIME=3.5
fi
if test "$MCS_BASENAME" = "dmcs"; then
	CLI_RUNTIME=4.0
fi
if test "$MCS_BASENAME" = "mcs"; then
	CLI_RUNTIME=4.7.2
fi
if test "$CLI_RUNTIME" = "4.7.2"; then
	if test ! -e "$($PKG_CONFIG --variable=libdir mono)/mono/4.7.2-api/mscorlib.dll" -a \
			! -e "$($PKG_CONFIG --variable=prefix mono)/lib/mono/4.7.2-api/mscorlib.dll"; then
		# Mono without 4.7.2 support -> downgrade to 4.5
		CLI_RUNTIME=4.5
	fi
fi
if test "$CLI_RUNTIME" = "4.5"; then
	if test ! -e "$($PKG_CONFIG --variable=libdir mono)/mono/4.5/mscorlib.dll" -a \
			! -e "$($PKG_CONFIG --variable=prefix mono)/lib/mono/4.5/mscorlib.dll"; then
		# Mono without 4.5 support -> downgrade to 4.0
		CLI_RUNTIME=4.0
	fi
fi
if test "$CLI_RUNTIME" = "4.0"; then
	if test ! -e "$($PKG_CONFIG --variable=libdir mono)/mono/4.0/mscorlib.dll" -a \
		    ! -e "$($PKG_CONFIG --variable=prefix mono)/lib/mono/4.0/mscorlib.dll"; then
		# Mono without 4.0 support -> downgrade to 3.5
		CLI_RUNTIME=3.5
	fi
fi
AC_SUBST(DOTNET_FRAMEWORK_VERSION, "$CLI_RUNTIME")

# ToolsVersion and .NET Framework values:
#  Visual Studio 2010 uses ToolsVersion 4.0 == .NET Framework 4.0
#  Visual Studio 2012 uses ToolsVersion 4.0 == .NET Framework 4.5
#  Visual Studio 2013 uses ToolsVersion 12.0 == .NET Framework 4.5.1
#  Visual Studio 2015 uses ToolsVersion 14.0 == .NET Framework 4.6
#  Visual Studio 2017 uses ToolsVersion 15.0 (not supported by mono-xbuild 6.8) == .NET Framework 4.6.1
if test "$CLI_RUNTIME" = "3.5"; then
	CSC_FLAGS+=" -sdk:3.5"
	XBUILD_FLAGS="/toolsversion:3.5 /p:TargetFrameworkVersion=v3.5 /property:DefineConstants=MONO_3_5"
fi
if test "$CLI_RUNTIME" = "4.0"; then
	CSC_FLAGS+=" -sdk:4.0"
	XBUILD_FLAGS="/toolsversion:4.0 /p:TargetFrameworkVersion=v4.0 /property:DefineConstants=MONO_4_0"
fi
if test "$CLI_RUNTIME" = "4.5"; then
	CSC_FLAGS+=" -sdk:4.5"
	XBUILD_FLAGS="/toolsversion:4.0 /p:TargetFrameworkVersion=v4.5 /property:DefineConstants=MONO_4_5"
fi
if test "$CLI_RUNTIME" = "4.7.2"; then
	CSC_FLAGS+=" -sdk:4.7.2"
	XBUILD_FLAGS="/toolsversion:14.0 /p:TargetFrameworkVersion=v4.7.2 /property:DefineConstants=MONO_4_5"
fi
# Mono's xbuild >= 5.0 no longer defaults to mcs as compiler but csc which will
# generate debug symbol files named .pdb instead of .mdb. Since we are
# installing the debug symbol files, we override the compiler to mcs to ensure
# the files will be avilable for the install during "make install"
AC_MSG_CHECKING([for Mono >= 5.0])
if $PKG_CONFIG 'mono >= 5.0'; then
	AC_MSG_RESULT(yes)
	XBUILD_FLAGS+=" /p:CscToolExe=mcs.exe"
else
	AC_MSG_RESULT(no)
fi
AC_SUBST(CSC_FLAGS)
AC_SUBST(XBUILD_FLAGS)

# Required Libraries	

if test "x$(uname)" != "xDarwin" -a ! -e lib/log4net.dll; then
	PKG_CHECK_MODULES([LOG4NET], [log4net])
	AM_CONDITIONAL([BUNDLE_LOG4NET], false)
else
	AM_CONDITIONAL([BUNDLE_LOG4NET], true)
fi

AC_MSG_CHECKING([for NUnit])
if $PKG_CONFIG 'nunit'; then
	AC_MSG_RESULT(yes)
	FOUND_NUNIT=yes
	AM_CONDITIONAL([ENABLE_TESTS], true)
else
	AC_MSG_RESULT(no)
	FOUND_NUNIT=no
	AM_CONDITIONAL([ENABLE_TESTS], false)
fi

nini_files=
if test "x$(uname)" != "xDarwin" -a ! -e lib/Nini.dll; then
	PKG_CHECK_EXISTS([nini-1.1], FOUND_NINI=yes, FOUND_NINI=no)
	if test "x$FOUND_NINI" = "xyes"; then
		nini_files=`pkg-config --variable=Libraries nini-1.1`
		if test -z "$nini_files" ; then
			# Debian-based distros place Nini into the GAC
			PKG_CHECK_MODULES([NINI], [nini-1.1])
		else
			# openSUSE has Nini as a private assembly; need to copy it.
			AC_SUBST([NINI_LIBS], "$nini_files")
		fi
	else
		PKG_CHECK_MODULES([NINI], [nini >= 1.1])
	fi
	AM_CONDITIONAL([BUNDLE_NINI], test -n "$nini_files")
	AM_CONDITIONAL([BUNDLE_NINI_BLOB], false)
else
	AM_CONDITIONAL([BUNDLE_NINI], false)
	AM_CONDITIONAL([BUNDLE_NINI_BLOB], true)
fi

AC_ARG_WITH([db4o],
	AC_HELP_STRING([--with-db4o=auto|system|included], [Use system or included db4o @<:@default=auto@:>@]),
	[],
	with_db4o=auto
)
WITH_DB4O=$with_db4o
if test "x$WITH_DB4O" = "xauto"; then
	PKG_CHECK_EXISTS([db4o >= 8.0], FOUND_DB4O=yes, FOUND_DB4O=no)
	if test "x$FOUND_DB4O" = "xyes"; then
		WITH_DB4O=system
	else
		WITH_DB4O=included
	fi
fi
if test "x$WITH_DB4O" = "xsystem"; then
	PKG_CHECK_MODULES([DB4O], [db4o >= 8.0])
	AM_CONDITIONAL([BUNDLE_DB4O], false)
fi
if test "x$WITH_DB4O" = "xincluded"; then
	if test ! -d "$srcdir/lib/db4o-net/Db4objects.Db4o"; then
		AC_MSG_ERROR([lib/db4o-net is empty!])
	fi

	AC_PATH_PROG(XBUILD, xbuild, no)
	if test "x$XBUILD" = "xno"; then
		AC_MSG_ERROR([You need to install xbuild])
	fi

	AM_CONDITIONAL([BUNDLE_DB4O], true)
	#AC_SUBST([DB4O_FILES], "Db4objects.Db4o.dll Db4objects.Db4o.Instrumentation.dll Db4objects.Db4o.NativeQueries.dll")
	AC_SUBST([DB4O_FILES], "Db4objects.Db4o.dll")
fi

# Optional Libraries
AC_ARG_WITH([messaging-menu],
	AC_HELP_STRING([--with-messaging-menu=auto|system|included|no], [Support Messaging Menu (Ubuntu >= 12.10) @<:@default=auto@:>@]),
	[],
	with_messagingmenu=auto
)
WITH_MESSAGINGMENU=$with_messagingmenu
if test "x$WITH_MESSAGINGMENU" = "xauto"; then
	PKG_CHECK_EXISTS([messagingmenu-sharp], FOUND_MESSAGINGMENU_SHARP=yes, FOUND_MESSAGINGMENU_SHARP=no)
	if test "x$FOUND_MESSAGINGMENU_SHARP" = "xyes"; then
		WITH_MESSAGINGMENU=system
	else
		PKG_CHECK_EXISTS([messaging-menu], FOUND_MESSAGING_MENU=yes, FOUND_MESSAGING_MENU=no)
		if test "x$FOUND_MESSAGING_MENU" = "xyes"; then
			WITH_MESSAGINGMENU=included
		else
			WITH_MESSAGINGMENU=no
		fi
	fi
fi
if test "x$WITH_MESSAGINGMENU" = "xsystem"; then
	PKG_CHECK_MODULES([MESSAGINGMENU_SHARP], [messagingmenu-sharp])
fi
if test "x$WITH_MESSAGINGMENU" = "xincluded"; then
	if test ! -f "$srcdir/lib/messagingmenu-sharp/configure.ac"; then
		AC_MSG_ERROR([lib/messagingmenu-sharp is empty!])
	fi

	PKG_CHECK_MODULES([MESSAGING_MENU], [messaging-menu])
	PKG_CHECK_MODULES([GIO_SHARP], [gio-sharp-2.0])
	GIO_SHARP_LIBS=`$PKG_CONFIG --variable=Libraries gio-sharp-2.0`
	AC_SUBST([GIO_SHARP_LIBS])
	GIO_SHARP_FILES="gio-sharp.dll"
	AC_SUBST([GIO_SHARP_FILES])

	ac_configure_args="$ac_configure_args CSC=$MCS"
	AC_CONFIG_SUBDIRS([lib/messagingmenu-sharp])
	AC_SUBST([MESSAGINGMENU_SHARP_FILES], "messagingmenu-sharp.dll")
fi
AM_CONDITIONAL([WITH_MESSAGINGMENU_SHARP_INCLUDED], test "x$WITH_MESSAGINGMENU" = "xincluded")

AC_ARG_WITH([indicate],
	AC_HELP_STRING([--with-indicate], [Support Messaging Menu (Ubuntu <= 12.04) @<:@default=auto@:>@]),
	[],
	with_indicate=auto
)
WITH_INDICATE=$with_indicate
PKG_CHECK_EXISTS([indicate-sharp-0.1], INDICATE_SHARP_SUPPORT=yes, INDICATE_SHARP_SUPPORT=no)
if test "x$WITH_INDICATE" = "xauto" -a "x$WITH_MESSAGINGMENU" != "xno"; then
	WITH_INDICATE=no
fi
if test "x$WITH_INDICATE" = "xauto"; then
	WITH_INDICATE=$INDICATE_SHARP_SUPPORT
fi
if test "x$WITH_INDICATE" = "xyes"; then
	PKG_CHECK_MODULES([INDICATE_SHARP], [indicate-sharp-0.1])
fi

AC_ARG_WITH([notify],
	AC_HELP_STRING([--with-notify], [Support Desktop Notifications @<:@default=auto@:>@]),
	[],
	with_notify=auto
)
WITH_NOTIFY=$with_notify
PKG_CHECK_EXISTS([notify-sharp], NOTIFY_SHARP_SUPPORT=yes, NOTIFY_SHARP_SUPPORT=no)
if test "x$WITH_NOTIFY" = "xauto"; then
	WITH_NOTIFY=$NOTIFY_SHARP_SUPPORT
fi
if test "x$WITH_NOTIFY" = "xyes"; then
	PKG_CHECK_MODULES([NOTIFY_SHARP], [notify-sharp])
fi

AC_ARG_WITH([dbus],
	AC_HELP_STRING([--with-dbus], [Support D-Bus @<:@default=auto@:>@]),
	[],
	with_dbus=auto
)
WITH_DBUS=$with_dbus
PKG_CHECK_EXISTS([dbus-sharp-1.0], DBUS_SHARP1_SUPPORT=yes, DBUS_SHARP1_SUPPORT=no)
PKG_CHECK_EXISTS([dbus-sharp-2.0], DBUS_SHARP2_SUPPORT=yes, DBUS_SHARP2_SUPPORT=no)
PKG_CHECK_EXISTS([ndesk-dbus-1.0], NDESK_DBUS_SUPPORT=yes, NDESK_DBUS_SUPPORT=no)
if test "x$WITH_DBUS" = "xauto"; then
	if test "x$DBUS_SHARP1_SUPPORT" = "xyes"; then
		WITH_DBUS=$DBUS_SHARP1_SUPPORT
	elif test "x$DBUS_SHARP2_SUPPORT" = "xyes"; then
		WITH_DBUS=$DBUS_SHARP2_SUPPORT
	else
		WITH_DBUS=$NDESK_DBUS_SUPPORT
	fi
fi
if test "x$WITH_DBUS" = "xyes"; then
	if test "x$DBUS_SHARP2_SUPPORT" = "xyes"; then
		PKG_CHECK_MODULES([DBUS_SHARP], [dbus-sharp-2.0])
		PKG_CHECK_MODULES([DBUS_SHARP_GLIB], [dbus-sharp-glib-2.0])
		AC_SUBST([DBUS_LIBS], "$DBUS_SHARP_LIBS $DBUS_SHARP_GLIB_LIBS")
		CSC_FLAGS+=" -define:IPC_DBUS -define:DBUS_SHARP"
	elif test "x$DBUS_SHARP1_SUPPORT" = "xyes"; then
		PKG_CHECK_MODULES([DBUS_SHARP], [dbus-sharp-1.0])
		PKG_CHECK_MODULES([DBUS_SHARP_GLIB], [dbus-sharp-glib-1.0])
		AC_SUBST([DBUS_LIBS], "$DBUS_SHARP_LIBS $DBUS_SHARP_GLIB_LIBS")
		CSC_FLAGS+=" -define:IPC_DBUS -define:DBUS_SHARP"
	else
		# fallback to ndesk-dbus
		PKG_CHECK_MODULES([NDESK_DBUS], [ndesk-dbus-1.0])
		PKG_CHECK_MODULES([NDESK_DBUS_GLIB], [ndesk-dbus-glib-1.0])
		AC_SUBST([DBUS_LIBS], "$NDESK_DBUS_LIBS $NDESK_DBUS_GLIB_LIBS")
		CSC_FLAGS+=" -define:IPC_DBUS -define:NDESK_DBUS"
	fi
fi

AC_ARG_WITH([gtkspell],
	AC_HELP_STRING([--with-gtkspell], [Support GTK+ spell checking @<:@default=auto@:>@]),
	[],
	with_gtkspell=auto
)
WITH_GTKSPELL=$with_gtkspell
PKG_CHECK_EXISTS([gtkspell-2.0], GTKSPELL_SUPPORT=yes, GTKSPELL_SUPPORT=no)
if test "x$WITH_GTKSPELL" = "xauto"; then
	WITH_GTKSPELL=$GTKSPELL_SUPPORT
fi
if test "x$WITH_GTKSPELL" = "xyes"; then
	PKG_CHECK_MODULES([GTKSPELL], [gtkspell-2.0 >= 2.0.9])
fi

# Engines
AC_ARG_ENABLE([engine-irc],
	AC_HELP_STRING([--enable-engine-irc], [Enable IRC protocol support (default yes)]),
	ENABLE_ENGINE_IRC=$enableval,
	ENABLE_ENGINE_IRC=yes
)
if test "x$ENABLE_ENGINE_IRC" != "xno"; then
	AC_PATH_PROG(XBUILD, xbuild, no)
	if test "x$XBUILD" = "xno"; then
		AC_MSG_ERROR([You need to install xbuild for irc support])
	fi

	if test ! -f "$srcdir/lib/SmartIrc4net/SmartIrc4net.csproj"; then
		AC_MSG_ERROR([lib/SmartIrc4net is empty!])
	fi
	SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
		System
		Mono.Posix
	])
fi
AM_CONDITIONAL(ENABLE_ENGINE_IRC, test "x$ENABLE_ENGINE_IRC" = "xyes")

AC_ARG_ENABLE([engine-campfire],
	AC_HELP_STRING([--enable-engine-campfire], [Enable Campfire protocol support (default yes)]),
	ENABLE_ENGINE_CAMPFIRE=$enableval,
	ENABLE_ENGINE_CAMPFIRE=yes
)
AM_CONDITIONAL(ENABLE_ENGINE_CAMPFIRE, test "x$ENABLE_ENGINE_CAMPFIRE" = "xyes")

AC_ARG_ENABLE([engine-xmpp],
	AC_HELP_STRING([--enable-engine-xmpp], [Enable XMPP (Jabber) protocol support (default yes)]),
	ENABLE_ENGINE_XMPP=$enableval,
	ENABLE_ENGINE_XMPP=yes
)
if test "x$ENABLE_ENGINE_XMPP" != "xno"; then
	AC_PATH_PROG(XBUILD, xbuild, no)
	if test "x$XBUILD" = "xno"; then
		AC_MSG_ERROR([You need to install xbuild for XMPP support])
	fi

	if test ! -f "$srcdir/lib/agsxmpp/agsxmpp/agsxmpp.csproj"; then
		AC_MSG_ERROR([lib/agsxmpp is empty!])
	fi
	SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
		System
		System.Core
		Mono.Posix
		System.Xml
	])
fi
AM_CONDITIONAL(ENABLE_ENGINE_XMPP, test "x$ENABLE_ENGINE_XMPP" = "xyes")

if test ! -f "$srcdir/lib/StarkSoftProxy/StarkSoftProxy.csproj"; then
	AC_MSG_ERROR([lib/StarkSoftProxy is empty!])
fi

AC_ARG_ENABLE([engine-twitter],
	AC_HELP_STRING([--enable-engine-twitter], [Enable Twitter support (default yes)]),
	ENABLE_ENGINE_TWITTER=$enableval,
	ENABLE_ENGINE_TWITTER=yes
)
if test "x$ENABLE_ENGINE_TWITTER" != "xno"; then
	AC_PATH_PROG(XBUILD, xbuild, no)
	if test "x$XBUILD" = "xno"; then
		AC_MSG_ERROR([You need to install xbuild for Twitter support])
	fi

	# compiling Json.NET with the C# compiler of Mono 2.4 will result in
	# a endless loop with eating all memory :/
	AC_MSG_CHECKING([for Mono >= 2.6])
	if $PKG_CONFIG 'mono >= 2.6'; then
		AC_MSG_RESULT(yes)
	else
		AC_MSG_RESULT(no)
		AC_MSG_ERROR([You need Mono 2.6 or later for Twitter support])
	fi

	if test ! -f "$srcdir/lib/Newtonsoft.Json/Src/Newtonsoft.Json/Newtonsoft.Json.csproj"; then
		AC_MSG_ERROR([lib/Newtonsoft.Json is empty!])
	fi
	SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
		System
		System.Core
		System.Data
		System.Runtime.Serialization
		System.Web
		System.Xml
		System.Xml.Linq
	])

	if test ! -f "$srcdir/lib/Twitterizer/Twitterizer2/Twitterizer2.csproj"; then
		AC_MSG_ERROR([lib/Twitterizer is empty!])
	fi
	SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
		System
		System.Configuration
		System.Core
		System.Data
		System.Drawing
		System.Web
		System.Web.Extensions
		System.Xml
		System.Xml.Linq
	])
fi
AM_CONDITIONAL(ENABLE_ENGINE_TWITTER, test "x$ENABLE_ENGINE_TWITTER" = "xyes")
AC_ARG_WITH([twitter-api-key],
	AC_HELP_STRING([--with-twitter-api-key], [Specify custom Twitter API key]),
	[],
	with_twitter_api_key="60QV2qQx9cS7y1BJDbgAA|2VgD6qQKddsF5HYQ0TrRgs3tFTnCwDONBmRlTmG658"
)
AC_SUBST([twitter_api_key], $with_twitter_api_key)

AC_ARG_ENABLE([engine-jabbr],
	AC_HELP_STRING([--enable-engine-jabbr], [Enable JabbR support (default yes)]),
	ENABLE_ENGINE_JABBR=$enableval,
	ENABLE_ENGINE_JABBR=yes
)
if test "x$ENABLE_ENGINE_JABBR" != "xno"; then
	AC_PATH_PROG(XBUILD, xbuild, no)
	if test "x$XBUILD" = "xno"; then
		AC_MSG_ERROR([You need to install xbuild for JabbR support])
	fi

	# compiling Json.NET with the C# compiler of Mono 2.4 will result in
	# a endless loop with eating all memory :/
	# SignalR.Client requires System.Threading.Tasks which is part of .NET 4.0
	# thus Mono 2.10 with dmcs is required!
	AC_MSG_CHECKING([for Mono >= 2.10])
	if $PKG_CONFIG 'mono >= 2.10'; then
		AC_MSG_RESULT(yes)
	else
		AC_MSG_RESULT(no)
		AC_MSG_ERROR([You need Mono 2.10 or later for JabbR support (or use --disable-engine-jabbr)])
	fi

	AC_MSG_CHECKING([for 4.0 compiler])
	if test "x$CLI_RUNTIME" = "x2.0"; then
		AC_MSG_RESULT(no)
		AC_MSG_ERROR([You need to compile with a 4.0 compiler (dmcs) for JabbR support (or use --disable-engine-jabbr)])
	else
		AC_MSG_RESULT(yes)
	fi

	if test ! -f "$srcdir/lib/Newtonsoft.Json/Src/Newtonsoft.Json/Newtonsoft.Json.csproj"; then
		AC_MSG_ERROR([lib/Newtonsoft.Json is empty!])
	fi
	SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
		System
		System.Core
		System.Data
		System.Runtime.Serialization
		System.Web
		System.Xml
		System.Xml.Linq
	])

	if test ! -f "$srcdir/lib/SignalR/src/Microsoft.AspNet.SignalR.Client/Microsoft.AspNet.SignalR.Client.csproj"; then
		AC_MSG_ERROR([lib/SignalR is empty!])
	fi
	SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
		System
		System.Core
	])

	if test ! -f "$srcdir/lib/JabbR/JabbR.Client/JabbR.Client.csproj"; then
		AC_MSG_ERROR([lib/JabbR is empty!])
	fi
	SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
		System
		System.Core
		System.Data
		System.Data.DataSetExtensions
		System.Xml
		System.Xml.Linq
	])
fi
AM_CONDITIONAL(ENABLE_ENGINE_JABBR, test "x$ENABLE_ENGINE_JABBR" = "xyes")

# MessageBuffer tool
if $PKG_CONFIG 'mono >= 2.6'; then
	MSG_BUFFER_COMPILER_FLAGS+=" -platform:x86"
fi
AC_SUBST(MSG_BUFFER_COMPILER_FLAGS)

# Server
if $PKG_CONFIG 'mono >= 2.6'; then
	SERVER_COMPILER_FLAGS+=" -platform:x86"
fi
AC_SUBST(SERVER_COMPILER_FLAGS)

# Frontends
AC_ARG_ENABLE([frontend-gnome],
	AC_HELP_STRING([--enable-frontend-gnome], [Enable GNOME frontend (default yes)]),
	ENABLE_FRONTEND_GNOME=$enableval,
	ENABLE_FRONTEND_GNOME=yes
)
if test "x$ENABLE_FRONTEND_GNOME" != "xno"; then
	PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0 >= 2.8])
	PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0 >= 2.12.39])
	
	AC_MSG_CHECKING([for gtk-sharp version >= 2.10])
	if $PKG_CONFIG 'gtk-sharp-2.0 >= 2.10'; then
		AC_MSG_RESULT(yes)
		AC_DEFINE(GTK_SHARP_2_10,1,[System has gtk-sharp 2.10 or better])
		FRONTEND_GNOME_COMPILER_FLAGS+=" -define:GTK_SHARP_2_10"
	else
		AC_MSG_RESULT(no)
	fi
	
	SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
		System.Drawing
	])
	
	if test "x$WITH_MESSAGINGMENU" = "xsystem"; then
		FRONTEND_GNOME_COMPILER_FLAGS+=" -define:MESSAGING_MENU_SHARP"
	fi
	if test "x$WITH_MESSAGINGMENU" = "xincluded"; then
		FRONTEND_GNOME_COMPILER_FLAGS+=" -define:MESSAGING_MENU_SHARP"
	fi
	if test "x$WITH_INDICATE" = "xyes"; then
		FRONTEND_GNOME_COMPILER_FLAGS+=" -define:INDICATE_SHARP"
	fi
	if test "x$WITH_NOTIFY" = "xyes"; then
		FRONTEND_GNOME_COMPILER_FLAGS+=" -define:NOTIFY_SHARP"
	fi
	if test "x$WITH_GTKSPELL" = "xyes"; then
		FRONTEND_GNOME_COMPILER_FLAGS+=" -define:GTKSPELL"
	fi
	if $PKG_CONFIG 'mono >= 2.6'; then
		FRONTEND_GNOME_COMPILER_FLAGS+=" -platform:x86"
	fi
	AC_SUBST(FRONTEND_GNOME_COMPILER_FLAGS)
	
	ENABLE_FRONTEND_GNOME_IRC=$ENABLE_ENGINE_IRC;
	ENABLE_FRONTEND_GNOME_TWITTER=$ENABLE_ENGINE_TWITTER;
	ENABLE_FRONTEND_GNOME_XMPP=$ENABLE_ENGINE_XMPP;
fi
AM_CONDITIONAL(ENABLE_FRONTEND_GNOME, test "x$ENABLE_FRONTEND_GNOME" = "xyes")
AM_CONDITIONAL(ENABLE_FRONTEND_GNOME_IRC, test "x$ENABLE_FRONTEND_GNOME_IRC" = "xyes")
AM_CONDITIONAL(ENABLE_FRONTEND_GNOME_TWITTER, test "x$ENABLE_FRONTEND_GNOME_TWITTER" = "xyes")
AM_CONDITIONAL(ENABLE_FRONTEND_GNOME_XMPP, test "x$ENABLE_FRONTEND_GNOME_XMPP" = "xyes")

if test "x$(uname)" = "xDarwin"; then
	ENABLE_FRONTEND_STFL_DEFAULT=no
else
	ENABLE_FRONTEND_STFL_DEFAULT=yes
fi
AC_ARG_ENABLE([frontend-stfl],
	AC_HELP_STRING([--enable-frontend-stfl], [Enable STFL frontend (default $ENABLE_FRONTEND_STFL_DEFAULT)]),
	ENABLE_FRONTEND_STFL=$enableval,
	ENABLE_FRONTEND_STFL=$ENABLE_FRONTEND_STFL_DEFAULT
)

if test "x$ENABLE_FRONTEND_STFL" != "xno"; then
	PKG_CHECK_MODULES([STFL], [stfl >= 0.21])
fi
AM_CONDITIONAL(ENABLE_STATIC_STFL, false)
AM_CONDITIONAL(ENABLE_FRONTEND_STFL, test "x$ENABLE_FRONTEND_STFL" = "xyes")

AC_ARG_ENABLE([frontend-curses],
	AC_HELP_STRING([--enable-frontend-curses], [Enable Ncurses frontend (default no)]),
	ENABLE_FRONTEND_CURSES=$enableval,
	ENABLE_FRONTEND_CURSES=no
)
if test "x$ENABLE_FRONTEND_CURSES" != "xno"; then
	# TODO: check deps
	AC_MSG_ERROR([Ncurses frontend not supported (yet)])
fi
AM_CONDITIONAL(ENABLE_FRONTEND_CURSES, test "x$ENABLE_FRONTEND_CURSES" = "xyes")

AC_ARG_ENABLE([frontend-swf],
	AC_HELP_STRING([--enable-frontend-swf], [Enable SWF (WinForms) frontend (default no)]),
	ENABLE_FRONTEND_SWF=$enableval,
	ENABLE_FRONTEND_SWF=no
)
if test "x$ENABLE_FRONTEND_SWF" != "xno"; then
	SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES([
		System.Windows.Forms
	])
fi
AM_CONDITIONAL(ENABLE_FRONTEND_SWF, test "x$ENABLE_FRONTEND_SWF" = "xyes")

AC_ARG_ENABLE([frontend-wpf],
	AC_HELP_STRING([--enable-frontend-wpf], [Enable WPF (Windows Presentation Foundation) frontend (default no)]),
	ENABLE_FRONTEND_WPF=$enableval,
	ENABLE_FRONTEND_WPF=no
)
if test "x$ENABLE_FRONTEND_WPF" != "xno"; then
	# TODO: check deps
	AC_MSG_ERROR([WPF frontend not supported (yet)])
fi
AM_CONDITIONAL(ENABLE_FRONTEND_WPF, test "x$ENABLE_FRONTEND_WPF" = "xyes")

AC_ARG_ENABLE([frontend-test],
	AC_HELP_STRING([--enable-frontend-test], [Enable Test frontend (default no)]),
	ENABLE_FRONTEND_TEST=$enableval,
	ENABLE_FRONTEND_TEST=no
)
AM_CONDITIONAL(ENABLE_FRONTEND_TEST, test "x$ENABLE_FRONTEND_TEST" = "xyes")

AC_CONFIG_FILES([
	Makefile
	src/Makefile
	src/AssemblyVersion.cs
	src/AssemblyTargetFramework.cs
	src/smuxi-win32.nsis
	src/Common/Makefile
	src/Common/Defines.cs
	src/Common/smuxi-common.pc
	src/Engine/Makefile
	src/Engine/smuxi-engine.pc
	src/Engine-IRC/Makefile
	src/Engine-IRC/smuxi-engine-irc.pc
	src/Engine-JabbR/Makefile
	src/Engine-JabbR/smuxi-engine-jabbr.pc
	src/Engine-Twitter/Makefile
	src/Engine-XMPP/Makefile
	src/Engine-XMPP/smuxi-engine-xmpp.pc
	src/Engine-Campfire/Makefile
	src/Engine-MessageBuffer/Makefile
	src/Engine-MessageBuffer/smuxi-message-buffer
	src/Server/Makefile
	src/Server/smuxi-server
	src/Frontend/Makefile
	src/Frontend/smuxi-frontend.pc
	src/Frontend-GNOME/Makefile
	src/Frontend-GNOME/smuxi-frontend-gnome
	src/Frontend-GNOME-IRC/Makefile
	src/Frontend-GNOME-Twitter/Makefile
	src/Frontend-GNOME-XMPP/Makefile
	src/Frontend-Curses/Makefile
	src/Frontend-Curses/smuxi-frontend-curses
	src/Frontend-STFL/Makefile
	src/Frontend-STFL/smuxi-frontend-stfl
	src/Frontend-STFL/STFL/Makefile
	src/Frontend-SWF/Makefile
	src/Frontend-SWF/smuxi-frontend-swf
	src/Frontend-Test/Makefile
	src/Frontend-Test/smuxi-frontend-test
	lib/Makefile
	lib/osx/Info.plist
	po/Makefile.in
	po-Engine/Makefile.in
	po-Engine-Campfire/Makefile.in
	po-Engine-IRC/Makefile.in
	po-Engine-JabbR/Makefile.in
	po-Engine-Twitter/Makefile.in
	po-Engine-XMPP/Makefile.in
	po-Engine-MessageBuffer/Makefile.in
	po-Server/Makefile.in
	po-Frontend/Makefile.in
	po-Frontend-GNOME/Makefile.in
	po-Frontend-GNOME-IRC/Makefile.in
	po-Frontend-GNOME-Twitter/Makefile.in
	po-Frontend-GNOME-XMPP/Makefile.in
])

AC_OUTPUT

if test "x$CLI_RUNTIME" = "x"; then
	CLI_RUNTIME=default
fi

AC_MSG_RESULT([
	Configuration summary for $PACKAGE_NAME $VERSION ($DIST_VERSION)
	
	* Installation prefix: $prefix
	* Build profile:       $PROFILE
	* Compiler:            $CSC
	* Target CLI runtime:  $CLI_RUNTIME

	* Tests:               $FOUND_NUNIT

	* Engines
	  -------
	  Core:                (sqlite: built-in, db4o: $WITH_DB4O)
	  IRC:                 $ENABLE_ENGINE_IRC
	  XMPP:                $ENABLE_ENGINE_XMPP
	  Twitter:             $ENABLE_ENGINE_TWITTER
	  Campfire:            $ENABLE_ENGINE_CAMPFIRE
	  JabbR:               $ENABLE_ENGINE_JABBR

	* Frontends
	  ---------
	  GNOME:               $ENABLE_FRONTEND_GNOME (IRC: $ENABLE_FRONTEND_GNOME_IRC XMPP: $ENABLE_FRONTEND_GNOME_XMPP Twitter: $ENABLE_FRONTEND_GNOME_TWITTER)
	  + Messaging Menu     (indicate: Ubuntu <= 12.04, messaging-menu: Ubuntu >= 12.04)
	    - indicate:        $WITH_INDICATE
	    - messaging-menu:  $WITH_MESSAGINGMENU
	  + Notifications:     $WITH_NOTIFY
	  + Spell Checking:    $WITH_GTKSPELL
	  + D-Bus:             $WITH_DBUS
	  Ncurses:             $ENABLE_FRONTEND_CURSES
	  STFL:                $ENABLE_FRONTEND_STFL
	  SWF (WinForms):      $ENABLE_FRONTEND_SWF
	  WPF:                 $ENABLE_FRONTEND_WPF
	  Test:                $ENABLE_FRONTEND_TEST
])
