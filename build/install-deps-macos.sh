#! /bin/sh
set -euxo pipefail

brew install automake
brew install intltool
brew install pkg-config
brew install gettext

#see https://unix.stackexchange.com/a/387362/56844
brew link gettext --force
