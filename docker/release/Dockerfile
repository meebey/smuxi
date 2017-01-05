# Docker scripts for Smuxi
#
# Copyright (C) 2014 Carlos Hernandez <carlos@techbyte.ca>
# Copytight (C) 2016 Pascal Bach <pascal.bach@nextrem.ch>
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
FROM ubuntu:xenial
MAINTAINER Pascal Bach <pascal.bach@nextrem.ch>

# Update Ubuntu
RUN apt-get update && apt-get install -y \
    smuxi-engine\
 && rm -rf /var/lib/apt/lists/*

# Add smuxi user
RUN groupadd --system smuxi &&\
    useradd --system \
	--home /var/lib/smuxi \
	--create-home \
	--shell /usr/sbin/nologin \
	-g smuxi smuxi

ADD ./start.sh /start.sh
RUN chmod a+x  /start.sh

ENV SMUXI_USER username
ENV SMUXI_PASS password

VOLUME /var/lib/smuxi

EXPOSE 7689

# DON'T RUN AS ROOT
USER smuxi
ENTRYPOINT ["/start.sh"]
