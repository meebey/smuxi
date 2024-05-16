#!/usr/bin/env bash
set -euxo pipefail

DEBIAN_FRONTEND=noninteractive apt install --yes \
    intltool \
    pkg-config \
    libstfl-dev \
    libgtk2.0-cil-dev
