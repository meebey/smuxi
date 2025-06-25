#!/usr/bin/env bash
set -euxo pipefail

sudo ./build/install-deps-ubuntu.sh

# just in case this is a retry-run, we want to clean artifacts from previous try
rm -rf ./staging

./autogen.sh --prefix=`pwd`/staging "$@"
make
make install

snapcraft --destructive-mode
