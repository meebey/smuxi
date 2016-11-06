#! /bin/sh

if smuxi-server --list-users | grep -Fq "$SMUXI_USER"
then
  smuxi-server --modify-user --username="$SMUXI_USER" --password="$SMUXI_PASS"
else
  smuxi-server --add-user --username="$SMUXI_USER" --password="$SMUXI_PASS"
fi

# Make sure we listen on 0.0.0.0
sed -i 's/BindAddress = 127.0.0.1/BindAddress = 0.0.0.0/g' ~/.config/smuxi/smuxi-engine.ini

smuxi-server -d
