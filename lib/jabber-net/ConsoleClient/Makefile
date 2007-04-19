#--------------------------------------------------------------------------
# Copyrights
#
# Portions created by or assigned to Cursive Systems, Inc. are
# Copyright (c) 2002-2004 Cursive Systems, Inc.  All Rights Reserved.  Contact
# information for Cursive Systems, Inc. is available at
# http://www.cursive.net/.
#
# License
#
# Jabber-Net can be used under either JOSL or the GPL.
# See LICENSE.txt for details.
# --------------------------------------------------------------------------

# The purpose of this Makefile is to facilitate mono builds.

BASEDIR:=$(shell pwd)

SOURCES = \
$(BASEDIR)/AssemblyInfo.cs \
$(BASEDIR)/Main.cs 

ASSEMBLIES = -r:jabber-net.dll #-r:stringprep.dll

RESOURCES = 

SYSTEM_REFERENCES = -r:System.dll -r:System.Xml.dll

MCS_OPTIONS =   -lib:$(BASEDIR)/bin/debug -debug \
		-define:DEBUG

all: bin/debug/console-client.exe

bin/debug/console-client.exe: $(SOURCES) $(BASEDIR)/../bin/debug/jabber-net.dll #$(BASEDIR)/../bin/debug/stringprep.dll
	-mkdir -p bin/debug
	cp -f $(BASEDIR)/../bin/debug/jabber-net.dll bin/debug/
#	cp -f $(BASEDIR)/../bin/debug/stringprep.dll bin/debug/
	cd bin/debug && mcs $(MCS_OPTIONS) -target:exe \
	-out:"console-client.exe" $(RESOURCES) $(SYSTEM_REFERENCES) \
	$(SOURCES) $(ASSEMBLIES) 

clean:
	rm -rf bin
