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
import os
import subprocess
os.chdir("/git/smuxi.git")

try:
    import GeoIP
except ImportError:
    print "You should install python-geoip"
    raise SystemExit(1)

from os.path import expanduser

HOME = expanduser("~")
IPS = set()
PTR=''
giV4 = GeoIP.new(GeoIP.GEOIP_MEMORY_CACHE)
giV6 = GeoIP.open("/usr/share/GeoIP/GeoIPv6.dat", GeoIP.GEOIP_STANDARD)

def parse_log():
    entries = dict()
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
    agent_p = re.compile(r'((?P<programV2>\S+\s+\-\s+\S+)|(?P<program>\S+))\s*(?P<version>[\S+]+/?)?\s*'+
                        r'\((?P<vendor>.+)\)\s+\-\s+running on\s+(?P<os>\S+)\s+?(?P<dist>\S+)?')

    for line in file:
        log_m = pattern.match(line)
        try:
            res = log_m.groupdict()
            agent_m = agent_p.match(res["agent"])
            agent_res = agent_m.groupdict()

            if res["host"] in IPS: continue
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
            res["PTR"] = PTR

            country = giV4.country_code_by_addr(res["host"])
            if country is None:
                country = giV6.country_code_by_addr_v6(res["host"])
                if country is None:
                    country = "Unknown"
            
            if agent_res["os"] == "GNU/Linux" and "(" in agent_res["dist"] : agent_res["os"] = "Linux " + agent_res["dist"][1]
            if agent_res["program"] == None: agent_res["program"] = agent_res["programV2"]

            if ":" in agent_res["vendor"]: agent_res["vendor"] = agent_res["vendor"].split(':')[1]
            elif "/" in agent_res["vendor"]:
                commithash = agent_res["vendor"].split("/")[-1]
                try:
                    agent_res["vendor"] = subprocess.check_output(["git", "describe", "%s"%commithash], stderr=subprocess.STDOUT).strip("\n")
                except subprocess.CalledProcessError:
                    agent_res["vendor"] = "Unknown (%s)"%commithash
                    
            elif len(agent_res["vendor"].split(' ')) == 2: 
                if (len(agent_res["vendor"].split(' ')[1]) < 3): agent_res["vendor"] = agent_res["version"]
                else: agent_res["vendor"] = agent_res["vendor"].split(' ')[1]
            elif len(agent_res["vendor"].split(' ')) == 3: agent_res["vendor"] = agent_res["vendor"].split(' ')[2]
            else: agent_res["vendor"] = agent_res["version"]           
            
            res["os"] = agent_res["os"]
            res["vendor"] =  agent_res["vendor"]
            res["program"] = agent_res["program"]
            res["version"] = agent_res["version"]
            res["country"] = country

            res["agent"] = res["agent"].split(" ")
            if len(res["agent"]) == 8:
                res["agent"][7] = res["agent"][6]

            if res["os"] == None:
                res["os"] = ""

            if res["vendor"] == None:
                res["vendor"] = ""

            if res["program"] == None:
                res["program"] = ""

            if res["version"] == None:
                res["version"] = ""

            if res["country"] == None:
                res["country"] = ""

            if res["agent"] == None:
                res["agent"] = ""

            entries[res["host"]] = res
        except AttributeError:
            pass
    return entries

if __name__=='__main__':
    log = parse_log()
    print "Smuxi World Domination Progress"
    print "==============================="
    print ""
    print "Number of unique IPs: %s"%len(log)
    print ""
    print 'IP'.ljust(25), '| PTR'.ljust(50), '  | OS'.ljust(15), '| Program'.ljust(22), '| Version'.ljust(20), '| C'
    print "-" * 145
    for x in log.values():
        print x["host"].ljust(25), '| ' + x["PTR"].ljust(50), '| ' + x["os"].ljust(16), '| ' + x["program"].ljust(22), '| ' + x["version"].ljust(15) , '| ' + x["country"]
