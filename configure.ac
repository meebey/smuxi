AC_PREREQ([2.54])
AC_INIT([smuxi], [0.6.1], [http://smuxi.meebey.net/cgi-bin/trac/trac.cgi/newticket])
AM_INIT_AUTOMAKE([foreign])
AM_MAINTAINER_MODE

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PROG_INSTALL

# I18N
AC_PROG_INTLTOOL([0.25])  
AM_PROG_LIBTOOL

POSUB="
	po
	po-Engine
	po-Engine-IRC
	po-Engine-XMPP
	po-Engine-OSCAR
	po-Engine-MSNP
	po-Server
	po-Frontend
	po-Frontend-GNOME
	po-Frontend-GNOME-IRC
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

GETTEXT_PACKAGE_ENGINE_OSCAR=smuxi-engine-oscar
AC_SUBST(GETTEXT_PACKAGE_ENGINE_OSCAR)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE_OSCAR, "$GETTEXT_PACKAGE_ENGINE_OSCAR", [Gettext package])

GETTEXT_PACKAGE_ENGINE_MSNP=smuxi-engine-msnp
AC_SUBST(GETTEXT_PACKAGE_ENGINE_MNSP)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE_MSNP, "$GETTEXT_PACKAGE_ENGINE_MSNP", [Gettext package])

GETTEXT_PACKAGE_SERVER=smuxi-server
AC_SUBST(GETTEXT_PACKAGE_SERVER)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_SERVER, "$GETTEXT_PACKAGE_SERVER", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_GNOME=smuxi-frontend
AC_SUBST(GETTEXT_PACKAGE_FRONTEND)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND, "$GETTEXT_PACKAGE_FRONTEND", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_GNOME=smuxi-frontend-gnome
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_GNOME)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_GNOME, "$GETTEXT_PACKAGE_FRONTEND_GNOME", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_GNOME_IRC=smuxi-frontend-gnome-irc
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_GNOME_IRC)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_GNOME_IRC, "$GETTEXT_PACKAGE_FRONTEND_GNOME_IRC", [Gettext package])

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

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_CHECK_MONO_MODULE(1.2.6)
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_CHECK_MONO_2_0_GAC_ASSEMBLIES([
	System
	System.Runtime.Remoting
	Mono.Posix
])

AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=NO]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=YES]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_DEBUG, true)
	enable_debug=yes
fi

PKG_CHECK_MODULES([LOG4NET], [log4net])
PKG_CHECK_MODULES([NINI], [nini-1.1])

# Engines
AC_ARG_ENABLE([engine-irc],
	AC_HELP_STRING([--enable-engine-irc], [Enable IRC protocol support (default yes)]),
	ENABLE_ENGINE_IRC=$enableval,
	ENABLE_ENGINE_IRC=yes
)
if test "x$ENABLE_ENGINE_IRC" != "xno"; then
	PKG_CHECK_MODULES([SMARTIRC4NET], [smartirc4net >= 0.4.5], IRC_SUPPORT=yes, IRC_SUPPORT=no)
	AC_SUBST(SMARTIRC4NET_LIBS)
	if test "x$ENABLE_ENGINE_IRC" = "xyes" -a "x$IRC_SUPPORT" != "xyes"; then
		AC_MSG_ERROR([SmartIrc4net >= 0.4.5 not found])
	else
		ENABLE_ENGINE_IRC=$IRC_SUPPORT
	fi
fi
AM_CONDITIONAL(ENABLE_ENGINE_IRC, test "x$ENABLE_ENGINE_IRC" = "xyes")

AC_ARG_ENABLE([engine-oscar],
	AC_HELP_STRING([--enable-engine-oscar], [Enable OSCAR (AIM/ICQ) protocol support (default no)]),
	ENABLE_ENGINE_OSCAR=$enableval,
	ENABLE_ENGINE_OSCAR=no
)
if test "x$ENABLE_ENGINE_OSCAR" != "xno"; then
	PKG_CHECK_MODULES([OSCARLIB], [oscarlib], OSCAR_SUPPORT=yes, OSCAR_SUPPORT=no)
	AC_SUBST(OSCARLIB_LIBS)
	if test "x$ENABLE_ENGINE_OSCAR" = "xyes" -a "x$OSCAR_ENGINE_SUPPORT" != "xyes"; then
		AC_MSG_ERROR([OscarLib not found])
	else
		ENABLE_ENGINE_OSCARC=$OSCAR_SUPPORT
	fi
