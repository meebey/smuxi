#!/usr/bin/env bash
set -euxo pipefail

sudo apt install -y snapd
snap version

# we can switch to a newer channel when we're ready to upgrade
# to use SNAPCRAFT_STORE_CREDENTIALS instead of --with when pushing
sudo snap install --classic snapcraft

snapcraft --version
