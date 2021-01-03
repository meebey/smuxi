# Dockerfile for Smuxi (smuxi-server)
#
# Copyright (c) 2020-2021 Mirco Bauer <meebey@meebey.net>
#
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

FROM mono:latest AS build-env
ARG DIST_VERSION

# install build-time dependencies from debian/control
WORKDIR /build
COPY debian/control /build/debian/control
RUN \
  apt-get update && \
  apt-get install -y devscripts equivs && \
  mk-build-deps --tool='apt-get -o Debug::pkgProblemResolver=yes --no-install-recommends --yes' \
    --install /build/debian/control && \
  rm -rf /var/lib/apt/lists/

COPY . /build
#RUN NOGIT=1 ./autogen.sh && NOGIT=1 make dist-linux-static
# mkbundle is broken with Mono 5.18 and 6.10, using make install as workaround
RUN \
  NOGIT=1 ./autogen.sh --prefix=/opt/smuxi \
    --without-indicate \
    --without-messaging-menu \
    --without-notify \
    --without-dbus \
    --with-db4o=included \
    --with-vendor-package-version="dist-docker ${DIST_VERSION}" \
    --disable-frontend-gnome \
    --disable-frontend-stfl && \
  make install

#FROM alpine:latest AS runtime-env
# without mkbundle we can't use Alpine for now as Alpine doesn't have Mono packages
#RUN apk --no-cache add ca-certificates
FROM mono:slim

# OpenContainers image annotations as per spec:
# https://github.com/opencontainers/image-spec/blob/master/annotations.md
LABEL "org.opencontainers.image.licenses"="GPL-2.0+"
LABEL "org.opencontainers.image.authors"="Mirco Bauer <meebey@meebey.net>"

# install runtime dependencies
RUN \
  apt-get update && \
  apt-get install -y --no-install-recommends smuxi-engine && \
  apt-get purge -y smuxi-engine && \
  rm -rf /var/lib/apt/lists/
COPY --from=build-env /opt/smuxi/ /opt/smuxi/

# for best security, create regular user so that the service doesn't run as root
RUN adduser --system --uid 7689 --group --disabled-password --home /data smuxi

USER smuxi
# storage of config and message history
VOLUME /data

# smuxi-server listens on 7689 but it needs to make a connection back to the
# frontend which may run on the same host (if a desktop) or a remote host
#EXPOSE 7689

ENTRYPOINT ["/opt/smuxi/bin/smuxi-server"]