fi
AM_CONDITIONAL(ENABLE_ENGINE_OSCAR, test "x$ENABLE_ENGINE_OSCAR" = "xyes")

AC_ARG_ENABLE([engine-xmpp],
	AC_HELP_STRING([--enable-engine-xmpp], [Enable XMPP (Jabber) protocol support (default no)]),
	ENABLE_ENGINE_XMPP=$enableval,
	ENABLE_ENGINE_XMPP=no
)
if test "x$ENABLE_ENGINE_XMPP" != "xno"; then
	PKG_CHECK_MODULES([JABBER_NET], [jabber-net], XMPP_SUPPORT=yes, XMPP_SUPPORT=no)
	AC_SUBST(JABBER_NET_LIBS)
	if test "x$ENABLE_ENGINE_XMPP" = "xyes" -a "x$XMPP_SUPPORT" != "xyes"; then
		AC_MSG_ERROR([jabber-net not found])
	else
		ENABLE_ENGINE_XMPP=$XMPP_SUPPORT
	fi
fi
AM_CONDITIONAL(ENABLE_ENGINE_XMPP, test "x$ENABLE_ENGINE_XMPP" = "xyes")

AC_ARG_ENABLE([engine-msnp],
	AC_HELP_STRING([--enable-engine-msnp], [Enable MSNP protocol support (default no)]),
	ENABLE_ENGINE_MSNP=$enableval,
	ENABLE_ENGINE_MSNP=no
)
if test "x$ENABLE_ENGINE_MSNP" != "xno"; then
	PKG_CHECK_MODULES([MSNPSHARP], [msnp-sharp], MSNP_SUPPORT=yes, MSNP_SUPPORT=no)
	AC_SUBST(MSNPSHARP_LIBS)
	if test "x$ENABLE_ENGINE_MSNP" = "xyes" -a "x$MSNP_SUPPORT" != "xyes"; then
		AC_MSG_ERROR([MSNPSharp not found])
	else
		ENABLE_ENGINE_MSNP=$MSNP_SUPPORT
	fi
fi
AM_CONDITIONAL(ENABLE_ENGINE_MSNP, test "x$ENABLE_ENGINE_MSNP" = "xyes")

# Frontends
AC_ARG_ENABLE([frontend-gnome],
	AC_HELP_STRING([--enable-frontend-gnome], [Enable GNOME frontend (default yes)]),
	ENABLE_FRONTEND_GNOME=$enableval,
	ENABLE_FRONTEND_GNOME=yes
)
if test "x$ENABLE_FRONTEND_GNOME" != "xno"; then
	PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0 >= 2.8])
	PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0 >= 2.8])
	PKG_CHECK_MODULES([GLADE_SHARP_20], [glade-sharp-2.0 >= 2.8])
	PKG_CHECK_MODULES([GNOME_SHARP_20], [gnome-sharp-2.0 >= 2.8])
	
	AC_MSG_CHECKING([for gtk-sharp version >= 2.10])
	if $PKG_CONFIG 'gtk-sharp-2.0 >= 2.10'; then
		AC_MSG_RESULT(yes)
		AC_DEFINE(GTK_SHARP_2_10,1,[System has gtk-sharp 2.10 or better])
		FRONTEND_GNOME_COMPILER_FLAGS=-define:GTK_SHARP_2_10
	else
		AC_MSG_RESULT(no)
	fi
	AC_SUBST(FRONTEND_GNOME_COMPILER_FLAGS)
	
	ENABLE_FRONTEND_GNOME_IRC=$ENABLE_ENGINE_IRC;
	ENABLE_FRONTEND_GNOME_XMPP=$ENABLE_ENGINE_XMPP;
