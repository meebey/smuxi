AC_PREREQ([2.54])
AC_INIT([smuxi], [0.6.0], http://smuxi.meebey.net/cgi-bin/trac/trac.cgi/newticket)
AM_INIT_AUTOMAKE([foreign])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

AC_PROG_INSTALL

AC_PATH_PROG(MONO, mono)
if test "x$MONO" = "x"; then
	AC_MSG_ERROR([You need to install the Mono runtime])
fi

AC_PATH_PROG(GMCS, gmcs)
if test "x$GMCS" = "x"; then
	AC_MSG_ERROR([You need to install the Mono gmcs compiler])
fi

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


dnl package checks, common for all configs
PKG_CHECK_MODULES([GNOME_SHARP_20], [gnome-sharp-2.0])
PKG_CHECK_MODULES([GLADE_SHARP_20], [glade-sharp-2.0])
PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])
PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0])
PKG_CHECK_MODULES([LOG4NET], [log4net])
PKG_CHECK_MODULES([NINI], [nini-1.1])

AC_ARG_ENABLE([irc],
	AC_HELP_STRING([--enable-irc], [Enable IRC protocol support (default yes)]),
	ENABLE_IRC=$enableval,
	ENABLE_IRC=yes
)
if test "x$ENABLE_IRC" != "xno"; then
	PKG_CHECK_MODULES([SMARTIRC4NET], [smartirc4net >= 0.4.5], IRC_SUPPORT=yes, IRC_SUPPORT=no)
	AC_SUBST(SMARTIRC4NET_LIBS)
	if test "x$ENABLE_IRC" = "xyes" -a "x$IRC_SUPPORT" != "xyes"; then
		AC_MSG_ERROR([SmartIrc4net >= 0.4.5 not found])
	else
		ENABLE_IRC=$IRC_SUPPORT
	fi
fi
AM_CONDITIONAL(ENABLE_IRC, test "x$ENABLE_IRC" = "xyes")

AC_ARG_ENABLE([oscar],
	AC_HELP_STRING([--enable-oscar], [Enable OSCAR (AIM/ICQ) protocol support (default no)]),
	ENABLE_OSCAR=$enableval,
	ENABLE_OSCAR=no
)
if test "x$ENABLE_OSCAR" != "xno"; then
	PKG_CHECK_MODULES([OSCARLIB], [oscarlib], OSCAR_SUPPORT=yes, OSCAR_SUPPORT=no)
	AC_SUBST(OSCARLIB_LIBS)
	if test "x$ENABLE_OSCAR" = "xyes" -a "x$OSCAR_SUPPORT" != "xyes"; then
		AC_MSG_ERROR([OscarLib not found])
	else
		ENABLE_OSCARC=$OSCAR_SUPPORT
	fi
fi
AM_CONDITIONAL(ENABLE_OSCAR, test "x$ENABLE_OSCAR" = "xyes")

AC_ARG_ENABLE([xmpp],
	AC_HELP_STRING([--enable-xmpp], [Enable XMPP (Jabber) protocol support (default no)]),
	ENABLE_XMPP=$enableval,
	ENABLE_XMPP=no
)
if test "x$ENABLE_XMPP" != "xno"; then
	PKG_CHECK_MODULES([JABBER_NET], [jabber-net], XMPP_SUPPORT=yes, XMPP_SUPPORT=no)
	AC_SUBST(JABBER_NET_LIBS)
	if test "x$ENABLE_XMPP" = "xyes" -a "x$XMPP_SUPPORT" != "xyes"; then
		AC_MSG_ERROR([jabber-net not found])
	else
		ENABLE_XMPP=$XMPP_SUPPORT
	fi
fi
AM_CONDITIONAL(ENABLE_XMPP, test "x$ENABLE_XMPP" = "xyes")

