#!/usr/bin/env bash
set -euxo pipefail

sudo apt install -y snapd
snap version

# we can switch to a newer channel when we're ready to upgrade
# to use SNAPCRAFT_STORE_CREDENTIALS instead of --with when pushing
sudo snap install --classic snapcraft

# workaround for GithubActionsCI+snapcraft, see https://forum.snapcraft.io/t/permissions-problem-using-snapcraft-in-azure-pipelines/13258/14?u=knocte
sudo chown root:root /

snapcraft --version
