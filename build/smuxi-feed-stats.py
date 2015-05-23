#!/usr/bin/env python2
#-*- coding: utf-8 -*-
# apt-get install python-geoip
#
# Copyright (c) 2015 James Axl <axlrose112@gmail.com>
#
# Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
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
# Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

import re
import time
import socket
import GeoIP
from os.path import expanduser

HOME = expanduser("~")
PTR=''
giV4 = GeoIP.new(GeoIP.GEOIP_MEMORY_CACHE)
giV6 = GeoIP.open("/usr/share/GeoIP/GeoIPv6.dat", GeoIP.GEOIP_STANDARD)

def unique_ips():
    file = open(HOME + '/smuxi-web/logs/access.log', 'r')
    ips = set()
    for line in file:
        ip = line.split()[0]
        ips.add(ip)
    return ips

def log():
    file = open(HOME + '/smuxi-web/logs/access.log', 'r')
    parts = [
        r'(?P<host>\S+)',                   # host %h
        r'\S+',                             # indent %l (unused)
        r'(?P<user>\S+)',                   # user %u
        r'\[(?P<time>.+)\]',                # time %t
        r'"(?P<request>.+)"',               # request "%r"
        r'(?P<status>[0-9]+)',              # status %>s
        r'(?P<size>\S+)',                   # size %b (careful, can be '-')
        r'"(?P<referer>.*)"',               # referer "%{Referer}i"
        r'"(?P<agent>(Smuxi|smuxi).*)"',    # user agent "%{User-agent}i"
    ]
    pattern = re.compile(r'\s+'.join(parts)+r'\s*\Z')

    for line in file:
        m = pattern.match(line)
        try:
            res = m.groupdict()
            if res["user"] == "-":
                res["user"] = None

            res["status"] = int(res["status"])

            if res["size"] == "-":
                res["size"] = 0
            else:
                res["size"] = int(res["size"])

            if res["referer"] == "-":
                res["referer"] = None
        
            try:
                PTR = socket.gethostbyaddr(res["host"])[0]
            except socket.herror:
                PTR = "Unknown"

            country = giV4.country_code_by_addr(res["host"])
            if country is None:
                country = giV6.country_code_by_addr_v6(res["host"])
                if country is None:
                    country = "Unknown"

            res["agent"] = res["agent"].split(" ")
            if len(res["agent"]) == 8:
                res["agent"][7] = res["agent"][6]

            print res["host"].ljust(25), '| ' + PTR.ljust(50), '| ' + res["agent"][7].ljust(16), '| ' + res["agent"][0].ljust(22), '| ' + res["agent"][1].ljust(15) , '| ' + country
        except AttributeError:
            pass

if __name__=='__main__':
    print "Smuxi World Domination Progress"
    print "==============================="
    print ""
    print "Number of unique IPs: %s"%len(unique_ips())
    print ""
    print 'IP'.ljust(25), '| PTR'.ljust(50), '  | OS'.ljust(20), '| Program'.ljust(24), '| Version'.ljust(17), '| C'
    print "-" * 145
    log()
