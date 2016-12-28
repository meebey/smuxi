#! /bin/sh

PROJECT=smuxi
FILE=
CONFIGURE=configure.ac

: ${AUTOCONF=autoconf}
: ${AUTOHEADER=autoheader}
: ${AUTOMAKE=automake}
: ${INTLTOOLIZE=intltoolize}
: ${ACLOCAL=aclocal}

srcdir=`dirname $0`
test -z "$srcdir" && srcdir=.

ORIGDIR=`pwd`
cd $srcdir
TEST_TYPE=-f
aclocalinclude="-I m4 $ACLOCAL_FLAGS"

DIE=0

($AUTOCONF --version) < /dev/null > /dev/null 2>&1 || {
        echo
        echo "You must have autoconf installed to compile $PROJECT."
        echo "Download the appropriate package for your distribution,"
        echo "or get the source tarball at ftp://ftp.gnu.org/pub/gnu/"
        DIE=1
}

($AUTOMAKE --version) < /dev/null > /dev/null 2>&1 || {
        echo
        echo "You must have automake installed to compile $PROJECT."
        echo "Get ftp://sourceware.cygnus.com/pub/automake/automake-1.4.tar.gz"
        echo "(or a newer version if it is available)"
        DIE=1
}

(grep "^IT_PROG_INTLTOOL" $CONFIGURE >/dev/null) && {
  ($INTLTOOLIZE --version) < /dev/null > /dev/null 2>&1 || {
    echo
    echo "**Error**: You must have \`intltool' installed."
    echo "You can get it from:"
    echo "  ftp://ftp.gnome.org/pub/GNOME/sources/intltool/"
    DIE=1
  }
}

if test "$DIE" -eq 1; then
        exit 1
fi
                                                                                
#test $TEST_TYPE $FILE || {
#        echo "You must run this script in the top-level $PROJECT directory"
#        exit 1
#}

if test -z "$*"; then
        echo "I am going to run ./configure with no arguments - if you wish "
        echo "to pass any to it, please specify them on the $0 command line."
fi

case $CC in
*xlc | *xlc\ * | *lcc | *lcc\ *) am_opt=--include-deps;;
esac

if grep "^IT_PROG_INTLTOOL" $CONFIGURE >/dev/null; then
	echo "Running $INTLTOOLIZE ..."
	$INTLTOOLIZE --copy --force --automake
fi

echo "Running $ACLOCAL $aclocalinclude ..."
$ACLOCAL $aclocalinclude

if grep "^AM_CONFIG_HEADER" $CONFIGURE >/dev/null; then
	echo "Running $AUTOHEADER ..."
	$AUTOHEADER
fi

echo "Running $AUTOMAKE ..."
$AUTOMAKE --add-missing --foreign $am_opt

echo "Running $AUTOCONF ..."
$AUTOCONF

if test x$NOGIT = x; then
    git submodule update --init --recursive || exit 1
else
    echo Skipping git submodule initialization.
fi

if test -d $srcdir/lib/messagingmenu-sharp; then
    echo Running lib/messagingmenu-sharp/autogen.sh ...
    (cd $srcdir/lib/messagingmenu-sharp; NOCONFIGURE=1 ./autogen.sh "$@")
    echo Done running lib/messagingmenu-sharp/autogen.sh ...
fi

if test x$NOCONFIGURE = x; then
    echo Running $srcdir/configure $conf_flags "$@" ...
    $srcdir/configure --enable-maintainer-mode $conf_flags "$@" || exit 1
else
    echo Skipping configure process.
fi
