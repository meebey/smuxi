#!/bin/sh
set -e

if [ "$(basename $PWD)" != "smuxi" ]; then
	echo "Creating smuxi directory..."
	mkdir smuxi
	cd smuxi
fi

echo Deleting old...
for i in *; do
	if [ "$i" = "smuxi.ini" ]; then
		continue
	fi
	rm -f $i
done

echo Downloading new files...
wget -q -r -np -nd --reject=html,=A,=D http://www.meebey.net/temp/smuxi/
if [ ! -f "smuxi.ini" ]; then
	echo "copying smuxi.ini.orig to smux.ini"
	cp smuxi.ini.orig smuxi.ini
fi

echo Making executable...
chmod +x *.exe
chmod +x *.sh
