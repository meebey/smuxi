#! /bin/sh

if [ ! -e /config/creds.conf ]; then
  cp /creds.conf /config/
  chown nobody:users /config/creds.conf
  chmod 755 /config/creds.conf
fi

user=`grep -E '^user\s' /config/creds.conf  | cut -f 2`
pass=`grep -E '^pass\s' /config/creds.conf  | cut -f 2`

if smuxi-server --list-users | grep -Fxq "$user"
  then
  echo "User already exists."
else
  smuxi-server --add-user --username=$user --password=$pass
fi

smuxi-server
