#!/bin/sh
set -e

if [ "$(basename $PWD)" != "smuxi" ]; then
	echo "Creating smuxi directory..."
	mkdir smuxi
	cd smuxi
fi

echo Deleting old...
for i in *; do
	if [ "$i" = "smuxi.ini" -o "$i" = "smuxi-engine.ini" -o "$i" = "smuxi-frontend.ini" ]; then
		continue
	fi
	rm -f $i
done

echo Downloading new files...
wget -q -r -np -nd --reject=html,=A,=D http://www.meebey.net/temp/smuxi/
if [ ! -f "smuxi-engine.ini" ]; then
	echo "copying smuxi-engine.ini.orig to smux-engine.ini"
	cp smuxi-engine.ini.orig smuxi-engine.ini
fi
if [ ! -f "smuxi-frontend.ini" ]; then
	echo "copying smuxi-frontend.ini.orig to smux-frontend.ini"
	cp smuxi-frontend.ini.orig smuxi-frontend.ini
fi

echo Making executable...
chmod +x *.exe
chmod +x *.sh
