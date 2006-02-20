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
if [ ! -f "~/.config/smuxi/smuxi-engine.ini" ]; then
	echo "copying smuxi-engine.ini.orig to ~/.config/smuxi/smux-engine.ini"
	mkdir -p ~/.config/smuxi/
	cp smuxi-engine.ini.orig ~/.config/smuxi/smuxi-engine.ini
fi
if [ ! -f "~/.config/smuxi/smuxi-frontend.ini" ]; then
	echo "copying smuxi-frontend.ini.orig to smux-frontend.ini"
	mkdir -p ~/.config/smuxi/
	cp smuxi-frontend.ini.orig ~/.config/smuxi/smuxi-frontend.ini
fi

echo Making executable...
chmod +x *.exe
chmod +x *.sh
