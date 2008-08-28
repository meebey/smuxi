#!/bin/sh
set -e

if [ "$(basename $PWD)" != "smuxi" ]; then
	echo "Creating smuxi directory..."
	mkdir smuxi
	cd smuxi
fi

echo Deleting old...
rm -f *

echo Downloading new files...
wget -q -r -np -nd --reject=html,=A,=D -X /temp/smuxi/win32 http://www.meebey.net/temp/smuxi/
mkdir -p ~/.config/smuxi/
if [ ! -f ~/.config/smuxi/smuxi-engine.ini ]; then
	echo "copying smuxi-engine.ini.orig to ~/.config/smuxi/smux-engine.ini"
	cp smuxi-engine.ini.orig ~/.config/smuxi/smuxi-engine.ini
fi
if [ ! -f ~/.config/smuxi/smuxi-frontend.ini ]; then
	echo "copying smuxi-frontend.ini.orig to smux-frontend.ini"
	cp smuxi-frontend.ini.orig ~/.config/smuxi/smuxi-frontend.ini
fi

echo Making executable...
chmod +x *.exe
chmod +x *.sh
