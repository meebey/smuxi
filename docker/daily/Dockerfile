# Docker scripts for Smuxi
#
# Copyright (C) 2014 Carlos Hernandez <carlos@techbyte.ca>
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

# Builds a docker image for smuxi
FROM ubuntu:trusty
MAINTAINER Carlos Hernandez <carlos@techbyte.ca>

# Let the container know that there is no tty
ENV DEBIAN_FRONTEND noninteractive

# Set user nobody to uid and gid of unRAID
RUN usermod -u 99 nobody
RUN usermod -g 100 nobody

# Set locale
ENV LANGUAGE en_US.UTF-8
ENV LANG en_US.UTF-8
RUN locale-gen en_US en_US.UTF-8
RUN update-locale LANG=en_US.UTF-8
RUN dpkg-reconfigure locales

# Update Ubuntu
RUN echo "deb http://ppa.launchpad.net/meebey/smuxi-daily/ubuntu trusty main" >> /etc/apt/sources.list
RUN apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 5C39B6F9FC6D77D5
RUN apt-get -q update
RUN apt-mark hold initscripts udev plymouth mountall
RUN apt-get -qy --force-yes dist-upgrade

# Install smuxi from apt
RUN usermod -m -d /config nobody
RUN apt-get install -qy --force-yes smuxi-engine

ADD ./start.sh /start.sh
ADD ./creds.conf /creds.conf
RUN chmod a+x  /start.sh

VOLUME /config

# DON'T RUN AS ROOT
USER nobody
ENTRYPOINT ["/start.sh"]
