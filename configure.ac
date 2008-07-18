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
        AC_MSG_ERROR([Can't find "mono" in your PATH])
fi

AC_PATH_PROG(GMCS, gmcs)
if test "x$GMCS" = "x"; then
        AC_MSG_ERROR([Can't find "gmcs" in your PATH])
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

dnl package checks, per config

# I18N
AC_PROG_INTLTOOL([0.25])  
AM_PROG_LIBTOOL

POSUB="po po/Engine po/Frontend-GNOME"
AC_SUBST(POSUB)
  
#ALL_LINGUAS="de da"
GETTEXT_PACKAGE_ENGINE=smuxi-engine
GETTEXT_PACKAGE_ENGINE_IRC=smuxi-engine-irc
GETTEXT_PACKAGE_FRONTEND_GNOME=smuxi-frontend-gnome
AC_SUBST(GETTEXT_PACKAGE_ENGINE)
AC_SUBST(GETTEXT_PACKAGE_ENGINE_IRC)
AC_SUBST(GETTEXT_PACKAGE_FRONTEND_GNOME)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE, "$GETTEXT_PACKAGE_ENGINE", [Gettext package])
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_ENGINE_IRC, "$GETTEXT_PACKAGE_ENGINE_IRC", [Gettext package])
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE_FRONTEND_GNOME, "$GETTEXT_PACKAGE_FRONTEND_GNOME", [Gettext package])

AM_GNU_GETTEXT([external])
AM_GNU_GETTEXT_VERSION([0.17])
#AM_GLIB_GNU_GETTEXT

AC_CONFIG_FILES([
Makefile
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