fi
AM_CONDITIONAL(ENABLE_FRONTEND_GNOME, test "x$ENABLE_FRONTEND_GNOME" = "xyes")
AM_CONDITIONAL(ENABLE_FRONTEND_GNOME_IRC, test "x$ENABLE_FRONTEND_GNOME_IRC" = "xyes")
AM_CONDITIONAL(ENABLE_FRONTEND_GNOME_XMPP, test "x$ENABLE_FRONTEND_GNOME_XMPP" = "xyes")

AC_ARG_ENABLE([frontend-stfl],
	AC_HELP_STRING([--enable-frontend-stfl], [Enable STFL frontend (default no)]),
	ENABLE_FRONTEND_STFL=$enableval,
	ENABLE_FRONTEND_STFL=no
)
if test "x$ENABLE_FRONTEND_STFL" != "xno"; then
	# TODO: check deps
	AC_MSG_ERROR([STFL frontend not supported (yet)])
fi
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
	SHAMROCK_CHECK_MONO_2_0_GAC_ASSEMBLIES([
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
	src/Common/Makefile
	src/Common/smuxi-common.pc
	src/Engine/Makefile
	src/Engine/smuxi-engine.pc
	src/Engine-IRC/Makefile
	src/Engine-IRC/smuxi-engine-irc.pc
	src/Engine-MSNP/Makefile
	src/Engine-MSNP/smuxi-engine-msnp.pc
	src/Engine-OSCAR/Makefile
	src/Engine-OSCAR/smuxi-engine-oscar.pc
	src/Engine-XMPP/Makefile
	src/Engine-XMPP/smuxi-engine-xmpp.pc
	src/Server/Makefile
	src/Server/smuxi-server
	src/Frontend/Makefile
	src/Frontend/smuxi-frontend.pc
	src/Frontend-GNOME/Makefile
	src/Frontend-GNOME/smuxi-frontend-gnome
	src/Frontend-GNOME/smuxi-frontend-gnome.pc
	src/Frontend-GNOME-IRC/Makefile
	src/Frontend-GNOME-IRC/smuxi-frontend-gnome-irc.pc
	src/Frontend-GNOME-XMPP/Makefile
	src/Frontend-GNOME-XMPP/smuxi-frontend-gnome-xmpp.pc
	src/Frontend-Curses/Makefile
	src/Frontend-Curses/smuxi-frontend-curses
	src/Frontend-STFL/Makefile
	src/Frontend-STFL/smuxi-frontend-stfl
	src/Frontend-SWF/Makefile
	src/Frontend-SWF/smuxi-frontend-swf
	src/Frontend-Test/Makefile
	src/Frontend-Test/smuxi-frontend-test
	po/Makefile.in
	po-Engine/Makefile.in
	po-Engine-IRC/Makefile.in
	po-Server/Makefile.in
	po-Frontend-GNOME/Makefile.in
	po-Frontend-GNOME-IRC/Makefile.in
])

AC_OUTPUT

AC_MSG_RESULT([
	Configuration summary for $PACKAGE_NAME ($VERSION)
	
	* Installation prefix: $prefix
	* Debug build:         $enable_debug

	* Engines
	  -------
	  IRC:                 $ENABLE_ENGINE_IRC
	  XMPP:                $ENABLE_ENGINE_XMPP
	  OSCAR:               $ENABLE_ENGINE_OSCAR
	  MSNP:                $ENABLE_ENGINE_MSNP

	* Frontends
	  ---------
	  GNOME:               $ENABLE_FRONTEND_GNOME (IRC: $ENABLE_FRONTEND_GNOME_IRC XMPP: $ENABLE_FRONTEND_GNOME_XMPP)
	  Ncurses:             $ENABLE_FRONTEND_CURSES
	  STFL:                $ENABLE_FRONTEND_STFL
	  SWF (WinForms):      $ENABLE_FRONTEND_SWF
	  WPF:                 $ENABLE_FRONTEND_WPF
	  Test:                $ENABLE_FRONTEND_TEST
])