AC_ARG_ENABLE([msnp],
	AC_HELP_STRING([--enable-msnp], [Enable MSNP protocol support (default no)]),
	ENABLE_MSNP=$enableval,
	ENABLE_MSNP=no
)
if test "x$ENABLE_XMPP" != "xno"; then
	PKG_CHECK_MODULES([MSNPSHARP],  [msnp-sharp], MSNP_SUPPORT=yes, MSNP_SUPPORT=no)
	AC_SUBST(MSNPSHARP_LIBS)
	if test "x$ENABLE_MSNP" = "xyes" -a "x$MSNP_SUPPORT" != "xyes"; then
		AC_MSG_ERROR([MSNPSharp not found])
	else
		ENABLE_MSNP=$MSNP_SUPPORT
	fi
fi
AM_CONDITIONAL(ENABLE_MSNP, test "x$ENABLE_MSNP" = "xyes")

dnl package checks, per config

# I18N
AC_PROG_INTLTOOL([0.25])  
AM_PROG_LIBTOOL

POSUB="po po/Engine po/Frontend-GNOME"
AC_SUBST(POSUB)
  
GETTEXT_PACKAGE=smuxi
AC_SUBST(GETTEXT_PACKAGE)

GETTEXT_PACKAGE_ENGINE=smuxi-engine
AC_SUBST(GETTEXT_PACKAGE_ENGINE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE, "$GETTEXT_PACKAGE_ENGINE", [Gettext package])

GETTEXT_PACKAGE_ENGINE_IRC=smuxi-engine-irc
AC_SUBST(GETTEXT_PACKAGE_ENGINE_IRC)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE_IRC, "$GETTEXT_PACKAGE_ENGINE_IRC", [Gettext package])

GETTEXT_PACKAGE_FRONTEND_GNOME=smuxi-frontend-gnome
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_GNOME)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_GNOME, "$GETTEXT_PACKAGE_FRONTEND_GNOME", [Gettext package])

AM_GNU_GETTEXT([external])
AM_GNU_GETTEXT_VERSION([0.17])
#AM_GLIB_GNU_GETTEXT

AC_CONFIG_FILES([
Makefile
src/Makefile
src/Common/Makefile
src/Common/common.pc
src/Engine/Makefile
src/Engine/engine.pc
src/Engine-IRC/Makefile
src/Engine-IRC/engine-irc.pc
src/Engine-MSNP/Makefile
src/Engine-MSNP/engine-msnp.pc
src/Engine-OSCAR/Makefile
src/Engine-OSCAR/engine-oscar.pc
src/Engine-XMPP/Makefile
src/Engine-XMPP/engine-xmpp.pc
src/Server/Makefile
src/Server/server
src/Frontend/Makefile
src/Frontend/frontend.pc
src/Frontend-GNOME/Makefile
src/Frontend-GNOME/frontend-gnome
src/Frontend-GNOME-IRC/Makefile
src/Frontend-GNOME-IRC/frontend-gnome-irc.pc
src/Frontend-GNOME-XMPP/Makefile
src/Frontend-GNOME-XMPP/frontend-gnome-xmpp.pc
src/Frontend-Curses/Makefile
src/Frontend-Curses/frontend-curses
src/Frontend-STFL/Makefile
src/Frontend-STFL/frontend-stfl
src/Frontend-SWF/Makefile
src/Frontend-SWF/frontend-swf
src/Frontend-Test/Makefile
src/Frontend-Test/frontend-test
])

AC_OUTPUT([
src/Common/AssemblyInfo.cs
src/Engine/AssemblyInfo.cs
src/Engine-IRC/AssemblyInfo.cs
src/Server/AssemblyInfo.cs
src/Frontend/AssemblyInfo.cs
src/Frontend-GNOME/AssemblyInfo.cs
src/Frontend-GNOME-IRC/AssemblyInfo.cs
po/Makefile.in
po/Engine/Makefile.in
po/Frontend-GNOME/Makefile.in
])

echo "
	smuxi version:           ${VERSION}
	Prefix:                  ${prefix}
	
	Protocol Support
	----------------
	IRC:                     ${ENABLE_IRC}
	XMPP:                    ${ENABLE_XMPP}
	OSCAR:                   ${ENABLE_OSCAR}
	MSNP:                    ${ENABLE_MSNP}
"
